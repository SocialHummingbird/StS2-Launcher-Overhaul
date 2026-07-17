using System;
using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherUI
{
    internal async Task NotifyBootTransitionWhenVisibleAsync()
    {
        if (!OperatingSystem.IsAndroid())
            return;

        try
        {
            var tree = GetTree();
            if (tree == null)
                return;

            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            AndroidGodotAppBridge.NotifyLauncherFirstFrameReady();
            PatchHelper.Log("Android launcher first rendered frame signalled to native boot transition");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Android launcher first-frame signal skipped: {ex.Message}");
        }
    }
}
