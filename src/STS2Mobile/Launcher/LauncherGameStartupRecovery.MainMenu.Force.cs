using Godot;
using System;
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
        if (!TryStartLoadMainMenu(game, out var task))
            return false;

        var timeout = Task.Delay(forceTimeoutMs);
        if (await Task.WhenAny(task, timeout) != task)
        {
            PatchHelper.Log($"Forced LoadMainMenu timed out after {forceTimeoutMs}ms");
            LauncherStartupStatus.Set(startupStatus, "Forced main menu load timed out.");
            return false;
        }

        await task;
        var ok = CurrentSceneLooksLikeMainMenu(game, out var sceneName);
        PatchHelper.Log(
            ok
                ? $"Forced main menu load succeeded: {sceneName}"
                : $"Forced main menu load returned but current scene is {sceneName ?? "<none>"}"
        );
        LauncherStartupStatus.Set(
            startupStatus,
            ok
                ? "Main menu loaded."
                : "Main menu force returned, but scene is still not main menu."
        );
        return ok;
    }

    private static bool TryStartLoadMainMenu(object game, out Task task)
    {
        try
        {
            task = LoadMainMenuAsync(game);
            return true;
        }
        catch (Exception ex) when (ex is MissingMethodException or InvalidOperationException)
        {
            PatchHelper.Log($"Cannot force main menu: {ex.Message}");
            task = Task.CompletedTask;
            return false;
        }
    }
}
