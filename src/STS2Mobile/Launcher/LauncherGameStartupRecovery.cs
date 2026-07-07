using Godot;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    private const int MainMenuForceTimeoutMs = 15_000;
    private const int PostStartupRecoveryMs = 3_000;

    internal static void MarkGameStartupCompleted(object game, Node gameNode)
        => WritePostStartupTrace(
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
        return mainMenuReady
            ? true
            : HandleMainMenuGuardFailure(ui);
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
        SchedulePostStartupTrace(game, gameNode);
    }
}
