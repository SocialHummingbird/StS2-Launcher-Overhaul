using System;
using System.Collections.Generic;

namespace STS2Mobile.Patches;

internal readonly struct AndroidAtlasFallbackResolution
{
    internal AndroidAtlasFallbackResolution(bool found, string fallback)
    {
        Found = found;
        Fallback = fallback;
    }

    internal bool Found { get; }
    internal string Fallback { get; }
}

internal sealed class AndroidAtlasFallbackResolutionCache
{
    private readonly Dictionary<string, AndroidAtlasFallbackResolution> _entries =
        new(StringComparer.Ordinal);
    private readonly object _lock = new();
    private long _generation;

    internal long CaptureGeneration()
    {
        lock (_lock)
            return _generation;
    }

    internal int Count
    {
        get
        {
            lock (_lock)
                return _entries.Count;
        }
    }

    internal bool TryGet(
        string path,
        long expectedGeneration,
        out AndroidAtlasFallbackResolution resolution
    )
    {
        lock (_lock)
        {
            if (_generation != expectedGeneration)
            {
                resolution = default;
                return false;
            }

            return _entries.TryGetValue(path, out resolution);
        }
    }

    internal bool TryStoreHit(
        string path,
        string fallback,
        long expectedGeneration
    )
    {
        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(fallback))
            return false;

        lock (_lock)
        {
            if (_generation != expectedGeneration)
                return false;

            _entries[path] = new AndroidAtlasFallbackResolution(true, fallback);
            return true;
        }
    }

    internal bool TryStoreMiss(string path, long expectedGeneration)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        lock (_lock)
        {
            if (_generation != expectedGeneration)
                return false;

            _entries[path] = new AndroidAtlasFallbackResolution(false, string.Empty);
            return true;
        }
    }

    internal bool TryRemove(
        string path,
        string expectedFallback,
        long expectedGeneration
    )
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(path)
                || _generation != expectedGeneration
                || !_entries.TryGetValue(path, out var current)
                || !current.Found
                || !string.Equals(
                    current.Fallback,
                    expectedFallback,
                    StringComparison.Ordinal
                ))
            {
                return false;
            }

            return _entries.Remove(path);
        }
    }

    internal bool IsCurrent(long expectedGeneration)
    {
        lock (_lock)
            return _generation == expectedGeneration;
    }

    internal AndroidAtlasFallbackCacheInvalidation Invalidate()
    {
        lock (_lock)
        {
            int removed = _entries.Count;
            _entries.Clear();
            long previousGeneration = _generation;
            _generation = checked(_generation + 1);
            return new AndroidAtlasFallbackCacheInvalidation(
                removed,
                previousGeneration,
                _generation
            );
        }
    }
}

internal readonly struct AndroidAtlasFallbackCacheInvalidation
{
    internal AndroidAtlasFallbackCacheInvalidation(
        int removedCount,
        long previousGeneration,
        long generation
    )
    {
        RemovedCount = removedCount;
        PreviousGeneration = previousGeneration;
        Generation = generation;
    }

    internal int RemovedCount { get; }
    internal long PreviousGeneration { get; }
    internal long Generation { get; }
}
