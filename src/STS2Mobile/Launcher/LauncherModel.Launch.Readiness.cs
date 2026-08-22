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

        var authorizationProblem = string.Empty;
        if (readiness.Ready
            && readiness.HasCurrentLaunchAuthorization(out authorizationProblem))
        {
            problem = "";
            return true;
        }

        problem = readiness.Ready
            ? $"Launch blocked: {authorizationProblem}"
            : readiness.ReadinessProblem;
        LauncherLaunchMarkers.RecordPhase("launch model blocked", problem);
        return false;
    }
}
