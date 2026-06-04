using System.Threading.Tasks;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private readonly partial struct StartupContext
    {
        private readonly struct GameStartupAttempt
        {
            private readonly StartupContext _startup;

            internal GameStartupAttempt(StartupContext startup)
            {
                _startup = startup;
            }

            internal async Task RunAsync()
            {
                _startup.SetPhase(PhaseGameStartup, "Starting game scene...");
                PatchHelper.Log("Invoking NGame.GameStartup");

                var recoveryControls = _startup.ShowRecoveryControls();
                _startup.WriteSceneSnapshot("before NGame.GameStartup");
                var startupTask = _startup.StartGameStartupAsync();

                if (await RecoverIfWatchdogTimedOutAsync(startupTask, recoveryControls))
                    return;

                await startupTask;
                PatchHelper.Log("NGame.GameStartup completed");
                if (!await EnsureMainMenuReadyAsync())
                    return;

                MarkStartupObserved(recoveryControls);
            }

            private Task<bool> RecoverIfWatchdogTimedOutAsync(
                Task startupTask,
                CanvasLayer recoveryControls
            )
            {
                var game = _startup.Game;
                var gameNode = _startup.GameNode;
                var status = _startup.Status;
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
                    _startup.Game,
                    _startup.GameNode,
                    _startup.Status
                );

            private void MarkStartupObserved(CanvasLayer recoveryControls)
                => LauncherGameStartupRecovery.MarkStartupObserved(
                    recoveryControls,
                    _startup.Status,
                    _startup.GameNode
                );
        }
    }
}
