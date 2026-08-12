using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using STS2Mobile.Launcher;

namespace STS2Mobile.Patches;

// Extends ModManager to scan Android mod roots after the built-in game scan.
// Workshop sync stages into app-private storage; shared storage remains available
// for manual sideloading through /storage/emulated/0/StS2Launcher/Mods/.
internal static partial class ModLoaderPatches
{
    private static readonly BindingFlags AllStatic =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly BindingFlags AllInstance =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const string ModManagerFileIoTypeName =
        "MegaCrit.Sts2.Core.Modding.IModManagerFileIo";
    private const string ModSettingsTypeName = "MegaCrit.Sts2.Core.Modding.ModSettings";
    private const string SemanticVersionTypeName =
        "MegaCrit.Sts2.Core.Debug.SemanticVersion";
    private static Harmony _harmony;
    private static readonly HashSet<string> AppliedModCompatibilityPatches = new(StringComparer.Ordinal);
    internal static IReadOnlyList<string> BaseLibAndroidSafeHarmonyPatchTypes { get; } =
        new[] { "BaseLib.Abstracts.CustomBadgesPatch" };
    private static readonly object RegisteredGodotScriptAssembliesGate = new();
    private static readonly HashSet<string> RegisteredGodotScriptAssemblies = new(
        StringComparer.Ordinal
    );
    private static readonly HashSet<string> BaseLibAndroidSkippedPatchTypes = new(StringComparer.Ordinal)
    {
        "BaseLib.Patches.Fixes.CombatRoomFromSerializableRewardExtPatch",
        "BaseLib.Patches.Content.ActModelGenerateRoomsPatch",
        "BaseLib.Patches.Content.AddCustomAncientsToPool",
        "BaseLib.Commands.MultiPileCardSelect+SortCardsPatch",
        "BaseLib.Commands.MultiPileCardSelect+AddPileIndicatorNodePatch",
        "BaseLib.Commands.MultiPileCardSelect+RefreshPileIndicatorNodePatch",
        "BaseLib.Commands.MultiPileCardSelect+HideTipPatch",
    };

    internal static void Apply(Harmony harmony)
    {
        _harmony = harmony;
        PatchGodotScriptRegistration(harmony);
        PatchModAssemblyLoadHooks(harmony);
        PatchModManagerInitialize(harmony);
        if (!IsInitializePostfixInstalled())
        {
            PatchHelper.Log("FAILED ModManager.Initialize: loader postfix ownership verification failed");
            TryWriteUnavailableActivationMarker(
                "The ModManager.Initialize loader patch was not installed."
            );
        }
    }

    private static void PatchModManagerInitialize(Harmony harmony)
    {
        var target = FindInitializeTarget(typeof(ModManager));
        var postfix = GetInitializePostfix(target);
        if (target == null || postfix == null)
        {
            PatchHelper.Log(
                "FAILED ModManager.Initialize: no exact supported void/two-parameter or Task/three-parameter signature was found"
            );
            return;
        }

        try
        {
            harmony.Patch(target, postfix: new HarmonyMethod(postfix));
            PatchHelper.Log(
                $"Patched ModManager.Initialize ({(target.ReturnType == typeof(Task) ? "Task" : "void")})"
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"FAILED ModManager.Initialize: {ex.Message}");
        }
    }

    internal static MethodInfo FindInitializeTarget(Type modManagerType)
    {
        if (modManagerType == null)
            return null;

        MethodInfo[] supported;
        try
        {
            supported = modManagerType
                .GetMethods(AllStatic)
                .Where(IsSupportedInitializeTarget)
                .ToArray();
        }
        catch
        {
            return null;
        }

        return supported.Length == 1 ? supported[0] : null;
    }

    private static bool IsSupportedInitializeTarget(MethodInfo method)
    {
        if (method == null
            || !method.IsStatic
            || !string.Equals(method.Name, "Initialize", StringComparison.Ordinal))
        {
            return false;
        }

        var parameters = method.GetParameters();
        if (parameters.Length < 2
            || !string.Equals(
                parameters[0].ParameterType.FullName,
                ModManagerFileIoTypeName,
                StringComparison.Ordinal
            )
            || !string.Equals(
                parameters[1].ParameterType.FullName,
                ModSettingsTypeName,
                StringComparison.Ordinal
            ))
        {
            return false;
        }

        if (method.ReturnType == typeof(void))
            return parameters.Length == 2;

        return method.ReturnType == typeof(Task)
            && parameters.Length == 3
            && string.Equals(
                parameters[2].ParameterType.FullName,
                SemanticVersionTypeName,
                StringComparison.Ordinal
            );
    }

    private static MethodInfo GetInitializePostfix(MethodInfo target)
    {
        if (target?.ReturnType == typeof(void))
        {
            return PatchHelper.Method(
                typeof(ModLoaderPatches),
                nameof(InitializePostfix)
            );
        }

        if (target?.ReturnType == typeof(Task))
        {
            return PatchHelper.Method(
                typeof(ModLoaderPatches),
                nameof(InitializeTaskPostfix)
            );
        }

        return null;
    }

    private static void PatchGodotScriptRegistration(Harmony harmony)
    {
        PatchHelper.Patch(
            harmony,
            typeof(Godot.Bridge.ScriptManagerBridge),
            nameof(Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly),
            prefix: PatchHelper.Method(
                typeof(ModLoaderPatches),
                nameof(GodotScriptRegistrationPrefix)
            ),
            postfix: PatchHelper.Method(
                typeof(ModLoaderPatches),
                nameof(GodotScriptRegistrationPostfix)
            )
        );
    }

    private static bool GodotScriptRegistrationPrefix(Assembly __0)
    {
        if (__0 == null || !DeclaresGodotScripts(__0))
            return true;

        var assemblyKey = AssemblyIdentity(__0);
        lock (RegisteredGodotScriptAssembliesGate)
            return !RegisteredGodotScriptAssemblies.Contains(assemblyKey);
    }

    private static void GodotScriptRegistrationPostfix(Assembly __0)
    {
        if (__0 == null || !DeclaresGodotScripts(__0))
            return;

        lock (RegisteredGodotScriptAssembliesGate)
            RegisteredGodotScriptAssemblies.Add(AssemblyIdentity(__0));
    }

    internal static bool IsInitializePostfixInstalled()
    {
        try
        {
            var target = FindInitializeTarget(typeof(ModManager));
            var postfix = GetInitializePostfix(target);
            var owner = _harmony?.Id;
            if (target == null || postfix == null || string.IsNullOrWhiteSpace(owner))
                return false;

            var patches = Harmony.GetPatchInfo(target);
            return patches?.Postfixes.Any(patch =>
                string.Equals(patch.owner, owner, StringComparison.Ordinal)
                && patch.PatchMethod == postfix
            ) == true;
        }
        catch
        {
            return false;
        }
    }

