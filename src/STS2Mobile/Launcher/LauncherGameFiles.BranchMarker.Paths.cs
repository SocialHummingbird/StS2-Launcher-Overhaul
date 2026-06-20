namespace STS2Mobile.Launcher;

internal static partial class LauncherGameFiles
{
    private const string AndroidDataUserPrefix = "/data/user/0/";
    private const string AndroidDataDataPrefix = "/data/data/";

    private static string NormalizeMarkerPath(string path)
        => string.IsNullOrWhiteSpace(path) || path.StartsWith("<", System.StringComparison.Ordinal)
            ? string.Empty
            : path.Trim().Replace('\\', '/').TrimEnd('/');

    private static bool MarkerPathsEquivalent(string markerPath, string expectedPath, string dataDir)
    {
        var marker = NormalizeMarkerPath(markerPath);
        var expected = NormalizeMarkerPath(expectedPath);
        if (string.Equals(marker, expected, System.StringComparison.OrdinalIgnoreCase))
            return true;

        var alternateExpected = AndroidAppPrivatePathAlias(expected, dataDir);
        return !string.IsNullOrWhiteSpace(alternateExpected)
            && string.Equals(marker, alternateExpected, System.StringComparison.OrdinalIgnoreCase);
    }

    private static string AndroidAppPrivatePathAlias(string path, string dataDir)
    {
        var normalizedDataDir = NormalizeMarkerPath(dataDir);
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(normalizedDataDir))
            return string.Empty;

        if (normalizedDataDir.StartsWith(AndroidDataUserPrefix, System.StringComparison.OrdinalIgnoreCase))
        {
            return AndroidAppPrivatePathAlias(
                path,
                normalizedDataDir,
                AndroidDataUserPrefix,
                AndroidDataDataPrefix
            );
        }

        if (normalizedDataDir.StartsWith(AndroidDataDataPrefix, System.StringComparison.OrdinalIgnoreCase))
        {
            return AndroidAppPrivatePathAlias(
                path,
                normalizedDataDir,
                AndroidDataDataPrefix,
                AndroidDataUserPrefix
            );
        }

        return string.Empty;
    }

    private static string AndroidAppPrivatePathAlias(
        string path,
        string normalizedDataDir,
        string sourceRootPrefix,
        string aliasRootPrefix
    )
    {
        var packageEnd = normalizedDataDir.IndexOf('/', sourceRootPrefix.Length);
        if (packageEnd < 0)
            return string.Empty;

        var packageName = normalizedDataDir.Substring(
            sourceRootPrefix.Length,
            packageEnd - sourceRootPrefix.Length
        );
        var sourceRoot = sourceRootPrefix + packageName;
        var aliasRoot = aliasRootPrefix + packageName;
        return path.StartsWith(sourceRoot, System.StringComparison.OrdinalIgnoreCase)
            ? aliasRoot + path.Substring(sourceRoot.Length)
            : string.Empty;
    }
}
