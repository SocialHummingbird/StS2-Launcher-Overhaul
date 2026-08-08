using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;

namespace STS2Mobile.Patches;

// Some Steam branches can remove or rename run-history boss art while older saves
// still reference the old room icon suffix. Keep Android startup/run-history usable
// by falling back to a branch-local generic icon instead of loading public assets.
internal static class RunHistoryAssetPatches
{
    private const string ImageHelperTypeName = "MegaCrit.Sts2.Core.Helpers.ImageHelper";
    private const string IconFallback = "res://images/ui/run_history/unknown_monster.png";
    private const string OutlineFallback = "res://images/ui/run_history/unknown_monster_outline.png";
    private const string RunHistoryPrefix = "res://images/ui/run_history/";

    private static readonly HashSet<string> LoggedFallbacks = new();

    internal static void Apply(Harmony harmony)
    {
        var imageHelperType = typeof(MegaCrit.Sts2.Core.Nodes.NGame).Assembly.GetType(ImageHelperTypeName);
        if (imageHelperType == null)
            return;

        PatchHelper.Patch(
            harmony,
            imageHelperType,
            "GetRoomIconPath",
            postfix: PatchHelper.Method(typeof(RunHistoryAssetPatches), nameof(RoomIconPathPostfix))
        );

        PatchHelper.Patch(
            harmony,
            imageHelperType,
            "GetRoomIconOutlinePath",
            postfix: PatchHelper.Method(typeof(RunHistoryAssetPatches), nameof(RoomIconOutlinePathPostfix))
        );
    }

    private static void RoomIconPathPostfix(ref string __result)
    {
        __result = ExistingOrFallback(__result, IconFallback);
    }

    private static void RoomIconOutlinePathPostfix(ref string __result)
    {
        __result = ExistingOrFallback(__result, OutlineFallback);
    }

    private static string ExistingOrFallback(string path, string fallback)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !IsRunHistoryPath(path))
                return path;

            if (ResourceLoader.Exists(path))
                return path;

            if (!ResourceLoader.Exists(fallback))
                return path;

            LogFallbackOnce(path, fallback);
            return fallback;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[RunHistoryAssets] Fallback check failed for {path}: {ex.Message}");
            return path;
        }
    }

    private static bool IsRunHistoryPath(string path)
        => path.StartsWith(RunHistoryPrefix, StringComparison.Ordinal);

    private static void LogFallbackOnce(string path, string fallback)
    {
        var key = path + " -> " + fallback;
        lock (LoggedFallbacks)
        {
            if (!LoggedFallbacks.Add(key))
                return;
        }

        PatchHelper.Log($"[RunHistoryAssets] Missing run-history icon {path}; using branch-local fallback {fallback}");
    }
}
