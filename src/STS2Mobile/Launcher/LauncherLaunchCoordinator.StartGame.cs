namespace STS2Mobile.Launcher;

internal sealed partial class LauncherLaunchCoordinator
{
    private void StartGame(LauncherStartGamePlan plan)
    {
        if (!TryBeginLaunchAttempt(plan, out var attempt))
            return;

        _view.SetStatus(plan.CheckingStatus);
        if (!TryEvaluateSelectedLaunchReadiness(plan, attempt, out var readiness))
            return;

        if (!TryEvaluateModLaunchReadiness(plan, attempt, readiness, out var modReadiness))
            return;

        CompleteLaunchHandoff(plan, attempt, readiness, modReadiness);
    }
}
