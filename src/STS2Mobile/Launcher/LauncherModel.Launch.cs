using System.Threading.Tasks;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

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
        => "Start Game";

    internal Task WaitForLaunch()
    {
        _launchTcs ??= CreateLaunchSignal();
        return _launchTcs.Task;
    }

    internal void Launch()
    {
        LauncherLaunchMarkers.RecordPhase("launch model entered", "normal");
        if (!SelectedGameVersionReadyForLaunch())
            return;

        LauncherLaunchMarkers.ClearManualSafeLaunchMarker();
        LauncherLaunchMarkers.RecordPhase("launch credentials saving", "normal");
        SaveLaunchCredentials();

        if (TrySignalInProcessLaunch())
            return;

        LauncherLaunchMarkers.RecordPhase("launch restart requested", "normal");
        RestartForLaunch(safe: false);
    }

    internal void LaunchSafe()
    {
        LauncherLaunchMarkers.RecordPhase("launch model entered", "safe");
        if (!SelectedGameVersionReadyForLaunch())
            return;

        LauncherLaunchMarkers.SaveManualSafeLaunchMarker();
        LauncherLaunchMarkers.RecordPhase("launch credentials saving", "safe");
        SaveLaunchCredentials();

        if (TrySignalInProcessLaunch())
            return;

        if (TrySafeAndroidRestart())
            return;

        LauncherLaunchMarkers.RecordPhase("launch restart requested", "safe");
        RestartForLaunch(safe: true);
    }

    private bool PreserveLaunchConnection => _launchTcs != null;

    private static TaskCompletionSource<bool> CreateLaunchSignal()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private void SaveLaunchCredentials()
    {
        LauncherCloudSaveState.SaveCredentials(_credentialStore);
    }

    private bool TrySignalInProcessLaunch()
    {
        if (_launchTcs == null)
            return false;

        var selectedBranch = LauncherPreferences.ReadGameBranch();
        if (!string.Equals(_processGameBranch, selectedBranch, System.StringComparison.OrdinalIgnoreCase))
        {
            LauncherLaunchMarkers.RecordPhase(
                "launch requires restart",
                $"processBranch={_processGameBranch}; selectedBranch={selectedBranch}"
            );
            PatchHelper.Log(
                "[Launcher] Selected game branch changed from process-loaded "
                    + $"{SteamGameBranch.DisplayName(_processGameBranch)} to {SteamGameBranch.DisplayName(selectedBranch)}; "
                    + "restarting so Godot loads the selected version from disk."
            );
            return false;
        }

        LauncherLaunchMarkers.RecordPhase("in-process launch signalled", $"branch={selectedBranch}");
        _launchTcs.TrySetResult(true);
        return true;
    }

    private bool SelectedGameVersionReadyForLaunch()
    {
        var branch = LauncherPreferences.ReadGameBranch();
        if (LauncherGameFiles.DownloadedForValidation(_dataDir, branch))
            PatchCompatibilityValidator.ValidateSelectedVersion(_dataDir, branch);

        if (LauncherGameFiles.Ready(_dataDir))
            return true;

        var problem = LauncherGameFiles.ReadinessProblem(_dataDir, branch)
            ?? "Selected game version is not ready to launch.";
        LauncherLaunchMarkers.RecordPhase("launch model blocked", problem);
        PatchHelper.Log($"[Launcher] Launch blocked: {problem}");
        return false;
    }
}
