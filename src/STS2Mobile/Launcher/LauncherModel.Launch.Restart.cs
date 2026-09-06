using System;
using STS2Mobile;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private LauncherLaunchHandoffResult RestartForLaunch(
        bool safe,
        LauncherLaunchReadiness readiness,
        LauncherModLaunchReadiness modReadiness,
        string launchSource,
        string attemptId,
        Func<LauncherLaunchAttemptTiming> timingSnapshot
    )
    {
        PatchHelper.Log(RestartMessage(safe));

        if (readiness?.Ready != true)
        {
            return LauncherLaunchHandoffResult.Failed(
                LauncherLaunchAttemptPhases.RestartRequestedWithoutReadyFiles,
                "Launch blocked because selected files were not ready in final bridge check",
                writePatchLog: true
            );
        }

        var acceptance = AndroidGodotAppBridge.RequestLaunchRestart(
            LauncherRestartRequest.Create(attemptId, safe, readiness));
        if (!acceptance.Accepted)
            return LauncherLaunchHandoffResult.Failed(
                LauncherLaunchAttemptPhases.LaunchHandoffFailed,
                string.IsNullOrWhiteSpace(acceptance.Error) ? "Android rejected the restart request." : acceptance.Error,
                writePatchLog: true);

        WriteBridgeLaunchAttempt(
            LauncherLaunchAttemptPhases.RestartRequested,
            safe ? "safe" : "normal", launchSource, attemptId, readiness, modReadiness,
            timingSnapshot(), "Android accepted the durable restart request");
        AndroidGodotAppBridge.FinishLaunchRestart(attemptId);
        return LauncherLaunchHandoffResult.Success(LauncherLaunchAttemptPhases.RestartRequested);
    }

    private static string RestartMessage(bool safe)
        => safe
            ? "[Launcher] Restarting app for safe game launch"
            : "[Launcher] Restarting app to launch game files";

    private static void WriteBridgeLaunchAttempt(
        string phase,
        string action,
        string source,
        string attemptId,
        LauncherLaunchReadiness readiness,
        LauncherModLaunchReadiness modReadiness,
        LauncherLaunchAttemptTiming timing,
        string detail
    )
    {
        LauncherLaunchMarkers.WriteLaunchAttempt(
            phase,
            action,
            source,
            attemptId,
            readiness,
            modReadiness,
            preparedReadinessUsed: true,
            timing,
            detail
        );
    }
}
