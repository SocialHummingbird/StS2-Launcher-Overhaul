using System;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherLaunchCoordinator
{
    private bool TryValidateSelectedSaveContext(
        LaunchAttemptContext attempt,
        LauncherModLaunchReadiness modReadiness,
        out string problem
    )
    {
        try
        {
            var selectedBranch = SteamGameBranch.Normalize(
                LauncherPreferences.ReadGameBranch()
            );
            if (!string.Equals(
                    selectedBranch,
                    attempt.Branch,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                problem =
                    "Launch blocked: the selected game version changed while launch was being prepared.";
                return false;
            }

            var selection = LauncherModSelectionState.Load();
            var isModded = LauncherModSelectionState.IsModdedModeFor(selection);
            if (isModded != modReadiness.IsModded)
            {
                problem =
                    "Launch blocked: Vanilla/Modded mode changed while launch was being prepared.";
                return false;
            }

            var fingerprint = isModded
                ? LauncherModSelectionState.EnabledModSetFingerprint(selection) ?? ""
                : "";
            if (!string.Equals(
                    fingerprint,
                    modReadiness.ModSetFingerprint,
                    StringComparison.Ordinal
                ))
            {
                problem =
                    "Launch blocked: the enabled mod set changed while launch was being prepared.";
                return false;
            }

            problem = "";
            return true;
        }
        catch (Exception ex)
        {
            problem = LaunchExceptionProblem(
                "selected save context could not be revalidated",
                ex
            );
            return false;
        }
    }

    private void FinishAutomaticSyncLaunchFailure(
        LauncherStartGamePlan plan,
        LaunchAttemptContext attempt,
        LauncherLaunchReadiness readiness,
        LauncherModLaunchReadiness modReadiness,
        string problem
    )
    {
        attempt.StopAttemptTiming();
        FinishFailedLaunchAttempt(
            plan,
            "launch automatic save sync blocked",
            problem,
            readiness,
            modReadiness,
            preparedReadinessUsed: true,
            attempt.ReadyTiming(),
            LauncherLaunchAttemptPhases.LaunchHandoffNotRequested,
            problem,
            attempt.AttemptId,
            writePatchLog: true
        );
    }

    private Task RunOnMainThreadAsync(Action action)
    {
        var completion = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        try
        {
            _runOnMainThread(() =>
            {
                try
                {
                    action();
                    completion.TrySetResult();
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                }
            });
        }
        catch (Exception ex)
        {
            completion.TrySetException(ex);
        }

        return completion.Task;
    }
}
