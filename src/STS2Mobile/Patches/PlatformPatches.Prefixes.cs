using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Patches;

internal static partial class PlatformPatches
{
    private static bool InitializePlatformPrefix(ref Task<bool> __result)
    {
        PatchHelper.Log("Skipping Steam initialization (mobile)");
        __result = Task.FromResult(true);
        return false;
    }

    private static bool SkipPrefix() => false;

    private static bool ReturnFalsePrefix(ref bool __result)
    {
        __result = false;
        return false;
    }

    // Skip paths that aren't valid Godot absolute paths (must contain "://").
    private static bool CreateDirectoryPrefix(GodotFileIo __instance, string directoryPath)
    {
        var fullPath = __instance.GetFullPath(directoryPath);
        if (!fullPath.Contains(GodotAbsolutePathMarker))
            return false;
        return true;
    }

    private static bool ReturnEmptyStringPrefix(ref string __result)
    {
        __result = string.Empty;
        return false;
    }

    private static bool GetRawLanguagePrefix(ref string __result)
    {
        __result = Godot.OS.GetLocale();
        return false;
    }

    private static bool GetSupportedWindowModePrefix(ref SupportedWindowMode __result)
    {
        __result = SupportedWindowMode.FullscreenOnly;
        return false;
    }

    private static bool GetPlayerNamePrefix(ref string __result)
    {
        __result = PlayerNameFallback;
        return false;
    }

    private static bool GetLocalPlayerIdPrefix(ref ulong __result)
    {
        __result = LocalPlayerIdFallback;
        return false;
    }

    private static bool GetFriendsWithOpenLobbiesPrefix(ref Task<IEnumerable<ulong>> __result)
    {
        __result = Task.FromResult<IEnumerable<ulong>>(Array.Empty<ulong>());
        return false;
    }

    // Keep locale resolution resilient across unusual BCP-47 tags and malformed data.
    private static bool GetThreeLetterLanguageCodePrefix(ref string __result)
    {
        __result = ResolveThreeLetterLanguageCode();
        return false;
    }
}
