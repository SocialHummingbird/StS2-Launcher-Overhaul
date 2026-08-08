using System;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherLaunchCoordinator
{
    private void StartGame(LauncherStartGamePlan plan)
        => _ = StartGameAsync(plan);

    private async Task StartGameAsync(LauncherStartGamePlan plan)
    {
        if (!TryBeginLaunchAttempt(plan, out var attempt))
            return;

        _view.SetStatus(plan.CheckingStatus);
        if (!TryEvaluateSelectedLaunchReadiness(plan, attempt, out var readiness))
            return;

        if (!TryEvaluateModLaunchReadiness(plan, attempt, readiness, out var modReadiness))
            return;

        if (!TryValidateSelectedSaveContext(attempt, modReadiness, out var contextProblem))
        {
            FinishAutomaticSyncLaunchFailure(
                plan,
                attempt,
                readiness,
                modReadiness,
                contextProblem
            );
            return;
        }

        var saveNamespace = modReadiness.IsModded
            ? SaveNamespace.Modded
            : SaveNamespace.Vanilla;
        var runtimeIdentity = SteamGameBranch.StorageIdentity(attempt.Branch);
        var modSetFingerprint = modReadiness.ModSetFingerprint;

        LauncherAutomaticSyncPreparation preparation;
        try
        {
            preparation = await _cloud.PrepareForLaunchAsync(
                saveNamespace,
                runtimeIdentity,
                modSetFingerprint,
                beginGameSession: _model.InGameMode
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            preparation = LauncherAutomaticSyncPreparation.Blocked(
                LaunchExceptionProblem("automatic save synchronization failed", ex)
            );
        }

        await RunOnMainThreadAsync(() =>
        {
            if (!preparation.CanContinue)
            {
                FinishAutomaticSyncLaunchFailure(
                    plan,
                    attempt,
                    readiness,
                    modReadiness,
                    preparation.Message
                );
                return;
            }

            if (!TryValidateSelectedSaveContext(
                    attempt,
                    modReadiness,
                    out var finalContextProblem
                ))
            {
                FinishAutomaticSyncLaunchFailure(
                    plan,
                    attempt,
                    readiness,
                    modReadiness,
                    finalContextProblem
                );
                return;
            }

            CompleteLaunchHandoff(plan, attempt, readiness, modReadiness);
        }).ConfigureAwait(false);
    }
}
