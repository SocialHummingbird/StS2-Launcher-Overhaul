using Godot;
using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    private static async Task<bool> EnsureMainMenuAfterStartupAsync(
        object game,
        Label startupStatus,
        int forceTimeoutMs
    )
    {
        if (!OperatingSystem.IsAndroid())
            return true;

        try
        {
            var scene = InspectCurrentScene(game);
            if (scene.IsMainMenu)
            {
                PatchHelper.Log(MainMenuPresentMessage(scene));
                return true;
            }

            PatchHelper.Log(MainMenuMissingMessage(scene));
            LauncherStartupStatus.Set(
                startupStatus,
                "Startup returned without main menu. Forcing main menu..."
            );

            return await ForceLoadMainMenuAsync(game, startupStatus, forceTimeoutMs);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"EnsureMainMenuAfterStartup failed: {ex}");
            LauncherStartupStatus.Set(
                startupStatus,
                $"Main menu guard failed: {ex.GetBaseException().Message}"
            );
            return false;
        }
    }
}
