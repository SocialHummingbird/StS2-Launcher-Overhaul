using System;
using System.Reflection;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private static Task StartGameStartupAsync(object game)
    {
        var gameStartup = game.GetType()
            .GetMethod(
                "GameStartup",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
        if (gameStartup == null)
            throw new MissingMethodException(game.GetType().FullName, "GameStartup");

        return (Task)gameStartup.Invoke(game, null);
    }
}
