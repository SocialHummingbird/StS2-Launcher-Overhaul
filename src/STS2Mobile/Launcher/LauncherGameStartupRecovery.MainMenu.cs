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
            if (CurrentSceneLooksLikeMainMenu(game, out var sceneName))
            {
                PatchHelper.Log($"Main menu present after startup: {sceneName}");
                return true;
            }

            PatchHelper.Log(
                $"Main menu missing after startup; current scene={sceneName ?? "<none>"}. Forcing LoadMainMenu."
            );
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
