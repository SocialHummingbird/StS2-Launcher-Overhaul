using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HarmonyLib;

namespace STS2Mobile.GameIdentityTests;

internal static class AndroidModRuntimeContract
{
    [UnmanagedCallersOnly]
    private static int AndroidModRuntimeProbe()
    {
        try
        {
            ModRuntimeLog("ANDROID_MOD_RUNTIME_BEGIN");
            True(CrossAssemblyAccessContract.Run(ModRuntimeLog, "/data/local/tmp/sts2-mono-transition-test"),
                "Ordinary assemblies must honor their named IgnoresAccessChecksTo scope without changing visibility.");
            True(ConstructionTimingContract.Run(ModRuntimeLog),
                "Beforefieldinit constructors must defer optional static initialization to field access.");
            var harmony = new Harmony("sts2-launcher.runtime-contract");
            var preparationPassed = MethodPreparationTimingContract.Run(harmony, ModRuntimeLog);
            var target = typeof(ModRuntimeTarget).GetMethod(nameof(ModRuntimeTarget.Value))!;
            var instance = new ModRuntimeTarget();
            CheckPrivateVisibility();
            Equal(13, instance.Value(2), "Fixture must exercise its original locals.");
            ModRuntimeLog("ANDROID_MOD_RUNTIME_PATCH_BEGIN");
            harmony.Patch(target,
                prefix: new HarmonyMethod(typeof(AndroidModRuntimeContract), nameof(ModRuntimePrefix)),
                postfix: new HarmonyMethod(typeof(AndroidModRuntimeContract), nameof(ModRuntimePostfix)),
                transpiler: new HarmonyMethod(typeof(AndroidModRuntimeContract), nameof(ModRuntimeTranspiler)),
                finalizer: new HarmonyMethod(typeof(AndroidModRuntimeContract), nameof(ModRuntimeFinalizer)));
            Equal(20, instance.Value(2), "Private access and float/nullable locals must retain their meaning.");
            Equal(4, instance.Value(-1), "Nullable's empty branch must survive IL copying.");
            try { instance.Value(99); throw new Exception("Original exception was swallowed."); }
            catch (InvalidOperationException ex) { Equal("runtime-contract", ex.Message, "Original exception must survive finalizers."); }
            Equal(4, instance.FinallyCount, "Original finally must run once per invocation.");
            True(_modRuntimeSawException, "Finalizer must observe the original failure.");
            var reverseReplacement = InstallReversePatch(harmony, target);
            var reversePassed = true;
            try { Equal(13, ModRuntimeReverse(instance, 2), "Reverse patch must call the unpatched body."); }
            catch (Exception ex) { reversePassed = false; ModRuntimeLog("REVERSE_DIAGNOSTIC cached-caller-error " + ex); }
            reversePassed &= CheckReverseCalls(instance, reverseReplacement);
            True(reversePassed, "Every reverse invocation path must call the unpatched body.");
            var reverseOwnerPassed = CheckReverseOwnerContext(harmony);
            harmony.Patch(typeof(ModRuntimeTarget).GetMethod(nameof(ModRuntimeTarget.OwnerType)),
                postfix: new HarmonyMethod(typeof(AndroidModRuntimeContract), nameof(ModRuntimeOwnerPostfix)));
            True(ModRuntimeTarget.OwnerType() == typeof(ModRuntimeTarget), "Type.GetType must retain original assembly context.");
            harmony.Patch(typeof(ModRuntimeTarget).GetMethod(nameof(ModRuntimeTarget.StaticValue)),
                prefix: new HarmonyMethod(typeof(AndroidModRuntimeContract), nameof(ModRuntimeStaticPrefix)));
            Equal(7, ModRuntimeTarget.StaticValue(), "Static by-reference __instance requires a typed null slot.");
            foreach (var name in new[] { nameof(ModRuntimeTarget.StringBranch), nameof(ModRuntimeTarget.EnumBranch) })
            {
                var branchTarget = typeof(ModRuntimeTarget).GetMethod(name)!;
                var prefix = typeof(AndroidModRuntimeContract).GetMethod(nameof(GenericBranchPrefix), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(branchTarget.ReturnType);
                harmony.Patch(branchTarget, prefix: new HarmonyMethod(prefix));
            }
            Equal("public-beta", ModRuntimeTarget.StringBranch(), "A closed generic string-result prefix must run.");
            True(ModRuntimeTarget.EnumBranch() == FixtureBranch.PublicBeta, "A closed generic enum-result prefix must run.");
            CheckLocalsAndFaultRegions(harmony);
            CheckModelConstruction(harmony);
            CheckPrivateVisibility();
            harmony.UnpatchAll(harmony.Id);
            Equal(13, instance.Value(2), "Unpatch must restore the original body.");
            True(preparationPassed, "Preparing or patching a method must not poison its optional static initializer.");
            True(reverseOwnerPassed, "Reverse patches must preserve the copied body's assembly context.");
            ModRuntimeLog("ANDROID_MOD_RUNTIME_PASS private-access private-visibility assembly-access-attribute locals raw-locals shorthand-locals nullable float branches exceptions fault prefix generic-prefix postfix transpiler finalizer reverse reverse-owner owner-context static-instance constructors constructor-failure method-preparation precise-invocation thread-static module-initializer unpatch");
            return 1;
        }
        catch (Exception ex) { ModRuntimeLog("ANDROID_MOD_RUNTIME_FAIL " + ex); return 0; }
    }

    private static void ModRuntimeLog(string text)
        => File.AppendAllText("/data/local/tmp/sts2-mono-transition-test/mod-runtime-result.log", text + "\n");
    private static void Equal(int expected, int actual, string message) => True(expected == actual, $"{message} Expected {expected}, actual {actual}.");
    private static void Equal(string expected, string actual, string message) => True(expected == actual, message);
    private static void True(bool condition, string message) { if (!condition) throw new Exception(message); }

    private static bool _modRuntimeSawException;
    private static int _faultCount;
    private static int _constructorPrefixes;
    private static int _constructorPostfixes;

    private static bool CheckReverseOwnerContext(Harmony harmony)
    {
        var ownerAssembly = Assembly.LoadFrom("/data/local/tmp/sts2-mono-transition-test/AndroidOwnerFixture.dll");
        var ownerType = ownerAssembly.GetType("RuntimeOwnerFixture.Target", throwOnError: true)!;
        var target = ownerType.GetMethod("OwnerType")!;
        True(ReferenceEquals(ownerType, target.Invoke(null, null)), "Cross-assembly source must resolve its original owner.");
        var replacement = harmony.CreateReversePatcher(target,
            new HarmonyMethod(typeof(AndroidModRuntimeContract), nameof(ModRuntimeReverseOwner))).Patch();
        var actual = ModRuntimeReverseOwner();
        ModRuntimeLog("REVERSE_OWNER_DIAGNOSTIC replacement=" + replacement.GetType().FullName
            + " owner=" + replacement.DeclaringType?.AssemblyQualifiedName + " module=" + replacement.Module.Assembly.FullName
            + " direct=" + ((Type?)replacement.Invoke(null, null))?.AssemblyQualifiedName);
        ModRuntimeLog("REVERSE_OWNER_CONTEXT expected=" + ownerType.AssemblyQualifiedName + " actual=" + actual?.AssemblyQualifiedName);
        return ReferenceEquals(ownerType, actual);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type? ModRuntimeReverseOwner() => throw new NotSupportedException();

    internal static void WriteOwnerFixture(string directory)
    {
        using var assembly = Mono.Cecil.AssemblyDefinition.CreateAssembly(
            new Mono.Cecil.AssemblyNameDefinition("AndroidOwnerFixture", new Version(1, 0)), "AndroidOwnerFixture", Mono.Cecil.ModuleKind.Dll);
        var module = assembly.MainModule;
        var target = new Mono.Cecil.TypeDefinition("RuntimeOwnerFixture", "Target",
            Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Abstract | Mono.Cecil.TypeAttributes.Sealed,
            module.TypeSystem.Object);
        module.Types.Add(target);
        var method = new Mono.Cecil.MethodDefinition("OwnerType",
            Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static,
            module.ImportReference(typeof(Type))) { ImplAttributes = Mono.Cecil.MethodImplAttributes.NoInlining };
        target.Methods.Add(method);
        var il = method.Body.GetILProcessor();
        il.Emit(Mono.Cecil.Cil.OpCodes.Ldstr, target.FullName);
        il.Emit(Mono.Cecil.Cil.OpCodes.Call, module.ImportReference(typeof(Type).GetMethod(nameof(Type.GetType), new[] { typeof(string) })!));
        il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
        assembly.Write(Path.Combine(directory, "AndroidOwnerFixture.dll"));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<MethodInfo> InstallReversePatch(Harmony harmony, MethodInfo target)
    {
        return new(harmony.CreateReversePatcher(target,
            new HarmonyMethod(typeof(AndroidModRuntimeContract), nameof(ModRuntimeReverse))).Patch());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CheckReverseCalls(ModRuntimeTarget instance, WeakReference<MethodInfo> replacementReference)
    {
        var standin = typeof(AndroidModRuntimeContract).GetMethod(nameof(ModRuntimeReverse), BindingFlags.Static | BindingFlags.NonPublic)!;
        var passed = true;
        void Observe(string name, Func<int> invoke)
        {
            try
            {
                Equal(13, invoke(), "Reverse " + name + " must call the original body.");
                ModRuntimeLog("REVERSE_DIAGNOSTIC " + name + " pass");
            }
            catch (Exception ex) { passed = false; ModRuntimeLog("REVERSE_DIAGNOSTIC " + name + " error " + ex); }
        }
        Observe("late-typed-caller", () => ModRuntimeReverse(instance, 2));
        Observe("reflection", () => (int)standin.Invoke(null, new object[] { instance, 2 })!);
        var invokeDelegate = (Func<ModRuntimeTarget, int, int>)standin.CreateDelegate(typeof(Func<ModRuntimeTarget, int, int>));
        Observe("delegate", () => invokeDelegate(instance, 2));
        GC.Collect();
        GC.WaitForPendingFinalizers();
        if (!replacementReference.TryGetTarget(out var replacement))
        {
            ModRuntimeLog("REVERSE_DIAGNOSTIC replacement-collected");
            return false;
        }
        Observe("replacement", () => (int)replacement.Invoke(null, new object[] { instance, 2 })!);
        Observe("delegate-after-gc", () => invokeDelegate(instance, 2));
        return passed;
    }

    private static void CheckPrivateVisibility()
    {
        True(typeof(ModRuntimeTarget).IsNestedPrivate, "Patching must preserve private type visibility.");
        True(typeof(ModRuntimeTarget).GetField("secret", BindingFlags.Instance | BindingFlags.Public) is null,
            "Private fields must remain absent from public reflection.");
        True(typeof(ModRuntimeTarget).GetField("secret", BindingFlags.Instance | BindingFlags.NonPublic)!.IsPrivate,
            "Private fields must retain their metadata visibility.");
    }

    private static void CheckLocalsAndFaultRegions(Harmony harmony)
    {
        True(ModRuntimeTarget.ShortLocal() == 4.25f, "Fixture must contain shorthand float local IL.");
        True(ModRuntimeTarget.RawLocal() == 4.0, "Fixture must contain short/long double local IL.");
        try { ModRuntimeTarget.FaultRegion(true); throw new Exception("Original fault fixture did not throw."); }
        catch (InvalidOperationException ex) { Equal("fault-contract", ex.Message, "Original fault fixture must throw its real exception."); }
        Equal(1, _faultCount, "Unpatched fault fixture must run its handler.");
        _faultCount = 0;
        foreach (var name in new[] { nameof(ModRuntimeTarget.ShortLocal), nameof(ModRuntimeTarget.RawLocal), nameof(ModRuntimeTarget.FaultRegion) })
            harmony.Patch(typeof(ModRuntimeTarget).GetMethod(name),
                transpiler: new HarmonyMethod(typeof(AndroidModRuntimeContract),
                    name == nameof(ModRuntimeTarget.RawLocal) ? nameof(RawLocalTranspiler) : nameof(ModRuntimeTranspiler)));
        True(ModRuntimeTarget.ShortLocal() == 4.25f, "Shorthand float locals must survive copying.");
        True(ModRuntimeTarget.RawLocal() == 4.0, "Numeric short/long double local operands must retain local types.");
        ModRuntimeTarget.FaultRegion(false);
        Equal(0, _faultCount, "Fault handlers must not run on success.");
        try { ModRuntimeTarget.FaultRegion(true); throw new Exception("Fault fixture did not throw."); }
        catch (InvalidOperationException ex) { Equal("fault-contract", ex.Message, "Fault regions must preserve the failure."); }
        Equal(1, _faultCount, "Fault handlers must run once on failure.");
    }

    private static IEnumerable<CodeInstruction> RawLocalTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            // Mods also emit numeric operands instead of LocalBuilder objects.
            if (instruction.opcode == OpCodes.Stloc_S || instruction.opcode == OpCodes.Ldloc_S)
                instruction.operand = (byte)0;
            if (instruction.opcode == OpCodes.Stloc || instruction.opcode == OpCodes.Ldloc)
                instruction.operand = (short)1;
            yield return instruction;
        }
    }

    private static void IncrementFault() => _faultCount++;

    private static void CheckModelConstruction(Harmony harmony)
    {
        var constructor = typeof(ConstructedModel).GetConstructor(new[] { typeof(int) })!;
        harmony.Patch(constructor,
            prefix: new HarmonyMethod(typeof(AndroidModRuntimeContract), nameof(ConstructorPrefix)),
            postfix: new HarmonyMethod(typeof(AndroidModRuntimeContract), nameof(ConstructorPostfix)),
            transpiler: new HarmonyMethod(typeof(AndroidModRuntimeContract), nameof(ModRuntimeTranspiler)));
        var model = (ConstructedModel)Activator.CreateInstance(typeof(ConstructedModel), 41)!;
        Equal(41, model.Value, "Activator must run the constructor with its real arguments.");
        True(ReferenceEquals(model, ConstructedModel.Registry[41]), "Only a constructed model may be published.");
        try { Activator.CreateInstance(typeof(ConstructedModel), -1); throw new Exception("Constructor failure was swallowed."); }
        catch (TargetInvocationException ex)
        {
            True(ex.InnerException is InvalidOperationException, "Activator must retain the original constructor failure.");
            Equal("constructor-contract", ex.InnerException!.Message, "Constructor failure details must survive.");
        }
        True(!ConstructedModel.Registry.ContainsKey(-1), "A failing constructor must not publish a placeholder.");
        Equal(2, _constructorPrefixes, "Constructor prefixes must observe both attempts.");
        Equal(1, _constructorPostfixes, "Constructor postfixes must only observe completed construction.");
    }

    private static void ConstructorPrefix(int value) => _constructorPrefixes++;
    private static void ConstructorPostfix(ConstructedModel __instance)
    {
        True(__instance.Value >= 0, "Constructor postfix must see initialized private state.");
        _constructorPostfixes++;
    }

    private sealed class ConstructedModel
    {
        internal static readonly Dictionary<int, ConstructedModel> Registry = new();
        private readonly int _value;
        internal int Value => _value;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public ConstructedModel(int value)
        {
            if (value < 0) throw new InvalidOperationException("constructor-contract");
            _value = value;
            Registry.Add(value, this);
        }
    }
    private static void ModRuntimePrefix(ref int value, int ___secret, out int __state)
    {
        Equal(7, ___secret, "Private field injection must work without publicizing.");
        __state = value;
        value++;
    }
    private static void ModRuntimePostfix(ref int __result, int __state) => __result += 5;
    private static IEnumerable<CodeInstruction> ModRuntimeTranspiler(IEnumerable<CodeInstruction> instructions) => instructions;
    private static Exception? ModRuntimeFinalizer(Exception? __exception)
    {
        _modRuntimeSawException |= __exception is InvalidOperationException;
        return __exception;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ModRuntimeReverse(ModRuntimeTarget instance, int value) => throw new NotSupportedException();
    private static void ModRuntimeOwnerPostfix(ref Type? __result) { }
    private static void ModRuntimeStaticPrefix(ref ModRuntimeTarget? __instance)
        => True(__instance is null, "A static method has no instance.");
    private enum FixtureBranch { Public, PublicBeta }
    private static bool GenericBranchPrefix<T>(ref T __result)
    {
        __result = (T)(typeof(T) == typeof(string) ? (object)"public-beta" : FixtureBranch.PublicBeta);
        return false;
    }

    private sealed class ModRuntimeTarget
    {
        private int secret = 7;
        public int FinallyCount;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public int Value(int value = 2)
        {
            try
            {
                float scaled = (secret + value) * 1.5f;
                int? result = value == 0 ? null : (int)scaled;
                if (value >= 99) throw new InvalidOperationException("runtime-contract");
                return result ?? -1;
            }
            finally { FinallyCount++; }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Type? OwnerType() => Type.GetType("STS2Mobile.GameIdentityTests.AndroidModRuntimeContract+ModRuntimeTarget");
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int StaticValue() => 7;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string StringBranch() => "original";
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static FixtureBranch EnumBranch() => FixtureBranch.Public;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float ShortLocal() => throw new NotSupportedException("Android fixture IL was not generated.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static double RawLocal() => throw new NotSupportedException("Android fixture IL was not generated.");
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void FaultRegion(bool fail) => throw new NotSupportedException("Android fixture IL was not generated.");
    }

    // C# cannot express fault regions or require a specific local opcode. Write
    // those method bodies only into the isolated Android test assembly.
    internal static void WriteFixtureBodies(Mono.Cecil.ModuleDefinition module)
    {
        var contract = module.GetType(typeof(AndroidModRuntimeContract).FullName!);
        var target = contract.NestedTypes.Single(type => type.Name == nameof(ModRuntimeTarget));
        var shortLocal = target.Methods.Single(method => method.Name == nameof(ModRuntimeTarget.ShortLocal));
        shortLocal.Body = new Mono.Cecil.Cil.MethodBody(shortLocal) { InitLocals = true };
        shortLocal.Body.Variables.Add(new Mono.Cecil.Cil.VariableDefinition(module.TypeSystem.Single));
        var il = shortLocal.Body.GetILProcessor();
        il.Emit(Mono.Cecil.Cil.OpCodes.Ldc_R4, 4.25f);
        il.Emit(Mono.Cecil.Cil.OpCodes.Stloc_0);
        il.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_0);
        il.Emit(Mono.Cecil.Cil.OpCodes.Ret);
        var rawLocal = target.Methods.Single(method => method.Name == nameof(ModRuntimeTarget.RawLocal));
        rawLocal.Body = new Mono.Cecil.Cil.MethodBody(rawLocal) { InitLocals = true };
        var first = new Mono.Cecil.Cil.VariableDefinition(module.TypeSystem.Double);
        var second = new Mono.Cecil.Cil.VariableDefinition(module.TypeSystem.Double);
        rawLocal.Body.Variables.Add(first);
        rawLocal.Body.Variables.Add(second);
        il = rawLocal.Body.GetILProcessor();
        il.Emit(Mono.Cecil.Cil.OpCodes.Ldc_R8, 2.25);
        il.Emit(Mono.Cecil.Cil.OpCodes.Stloc_S, first);
        il.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_S, first);
        il.Emit(Mono.Cecil.Cil.OpCodes.Ldc_R8, 1.75);
        il.Emit(Mono.Cecil.Cil.OpCodes.Add);
        il.Emit(Mono.Cecil.Cil.OpCodes.Stloc, second);
        il.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, second);
        il.Emit(Mono.Cecil.Cil.OpCodes.Ret);

        var fault = target.Methods.Single(method => method.Name == nameof(ModRuntimeTarget.FaultRegion));
        var exceptionCtor = target.Methods.Single(method => method.Name == nameof(ModRuntimeTarget.Value)).Body.Instructions
            .Where(instruction => instruction.OpCode == Mono.Cecil.Cil.OpCodes.Newobj)
            .Select(instruction => (Mono.Cecil.MethodReference)instruction.Operand)
            .Single(method => method.DeclaringType.FullName == typeof(InvalidOperationException).FullName);
        fault.Body = new Mono.Cecil.Cil.MethodBody(fault);
        il = fault.Body.GetILProcessor();
        var start = il.Create(Mono.Cecil.Cil.OpCodes.Ldarg_0);
        var success = il.Create(Mono.Cecil.Cil.OpCodes.Nop);
        var handler = il.Create(Mono.Cecil.Cil.OpCodes.Call, contract.Methods.Single(method => method.Name == nameof(IncrementFault)));
        var end = il.Create(Mono.Cecil.Cil.OpCodes.Ret);
        il.Append(start);
        il.Emit(Mono.Cecil.Cil.OpCodes.Brfalse, success);
        il.Emit(Mono.Cecil.Cil.OpCodes.Ldstr, "fault-contract");
        il.Emit(Mono.Cecil.Cil.OpCodes.Newobj, exceptionCtor!);
        il.Emit(Mono.Cecil.Cil.OpCodes.Throw);
        il.Append(success);
        il.Emit(Mono.Cecil.Cil.OpCodes.Leave, end);
        il.Append(handler);
        il.Emit(Mono.Cecil.Cil.OpCodes.Endfinally);
        il.Append(end);
        fault.Body.ExceptionHandlers.Add(new Mono.Cecil.Cil.ExceptionHandler(Mono.Cecil.Cil.ExceptionHandlerType.Fault)
        {
            TryStart = start, TryEnd = handler, HandlerStart = handler, HandlerEnd = end,
        });
    }
}
