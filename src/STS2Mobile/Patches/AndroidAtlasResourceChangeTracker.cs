using System;

namespace STS2Mobile.Patches;

internal enum AndroidAtlasResourceChangeOutcome
{
    Ignored,
    BaselineRecorded,
    Unchanged,
    Invalidated,
}

internal readonly struct AndroidAtlasResourceChangeResult
{
    private AndroidAtlasResourceChangeResult(
        AndroidAtlasResourceChangeOutcome outcome,
        int removedCount,
        long previousGeneration,
        long generation
    )
    {
        Outcome = outcome;
        RemovedCount = removedCount;
        PreviousGeneration = previousGeneration;
        Generation = generation;
    }

    internal AndroidAtlasResourceChangeOutcome Outcome { get; }
    internal int RemovedCount { get; }
    internal long PreviousGeneration { get; }
    internal long Generation { get; }
    internal bool Invalidated => Outcome == AndroidAtlasResourceChangeOutcome.Invalidated;

    internal static AndroidAtlasResourceChangeResult NoChange(
        AndroidAtlasResourceChangeOutcome outcome,
        long generation
    )
        => new(outcome, 0, generation, generation);

    internal static AndroidAtlasResourceChangeResult FromInvalidation(
        AndroidAtlasFallbackCacheInvalidation invalidation
    )
        => new(
            AndroidAtlasResourceChangeOutcome.Invalidated,
            invalidation.RemovedCount,
            invalidation.PreviousGeneration,
            invalidation.Generation
        );
}

internal sealed class AndroidAtlasResourceChangeTracker
{
    private readonly AndroidAtlasFallbackResolutionCache _cache;
    private readonly object _lock = new();
    private string _mountedResourceSetIdentity;

    internal AndroidAtlasResourceChangeTracker(AndroidAtlasFallbackResolutionCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    internal AndroidAtlasResourceChangeResult RecordResourcePackLoad(
        bool loadSucceeded,
        bool resourcesMayHaveChanged
    )
    {
        lock (_lock)
        {
            if (!loadSucceeded || !resourcesMayHaveChanged)
            {
                return AndroidAtlasResourceChangeResult.NoChange(
                    AndroidAtlasResourceChangeOutcome.Ignored,
                    _cache.CaptureGeneration()
                );
            }

            return AndroidAtlasResourceChangeResult.FromInvalidation(
                _cache.Invalidate()
            );
        }
    }

    internal AndroidAtlasResourceChangeResult ObserveMountedResourceSet(
        string identity
    )
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            return AndroidAtlasResourceChangeResult.NoChange(
                AndroidAtlasResourceChangeOutcome.Ignored,
                _cache.CaptureGeneration()
            );
        }

        lock (_lock)
        {
            if (_mountedResourceSetIdentity == null)
            {
                _mountedResourceSetIdentity = identity;
                return AndroidAtlasResourceChangeResult.NoChange(
                    AndroidAtlasResourceChangeOutcome.BaselineRecorded,
                    _cache.CaptureGeneration()
                );
            }

            if (string.Equals(
                _mountedResourceSetIdentity,
                identity,
                StringComparison.Ordinal
            ))
            {
                return AndroidAtlasResourceChangeResult.NoChange(
                    AndroidAtlasResourceChangeOutcome.Unchanged,
                    _cache.CaptureGeneration()
                );
            }

            _mountedResourceSetIdentity = identity;
            return AndroidAtlasResourceChangeResult.FromInvalidation(
                _cache.Invalidate()
            );
        }
    }

    internal AndroidAtlasResourceChangeResult InvalidateExplicitly()
    {
        lock (_lock)
        {
            return AndroidAtlasResourceChangeResult.FromInvalidation(
                _cache.Invalidate()
            );
        }
    }
}
