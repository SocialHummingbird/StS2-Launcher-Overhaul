using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private async Task WaitPostDrawAsync(LauncherMonotonicDeadline deadline)
    {
        await LauncherAsyncYield.FramePostDrawAsync(deadline);
    }

    private async Task WaitFinishDelayAsync(LauncherMonotonicDeadline deadline)
    {
        await LauncherAsyncYield.DelayAsync(TimeSpan.FromSeconds(0.5), deadline);
    }
}
