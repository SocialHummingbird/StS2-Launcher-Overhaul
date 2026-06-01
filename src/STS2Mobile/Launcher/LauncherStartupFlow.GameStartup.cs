using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
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

            var recoveryControls = LauncherStartupRecoveryControlPanel.Show(startup.GameNode);
            LauncherDiagnostics.WriteStartupSceneSnapshot(startup.GameNode, "before NGame.GameStartup");
            var startupTask = StartGameStartupAsync(startup.Game);
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
            if (!await LauncherGameStartupRecovery.EnsureMainMenuReadyAsync(
                    startup.Game,
                    startup.GameNode,
                    startup.Status
                ))
                return;

            LauncherGameStartupRecovery.MarkStartupObserved(
                recoveryControls,
                startup.Status,
                startup.GameNode
            );
        }
        catch (TargetInvocationException ex)
        {
            var root = ex.InnerException ?? ex;
            LauncherGameStartupRecovery.HandleFailure(startup.GameNode, startup.Status, root);
        }
        catch (Exception ex)
        {
            LauncherGameStartupRecovery.HandleFailure(startup.GameNode, startup.Status, ex);
        }
    }
}
