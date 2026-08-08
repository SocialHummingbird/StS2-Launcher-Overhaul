namespace STS2Mobile.Launcher;

internal static class LauncherLaunchReadinessCacheStatus
{
    internal const string Fresh = "fresh";
    internal const string FreshCached = "fresh-cached";
    internal const string MemoryCacheHit = "memory-cache-hit";
    internal const string Pending = "pending";
    internal const string DownloadedStateOnly = "downloaded-state-only";
    internal const string DownloadedStateCheckFailed = "downloaded-state-check-failed";
}
