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
            ISaveStore localStore = OperatingSystem.IsAndroid()
                ? new AndroidLocalSaveStore()
                : new GodotFileIo(UserDataPathProvider.GetAccountScopedBasePath(null));
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
        var manualSafeLaunch = ConsumeManualSafeLaunchMarker();
        CloudSyncEnabled = LauncherModel.LoadCloudSyncPref();
        var forceLocalSaves =
            manualSafeLaunch
            || string.Equals(previousIncompletePhase, "manual safe launch", StringComparison.OrdinalIgnoreCase)
            || string.Equals(previousIncompletePhase, "settings and saves", StringComparison.OrdinalIgnoreCase)
            || string.Equals(previousIncompletePhase, "game startup", StringComparison.OrdinalIgnoreCase);
        if (forceLocalSaves)
        {
            CloudSyncEnabled = false;
            PatchHelper.Log(
                manualSafeLaunch
                    ? "Disabling cloud save injection for manual safe launch"
                    : $"Disabling cloud save injection for this launch because previous launch stalled at {previousIncompletePhase}"
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
            manualSafeLaunch
            || string.Equals(previousIncompletePhase, "manual safe launch", StringComparison.OrdinalIgnoreCase)
            || string.Equals(previousIncompletePhase, "shader warmup", StringComparison.OrdinalIgnoreCase);
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
            PatchHelper.Log(
                manualSafeLaunch
                    ? "Skipping shader warmup for manual safe launch"
                    : "Skipping shader warmup because the previous launch stalled there"
            );
            SetStartupStatus(
                startupStatus,
                manualSafeLaunch
                    ? "Skipping shader warmup for safe launch..."
                    : "Skipping shader warmup after previous stall..."
            );
        }

        WriteStartupMarker("settings and saves");
        SetStartupStatus(
            startupStatus,
            forceLocalSaves
                ? "Loading settings and saves in local-only safe mode..."
                : "Loading settings and saves..."
        );
        PatchHelper.Log("Initializing settings and save manager");
        try
        {
            SaveManager.Instance.InitSettingsData();
        }
        catch (Exception ex)
        {
            WriteStartupMarker("settings and saves failed");
            SetStartupStatus(startupStatus, $"Settings/save init failed: {ex.GetBaseException().Message}");
            PatchHelper.Log($"Settings/save init failed: {ex}");
            ShowStartupRecoveryControls(gameNode);
            return;
        }

        var gameStartup = game.GetType()
            .GetMethod("GameStartup", BindingFlags.NonPublic | BindingFlags.Instance);

        try
        {
            WriteStartupMarker("game startup");
            SetStartupStatus(startupStatus, "Starting game scene...");
            PatchHelper.Log("Invoking NGame.GameStartup");
            var recoveryControls = ShowStartupRecoveryControls(gameNode);
            var startupTask = (Task)gameStartup.Invoke(game, null);
            var watchdogTask = Task.Delay(StartupWatchdogMs);
            if (await Task.WhenAny(startupTask, watchdogTask) == watchdogTask)
            {
                WriteStartupMarker("game startup watchdog");
                SetStartupStatus(
                    startupStatus,
                    "Game startup stalled. Use the recovery buttons below."
                );
                PatchHelper.Log(
                    $"Game startup watchdog fired after {StartupWatchdogMs}ms; startup task still running"
                );
                return;
            }

            await startupTask;
            PatchHelper.Log("NGame.GameStartup completed");
            WriteStartupMarker("game startup completed");
            recoveryControls?.QueueFree();
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

    private static bool ConsumeManualSafeLaunchMarker()
    {
        try
        {
            if (!File.Exists(LauncherModel.ManualSafeLaunchPath))
                return false;

            File.Delete(LauncherModel.ManualSafeLaunchPath);
            PatchHelper.Log("Manual safe launch marker consumed");
            return true;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Failed to consume manual safe launch marker: {ex.Message}");
            return true;
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

    private static CanvasLayer ShowStartupRecoveryControls(Node parent)
    {
        try
        {
            if (parent.HasNode("STS2MobileStartupRecovery"))
                return parent.GetNode<CanvasLayer>("STS2MobileStartupRecovery");

            var layer = new CanvasLayer
            {
                Name = "STS2MobileStartupRecovery",
                Layer = 128,
            };
            parent.AddChild(layer);

            var box = new VBoxContainer
            {
                Position = new Vector2(24, 72),
                CustomMinimumSize = new Vector2(820, 0),
            };
            box.AddThemeConstantOverride("separation", 10);
            layer.AddChild(box);

            var title = new Label
            {
                Text = "Game is starting.",
            };
            title.AddThemeFontSizeOverride("font_size", 24);
            title.AddThemeColorOverride("font_color", new Color(0.55f, 0.85f, 1f));
            box.AddChild(title);

            var detail = new Label
            {
                Text = "If this screen does not change, return to the launcher to export diagnostics or restart with safe launch.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            detail.AddThemeFontSizeOverride("font_size", 18);
            detail.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
            box.AddChild(detail);

            var launcherButton = new Button
            {
                Text = "RETURN TO LAUNCHER",
                CustomMinimumSize = new Vector2(420, 56),
            };
            launcherButton.Pressed += () => LauncherModel.GetGodotApp()?.Call("restartApp");
            box.AddChild(launcherButton);

            var safeButton = new Button
            {
                Text = "RESTART WITH SAFE LAUNCH",
                CustomMinimumSize = new Vector2(420, 56),
            };
            safeButton.Pressed += () =>
            {
                try
                {
                    File.WriteAllText(LauncherModel.ManualSafeLaunchPath, $"{DateTime.UtcNow:O}\n");
                }
                catch (Exception ex)
                {
                    PatchHelper.Log($"Failed to write manual safe launch marker from recovery: {ex.Message}");
                }

                LauncherModel.GetGodotApp()?.Call("launchGameSafelyOnRestart");
            };
            box.AddChild(safeButton);
            return layer;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Startup recovery controls failed: {ex.Message}");
            return null;
        }
    }
}
