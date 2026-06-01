namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void InitializeActionPreferences()
    {
        _view.SetActionPreferences(
            LauncherPreferences.LoadAndApplyLocalBackupEnabled(),
            LauncherPreferences.LoadAndApplyCloudSyncEnabled()
        );
    }

    private void StartSessionFlow()
    {
        var result = _model.StartSession();
        HandleFastPath(result);
        StartLocalLoginHandoff();
    }

    private void LocalBackupToggled(bool pressed)
        => LauncherPreferences.SaveLocalBackupEnabled(pressed);

    private void RetryPressed()
    {
        var result = _model.Retry();
        HandleFastPath(result);
    }

    private void LaunchPressed()
        => _model.Launch();

    private void SafeLaunchPressed()
    {
        _view.AppendLog(
            "Safe launch requested: default renderer, no shader warmup, local saves only for one run."
        );
        _model.LaunchSafe();
    }
}
