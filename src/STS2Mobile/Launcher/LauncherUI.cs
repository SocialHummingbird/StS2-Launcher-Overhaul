using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

// Thin wrapper Control that initializes the MVC launcher components and
// processes a main-thread action queue so SteamKit callbacks can update the UI.
internal sealed class LauncherUI : Control
{
    private const string AutoLaunchVariable = "STS2_AUTO_LAUNCH_GAME";
    private const string AutoSafeLaunchVariable = "STS2_AUTO_SAFE_LAUNCH";
    private const int LauncherZIndex = 100;
    private static readonly Vector2 DefaultViewportSize = new(1920, 1080);

    private readonly ConcurrentQueue<Action> _mainThreadActions = new();
    private LauncherModel _model;
    private LauncherView _view;
    private LauncherController _controller;
    private Vector2 _lastViewportSize;
    private bool _inGameMode;

    internal void Initialize()
    {
        ZIndex = LauncherZIndex;
        AndroidBridgeDispatcher.RegisterCurrentThread();

        try
        {
            var viewportSize = GetViewportSize();
            _lastViewportSize = viewportSize;
            SetAnchorsPreset(LayoutPreset.FullRect);
            Size = viewportSize;
            var layoutProfile = LauncherLayoutProfile.ForViewport(viewportSize);
            _model = new LauncherModel(ResolveLauncherDataDirectory());
            _model.InGameMode = _inGameMode;
            _view = new LauncherView(this, layoutProfile);
            _controller = new LauncherController(
                _model,
                _view,
                EnqueueMainThreadAction
            );

            PatchHelper.Log($"LauncherUI initialized. {layoutProfile}");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"BuildUI FAILED: {ex}");
            return;
        }

        var tree = GetTree();
        tree.AutoAcceptQuit = false;
        tree.ProcessFrame += OnProcessFrame;
        TreeExiting += OnExitTree;
        Callable.From(StartControllerSafely).CallDeferred();
    }

    private void StartControllerSafely()
    {
        try
        {
            PatchHelper.Log("Launcher controller starting");
            _controller.Start();
            PatchHelper.Log("Launcher controller started");
            AutoLaunchIfRequested();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Launcher controller startup FAILED: {ex}");
            _view?.SetStatus("Launcher startup failed. Diagnostics are available below.");
            _view?.AppendLog(ex.ToString());
        }
    }

    internal void SetGameMode(bool inGameMode) => _inGameMode = inGameMode;

    internal Task WaitForLaunch() => _model.WaitForLaunch();

    private void OnProcessFrame()
    {
        AndroidBridgeDispatcher.Pump();
        DrainMainThreadActions();
        SyncViewportSize();
        _view?.UpdateKeyboardOffset();
    }

    private void SyncViewportSize()
    {
        var viewportSize = GetViewportSize();
        if (_lastViewportSize.DistanceSquaredTo(viewportSize) <= 1f)
            return;

        _lastViewportSize = viewportSize;
        Size = viewportSize;
        _view?.UpdateViewportSize(viewportSize);
    }

    private void EnqueueMainThreadAction(Action action) => _mainThreadActions.Enqueue(action);

    private void DrainMainThreadActions()
    {
        while (_mainThreadActions.TryDequeue(out var action))
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"UI update error: {ex.Message}");
            }
        }
    }

    private void OnExitTree()
    {
        var tree = GetTree();
        tree.ProcessFrame -= OnProcessFrame;
        tree.AutoAcceptQuit = true;
        _model?.Dispose();
        if (!_inGameMode)
            AndroidBridgeDispatcher.UnregisterCurrentThread();
    }

    private Vector2 GetViewportSize()
        => GetViewport()?.GetVisibleRect().Size ?? DefaultViewportSize;

    private static string ResolveLauncherDataDirectory()
    {
        if (OperatingSystem.IsAndroid())
        {
            try
            {
                var bridgedFilesDir = AndroidGodotAppBridge.GetInternalFilesDirPath();
                if (BootstrapTrace.TryNormalizeDirectory(bridgedFilesDir, out var normalizedBridgedFilesDir))
                    return normalizedBridgedFilesDir;
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"Launcher internal files dir bridge unavailable: {ex.Message}");
            }

            var androidFilesDir = System.Environment.GetEnvironmentVariable("STS2_ANDROID_FILES_DIR");
            if (BootstrapTrace.TryNormalizeDirectory(androidFilesDir, out var normalizedAndroidFilesDir))
                return normalizedAndroidFilesDir;

            return BootstrapTrace.ResolveFallbackDataDirectory();
        }

        try
        {
            var dataDir = OS.GetDataDir();
            if (BootstrapTrace.TryNormalizeDirectory(dataDir, out var normalized))
                return normalized;
        }
        catch
        {
        }

        return BootstrapTrace.ResolveFallbackDataDirectory();
    }

    private void AutoLaunchIfRequested()
    {
        if (!_inGameMode)
            return;

        if (
            !string.Equals(
                System.Environment.GetEnvironmentVariable(AutoLaunchVariable),
                "1",
                StringComparison.Ordinal
            )
        )
            return;

        System.Environment.SetEnvironmentVariable(AutoLaunchVariable, "0");
        var safeLaunch = string.Equals(
            System.Environment.GetEnvironmentVariable(AutoSafeLaunchVariable),
            "1",
            StringComparison.Ordinal
        );
        System.Environment.SetEnvironmentVariable(AutoSafeLaunchVariable, "0");
        PatchHelper.Log(
            safeLaunch
                ? "Auto-safe-launching downloaded game from launch request."
                : "Auto-launching downloaded game from launch request."
        );
        if (safeLaunch)
            _model.LaunchSafe();
        else
            _model.Launch();
    }
}
