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
            private readonly struct StartedGameStartup
            {
                private StartedGameStartup(
                    StartupContext startup,
                    CanvasLayer recoveryControls,
                    Task startupTask
                )
                {
                    Startup = startup;
                    RecoveryControls = recoveryControls;
                    StartupTask = startupTask;
                }

                private StartupContext Startup { get; }
                private CanvasLayer RecoveryControls { get; }
                private Task StartupTask { get; }

                internal static StartedGameStartup Begin(StartupContext startup)
                {
                    startup.SetPhase(PhaseGameStartup, "Starting game scene...");
                    PatchHelper.Log("Invoking NGame.GameStartup");

                    var recoveryControls = startup.ShowRecoveryControls();
                    startup.WriteSceneSnapshot("before NGame.GameStartup");
                    return new StartedGameStartup(
                        startup,
                        recoveryControls,
                        startup.StartGameStartupAsync()
                    );
                }

                internal async Task RunAsync()
                {
                    if (await TryRecoverFromWatchdogAsync())
                        return;

                    if (!await CompleteAsync())
                        return;

                    MarkObserved();
                }

                private Task<bool> TryRecoverFromWatchdogAsync()
                    => Startup.RecoverIfWatchdogTimedOutAsync(
                        StartupTask,
                        RecoveryControls
                    );

                private async Task<bool> CompleteAsync()
                {
                    await StartupTask;
                    PatchHelper.Log("NGame.GameStartup completed");
                    return await Startup.EnsureMainMenuReadyAsync();
                }

                private void MarkObserved()
                    => Startup.MarkStartupObserved(RecoveryControls);
            }

            private readonly StartupContext _startup;

            internal GameStartupAttempt(StartupContext startup)
            {
                _startup = startup;
            }

            internal async Task RunAsync()
            {
                await StartedGameStartup.Begin(_startup).RunAsync();
            }
        }
    }
}
