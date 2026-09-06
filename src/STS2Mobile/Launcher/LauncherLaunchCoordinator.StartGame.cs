using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherLaunchCoordinator
{
    private void StartGame(LauncherStartGamePlan plan) => _ = StartGameAsync(plan);

    private async Task StartGameAsync(LauncherStartGamePlan plan)
    {
        if (!TryBeginLaunchAttempt(plan, out var attempt))
            return;

        LauncherLaunchReadiness readiness;
        LauncherModLaunchReadiness modReadiness;
        var interactionLock = _view.LockPreparationInteraction();
        try
        {
            _view.SetStatus(plan.CheckingStatus, LauncherStatusSeverity.Working);
            readiness = await EvaluateLaunchReadinessAsync(plan, attempt, interactionLock);
            if (readiness == null)
                return;

            interactionLock.Message.Text = "Preparing selected mods…";
            modReadiness = await EvaluateModReadinessAsync(plan, attempt, readiness, interactionLock);
            if (modReadiness == null)
                return;
        }
        finally
        {
            interactionLock.Release();
        }

        CompleteLaunchHandoff(plan, attempt, readiness, modReadiness);
    }

    private static void ShowPreparationDraining(LauncherPreparationOverlay interactionLock)
    {
        if (Godot.GodotObject.IsInstanceValid(interactionLock)
            && Godot.GodotObject.IsInstanceValid(interactionLock.Message))
            interactionLock.Message.Text = "Preparation is taking too long. Waiting for file operations to finish safely…";
    }

    private static void ObservePreparationFailure(Exception error)
        => PatchHelper.Log($"[Launch] Preparation task settled with failure: {error}");
}
