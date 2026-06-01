using Godot;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    private static async Task<bool> ForceLoadMainMenuAsync(
        object game,
        Label startupStatus,
        int forceTimeoutMs
    )
    {
        var loadMainMenu = StartLoadMainMenu(game);
        if (loadMainMenu == null)
            return false;

        var timeout = Task.Delay(forceTimeoutMs);
        if (await Task.WhenAny(loadMainMenu, timeout) != loadMainMenu)
        {
            PatchHelper.Log($"Forced LoadMainMenu timed out after {forceTimeoutMs}ms");
            SetRecoveryStatus(startupStatus, "Forced main menu load timed out.");
            return false;
        }

        await loadMainMenu;
        var scene = InspectCurrentScene(game);
        PatchHelper.Log(
            scene.IsMainMenu
                ? $"Forced main menu load succeeded: {scene.SceneName}"
                : $"Forced main menu load returned but current scene is {scene.SceneName ?? "<none>"}"
        );
        SetRecoveryStatus(
            startupStatus,
            scene.IsMainMenu
                ? "Main menu loaded."
                : "Main menu force returned, but scene is still not main menu."
        );
        return scene.IsMainMenu;
    }
}
