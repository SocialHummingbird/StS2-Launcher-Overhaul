using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HarmonyLib;

namespace STS2Mobile.GameIdentityTests;

internal static partial class MethodPreparationTimingContract
{
    private static int _ownEntryInitializations, _ownBodyEntries, _ownFinallyEntries;
    private static int _fieldInitializations, _fieldCatches;
    private static int _normalFailureInitializations, _normalFailureBodyEntries;
    private static int _patchedFailureInitializations, _patchedFailurePrefixes, _patchedFailureFinalizers;

    private static void VerifyPreparationBoundaries(Harmony harmony)
    {
        ObserveBoundary("own-cctor-outside-eh", () =>
        {
            var invoke = PointerInvoker(typeof(OwnEntryFailure).GetMethod(nameof(OwnEntryFailure.Add))!.MethodHandle.GetFunctionPointer());
            if (_ownEntryInitializations != 0) throw new Exception("Preparation ran the owner initializer.");
            ExpectInitializerFailure(() => invoke(5), "own-entry-failure");
            if (_ownEntryInitializations != 1 || _ownBodyEntries != 0 || _ownFinallyEntries != 0)
                throw new Exception("The method's catch/finally must not intercept owner entry initialization.");
        });
        ObserveBoundary("field-cctor-inside-eh", () =>
        {
            var invoke = PointerInvoker(typeof(CatchesFieldFailure).GetMethod(nameof(CatchesFieldFailure.Add))!.MethodHandle.GetFunctionPointer());
            if (_fieldInitializations != 0) throw new Exception("Preparation ran the field initializer.");
            if (invoke(5) != -1 || _fieldCatches != 1 || _fieldInitializations != 1)
                throw new Exception("A body must be able to catch a required field access failure.");
        });
        ObserveBoundary("normally-compiled-failure-cache", () =>
        {
            ExpectInitializerFailure(() => NormalFailure.Add(5), "normal-entry-failure");
            var invoke = PointerInvoker(typeof(NormalFailure).GetMethod(nameof(NormalFailure.Add))!.MethodHandle.GetFunctionPointer());
            ExpectInitializerFailure(() => invoke(7), "normal-entry-failure");
            if (_normalFailureInitializations != 1 || _normalFailureBodyEntries != 0)
                throw new Exception("A cached normal body bypassed its failed initializer.");
        });
        ObserveBoundary("shared-generic-owner", () =>
        {
            Dependencies.Add("generic-FirstOwner", new object());
            if (SharedOwner<FirstOwner>.Add(5) != 53) throw new Exception("Normal generic warmup failed.");
            if (!RunInvocationCase("generic-SecondOwner", typeof(SharedOwner<SecondOwner>), (_, pointer) => PointerInvoker(pointer)))
                throw new Exception("Shared generic owner invocation failed.");
        });
        RunInvocationCase("thread-static", typeof(ThreadFieldOwner), (_, pointer) => PointerInvoker(pointer));
        ObserveBoundary("dynamic-owner-association", () =>
        {
            var method = new DynamicMethod("association-only", typeof(int), Type.EmptyTypes, typeof(DynamicAssociationOwner), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldc_I4, 53);
            il.Emit(OpCodes.Ret);
            var invoke = (Func<int>)method.CreateDelegate(typeof(Func<int>));
            if (invoke() != 53 || Initializations.GetValueOrDefault("dynamic-owner-only") != 0)
                throw new Exception("A DynamicMethod's access owner was treated as a declared method owner.");
            var dependency = new object();
            Dependencies.Add("dynamic-owner-only", dependency);
            if (!ReferenceEquals(dependency, DynamicAssociationOwner.ReadDependency()))
                throw new Exception("A real owner field access did not initialize.");
        });
        RunInvocationCase("harmony-static-first-call", typeof(PatchedStaticOwner), (method, pointer) =>
        {
            harmony.Patch(method, prefix: new HarmonyMethod(typeof(MethodPreparationTimingContract), nameof(RequireInitializedPrefix)));
            return PointerInvoker(pointer);
        });
        ObserveBoundary("harmony-static-failed-first-call", () =>
        {
            var method = typeof(PatchedStaticFailure).GetMethod(nameof(PatchedStaticFailure.Add))!;
            var pointer = method.MethodHandle.GetFunctionPointer();
            harmony.Patch(method, prefix: new HarmonyMethod(typeof(MethodPreparationTimingContract), nameof(FailedOwnerPrefix)),
                finalizer: new HarmonyMethod(typeof(MethodPreparationTimingContract), nameof(FailedOwnerFinalizer)));
            if (_patchedFailureInitializations != 0) throw new Exception("Patching ran the failing owner initializer.");
            ExpectInitializerFailure(() => PointerInvoker(pointer)(5), "patched-entry-failure");
            if (_patchedFailureInitializations != 1 || _patchedFailurePrefixes != 0 || _patchedFailureFinalizers != 0)
                throw new Exception("A patch prefix ran before the owner's failed initialization.");
        });
        RunInvocationCase("invoke-native", RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? typeof(WindowsNativeOwner) : typeof(AndroidNativeOwner),
            (_, pointer) => { var invoke = PointerInvoker(pointer); return value => invoke(-48 - value); });
        ObserveBoundary("module-initializer", () =>
        {
            var root = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? AppContext.BaseDirectory : "/data/local/tmp/sts2-mono-transition-test";
            var assembly = Assembly.LoadFrom(Path.Combine(root, "AndroidModuleFixture.dll"));
            var method = assembly.GetType("RuntimeModuleFixture.Target", true)!.GetMethod("Add")!;
            var invoke = PointerInvoker(method.MethodHandle.GetFunctionPointer());
            if (invoke(5) != 53 || ModuleInitializationObserver.Count != 1)
                throw new Exception("First method invocation did not run the module initializer exactly once.");
        });
    }

