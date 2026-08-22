using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class LauncherLaunchReadinessCache
{
    private static readonly object Gate = new();
    private static CacheEntry _cached;

    internal static bool TryGet(
        string dataDir,
        string branch,
        string phase,
        out LauncherLaunchReadiness readiness
    )
    {
        readiness = null;
        branch = SteamGameBranch.Normalize(branch);
        CacheEntry cached;
        lock (Gate)
        {
            cached = _cached;
            if (cached == null)
            {
                LauncherLaunchMarkers.RecordPhase(
                    $"{phase}: launch readiness cache miss",
                    $"branch={branch}; reason=empty"
                );
                return false;
            }

            if (!cached.Key.TargetMatches(dataDir, branch))
            {
                LauncherLaunchMarkers.RecordPhase(
                    $"{phase}: launch readiness cache miss",
                    $"branch={branch}; reason=target changed"
                );
                return false;
            }
        }

        bool matches;
        string mismatchReason;
        try
        {
            matches = cached.Key.MatchesCurrent(dataDir, branch, out mismatchReason);
        }
        catch (Exception ex)
        {
            var clearedStaleCache = ClearSnapshot(cached, dataDir, branch);
            LauncherLaunchMarkers.RecordPhase(
                $"{phase}: launch readiness cache miss",
                $"branch={branch}; reason=cache validation failed; clearedStaleCache={clearedStaleCache}; {ExceptionDetail(ex)}"
            );
            return false;
        }

        lock (Gate)
        {
            if (_cached != cached)
            {
                LauncherLaunchMarkers.RecordPhase(
                    $"{phase}: launch readiness cache miss",
                    $"branch={branch}; reason=cache changed"
                );
                return false;
            }

            if (!matches)
            {
                _cached = null;
                LauncherLaunchMarkers.RecordPhase(
                    $"{phase}: launch readiness cache miss",
                    $"branch={branch}; reason={mismatchReason}; clearedStaleCache=true"
                );
                return false;
            }

            readiness = _cached.Readiness.WithCacheStatus(
                phase,
                LauncherLaunchReadinessCacheStatus.MemoryCacheHit
            );
        }

        LauncherLaunchMarkers.RecordPhase(
            $"{phase}: launch readiness cache hit",
            $"branch={readiness.Branch}; ready={readiness.Ready}; gameIdentity={readiness.GameIdentityId}"
        );
        return true;
    }

    internal static void Store(string dataDir, LauncherLaunchReadiness readiness)
    {
        if (readiness == null)
            return;

        if (OperatingSystem.IsAndroid())
        {
            LauncherLaunchMarkers.RecordPhase(
                $"{readiness.EvaluationPhase}: launch readiness cache store skipped",
                $"branch={readiness.Branch}; reason=android runtime cache identity disabled; readiness remains validated by runtime slot evidence marker"
            );
            return;
        }

        if (!IsCacheable(readiness))
        {
            var clearedMatchingCache = ClearMatchingTarget(dataDir, readiness.Branch);
            LauncherLaunchMarkers.RecordPhase(
                $"{readiness.EvaluationPhase}: launch readiness cache store skipped",
                $"branch={readiness.Branch}; reason=not inspected full-runtime readiness; status={readiness.CacheStatus}; hasRuntimeSlot={readiness.HasRuntimeSlot}; clearedMatchingCache={clearedMatchingCache}"
            );
            return;
        }

        try
        {
            var key = LauncherLaunchReadinessCacheKey.Create(dataDir, readiness);
            lock (Gate)
            {
                _cached = new CacheEntry(
                    key,
                    readiness.WithCacheStatus(
                        readiness.EvaluationPhase,
                        LauncherLaunchReadinessCacheStatus.FreshCached
                    )
                );
            }
        }
        catch (Exception ex)
        {
            LauncherLaunchMarkers.RecordPhase(
                $"{readiness.EvaluationPhase}: launch readiness cache store skipped",
                $"branch={readiness.Branch}; {ExceptionDetail(ex)}"
            );
        }
    }

    private static bool IsCacheable(LauncherLaunchReadiness readiness)
        => string.Equals(
                readiness.CacheStatus,
                LauncherLaunchReadinessCacheStatus.Fresh,
                StringComparison.Ordinal
            )
            && readiness.HasRuntimeSlot;

    private static bool ClearMatchingTarget(string dataDir, string branch)
    {
        lock (Gate)
        {
            if (_cached == null || !_cached.Key.TargetMatches(dataDir, branch))
                return false;

            _cached = null;
            return true;
        }
    }

    private static bool ClearSnapshot(CacheEntry cached, string dataDir, string branch)
    {
        lock (Gate)
        {
            if (_cached != cached || cached == null || !cached.Key.TargetMatches(dataDir, branch))
                return false;

            _cached = null;
            return true;
        }
    }

    internal static void Clear(string reason)
    {
        lock (Gate)
        {
            _cached = null;
        }

        LauncherLaunchMarkers.RecordPhase(
            "launch readiness cache cleared",
            string.IsNullOrWhiteSpace(reason) ? "<none>" : reason
        );
    }

    private static string ExceptionDetail(Exception exception)
    {
        var name = exception?.GetType().Name ?? "Exception";
        var message = string.IsNullOrWhiteSpace(exception?.Message)
            ? "No exception message was provided."
            : exception.Message.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return $"exception={name}; message={message}";
    }

    private sealed class CacheEntry
    {
        internal CacheEntry(LauncherLaunchReadinessCacheKey key, LauncherLaunchReadiness readiness)
        {
            Key = key;
            Readiness = readiness;
        }

        internal LauncherLaunchReadinessCacheKey Key { get; }
        internal LauncherLaunchReadiness Readiness { get; }
    }
}
