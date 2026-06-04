using Godot;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    internal static async Task HandleWatchdogAsync(
        object game,
        Node gameNode,
        Label startupStatus,
        CanvasLayer recoveryControls,
        int watchdogMs
    )
    {
        var ui = RecoveryUi.For(gameNode, startupStatus);
        ui.Apply(RecoveryStateUpdate.WatchdogStalled());
        PatchHelper.Log(
            $"Game startup watchdog fired after {watchdogMs}ms; startup task still running"
        );

        var recovered = await EnsureMainMenuAfterStartupAsync(
            game,
            startupStatus,
            MainMenuForceTimeoutMs
        );
        if (recovered)
        {
            MarkRecoveredStartup(
                recoveryControls,
                ui,
                RecoveryStateUpdate.WatchdogRecovered()
            );
            return;
        }

        ShowFailure(
            ui,
            RecoveryStateUpdate.MainMenuRecoveryFailed()
        );
    }
}
