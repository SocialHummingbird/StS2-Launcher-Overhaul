using System;
using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private void Initialize()
    {
        ZIndex = 100;

        try
        {
            var vpSize = GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920, 1080);
            SetAnchorsPreset(LayoutPreset.FullRect);
            Size = vpSize;
            BuildUI(vpSize);
            PatchHelper.Log(Message.ScreenInitialized);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(Message.ScreenBuildFailed(ex));
            _tcs?.TrySetResult(false);
            return;
        }

        Callable.From(RunWarmup).CallDeferred();
    }

    private void RunWarmup()
        => _ = RunWarmupTaskAsync();

    private async Task RunWarmupTaskAsync()
    {
        try
        {
            await RunWarmupAsync();
        }
        catch (Exception ex)
        {
            PatchHelper.Log(Message.RunFailed(ex));
        }

        _tcs?.TrySetResult(true);
    }
}
