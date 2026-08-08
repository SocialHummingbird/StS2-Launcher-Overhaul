namespace STS2Mobile.Launcher;

internal static class LauncherModLaunchReadinessCache
{
    private static readonly object Gate = new();
    private static CacheEntry _cached;

    internal static bool TryGet(
        LauncherModSourceIdentity identity,
        string phase,
        out LauncherModLaunchReadiness readiness
    )
    {
        readiness = null;
        var key = identity ?? LauncherModSourceIdentity.Create();
        lock (Gate)
        {
            if (_cached == null || !_cached.Identity.Matches(key))
            {
                LauncherLaunchMarkers.RecordPhase(
                    $"{phase}: mod readiness cache miss",
                    $"selection={key.SelectionIdentity}"
                );
                return false;
            }

            readiness = _cached.Readiness.WithCacheStatus(
                phase,
                LauncherModLaunchReadinessCacheStatus.MemoryCacheHit
            );
        }

        LauncherLaunchMarkers.RecordPhase(
            $"{phase}: mod readiness cache hit",
            readiness.Summary
        );
        return true;
    }

    internal static void Store(
        LauncherModSourceIdentity identity,
        LauncherModLaunchReadiness readiness
    )
    {
        if (readiness == null)
            return;

        lock (Gate)
        {
            _cached = new CacheEntry(
                identity ?? LauncherModSourceIdentity.Create(),
                readiness.WithCacheStatus(
                    readiness.Phase,
                    LauncherModLaunchReadinessCacheStatus.FreshCached
                )
            );
        }
    }

    internal static void Clear(string reason)
    {
        lock (Gate)
        {
            _cached = null;
        }

        LauncherLaunchMarkers.RecordPhase(
            "mod launch readiness cache cleared",
            string.IsNullOrWhiteSpace(reason) ? "<none>" : reason
        );
    }

    private sealed class CacheEntry
    {
        internal CacheEntry(
            LauncherModSourceIdentity identity,
            LauncherModLaunchReadiness readiness
        )
        {
            Identity = identity;
            Readiness = readiness;
        }

        internal LauncherModSourceIdentity Identity { get; }
        internal LauncherModLaunchReadiness Readiness { get; }
    }
}
