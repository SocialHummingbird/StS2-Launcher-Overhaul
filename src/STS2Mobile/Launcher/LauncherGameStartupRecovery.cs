using Godot;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    private const int MainMenuForceTimeoutMs = 15_000;
    private const int PostStartupRecoveryMs = 180_000;

    internal static async Task<bool> EnsureMainMenuReadyAsync(
        object game,
        Node gameNode,
        Label startupStatus
    )
    {
        var ui = RecoveryUi.For(gameNode, startupStatus);
        var mainMenuReady = await EnsureMainMenuAfterStartupAsync(
            game,
            startupStatus,
            MainMenuForceTimeoutMs
        );
        return mainMenuReady
            ? true
            : HandleMainMenuGuardFailure(ui);
    }

    internal static void MarkStartupObserved(
        CanvasLayer recoveryControls,
        Label startupStatus,
        Node gameNode
    )
    {
        MarkRecoveredStartup(
            recoveryControls,
            RecoveryUi.For(gameNode, startupStatus),
            RecoveryStateUpdate.StartupObserved()
        );
    }
}
