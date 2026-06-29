namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void LaunchPressed()
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

    private void SafeLaunchPressed()
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
}
