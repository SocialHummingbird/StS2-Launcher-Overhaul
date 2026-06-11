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

    private void GameBranchChanged(string branch)
    {
        var previous = LauncherPreferences.ReadGameBranch();
        if (string.Equals(previous, branch, System.StringComparison.OrdinalIgnoreCase))
            return;

        _view.ShowConfirmation(
            BranchSwitchConfirmationMessage(previous, branch),
            () => ApplyGameBranchChanged(previous, branch),
            () => _view.SetGameBranch(previous)
        );
    }

    private static string BranchSwitchConfirmationMessage(string previousBranch, string branch)
    {
        var previous = STS2Mobile.Steam.SteamGameBranch.DisplayName(previousBranch);
        var selected = STS2Mobile.Steam.SteamGameBranch.DisplayName(branch);
        var selectedNote = STS2Mobile.Steam.SteamGameBranch.SelectorInstallSlotHelpText(branch);
        var message =
            $"Switch game version from {previous} to {selected}?\n"
            + "This can require another download, and saves may not be compatible between Steam branches. "
            + "Local backup will be enabled before switching. "
            + "Steam Cloud Push will require backup storage permission after switching.\n\n"
            + selectedNote;

        return message;
    }

    private void ApplyGameBranchChanged(string previousBranch, string branch)
    {
        LauncherPreferences.SaveGameBranch(branch);
        LauncherPreferences.SaveLocalBackupEnabled(true);
        LauncherBranchSwitchSafety.WriteMarker(_model.DataDir, previousBranch, branch);
        _view.SetActionPreferences(LauncherPreferences.ReadActionPreferences());
        _view.AppendLog($"Game version set to {STS2Mobile.Steam.SteamGameBranch.DisplayName(branch)}. Local backup enabled for branch switching.");
        _view.AppendLog(STS2Mobile.Steam.SteamGameBranch.SelectorInstallSlotHelpText(branch));

        if (LauncherGameFiles.Ready())
        {
            ShowReadyToLaunch(SelectedVersionReadyStatus(), LaunchUpdateAction.Visible);
            return;
        }

        var readinessProblem = LauncherGameFiles.ReadinessProblem(_model.DataDir, branch);
        _view.SetStatus(readinessProblem
            ?? "Selected game version is not downloaded. Download game files to continue.");
        _view.HideActions();
        if (LauncherGameFiles.HasBranchMetadataProblem(_model.DataDir, branch))
            ShowRedownloadSelectedVersionAction();
        else
            ShowDownloadReadyAction();
    }

    private string SelectedVersionReadyStatus()
        => SelectedVersionReadyStatus(_model.LoggedInStatus());

    private string SelectedVersionReadyStatus(string baseStatus)
    {
        var branch = LauncherPreferences.ReadGameBranch();
        return $"{baseStatus} Selected game version: {STS2Mobile.Steam.SteamGameBranch.DisplayName(branch)}. Active install slot: {STS2Mobile.Steam.SteamGameInstallPaths.VersionSlotKind(branch)}.";
    }

    private string SelectedVersionDownloadRequiredStatus()
    {
        var branch = LauncherPreferences.ReadGameBranch();
        return $"{_model.LoggedInStatus()} Download selected game version: {STS2Mobile.Steam.SteamGameBranch.DisplayName(branch)}. Active install slot: {STS2Mobile.Steam.SteamGameInstallPaths.VersionSlotKind(branch)}.";
    }

    private void SafeLaunchPressed()
    {
        _view.AppendLog(
            "Safe launch requested: default renderer, no shader warmup, local saves only for one run."
        );
        _model.LaunchSafe();
    }
}
