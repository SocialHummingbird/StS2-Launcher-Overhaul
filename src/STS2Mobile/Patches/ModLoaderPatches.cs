using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using STS2Mobile.Launcher;

namespace STS2Mobile.Patches;

// Extends ModManager to scan Android mod roots after the built-in game scan.
// Workshop sync stages into app-private storage; shared storage remains available
// for manual sideloading through /storage/emulated/0/StS2Launcher/Mods/.
internal static class ModLoaderPatches
{
    private static readonly BindingFlags AllStatic =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly BindingFlags AllInstance =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags LoadedMarkerFlags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static Harmony _harmony;
    private static readonly HashSet<string> AppliedModCompatibilityPatches = new(StringComparer.Ordinal);
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
        PatchBaseLibAssemblyLoadHooks(harmony);
        PatchHelper.Patch(
            harmony,
            typeof(ModManager),
            "Initialize",
            postfix: PatchHelper.Method(typeof(ModLoaderPatches), nameof(InitializePostfix))
        );
    }

    // Runs after the original Initialize() to pick up Android-only mod roots.
    private static void InitializePostfix()
    {
        try
        {
            if (!TryLoadModManagerAccess(out var access))
            {
                return;
            }

            if (!LauncherModSelectionState.IsModdedMode)
            {
                WriteModLaunchMarker("vanilla", 0, 0, "Android mod scan skipped by launcher Play Vanilla mode");
                PatchHelper.Log("[Mods] Android mod scan skipped: launcher Play Vanilla mode is selected");
                return;
            }

            var loadedRoots = LoadAndroidModRoots(access);
            if (loadedRoots == 0)
            {
                WriteModLaunchMarker("modded", 0, 0, "No Android mod roots were available for scanning");
                PatchHelper.Log("[Mods] No Android mod roots were available for scanning");
                return;
            }

            access.RebuildLoadedModsCacheIfAvailable();
            WriteModLaunchMarker(
                "modded",
                loadedRoots,
                LauncherModSelectionState.EnabledModCount(),
                "Android mod scan completed with launcher-selected mods"
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] Failed to load Android mods: {ex}");
        }
    }

    private static void WriteModLaunchMarker(string playMode, int scannedRoots, int enabledMods, string status)
    {
        try
        {
            var parent = Path.GetDirectoryName(AppPaths.AppPrivateLastModLaunchPath);
            if (!string.IsNullOrWhiteSpace(parent))
                Directory.CreateDirectory(parent);

            var payload = new
            {
                version = 1,
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                playMode,
                scannedRoots,
                enabledMods,
                status,
                selectionPath = AppPaths.AppPrivateModSelectionPath,
                workshopModdedSaveCloudPushLocked = LauncherWorkshopModSafety.HasActiveStagedMods(),
                steamCloudPushPerformed = false,
                selectedMods = string.Equals(playMode, "modded", StringComparison.OrdinalIgnoreCase)
                    ? LauncherModSelectionState.KnownMods()
                    .Where(mod => mod.Enabled && !mod.IsUnsupported)
                    .Select(mod => new
                    {
                        mod.Key,
                        mod.Id,
                        mod.Title,
                        mod.Source,
                        mod.Path,
                        mod.IsDependency,
                        mod.IsRequiredDependency,
                    })
                    .ToArray()
                    : Array.Empty<object>(),
            };
            File.WriteAllText(
                AppPaths.AppPrivateLastModLaunchPath,
                JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true })
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] Failed to write mod launch marker: {ex.Message}");
        }
    }

    private static void PatchBaseLibAssemblyLoadHooks(Harmony harmony)
    {
        var postfix = PatchHelper.Method(
            typeof(ModLoaderPatches),
            nameof(BaseLibAssemblyLoadPostfix)
        );

        PatchAssemblyLoadMethod(
            harmony,
            typeof(Assembly).GetMethod(nameof(Assembly.LoadFrom), new[] { typeof(string) }),
            "Assembly.LoadFrom(string)",
            postfix
        );
        PatchAssemblyLoadMethod(
            harmony,
            typeof(Assembly).GetMethod(nameof(Assembly.LoadFile), new[] { typeof(string) }),
            "Assembly.LoadFile(string)",
            postfix
        );
        PatchAssemblyLoadMethod(
            harmony,
            typeof(Assembly).GetMethod(nameof(Assembly.Load), new[] { typeof(byte[]) }),
            "Assembly.Load(byte[])",
            postfix
        );
        PatchAssemblyLoadMethod(
            harmony,
            typeof(Assembly).GetMethod(nameof(Assembly.Load), new[] { typeof(byte[]), typeof(byte[]) }),
            "Assembly.Load(byte[], byte[])",
            postfix
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
        PatchAssemblyLoadMethod(
            harmony,
            typeof(AssemblyLoadContext).GetMethod(
                nameof(AssemblyLoadContext.LoadFromStream),
                AllInstance,
                binder: null,
                types: new[] { typeof(Stream) },
                modifiers: null
            ),
            "AssemblyLoadContext.LoadFromStream(Stream)",
            postfix
        );
        PatchAssemblyLoadMethod(
            harmony,
            typeof(AssemblyLoadContext).GetMethod(
                nameof(AssemblyLoadContext.LoadFromStream),
                AllInstance,
                binder: null,
                types: new[] { typeof(Stream), typeof(Stream) },
                modifiers: null
            ),
            "AssemblyLoadContext.LoadFromStream(Stream, Stream)",
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

    private static void BaseLibAssemblyLoadPostfix(Assembly __result)
    {
        TryApplyBaseLibJsonCompatibilityPatch(__result);
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
        TryInvokeBaseLibPatch(
            baseLibAssembly,
            "BaseLib.Patches.Content.TheBigPatchToCardPileCmdAdd",
            mainHarmony
        );
        TryInvokeBaseLibPatch(baseLibAssembly, "BaseLib.Abstracts.CustomBadgesPatch", mainHarmony);
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

    private static void TryInvokeBaseLibTryPatchAll(Assembly baseLibAssembly, Harmony harmony)
    {
        try
        {
            var harmonyExtensions = baseLibAssembly.GetType(
                "BaseLib.Extensions.HarmonyExtensions",
                throwOnError: false
            );
            var tryPatchAllMethod = harmonyExtensions?.GetMethod("TryPatchAll", AllStatic);
            tryPatchAllMethod?.Invoke(null, new object[] { harmony, baseLibAssembly, null });
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] BaseLib Android PatchAll invocation failed: {ex.Message}");
        }
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

    private static bool TryLoadBaseLibForAndroid(Mod mod)
    {
        if (mod?.manifest == null
            || !string.Equals(mod.manifest.id, "BaseLib", StringComparison.Ordinal))
        {
            return false;
        }

        var errors = mod.errors ?? new List<LocString>();
        Assembly assembly = null;

        try
        {
            TrySetModSemanticVersion(mod);

            var loadedSomething = false;
            var dllPath = Path.Combine(mod.path, "BaseLib.dll");
            if (mod.manifest.hasDll)
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

            var pckPath = Path.Combine(mod.path, "BaseLib.pck");
            if (mod.manifest.hasPck)
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
                PatchHelper.Log("[Mods] Running BaseLib Android-safe initializer through direct loader");
                RunBaseLibAndroidSafeInitialize(assembly);
                PatchHelper.Log("[Mods] BaseLib Android-safe initializer through direct loader complete");
            }
            else
            {
                PatchHelper.Log("[Mods] BaseLib Android-safe initializer skipped because no assembly was returned");
            }

            mod.state = ModLoadState.Loaded;
            mod.assembly = assembly;
            mod.errors = errors.Count == 0 ? null : errors;
            PatchHelper.Log("[Mods] BaseLib loaded through Android-safe staged Workshop path");
            return true;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] BaseLib Android-safe load failed: {ex}");
            mod.state = ModLoadState.Failed;
            mod.assembly = assembly;
            mod.errors = errors.Count == 0 ? null : errors;
            return true;
        }
    }

    private static void TrySetModSemanticVersion(Mod mod)
    {
        if (mod?.manifest?.version == null)
            return;

        try
        {
            var versionField = mod.GetType().GetField("version", AllInstance);
            if (versionField == null)
                return;

            var semanticVersionType = versionField.FieldType;
            var tryFromString = semanticVersionType.GetMethod(
                "TryFromString",
                BindingFlags.Public | BindingFlags.Static
            );
            if (tryFromString == null)
                return;

            var args = new object[] { mod.manifest.version, null };
            if (tryFromString.Invoke(null, args) is true)
                versionField.SetValue(mod, args[1]);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] BaseLib version parse skipped on Android: {ex.Message}");
        }
    }

    private static bool HasHarmonyPatchAttribute(Type type)
    {
        foreach (var attribute in type.GetCustomAttributes(inherit: true))
        {
            var attributeType = attribute.GetType();
            if (string.Equals(attributeType.Namespace, "HarmonyLib", StringComparison.Ordinal)
                && attributeType.Name.StartsWith("Harmony", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryLoadModManagerAccess(out ModManagerAccess access)
    {
        var modManagerType = typeof(ModManager);
        access = null;

        var scanMethod = FindScanMethod(modManagerType);
        if (scanMethod == null)
        {
            PatchHelper.Log("[Mods] Failed to locate a compatible ModManager mod scan method");
            return false;
        }

        var modsField = modManagerType.GetField("_mods", AllStatic);
        if (modsField == null)
        {
            PatchHelper.Log("[Mods] Failed to locate ModManager._mods");
            return false;
        }

        var initializedField = modManagerType.GetField("_initialized", AllStatic);
        var stateProperty = modManagerType.GetProperty("State", AllStatic);
        if (initializedField == null && stateProperty == null)
        {
            PatchHelper.Log("[Mods] Failed to locate a ModManager runtime load gate");
            return false;
        }

        var sourceType = scanMethod.GetParameters()[1].ParameterType;
        var sourceValue = ResolveModSource(sourceType);
        if (sourceValue == null)
        {
            PatchHelper.Log("[Mods] Failed to resolve ModSource.ModsDirectory");
            return false;
        }

        var tryLoadMod = FindTryLoadMod(modManagerType);
        var modType = ResolveModType(scanMethod, tryLoadMod, modsField);
        var newModsListType = modType == null ? null : typeof(List<>).MakeGenericType(modType);

        access = new ModManagerAccess(
            initializedField,
            stateProperty,
            scanMethod,
            tryLoadMod,
            modManagerType.GetMethod("SortModList", AllStatic),
            modsField,
            modManagerType.GetField("_loadedMods", AllStatic),
            modManagerType.GetField("_settings", AllStatic),
            sourceValue,
            newModsListType
        );
        return true;
    }

    private static MethodInfo FindScanMethod(Type modManagerType)
    {
        foreach (var methodName in new[] { "ReadModsInDirRecursive", "LoadModsInDirRecursive" })
        {
            foreach (var method in modManagerType.GetMethods(AllStatic))
            {
                if (!string.Equals(method.Name, methodName, StringComparison.Ordinal))
                    continue;

                var parameters = method.GetParameters();
                if (parameters.Length is < 2 or > 3)
                    continue;

                if (!parameters[1].ParameterType.IsEnum)
                    continue;

                if (parameters[0].ParameterType == typeof(string)
                    || typeof(DirAccess).IsAssignableFrom(parameters[0].ParameterType))
                {
                    return method;
                }
            }
        }

        return null;
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

    private static Type ResolveModType(
        MethodInfo scanMethod,
        MethodInfo tryLoadMod,
        FieldInfo modsField
    )
    {
        var tryLoadParameters = tryLoadMod?.GetParameters();
        if (tryLoadParameters?.Length == 1)
            return tryLoadParameters[0].ParameterType;

        var scanParameters = scanMethod.GetParameters();
        if (scanParameters.Length == 3)
        {
            var scanListElementType = ResolveEnumerableElementType(scanParameters[2].ParameterType);
            if (scanListElementType != null)
                return scanListElementType;
        }

        return ResolveEnumerableElementType(modsField.FieldType);
    }

    private static Type ResolveEnumerableElementType(Type type)
    {
        if (type == null)
            return null;

        if (type.IsArray)
            return type.GetElementType();

        if (type.IsGenericType && type.GetGenericArguments().Length == 1)
            return type.GetGenericArguments()[0];

        foreach (var interfaceType in type.GetInterfaces())
        {
            if (interfaceType.IsGenericType
                && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return interfaceType.GetGenericArguments()[0];
            }
        }

        return null;
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

    private static int LoadAndroidModRoots(ModManagerAccess access)
    {
        AppPaths.EnsureWorkshopDirectories();

        var loadedRoots = 0;
        foreach (var root in AndroidModRoots())
        {
            using var dirAccess = DirAccess.Open(root.Path);
            if (dirAccess == null)
            {
                PatchHelper.Log(
                    $"[Mods] {root.Label} not found: {root.Path} (error: {DirAccess.GetOpenError()})"
                );
                continue;
            }

            PatchHelper.Log($"[Mods] Scanning {root.Label}: {root.Path}");
            if (access.LoadModsInRoot(root, dirAccess))
                loadedRoots++;
        }

        return loadedRoots;
    }

    private static IEnumerable<ModRoot> AndroidModRoots()
    {
        yield return new ModRoot(
            "Workshop staged mods",
            AppPaths.AppPrivateWorkshopStagedModsDir,
            requiresWorkshopConsent: true
        );
        yield return new ModRoot(
            "external sideloaded mods",
            AppPaths.ExternalModsDir,
            requiresWorkshopConsent: false
        );
    }

    private static object CreateLoadedModsList(IEnumerable allMods, Type targetListType)
    {
        try
        {
            if (!typeof(IEnumerable).IsAssignableFrom(targetListType))
                return null;

            if (Activator.CreateInstance(targetListType) is not IList targetList)
            {
                PatchHelper.Log("[Mods] _loadedMods field is not a concrete IList");
                return null;
            }

            foreach (var mod in allMods)
            {
                if (mod == null || !IsLoaded(mod))
                    continue;

                targetList.Add(mod);
            }

            return targetList;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] Rebuild of _loadedMods failed: {ex.Message}");
            return null;
        }
    }

    private static bool IsLoaded(object mod)
    {
        foreach (var member in LoadedMarkerCandidates(mod.GetType()))
        {
            if (member is null)
                continue;

            object value = member switch
            {
                FieldInfo field => field.GetValue(mod),
                PropertyInfo prop => prop.GetValue(mod),
                _ => null,
            };

            if (value is bool loaded)
                return loaded;

            if (value != null && value.GetType().IsEnum)
                return string.Equals(value.ToString(), "Loaded", StringComparison.Ordinal);
        }

        // Without a reliable marker, assume loaded to avoid dropping mods.
        return true;
    }

    private static IEnumerable<MemberInfo> LoadedMarkerCandidates(Type modType)
    {
        yield return modType.GetField("wasLoaded", LoadedMarkerFlags);
        yield return modType.GetField("WasLoaded", LoadedMarkerFlags);
        yield return modType.GetField("isLoaded", LoadedMarkerFlags);
        yield return modType.GetField("IsLoaded", LoadedMarkerFlags);
        yield return modType.GetField("state", LoadedMarkerFlags);
        yield return modType.GetField("State", LoadedMarkerFlags);
        yield return modType.GetProperty("wasLoaded", LoadedMarkerFlags);
        yield return modType.GetProperty("WasLoaded", LoadedMarkerFlags);
        yield return modType.GetProperty("isLoaded", LoadedMarkerFlags);
        yield return modType.GetProperty("IsLoaded", LoadedMarkerFlags);
        yield return modType.GetProperty("state", LoadedMarkerFlags);
        yield return modType.GetProperty("State", LoadedMarkerFlags);
    }

    private sealed class ModManagerAccess
    {
        private readonly FieldInfo _initializedField;
        private readonly PropertyInfo _stateProperty;
        private readonly MethodInfo _scanMethod;
        private readonly MethodInfo _tryLoadMod;
        private readonly MethodInfo _sortModList;
        private readonly FieldInfo _modsField;
        private readonly FieldInfo _loadedModsField;
        private readonly FieldInfo _settingsField;
        private readonly object _sourceValue;
        private readonly Type _newModsListType;

        internal ModManagerAccess(
            FieldInfo initializedField,
            PropertyInfo stateProperty,
            MethodInfo scanMethod,
            MethodInfo tryLoadMod,
            MethodInfo sortModList,
            FieldInfo modsField,
            FieldInfo loadedModsField,
            FieldInfo settingsField,
            object sourceValue,
            Type newModsListType
        )
        {
            _initializedField = initializedField;
            _stateProperty = stateProperty;
            _scanMethod = scanMethod;
            _tryLoadMod = tryLoadMod;
            _sortModList = sortModList;
            _modsField = modsField;
            _loadedModsField = loadedModsField;
            _settingsField = settingsField;
            _sourceValue = sourceValue;
            _newModsListType = newModsListType;
        }

        internal bool LoadModsInRoot(ModRoot root, DirAccess dirAccess)
        {
            var newMods = CreateNewModsList();
            var scanArguments = BuildScanArguments(root.Path, dirAccess, newMods);
            if (scanArguments == null)
            {
                PatchHelper.Log($"[Mods] Unsupported scan signature for {root.Label}");
                return false;
            }

            using var loadWindow = BeginRuntimeLoadWindow();
            try
            {
                ApplyWorkshopConsentIfAvailable(root);
                _scanMethod.Invoke(null, scanArguments);
                var discovered = Count(newMods);

                ApplyLoadedAssemblyCompatibilityPatches();
                SortModsIfAvailable();
                var attempted = TryLoadNewMods(newMods);
                var loadedTotal = CountLoadedMods();

                PatchHelper.Log(
                    $"[Mods] {root.Label} scan complete. Discovered: {discovered}, load attempts: {attempted}, total loaded: {loadedTotal}"
                );
                return true;
            }
            catch (TargetInvocationException ex)
            {
                PatchHelper.Log($"[Mods] {root.Label} scan failed: {ex.InnerException ?? ex}");
                return false;
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Mods] {root.Label} scan failed: {ex}");
                return false;
            }
        }

        internal void RebuildLoadedModsCacheIfAvailable()
        {
            if (_loadedModsField == null)
            {
                PatchHelper.Log($"[Mods] External scan complete. Total loaded: {CountLoadedMods()}");
                return;
            }

            var allMods = _modsField.GetValue(null) as IEnumerable;
            if (allMods == null)
            {
                PatchHelper.Log("[Mods] ModManager._mods was null; skipping loaded-mod rebuild");
                return;
            }

            var loadedMods = CreateLoadedModsList(allMods, _loadedModsField.FieldType);
            if (loadedMods == null)
            {
                PatchHelper.Log("[Mods] Failed to rebuild _loadedMods via reflection");
                return;
            }

            _loadedModsField.SetValue(null, loadedMods);
            PatchHelper.Log(
                $"[Mods] External scan complete. Total loaded: {(loadedMods as ICollection)?.Count ?? 0}"
            );
        }

        private object CreateNewModsList()
        {
            if (_newModsListType == null)
                return null;

            try
            {
                return Activator.CreateInstance(_newModsListType);
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Mods] Failed to create new-mod capture list: {ex.Message}");
                return null;
            }
        }

        private object[] BuildScanArguments(string path, DirAccess dirAccess, object newMods)
        {
            var parameters = _scanMethod.GetParameters();
            if (parameters.Length == 2)
            {
                if (parameters[0].ParameterType == typeof(string))
                    return new[] { path, _sourceValue };

                if (typeof(DirAccess).IsAssignableFrom(parameters[0].ParameterType))
                    return new object[] { dirAccess, _sourceValue };
            }

            if (parameters.Length == 3 && parameters[0].ParameterType == typeof(string))
                return new[] { path, _sourceValue, newMods };

            return null;
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

        private void SortModsIfAvailable()
        {
            if (_sortModList == null)
                return;

            try
            {
                var parameters = _sortModList.GetParameters();
                if (parameters.Length != 1)
                    return;

                var modList = ResolveSettingsModList(parameters[0].ParameterType);
                if (modList == null)
                    return;

                _sortModList.Invoke(null, new[] { modList });
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Mods] Mod sort skipped: {ex.Message}");
            }
        }

        private object ResolveSettingsModList(Type parameterType)
        {
            var settings = _settingsField?.GetValue(null);
            if (settings != null)
            {
                var modList = settings.GetType()
                    .GetProperty("ModList", AllInstance)
                    ?.GetValue(settings);
                if (modList != null && parameterType.IsInstanceOfType(modList))
                    return modList;
            }

            try
            {
                return Activator.CreateInstance(parameterType);
            }
            catch
            {
                return null;
            }
        }

        private int TryLoadNewMods(object newMods)
        {
            if (_tryLoadMod == null || newMods is not IEnumerable mods)
                return 0;

            var newModSet = new HashSet<object>();
            foreach (var mod in mods)
            {
                if (mod != null)
                    newModSet.Add(mod);
            }

            var attempts = 0;
            foreach (var mod in OrderedNewMods(newModSet))
            {
                if (mod == null)
                    continue;

                try
                {
                    if (!IsSelectedForLaunch(mod))
                    {
                        PatchHelper.Log($"[Mods] Skipping disabled launcher-selected mod: {DescribeMod(mod)}");
                        continue;
                    }

                    if (mod is Mod typedMod && TryLoadBaseLibForAndroid(typedMod))
                    {
                        attempts++;
                        continue;
                    }

                    _tryLoadMod.Invoke(null, new[] { mod });
                    attempts++;
                }
                catch (TargetInvocationException ex)
                {
                    PatchHelper.Log($"[Mods] Mod load failed: {ex.InnerException ?? ex}");
                }
                catch (Exception ex)
                {
                    PatchHelper.Log($"[Mods] Mod load failed: {ex}");
                }
            }

            return attempts;
        }

        private static bool IsSelectedForLaunch(object mod)
        {
            if (mod is Mod typedMod)
                return LauncherModSelectionState.IsPathEnabled(typedMod.path);

            var path = TryReadStringMember(mod, "path")
                ?? TryReadStringMember(mod, "Path")
                ?? "";
            return LauncherModSelectionState.IsPathEnabled(path);
        }

        private static string DescribeMod(object mod)
        {
            if (mod is Mod typedMod)
                return $"{typedMod.manifest?.id ?? "<unknown>"} at {typedMod.path}";

            var id = TryReadManifestId(mod) ?? "<unknown>";
            var path = TryReadStringMember(mod, "path")
                ?? TryReadStringMember(mod, "Path")
                ?? "<unknown path>";
            return $"{id} at {path}";
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
                var type = target.GetType();
                if (type.GetField(name, AllInstance)?.GetValue(target) is string fieldValue)
                    return fieldValue;
                if (type.GetProperty(name, AllInstance)?.GetValue(target) is string propertyValue)
                    return propertyValue;
            }
            catch
            {
            }

            return null;
        }

        private IEnumerable<object> OrderedNewMods(ISet<object> newMods)
        {
            if (newMods == null || newMods.Count == 0)
                yield break;

            if (_modsField.GetValue(null) is not IEnumerable allMods)
                yield break;

            foreach (var mod in allMods)
            {
                if (mod != null && newMods.Contains(mod))
                    yield return mod;
            }
        }

        private int CountLoadedMods()
        {
            var allMods = _modsField.GetValue(null) as IEnumerable;
            if (allMods == null)
                return 0;

            var count = 0;
            foreach (var mod in allMods)
            {
                if (mod != null && IsLoaded(mod))
                    count++;
            }

            return count;
        }

        private static int Count(object list)
        {
            return list switch
            {
                ICollection collection => collection.Count,
                IEnumerable enumerable => CountEnumerable(enumerable),
                _ => 0,
            };
        }

        private static int CountEnumerable(IEnumerable enumerable)
        {
            var count = 0;
            foreach (var _ in enumerable)
                count++;
            return count;
        }
    }

    private sealed class RuntimeLoadWindow : IDisposable
    {
        private readonly FieldInfo _initializedField;
        private readonly PropertyInfo _stateProperty;
        private readonly object _previousInitialized;
        private readonly object _previousState;
        private readonly bool _changedInitialized;
        private readonly bool _changedState;

        internal RuntimeLoadWindow(FieldInfo initializedField, PropertyInfo stateProperty)
        {
            _initializedField = initializedField;
            _stateProperty = stateProperty;

            if (_initializedField != null)
            {
                _previousInitialized = _initializedField.GetValue(null);
                _initializedField.SetValue(null, false);
                _changedInitialized = true;
            }

            if (_stateProperty != null && TryResolveStateValue(_stateProperty.PropertyType, out var noneState))
            {
                _previousState = _stateProperty.GetValue(null);
                SetState(_stateProperty, noneState);
                _changedState = true;
            }
        }

        public void Dispose()
        {
            if (_changedState)
                SetState(_stateProperty, _previousState);

            if (_changedInitialized)
                _initializedField.SetValue(null, _previousInitialized);
        }

        private static bool TryResolveStateValue(Type stateType, out object noneState)
        {
            try
            {
                noneState = Enum.Parse(stateType, "None");
                return true;
            }
            catch
            {
                noneState = null;
                return false;
            }
        }

        private static void SetState(PropertyInfo stateProperty, object value)
        {
            var setter = stateProperty.GetSetMethod(true);
            if (setter != null)
            {
                setter.Invoke(null, new[] { value });
                return;
            }

            stateProperty.SetValue(null, value);
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
