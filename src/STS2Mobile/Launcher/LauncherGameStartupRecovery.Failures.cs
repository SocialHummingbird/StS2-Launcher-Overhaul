using Godot;
using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    internal static void HandleFailure(Node gameNode, Label startupStatus, Exception ex)
    {
        var root = ex.GetBaseException();
        var message = $"{root.GetType().Name}: {root.Message}";
        PatchHelper.Log($"Game startup failed: {ex}");
        ShowFailure(
            gameNode,
            startupStatus,
            $"game startup failed: {message}",
            $"Game startup failed: {message}"
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
            "settings and saves failed",
            $"Settings/save init failed: {ex.GetBaseException().Message}"
        );
    }

    private static bool HandleMainMenuGuardFailure(Node gameNode, Label startupStatus)
    {
        ShowFailure(
            gameNode,
            startupStatus,
            "main menu guard failed",
            "Main menu did not load. Use recovery controls below."
        );
        return false;
    }
}
