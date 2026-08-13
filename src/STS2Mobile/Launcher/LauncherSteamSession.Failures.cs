using System;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSteamSession
{
    private static string SessionFailure(
        string logContext,
        Exception ex,
        string? userPrefix = null
    )
    {
        SaveSyncService.ReportFailure(SaveSyncService.FailureKindFor(ex));
        var authFailure = SteamAuthFailureReport.From(ex);
        LauncherLaunchMarkers.RecordSteamAuthFailure(authFailure, logContext);
        PatchHelper.Log($"[Launcher] {logContext}: {ex}");
        var message = authFailure.UserMessage;
        return userPrefix == null ? message : $"{userPrefix}: {message}";
    }
}
