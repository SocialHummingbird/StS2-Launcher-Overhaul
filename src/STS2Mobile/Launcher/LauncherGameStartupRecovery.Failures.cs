using Godot;
using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    internal static void HandleFailure(Node gameNode, Label startupStatus, Exception ex)
        => LogAndShowFailure(
            gameNode,
            startupStatus,
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
            gameNode,
            startupStatus,
            "Settings/save init failed",
            ex,
            RecoveryStateUpdate.SettingsAndSavesFailed
        );

    private static bool HandleMainMenuGuardFailure(Node gameNode, Label startupStatus)
    {
        ShowFailure(
            gameNode,
            startupStatus,
            RecoveryStateUpdate.MainMenuGuardFailed()
        );
        return false;
    }

    private static void LogAndShowFailure(
        Node gameNode,
        Label startupStatus,
        string logPrefix,
        Exception ex,
        Func<Exception, RecoveryStateUpdate> createUpdate
    )
    {
        PatchHelper.Log($"{logPrefix}: {ex}");
        ShowFailure(gameNode, startupStatus, createUpdate(ex));
    }
}
