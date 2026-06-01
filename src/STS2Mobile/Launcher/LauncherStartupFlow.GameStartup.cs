using System;
using System.Reflection;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private static async Task RunGameStartupAsync(StartupContext startup)
    {
        try
        {
            startup.SetPhase(PhaseGameStartup, "Starting game scene...");
            PatchHelper.Log("Invoking NGame.GameStartup");

            var recoveryControls = startup.ShowRecoveryControls();
            startup.WriteSceneSnapshot("before NGame.GameStartup");
            var startupTask = startup.StartGameStartupAsync();
            if (await RecoverIfWatchdogTimedOutAsync(
                    startupTask,
                    startup,
                    recoveryControls
                ))
            {
                return;
            }

            await startupTask;
            PatchHelper.Log("NGame.GameStartup completed");
            if (!await startup.EnsureMainMenuReadyAsync())
                return;

            startup.MarkStartupObserved(recoveryControls);
        }
        catch (TargetInvocationException ex)
        {
            var root = ex.InnerException ?? ex;
            startup.HandleFailure(root);
        }
        catch (Exception ex)
        {
            startup.HandleFailure(ex);
        }
    }
}
