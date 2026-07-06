using System;
using STS2Mobile;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private bool TrySafeAndroidRestart(
        LauncherLaunchReadiness readiness,
        LauncherModLaunchReadiness modReadiness,
        string launchSource,
        string attemptId,
        Func<LauncherLaunchAttemptTiming> timingSnapshot
    )
    {
        if (!OperatingSystem.IsAndroid() || readiness?.Ready != true)
            return false;

        PatchHelper.Log(RestartMessage(safe: true));
        WriteBridgeLaunchAttempt(
            LauncherLaunchAttemptPhases.SafeAndroidRestartRequested,
            "safe",
            launchSource,
            attemptId,
            readiness,
            modReadiness,
            timingSnapshot(),
            "Android bridge safe restart requested from prepared readiness"
        );
        AndroidGodotAppBridge.LaunchGameSafelyOnRestart();
        return true;
    }

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

        if (safe)
        {
            WriteBridgeLaunchAttempt(
                LauncherLaunchAttemptPhases.RestartRequested,
                "safe",
                launchSource,
                attemptId,
                readiness,
                modReadiness,
                timingSnapshot(),
                "Restarting app for safe game launch"
            );
            AndroidGodotAppBridge.LaunchGameSafelyOnRestart();
            return LauncherLaunchHandoffResult.Success(LauncherLaunchAttemptPhases.RestartRequested);
        }

        WriteBridgeLaunchAttempt(
            LauncherLaunchAttemptPhases.RestartRequested,
            "normal",
            launchSource,
            attemptId,
            readiness,
            modReadiness,
            timingSnapshot(),
            "Restarting app to launch selected game version"
        );
        AndroidGodotAppBridge.LaunchGameOnRestart();
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
