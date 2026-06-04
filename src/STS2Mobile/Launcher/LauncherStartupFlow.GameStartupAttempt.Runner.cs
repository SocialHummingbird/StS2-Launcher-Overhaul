using System.Threading.Tasks;
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

                if (await _startup.RecoverIfWatchdogTimedOutAsync(startupTask, recoveryControls))
                    return;

                await startupTask;
                PatchHelper.Log("NGame.GameStartup completed");
                if (!await _startup.EnsureMainMenuReadyAsync())
                    return;

                _startup.MarkStartupObserved(recoveryControls);
            }
        }
    }
}
