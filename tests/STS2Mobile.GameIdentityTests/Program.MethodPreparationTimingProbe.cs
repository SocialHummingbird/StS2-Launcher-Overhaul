using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace STS2Mobile.GameIdentityTests;

internal static partial class MethodPreparationTimingContract
{
    private static readonly Dictionary<string, object> Dependencies = new();
    private static readonly Dictionary<string, int> Initializations = new();
    private static Action<string> _log = null!;
    private static bool _passed = true;

    internal static bool Run(Harmony harmony, Action<string> log)
    {
        _log = log;
        log("METHOD_PREPARATION_TIMING_BEGIN");
        RunCase("prepare-getter", typeof(PreparedGetter),
            getter => RuntimeHelpers.PrepareMethod(getter.MethodHandle), () => PreparedGetter.ReadDependency());
        RunCase("function-pointer-getter", typeof(PointerGetter),
            getter => { _ = getter.MethodHandle.GetFunctionPointer(); }, () => PointerGetter.ReadDependency());
        RunCase("harmony-getter", typeof(PatchedGetter),
            getter => harmony.Patch(getter, postfix: new HarmonyMethod(typeof(MethodPreparationTimingContract), nameof(NoopPostfix))),
            () => PatchedGetter.ReadDependency());
        ObserveExplicitOwner();
        RunInvocationCase("invoke-pointer", typeof(PointerInvocation), (_, pointer) => PointerInvoker(pointer));
        RunInvocationCase("invoke-direct", typeof(DirectInvocation), (_, _) => DirectInvocation.Add);
        RunInvocationCase("invoke-reflection", typeof(ReflectionInvocation), (method, _) => value => (int)method.Invoke(null, new object[] { value })!);
        RunInvocationCase("invoke-delegate", typeof(DelegateInvocation), (method, _) => (Func<int, int>)method.CreateDelegate(typeof(Func<int, int>)));
        RunInvocationCase("cross-type-field", typeof(CrossFieldCaller), (_, pointer) => PointerInvoker(pointer));
        RunInvocationCase("inlined-callee", typeof(InlineCaller), (_, pointer) => PointerInvoker(pointer));
        VerifyReentrancyAndCachedFailure();
        VerifyPreparationBoundaries(harmony);
        log("METHOD_PREPARATION_TIMING_END deferred=" + _passed);
        return _passed;
    }

    private static bool RunInvocationCase(string name, Type owner, Func<MethodInfo, IntPtr, Func<int, int>> makeInvoker)
    {
        _log("METHOD_PREPARATION_INVOCATION " + name + " pointer-begin");
        try
        {
            var method = owner.GetMethod("Add", BindingFlags.Static | BindingFlags.Public)!;
            var pointer = method.MethodHandle.GetFunctionPointer();
            _ = method.MethodHandle.GetFunctionPointer();
            var invoke = makeInvoker(method, pointer);
            if (Initializations.GetValueOrDefault(name) != 0)
                throw new InvalidOperationException("Method preparation ran a managed initializer: " + name);
            Dependencies.Add(name, new object());
            _log("METHOD_PREPARATION_INVOCATION " + name + " dependency-ready first-call");
            if (invoke(5) != 53 || Initializations.GetValueOrDefault(name) != 1)
                throw new InvalidOperationException("First invocation must initialize once and preserve its argument: " + name);
            _ = method.MethodHandle.GetFunctionPointer();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (invoke(7) != 55 || Initializations.GetValueOrDefault(name) != 1)
                throw new InvalidOperationException("Cached entry must preserve initialization and arguments after collection: " + name);
            _log("METHOD_PREPARATION_INVOCATION " + name + " pass cached-pass gc-pass");
            return true;
        }
        catch (Exception error)
        {
            _passed = false;
            _log("METHOD_PREPARATION_INVOCATION " + name + " error " + error);
            return false;
        }
    }

