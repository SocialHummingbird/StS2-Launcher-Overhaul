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
        var authFailure = SteamAuthFailureReport.From(ex);
        LauncherLaunchMarkers.RecordSteamAuthFailure(authFailure, logContext);
        PatchHelper.Log($"[Launcher] {logContext}: {ex}");
        var message = authFailure.UserMessage;
        return userPrefix == null ? message : $"{userPrefix}: {message}";
    }
}
