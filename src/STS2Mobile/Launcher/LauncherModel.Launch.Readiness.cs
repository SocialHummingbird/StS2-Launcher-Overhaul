namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private static bool SelectedGameVersionReadyForLaunch(
        LauncherLaunchReadiness readiness,
        out string problem
    )
    {
        if (readiness == null)
        {
            problem = "Launch blocked: selected game version readiness was not prepared.";
            LauncherLaunchMarkers.RecordPhase("launch model blocked", problem);
            return false;
        }

        if (readiness.Ready)
        {
            problem = "";
            return true;
        }

        problem = readiness.ReadinessProblem;
        LauncherLaunchMarkers.RecordPhase("launch model blocked", problem);
        return false;
    }
}
