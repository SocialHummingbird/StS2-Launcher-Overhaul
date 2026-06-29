namespace STS2Mobile.Launcher;

internal sealed partial class LauncherLaunchCoordinator
{
    private readonly LauncherModel _model;
    private readonly LauncherView _view;
    private readonly LauncherDiagnosticsCoordinator _diagnostics;

    internal LauncherLaunchCoordinator(
        LauncherModel model,
        LauncherView view,
        LauncherDiagnosticsCoordinator diagnostics
    )
    {
        _model = model;
        _view = view;
        _diagnostics = diagnostics;
    }

    internal void LaunchPressed()
    {
        LauncherLaunchMarkers.RecordPhase(
            "launch button pressed",
            $"branch={LauncherPreferences.ReadGameBranch()}"
        );
        RefreshSelectedRuntimeSlotEvidence();

        var branch = LauncherPreferences.ReadGameBranch();
        if (!LauncherGameFiles.Ready(_model.DataDir, branch))
        {
            var problem = LauncherGameFiles.ReadinessProblem(_model.DataDir, branch)
                ?? "Selected game version is not ready to launch.";
            LauncherLaunchMarkers.RecordPhase("launch blocked", problem);
            _view.SetStatus(problem);
            _view.AppendLog(problem);
            return;
        }

        LauncherLaunchMarkers.RecordPhase("launch readiness passed", $"branch={branch}");
        _model.Launch();
    }

    internal void SafeLaunchPressed()
    {
        LauncherLaunchMarkers.RecordPhase(
            "safe launch button pressed",
            $"branch={LauncherPreferences.ReadGameBranch()}"
        );
        RefreshSelectedRuntimeSlotEvidence();

        var branch = LauncherPreferences.ReadGameBranch();
        if (!LauncherGameFiles.Ready(_model.DataDir, branch))
        {
            var problem = LauncherGameFiles.ReadinessProblem(_model.DataDir, branch)
                ?? "Selected game version is not ready to safe launch.";
            LauncherLaunchMarkers.RecordPhase("safe launch blocked", problem);
            _view.SetStatus(problem);
            _view.AppendLog(problem);
            return;
        }

        LauncherLaunchMarkers.RecordPhase("safe launch readiness passed", $"branch={branch}");
        _view.AppendLog(
            "Safe launch requested: default renderer, no shader warmup, local saves only for one run."
        );
        _model.LaunchSafe();
    }

    internal void ShowLoggedIn(System.Action showDownloadReadyAction)
    {
        _model.MarkConnectionResolved();
        if (RefreshSelectedRuntimeAndCheckReady())
        {
            ShowReadyToLaunch(SelectedVersionReadyStatus(), LaunchUpdateAction.Visible);
            return;
        }

        var branch = LauncherPreferences.ReadGameBranch();
        var readinessProblem = LauncherGameFiles.ReadinessProblem(_model.DataDir, branch);
        _view.SetStatus(readinessProblem ?? SelectedVersionDownloadRequiredStatus());
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

    internal bool RefreshSelectedRuntimeAndCheckReady()
    {
        RefreshSelectedRuntimeSlotEvidence();
        return LauncherGameFiles.Ready(_model.DataDir, LauncherPreferences.ReadGameBranch());
    }

    internal string SelectedVersionReadyStatus()
    {
        RefreshSelectedRuntimeSlotEvidence();
        return SelectedVersionReadyStatus(_model.LoggedInStatus());
    }

    internal string SelectedVersionReadyStatus(string baseStatus)
    {
        var branch = LauncherPreferences.ReadGameBranch();
        return $"{baseStatus} Selected game version: {STS2Mobile.Steam.SteamGameBranch.DisplayName(branch)}. Active install slot: {STS2Mobile.Steam.SteamGameInstallPaths.VersionSlotKind(branch)}. Runtime pairing is verified when launching.";
    }

    internal void RefreshSelectedRuntimeSlotEvidence()
    {
        var branch = LauncherPreferences.ReadGameBranch();
        if (LauncherGameFiles.DownloadedForValidation(_model.DataDir, branch))
            PatchCompatibilityValidator.ValidateSelectedVersion(_model.DataDir, branch);

        var filesReady = LauncherGameFiles.Ready(_model.DataDir, branch);
        var readinessProblem = LauncherGameFiles.ReadinessProblem(_model.DataDir, branch);
        LauncherRuntimeSlotEvidence.Write(_model.DataDir, branch, filesReady, readinessProblem);
    }

    private string SelectedVersionDownloadRequiredStatus()
    {
        var branch = LauncherPreferences.ReadGameBranch();
        return $"{_model.LoggedInStatus()} Download selected game version: {STS2Mobile.Steam.SteamGameBranch.DisplayName(branch)}. Active install slot: {STS2Mobile.Steam.SteamGameInstallPaths.VersionSlotKind(branch)}.";
    }
}
