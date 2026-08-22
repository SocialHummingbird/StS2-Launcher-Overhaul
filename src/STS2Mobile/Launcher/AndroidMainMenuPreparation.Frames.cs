using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

internal static partial class AndroidMainMenuPreparation
{
    private static async Task<(AndroidMainMenuPreparationResult Result, long ElapsedMs)>
        WaitForStableRenderedFramesAsync(
            MainMenuFrameStabilityTracker tracker,
            Node gameNode,
            LauncherMonotonicDeadline overallDeadline,
            LauncherOperationLifecycle lifecycle
        )
    {
        var frameDeadline = overallDeadline.CreateChild(
            System.TimeSpan.FromMilliseconds(MaximumFrameObservationMs)
        );
        var result = await MainMenuRenderedFrameHandoff.WaitAsync(
            tracker,
            LauncherAsyncYield.CreateWaiter(gameNode.GetTree(), lifecycle),
            () => IsPreparationTargetAlive(gameNode),
            () => frameDeadline.ElapsedMilliseconds,
            frameDeadline,
            MaximumFrameObservationMs
        );
        return (result.Result, frameDeadline.ElapsedMilliseconds);
    }
}
