using System;
using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private void Initialize()
    {
        ZIndex = 100;
        WriteWarmupStatus("initializing", "Building shader warmup screen");

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
            WriteWarmupStatus("ui-build-failed", ex.GetBaseException().Message);
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
        _warmupFinished = false;
        _ = WatchWarmupDurationAsync();
        var deadline = LauncherMonotonicDeadline.Start(
            TimeSpan.FromSeconds(WarmupTimeBudgetSeconds)
        );

        try
        {
            WriteWarmupStatus("running", "Shader warmup task started");
            await RunWarmupAsync(deadline);
        }
        catch (Exception ex)
        {
            WriteWarmupStatus("failed", ex.GetBaseException().Message);
            PatchHelper.Log(Message.RunFailed(ex));
        }
        finally
        {
            _warmupFinished = true;
        }

        _tcs?.TrySetResult(true);
    }

    private async Task WatchWarmupDurationAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(WatchdogWarningSeconds));
        if (_warmupFinished)
            return;

        WriteWarmupStatus(
            "watchdog-warning",
            $"Shader warmup still active after {WatchdogWarningSeconds}s"
        );
        PatchHelper.Log(Message.WatchdogWarning(WatchdogWarningSeconds));
    }
}
