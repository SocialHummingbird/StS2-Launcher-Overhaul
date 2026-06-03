using Godot;
using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    internal static void HandleFailure(Node gameNode, Label startupStatus, Exception ex)
    {
        PatchHelper.Log($"Game startup failed: {ex}");
        ShowFailure(
            gameNode,
            startupStatus,
            RecoveryStateUpdate.GameStartupFailed(ex)
        );
    }

    internal static void HandleSettingsAndSavesFailure(
        Node gameNode,
        Label startupStatus,
        Exception ex
    )
    {
        PatchHelper.Log($"Settings/save init failed: {ex}");
        ShowFailure(
            gameNode,
            startupStatus,
            RecoveryStateUpdate.SettingsAndSavesFailed(ex)
        );
    }

    private static bool HandleMainMenuGuardFailure(Node gameNode, Label startupStatus)
    {
        ShowFailure(
            gameNode,
            startupStatus,
            RecoveryStateUpdate.MainMenuGuardFailed()
        );
        return false;
    }
}
