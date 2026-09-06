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
        internal CurrentSceneInspection(bool isMainMenu, string sceneName)
        {
            IsMainMenu = isMainMenu;
            SceneName = sceneName;
        }

        internal bool IsMainMenu { get; }
        internal string SceneName { get; }
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

}
