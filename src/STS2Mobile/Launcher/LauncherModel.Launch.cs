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
                _launchTcs = new TaskCompletionSource<bool>();
        }
    }

    internal Task WaitForLaunch()
    {
        _launchTcs ??= new TaskCompletionSource<bool>();
        return _launchTcs.Task;
    }

    internal void Launch()
    {
        SaveLaunchCredentials();

        if (TrySignalInProcessLaunch())
            return;

        RestartForLaunch(safe: false);
    }

    internal void LaunchSafe()
    {
        LauncherLaunchMarkers.SaveManualSafeLaunchMarker();
        SaveLaunchCredentials();

        if (TrySafeAndroidRestart())
            return;

        if (TrySignalInProcessLaunch())
            return;

        RestartForLaunch(safe: true);
    }

    private bool PreserveLaunchConnection => _launchTcs != null;

    private void SaveLaunchCredentials()
    {
        LauncherCloudSaveState.SaveCredentials(_credentialStore);
    }

    private bool TrySignalInProcessLaunch()
    {
        if (_launchTcs == null)
            return false;

        _launchTcs.TrySetResult(true);
        return true;
    }
}
