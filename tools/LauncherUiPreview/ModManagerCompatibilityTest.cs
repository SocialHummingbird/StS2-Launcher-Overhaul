using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Modding;
using STS2Mobile.Patches;

namespace LauncherUiPreview;

// One structural regression contract for the two game assemblies the launcher
// supports: the original synchronous ModManager and the current async manager.
internal static class ModManagerCompatibilityTest
{
    private const string CurrentAssemblyTestPathEnvironmentVariable =
        "STS2_MOD_MANAGER_CURRENT_ASSEMBLY_TEST_PATH";
    private const string CurrentSemanticVersionTypeName =
        "MegaCrit.Sts2.Core.Debug.SemanticVersion";

    internal static void Run()
    {
        var legacyType = BuildLegacyShape();
        var legacyTarget = ModLoaderPatches.FindInitializeTarget(legacyType);
        Require(
            legacyTarget != null
                && legacyTarget.ReturnType == typeof(void)
                && legacyTarget.GetParameters().Length == 2,
            "The legacy void ModManager.Initialize shape was not selected."
        );
        Require(
            ModLoaderPatches.TryResolveRuntimeLoadState(
                legacyType,
                out var initializedField,
                out var legacyStateProperty
            )
                && initializedField?.Name == "_initialized"
                && initializedField.FieldType == typeof(bool)
                && legacyStateProperty == null,
            "The legacy ModManager._initialized load gate was not resolved."
        );
        AssertBundledLegacyAssembly();

        var currentType = BuildCurrentShape();
        var currentTarget = ModLoaderPatches.FindInitializeTarget(currentType);
        var currentParameters = currentTarget?.GetParameters();
        Require(
            currentTarget != null
                && currentTarget.ReturnType == typeof(Task)
                && currentParameters?.Length == 3
                && currentParameters[2].ParameterType.FullName
                    == CurrentSemanticVersionTypeName,
            "The current Task ModManager.Initialize(..., SemanticVersion) shape was not selected."
        );
        Require(
            ModLoaderPatches.TryResolveRuntimeLoadState(
                currentType,
                out var currentInitializedField,
                out var stateProperty
            )
                && currentInitializedField == null
                && stateProperty?.Name == "State"
                && stateProperty.PropertyType.IsEnum
                && stateProperty.SetMethod?.IsStatic == true
                && Enum.GetName(stateProperty.PropertyType, 0) == "None",
            "The current ModManager.State=None load gate was not resolved."
        );

        AssertAsyncInitializeSequencing();
        AssertCurrentAssemblyCollection();
        AssertBaseLibAndroidSafePatchSet();
        AssertActualCurrentAssemblyIfRequested();

        GD.Print(
            "MOD_MANAGER_COMPATIBILITY=legacy-void,current-task-state,"
                + "async-after-completion,current-assemblies"
        );
    }

    private static void AssertBundledLegacyAssembly()
    {
        var target = ModLoaderPatches.FindInitializeTarget(typeof(ModManager));
        Require(
            target != null
                && target.ReturnType == typeof(void)
                && target.GetParameters().Length == 2,
            "The bundled legacy game assembly did not resolve to its exact Initialize target."
        );
        Require(
            ModLoaderPatches.TryResolveRuntimeLoadState(
                typeof(ModManager),
                out var initializedField,
                out var stateProperty
            )
                && initializedField?.Name == "_initialized"
                && initializedField.FieldType == typeof(bool)
                && stateProperty == null,
            "The bundled legacy game assembly did not resolve to _initialized only."
        );
    }

    private static void AssertAsyncInitializeSequencing()
    {
        var original = new TaskCompletionSource();
        var callbackCount = 0;
        var wrapped = ModLoaderPatches.AwaitInitializeThenRun(
            original.Task,
            () => callbackCount++
        );
        Require(
            !wrapped.IsCompleted && callbackCount == 0,
            "The Android mod loader callback ran before current ModManager.Initialize completed."
        );

        original.SetResult();
        wrapped.GetAwaiter().GetResult();
        Require(
            callbackCount == 1,
            "The Android mod loader callback did not run exactly once after Initialize completed."
        );

        var failedCallbackCount = 0;
        var expectedFailure = new InvalidOperationException("current initialize failed");
        var failed = ModLoaderPatches.AwaitInitializeThenRun(
            Task.FromException(expectedFailure),
            () => failedCallbackCount++
        );
        try
        {
            failed.GetAwaiter().GetResult();
            throw new InvalidOperationException(
                "A failed current ModManager.Initialize Task was swallowed."
            );
        }
        catch (InvalidOperationException ex) when (ReferenceEquals(ex, expectedFailure))
        {
        }
        Require(
            failedCallbackCount == 0,
            "The Android mod loader callback ran after current ModManager.Initialize failed."
        );
    }

