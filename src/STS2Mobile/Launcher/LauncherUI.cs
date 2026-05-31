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
    private const float ReferenceLongEdge = 960f;
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

        try
        {
            var viewportSize = GetViewportSize();
            SetAnchorsPreset(LayoutPreset.FullRect);
            Size = viewportSize;
            var scale = Math.Max(viewportSize.X, viewportSize.Y) / ReferenceLongEdge;
            _model = new LauncherModel(OS.GetDataDir());
            _model.InGameMode = _inGameMode;
            _view = new LauncherView(this, scale);
            _controller = new LauncherController(
                _model,
                _view,
                EnqueueMainThreadAction
            );

            PatchHelper.Log($"LauncherUI initialized. Viewport={viewportSize}");
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
        _controller.Start();
    }

    internal void SetGameMode(bool inGameMode) => _inGameMode = inGameMode;

    internal Task WaitForLaunch() => _model.WaitForLaunch();

    private void OnProcessFrame()
    {
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
    }

    private Vector2 GetViewportSize()
        => GetViewport()?.GetVisibleRect().Size ?? DefaultViewportSize;
}
