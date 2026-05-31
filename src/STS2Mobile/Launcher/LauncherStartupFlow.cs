using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static class LauncherStartupFlow
{
    private const int StartupWatchdogMs = 60_000;

    internal static async Task RunAsync(object game)
    {
        var gameNode = (Node)game;

        var launcher = new LauncherUI();
        launcher.SetGameMode(true);
        launcher.Initialize();
        gameNode.AddChild(launcher);
        PatchHelper.Log("Launcher UI displayed");
        await launcher.WaitForLaunch();

        var startupStatus = LauncherStartupStatus.CreateLabel(gameNode);
        var previousIncompletePhase = LauncherStartupMarkerTrace.ReadPhase();
        var manualSafeLaunch = LauncherLaunchMarkers.ConsumeManualSafeLaunchMarker();
        var startupMode = new StartupMode(previousIncompletePhase, manualSafeLaunch);
        ApplyStartupSaveMode(startupMode);

        LauncherStartupMarkerTrace.Write("launch requested");
        LauncherStartupStatus.Set(startupStatus, "Starting game...");
        PatchHelper.Log("User launched game, proceeding to startup...");

        ResetSaveManagerInstance();

        launcher.QueueFree();
        LauncherStartupMarkerTrace.Write("launcher closed");
        LauncherStartupStatus.Set(startupStatus, "Launcher closed. Preparing game startup...");

        await RunShaderWarmupIfNeededAsync(gameNode, startupStatus, startupMode);
        if (!InitializeSettingsAndSaves(gameNode, startupStatus, startupMode))
            return;

        await RunGameStartupAsync(game, gameNode, startupStatus);
    }

    private static void ResetSaveManagerInstance()
    {
        var instanceField = typeof(SaveManager).GetField(
            "_instance",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        if (instanceField == null)
            return;

        instanceField.SetValue(null, null);
        PatchHelper.Log("[Cloud] Reset SaveManager._instance for cloud store re-injection");
    }

    private static bool InitializeSettingsAndSaves(
        Node gameNode,
        Label startupStatus,
        StartupMode startupMode
    )
    {
        LauncherStartupMarkerTrace.Write("settings and saves");
        LauncherStartupStatus.Set(
            startupStatus,
            startupMode.ForceLocalSaves
                ? "Loading settings and saves in local-only safe mode..."
                : "Loading settings and saves..."
        );
        PatchHelper.Log("Initializing settings and save manager");
        try
        {
            SaveManager.Instance.InitSettingsData();
            return true;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Settings/save init failed: {ex}");
            LauncherStartupMarkerTrace.Write("settings and saves failed");
            LauncherStartupSceneSnapshot.Write(gameNode, "settings and saves failed");
            LauncherStartupStatus.Set(
                startupStatus,
                $"Settings/save init failed: {ex.GetBaseException().Message}"
            );
            LauncherStartupRecoveryControlPanel.Show(gameNode);
            return false;
        }
    }

    private static async Task RunGameStartupAsync(object game, Node gameNode, Label startupStatus)
    {
        try
        {
            LauncherStartupMarkerTrace.Write("game startup");
            LauncherStartupStatus.Set(startupStatus, "Starting game scene...");
            PatchHelper.Log("Invoking NGame.GameStartup");

            var recoveryControls = LauncherStartupRecoveryControlPanel.Show(gameNode);
            LauncherStartupSceneSnapshot.Write(gameNode, "before NGame.GameStartup");
            var startupTask = StartGameStartupAsync(game);
            if (await RecoverIfWatchdogTimedOutAsync(
                    startupTask,
                    game,
                    gameNode,
                    startupStatus,
                    recoveryControls
                ))
            {
                return;
            }

            await startupTask;
            PatchHelper.Log("NGame.GameStartup completed");
            if (!await LauncherGameStartupRecovery.EnsureMainMenuReadyAsync(game, gameNode, startupStatus))
                return;

            LauncherGameStartupRecovery.MarkStartupObserved(recoveryControls, startupStatus, gameNode);
        }
        catch (TargetInvocationException ex)
        {
            var root = ex.InnerException ?? ex;
            LauncherGameStartupRecovery.HandleFailure(gameNode, startupStatus, root);
        }
        catch (Exception ex)
        {
            LauncherGameStartupRecovery.HandleFailure(gameNode, startupStatus, ex);
        }
    }

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

    private static async Task<bool> RecoverIfWatchdogTimedOutAsync(
        Task startupTask,
        object game,
        Node gameNode,
        Label startupStatus,
        CanvasLayer recoveryControls
    )
    {
        var watchdogTask = Task.Delay(StartupWatchdogMs);
        if (await Task.WhenAny(startupTask, watchdogTask) != watchdogTask)
        {
            return false;
        }

        await LauncherGameStartupRecovery.HandleWatchdogAsync(
            game,
            gameNode,
            startupStatus,
            recoveryControls,
            StartupWatchdogMs
        );
        return true;
    }

    private static async Task RunShaderWarmupIfNeededAsync(
        Node gameNode,
        Label startupStatus,
        StartupMode startupMode
    )
    {
        if (ShaderWarmupScreen.NeedsWarmup() && !startupMode.SkipShaderWarmup)
        {
            LauncherStartupMarkerTrace.Write("shader warmup");
            LauncherStartupStatus.Set(startupStatus, "Warming shaders...");
            PatchHelper.Log("Shader warmup starting");

            var warmup = new ShaderWarmupScreen();
            gameNode.AddChild(warmup);
            warmup.Initialize();
            await warmup.WaitForCompletion();
            warmup.QueueFree();

            PatchHelper.Log("Shader warmup complete");
        }
        else if (startupMode.SkipShaderWarmup)
        {
            PatchHelper.Log(startupMode.ShaderWarmupSkipLog);
            LauncherStartupStatus.Set(startupStatus, startupMode.ShaderWarmupSkipStatus);
        }
    }

    private static void ApplyStartupSaveMode(StartupMode startupMode)
    {
        LauncherPreferences.LoadAndApplyCloudSyncEnabled();
        if (!startupMode.ForceLocalSaves)
            return;

        LauncherCloudSaveState.DisableCloudSyncForLaunch();
        PatchHelper.Log(startupMode.LocalSavesReasonLog);
    }

    private sealed class StartupMode
    {
        private readonly string _previousIncompletePhase;

        private StartupMode(string previousIncompletePhase, bool manualSafeLaunch)
        {
            _previousIncompletePhase = previousIncompletePhase;
            ManualSafeLaunch = manualSafeLaunch;
        }

        private bool ManualSafeLaunch { get; }

        private bool ForceLocalSaves
            => ManualSafeLaunch
                || IsPreviousPhase("manual safe launch")
                || IsPreviousPhase("settings and saves")
                || IsPreviousPhase("game startup");

        private bool SkipShaderWarmup
            => ManualSafeLaunch
                || IsPreviousPhase("manual safe launch")
                || IsPreviousPhase("shader warmup");

        private string LocalSavesReasonLog
            => ManualSafeLaunch
                ? "Disabling cloud save injection for manual safe launch"
                : $"Disabling cloud save injection for this launch because previous launch stalled at {_previousIncompletePhase}";

        private string ShaderWarmupSkipLog
            => ManualSafeLaunch
                ? "Skipping shader warmup for manual safe launch"
                : "Skipping shader warmup because the previous launch stalled there";

        private string ShaderWarmupSkipStatus
            => ManualSafeLaunch
                ? "Skipping shader warmup for safe launch..."
                : "Skipping shader warmup after previous stall...";

        private bool IsPreviousPhase(string phase)
            => string.Equals(_previousIncompletePhase, phase, StringComparison.OrdinalIgnoreCase);
    }
}
