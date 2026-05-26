using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile.Launcher;
using STS2Mobile.Steam;

namespace STS2Mobile.Patches;

// Core patches for the mobile launcher flow. Intercepts GameStartupWrapper to show
// the Steam login UI before the game starts, injects cloud save support via SteamKit2,
// and delegates sync logic to CloudSyncCoordinator.
public static class LauncherPatches
{
    private const int StartupWatchdogMs = 60_000;
    internal static bool CloudSyncEnabled = true;
    internal static string SavedAccountName;
    internal static string SavedRefreshToken;

    public static void Apply(Harmony harmony)
    {
        PatchHelper.PatchCritical(
            harmony,
            typeof(NGame),
            "GameStartupWrapper",
            prefix: PatchHelper.Method(typeof(LauncherPatches), nameof(GameStartupWrapperPrefix))
        );

        PatchHelper.Patch(
            harmony,
            typeof(SaveManager),
            "ConstructDefault",
            prefix: PatchHelper.Method(typeof(LauncherPatches), nameof(ConstructDefaultPrefix))
        );

        PatchHelper.PatchCritical(
            harmony,
            typeof(CloudSaveStore),
            "SyncCloudToLocal",
            prefix: PatchHelper.Method(typeof(LauncherPatches), nameof(SyncCloudToLocalPrefix))
        );
    }

    public static bool GameStartupWrapperPrefix(object __instance, ref Task __result)
    {
        __result = RunLauncherThenGame(__instance);
        return false;
    }

