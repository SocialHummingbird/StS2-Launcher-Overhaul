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
        RecordStartupState(
            gameNode,
            startupStatus,
            new RecoveryStateUpdate(
                WatchdogStalledReason,
                "Game startup stalled. Attempting main menu recovery..."
            )
        );
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
                startupStatus,
                gameNode,
                new RecoveryStateUpdate(
                    WatchdogRecoveredReason,
                    "Main menu recovered after startup stall. Recovery controls remain briefly."
                )
            );
            return;
        }

        ShowFailure(
            gameNode,
            startupStatus,
            new RecoveryStateUpdate(
                MainMenuRecoveryFailureReason,
                "Game startup stalled and main menu recovery failed. Use recovery controls below."
            )
        );
    }
}
