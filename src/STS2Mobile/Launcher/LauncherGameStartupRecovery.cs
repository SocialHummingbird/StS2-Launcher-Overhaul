using Godot;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    private const int GameVisibilityConfirmationTimeoutMs = 15_000;
    internal static void MarkGameStartupCompleted(object game, Node gameNode)
        => WriteSuccessfulPostStartupEvidence(
            game,
            gameNode,
            "NGame.GameStartup completed before main-menu guard"
        );

    internal static async Task<bool> EnsureMainMenuReadyAsync(
        object game,
        Node gameNode,
        Label startupStatus,
        string attemptId
    )
    {
        var ui = RecoveryUi.For(gameNode, startupStatus, attemptId);
        var operation = LauncherHandoffStateOwner.Shared.GetOperation(attemptId);
        using (var stage = new LauncherStartupStageScope(gameNode, operation, TimeSpan.FromMilliseconds(GameVisibilityConfirmationTimeoutMs)))
        {
            while (true)
            {
                var scene = LauncherGameSceneReadiness.GetCurrentReadyScene(game, attemptId);
                if (scene != null)
                {
                    operation.SetStage(LauncherStartupStage.WaitingForVisibleFrame);
                    await stage.PostDrawAsync();
                    if (LauncherGameSceneReadiness.TryConfirmRendered(game, attemptId, scene)) break;
                }
                await stage.ProcessFrameAsync();
            }
        }

        return await CompleteReadyMainMenuHandoffAsync(
            ui,
            gameNode,
            attemptId
        );
    }

    private static async Task<bool> CompleteReadyMainMenuHandoffAsync(
        RecoveryUi ui,
        Node gameNode,
        string attemptId
    )
    {
        if (!await LauncherHandoffVisibilityConfirmation.WaitForGameVisibleAsync(
            LauncherHandoffStateOwner.Shared,
            attemptId,
            gameNode,
            GameVisibilityConfirmationTimeoutMs
        ))
            return HandleGameVisibilityFailure(ui);

        return true;
    }

    internal static void MarkStartupObserved(
        object game,
        CanvasLayer recoveryControls,
        Label startupStatus,
        Node gameNode,
        string attemptId
    )
    {
        if (!RecoveryUi.For(gameNode, startupStatus, attemptId).MarkRecoveredStartup(
            recoveryControls,
            RecoveryStateUpdate.StartupObserved()
        ))
            return;

        SchedulePostStartupDiagnostics(game, gameNode);
    }

    internal static async Task CompleteMainMenuHandoffAsync(
        Func<Task<bool>> prepareAsync, Action markObserved, Func<Task> holdLifetimeAsync)
    {
        ArgumentNullException.ThrowIfNull(prepareAsync);
        ArgumentNullException.ThrowIfNull(markObserved);
        ArgumentNullException.ThrowIfNull(holdLifetimeAsync);
        if (!await prepareAsync()) return;
        markObserved();
        await holdLifetimeAsync();
    }

    internal static async Task HoldAndroidStartupTaskAfterObservedAsync()
    {
        if (!OperatingSystem.IsAndroid())
            return;

        await HoldAndroidStartupTaskAfterObservedAsync(
            isAndroid: true,
            Task.Delay(Timeout.InfiniteTimeSpan)
        );
    }

    internal static async Task HoldAndroidStartupTaskAfterObservedAsync(
        bool isAndroid,
        Task lifetimeAnchor
    )
    {
        if (!isAndroid)
            return;

        ArgumentNullException.ThrowIfNull(lifetimeAnchor);
        PatchHelper.Log(
            "Android post-startup task anchor active; keeping GameStartupWrapper pending after startup observation"
        );
        await lifetimeAnchor;
    }
}
