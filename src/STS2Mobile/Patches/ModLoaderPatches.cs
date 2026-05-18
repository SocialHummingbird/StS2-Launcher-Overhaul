using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace STS2Mobile.Patches;

// Extends ModManager to scan an external mods directory on Android so users
// can sideload mods to /storage/emulated/0/StS2Launcher/Mods/ without root.
public static class ModLoaderPatches
{
    private static readonly BindingFlags AllStatic =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    public static void Apply(Harmony harmony)
    {
        PatchHelper.Patch(
            harmony,
            typeof(ModManager),
            "Initialize",
            postfix: PatchHelper.Method(typeof(ModLoaderPatches), nameof(InitializePostfix))
        );
    }

    // Runs after the original Initialize() to pick up mods from external storage.
    // Temporarily clears _initialized so TryLoadModFromPck accepts new entries.
    public static void InitializePostfix()
    {
        var modManagerType = typeof(ModManager);

        try
        {
            using var dirAccess = DirAccess.Open(AppPaths.ExternalModsDir);
            if (dirAccess == null)
            {
                PatchHelper.Log(
                    $"[Mods] External mods directory not found: {AppPaths.ExternalModsDir} "
                        + $"(error: {DirAccess.GetOpenError()})"
                );
                return;
            }

            PatchHelper.Log($"[Mods] Scanning external mods: {AppPaths.ExternalModsDir}");

            var initializedField = modManagerType.GetField("_initialized", AllStatic);
            if (initializedField == null)
            {
                PatchHelper.Log("[Mods] Failed to locate ModManager._initialized");
                return;
            }

            var loadMethod = modManagerType.GetMethod("LoadModsInDirRecursive", AllStatic);
            if (loadMethod == null)
            {
                PatchHelper.Log("[Mods] Failed to locate ModManager.LoadModsInDirRecursive");
                return;
            }

            var modsField = modManagerType.GetField("_mods", AllStatic);
            var loadedModsField = modManagerType.GetField("_loadedMods", AllStatic);
            if (modsField == null || loadedModsField == null)
            {
                PatchHelper.Log("[Mods] Failed to locate ModManager mod caches (_mods or _loadedMods)");
                return;
            }

            var previousInitialized = initializedField.GetValue(null);
            initializedField.SetValue(null, false);
            try
            {
                loadMethod.Invoke(null, new object[] { dirAccess, ModSource.ModsDirectory });
            }
            finally
            {
                initializedField.SetValue(null, previousInitialized);
            }

            // Rebuild _loadedMods to include anything new
            var allMods = modsField.GetValue(null) as IEnumerable;
            if (allMods != null)
            {
                // Build a list using reflection only, so this keeps working if ModManager internals
                // rename wasLoaded or change the Mod type representation again.
                var loadedMods = CreateLoadedModList(allMods, loadedModsField.FieldType);
                if (loadedMods != null)
                {
                    loadedModsField.SetValue(null, loadedMods);
                    PatchHelper.Log(
                        $"[Mods] External scan complete. Total loaded: {(loadedMods as System.Collections.ICollection)?.Count ?? 0}"
                    );
                }
                else
                    PatchHelper.Log("[Mods] Failed to rebuild _loadedMods via reflection");
            }
            else
            {
                PatchHelper.Log("[Mods] ModManager._mods was null; skipping loaded-mod rebuild");
            }
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] Failed to load external mods: {ex}");
        }
    }

    private static object CreateLoadedModList(IEnumerable allMods, Type targetListType)
    {
        try
        {
            if (!typeof(System.Collections.IEnumerable).IsAssignableFrom(targetListType))
                return null;

            if (Activator.CreateInstance(targetListType) is not System.Collections.IList targetList)
            {
                PatchHelper.Log("[Mods] _loadedMods field is not a concrete IList");
                return null;
            }

            foreach (var mod in allMods)
            {
                if (mod == null || !IsLoadedMod(mod))
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

    private static bool IsLoadedMod(object mod)
    {
        if (mod == null)
            return false;

        var modType = mod.GetType();
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var candidates = new List<MemberInfo>
        {
            modType.GetField("wasLoaded", flags),
            modType.GetField("WasLoaded", flags),
            modType.GetField("isLoaded", flags),
            modType.GetField("IsLoaded", flags),
            modType.GetProperty("wasLoaded", flags),
            modType.GetProperty("WasLoaded", flags),
            modType.GetProperty("isLoaded", flags),
            modType.GetProperty("IsLoaded", flags),
        };

        foreach (var member in candidates)
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
        }

        // Without a reliable marker, assume loaded to avoid dropping mods.
        return true;
    }
}
