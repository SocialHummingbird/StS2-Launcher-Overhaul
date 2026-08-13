using System;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherUI
{
    internal bool Initialize()
    {
        LauncherLaunchMarkers.RecordPhase("launcher ui initialize", "Building managed launcher UI");
        ZIndex = LauncherZIndex;
        AndroidBridgeDispatcher.RegisterCurrentThread();
        if (OperatingSystem.IsAndroid())
        {
            AndroidGodotAppBridge.NotifyLauncherUiActive(true);
            _launcherImeActiveSignalled = true;
        }

        try
        {
            var viewportSize = GetViewportSize();
            _lastViewportSize = viewportSize;
            SetAnchorsPreset(LayoutPreset.FullRect);
            Size = viewportSize;
            var layoutProfile = LauncherLayoutProfile.ForViewport(viewportSize);
            var dataDir = ResolveLauncherDataDirectory();
            var powerVrCompatibility = LauncherGraphicsDeviceEvidence.CaptureAndApplyCompatibility(dataDir);
            _model = new LauncherModel(dataDir);
            _model.InGameMode = _inGameMode;
            _view = new LauncherView(this, layoutProfile);
            _view.SetPowerVrCompatibility(powerVrCompatibility);
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
            return false;
        }

        var tree = GetTree();
        tree.AutoAcceptQuit = false;
        tree.ProcessFrame += OnProcessFrame;
        TreeExiting += OnExitTree;
        Callable.From(StartControllerSafely).CallDeferred();
        return true;
    }

    private void StartControllerSafely()
    {
        try
        {
            LauncherLaunchMarkers.RecordPhase("launcher controller starting");
            PatchHelper.Log("Launcher controller starting");
            _controller.Start();
            LauncherLaunchMarkers.RecordPhase("launcher controller started");
            PatchHelper.Log("Launcher controller started");
            AutoLaunchIfRequested();
        }
        catch (Exception ex)
        {
            LauncherLaunchMarkers.RecordPhase("launcher controller failed", ex.GetBaseException().Message);
            PatchHelper.Log($"Launcher controller startup FAILED: {ex}");
            _view?.SetStatus(
                "Launcher startup failed. Diagnostics are available below.",
                LauncherStatusSeverity.Error
            );
            _view?.AppendLog(ex.ToString());
        }
    }

    private void OnExitTree()
    {
        _controller?.CancelSaveSyncForLauncherExit();
        if (_launcherImeActiveSignalled)
        {
            AndroidGodotAppBridge.NotifyLauncherUiActive(false);
            _launcherImeActiveSignalled = false;
        }
        var tree = GetTree();
        tree.ProcessFrame -= OnProcessFrame;
        tree.AutoAcceptQuit = true;
        _model?.Dispose();
        if (!_inGameMode)
            AndroidBridgeDispatcher.UnregisterCurrentThread();
    }
}
