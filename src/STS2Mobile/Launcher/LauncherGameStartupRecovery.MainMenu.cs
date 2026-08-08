using Godot;
using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    private static async Task<bool> EnsureMainMenuAfterStartupAsync(
        object game,
        Node gameNode,
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
                WriteSuccessfulPostStartupEvidence(
                    game,
                    gameNode,
                    "main menu guard passed",
                    "Main menu was present before forced recovery"
                );
                return true;
            }

            PatchHelper.Log(MainMenuMissingMessage(scene));
            WritePostStartupTrace(
                game,
                gameNode,
                "main menu guard missing",
                "Attempting forced main-menu recovery"
            );
            LauncherStartupStatus.Set(
                startupStatus,
                "Startup returned without main menu. Forcing main menu..."
            );

            return await ForceLoadMainMenuAsync(
                game,
                gameNode,
                startupStatus,
                forceTimeoutMs
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"EnsureMainMenuAfterStartup failed: {ex}");
            LauncherDiagnostics.WritePostStartupTrace(
                gameNode,
                "main menu guard failed",
                $"{ex.GetBaseException().GetType().Name}: {ex.GetBaseException().Message}"
            );
            LauncherStartupStatus.Set(
                startupStatus,
                $"Main menu guard failed: {ex.GetBaseException().Message}"
            );
            return false;
        }
    }
}
