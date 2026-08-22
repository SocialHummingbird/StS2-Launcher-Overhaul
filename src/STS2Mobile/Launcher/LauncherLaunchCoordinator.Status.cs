namespace STS2Mobile.Launcher;

internal sealed partial class LauncherLaunchCoordinator
{
    internal void ShowLoggedIn(System.Action showDownloadReadyAction)
    {
        _model.MarkConnectionResolved();
        var readiness = RefreshSelectedDownloadedStateEvidence("login downloaded-state readiness");
        if (readiness.Ready)
        {
            ShowReadyToLaunch(
                SelectedVersionReadyStatus(readiness),
                LaunchUpdateAction.Visible,
                LauncherStatusSeverity.Ready
            );
            return;
        }

        var readinessProblem = readiness.ReadinessProblem;
        _view.SetStatus(
            readinessProblem ?? SelectedVersionDownloadRequiredStatus(readiness),
            LauncherStatusSeverity.Warning
        );
        showDownloadReadyAction();
    }

    internal void ShowReadyToLaunch(
        string status,
        LaunchUpdateAction updateAction,
        LauncherStatusSeverity severity
    )
    {
        STS2Mobile.PatchHelper.Log("[Launcher] Ready-to-launch UI phase: set status");
        _view.SetStatus(status, severity);
        STS2Mobile.PatchHelper.Log("[Launcher] Ready-to-launch UI phase complete: set status");
        STS2Mobile.PatchHelper.Log("[Launcher] Ready-to-launch UI phase: show launch actions");
        ShowLaunchActions(updateAction);
        STS2Mobile.PatchHelper.Log("[Launcher] Ready-to-launch UI phase complete: show launch actions");
        STS2Mobile.PatchHelper.Log("[Launcher] Ready-to-launch UI phase: previous launch warning");
        _diagnostics.ShowPreviousLaunchWarningIfNeeded();
        STS2Mobile.PatchHelper.Log("[Launcher] Ready-to-launch UI phase complete: previous launch warning");
    }

    internal void ShowLaunchActions(LaunchUpdateAction updateAction)
    {
        _view.ShowLaunchActions(
            _model.LaunchButtonText(),
            updateAction == LaunchUpdateAction.Visible
        );
    }

    internal string SelectedVersionReadyStatus(LauncherLaunchReadiness readiness)
        => "Ready to play.";

    internal string SelectedVersionReadyStatus(string baseStatus, LauncherLaunchReadiness readiness)
    {
        var prefix = baseStatus?.Trim().TrimEnd('.');
        return string.IsNullOrWhiteSpace(prefix)
            ? "Ready to play."
            : $"{prefix}. Ready to play.";
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
        return $"Download {STS2Mobile.Steam.SteamGameBranch.DisplayName(branch)} to play.";
    }

    internal LauncherLaunchReadiness RefreshSelectedRuntimeSlotEvidence(
        BranchInstallCompletion completion
    )
    {
        if (completion == null)
            throw new System.ArgumentNullException(nameof(completion));

        return LauncherLaunchReadiness.EvaluateCompletedInstall(
            _model.DataDir,
            completion.GameIdentity,
            "completed download runtime evidence refresh"
        );
    }
}
