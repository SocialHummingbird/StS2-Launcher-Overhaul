using System;

namespace STS2Mobile.Launcher;

internal enum MainMenuFrameStabilityOutcome
{
    Pending,
    Stable,
    TimedOut,
    Aborted,
}

internal sealed class MainMenuFrameStabilityTracker
{
    private readonly int _requiredStableFrames;
    private readonly int _stableFrameThresholdMs;
    private readonly int _minimumObservationMs;
    private readonly int _maximumObservationMs;
    private long _totalFrameIntervalMs;
    private long _previousFramePostDrawAtMs;

    internal MainMenuFrameStabilityTracker(
        int requiredStableFrames,
        int stableFrameThresholdMs,
        int minimumObservationMs,
        int maximumObservationMs
    )
    {
        _requiredStableFrames = Math.Max(1, requiredStableFrames);
        _stableFrameThresholdMs = Math.Max(1, stableFrameThresholdMs);
        _minimumObservationMs = Math.Max(0, minimumObservationMs);
        _maximumObservationMs = Math.Max(
            _minimumObservationMs,
            maximumObservationMs
        );
    }

    internal int ConsecutiveStableFrames { get; private set; }
    internal int FramePostDrawSamples { get; private set; }
    internal long LatestFrameIntervalMs { get; private set; }
    internal int SlowFrames { get; private set; }
    internal long MaximumFrameIntervalMs { get; private set; }
    internal MainMenuFrameStabilityOutcome Outcome { get; private set; }

    internal long AverageFrameIntervalMs
        => FramePostDrawSamples > 0
            ? _totalFrameIntervalMs / FramePostDrawSamples
            : 0;

    internal bool ObserveRenderedFrame(long framePostDrawAtMs)
    {
        if (Outcome != MainMenuFrameStabilityOutcome.Pending)
            return false;

        long normalizedElapsed = Math.Max(0, framePostDrawAtMs);
        long interval = Math.Max(0, normalizedElapsed - _previousFramePostDrawAtMs);
        FramePostDrawSamples++;
        LatestFrameIntervalMs = interval;
        _totalFrameIntervalMs += interval;
        MaximumFrameIntervalMs = Math.Max(MaximumFrameIntervalMs, interval);
        if (interval <= _stableFrameThresholdMs)
            ConsecutiveStableFrames++;
        else
        {
            SlowFrames++;
            ConsecutiveStableFrames = 0;
        }

        _previousFramePostDrawAtMs = normalizedElapsed;
        if (!MeetsStableThreshold(normalizedElapsed))
            return false;

        Outcome = MainMenuFrameStabilityOutcome.Stable;
        return true;
    }

    internal bool TryTimeout(long elapsedMs)
    {
        if (Outcome != MainMenuFrameStabilityOutcome.Pending
            || elapsedMs < _maximumObservationMs)
            return false;

        Outcome = MainMenuFrameStabilityOutcome.TimedOut;
        return true;
    }

    internal bool TryAbort()
    {
        if (Outcome != MainMenuFrameStabilityOutcome.Pending)
            return false;

        Outcome = MainMenuFrameStabilityOutcome.Aborted;
        return true;
    }

    private bool MeetsStableThreshold(long elapsedMs)
        => elapsedMs >= _minimumObservationMs
            && ConsecutiveStableFrames >= _requiredStableFrames;

}
