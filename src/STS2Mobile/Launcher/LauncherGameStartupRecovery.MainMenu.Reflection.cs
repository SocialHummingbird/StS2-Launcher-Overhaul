using Godot;
using System;
using System.Reflection;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    private readonly struct CurrentSceneInspection
    {
        internal CurrentSceneInspection(bool isMainMenu, string? sceneName)
        {
            IsMainMenu = isMainMenu;
            SceneName = sceneName;
        }

        internal bool IsMainMenu { get; }
        internal string? SceneName { get; }
    }

    private static Task LoadMainMenuAsync(object game)
    {
        var loadMainMenu = game.GetType()
            .GetMethod(
                "LoadMainMenu",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            )
            ?? throw new MissingMethodException("NGame.LoadMainMenu not found");
        if (loadMainMenu.Invoke(game, new object[] { false }) is not Task task)
            throw new InvalidOperationException("NGame.LoadMainMenu did not return Task");

        return task;
    }

    private static Task StartLoadMainMenu(object game)
    {
        try
        {
            return LoadMainMenuAsync(game);
        }
        catch (Exception ex) when (ex is MissingMethodException or InvalidOperationException)
        {
            PatchHelper.Log($"Cannot force main menu: {ex.Message}");
            return null;
        }
    }

    private static CurrentSceneInspection InspectCurrentScene(object game)
    {
        try
        {
            var rootSceneContainer = game.GetType()
                .GetProperty("RootSceneContainer", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(game);
            var currentScene = rootSceneContainer?.GetType()
                .GetProperty("CurrentScene", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(rootSceneContainer);
            if (currentScene == null)
                return new CurrentSceneInspection(false, null);

            var sceneName = $"{currentScene.GetType().FullName} name={((Node)currentScene).Name}";
            return new CurrentSceneInspection(
                currentScene.GetType().FullName?.Contains(
                    "NMainMenu",
                    StringComparison.Ordinal
                ) == true,
                sceneName
            );
        }
        catch (Exception ex)
        {
            return new CurrentSceneInspection(
                false,
                $"<inspect failed: {ex.Message}>"
            );
        }
    }

    private static string MainMenuPresentMessage(CurrentSceneInspection scene)
        => $"Main menu present after startup: {scene.SceneName}";

    private static string MainMenuMissingMessage(CurrentSceneInspection scene)
        => $"Main menu missing after startup; current scene={scene.SceneName ?? "<none>"}. Forcing LoadMainMenu.";

    private static string ForcedLoadResultMessage(CurrentSceneInspection scene)
        => scene.IsMainMenu
            ? $"Forced main menu load succeeded: {scene.SceneName}"
            : $"Forced main menu load returned but current scene is {scene.SceneName ?? "<none>"}";

    private static string ForcedLoadStatus(CurrentSceneInspection scene)
        => scene.IsMainMenu
            ? "Main menu loaded."
            : "Main menu force returned, but scene is still not main menu.";
}
