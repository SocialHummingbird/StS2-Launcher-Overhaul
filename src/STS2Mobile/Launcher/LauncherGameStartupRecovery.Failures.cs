using Godot;
using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    internal static void HandleFailure(
        Node gameNode,
        Label startupStatus,
        string attemptId,
        Exception ex
    )
        => LogAndShowFailure(
            RecoveryUi.For(gameNode, startupStatus, attemptId),
            "Game startup failed",
            ex,
            RecoveryStateUpdate.GameStartupFailed
        );

    internal static void HandleSettingsAndSavesFailure(
        Node gameNode,
        Label startupStatus,
        string attemptId,
        Exception ex
    )
        => LogAndShowFailure(
            RecoveryUi.For(gameNode, startupStatus, attemptId),
            "Settings/save init failed",
            ex,
            RecoveryStateUpdate.SettingsAndSavesFailed
        );

    private static bool HandleGameVisibilityFailure(RecoveryUi ui)
    {
        ui.ShowFailure(RecoveryStateUpdate.GameVisibilityUnconfirmed());
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
