using System;
using System.IO;
using System.Reflection;
using System.Text;
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
    private const int PostStartupRecoveryMs = 30_000;
    private const int MainMenuForceTimeoutMs = 15_000;
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

        PatchHelper.PatchCritical(
            harmony,
            typeof(SaveManager),
            "TryFirstTimeCloudSync",
            prefix: PatchHelper.Method(typeof(LauncherPatches), nameof(TryFirstTimeCloudSyncPrefix))
        );

        PatchHelper.PatchCritical(
            harmony,
            typeof(SaveManager),
            "SyncCloudToLocal",
            prefix: PatchHelper.Method(typeof(LauncherPatches), nameof(SaveManagerSyncCloudToLocalPrefix))
        );

        PatchHelper.PatchGetter(
            harmony,
            typeof(NGame),
            "StartOnMainMenu",
            prefix: PatchHelper.Method(typeof(LauncherPatches), nameof(StartOnMainMenuPrefix))
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

    public static bool TryFirstTimeCloudSyncPrefix(ref Task<bool> __result)
    {
        if (!OperatingSystem.IsAndroid())
            return true;

        __result = Task.FromResult(false);
        PatchHelper.Log("[Cloud] Skipping upstream first-time cloud sync on Android");
        return false;
    }

    public static bool SaveManagerSyncCloudToLocalPrefix(ref Task __result)
    {
        if (!OperatingSystem.IsAndroid())
            return true;

        __result = Task.CompletedTask;
        PatchHelper.Log("[Cloud] Skipping upstream startup cloud sync on Android");
        return false;
    }

    public static bool StartOnMainMenuPrefix(ref bool __result)
    {
        if (!OperatingSystem.IsAndroid())
            return true;

        __result = true;
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
            WriteSceneSnapshot(gameNode, "before NGame.GameStartup");
            var startupTask = (Task)gameStartup.Invoke(game, null);
            var watchdogTask = Task.Delay(StartupWatchdogMs);
            if (await Task.WhenAny(startupTask, watchdogTask) == watchdogTask)
            {
                WriteStartupMarker("game startup watchdog");
                WriteSceneSnapshot(gameNode, "game startup watchdog");
                SetStartupStatus(
                    startupStatus,
                    "Game startup stalled. Attempting main menu recovery..."
                );
                PatchHelper.Log(
                    $"Game startup watchdog fired after {StartupWatchdogMs}ms; startup task still running"
                );

                var recovered = await EnsureMainMenuAfterStartup(game, startupStatus);
                WriteSceneSnapshot(
                    gameNode,
                    recovered ? "main menu recovered after watchdog" : "main menu recovery failed after watchdog"
                );
                if (recovered)
                {
                    WriteStartupMarker("main menu recovered after watchdog");
                    SetStartupStatus(
                        startupStatus,
                        "Main menu recovered after startup stall. Recovery controls remain briefly."
                    );
                    SchedulePostStartupRecoveryCleanup(recoveryControls, startupStatus);
                    return;
                }

                WriteStartupMarker("main menu recovery failed after watchdog");
                SetStartupStatus(
                    startupStatus,
                    "Game startup stalled and main menu recovery failed. Use recovery controls below."
                );
                ShowStartupRecoveryControls(gameNode);
                return;
            }

            await startupTask;
            PatchHelper.Log("NGame.GameStartup completed");
            var mainMenuReady = await EnsureMainMenuAfterStartup(game, startupStatus);
            if (!mainMenuReady)
            {
                WriteStartupMarker("main menu guard failed");
                WriteSceneSnapshot(gameNode, "main menu guard failed");
                SetStartupStatus(
                    startupStatus,
                    "Main menu did not load. Use recovery controls below."
                );
                ShowStartupRecoveryControls(gameNode);
                return;
            }

            WriteStartupMarker("post-startup observation");
            WriteSceneSnapshot(gameNode, "after NGame.GameStartup returned");
            SetStartupStatus(
                startupStatus,
                "Game startup returned. Recovery controls remain briefly."
            );
            SchedulePostStartupRecoveryCleanup(recoveryControls, startupStatus);
        }
        catch (TargetInvocationException ex)
        {
            var root = ex.InnerException ?? ex;
            HandleGameStartupFailure(gameNode, startupStatus, root);
        }
        catch (Exception ex)
        {
            HandleGameStartupFailure(gameNode, startupStatus, ex);
        }
    }

    private static void HandleGameStartupFailure(Node gameNode, Label startupStatus, Exception ex)
    {
        var root = ex.GetBaseException();
        var message = $"{root.GetType().Name}: {root.Message}";
        WriteStartupMarker($"game startup failed: {message}");
        WriteSceneSnapshot(gameNode, $"game startup failed: {message}");
        SetStartupStatus(startupStatus, $"Game startup failed: {message}");
        PatchHelper.Log($"Game startup failed: {ex}");
        ShowStartupRecoveryControls(gameNode);
    }

    private static async void SchedulePostStartupRecoveryCleanup(
        CanvasLayer recoveryControls,
        Label startupStatus
    )
    {
        try
        {
            await Task.Delay(PostStartupRecoveryMs);
            ClearStartupMarker();
            recoveryControls?.QueueFree();
            startupStatus?.QueueFree();
            PatchHelper.Log("Post-startup recovery controls cleared; scene snapshot retained");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Post-startup recovery cleanup failed: {ex.Message}");
        }
    }

    private static async Task<bool> EnsureMainMenuAfterStartup(object game, Label startupStatus)
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

            PatchHelper.Log($"Main menu missing after startup; current scene={sceneName ?? "<none>"}. Forcing LoadMainMenu.");
            SetStartupStatus(startupStatus, "Startup returned without main menu. Forcing main menu...");

            var loadMainMenu = game.GetType().GetMethod(
                "LoadMainMenu",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );
            if (loadMainMenu == null)
            {
                PatchHelper.Log("Cannot force main menu: NGame.LoadMainMenu not found");
                return false;
            }

            var task = loadMainMenu.Invoke(game, new object[] { false }) as Task;
            if (task == null)
            {
                PatchHelper.Log("Cannot force main menu: NGame.LoadMainMenu did not return Task");
                return false;
            }

            var timeout = Task.Delay(MainMenuForceTimeoutMs);
            if (await Task.WhenAny(task, timeout) != task)
            {
                PatchHelper.Log($"Forced LoadMainMenu timed out after {MainMenuForceTimeoutMs}ms");
                SetStartupStatus(startupStatus, "Forced main menu load timed out.");
                return false;
            }

            await task;
            var ok = CurrentSceneLooksLikeMainMenu(game, out sceneName);
            PatchHelper.Log(ok
                ? $"Forced main menu load succeeded: {sceneName}"
                : $"Forced main menu load returned but current scene is {sceneName ?? "<none>"}");
            SetStartupStatus(
                startupStatus,
                ok ? "Main menu loaded." : "Main menu force returned, but scene is still not main menu."
            );
            return ok;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"EnsureMainMenuAfterStartup failed: {ex}");
            SetStartupStatus(startupStatus, $"Main menu guard failed: {ex.GetBaseException().Message}");
            return false;
        }
    }

    private static bool CurrentSceneLooksLikeMainMenu(object game, out string sceneName)
    {
        sceneName = null;
        try
        {
            var rootSceneContainer = game.GetType()
                .GetProperty("RootSceneContainer", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(game);
            if (rootSceneContainer == null)
                return false;

            var currentScene = rootSceneContainer.GetType()
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

    private static string StartupMarkerPath =>
        Path.Combine(OS.GetDataDir(), "last_game_start_incomplete");

    private static string StartupSceneSnapshotPath =>
        Path.Combine(OS.GetDataDir(), "last_game_start_scene_tree.txt");

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

    private static void WriteSceneSnapshot(Node root, string reason)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("STS2 startup scene snapshot");
            sb.AppendLine($"UTC: {DateTime.UtcNow:O}");
            sb.AppendLine($"Reason: {reason}");
            sb.AppendLine();
            AppendNodeSnapshot(sb, root, depth: 0, maxDepth: 6);
            File.WriteAllText(StartupSceneSnapshotPath, sb.ToString());
            PatchHelper.Log($"Startup scene snapshot written: {reason}");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Startup scene snapshot failed: {ex.Message}");
        }
    }

    private static void AppendNodeSnapshot(StringBuilder sb, Node node, int depth, int maxDepth)
    {
        if (node == null)
            return;

        var indent = new string(' ', depth * 2);
        sb.Append(indent)
            .Append(node.GetType().FullName)
            .Append(" name=")
            .Append(node.Name)
            .Append(" children=")
            .Append(node.GetChildCount())
            .AppendLine();

        if (depth >= maxDepth)
            return;

        var childCount = node.GetChildCount();
        var limit = Math.Min(childCount, 40);
        for (var i = 0; i < limit; i++)
            AppendNodeSnapshot(sb, node.GetChild(i), depth + 1, maxDepth);

        if (childCount > limit)
            sb.Append(indent).Append("  ... ").Append(childCount - limit).AppendLine(" more children");
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

            var diagnosticsButton = new Button
            {
                Text = "EXPORT STARTUP DIAGNOSTICS",
                CustomMinimumSize = new Vector2(420, 56),
            };
            diagnosticsButton.Pressed += () =>
            {
                try
                {
                    var path = WriteStartupRecoveryDiagnosticsReport();
                    PatchHelper.Log($"Startup recovery diagnostics written: {path}");
                    var shared = (bool)(LauncherModel.GetGodotApp()?.Call("shareTextFile", path) ?? false);
                    detail.Text = shared
                        ? $"Diagnostics exported and share sheet opened.\n\nSaved at:\n{path}"
                        : $"Diagnostics exported, but the share sheet did not open.\n\nSaved at:\n{path}";
                }
                catch (Exception ex)
                {
                    PatchHelper.Log($"Startup recovery diagnostics export failed: {ex}");
                    detail.Text = $"Diagnostics export failed:\n{ex.GetBaseException().Message}";
                }
            };
            box.AddChild(diagnosticsButton);
            return layer;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Startup recovery controls failed: {ex.Message}");
            return null;
        }
    }

    private static string WriteStartupRecoveryDiagnosticsReport()
    {
        var fileName = $"sts2-startup-recovery-diagnostics-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt";
        var path = GetStartupRecoveryDiagnosticsPath(fileName);

        var sb = new StringBuilder();
        sb.AppendLine("STS2 startup recovery diagnostics");
        sb.AppendLine($"Generated UTC: {DateTime.UtcNow:O}");
        sb.AppendLine($"Data dir: {OS.GetDataDir()}");
        sb.AppendLine();

        AppendDiagnosticFile(sb, "Startup marker", StartupMarkerPath);
        AppendDiagnosticFile(sb, "Startup scene snapshot", StartupSceneSnapshotPath);
        AppendDiagnosticFile(sb, "Bootstrap trace", BootstrapTrace.TracePath);

        sb.AppendLine("Android logcat tail:");
        try
        {
            sb.AppendLine((string)LauncherModel.GetGodotApp()?.Call("getLogcatTail", 500) ?? "<unavailable>");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"<logcat unavailable: {ex.Message}>");
        }

        File.WriteAllText(path, sb.ToString());
        return path;
    }

    private static string GetStartupRecoveryDiagnosticsPath(string fileName)
    {
        try
        {
            var externalDir = (string)LauncherModel.GetGodotApp()?.Call("getExternalFilesDirPath");
            if (!string.IsNullOrWhiteSpace(externalDir))
            {
                var dir = Path.Combine(externalDir, "diagnostics");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, fileName);
            }
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Startup recovery external diagnostics path unavailable: {ex.Message}");
        }

        return Path.Combine(OS.GetDataDir(), fileName);
    }

    private static void AppendDiagnosticFile(StringBuilder sb, string label, string path)
    {
        sb.AppendLine($"{label}: {path}");
        try
        {
            if (!File.Exists(path))
            {
                sb.AppendLine("  exists=false");
                sb.AppendLine();
                return;
            }

            var file = new FileInfo(path);
            sb.AppendLine($"  exists=true bytes={file.Length} modifiedUtc={file.LastWriteTimeUtc:O}");
            sb.AppendLine("  contents:");
            sb.AppendLine(File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            sb.AppendLine($"  failed={ex.Message}");
        }

        sb.AppendLine();
    }
}
