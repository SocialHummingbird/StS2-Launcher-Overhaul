using System.IO;

namespace STS2Mobile.Launcher;

internal static partial class LauncherRuntimeCacheEvidence
{
    internal const string MarkerFileName = "current_runtime_cache.txt";

    internal static string MarkerPath(string dataDir)
        => Path.Combine(dataDir, MarkerFileName);

    internal static bool MarkerPresent(string dataDir)
        => File.Exists(MarkerPath(dataDir));
}
