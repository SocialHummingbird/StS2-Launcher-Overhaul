using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSteamSession
{
    private static string SessionFailure(
        string logContext,
        Exception ex,
        string? userPrefix = null
    )
    {
        PatchHelper.Log($"[Launcher] {logContext}: {ex.Message}");
        return userPrefix == null ? ex.Message : $"{userPrefix}: {ex.Message}";
    }
}
