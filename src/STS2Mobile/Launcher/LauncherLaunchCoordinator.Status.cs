namespace STS2Mobile.Launcher;

internal sealed partial class LauncherLaunchCoordinator
{
    internal void ShowLoggedIn(System.Action showDownloadReadyAction)
    {
        _model.MarkConnectionResolved();
        var readiness = RefreshSelectedDownloadedStateEvidence("login downloaded-state readiness");
        if (readiness.Ready)
        {
            ShowReadyToLaunch(SelectedVersionReadyStatus(readiness), LaunchUpdateAction.Visible);
            return;
        }

        var readinessProblem = readiness.ReadinessProblem;
        _view.SetStatus(readinessProblem ?? SelectedVersionDownloadRequiredStatus(readiness));
        showDownloadReadyAction();
    }

    internal void ShowReadyToLaunch(string status, LaunchUpdateAction updateAction)
    {
        _view.SetStatus(status);
        _diagnostics.ShowPreviousLaunchWarningIfNeeded();
        ShowLaunchActions(updateAction);
    }

    internal void ShowLaunchActions(LaunchUpdateAction updateAction)
    {
        _view.ShowLaunchActions(
            _model.LaunchButtonText(),
            updateAction == LaunchUpdateAction.Visible
        );
    }

    internal string SelectedVersionReadyStatus(LauncherLaunchReadiness readiness)
    {
        return SelectedVersionReadyStatus(_model.LoggedInStatus(), readiness);
    }

    internal string SelectedVersionReadyStatus(string baseStatus, LauncherLaunchReadiness readiness)
    {
        var branch = readiness?.Branch ?? LauncherPreferences.ReadGameBranch();
        var cacheStatus = readiness?.CacheStatus ?? "not checked";
        var runtimeStatus = readiness?.HasRuntimeSlot == true
            ? "Runtime pairing is verified."
            : "Downloaded files are present; final runtime pairing check runs when Start Game is pressed.";
        return $"{baseStatus} Selected game version: {STS2Mobile.Steam.SteamGameBranch.DisplayName(branch)}. Active install slot: {STS2Mobile.Steam.SteamGameInstallPaths.VersionSlotKind(branch)}. {runtimeStatus} Readiness check: {cacheStatus}.";
    }

    internal LauncherLaunchReadiness RefreshSelectedRuntimeSlotEvidence()
    {
        var branch = LauncherPreferences.ReadGameBranch();
        return RefreshSelectedRuntimeSlotEvidence(branch);
    }

    internal LauncherLaunchReadiness RefreshSelectedRuntimeSlotEvidence(string branch)
    {
        return LauncherLaunchReadiness.Evaluate(
            _model.DataDir,
            branch,
            "runtime evidence refresh"
        );
    }

    internal LauncherLaunchReadiness RefreshSelectedDownloadedStateEvidence(string phase)
    {
        var branch = LauncherPreferences.ReadGameBranch();
        return RefreshSelectedDownloadedStateEvidence(branch, phase);
    }

    internal LauncherLaunchReadiness RefreshSelectedDownloadedStateEvidence(
        string branch,
        string phase
    )
    {
        return LauncherLaunchReadiness.EvaluateDownloadedState(
            _model.DataDir,
            branch,
            phase
        );
    }

    private string SelectedVersionDownloadRequiredStatus(LauncherLaunchReadiness readiness)
    {
        var branch = readiness?.Branch ?? LauncherPreferences.ReadGameBranch();
        return $"{_model.LoggedInStatus()} Download selected game version: {STS2Mobile.Steam.SteamGameBranch.DisplayName(branch)}. Active install slot: {STS2Mobile.Steam.SteamGameInstallPaths.VersionSlotKind(branch)}.";
    }
}
