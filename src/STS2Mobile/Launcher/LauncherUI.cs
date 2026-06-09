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
    private bool _inGameMode;

    internal void Initialize()
    {
        ZIndex = LauncherZIndex;
        AndroidBridgeDispatcher.RegisterCurrentThread();

        try
        {
            var viewportSize = GetViewportSize();
            SetAnchorsPreset(LayoutPreset.FullRect);
            Size = viewportSize;
            var layoutProfile = LauncherLayoutProfile.ForViewport(viewportSize);
            _model = new LauncherModel(OS.GetDataDir());
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
        try
        {
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
        _view?.UpdateKeyboardOffset();
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
        AndroidBridgeDispatcher.UnregisterCurrentThread();
    }

    private Vector2 GetViewportSize()
        => GetViewport()?.GetVisibleRect().Size ?? DefaultViewportSize;

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
