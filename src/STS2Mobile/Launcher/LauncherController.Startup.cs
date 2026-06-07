namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void InitializeActionPreferences()
        => _view.SetActionPreferences(
            LauncherPreferences.LoadAndApplyActionPreferences()
        );

    private void StartSessionFlow()
    {
        var result = _model.StartSession();
        HandleSessionFlow(result);
    }

    private void HandleSessionFlow(LauncherModel.FastPathResult result)
    {
        if (
            result != LauncherModel.FastPathResult.ReadyToLaunch
            && TryStartImmediateLocalLoginHandoff()
        )
            return;

        HandleFastPath(result);
        if (result != LauncherModel.FastPathResult.ReadyToLaunch)
            StartLocalLoginHandoff();
    }

    private void LocalBackupToggled(bool pressed)
        => LauncherPreferences.SaveLocalBackupEnabled(pressed);

    private void RetryPressed()
    {
        var result = _model.Retry();
        HandleSessionFlow(result);
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