    private static Func<int, int> PointerInvoker(IntPtr pointer)
    {
        var method = new DynamicMethod("method-preparation-calli", typeof(int), new[] { typeof(int) }, typeof(MethodPreparationTimingContract), true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I8, pointer.ToInt64());
        il.Emit(OpCodes.Conv_I);
        il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(int), new[] { typeof(int) }, null);
        il.Emit(OpCodes.Ret);
        return (Func<int, int>)method.CreateDelegate(typeof(Func<int, int>));
    }

    private static void RunCase(string name, Type type, Action<MethodInfo> prepare, Func<object> readDependency)
    {
        if ((type.Attributes & TypeAttributes.BeforeFieldInit) == 0)
            throw new InvalidOperationException("Method preparation fixture must have beforefieldinit: " + name);
        var getter = type.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public)!.GetMethod!;
        _log("METHOD_PREPARATION_TIMING " + name + " prepare-begin");
        try
        {
            prepare(getter);
            _log("METHOD_PREPARATION_TIMING " + name + " prepare-complete");
        }
        catch (Exception error)
        {
            _passed = false;
            _log("METHOD_PREPARATION_TIMING " + name + " prepare-error " + error);
        }
        var dependency = new object();
        Dependencies.Add(name, dependency);
        _log("METHOD_PREPARATION_TIMING " + name + " dependency-ready");
        try
        {
            if (!ReferenceEquals(dependency, readDependency()))
                throw new InvalidOperationException("A required static field lost its initialization: " + name);
            var instance = Activator.CreateInstance(type)!;
            if ((int)getter.Invoke(instance, null)! != 53)
                throw new InvalidOperationException("The prepared instance getter lost its ordinary result: " + name);
            _log("METHOD_PREPARATION_TIMING " + name + " field-read-and-getter-pass");
        }
        catch (Exception error)
        {
            _passed = false;
            _log("METHOD_PREPARATION_TIMING " + name + " field-read-error " + error);
        }
    }

    private static object Lookup(string name)
    {
        Initializations[name] = Initializations.GetValueOrDefault(name) + 1;
        _log("METHOD_PREPARATION_TIMING " + name + " cctor dependency-ready=" + Dependencies.ContainsKey(name));
        return Dependencies[name];
    }

    private static void NoopPostfix(ref int __result) { }

    private static void ObserveExplicitOwner()
    {
        const string name = "explicit-owner-control";
        var getter = typeof(ExplicitGetter).GetProperty(nameof(ExplicitGetter.Value))!.GetMethod!;
        _log("METHOD_PREPARATION_CONTROL explicit-owner pointer-before-dependency");
        try
        {
            _ = getter.MethodHandle.GetFunctionPointer();
            _log("METHOD_PREPARATION_CONTROL explicit-owner pointer-complete");
        }
        catch (Exception error)
        {
            _passed = false;
            _log("METHOD_PREPARATION_CONTROL explicit-owner pointer-error " + error);
        }
        Dependencies.Add(name, new object());
        _log("METHOD_PREPARATION_CONTROL explicit-owner dependency-ready first-invocation-begin");
        try
        {
            if ((int)getter.Invoke(new ExplicitGetter(), null)! != 53)
                throw new InvalidOperationException("Explicit-owner first invocation returned the wrong value.");
            _log("METHOD_PREPARATION_CONTROL explicit-owner first-invocation-pass");
        }
        catch (Exception error)
        {
            _passed = false;
            _log("METHOD_PREPARATION_CONTROL explicit-owner first-invocation-error " + error);
        }
    }

    private static int CheckValue(string name, object dependency, int value)
    {
        if (!ReferenceEquals(Dependencies[name], dependency))
            throw new InvalidOperationException("Required dependency was not initialized: " + name);
        return value + 48;
    }

    private static void VerifyReentrancyAndCachedFailure()
    {
        try
        {
            var reentrant = typeof(ReentrantInvocation).GetMethod(nameof(ReentrantInvocation.Add))!.MethodHandle.GetFunctionPointer();
            if (_reentrantInitializations != 0)
                throw new InvalidOperationException("Reentrant owner's initializer ran during preparation.");
            if (PointerInvoker(reentrant)(10) != 17 || _reentrantInitializations != 1)
                throw new InvalidOperationException("Reentrant initializer did not execute exactly once.");
            var failing = typeof(FailingInvocation).GetMethod(nameof(FailingInvocation.Add))!.MethodHandle.GetFunctionPointer();
            if (_failingInitializations != 0)
                throw new InvalidOperationException("Throwing initializer ran during preparation.");
            var invoke = PointerInvoker(failing);
            for (var attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    invoke(attempt);
                    throw new InvalidOperationException("Actual type initializer failure was swallowed.");
                }
                catch (TypeInitializationException error) when (error.InnerException is InvalidOperationException { Message: "method-preparation-static-failure" }) { }
            }
            if (_failingInitializations != 1)
                throw new InvalidOperationException("Failed initializer must remain cached after direct pointer invocation.");
            _log("METHOD_PREPARATION_INVOCATION reentrancy-pass cached-failure-pass");
        }
        catch (Exception error)
        {
            _passed = false;
            _log("METHOD_PREPARATION_INVOCATION reentrancy-or-failure-error " + error);
        }
    }

    private sealed class PreparedGetter
    {
        private static readonly object Dependency = Lookup("prepare-getter");
        [MethodImpl(MethodImplOptions.NoInlining)] public PreparedGetter() { }
        public int Value { [MethodImpl(MethodImplOptions.NoInlining)] get => 53; }
        [MethodImpl(MethodImplOptions.NoInlining)] internal static object ReadDependency() => Dependency;
    }

    private sealed class PointerGetter
    {
        private static readonly object Dependency = Lookup("function-pointer-getter");
        [MethodImpl(MethodImplOptions.NoInlining)] public PointerGetter() { }
        public int Value { [MethodImpl(MethodImplOptions.NoInlining)] get => 53; }
        [MethodImpl(MethodImplOptions.NoInlining)] internal static object ReadDependency() => Dependency;
    }

    private sealed class PatchedGetter
    {
        private static readonly object Dependency = Lookup("harmony-getter");
        [MethodImpl(MethodImplOptions.NoInlining)] public PatchedGetter() { }
        public int Value { [MethodImpl(MethodImplOptions.NoInlining)] get => 53; }
        [MethodImpl(MethodImplOptions.NoInlining)] internal static object ReadDependency() => Dependency;
    }

    private sealed class ExplicitGetter
    {
        private static readonly object Dependency;
        static ExplicitGetter() => Dependency = Lookup("explicit-owner-control");
        public ExplicitGetter()
        {
            if (!ReferenceEquals(Dependency, Dependencies["explicit-owner-control"]))
                throw new InvalidOperationException("Explicit initializer did not run before the constructor.");
        }
        public int Value { [MethodImpl(MethodImplOptions.NoInlining)] get => 53; }
    }

    private static class PointerInvocation
    {
        private static readonly object Dependency;
        static PointerInvocation() => Dependency = Lookup("invoke-pointer");
        [MethodImpl(MethodImplOptions.NoInlining)] public static int Add(int value) => CheckValue("invoke-pointer", Dependency, value);
    }
    private static class DirectInvocation
    {
        private static readonly object Dependency;
        static DirectInvocation() => Dependency = Lookup("invoke-direct");
        [MethodImpl(MethodImplOptions.NoInlining)] public static int Add(int value) => CheckValue("invoke-direct", Dependency, value);
    }
    private static class ReflectionInvocation
    {
        private static readonly object Dependency;
        static ReflectionInvocation() => Dependency = Lookup("invoke-reflection");
        [MethodImpl(MethodImplOptions.NoInlining)] public static int Add(int value) => CheckValue("invoke-reflection", Dependency, value);
    }
    private static class DelegateInvocation
    {
        private static readonly object Dependency;
        static DelegateInvocation() => Dependency = Lookup("invoke-delegate");
        [MethodImpl(MethodImplOptions.NoInlining)] public static int Add(int value) => CheckValue("invoke-delegate", Dependency, value);
    }
    private static class CrossFieldCaller
    {
        [MethodImpl(MethodImplOptions.NoInlining)] public static int Add(int value) => CheckValue("cross-type-field", CrossFieldOwner.Dependency, value);
    }
    private static class CrossFieldOwner
    {
        internal static readonly object Dependency = Lookup("cross-type-field");
    }
    private static class InlineCaller
    {
        [MethodImpl(MethodImplOptions.NoInlining)] public static int Add(int value) => InlineCallee.Add(value);
    }
    private static class InlineCallee
    {
        private static readonly object Dependency = Lookup("inlined-callee");
        [MethodImpl(MethodImplOptions.AggressiveInlining)] internal static int Add(int value) => CheckValue("inlined-callee", Dependency, value);
    }
    private static int _reentrantInitializations;
    private static class ReentrantInvocation
    {
        private static readonly int Seed;
        static ReentrantInvocation()
        {
            _reentrantInitializations++;
            Seed = 7;
            var self = typeof(ReentrantInvocation).GetMethod(nameof(Add))!.MethodHandle.GetFunctionPointer();
            if (PointerInvoker(self)(2) != 9)
                throw new InvalidOperationException("Reentrant class initialization lost its initialized state.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)] public static int Add(int value) => Seed + value;
    }
    private static int _failingInitializations;
    private static class FailingInvocation
    {
        static FailingInvocation()
        {
            _failingInitializations++;
            throw new InvalidOperationException("method-preparation-static-failure");
        }
        [MethodImpl(MethodImplOptions.NoInlining)] public static int Add(int value) => value;
    }
}
