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
internal static class ModLoaderPatches
{
    private static readonly BindingFlags AllStatic =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags LoadedMarkerFlags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    internal static void Apply(Harmony harmony)
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
    private static void InitializePostfix()
    {
        try
        {
            using var dirAccess = DirAccess.Open(AppPaths.ExternalModsDir);
            if (dirAccess == null)
            {
                PatchHelper.Log(
                    $"[Mods] External mods directory not found: {AppPaths.ExternalModsDir} (error: {DirAccess.GetOpenError()})"
                );
                return;
            }

            PatchHelper.Log($"[Mods] Scanning external mods: {AppPaths.ExternalModsDir}");

            if (!TryLoadModManagerAccess(
                    out var initializedField,
                    out var loadModsInDirRecursive,
                    out var modsField,
                    out var loadedModsField
                ))
            {
                return;
            }

            LoadExternalMods(dirAccess, initializedField, loadModsInDirRecursive);
            RebuildLoadedModsCache(modsField, loadedModsField);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] Failed to load external mods: {ex}");
        }
    }

    private static bool TryLoadModManagerAccess(
        out FieldInfo initializedField,
        out MethodInfo loadModsInDirRecursive,
        out FieldInfo modsField,
        out FieldInfo loadedModsField
    )
    {
        var modManagerType = typeof(ModManager);

        initializedField = modManagerType.GetField("_initialized", AllStatic);
        if (initializedField == null)
        {
            PatchHelper.Log("[Mods] Failed to locate ModManager._initialized");
            loadModsInDirRecursive = null;
            modsField = null;
            loadedModsField = null;
            return false;
        }

        loadModsInDirRecursive = modManagerType.GetMethod("LoadModsInDirRecursive", AllStatic);
        if (loadModsInDirRecursive == null)
        {
            PatchHelper.Log("[Mods] Failed to locate ModManager.LoadModsInDirRecursive");
            modsField = null;
            loadedModsField = null;
            return false;
        }

        modsField = modManagerType.GetField("_mods", AllStatic);
        loadedModsField = modManagerType.GetField("_loadedMods", AllStatic);
        if (modsField == null || loadedModsField == null)
        {
            PatchHelper.Log("[Mods] Failed to locate ModManager mod caches (_mods or _loadedMods)");
            return false;
        }

        return true;
    }

    private static void LoadExternalMods(
        DirAccess dirAccess,
        FieldInfo initializedField,
        MethodInfo loadModsInDirRecursive
    )
    {
        var previousInitialized = initializedField.GetValue(null);
        initializedField.SetValue(null, false);
        try
        {
            loadModsInDirRecursive.Invoke(null, new object[] { dirAccess, ModSource.ModsDirectory });
        }
        finally
        {
            initializedField.SetValue(null, previousInitialized);
        }
    }

    private static void RebuildLoadedModsCache(FieldInfo modsField, FieldInfo loadedModsField)
    {
        var allMods = modsField.GetValue(null) as IEnumerable;
        if (allMods == null)
        {
            PatchHelper.Log("[Mods] ModManager._mods was null; skipping loaded-mod rebuild");
            return;
        }

        // Build a list using reflection only, so this keeps working if ModManager internals
        // rename wasLoaded or change the Mod type representation again.
        var loadedMods = CreateLoadedModsList(allMods, loadedModsField.FieldType);
        if (loadedMods == null)
        {
            PatchHelper.Log("[Mods] Failed to rebuild _loadedMods via reflection");
            return;
        }

        loadedModsField.SetValue(null, loadedMods);
        PatchHelper.Log(
            $"[Mods] External scan complete. Total loaded: {(loadedMods as ICollection)?.Count ?? 0}"
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
        yield return modType.GetProperty("wasLoaded", LoadedMarkerFlags);
        yield return modType.GetProperty("WasLoaded", LoadedMarkerFlags);
        yield return modType.GetProperty("isLoaded", LoadedMarkerFlags);
        yield return modType.GetProperty("IsLoaded", LoadedMarkerFlags);
    }
}
