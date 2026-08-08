#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherCloudSyncCoordinator
{
    private readonly partial struct ManualCloudSyncRequest
    {
        internal void ShowStarted(LauncherView view)
        {
            view.SetPushPullDisabled(true);
            if (OperationProgress == null)
                view.ClearCloudOperationState();
            else
                view.SetCloudOperationState(OperationProgress.State);
            view.SetStatus(StartMessage);
            view.AppendLog(StartMessage);
        }

        internal void PrepareOrThrow()
        {
            if (PrepareOperation != null && !PrepareOperation())
            {
                throw new InvalidOperationException(
                    $"{Name} is no longer safe for the selected save context."
                );
            }
        }

        internal CloudPostOperationResolution ResolveCompletion(
            ManualCloudSyncResult result,
            Func<CloudPostOperationSnapshot> captureCurrentState
        )
        {
            var resolution = CloudPostOperationRefresh.ResolveCompletion(
                result,
                captureCurrentState
            );
            if (OnSuccessfulCompletion != null)
            {
                try
                {
                    OnSuccessfulCompletion();
                }
                catch (Exception ex)
                {
                    PatchHelper.Log(
                        $"[Cloud] {Name} completion diagnostic could not be recorded: {ex.Message}"
                    );
                }
            }

            return resolution;
        }

        internal void RecordTerminalFailure(
            string outcome,
            string detail,
            Exception? exception = null
        )
        {
            try
            {
                RecordTerminalFailureAction?.Invoke(outcome, detail);
            }
            catch (Exception ex)
            {
                PatchHelper.Log(
                    $"[Cloud] {Name} failure diagnostic could not be recorded: {ex.Message}"
                );
            }

            if (exception == null || OnFailed == null)
                return;

            try
            {
                OnFailed(exception);
            }
            catch (Exception ex)
            {
                PatchHelper.Log(
                    $"[Cloud] {Name} blocked-operation diagnostic could not be recorded: {ex.Message}"
                );
            }
        }

        internal void ShowTerminal(
            LauncherView view,
            CloudOperationTerminalPresentation presentation
        )
        {
            PatchHelper.Log(
                $"[Cloud] {Name} terminal outcome={presentation.Outcome}: "
                    + presentation.LogText
            );
            view.SetStatus(presentation.StatusText);
            view.AppendLog(
                $"{presentation.LogText} ({DateTime.Now:HH:mm:ss})"
            );
        }

        internal void ShowFinished(LauncherView view)
            => view.SetPushPullDisabled(false);

        internal CloudOperationState ProgressState
            => OperationProgress?.State
                ?? CloudOperationState.Idle(CloudOperationKind.Pull);

        internal string OperationName => Name;

        internal Task<ManualCloudSyncResult> RunWithTimeoutAsync(
            CancellationToken cancellationToken
        )
        {
            var run = Run;
            return LauncherTimeout.RunOrThrowAsync(
                token => Task.Run(
                    () => run(token),
                    CancellationToken.None
                ),
                cancellationToken,
                TimeoutMs,
                $"{Name} timed out after {TimeoutMs}ms"
            );
        }
    }
}
