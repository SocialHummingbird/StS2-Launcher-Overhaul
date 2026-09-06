using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private bool _inGameMode;
    private TaskCompletionSource<bool> _launchTcs;

    // True when launched from GameStartupWrapper (game files present). False in
    // standalone launcher mode where a restart is needed after downloading files.
    // Setting this to true eagerly creates the launch TCS so it exists before the
    // UI is shown (preventing a race between PLAY button and WaitForLaunch).
    internal bool InGameMode
    {
        get => _inGameMode;
        set
        {
            _inGameMode = value;
            if (value && _launchTcs == null)
                _launchTcs = CreateLaunchSignal();
        }
    }

    internal string LaunchButtonText()
        => "Play";

    internal Task WaitForLaunch()
    {
        _launchTcs ??= CreateLaunchSignal();
        return _launchTcs.Task;
    }

    internal LauncherLaunchHandoffResult Launch(
        LauncherLaunchReadiness readiness,
        LauncherModLaunchReadiness modReadiness,
        string launchSource,
        string attemptId,
        Func<LauncherLaunchAttemptTiming> timingSnapshot
    )
        => LaunchPrepared(
            safe: false,
            readiness,
            modReadiness,
            launchSource,
            attemptId,
            timingSnapshot
        );

    internal LauncherLaunchHandoffResult LaunchSafe(
        LauncherLaunchReadiness readiness,
        LauncherModLaunchReadiness modReadiness,
        string launchSource,
        string attemptId,
        Func<LauncherLaunchAttemptTiming> timingSnapshot
    )
        => LaunchPrepared(
            safe: true,
            readiness,
            modReadiness,
            launchSource,
            attemptId,
            timingSnapshot
        );

    private LauncherLaunchHandoffResult LaunchPrepared(
        bool safe,
        LauncherLaunchReadiness readiness,
        LauncherModLaunchReadiness modReadiness,
        string launchSource,
        string attemptId,
        Func<LauncherLaunchAttemptTiming> timingSnapshot
    )
    {
        var action = safe ? "safe" : "normal";
        LauncherLaunchMarkers.RecordPhase("launch model entered", action);
        if (!SelectedGameVersionReadyForLaunch(readiness, out var readinessProblem))
            return LauncherLaunchHandoffResult.Failed(
                LauncherLaunchAttemptPhases.BlockedInModel,
                readinessProblem,
                writePatchLog: true
            );

        SetSafeLaunchMarker(safe);

        timingSnapshot ??= LauncherLaunchAttemptTiming.NotMeasured;

        if (TrySignalInProcessLaunch(readiness, modReadiness, safe, launchSource, attemptId, timingSnapshot))
            return LauncherLaunchHandoffResult.Success(LauncherLaunchAttemptPhases.InProcessSignalled);

        LauncherLaunchMarkers.RecordPhase("launch restart requested", action);
        return RestartForLaunch(safe, readiness, modReadiness, launchSource, attemptId, timingSnapshot);
    }

    private static TaskCompletionSource<bool> CreateLaunchSignal()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void SetSafeLaunchMarker(bool safe)
    {
        if (safe)
            LauncherLaunchMarkers.SaveManualSafeLaunchMarker();
        else
            LauncherLaunchMarkers.ClearManualSafeLaunchMarker();
    }
}
