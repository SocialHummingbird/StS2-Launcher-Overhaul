using System;

namespace STS2Mobile.Launcher;

internal sealed class LauncherMonotonicDeadline
{
    private readonly PausableMonotonicClock _clock;
    private readonly long _deadlineAtMilliseconds;
    private readonly long _startedAtMilliseconds;

    private LauncherMonotonicDeadline(
        TimeSpan budget,
        PausableMonotonicClock clock,
        long latestDeadlineAtMilliseconds
    )
    {
        _clock = clock;
        _startedAtMilliseconds = clock.Now;
        var budgetMilliseconds = Math.Max(
            1L,
            (long)Math.Ceiling(budget.TotalMilliseconds)
        );
        var requestedDeadline = _startedAtMilliseconds > long.MaxValue - budgetMilliseconds
            ? long.MaxValue
            : _startedAtMilliseconds + budgetMilliseconds;
        _deadlineAtMilliseconds = Math.Min(
            requestedDeadline,
            latestDeadlineAtMilliseconds
        );
    }

    internal long ElapsedMilliseconds
        => Math.Max(0L, _clock.Now - _startedAtMilliseconds);

    internal bool IsExpired => _clock.Now >= _deadlineAtMilliseconds;

    internal bool IsPaused => _clock.IsPaused;

    internal int RemainingDelayMilliseconds
    {
        get
        {
            var remaining = Math.Max(0L, _deadlineAtMilliseconds - _clock.Now);
            return (int)Math.Min(int.MaxValue, remaining);
        }
    }

    internal LauncherMonotonicDeadline CreateChild(TimeSpan budget)
        => new(budget, _clock, _deadlineAtMilliseconds);

    internal bool Pause() => _clock.Pause();

    internal bool Resume() => _clock.Resume();

    internal static LauncherMonotonicDeadline Start(TimeSpan budget)
        => Start(budget, () => Environment.TickCount64);

    internal static LauncherMonotonicDeadline Start(
        TimeSpan budget,
        Func<long> clock
    )
        => new(
            budget,
            new PausableMonotonicClock(
                clock ?? throw new ArgumentNullException(nameof(clock))
            ),
            long.MaxValue
        );

    private sealed class PausableMonotonicClock
    {
        private readonly object _lock = new();
        private readonly Func<long> _source;
        private bool _paused;
        private long _pausedAt;
        private long _pausedDuration;

        internal PausableMonotonicClock(Func<long> source)
        {
            _source = source;
        }

        internal long Now
        {
            get
            {
                lock (_lock)
                {
                    var raw = _paused ? _pausedAt : _source();
                    return raw - _pausedDuration;
                }
            }
        }

        internal bool IsPaused
        {
            get
            {
                lock (_lock)
                    return _paused;
            }
        }

        internal bool Pause()
        {
            lock (_lock)
            {
                if (_paused)
                    return false;

                _pausedAt = _source();
                _paused = true;
                return true;
            }
        }

        internal bool Resume()
        {
            lock (_lock)
            {
                if (!_paused)
                    return false;

                var resumedAt = _source();
                _pausedDuration += Math.Max(0L, resumedAt - _pausedAt);
                _paused = false;
                return true;
            }
        }
    }
}