    private static void ObserveBoundary(string name, Action verify)
    {
        try { verify(); _log("METHOD_PREPARATION_BOUNDARY " + name + " pass"); }
        catch (Exception error) { _passed = false; _log("METHOD_PREPARATION_BOUNDARY " + name + " error " + error); }
    }

    private static void ExpectInitializerFailure(Action invoke, string message)
    {
        try { invoke(); }
        catch (TypeInitializationException error) when (error.InnerException is InvalidOperationException original && original.Message == message) { return; }
        throw new Exception("Required initializer failure did not escape: " + message);
    }

    private static void RequireInitializedPrefix()
    {
        if (Initializations.GetValueOrDefault("harmony-static-first-call") != 1)
            throw new Exception("The patch prefix ran before its declared owner initialized.");
    }
    private static void FailedOwnerPrefix() => _patchedFailurePrefixes++;
    private static Exception? FailedOwnerFinalizer(Exception? __exception) { _patchedFailureFinalizers++; return null; }

    private static class OwnEntryFailure
    {
        static OwnEntryFailure() { _ownEntryInitializations++; throw new InvalidOperationException("own-entry-failure"); }
        [MethodImpl(MethodImplOptions.NoInlining)] public static int Add(int value)
        {
            try { _ownBodyEntries++; return value; }
            catch (TypeInitializationException) { return -1; }
            finally { _ownFinallyEntries++; }
        }
    }
    private static class CatchesFieldFailure
    {
        private static readonly int Value = Fail();
        private static int Fail() { _fieldInitializations++; throw new InvalidOperationException("field-entry-failure"); }
        [MethodImpl(MethodImplOptions.NoInlining)] public static int Add(int value)
        {
            try { return Value + value; }
            catch (TypeInitializationException) { _fieldCatches++; return -1; }
        }
    }
    private static class NormalFailure
    {
        static NormalFailure() { _normalFailureInitializations++; throw new InvalidOperationException("normal-entry-failure"); }
        [MethodImpl(MethodImplOptions.NoInlining)] public static int Add(int value) { _normalFailureBodyEntries++; return value; }
    }
    private sealed class FirstOwner { }
    private sealed class SecondOwner { }
    private static class SharedOwner<T>
    {
        private static readonly object Dependency;
        static SharedOwner() => Dependency = Lookup("generic-" + typeof(T).Name);
        [MethodImpl(MethodImplOptions.NoInlining)] public static int Add(int value)
        {
            var name = "generic-" + typeof(T).Name;
            if (Initializations.GetValueOrDefault(name) != 1) throw new Exception("Shared body entered before its actual owner's initializer.");
            return CheckValue(name, Dependency, value);
        }
    }
    private static class ThreadFieldOwner
    {
        [ThreadStatic] private static int _value = Initialize();
        private static int Initialize() { _ = Lookup("thread-static"); return 48; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static int Add(int value) => _value + value;
    }
    private static class DynamicAssociationOwner
    {
        private static readonly object Dependency;
        static DynamicAssociationOwner() => Dependency = Lookup("dynamic-owner-only");
        [MethodImpl(MethodImplOptions.NoInlining)] internal static object ReadDependency() => Dependency;
    }
    private static class PatchedStaticOwner
    {
        private static readonly object Dependency;
        static PatchedStaticOwner() => Dependency = Lookup("harmony-static-first-call");
        [MethodImpl(MethodImplOptions.NoInlining)] public static int Add(int value) => CheckValue("harmony-static-first-call", Dependency, value);
    }
    private static class PatchedStaticFailure
    {
        static PatchedStaticFailure() { _patchedFailureInitializations++; throw new InvalidOperationException("patched-entry-failure"); }
        [MethodImpl(MethodImplOptions.NoInlining)] public static int Add(int value) => value;
    }
    private static class WindowsNativeOwner
    {
        static WindowsNativeOwner() => _ = Lookup("invoke-native");
        [DllImport("ucrtbase", EntryPoint = "abs", ExactSpelling = true)] public static extern int Add(int value);
    }
    private static class AndroidNativeOwner
    {
        static AndroidNativeOwner() => _ = Lookup("invoke-native");
        [DllImport("libc", EntryPoint = "abs", ExactSpelling = true)] public static extern int Add(int value);
    }

