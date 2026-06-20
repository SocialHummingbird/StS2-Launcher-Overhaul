namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void GameBranchChanged(string branch)
    {
        var previous = LauncherPreferences.ReadGameBranch();
        if (string.Equals(previous, branch, System.StringComparison.OrdinalIgnoreCase))
            return;

        _view.ShowConfirmation(
            BranchSwitchConfirmationMessage(previous, branch),
            () => ApplyGameBranchChanged(previous, branch),
            () => _view.SetGameBranch(previous),
            "Switch Version",
            "Keep Current"
        );
    }

    private string BranchSwitchConfirmationMessage(string previousBranch, string branch)
    {
        var previous = STS2Mobile.Steam.SteamGameBranch.DisplayName(previousBranch);
        var selected = STS2Mobile.Steam.SteamGameBranch.DisplayName(branch);
        var selectedNote = STS2Mobile.Steam.SteamGameBranch.SelectorInstallSlotHelpText(branch);
        var availableBranches = LauncherBranchCatalog.ReadSelectableBranches(_model.DataDir);
        var selectedStatus = LauncherBranchCatalog.SelectedOptionStatus(branch, availableBranches);
        var selectedProblem = LauncherBranchCatalog.SelectedOptionDownloadProblem(
            branch,
            LauncherBranchCatalog.ReadVisibleBranches(_model.DataDir)
        );
        var message =
            $"Switch game version from {previous} to {selected}?\n"
            + "This can require another download, and saves may not be compatible between Steam branches. "
            + "Local backup will be enabled before switching. "
            + "Steam Cloud Push will require backup storage permission after switching.\n\n"
            + selectedNote
            + "\n"
            + selectedStatus;

        if (!string.IsNullOrWhiteSpace(selectedProblem))
            message += "\n\n" + selectedProblem;

        return message;
    }

    private void ApplyGameBranchChanged(string previousBranch, string branch)
    {
        LauncherPreferences.SaveGameBranch(branch);
        LauncherPreferences.SaveLocalBackupEnabled(true);
        LauncherBranchAvailabilityStatus.Clear(_model.DataDir);
        RefreshGameBranchOptions();
        LauncherBranchSwitchSafety.WriteMarker(_model.DataDir, previousBranch, branch);
        _view.SetActionPreferences(LauncherPreferences.ReadActionPreferences());
        _view.AppendLog($"Game version set to {STS2Mobile.Steam.SteamGameBranch.DisplayName(branch)}. Local backup enabled for branch switching.");
        _view.AppendLog(STS2Mobile.Steam.SteamGameBranch.SelectorInstallSlotHelpText(branch));

        if (RefreshSelectedRuntimeAndCheckReady())
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
}
