using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private readonly partial struct StartupContext
    {
        internal Task RunGameStartupWithRecoveryAsync()
            => new GameStartupAttempt(this).RunAsync();

        private CanvasLayer ShowRecoveryControls()
            => LauncherStartupRecoveryControlPanel.Show(GameNode);

        private void WriteSceneSnapshot(string reason)
            => LauncherDiagnostics.WriteStartupSceneSnapshot(GameNode, reason);

        private Task StartGameStartupAsync()
            => LauncherStartupFlow.StartGameStartupAsync(Game);

        private Task<bool> RecoverIfWatchdogTimedOutAsync(
            Task startupTask,
            CanvasLayer recoveryControls
        )
        {
            var game = Game;
            var gameNode = GameNode;
            var status = Status;

            return LauncherTimeout.RecoverIfTimedOutAsync(
                startupTask,
                StartupWatchdogMs,
                () => LauncherGameStartupRecovery.HandleWatchdogAsync(
                    game,
                    gameNode,
                    status,
                    recoveryControls,
                    StartupWatchdogMs
                )
            );
        }

        private Task<bool> EnsureMainMenuReadyAsync()
            => LauncherGameStartupRecovery.EnsureMainMenuReadyAsync(
                Game,
                GameNode,
                Status
            );

        private void MarkStartupObserved(CanvasLayer recoveryControls)
            => LauncherGameStartupRecovery.MarkStartupObserved(
                recoveryControls,
                Status,
                GameNode
            );
    }
}
