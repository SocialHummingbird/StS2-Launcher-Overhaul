using System.Threading.Tasks;
using System;
using Godot;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private readonly partial struct StartupContext
    {
        internal Task RunGameStartupWithRecoveryAsync()
            => new GameStartupAttempt(this).RunAsync();

        private CanvasLayer ShowRecoveryControls()
            => OperatingSystem.IsAndroid()
                ? null
                : LauncherStartupRecoveryControlPanel.Show(GameNode);

        private void WriteStartupEntryEvidence(string phase)
        {
            var writeFullDiagnostics =
                PostStartupDiagnosticsPolicy.ShouldWriteFullDiagnostics(
                    PostStartupDiagnosticsSettings.DetailedTraceEnabled(),
                    failureOrRecovery: false
                );
            if (writeFullDiagnostics)
            {
                LauncherDiagnostics.WriteStartupSceneSnapshot(GameNode, phase);
                return;
            }

            LauncherDiagnostics.WritePostStartupHeartbeat(
                phase,
                "Scene-tree traversal skipped on ordinary startup path"
            );
        }

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
            var attemptId = AttemptId;

            return LauncherTimeout.RecoverIfTimedOutAsync(
                startupTask,
                StartupWatchdogMs,
                () => LauncherGameStartupRecovery.HandleWatchdogAsync(
                    game,
                    gameNode,
                    status,
                    recoveryControls,
                    StartupWatchdogMs,
                    attemptId
                )
            );
        }

        private Task<bool> EnsureMainMenuReadyAsync()
            => LauncherGameStartupRecovery.EnsureMainMenuReadyAsync(
                Game,
                GameNode,
                Status,
                AttemptId
            );

        private void MarkStartupObserved(CanvasLayer recoveryControls)
            => LauncherGameStartupRecovery.MarkStartupObserved(
                Game,
                recoveryControls,
                Status,
                GameNode,
                AttemptId
            );
    }
}
