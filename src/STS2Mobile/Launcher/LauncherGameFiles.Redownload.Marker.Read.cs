using System.IO;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameFiles
{
    internal const string RedownloadMarkerFileName = "last_game_version_redownload.txt";

    internal static string RedownloadMarkerPath(string dataDir)
        => Path.Combine(dataDir, RedownloadMarkerFileName);

    internal static string RedownloadMarkerUtc(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), RedownloadMarkerUtcPrefix);

    internal static bool RedownloadMarkerUtcParseable(string dataDir)
        => MarkerUtcParseable(RedownloadMarkerPath(dataDir));

    internal static string RedownloadMarkerSelectedBranch(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), RedownloadMarkerSelectedBranchPrefix);

    internal static string RedownloadMarkerSelectedVersion(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), RedownloadMarkerSelectedVersionPrefix);

    internal static string RedownloadMarkerVersionSlotKind(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), RedownloadMarkerVersionSlotKindPrefix);

    internal static string RedownloadMarkerVersionSlotDirectory(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), RedownloadMarkerVersionSlotDirectoryPrefix);

    internal static string RedownloadMarkerGameDirectory(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), RedownloadMarkerGameDirectoryPrefix);

    internal static string RedownloadMarkerGameDirectoryExisted(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), RedownloadMarkerGameDirectoryExistedPrefix);

    internal static string RedownloadMarkerGameDirectoryExistsAfterDelete(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), RedownloadMarkerGameDirectoryExistsAfterDeletePrefix);

    internal static string RedownloadMarkerDownloadStateDirectory(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), RedownloadMarkerDownloadStateDirectoryPrefix);

    internal static string RedownloadMarkerDownloadStateDirectoryExisted(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), RedownloadMarkerDownloadStateDirectoryExistedPrefix);

    internal static string RedownloadMarkerDownloadStateDirectoryExistsAfterDelete(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), RedownloadMarkerDownloadStateDirectoryExistsAfterDeletePrefix);

    internal static string RedownloadMarkerRuntimePackDirectory(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), RedownloadMarkerRuntimePackDirectoryPrefix);

    internal static string RedownloadMarkerRuntimePackDirectoryExisted(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), RedownloadMarkerRuntimePackDirectoryExistedPrefix);

    internal static string RedownloadMarkerRuntimePackDirectoryExistsAfterDelete(string dataDir)
        => ReadMarkerValue(RedownloadMarkerPath(dataDir), RedownloadMarkerRuntimePackDirectoryExistsAfterDeletePrefix);

    internal static bool RedownloadMarkerSelectedDirectoriesCleared(string dataDir)
        => string.Equals(
            RedownloadMarkerGameDirectoryExistsAfterDelete(dataDir),
            "false",
            System.StringComparison.OrdinalIgnoreCase
        )
        && string.Equals(
            RedownloadMarkerDownloadStateDirectoryExistsAfterDelete(dataDir),
            "false",
            System.StringComparison.OrdinalIgnoreCase
        )
        && string.Equals(
            RedownloadMarkerRuntimePackDirectoryExistsAfterDelete(dataDir),
            "false",
            System.StringComparison.OrdinalIgnoreCase
        );
}
