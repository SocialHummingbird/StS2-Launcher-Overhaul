using Godot;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
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

    private static bool CurrentSceneLooksLikeMainMenu(object game, out string sceneName)
    {
        sceneName = null;
        try
        {
            var rootSceneContainer = game.GetType()
                .GetProperty("RootSceneContainer", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(game);
            var currentScene = rootSceneContainer?.GetType()
                .GetProperty("CurrentScene", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(rootSceneContainer);
            if (currentScene == null)
                return false;

            sceneName = $"{currentScene.GetType().FullName} name={((Node)currentScene).Name}";
            return currentScene.GetType().FullName?.Contains("NMainMenu", StringComparison.Ordinal) == true;
        }
        catch (Exception ex)
        {
            sceneName = $"<inspect failed: {ex.Message}>";
            return false;
        }
    }
}
