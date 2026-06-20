namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void InitializeActionPreferences()
    {
        RefreshGameBranchOptions();
        _view.SetActionPreferences(
            LauncherPreferences.LoadAndApplyActionPreferences()
        );
    }

    private void RefreshGameBranchOptions()
        => _view.SetGameBranchOptions(LauncherBranchCatalog.ReadSelectableBranches(_model.DataDir));

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
}
