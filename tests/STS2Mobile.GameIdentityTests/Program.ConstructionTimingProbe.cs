using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace STS2Mobile.GameIdentityTests;

// Observational comparison: each process uses fresh types so a failed type
// initializer cannot contaminate another construction path.
internal static class ConstructionTimingContract
{
    private static readonly Dictionary<string, object> Dependencies = new();
    private static Action<string> _log = null!;
    private static int _explicitInitializations;
    private static int _requiredFieldInitializations;
    private static int _failedFieldInitializations;
    private static bool _deferredConstruction = true;

    internal static bool Run(Action<string> log)
    {
        _log = log;
        log("CONSTRUCTION_TIMING_BEGIN " + System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
        RunCase("activator-default", typeof(ActivatorDefault),
            () => (FixtureValue)Activator.CreateInstance(typeof(ActivatorDefault))!, () => ActivatorDefault.ReadDependency(), 17);
        RunCase("activator-args", typeof(ActivatorArgs),
            () => (FixtureValue)Activator.CreateInstance(typeof(ActivatorArgs), 23)!, () => ActivatorArgs.ReadDependency(), 23);
        RunCase("typed-newobj", typeof(TypedNew), NewTyped, () => TypedNew.ReadDependency(), 29);
        log("CONSTRUCTION_TIMING delegate-create-begin");
        var constructor = typeof(DelegateNew).GetConstructor(new[] { typeof(int) })!;
        var method = new DynamicMethod("construction-timing-newobj", typeof(FixtureValue), new[] { typeof(int) },
            typeof(ConstructionTimingContract), skipVisibility: true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Newobj, constructor);
        il.Emit(OpCodes.Ret);
        var factory = (Func<int, FixtureValue>)method.CreateDelegate(typeof(Func<int, FixtureValue>));
        log("CONSTRUCTION_TIMING delegate-create-end");
        RunCase("delegate-newobj", typeof(DelegateNew), () => factory(31), () => DelegateNew.ReadDependency(), 31);
        VerifyRequiredInitialization();
        VerifyFailedFieldInitialization();
        log("CONSTRUCTION_TIMING_END deferred=" + _deferredConstruction);
        return _deferredConstruction;
    }

    private static void RunCase(string name, Type type, Func<FixtureValue> construct, Func<object> readDependency, int expected)
    {
        if ((type.Attributes & TypeAttributes.BeforeFieldInit) == 0)
            throw new InvalidOperationException("Timing fixture must retain beforefieldinit: " + name);
        _log("CONSTRUCTION_TIMING " + name + " construct-begin");
        try
        {
            var value = construct();
            if (value.Value != expected) throw new InvalidOperationException("Constructor argument was lost: " + name);
            _log("CONSTRUCTION_TIMING " + name + " constructed value=" + value.Value);
        }
        catch (Exception ex)
        {
            _deferredConstruction = false;
            _log("CONSTRUCTION_TIMING " + name + " construct-error " + Describe(ex));
        }
        var dependency = new object();
        Dependencies.Add(name, dependency);
        _log("CONSTRUCTION_TIMING " + name + " dependency-ready");
        try
        {
            if (!ReferenceEquals(dependency, readDependency()))
                throw new InvalidOperationException("Static field did not receive its dependency: " + name);
            _log("CONSTRUCTION_TIMING " + name + " field-read-pass");
        }
        catch (Exception ex)
        {
            _deferredConstruction = false;
            _log("CONSTRUCTION_TIMING " + name + " field-read-error " + Describe(ex));
        }
    }

    private static object Lookup(string name)
    {
        _log("CONSTRUCTION_TIMING " + name + " cctor dependency-ready=" + Dependencies.ContainsKey(name));
        return Dependencies[name];
    }

    private static string Describe(Exception ex)
        => ex.GetType().Name + ":" + ex.Message.Replace('\n', ' ') + (ex.InnerException is null ? "" : " -> " + Describe(ex.InnerException));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static FixtureValue NewTyped() => new TypedNew(29);

    private static void VerifyRequiredInitialization()
    {
        if ((typeof(ExplicitInitialization).Attributes & TypeAttributes.BeforeFieldInit) != 0)
            throw new InvalidOperationException("Explicit initializer fixture unexpectedly has beforefieldinit.");
        var value = (ExplicitInitialization)Activator.CreateInstance(typeof(ExplicitInitialization), 37)!;
        if (value.Value != 37 || _explicitInitializations != 1)
            throw new InvalidOperationException("Explicit type initialization or constructor arguments were lost.");
        try
        {
            Activator.CreateInstance(typeof(ExplicitInitialization), -1);
            throw new InvalidOperationException("Constructor failure was swallowed.");
        }
        catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException { Message: "construction-timing-failure" }) { }
        var readsField = (ConstructorReadsField)Activator.CreateInstance(typeof(ConstructorReadsField), 3)!;
        if (readsField.Value != 43 || _requiredFieldInitializations != 1)
            throw new InvalidOperationException("A constructor must initialize the static field it actually reads.");
        // This beforefieldinit type has already reached its legitimate field
        // access above; its ordinary constructor must still propagate errors.
        if (_deferredConstruction)
        {
            try
            {
                Activator.CreateInstance(typeof(ActivatorArgs), -1);
                throw new InvalidOperationException("Beforefieldinit constructor failure was swallowed.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException { Message: "construction-timing-failure" }) { }
        }
        _log("CONSTRUCTION_TIMING explicit-cctor-pass own-static-field-pass arguments-pass constructor-failure-pass");
    }

    private abstract class FixtureValue
    {
        internal readonly int Value;
        protected FixtureValue(int value)
        {
            if (value < 0) throw new InvalidOperationException("construction-timing-failure");
            Value = value;
        }
    }

    private sealed class ActivatorDefault : FixtureValue
    {
        private static readonly object Dependency = Lookup("activator-default");
        [MethodImpl(MethodImplOptions.NoInlining)] public ActivatorDefault() : base(17) { }
        [MethodImpl(MethodImplOptions.NoInlining)] internal static object ReadDependency() => Dependency;
    }

    private sealed class ActivatorArgs : FixtureValue
    {
        private static readonly object Dependency = Lookup("activator-args");
        [MethodImpl(MethodImplOptions.NoInlining)] public ActivatorArgs(int value) : base(value) { }
        [MethodImpl(MethodImplOptions.NoInlining)] internal static object ReadDependency() => Dependency;
    }

    private sealed class TypedNew : FixtureValue
    {
        private static readonly object Dependency = Lookup("typed-newobj");
        [MethodImpl(MethodImplOptions.NoInlining)] public TypedNew(int value) : base(value) { }
        [MethodImpl(MethodImplOptions.NoInlining)] internal static object ReadDependency() => Dependency;
    }

    private sealed class DelegateNew : FixtureValue
    {
        private static readonly object Dependency = Lookup("delegate-newobj");
        [MethodImpl(MethodImplOptions.NoInlining)] public DelegateNew(int value) : base(value) { }
        [MethodImpl(MethodImplOptions.NoInlining)] internal static object ReadDependency() => Dependency;
    }

    private sealed class ExplicitInitialization : FixtureValue
    {
        static ExplicitInitialization() => _explicitInitializations++;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public ExplicitInitialization(int value) : base(value)
        {
            if (_explicitInitializations != 1) throw new InvalidOperationException("Explicit initializer must run before the constructor.");
            if (value < 0) throw new InvalidOperationException("construction-timing-failure");
        }
    }

    private static int RequiredSeed()
    {
        _requiredFieldInitializations++;
        return 40;
    }

    private sealed class ConstructorReadsField : FixtureValue
    {
        private static readonly int Seed = RequiredSeed();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public ConstructorReadsField(int value) : base(Seed + value) { }
    }

    private static void VerifyFailedFieldInitialization()
    {
        try
        {
            var instance = (FailingFieldInitializer)Activator.CreateInstance(typeof(FailingFieldInitializer))!;
            if (instance.Value != 41) throw new InvalidOperationException("Failing-field constructor did not run normally.");
        }
        catch (TargetInvocationException ex) when (ex.InnerException is TypeInitializationException)
        {
            _deferredConstruction = false;
            _log("CONSTRUCTION_TIMING throwing-cctor eagerly failed construction");
            return;
        }
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                FailingFieldInitializer.ReadDependency();
                throw new InvalidOperationException("Static initializer failure was swallowed.");
            }
            catch (TypeInitializationException ex) when (ex.InnerException is InvalidOperationException { Message: "construction-timing-static-failure" }) { }
        }
        if (_failedFieldInitializations != 1)
            throw new InvalidOperationException("A failed static initializer must remain cached.");
        _log("CONSTRUCTION_TIMING throwing-cctor-field-read-pass cached-failure-pass");
    }

    private static object FailedDependency()
    {
        _failedFieldInitializations++;
        throw new InvalidOperationException("construction-timing-static-failure");
    }

    private sealed class FailingFieldInitializer : FixtureValue
    {
        private static readonly object Dependency = FailedDependency();
        [MethodImpl(MethodImplOptions.NoInlining)] public FailingFieldInitializer() : base(41) { }
        [MethodImpl(MethodImplOptions.NoInlining)] internal static object ReadDependency() => Dependency;
    }
}