    private static void AssertCurrentAssemblyCollection()
    {
        var expected = typeof(ModManagerCompatibilityTest).Assembly;
        var currentMod = new CurrentModShape();
        currentMod.assemblies.Add(expected);
        Require(
            ReferenceEquals(
                ModLoaderPatches.TryReadRuntimeAssembly(currentMod),
                expected
            ),
            "Activation evidence did not read the current Mod.assemblies collection."
        );
    }

    private static void AssertBaseLibAndroidSafePatchSet()
    {
        var patchTypes = ModLoaderPatches.BaseLibAndroidSafeHarmonyPatchTypes;
        Require(
            patchTypes.Count == 1
                && string.Equals(
                    patchTypes[0],
                    "BaseLib.Abstracts.CustomBadgesPatch",
                    StringComparison.Ordinal
            ),
            "The Android-safe BaseLib initializer must retain only CustomBadgesPatch; "
                + "TheBigPatchToCardPileCmdAdd hangs on the current Android runtime."
        );
    }

    private static void AssertActualCurrentAssemblyIfRequested()
    {
        var assemblyPath = System.Environment.GetEnvironmentVariable(
            CurrentAssemblyTestPathEnvironmentVariable
        );
        if (string.IsNullOrWhiteSpace(assemblyPath))
            return;

        assemblyPath = Path.GetFullPath(assemblyPath);
        Require(
            File.Exists(assemblyPath),
            $"The requested current game assembly was not found: {assemblyPath}"
        );

        var context = new CurrentGameAssemblyLoadContext(
            Path.GetDirectoryName(assemblyPath)!
        );
        try
        {
            var assembly = context.LoadFromAssemblyPath(assemblyPath);
            var managerType = assembly.GetType(
                "MegaCrit.Sts2.Core.Modding.ModManager",
                throwOnError: true
            )!;
            var target = ModLoaderPatches.FindInitializeTarget(managerType);
            Require(
                target != null
                    && target.ReturnType == typeof(Task)
                    && target.GetParameters().Length == 3
                    && target.GetParameters()[2].ParameterType.FullName
                        == CurrentSemanticVersionTypeName,
                "The actual current game assembly did not resolve to the supported async Initialize target."
            );
            Require(
                ModLoaderPatches.TryResolveRuntimeLoadState(
                    managerType,
                    out var initializedField,
                    out var stateProperty
                )
                    && initializedField == null
                    && stateProperty?.Name == "State"
                    && Enum.GetName(stateProperty.PropertyType, 0) == "None",
                "The actual current game assembly did not resolve to the State=None load gate."
            );

            var modType = assembly.GetType(
                "MegaCrit.Sts2.Core.Modding.Mod",
                throwOnError: true
            )!;
            var assembliesMember = modType.GetField(
                "assemblies",
                BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.Instance
            );
            Require(
                assembliesMember != null
                    && typeof(IList).IsAssignableFrom(assembliesMember.FieldType),
                "The actual current game Mod type did not expose its assemblies collection."
            );

            var currentMod = Activator.CreateInstance(modType, nonPublic: true)!;
            var values = assembliesMember.GetValue(currentMod) as IList;
            if (values == null)
            {
                values = (IList)Activator.CreateInstance(assembliesMember.FieldType)!;
                assembliesMember.SetValue(currentMod, values);
            }
            var expected = typeof(ModManagerCompatibilityTest).Assembly;
            values.Add(expected);
            Require(
                ReferenceEquals(
                    ModLoaderPatches.TryReadRuntimeAssembly(currentMod),
                    expected
                ),
                "Activation evidence did not read assemblies from the actual current Mod shape."
            );

            GD.Print("MOD_MANAGER_CURRENT_ASSEMBLY=Task,State,assemblies");
        }
        finally
        {
            context.Unload();
        }
    }

    private static Type BuildLegacyShape()
    {
        var module = CreateModule("Legacy");
        var manager = module.DefineType(
            "CompatibilityFixture.LegacyModManager",
            TypeAttributes.Abstract | TypeAttributes.Sealed
        );
        manager.DefineField(
            "_initialized",
            typeof(bool),
            FieldAttributes.Private | FieldAttributes.Static
        );
        EmitReturn(
            manager.DefineMethod(
                "Initialize",
                MethodAttributes.Public | MethodAttributes.Static,
                typeof(void),
                new[] { typeof(IModManagerFileIo), typeof(ModSettings) }
            )
        );
        EmitReturn(
            manager.DefineMethod(
                "Initialize",
                MethodAttributes.Private | MethodAttributes.Static,
                typeof(void),
                Type.EmptyTypes
            )
        );
        return manager.CreateType()!;
    }