    private static void InitializeTaskPostfix(ref Task __result)
    {
        if (__result == null)
        {
            PatchHelper.Log(
                "[Mods] ModManager.Initialize returned a null Task; Android mods were not loaded"
            );
            TryWriteUnavailableActivationMarker(
                "ModManager.Initialize returned a null Task; selected mods were not loaded."
            );
            return;
        }

        __result = AwaitInitializeThenLoad(__result);
    }

    private static Task AwaitInitializeThenLoad(Task initializeTask)
        => AwaitInitializeThenRun(initializeTask, InitializePostfix);

    internal static async Task AwaitInitializeThenRun(
        Task initializeTask,
        Action afterCompletion
    )
    {
        ArgumentNullException.ThrowIfNull(initializeTask);
        ArgumentNullException.ThrowIfNull(afterCompletion);

        await initializeTask;
        afterCompletion();
    }

    // Runs after the original Initialize() to pick up Android-only mod roots.
    private static void InitializePostfix()
    {
        LauncherModSelectionDocument selection = null;
        LauncherModLaunchPlan launchPlan = null;
        try
        {
            selection = LauncherModSelectionState.Load();
            var resolution = LauncherModLaunchPlan.Resolve(selection);
            if (!resolution.Success)
            {
                var failure = FormatLaunchPlanFailure(resolution.Error);
                PatchHelper.Log($"[Mods] {failure}");
                WriteModLaunchMarker(
                    LauncherModSelectionState.ModdedModeName,
                    selection,
                    planError: resolution.Error
                );
                return;
            }

            launchPlan = resolution.Plan;
            if (launchPlan.Mode == LauncherModPlayMode.Vanilla)
            {
                WriteModLaunchMarker(
                    LauncherModSelectionState.VanillaModeName,
                    selection,
                    launchPlan
                );
                PatchHelper.Log("[Mods] Android mod scan skipped: launcher Play Vanilla mode is selected");
                return;
            }

            if (!TryLoadModManagerAccess(out var access))
            {
                const string unavailable =
                    "Android mod manager reflection bridge was unavailable; selected mods were not loaded";
                WriteModLaunchMarker(
                    LauncherModSelectionState.ModdedModeName,
                    selection,
                    launchPlan,
                    BuildUnavailableActivationEvidence(launchPlan, unavailable)
                );
                return;
            }

            var loadedRoots = LoadAndroidModRoots(access, launchPlan);
            if (loadedRoots == 0)
            {
                var unavailableActivationEvidence = access.BuildActivationEvidence(launchPlan);
                WriteModLaunchMarker(
                    LauncherModSelectionState.ModdedModeName,
                    selection,
                    launchPlan,
                    unavailableActivationEvidence
                );
                PatchHelper.Log("[Mods] No validated Android mod payload was loaded");
                return;
            }

            var activationEvidence = access.BuildActivationEvidence(launchPlan);
            WriteModLaunchMarker(
                LauncherModSelectionState.ModdedModeName,
                selection,
                launchPlan,
                activationEvidence
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] Failed to load Android mods: {ex}");
            TryWriteUnavailableActivationMarker(
                $"Android mod load failed before activation evidence completed: {ex.GetType().Name}",
                selection,
                launchPlan
            );
        }
    }

    private static void TryWriteUnavailableActivationMarker(
        string status,
        LauncherModSelectionDocument selection = null,
        LauncherModLaunchPlan launchPlan = null
    )
    {
        try
        {
            selection ??= LauncherModSelectionState.Load();
            if (launchPlan == null)
            {
                var resolution = LauncherModLaunchPlan.Resolve(selection);
                if (!resolution.Success)
                {
                    WriteModLaunchMarker(
                        LauncherModSelectionState.ModdedModeName,
                        selection,
                        planError: resolution.Error
                    );
                    return;
                }

                launchPlan = resolution.Plan;
            }

            var playMode = launchPlan.Mode == LauncherModPlayMode.Modded
                ? LauncherModSelectionState.ModdedModeName
                : LauncherModSelectionState.VanillaModeName;
            WriteModLaunchMarker(
                playMode,
                selection,
                launchPlan,
                BuildUnavailableActivationEvidence(launchPlan, status)
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] Failed to write unavailable activation marker: {ex.Message}");
        }
    }

    private static void WriteModLaunchMarker(
        string launchMode,
        LauncherModSelectionDocument selection,
        LauncherModLaunchPlan launchPlan = null,
        RuntimeModActivationSummary[] activationEvidence = null,
        LauncherModDiscoveryError planError = null
    )
    {
        selection ??= LauncherModSelectionState.Load();
        if (planError != null)
        {
            LauncherModLaunchResultStore.WritePlanFailure(selection, planError);
            return;
        }

        if (launchPlan?.Mode == LauncherModPlayMode.Vanilla)
        {
            LauncherModLaunchResultStore.WriteVanilla(selection);
            return;
        }

        activationEvidence ??= Array.Empty<RuntimeModActivationSummary>();
        var results = BuildModLaunchResults(launchPlan, activationEvidence);
        LauncherModLaunchResultStore.Write(
            launchMode,
            selection,
            activationEvidence.Count(mod => mod.Discovered),
            activationEvidence.Count(mod => mod.PayloadLoadingSucceeded),
            results
        );
    }

    private static LauncherModLaunchResult[] BuildModLaunchResults(
        LauncherModLaunchPlan launchPlan,
        IReadOnlyList<RuntimeModActivationSummary> activationEvidence
    )
    {
        if (launchPlan?.Mode == LauncherModPlayMode.Modded)
        {
            return launchPlan.EnabledMods
                .Select(mod =>
                {
                    var evidence = (activationEvidence ?? Array.Empty<RuntimeModActivationSummary>())
                        .FirstOrDefault(item =>
                            string.Equals(item.Id, mod.ManifestId, StringComparison.Ordinal)
                            && string.Equals(
                                item.SelectionKey,
                                mod.SelectionKey,
                                StringComparison.OrdinalIgnoreCase
                            )
                        );
                    if (evidence?.ActivationSucceeded == true
                        && string.Equals(
                            evidence.CompatibilityMode,
                            "partial-android",
                            StringComparison.Ordinal
                        ))
                    {
                        return new LauncherModLaunchResult(
                            mod.ManifestId,
                            "Partial",
                            "Concrete activation was observed, but Android compatibility remains partial."
                        );
                    }

                    if (evidence?.ActivationSucceeded == true)
                    {
                        return new LauncherModLaunchResult(
                            mod.ManifestId,
                            "Active",
                            "Initialization and activation verified."
                        );
                    }

                    return new LauncherModLaunchResult(
                        mod.ManifestId,
                        "Failed",
                        ShortFailure(evidence?.FirstFailingGate)
                    );
                })
                .ToArray();
        }

        return Array.Empty<LauncherModLaunchResult>();
    }

    private static string ShortFailure(string gate)
        => gate switch
        {
            DiscoveryGate => "Discovery failed.",
            PayloadLoadingGate => "Payload loading failed.",
            InitializationGate => "Initialization failed.",
            ActivationGate => "Activation was not verified.",
            _ => "Runtime loading was not verified.",
        };

