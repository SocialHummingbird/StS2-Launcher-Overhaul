using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;
using STS2Mobile.Steam;
using STS2Mobile;

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

    internal Task WaitForLaunch()
    {
        _launchTcs ??= CreateLaunchSignal();
        return _launchTcs.Task;
    }

    internal LauncherLaunchHandoffResult Launch(LauncherLaunchReadiness readiness, LauncherModLaunchReadiness modReadiness, string launchSource, string attemptId, Func<LauncherLaunchAttemptTiming> timingSnapshot) => LaunchPrepared(safe: false, readiness, modReadiness, launchSource, attemptId, timingSnapshot);
    internal LauncherLaunchHandoffResult LaunchSafe(LauncherLaunchReadiness readiness, LauncherModLaunchReadiness modReadiness, string launchSource, string attemptId, Func<LauncherLaunchAttemptTiming> timingSnapshot) => LaunchPrepared(safe: true, readiness, modReadiness, launchSource, attemptId, timingSnapshot);
    private LauncherLaunchHandoffResult LaunchPrepared(bool safe, LauncherLaunchReadiness readiness, LauncherModLaunchReadiness modReadiness, string launchSource, string attemptId, Func<LauncherLaunchAttemptTiming> timingSnapshot)
    {
        var action = safe ? "safe" : "normal";
        LauncherLaunchMarkers.RecordPhase("launch model entered", action);
        if (!SelectedGameVersionReadyForLaunch(readiness, out var readinessProblem))
            return LauncherLaunchHandoffResult.Failed(LauncherLaunchAttemptPhases.BlockedInModel, readinessProblem, writePatchLog: true);
        SetSafeLaunchMarker(safe);
        timingSnapshot ??= LauncherLaunchAttemptTiming.NotMeasured;
        if (TrySignalInProcessLaunch(readiness, modReadiness, safe, launchSource, attemptId, timingSnapshot))
            return LauncherLaunchHandoffResult.Success(LauncherLaunchAttemptPhases.InProcessSignalled);
        LauncherLaunchMarkers.RecordPhase("launch restart requested", action);
        return RestartForLaunch(safe, readiness, modReadiness, launchSource, attemptId, timingSnapshot);
    }

    private static TaskCompletionSource<bool> CreateLaunchSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static void SetSafeLaunchMarker(bool safe)
    {
        if (safe)
            LauncherLaunchMarkers.SaveManualSafeLaunchMarker();
        else
            LauncherLaunchMarkers.ClearManualSafeLaunchMarker();
    }

    private bool TrySignalInProcessLaunch(LauncherLaunchReadiness readiness, LauncherModLaunchReadiness modReadiness, bool safe, string launchSource, string attemptId, Func<LauncherLaunchAttemptTiming> timingSnapshot)
    {
        if (_launchTcs == null)
            return false;
        var selectedBranch = readiness.Branch;
        if (!string.Equals(_processGameBranch, selectedBranch, System.StringComparison.OrdinalIgnoreCase))
        {
            LauncherLaunchMarkers.RecordPhase("launch requires restart", $"processBranch={_processGameBranch}; selectedBranch={selectedBranch}");
            PatchHelper.Log("[Launcher] Selected game branch changed from process-loaded " + $"{SteamGameBranch.DisplayName(_processGameBranch)} to {SteamGameBranch.DisplayName(selectedBranch)}; " + "restarting so Godot loads the selected version from disk.");
            return false;
        }

        if (readiness.RuntimeSlot?.RequiresProcessRestartForPreparedRuntime == true)
        {
            var slot = readiness.RuntimeSlot;
            var detail = $"branch={selectedBranch}; activeAndroidAssemblySha256={slot.ActiveAndroidAssemblySha256}; preparedAndroidAssemblySha256={slot.PreparedAndroidAssemblySha256}";
            LauncherLaunchMarkers.RecordPhase("launch requires restart", detail);
            PatchHelper.Log("[Launcher] Prepared Android game-code runtime does not match the assembly loaded by the current process; " + $"restarting so Godot loads runtime pack {slot.RuntimePack?.PackId ?? "<unknown>"}. " + $"active={slot.ActiveAndroidAssemblySha256} prepared={slot.PreparedAndroidAssemblySha256}");
            return false;
        }

        if (!_launchTcs.TrySetResult(true))
        {
            LauncherLaunchMarkers.RecordPhase("in-process launch signal failed", $"branch={selectedBranch}");
            LauncherLaunchMarkers.WriteLaunchAttempt(LauncherLaunchAttemptPhases.InProcessSignalFailed, safe ? "safe" : "normal", launchSource, attemptId, readiness, modReadiness, preparedReadinessUsed: true, timingSnapshot(), "Launch signal could not be delivered to current game process; restart fallback will be requested.");
            return false;
        }

        LauncherLaunchMarkers.RecordPhase("in-process launch signalled", $"branch={selectedBranch}");
        LauncherLaunchMarkers.WriteLaunchAttempt(LauncherLaunchAttemptPhases.InProcessSignalled, safe ? "safe" : "normal", launchSource, attemptId, readiness, modReadiness, preparedReadinessUsed: true, timingSnapshot(), "Launch signal delivered to current game process from prepared readiness");
        return true;
    }

    private static bool SelectedGameVersionReadyForLaunch(LauncherLaunchReadiness readiness, out string problem)
    {
        if (readiness == null)
        {
            problem = "Launch blocked: selected game version readiness was not prepared.";
            LauncherLaunchMarkers.RecordPhase("launch model blocked", problem);
            return false;
        }

        var authorizationProblem = string.Empty;
        if (readiness.Ready && readiness.HasCurrentLaunchAuthorization(out authorizationProblem))
        {
            problem = "";
            return true;
        }

        problem = readiness.Ready ? $"Launch blocked: {authorizationProblem}" : readiness.ReadinessProblem;
        LauncherLaunchMarkers.RecordPhase("launch model blocked", problem);
        return false;
    }

    private LauncherLaunchHandoffResult RestartForLaunch(bool safe, LauncherLaunchReadiness readiness, LauncherModLaunchReadiness modReadiness, string launchSource, string attemptId, Func<LauncherLaunchAttemptTiming> timingSnapshot)
    {
        PatchHelper.Log(RestartMessage(safe));
        if (readiness?.Ready != true)
        {
            return LauncherLaunchHandoffResult.Failed(LauncherLaunchAttemptPhases.RestartRequestedWithoutReadyFiles, "Launch blocked because selected files were not ready in final bridge check", writePatchLog: true);
        }

        var acceptance = AndroidGodotAppBridge.RequestLaunchRestart(LauncherRestartRequest.Create(attemptId, safe, readiness));
        if (!acceptance.Accepted)
            return LauncherLaunchHandoffResult.Failed(LauncherLaunchAttemptPhases.LaunchHandoffFailed, string.IsNullOrWhiteSpace(acceptance.Error) ? "Android rejected the restart request." : acceptance.Error, writePatchLog: true);
        WriteBridgeLaunchAttempt(LauncherLaunchAttemptPhases.RestartRequested, safe ? "safe" : "normal", launchSource, attemptId, readiness, modReadiness, timingSnapshot(), "Android accepted the durable restart request");
        AndroidGodotAppBridge.FinishLaunchRestart(attemptId);
        return LauncherLaunchHandoffResult.Success(LauncherLaunchAttemptPhases.RestartRequested);
    }

    private static string RestartMessage(bool safe) => safe ? "[Launcher] Restarting app for safe game launch" : "[Launcher] Restarting app to launch game files";
    private static void WriteBridgeLaunchAttempt(string phase, string action, string source, string attemptId, LauncherLaunchReadiness readiness, LauncherModLaunchReadiness modReadiness, LauncherLaunchAttemptTiming timing, string detail)
    {
        LauncherLaunchMarkers.WriteLaunchAttempt(phase, action, source, attemptId, readiness, modReadiness, preparedReadinessUsed: true, timing, detail);
    }
}
