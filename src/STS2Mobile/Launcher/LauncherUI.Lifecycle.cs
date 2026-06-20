using System;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherUI
{
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

    private void OnExitTree()
    {
        var tree = GetTree();
        tree.ProcessFrame -= OnProcessFrame;
        tree.AutoAcceptQuit = true;
        _model?.Dispose();
        if (!_inGameMode)
            AndroidBridgeDispatcher.UnregisterCurrentThread();
    }
}
