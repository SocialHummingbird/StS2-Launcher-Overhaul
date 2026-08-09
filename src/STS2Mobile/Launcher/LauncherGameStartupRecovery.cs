using Godot;
using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    private const int MainMenuForceTimeoutMs = 15_000;
    internal static void MarkGameStartupCompleted(object game, Node gameNode)
        => WriteSuccessfulPostStartupEvidence(
            game,
            gameNode,
            "NGame.GameStartup completed before main-menu guard"
        );

    internal static async Task<bool> EnsureMainMenuReadyAsync(
        object game,
        Node gameNode,
        Label startupStatus
    )
    {
        var ui = RecoveryUi.For(gameNode, startupStatus);
        var mainMenuReady = await EnsureMainMenuAfterStartupAsync(
            game,
            gameNode,
            startupStatus,
            MainMenuForceTimeoutMs
        );
        if (!mainMenuReady)
            return HandleMainMenuGuardFailure(ui);
        return true;
    }

    internal static void MarkStartupObserved(
        object game,
        CanvasLayer recoveryControls,
        Label startupStatus,
        Node gameNode
    )
    {
        RecoveryUi.For(gameNode, startupStatus).MarkRecoveredStartup(
            recoveryControls,
            RecoveryStateUpdate.StartupObserved()
        );
        SchedulePostStartupDiagnostics(game, gameNode);
    }

}