    internal static void WriteModuleFixture(string directory)
    {
        using var assembly = Mono.Cecil.AssemblyDefinition.CreateAssembly(
            new Mono.Cecil.AssemblyNameDefinition("AndroidModuleFixture", new Version(1, 0)), "AndroidModuleFixture", Mono.Cecil.ModuleKind.Dll);
        var module = assembly.MainModule;
        var initializer = new Mono.Cecil.MethodDefinition(".cctor",
            Mono.Cecil.MethodAttributes.Private | Mono.Cecil.MethodAttributes.Static | Mono.Cecil.MethodAttributes.SpecialName | Mono.Cecil.MethodAttributes.RTSpecialName,
            module.TypeSystem.Void);
        module.Types[0].Methods.Add(initializer);
        var il = initializer.Body.GetILProcessor();
        il.Emit(Mono.Cecil.Cil.OpCodes.Call, module.ImportReference(typeof(ModuleInitializationObserver).GetMethod(nameof(ModuleInitializationObserver.Initialize))!));
        il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
        var target = new Mono.Cecil.TypeDefinition("RuntimeModuleFixture", "Target",
            Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Abstract | Mono.Cecil.TypeAttributes.Sealed | Mono.Cecil.TypeAttributes.BeforeFieldInit,
            module.TypeSystem.Object);
        module.Types.Add(target);
        var add = new Mono.Cecil.MethodDefinition("Add", Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static, module.TypeSystem.Int32)
            { ImplAttributes = Mono.Cecil.MethodImplAttributes.NoInlining };
        add.Parameters.Add(new Mono.Cecil.ParameterDefinition(module.TypeSystem.Int32));
        target.Methods.Add(add);
        il = add.Body.GetILProcessor();
        il.Emit(Mono.Cecil.Cil.OpCodes.Call, module.ImportReference(typeof(ModuleInitializationObserver).GetMethod(nameof(ModuleInitializationObserver.EnterBody))!));
        il.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
        il.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4, 48);
        il.Emit(Mono.Cecil.Cil.OpCodes.Add);
        il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
        assembly.Write(Path.Combine(directory, "AndroidModuleFixture.dll"));
    }
}

public static class ModuleInitializationObserver
{
    public static int Count;
    public static void Initialize() => Count++;
    public static void EnterBody()
    {
        if (Count != 1) throw new Exception("The method body entered before its module initializer.");
    }
}
