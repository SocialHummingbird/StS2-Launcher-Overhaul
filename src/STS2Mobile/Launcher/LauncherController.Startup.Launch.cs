namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void LaunchPressed()
    {
        RefreshSelectedRuntimeSlotEvidence();

        var branch = LauncherPreferences.ReadGameBranch();
        if (!LauncherGameFiles.Ready(_model.DataDir, branch))
        {
            var problem = LauncherGameFiles.ReadinessProblem(_model.DataDir, branch)
                ?? "Selected game version is not ready to launch.";
            _view.SetStatus(problem);
            _view.AppendLog(problem);
            return;
        }

        _model.Launch();
    }

    private void SafeLaunchPressed()
    {
        RefreshSelectedRuntimeSlotEvidence();

        var branch = LauncherPreferences.ReadGameBranch();
        if (!LauncherGameFiles.Ready(_model.DataDir, branch))
        {
            var problem = LauncherGameFiles.ReadinessProblem(_model.DataDir, branch)
                ?? "Selected game version is not ready to safe launch.";
            _view.SetStatus(problem);
            _view.AppendLog(problem);
            return;
        }

        _view.AppendLog(
            "Safe launch requested: default renderer, no shader warmup, local saves only for one run."
        );
        _model.LaunchSafe();
    }
}
