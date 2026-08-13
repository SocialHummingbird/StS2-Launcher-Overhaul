namespace STS2Mobile.Launcher;

internal sealed class LauncherBranchSwitchCoordinator
{
    private readonly LauncherModel _model;
    private readonly LauncherView _view;
    private readonly LauncherVersionCoordinator _versions;
    private readonly LauncherLaunchCoordinator _launch;
    private readonly LauncherDownloadCoordinator _downloads;

    internal LauncherBranchSwitchCoordinator(
        LauncherModel model,
        LauncherView view,
        LauncherVersionCoordinator versions,
        LauncherLaunchCoordinator launch,
        LauncherDownloadCoordinator downloads
    )
    {
        _model = model;
        _view = view;
        _versions = versions;
        _launch = launch;
        _downloads = downloads;
    }

    internal void GameBranchChanged(string branch)
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
        var visibleBranches = LauncherBranchCatalog.ReadVisibleBranches(_model.DataDir);
        var availableBranches = LauncherBranchCatalog.ReadSelectableBranches(
            _model.DataDir,
            visibleBranches
        );
        var selectedStatus = LauncherBranchCatalog.SelectedOptionStatus(branch, availableBranches);
        var selectedProblem = LauncherBranchCatalog.SelectedOptionDownloadProblem(
            branch,
            visibleBranches
        );
        var message =
            $"Switch game version from {previous} to {selected}?\n"
            + "This can require another download, and saves may not be compatible between Steam branches.\n\n"
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
        LauncherLaunchReadinessCache.Clear($"branch changed from {previousBranch} to {branch}");
        LauncherBranchAvailabilityStatus.Clear(_model.DataDir);
        var branches = _versions.ReadGameBranchOptions();
        _view.SetActionPreferences(LauncherPreferences.ReadActionPreferences(branch), branches);
        _view.AppendLog($"Game version set to {STS2Mobile.Steam.SteamGameBranch.DisplayName(branch)}.");
        _view.AppendLog(STS2Mobile.Steam.SteamGameBranch.SelectorInstallSlotHelpText(branch));

        var readiness = _launch.RefreshSelectedDownloadedStateEvidence(
            branch,
            "branch switch downloaded-state readiness"
        );
        if (readiness.Ready)
        {
            _launch.ShowReadyToLaunch(
                _launch.SelectedVersionReadyStatus(readiness),
                LaunchUpdateAction.Visible,
                LauncherStatusSeverity.Ready
            );
            return;
        }

        var readinessProblem = readiness.ReadinessProblem;
        _view.SetStatus(
            readinessProblem
                ?? "Selected game version is not downloaded. Download game files to continue.",
            LauncherStatusSeverity.Warning
        );
        _view.HideActions();
        if (LauncherGameFiles.HasBranchMetadataProblem(_model.DataDir, branch))
            _downloads.ShowRedownloadSelectedVersionAction();
        else
            _downloads.ShowDownloadReadyAction();
    }
}
