using System;

namespace STS2Mobile.Launcher;

internal sealed class LauncherMonotonicDeadline
{
    private readonly Func<long> _clock;
    private readonly long _deadlineAtMilliseconds;
    private readonly long _startedAtMilliseconds;

    private LauncherMonotonicDeadline(
        TimeSpan budget,
        Func<long> clock,
        long latestDeadlineAtMilliseconds
    )
    {
        _clock = clock;
        _startedAtMilliseconds = clock();
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
        => Math.Max(0L, _clock() - _startedAtMilliseconds);

    internal bool IsExpired => _clock() >= _deadlineAtMilliseconds;

    internal int RemainingDelayMilliseconds
    {
        get
        {
            var remaining = Math.Max(0L, _deadlineAtMilliseconds - _clock());
            return (int)Math.Min(int.MaxValue, remaining);
        }
    }

    internal LauncherMonotonicDeadline CreateChild(TimeSpan budget)
        => new(budget, _clock, _deadlineAtMilliseconds);

    internal static LauncherMonotonicDeadline Start(TimeSpan budget)
        => new(budget, () => Environment.TickCount64, long.MaxValue);
}
