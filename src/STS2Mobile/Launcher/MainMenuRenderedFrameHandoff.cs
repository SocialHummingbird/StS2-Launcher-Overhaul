using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal static class MainMenuRenderedFrameHandoff
{
    internal static async Task<(AndroidMainMenuPreparationResult Result, long ElapsedMs)>
        WaitAsync(
            MainMenuFrameStabilityTracker tracker,
            LauncherAsyncSignalWaiter waiter,
            Func<bool> isTargetAlive,
            Func<long> elapsedMilliseconds,
            LauncherMonotonicDeadline deadline,
            int maximumObservationMilliseconds
        )
    {
        while (!deadline.IsExpired)
        {
            if (!isTargetAlive())
                return Abort(tracker, elapsedMilliseconds());

            var frame = await waiter.WaitForFramePostDrawAsync(deadline);
            if (frame == LauncherAsyncWaitOutcome.Destroyed || !isTargetAlive())
                return Abort(tracker, elapsedMilliseconds());
            if (frame == LauncherAsyncWaitOutcome.DeadlineExceeded)
                break;

            var elapsed = elapsedMilliseconds();
            if (tracker.ObserveRenderedFrame(elapsed))
                return (AndroidMainMenuPreparationResult.Stable(), elapsed);
        }

        if (!isTargetAlive())
            return Abort(tracker, elapsedMilliseconds());

        tracker.TryTimeout(maximumObservationMilliseconds);
        return (
            AndroidMainMenuPreparationResult.TimedOut(),
            elapsedMilliseconds()
        );
    }

    private static (AndroidMainMenuPreparationResult Result, long ElapsedMs) Abort(
        MainMenuFrameStabilityTracker tracker,
        long elapsedMilliseconds
    )
    {
        tracker.TryAbort();
        return (AndroidMainMenuPreparationResult.Aborted(), elapsedMilliseconds);
    }
}