    public static bool ConstructDefaultPrefix(ref SaveManager __result)
    {
        PatchHelper.Log(
            $"[Cloud] ConstructDefaultPrefix called. HasToken={SavedRefreshToken != null}, CloudSync={CloudSyncEnabled}"
        );

        if (!CloudSyncEnabled)
        {
            PatchHelper.Log("[Cloud] Cloud sync disabled by user — using local-only SaveManager");
            return true;
        }

        if (SavedAccountName == null || SavedRefreshToken == null)
        {
            PatchHelper.Log("[Cloud] No saved credentials — using local-only SaveManager");
            return true;
        }

        try
        {
            var localStore = new GodotFileIo(UserDataPathProvider.GetAccountScopedBasePath(null));
            var cloudStore = new SteamKit2CloudSaveStore(SavedAccountName, SavedRefreshToken);
            var wrappedStore = new CloudSaveStore(localStore, cloudStore);

            __result = new SaveManager(wrappedStore);
            PatchHelper.Log("[Cloud] Created SaveManager with SteamKit2 cloud store");
            return false;
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"[Cloud] Cloud store injection failed, falling back to local: {ex.Message}"
            );
            return true;
        }
    }

    public static bool SyncCloudToLocalPrefix(
        CloudSaveStore __instance,
        string path,
        ref Task __result
    )
    {
        __result = CloudSyncCoordinator.AutoSyncFileAsync(
            __instance.LocalStore,
            __instance.CloudStore,
            path
        );
        return false;
    }

    private static async Task RunLauncherThenGame(object game)
    {
        var gameNode = (Node)game;

        var launcher = new LauncherUI();
        gameNode.AddChild(launcher);
        launcher.SetGameMode(true);
        launcher.Initialize();
        PatchHelper.Log("Launcher UI displayed");

        await launcher.WaitForLaunch();
        var startupStatus = CreateStartupStatusLabel(gameNode);
        var previousIncompletePhase = ReadStartupMarkerPhase();
        var forceLocalSaves =
            string.Equals(previousIncompletePhase, "settings and saves", StringComparison.OrdinalIgnoreCase)
            || string.Equals(previousIncompletePhase, "game startup", StringComparison.OrdinalIgnoreCase);
        if (forceLocalSaves)
        {
            CloudSyncEnabled = false;
            PatchHelper.Log(
                $"Disabling cloud save injection for this launch because previous launch stalled at {previousIncompletePhase}"
            );
        }
        WriteStartupMarker("launch requested");
        SetStartupStatus(startupStatus, "Starting game...");
        PatchHelper.Log("User launched game, proceeding to startup...");

        var instanceField = typeof(SaveManager).GetField(
            "_instance",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        if (instanceField != null)
        {
            instanceField.SetValue(null, null);
            PatchHelper.Log("[Cloud] Reset SaveManager._instance for cloud store re-injection");
        }

        launcher.QueueFree();
        WriteStartupMarker("launcher closed");
        SetStartupStatus(startupStatus, "Launcher closed. Preparing game startup...");

        var skipShaderWarmup =
            string.Equals(previousIncompletePhase, "shader warmup", StringComparison.OrdinalIgnoreCase);
        if (ShaderWarmupScreen.NeedsWarmup() && !skipShaderWarmup)
        {
            WriteStartupMarker("shader warmup");
            SetStartupStatus(startupStatus, "Warming shaders...");
            PatchHelper.Log("Shader warmup starting");
            var warmup = new ShaderWarmupScreen();
            gameNode.AddChild(warmup);
            warmup.Initialize();
            await warmup.WaitForCompletion();
            warmup.QueueFree();
            PatchHelper.Log("Shader warmup complete");
        }
        else if (skipShaderWarmup)
        {
            PatchHelper.Log("Skipping shader warmup because the previous launch stalled there");
            SetStartupStatus(startupStatus, "Skipping shader warmup after previous stall...");
        }

        WriteStartupMarker("settings and saves");
        SetStartupStatus(
            startupStatus,
            forceLocalSaves
                ? "Loading settings and saves in local-only safe mode..."
                : "Loading settings and saves..."
        );
        PatchHelper.Log("Initializing settings and save manager");
        SaveManager.Instance.InitSettingsData();

        var gameStartup = game.GetType()
            .GetMethod("GameStartup", BindingFlags.NonPublic | BindingFlags.Instance);

        try
        {
            WriteStartupMarker("game startup");
            SetStartupStatus(startupStatus, "Starting game scene...");
            PatchHelper.Log("Invoking NGame.GameStartup");
            var startupTask = (Task)gameStartup.Invoke(game, null);
            var watchdogTask = Task.Delay(StartupWatchdogMs);
            if (await Task.WhenAny(startupTask, watchdogTask) == watchdogTask)
            {
                SetStartupStatus(
                    startupStatus,
                    "Game startup is still running. If this stays black, capture diagnostics."
                );
                PatchHelper.Log(
                    $"Game startup watchdog fired after {StartupWatchdogMs}ms; startup task still running"
                );
            }

            await startupTask;
            PatchHelper.Log("NGame.GameStartup completed");
            ClearStartupMarker();
            startupStatus?.QueueFree();
        }
        catch (TargetInvocationException ex)
        {
            SetStartupStatus(startupStatus, $"Game startup failed: {ex.InnerException?.Message}");
            PatchHelper.Log($"Game startup failed: {ex.InnerException?.Message}");
            throw ex.InnerException ?? ex;
        }
    }

    private static string StartupMarkerPath =>
        Path.Combine(OS.GetDataDir(), "last_game_start_incomplete");

    private static void WriteStartupMarker(string phase)
    {
        try
        {
            File.WriteAllText(
                StartupMarkerPath,
                $"{DateTime.UtcNow:O}\n{phase}\n"
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Failed to write startup marker: {ex.Message}");
        }
    }

    private static string ReadStartupMarkerPhase()
    {
        try
        {
            if (!File.Exists(StartupMarkerPath))
                return null;

            var lines = File.ReadAllLines(StartupMarkerPath);
            return lines.Length >= 2 ? lines[1].Trim() : null;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Failed to read startup marker: {ex.Message}");
            return null;
        }
    }

    private static void ClearStartupMarker()
    {
        try
        {
            if (File.Exists(StartupMarkerPath))
                File.Delete(StartupMarkerPath);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Failed to clear startup marker: {ex.Message}");
        }
    }

    private static Label CreateStartupStatusLabel(Node parent)
    {
        try
        {
            var label = new Label
            {
                Name = "STS2MobileStartupStatus",
                Position = new Vector2(24, 24),
                ZIndex = 4096,
            };
            label.AddThemeFontSizeOverride("font_size", 22);
            label.AddThemeColorOverride("font_color", new Color(0.55f, 0.85f, 1f));
            parent.AddChild(label);
            return label;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Startup status label creation failed: {ex.Message}");
            return null;
        }
    }

    private static void SetStartupStatus(Label label, string message)
    {
        PatchHelper.Log($"[Startup] {message}");
        if (label == null)
            return;

        try
        {
            label.Text = message;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Startup status label update failed: {ex.Message}");
        }
    }
}
