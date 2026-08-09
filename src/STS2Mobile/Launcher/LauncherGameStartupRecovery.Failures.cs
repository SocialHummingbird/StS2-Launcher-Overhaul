using Godot;
using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    internal static void HandleFailure(Node gameNode, Label startupStatus, Exception ex)
        => LogAndShowFailure(
            RecoveryUi.For(gameNode, startupStatus),
            "Game startup failed",
            ex,
            RecoveryStateUpdate.GameStartupFailed
        );

    internal static void HandleSettingsAndSavesFailure(
        Node gameNode,
        Label startupStatus,
        Exception ex
    )
        => LogAndShowFailure(
            RecoveryUi.For(gameNode, startupStatus),
            "Settings/save init failed",
            ex,
            RecoveryStateUpdate.SettingsAndSavesFailed
        );

    private static bool HandleMainMenuGuardFailure(RecoveryUi ui)
    {
        ui.ShowFailure(RecoveryStateUpdate.MainMenuGuardFailed());
        return false;
    }

    private static void LogAndShowFailure(
        RecoveryUi ui,
        string logPrefix,
        Exception ex,
        Func<Exception, RecoveryStateUpdate> createUpdate
    )
    {
        PatchHelper.Log($"{logPrefix}: {ex}");
        ui.ShowFailure(createUpdate(ex));
    }
}
