using System;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherUI
{
    internal void Initialize()
    {
        LauncherLaunchMarkers.RecordPhase("launcher ui initialize", "Building managed launcher UI");
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

            LauncherLaunchMarkers.RecordPhase("launcher ui ready", layoutProfile.ToString());
            PatchHelper.Log($"LauncherUI initialized. {layoutProfile}");
        }
        catch (Exception ex)
        {
            LauncherLaunchMarkers.RecordPhase("launcher ui failed", ex.GetBaseException().Message);
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
            LauncherLaunchMarkers.RecordPhase("launcher controller starting");
            PatchHelper.Log("Launcher controller starting");
            var automationStarted = _controller.Start();
            LauncherLaunchMarkers.RecordPhase("launcher controller started", $"automationStarted={automationStarted}");
            PatchHelper.Log("Launcher controller started");
            AutoLaunchIfRequested(automationStarted);
        }
        catch (Exception ex)
        {
            LauncherLaunchMarkers.RecordPhase("launcher controller failed", ex.GetBaseException().Message);
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
