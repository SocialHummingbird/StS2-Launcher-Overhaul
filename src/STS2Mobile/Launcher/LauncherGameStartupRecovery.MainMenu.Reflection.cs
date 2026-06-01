using Godot;
using System;
using System.Reflection;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    private readonly struct CurrentSceneInfo
    {
        private CurrentSceneInfo(bool isMainMenu, string? sceneName)
        {
            IsMainMenu = isMainMenu;
            SceneName = sceneName;
        }

        private bool IsMainMenu { get; }
        private string? SceneName { get; }

        internal static CurrentSceneInfo Missing()
            => new(isMainMenu: false, sceneName: null);

        internal static CurrentSceneInfo Found(bool isMainMenu, string sceneName)
            => new(isMainMenu, sceneName);

        internal static CurrentSceneInfo Failed(Exception ex)
            => new(isMainMenu: false, sceneName: $"<inspect failed: {ex.Message}>");

        internal bool IsMainMenuReady()
            => IsMainMenu;

        internal string MainMenuPresentMessage()
            => $"Main menu present after startup: {SceneName}";

        internal string MainMenuMissingMessage()
            => $"Main menu missing after startup; current scene={SceneName ?? "<none>"}. Forcing LoadMainMenu.";

        internal string ForcedLoadResultMessage()
            => IsMainMenu
                ? $"Forced main menu load succeeded: {SceneName}"
                : $"Forced main menu load returned but current scene is {SceneName ?? "<none>"}";

        internal string ForcedLoadStatus()
            => IsMainMenu
                ? "Main menu loaded."
                : "Main menu force returned, but scene is still not main menu.";
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

    private static CurrentSceneInfo InspectCurrentScene(object game)
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
                return CurrentSceneInfo.Missing();

            var sceneName = $"{currentScene.GetType().FullName} name={((Node)currentScene).Name}";
            return CurrentSceneInfo.Found(
                currentScene.GetType().FullName?.Contains("NMainMenu", StringComparison.Ordinal) == true,
                sceneName
            );
        }
        catch (Exception ex)
        {
            return CurrentSceneInfo.Failed(ex);
        }
    }
}