    private static string FormatLaunchPlanFailure(LauncherModDiscoveryError error)
    {
        if (error == null)
            return "Mod discovery failed without a specific error.";

        var selection = string.IsNullOrWhiteSpace(error.SelectionKey)
            ? "<unknown selection>"
            : error.SelectionKey;
        return $"Mod discovery failed [{error.Code}] for '{selection}': {error.Message}";
    }

    private static void PatchModAssemblyLoadHooks(Harmony harmony)
    {
        var postfix = PatchHelper.Method(
            typeof(ModLoaderPatches),
            nameof(ModAssemblyLoadPostfix)
        );

        PatchAssemblyLoadMethod(
            harmony,
            typeof(AssemblyLoadContext).GetMethod(
                nameof(AssemblyLoadContext.LoadFromAssemblyPath),
                AllInstance,
                binder: null,
                types: new[] { typeof(string) },
                modifiers: null
            ),
            "AssemblyLoadContext.LoadFromAssemblyPath(string)",
            postfix
        );
    }

    private static void PatchAssemblyLoadMethod(
        Harmony harmony,
        MethodInfo target,
        string label,
        MethodInfo postfix
    )
    {
        if (target == null)
        {
            PatchHelper.Log($"FAILED {label}: method not found");
            return;
        }

        try
        {
            harmony.Patch(target, postfix: new HarmonyMethod(postfix));
            PatchHelper.Log($"Patched {label}");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"FAILED {label}: {ex.Message}");
        }
    }

    private static void ModAssemblyLoadPostfix(Assembly __result)
    {
        TryRegisterGodotScripts(__result);
        TryApplyBaseLibJsonCompatibilityPatch(__result);
    }

    private static void TryRegisterGodotScripts(Assembly assembly)
    {
        if (assembly == null || !DeclaresGodotScripts(assembly))
            return;

        var assemblyKey = AssemblyIdentity(assembly);
        lock (RegisteredGodotScriptAssembliesGate)
        {
            if (RegisteredGodotScriptAssemblies.Contains(assemblyKey))
                return;
        }

        try
        {
            // ModManager loads the DLL before its PCK and invokes the initializer afterwards.
            // Registering compiled Godot script types at the assembly-load boundary lets PCK
            // scenes resolve their ScriptPath entries even when a mod's static initializer
            // preloads a scene before its Initialize method performs the same registration.
            Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(assembly);
            lock (RegisteredGodotScriptAssembliesGate)
                RegisteredGodotScriptAssemblies.Add(assemblyKey);
            PatchHelper.Log($"[Mods] Registered compiled Godot scripts for {assembly.GetName().Name}");
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"[Mods] Godot script registration failed for {assembly.GetName().Name}: {ex.GetType().Name}: {ex.Message}"
            );
        }
    }

    private static string AssemblyIdentity(Assembly assembly)
        => assembly.FullName ?? assembly.GetName().Name ?? string.Empty;

    private static bool DeclaresGodotScripts(Assembly assembly)
    {
        try
        {
            return assembly.GetCustomAttributesData().Any(attribute =>
                string.Equals(
                    attribute.AttributeType.FullName,
                    "Godot.AssemblyHasScriptsAttribute",
                    StringComparison.Ordinal
                )
            );
        }
        catch
        {
            return false;
        }
    }

    private static void ApplyLoadedAssemblyCompatibilityPatches()
    {
        TryApplyBaseLibJsonCompatibilityPatch();
    }

    private static void TryApplyBaseLibJsonCompatibilityPatch(Assembly baseLibAssembly = null)
    {
        if (_harmony == null)
            return;

        if (baseLibAssembly != null
            && !string.Equals(baseLibAssembly.GetName().Name, "BaseLib", StringComparison.Ordinal))
        {
            return;
        }

        baseLibAssembly ??= FindLoadedBaseLibAssembly();
        if (baseLibAssembly == null)
            return;

        var patchKey = baseLibAssembly.FullName;
        if (AppliedModCompatibilityPatches.Contains(patchKey))
            return;

        try
        {
            var savePatchUtils = baseLibAssembly.GetType("BaseLib.Utils.SavePatchUtils", throwOnError: false);
            var extendedSavePatches = baseLibAssembly.GetType(
                "BaseLib.Patches.Saves.ExtendedSavePatches",
                throwOnError: false
            );
            var harmonyExtensions = baseLibAssembly.GetType(
                "BaseLib.Extensions.HarmonyExtensions",
                throwOnError: false
            );
            var baseLibMain = baseLibAssembly.GetType(
                "BaseLib.BaseLibMain",
                throwOnError: false
            );

            var patched = 0;
            if (baseLibMain != null)
            {
                var initializeMethod = baseLibMain.GetMethod("Initialize", AllStatic);
                var prefix = PatchHelper.Method(
                    typeof(ModLoaderPatches),
                    nameof(BaseLibInitializePrefix)
                );
                if (initializeMethod != null && prefix != null)
                {
                    _harmony.Patch(initializeMethod, prefix: new HarmonyMethod(prefix));
                    patched++;
                }
            }

            if (harmonyExtensions != null)
            {
                var tryPatchAllMethod = harmonyExtensions.GetMethod("TryPatchAll", AllStatic);
                var prefix = PatchHelper.Method(
                    typeof(ModLoaderPatches),
                    nameof(BaseLibTryPatchAllPrefix)
                );
                if (tryPatchAllMethod != null && prefix != null)
                {
                    _harmony.Patch(tryPatchAllMethod, prefix: new HarmonyMethod(prefix));
                    patched++;
                }
            }

            if (extendedSavePatches != null)
            {
                var patchMethod = extendedSavePatches.GetMethod("Patch", AllStatic);
                var prefix = PatchHelper.Method(
                    typeof(ModLoaderPatches),
                    nameof(BaseLibExtendedSavePatchesPatchPrefix)
                );
                if (patchMethod != null && prefix != null)
                {
                    _harmony.Patch(patchMethod, prefix: new HarmonyMethod(prefix));
                    patched++;
                }
            }

            if (patched <= 0 && savePatchUtils == null)
            {
                PatchHelper.Log("[Mods] BaseLib compatibility patch skipped: expected save patch types not found");
                return;
            }

            if (patched <= 0)
            {
                PatchHelper.Log("[Mods] BaseLib compatibility patch skipped: no compatible entry points were patched");
                return;
            }

            AppliedModCompatibilityPatches.Add(patchKey);
            PatchHelper.Log(
                $"[Mods] BaseLib Android compatibility patch applied to {patched} save patch entry point(s)"
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] BaseLib Android compatibility patch failed: {ex}");
        }
    }

    private static Assembly FindLoadedBaseLibAssembly()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (string.Equals(assembly.GetName().Name, "BaseLib", StringComparison.Ordinal))
                return assembly;
        }

        return null;
    }

    private static bool BaseLibExtendedSavePatchesPatchPrefix()
    {
        PatchHelper.Log(
            "[Mods] BaseLib extended save patch registration skipped on Android; mod loading continues without BaseLib custom save extensions."
        );
        return false;
    }

    private static bool BaseLibInitializePrefix()
    {
        var baseLibAssembly = FindLoadedBaseLibAssembly();
        if (baseLibAssembly == null)
            return true;

        try
        {
            PatchHelper.Log(
                "[Mods] BaseLib Android-safe initializer active; skipping Linux libgcc dlopen."
            );
            RunBaseLibAndroidSafeInitialize(baseLibAssembly);
            PatchHelper.Log("[Mods] BaseLib Android-safe initializer complete");
            return false;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] BaseLib Android-safe initializer failed: {ex}");
            return true;
        }
    }

    private static void RunBaseLibAndroidSafeInitialize(Assembly baseLibAssembly)
    {
        var baseLibMain = baseLibAssembly.GetType("BaseLib.BaseLibMain", throwOnError: true);
        baseLibMain.GetField("IsMainThread", AllStatic)?.SetValue(null, true);

        TryInstallBaseLibLogListener(baseLibAssembly);
        TryInvokeStatic(baseLibAssembly, "BaseLib.Utils.NodeFactories.NodeFactory", "Init");
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(baseLibAssembly);
        TryRegisterBaseLibConfig(baseLibAssembly);

        var mainHarmony = ResolveBaseLibMainHarmony(baseLibMain);
        foreach (var patchType in BaseLibAndroidSafeHarmonyPatchTypes)
            TryInvokeBaseLibPatch(baseLibAssembly, patchType, mainHarmony);
        PatchHelper.Log(
            "[Mods] BaseLib Android-safe initializer skipped "
                + "BaseLib.Patches.Content.TheBigPatchToCardPileCmdAdd.Patch; "
                + "the BaseLib CustomPile compatibility patch hangs on the current Android runtime."
        );
        BaseLibExtendedSavePatchesPatchPrefix();
        PatchHelper.Log(
            "[Mods] BaseLib Android-safe initializer skipped BaseLib PatchAll; full type enumeration hangs on Android public-beta."
        );
        TryInvokeStatic(
            baseLibAssembly,
            "BaseLib.Utils.CustomLocTableManager",
            "Register",
            "card_modifiers"
        );
    }

    private static void TryInstallBaseLibLogListener(Assembly baseLibAssembly)
    {
        try
        {
            var logListenerType = baseLibAssembly.GetType(
                "BaseLib.Patches.Utils.LogListener",
                throwOnError: false
            );
            if (logListenerType == null)
                return;

            var listener = Activator.CreateInstance(logListenerType);
            typeof(OS).GetMethod(nameof(OS.AddLogger), new[] { listener.GetType() })
                ?.Invoke(null, new[] { listener });
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] BaseLib log listener skipped on Android: {ex.Message}");
        }
    }

    private static void TryRegisterBaseLibConfig(Assembly baseLibAssembly)
    {
        try
        {
            var configType = baseLibAssembly.GetType(
                "BaseLib.Config.BaseLibConfig",
                throwOnError: false
            );
            var registryType = baseLibAssembly.GetType(
                "BaseLib.Config.ModConfigRegistry",
                throwOnError: false
            );
            var registerMethod = registryType?.GetMethod("Register", AllStatic);
            if (configType == null || registerMethod == null)
                return;

            var config = Activator.CreateInstance(configType, nonPublic: true);
            registerMethod.Invoke(null, new[] { "BaseLib", config });
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] BaseLib config registration skipped on Android: {ex.Message}");
        }
    }

    private static Harmony ResolveBaseLibMainHarmony(Type baseLibMain)
    {
        if (baseLibMain.GetProperty("MainHarmony", AllStatic)?.GetValue(null) is Harmony harmony)
            return harmony;

        return new Harmony("BaseLib");
    }

    private static void TryInvokeBaseLibPatch(
        Assembly baseLibAssembly,
        string typeName,
        Harmony harmony
    )
    {
        TryInvokeStatic(baseLibAssembly, typeName, "Patch", harmony);
    }

    private static void TryInvokeStatic(
        Assembly assembly,
        string typeName,
        string methodName,
        params object[] arguments
    )
    {
        try
        {
            var type = assembly.GetType(typeName, throwOnError: false);
            var method = type?.GetMethod(methodName, AllStatic);
            method?.Invoke(null, arguments);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"[Mods] BaseLib Android-safe initializer skipped {typeName}.{methodName}: {ex.Message}"
            );
        }
    }

    private static bool BaseLibTryPatchAllPrefix(
        Harmony harmony,
        Assembly assembly,
        string category,
        ref bool __result
    )
    {
        if (assembly == null
            || !string.Equals(assembly.GetName().Name, "BaseLib", StringComparison.Ordinal))
        {
            return true;
        }

        try
        {
            var successCount = 0;
            var failCount = 0;
            var skippedCount = 0;

            PatchHelper.Log(
                "[Mods] BaseLib Android PatchAll compatibility filter active; known incompatible patch classes will be skipped."
            );

            foreach (var type in assembly.GetTypes())
            {
                if (!HasHarmonyPatchAttribute(type))
                    continue;

                if (BaseLibAndroidSkippedPatchTypes.Contains(type.FullName ?? string.Empty))
                {
                    skippedCount++;
                    PatchHelper.Log($"[Mods] BaseLib Android skipped patch class: {type.FullName}");
                    continue;
                }

                var processor = harmony.CreateClassProcessor(type);
                if (!CategoryMatches(category, processor.Category))
                    continue;

                try
                {
                    processor.Patch();
                    successCount++;
                }
                catch (Exception ex)
                {
                    failCount++;
                    PatchHelper.Log(
                        $"[Mods] BaseLib Android PatchAll failed for {type.FullName}: {ex.GetType().Name}: {ex.Message}"
                    );
                }
            }

            PatchHelper.Log(
                $"[Mods] BaseLib Android PatchAll complete. Applied={successCount} skipped={skippedCount} failed={failCount}"
            );
            __result = failCount == 0;
            return false;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] BaseLib Android PatchAll compatibility filter failed: {ex}");
            return true;
        }
    }

    private static bool CategoryMatches(string requestedCategory, string processorCategory)
    {
        if (!string.IsNullOrEmpty(requestedCategory))
            return string.Equals(requestedCategory, processorCategory, StringComparison.Ordinal);

        return string.IsNullOrEmpty(processorCategory);
    }

    private static bool HasHarmonyPatchAttribute(Type type)
    {
        if (type == null)
            return false;

        try
        {
            return type.GetCustomAttributesData().Any(attribute =>
                string.Equals(
                    attribute.AttributeType.Namespace,
                    "HarmonyLib",
                    StringComparison.Ordinal
                )
                && attribute.AttributeType.Name.StartsWith("Harmony", StringComparison.Ordinal)
            );
        }
        catch
        {
            // Diagnostics must not load unrelated attribute dependencies or turn a
            // successfully loaded mod into a marker-writing failure.
            return false;
        }
    }

    private static bool TryLoadModManagerAccess(out ModManagerAccess access)
    {
        var modManagerType = typeof(ModManager);
        access = null;

        var modsField = modManagerType.GetField("_mods", AllStatic);
        if (modsField == null)
        {
            PatchHelper.Log("[Mods] Failed to locate ModManager._mods");
            return false;
        }

        if (!TryResolveRuntimeLoadState(
            modManagerType,
            out var initializedField,
            out var stateProperty
        ))
        {
            PatchHelper.Log(
                "[Mods] Failed to locate an exact ModManager runtime-load state (_initialized bool or State enum with None=0)"
            );
            return false;
        }

        var tryLoadMod = FindTryLoadMod(modManagerType);
        if (tryLoadMod == null)
        {
            PatchHelper.Log("[Mods] Failed to locate ModManager.TryLoadMod(Mod)");
            return false;
        }

        var modType = tryLoadMod.GetParameters()[0].ParameterType;
        var sourceType = modType.GetField("modSource", AllInstance)?.FieldType;
        var sourceValue = ResolveModSource(sourceType);
        if (sourceValue == null)
        {
            PatchHelper.Log("[Mods] Failed to resolve ModSource.ModsDirectory");
            return false;
        }

        var manifestType = ResolveManifestType(modType);
        if (manifestType == null)
        {
            PatchHelper.Log("[Mods] Failed to locate Mod.manifest");
            return false;
        }

        access = new ModManagerAccess(
            initializedField,
            stateProperty,
            tryLoadMod,
            modsField,
            modManagerType.GetField("_settings", AllStatic),
            sourceValue,
            modType,
            manifestType
        );
        return true;
    }

    internal static bool TryResolveRuntimeLoadState(
        Type modManagerType,
        out FieldInfo initializedField,
        out PropertyInfo stateProperty
    )
    {
        initializedField = null;
        stateProperty = null;
        if (modManagerType == null)
            return false;

        try
        {
            var legacyField = modManagerType.GetField("_initialized", AllStatic);
            var legacySupported = legacyField != null
                && legacyField.IsStatic
                && !legacyField.IsInitOnly
                && !legacyField.IsLiteral
                && legacyField.FieldType == typeof(bool);

            var currentProperty = modManagerType.GetProperty("State", AllStatic);
            var getter = currentProperty?.GetGetMethod(nonPublic: true);
            var setter = currentProperty?.GetSetMethod(nonPublic: true);
            var currentSupported = currentProperty != null
                && currentProperty.GetIndexParameters().Length == 0
                && getter?.IsStatic == true
                && setter?.IsStatic == true
                && IsExactNoneState(currentProperty.PropertyType);

            // More than one recognized gate is ambiguous. Refuse to mutate either.
            if (legacySupported == currentSupported)
                return false;

            if (legacySupported)
                initializedField = legacyField;
            else
                stateProperty = currentProperty;

            return true;
        }
        catch
        {
            initializedField = null;
            stateProperty = null;
            return false;
        }
    }

    private static bool IsExactNoneState(Type stateType)
    {
        if (stateType?.IsEnum != true
            || !Enum.GetNames(stateType).Contains("None", StringComparer.Ordinal))
        {
            return false;
        }

        try
        {
            var none = Enum.Parse(stateType, "None", ignoreCase: false);
            return Convert.ToInt64(none) == 0;
        }
        catch
        {
            return false;
        }
    }

    private static MethodInfo FindTryLoadMod(Type modManagerType)
    {
        foreach (var method in modManagerType.GetMethods(AllStatic))
        {
            if (!string.Equals(method.Name, "TryLoadMod", StringComparison.Ordinal))
                continue;

            if (method.GetParameters().Length == 1)
                return method;
        }

        return null;
    }


    private static Type ResolveManifestType(Type modType)
    {
        if (modType == null)
            return null;

        return modType.GetField("manifest", AllInstance)?.FieldType
            ?? modType.GetProperty("manifest", AllInstance)?.PropertyType
            ?? modType.GetProperty("Manifest", AllInstance)?.PropertyType;
    }

    private static object ResolveModSource(Type sourceType)
    {
        try
        {
            return Enum.Parse(sourceType, "ModsDirectory");
        }
        catch
        {
            return null;
        }
    }

    private static int LoadAndroidModRoots(
        ModManagerAccess access,
        LauncherModLaunchPlan launchPlan
    )
    {
        var loadedRoots = 0;
        foreach (var mod in launchPlan.EnabledMods)
        {
            var root = new ModRoot(
                $"{mod.Source} mod {mod.Name}",
                mod.RootPath,
                mod.RequiresWorkshopConsent
            );
            PatchHelper.Log(
                $"[Mods] Loading validated manifest for {mod.ManifestId}: {mod.ManifestPath}"
            );
            if (access.TryLoadPlannedMod(root, mod))
                loadedRoots++;
            else
                PatchHelper.Log($"[Mods] Validated Android mod load failed for {root.Label}");
        }

        return loadedRoots;
    }

    internal static bool IsRuntimeModExplicitlyLoaded(object mod)
    {
        return TryReadRuntimeLoadState(mod, out var state)
            && state != null
            && state.GetType().IsEnum
            && string.Equals(state.ToString(), "Loaded", StringComparison.Ordinal);
    }

    internal static Assembly TryReadRuntimeAssembly(object mod)
    {
        if (mod == null)
            return null;

        try
        {
            object ReadMember(string name)
            {
                var type = mod.GetType();
                var field = type.GetField(name, AllInstance);
                if (field != null)
                    return field.GetValue(mod);

                var property = type.GetProperty(name, AllInstance);
                return property?.GetIndexParameters().Length == 0
                    ? property.GetValue(mod)
                    : null;
            }

            var legacyAssembly = ReadMember("assembly") as Assembly
                ?? ReadMember("Assembly") as Assembly;
            if (legacyAssembly != null)
                return legacyAssembly;

            var assemblies = ReadMember("assemblies") ?? ReadMember("Assemblies");
            if (assemblies is IEnumerable values)
            {
                foreach (var value in values)
                {
                    if (value is Assembly assembly)
                        return assembly;
                }
            }
        }
        catch
        {
        }

        return null;
    }

    private static bool TryReadRuntimeLoadState(object mod, out object state)
    {
        state = null;
        if (mod == null)
            return false;

        try
        {
            var type = mod.GetType();
            var field = type.GetField("state", AllInstance)
                ?? type.GetField("State", AllInstance);
            if (field != null)
            {
                state = field.GetValue(mod);
                return state != null;
            }

            var property = type.GetProperty("state", AllInstance)
                ?? type.GetProperty("State", AllInstance);
            if (property != null && property.GetIndexParameters().Length == 0)
            {
                state = property.GetValue(mod);
                return state != null;
            }
        }
        catch
        {
        }

        return false;
    }

    private sealed partial class ModManagerAccess
    {
        private readonly FieldInfo _initializedField;
        private readonly PropertyInfo _stateProperty;
        private readonly MethodInfo _tryLoadMod;
        private readonly FieldInfo _modsField;
        private readonly FieldInfo _settingsField;
        private readonly object _sourceValue;
        private readonly Type _modType;
        private readonly Type _manifestType;

        internal ModManagerAccess(
            FieldInfo initializedField,
            PropertyInfo stateProperty,
            MethodInfo tryLoadMod,
            FieldInfo modsField,
            FieldInfo settingsField,
            object sourceValue,
            Type modType,
            Type manifestType
        )
        {
            _initializedField = initializedField;
            _stateProperty = stateProperty;
            _tryLoadMod = tryLoadMod;
            _modsField = modsField;
            _settingsField = settingsField;
            _sourceValue = sourceValue;
            _modType = modType;
            _manifestType = manifestType;
        }

        internal bool TryLoadPlannedMod(ModRoot root, LauncherResolvedMod plannedMod)
        {
            return TryLoadPlannedModCore(root, plannedMod);
        }

        private bool TryLoadPlannedModCore(ModRoot root, LauncherResolvedMod plannedMod)
        {
            try
            {
                if (plannedMod == null || string.IsNullOrWhiteSpace(plannedMod.ManifestId))
                    return false;

                PatchHelper.Log(
                    $"[Mods] Loading planned mod {plannedMod.ManifestId}: {plannedMod.ManifestPath}"
                );
                var mod = CreatePlannedRuntimeMod(plannedMod);
                if (mod == null)
                {
                    PatchHelper.Log("[Mods] Planned loader could not create the runtime mod object");
                    return false;
                }

                TrySetModSource(mod);

                var loaded = string.Equals(
                        plannedMod.ManifestId,
                        "BaseLib",
                        StringComparison.Ordinal
                    )
                    ? TryLoadBaseLibForAndroidObject(mod, plannedMod)
                    : TryLoadWithModManager(root, mod, plannedMod);
                if (!loaded || !IsRuntimeModExplicitlyLoaded(mod))
                {
                    PatchHelper.Log(
                        $"[Mods] Planned loader did not produce a loaded mod: {plannedMod.ManifestId}"
                    );
                    return false;
                }

                AddOrReplaceMod(mod);
                PatchHelper.Log(
                    $"[Mods] Planned mod load complete. Id={plannedMod.ManifestId} Path={plannedMod.RootPath}"
                );
                return true;
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Mods] Planned mod load failed: {ex}");
                return false;
            }
        }

        private object CreatePlannedRuntimeMod(LauncherResolvedMod plannedMod)
        {
            if (_modType == null || _manifestType == null)
                return null;

            try
            {
                var manifest = Activator.CreateInstance(_manifestType);
                if (manifest == null)
                    return null;

                SetMemberValue(manifest, "id", plannedMod.ManifestId);
                SetMemberValue(manifest, "name", plannedMod.Name);
                SetMemberValue(manifest, "author", plannedMod.Author);
                SetMemberValue(manifest, "description", plannedMod.Description);
                SetMemberValue(manifest, "version", plannedMod.Version);
                SetMemberValue(manifest, "hasPck", !string.IsNullOrWhiteSpace(plannedMod.PckPath));
                SetMemberValue(manifest, "hasDll", !string.IsNullOrWhiteSpace(plannedMod.DllPath));
                SetMemberValue(manifest, "affectsGameplay", plannedMod.AffectsGameplay);
                SetManifestDependencies(
                    manifest,
                    plannedMod.Dependencies.Select(dependency => dependency.Id).ToArray()
                );

                var mod = Activator.CreateInstance(_modType);
                if (mod == null)
                    return null;

                SetMemberValue(mod, "path", plannedMod.RootPath);
                SetMemberValue(mod, "manifest", manifest);
                SetMemberValue(mod, "errors", null);
                return mod;
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Mods] Planned runtime object creation failed: {ex.Message}");
                return null;
            }
        }

        private void TrySetModSource(object mod)
        {
            try
            {
                if (_sourceValue == null)
                    return;

                SetMemberValue(mod, "modSource", _sourceValue);
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Mods] Planned loader could not set mod source: {ex.Message}");
            }
        }

        private bool TryLoadWithModManager(
            ModRoot root,
            object mod,
            LauncherResolvedMod plannedMod
        )
        {
            if (_tryLoadMod == null)
            {
                PatchHelper.Log(
                    $"[Mods] Planned loader cannot load {plannedMod.ManifestId}: ModManager.TryLoadMod was not found"
                );
                return false;
            }

            using var loadWindow = BeginRuntimeLoadWindow();
            try
            {
                ApplyWorkshopConsentIfAvailable(root);
                AddOrReplaceMod(mod);
                PatchHelper.Log($"[Mods] Loading planned mod {plannedMod.ManifestId}");
                _tryLoadMod.Invoke(null, new[] { mod });
                ApplyLoadedAssemblyCompatibilityPatches();
                LogAndroidModHarmonyDiagnostics(plannedMod, mod);
                var explicitlyLoaded = IsRuntimeModExplicitlyLoaded(mod);
                PatchHelper.Log(
                    $"[Mods] Planned mod load complete: {plannedMod.ManifestId} loaded={explicitlyLoaded}"
                );
                return explicitlyLoaded;
            }
            catch (TargetInvocationException ex)
            {
                PatchHelper.Log(
                    $"[Mods] Planned mod load failed for {plannedMod.ManifestId}: {ex.InnerException ?? ex}"
                );
                return false;
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Mods] Planned mod load failed for {plannedMod.ManifestId}: {ex}");
                return false;
            }
        }

        private static void LogAndroidModHarmonyDiagnostics(
            LauncherResolvedMod plannedMod,
            object mod
        )
        {
            try
            {
                var assembly = TryReadAssemblyMember(mod)
                    ?? FindLoadedAssemblyBySimpleName(plannedMod?.ManifestId);
                var assemblyName = assembly?.GetName().Name ?? "<none>";
                var patchTypes = CountHarmonyPatchTypes(assembly);
                var ownerCandidates = new[] { plannedMod?.ManifestId }
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToArray();
                var targetSummaries = FindHarmonyTargetsForOwners(ownerCandidates, out var matchedTargetCount);

                PatchHelper.Log(
                    $"[Mods] Harmony diagnostics for {plannedMod?.ManifestId ?? "<unknown>"}: assembly={assemblyName} patchTypes={patchTypes} ownerCandidates=[{string.Join(", ", ownerCandidates)}] matchedTargets={matchedTargetCount}"
                );

                foreach (var target in targetSummaries)
                    PatchHelper.Log($"[Mods] Harmony target for {plannedMod?.ManifestId ?? "<unknown>"}: {target}");
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Mods] Harmony diagnostics failed for {plannedMod?.ManifestId ?? "<unknown>"}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private static Assembly TryReadAssemblyMember(object mod)
            => TryReadRuntimeAssembly(mod);

        private static Assembly FindLoadedAssemblyBySimpleName(string simpleName)
        {
            if (string.IsNullOrWhiteSpace(simpleName))
                return null;

            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (string.Equals(assembly.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase))
                        return assembly;
                }
            }
            catch
            {
            }

            return null;
        }

        private static int CountHarmonyPatchTypes(Assembly assembly)
        {
            if (assembly == null)
                return 0;

            try
            {
                return assembly
                    .GetTypes()
                    .Count(HasHarmonyPatchAttribute);
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types
                    .Where(type => type != null)
                    .Count(HasHarmonyPatchAttribute);
            }
        }

        private static string[] FindHarmonyTargetsForOwners(IReadOnlyCollection<string> ownerCandidates, out int matchedTargetCount)
        {
            matchedTargetCount = 0;
            if (ownerCandidates == null || ownerCandidates.Count == 0)
                return Array.Empty<string>();

            var summaries = new List<string>();
            try
            {
                foreach (var method in Harmony.GetAllPatchedMethods())
                {
                    var patchInfo = Harmony.GetPatchInfo(method);
                    var owners = ReadHarmonyPatchOwners(patchInfo).ToArray();
                    if (!owners.Any(owner => ownerCandidates.Contains(owner, StringComparer.OrdinalIgnoreCase)))
                        continue;

                    matchedTargetCount++;
                    if (summaries.Count < 12)
                    {
                        var matchedOwners = owners
                            .Where(owner => ownerCandidates.Contains(owner, StringComparer.OrdinalIgnoreCase))
                            .Distinct(StringComparer.OrdinalIgnoreCase);
                        summaries.Add($"{DescribeMethod(method)} owners=[{string.Join(", ", matchedOwners)}]");
                    }
                }
            }
            catch (Exception ex)
            {
                summaries.Add($"<diagnostic failed: {ex.GetType().Name}: {ex.Message}>");
            }

            return summaries.ToArray();
        }

        private static IEnumerable<string> ReadHarmonyPatchOwners(object patchInfo)
        {
            if (patchInfo == null)
                yield break;

            foreach (var listName in new[] { "Prefixes", "Postfixes", "Transpilers", "Finalizers" })
            {
                if (TryReadMemberValue(patchInfo, listName) is not IEnumerable patches)
                    continue;

                foreach (var patch in patches)
                {
                    var owner = TryReadStringMember(patch, "owner")
                        ?? TryReadStringMember(patch, "Owner");
                    if (!string.IsNullOrWhiteSpace(owner))
                        yield return owner;
                }
            }
        }

        private static string DescribeMethod(MethodBase method)
        {
            if (method == null)
                return "<unknown>";

            var declaringType = method.DeclaringType?.FullName ?? "<unknown type>";
            return $"{declaringType}.{method.Name}";
        }

        private static string SanitizeAssemblyName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var chars = name
                .Where(char.IsLetterOrDigit)
                .ToArray();
            return chars.Length == 0 ? null : new string(chars);
        }

        private bool TryLoadBaseLibForAndroidObject(
            object mod,
            LauncherResolvedMod plannedMod
        )
        {
            if (mod == null
                || plannedMod == null
                || !string.Equals(plannedMod.ManifestId, "BaseLib", StringComparison.Ordinal))
            {
                return false;
            }

            Assembly assembly = null;
            try
            {
                var loadedSomething = false;
                var dllPath = plannedMod.DllPath;
                if (!string.IsNullOrWhiteSpace(dllPath))
                {
                    if (Godot.FileAccess.FileExists(dllPath))
                    {
                        PatchHelper.Log($"[Mods] Loading BaseLib DLL via Android-safe path: {dllPath}");
                        var loadContext = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly());
                        assembly = loadContext?.LoadFromAssemblyPath(dllPath) ?? Assembly.LoadFrom(dllPath);
                        TryApplyBaseLibJsonCompatibilityPatch(assembly);
                        loadedSomething = true;
                    }
                    else
                    {
                        PatchHelper.Log($"[Mods] BaseLib manifest declares DLL but file is missing: {dllPath}");
                    }
                }

                var pckPath = plannedMod.PckPath;
                if (!string.IsNullOrWhiteSpace(pckPath))
                {
                    if (Godot.FileAccess.FileExists(pckPath))
                    {
                        PatchHelper.Log($"[Mods] Loading BaseLib PCK via Android-safe path: {pckPath}");
                        if (!ProjectSettings.LoadResourcePack(pckPath, true, 0))
                            throw new InvalidOperationException("Godot errored while loading BaseLib PCK.");

                        loadedSomething = true;
                    }
                    else
                    {
                        PatchHelper.Log($"[Mods] BaseLib manifest declares PCK but file is missing: {pckPath}");
                    }
                }

                if (!loadedSomething)
                    throw new InvalidOperationException("Neither BaseLib DLL nor BaseLib PCK was loaded.");

                if (assembly != null)
                {
                    PatchHelper.Log("[Mods] Running BaseLib Android-safe initializer through reflection loader");
                    RunBaseLibAndroidSafeInitialize(assembly);
                    PatchHelper.Log("[Mods] BaseLib Android-safe initializer through reflection loader complete");
                }

                if (assembly != null && !TryRecordRuntimeAssembly(mod, assembly))
                {
                    throw new InvalidOperationException(
                        "BaseLib payload completed, but ModManager exposed no supported assembly marker."
                    );
                }
                SetMemberValue(mod, "errors", null);
                if (!SetEnumMember(mod, "state", "Loaded")
                    || !IsRuntimeModExplicitlyLoaded(mod))
                {
                    throw new InvalidOperationException(
                        "BaseLib payload completed, but an exact Loaded runtime state could not be recorded."
                    );
                }
                PatchHelper.Log("[Mods] BaseLib loaded through Android-safe staged Workshop path");
                return true;
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Mods] BaseLib Android-safe load failed: {ex}");
                SetEnumMember(mod, "state", "Failed");
                if (assembly != null)
                    TryRecordRuntimeAssembly(mod, assembly);
                return false;
            }
        }

        private static bool TryRecordRuntimeAssembly(object mod, Assembly assembly)
        {
            if (mod == null || assembly == null)
                return false;

            try
            {
                var legacyType = GetWritableMemberType(mod, "assembly")
                    ?? GetWritableMemberType(mod, "Assembly");
                if (legacyType?.IsInstanceOfType(assembly) == true)
                {
                    return SetMemberValue(mod, "assembly", assembly)
                        || SetMemberValue(mod, "Assembly", assembly);
                }

                var values = TryReadMemberValue(mod, "assemblies") as IList
                    ?? TryReadMemberValue(mod, "Assemblies") as IList;
                if (values == null)
                    return false;

                if (!values.Contains(assembly))
                    values.Add(assembly);
                return values.Contains(assembly);
            }
            catch (Exception ex)
            {
                PatchHelper.Log(
                    $"[Mods] Failed to record runtime assembly for {assembly.GetName().Name}: {ex.Message}"
                );
                return false;
            }
        }

        private static void SetManifestDependencies(object manifest, IReadOnlyCollection<string> dependencies)
        {
            var memberType = GetWritableMemberType(manifest, "dependencies");
            if (memberType == null)
                return;

            try
            {
                if (memberType.IsAssignableFrom(typeof(List<string>)))
                {
                    SetMemberValue(manifest, "dependencies", dependencies?.ToList() ?? new List<string>());
                    return;
                }

                if (!typeof(IList).IsAssignableFrom(memberType))
                    return;

                var list = Activator.CreateInstance(memberType) as IList;
                if (list == null)
                    return;

                var itemType = memberType.IsGenericType ? memberType.GetGenericArguments()[0] : typeof(object);
                if (itemType == typeof(string) && dependencies != null)
                {
                    foreach (var dependency in dependencies)
                        list.Add(dependency);
                }

                SetMemberValue(manifest, "dependencies", list);
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Mods] Failed to set manifest dependencies: {ex.Message}");
            }
        }

        private static bool SetEnumMember(object target, string name, string enumName)
        {
            var memberType = GetWritableMemberType(target, name);
            if (memberType == null || !memberType.IsEnum)
                return false;

            return SetMemberValue(target, name, Enum.Parse(memberType, enumName));
        }

        private static Type GetWritableMemberType(object target, string name)
        {
            if (target == null)
                return null;

            var type = target.GetType();
            var field = type.GetField(name, AllInstance);
            if (field != null)
                return field.FieldType;

            var property = type.GetProperty(name, AllInstance);
            return property?.CanWrite == true ? property.PropertyType : null;
        }

        private static bool SetMemberValue(object target, string name, object value)
        {
            if (target == null)
                return false;

            try
            {
                var type = target.GetType();
                var field = type.GetField(name, AllInstance);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return true;
                }

                var property = type.GetProperty(name, AllInstance);
                if (property?.CanWrite == true)
                {
                    property.SetValue(target, value);
                    return true;
                }
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Mods] Failed to set {name}: {ex.Message}");
            }

            return false;
        }

        private void AddOrReplaceMod(object mod)
        {
            if (_modsField.GetValue(null) is not IList mods)
            {
                PatchHelper.Log("[Mods] Planned loader could not update ModManager._mods");
                return;
            }

            var modPath = TryReadStringMember(mod, "path") ?? TryReadStringMember(mod, "Path") ?? "";
            for (var i = mods.Count - 1; i >= 0; i--)
            {
                var existingPath = TryReadStringMember(mods[i], "path") ?? TryReadStringMember(mods[i], "Path") ?? "";
                if (string.Equals(existingPath, modPath, StringComparison.OrdinalIgnoreCase))
                {
                    mods.RemoveAt(i);
                }
            }

            mods.Add(mod);
        }

        private RuntimeLoadWindow BeginRuntimeLoadWindow()
        {
            return new RuntimeLoadWindow(_initializedField, _stateProperty);
        }

        private void ApplyWorkshopConsentIfAvailable(ModRoot root)
        {
            if (!root.RequiresWorkshopConsent)
                return;

            if (!WorkshopModConsent.IsAccepted())
            {
                PatchHelper.Log(
                    "[Mods] Workshop staged mods require the launcher Workshop mod consent marker; game mod warning remains active"
                );
                return;
            }

            if (TrySetPlayerAgreedToModLoading())
                PatchHelper.Log("[Mods] Workshop mod consent marker applied to game mod settings");
            else
                PatchHelper.Log("[Mods] Workshop mod consent marker present but game mod settings could not be updated");
        }

        private bool TrySetPlayerAgreedToModLoading()
        {
            if (_settingsField == null)
                return false;

            try
            {
                var settings = _settingsField.GetValue(null);
                if (settings == null)
                {
                    settings = Activator.CreateInstance(_settingsField.FieldType);
                    _settingsField.SetValue(null, settings);
                }

                var property = settings.GetType().GetProperty(
                    "PlayerAgreedToModLoading",
                    AllInstance
                );
                if (property == null || !property.CanWrite)
                    return false;

                property.SetValue(settings, true);
                return true;
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Mods] Failed to apply Workshop mod consent: {ex.Message}");
                return false;
            }
        }

        private static string TryReadManifestId(object mod)
        {
            try
            {
                var manifest = mod.GetType().GetField("manifest", AllInstance)?.GetValue(mod)
                    ?? mod.GetType().GetProperty("manifest", AllInstance)?.GetValue(mod)
                    ?? mod.GetType().GetProperty("Manifest", AllInstance)?.GetValue(mod);
                if (manifest == null)
                    return null;

                return TryReadStringMember(manifest, "id")
                    ?? TryReadStringMember(manifest, "Id");
            }
            catch
            {
                return null;
            }
        }

        private static string TryReadStringMember(object target, string name)
        {
            try
            {
                return TryReadMemberValue(target, name) as string;
            }
            catch
            {
            }

            return null;
        }

        private static object TryReadMemberValue(object target, string name)
        {
            try
            {
                if (target == null)
                    return null;

                var type = target.GetType();
                var field = type.GetField(name, AllInstance);
                if (field != null)
                    return field.GetValue(target);

                var property = type.GetProperty(name, AllInstance);
                return property?.GetValue(target);
            }
            catch
            {
                return null;
            }
        }

    }

    private sealed class RuntimeLoadWindow : IDisposable
    {
        private readonly FieldInfo _initializedField;
        private readonly PropertyInfo _stateProperty;
        private readonly object _previousInitialized;
        private readonly bool _changedState;

        internal RuntimeLoadWindow(FieldInfo initializedField, PropertyInfo stateProperty)
        {
            _initializedField = initializedField;
            _stateProperty = stateProperty;

            if (_initializedField != null)
            {
                _previousInitialized = _initializedField.GetValue(null);
                _initializedField.SetValue(null, false);
                _changedState = true;
                return;
            }

            if (_stateProperty == null)
                throw new InvalidOperationException("No supported ModManager runtime-load state was resolved.");

            _previousInitialized = _stateProperty.GetValue(null);
            var none = Enum.Parse(_stateProperty.PropertyType, "None", ignoreCase: false);
            SetState(_stateProperty, none);
            _changedState = true;
        }

        public void Dispose()
        {
            if (!_changedState)
                return;

            if (_initializedField != null)
                _initializedField.SetValue(null, _previousInitialized);
            else
                SetState(_stateProperty, _previousInitialized);
        }

        private static void SetState(PropertyInfo stateProperty, object value)
        {
            var setter = stateProperty?.GetSetMethod(nonPublic: true)
                ?? throw new InvalidOperationException(
                    "The resolved ModManager State property has no setter."
                );
            setter.Invoke(null, new[] { value });
        }
    }

    private readonly struct ModRoot
    {
        internal ModRoot(string label, string path, bool requiresWorkshopConsent)
        {
            Label = label;
            Path = path;
            RequiresWorkshopConsent = requiresWorkshopConsent;
        }

        internal string Label { get; }
        internal string Path { get; }
        internal bool RequiresWorkshopConsent { get; }
    }

}
