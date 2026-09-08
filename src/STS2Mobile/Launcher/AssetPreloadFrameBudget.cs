using System;
using System.Diagnostics;

namespace STS2Mobile.Launcher;

// A resource operation cannot be interrupted. Yield before the next operation
// once either bound is reached, while always allowing one item to make progress.
internal sealed class AssetPreloadFrameBudget
{
    private readonly int _maximumItems;
    private readonly double _maximumMilliseconds;
    private readonly Func<double> _milliseconds;
    private readonly double _started;
    private int _items;

    internal AssetPreloadFrameBudget(int maximumItems, double maximumMilliseconds,
        Func<double> milliseconds = null)
    {
        if (maximumItems < 1) throw new ArgumentOutOfRangeException(nameof(maximumItems));
        if (maximumMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(maximumMilliseconds));
        _maximumItems = maximumItems;
        _maximumMilliseconds = maximumMilliseconds;
        _milliseconds = milliseconds ?? MonotonicMilliseconds;
        _started = _milliseconds();
    }

    internal bool TryBeginItem()
    {
        if (_items >= _maximumItems
            || (_items > 0 && _milliseconds() - _started >= _maximumMilliseconds)) return false;
        _items++;
        return true;
    }

    private static double MonotonicMilliseconds()
        => Stopwatch.GetTimestamp() * (1000.0 / Stopwatch.Frequency);
}
