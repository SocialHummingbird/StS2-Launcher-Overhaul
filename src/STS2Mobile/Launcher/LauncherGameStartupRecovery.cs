using Godot;
using System;
using System.Reflection;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static class LauncherGameStartupRecovery
{
    private const int MainMenuForceTimeoutMs = 15_000;
    private const int PostStartupRecoveryMs = 180_000;

    internal static async Task HandleWatchdogAsync(
        object game,
        Node gameNode,
        Label startupStatus,
        CanvasLayer recoveryControls,
        int watchdogMs
    )
    {
        LauncherStartupMarkerTrace.Write("game startup watchdog");
        LauncherStartupSceneSnapshot.Write(gameNode, "game startup watchdog");
        LauncherStartupStatus.Set(
            startupStatus,
            "Game startup stalled. Attempting main menu recovery..."
        );
        PatchHelper.Log(
            $"Game startup watchdog fired after {watchdogMs}ms; startup task still running"
        );

        var recovered = await EnsureMainMenuAfterStartupAsync(
            game,
            startupStatus,
            MainMenuForceTimeoutMs
        );
        LauncherStartupSceneSnapshot.Write(
            gameNode,
            recovered ? "main menu recovered after watchdog" : "main menu recovery failed after watchdog"
        );
        if (recovered)
        {
            LauncherStartupMarkerTrace.Write("main menu recovered after watchdog");
            LauncherStartupStatus.Set(
                startupStatus,
                "Main menu recovered after startup stall. Recovery controls remain briefly."
            );
            ScheduleCleanup(recoveryControls, startupStatus);
            return;
        }

        ShowRecoveryFailure(
            gameNode,
            startupStatus,
            "main menu recovery failed after watchdog",
            "Game startup stalled and main menu recovery failed. Use recovery controls below."
        );
    }

    internal static async Task<bool> EnsureMainMenuReadyAsync(
        object game,
        Node gameNode,
        Label startupStatus
    )
    {
        var mainMenuReady = await EnsureMainMenuAfterStartupAsync(
            game,
            startupStatus,
            MainMenuForceTimeoutMs
        );
        return mainMenuReady
            ? true
            : HandleMainMenuGuardFailure(gameNode, startupStatus);
    }

    internal static void MarkStartupObserved(CanvasLayer recoveryControls, Label startupStatus, Node gameNode)
    {
        LauncherStartupMarkerTrace.Write("post-startup observation");
        LauncherStartupSceneSnapshot.Write(
            gameNode,
            "after NGame.GameStartup returned"
        );
        LauncherStartupStatus.Set(
            startupStatus,
            "Game startup returned. Recovery controls remain briefly."
        );
        ScheduleCleanup(recoveryControls, startupStatus);
    }

    internal static void HandleFailure(Node gameNode, Label startupStatus, Exception ex)
    {
        var root = ex.GetBaseException();
        var message = $"{root.GetType().Name}: {root.Message}";
        PatchHelper.Log($"Game startup failed: {ex}");
        ShowRecoveryFailure(
            gameNode,
            startupStatus,
            $"game startup failed: {message}",
            $"Game startup failed: {message}"
        );
    }

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

    private static bool HandleMainMenuGuardFailure(Node gameNode, Label startupStatus)
    {
        ShowRecoveryFailure(
            gameNode,
            startupStatus,
            "main menu guard failed",
            "Main menu did not load. Use recovery controls below."
        );
        return false;
    }

    private static void ShowRecoveryFailure(
        Node gameNode,
        Label startupStatus,
        string reason,
        string statusMessage
    )
    {
        LauncherStartupMarkerTrace.Write(reason);
        LauncherStartupSceneSnapshot.Write(gameNode, reason);
        LauncherStartupStatus.Set(startupStatus, statusMessage);
        LauncherStartupRecoveryControlPanel.Show(gameNode);
    }

    private static async void ScheduleCleanup(CanvasLayer recoveryControls, Label startupStatus)
    {
        try
        {
            await Task.Delay(PostStartupRecoveryMs);
            LauncherStartupMarkerTrace.Clear();
            recoveryControls?.QueueFree();
            startupStatus?.QueueFree();
            PatchHelper.Log("Post-startup recovery controls cleared; scene snapshot retained");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Post-startup recovery cleanup failed: {ex.Message}");
        }
    }
}