    private static Type BuildCurrentShape()
    {
        var module = CreateModule("Current");
        var semanticVersion = module
            .DefineType(
                CurrentSemanticVersionTypeName,
                TypeAttributes.Public
                    | TypeAttributes.Sealed
                    | TypeAttributes.SequentialLayout,
                typeof(ValueType)
            )
            .CreateType()!;
        var stateType = module
            .DefineEnum(
                "CompatibilityFixture.ModManagerState",
                TypeAttributes.Public,
                typeof(int)
            );
        stateType.DefineLiteral("None", 0);
        stateType.DefineLiteral("Initialized", 1);
        stateType.DefineLiteral("Skipped", 2);
        var createdStateType = stateType.CreateType()!;

        var manager = module.DefineType(
            "CompatibilityFixture.CurrentModManager",
            TypeAttributes.Abstract | TypeAttributes.Sealed
        );
        var stateField = manager.DefineField(
            "<State>k__BackingField",
            createdStateType,
            FieldAttributes.Private | FieldAttributes.Static
        );
        var stateProperty = manager.DefineProperty(
            "State",
            PropertyAttributes.None,
            createdStateType,
            Type.EmptyTypes
        );
        var stateGetter = manager.DefineMethod(
            "get_State",
            MethodAttributes.Public
                | MethodAttributes.Static
                | MethodAttributes.SpecialName
                | MethodAttributes.HideBySig,
            createdStateType,
            Type.EmptyTypes
        );
        var getterIl = stateGetter.GetILGenerator();
        getterIl.Emit(OpCodes.Ldsfld, stateField);
        getterIl.Emit(OpCodes.Ret);
        var stateSetter = manager.DefineMethod(
            "set_State",
            MethodAttributes.Private
                | MethodAttributes.Static
                | MethodAttributes.SpecialName
                | MethodAttributes.HideBySig,
            typeof(void),
            new[] { createdStateType }
        );
        var setterIl = stateSetter.GetILGenerator();
        setterIl.Emit(OpCodes.Ldarg_0);
        setterIl.Emit(OpCodes.Stsfld, stateField);
        setterIl.Emit(OpCodes.Ret);
        stateProperty.SetGetMethod(stateGetter);
        stateProperty.SetSetMethod(stateSetter);

        var initialize = manager.DefineMethod(
            "Initialize",
            MethodAttributes.Public | MethodAttributes.Static,
            typeof(Task),
            new[] { typeof(IModManagerFileIo), typeof(ModSettings), semanticVersion }
        );
        var initializeIl = initialize.GetILGenerator();
        initializeIl.Emit(
            OpCodes.Call,
            typeof(Task).GetProperty(nameof(Task.CompletedTask))!.GetMethod!
        );
        initializeIl.Emit(OpCodes.Ret);
        EmitReturn(
            manager.DefineMethod(
                "Initialize",
                MethodAttributes.Private | MethodAttributes.Static,
                typeof(void),
                new[] { typeof(string) }
            )
        );
        return manager.CreateType()!;
    }

    private static ModuleBuilder CreateModule(string shape)
    {
        var name = new AssemblyName(
            $"LauncherModManagerCompatibility.{shape}.{Guid.NewGuid():N}"
        );
        return AssemblyBuilder
            .DefineDynamicAssembly(name, AssemblyBuilderAccess.Run)
            .DefineDynamicModule(name.Name!);
    }

    private static void EmitReturn(MethodBuilder method)
        => method.GetILGenerator().Emit(OpCodes.Ret);

    private static void Require(bool condition, string failure)
    {
        if (!condition)
            throw new InvalidOperationException(failure);
    }

    private sealed class CurrentModShape
    {
#pragma warning disable IDE1006
        internal readonly List<Assembly> assemblies = new();
#pragma warning restore IDE1006
    }

    private sealed class CurrentGameAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly string _directory;

        internal CurrentGameAssemblyLoadContext(string directory)
            : base(isCollectible: true)
        {
            _directory = directory;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            var candidate = Path.Combine(_directory, assemblyName.Name + ".dll");
            return File.Exists(candidate) ? LoadFromAssemblyPath(candidate) : null;
        }
    }
}
