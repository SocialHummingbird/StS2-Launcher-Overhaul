using System;

namespace STS2Mobile.Launcher;

internal static class LauncherAndroidAppPrivatePath
{
    private const string DataUserPrefix = "/data/user/0/";
    private const string DataDataPrefix = "/data/data/";

    internal static string NormalizePath(string path)
        => string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : path.Trim().Replace('\\', '/').TrimEnd('/');

    internal static string NormalizeMarkerPath(string path)
        => string.IsNullOrWhiteSpace(path) || path.StartsWith("<", StringComparison.Ordinal)
            ? string.Empty
            : NormalizePath(path);

    internal static bool MarkerPathMatchesExpectedPath(string markerPath, string expectedPath, string dataDir)
    {
        var marker = NormalizeMarkerPath(markerPath);
        var expected = NormalizeMarkerPath(expectedPath);
        if (string.Equals(marker, expected, StringComparison.OrdinalIgnoreCase))
            return true;

        var alternateExpected = AndroidAppPrivatePathAlias(expected, dataDir);
        return !string.IsNullOrWhiteSpace(alternateExpected)
            && string.Equals(marker, alternateExpected, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool PathMatchesOrLeftAliasMatches(string left, string right, string dataDir)
    {
        left = NormalizePath(left);
        right = NormalizePath(right);
        if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
            return true;

        var leftAlias = AndroidAppPrivatePathAlias(left, dataDir);
        return !string.IsNullOrWhiteSpace(leftAlias)
            && string.Equals(leftAlias, right, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool NormalizedMarkerPathsEqual(string left, string right)
        => string.Equals(
            NormalizeMarkerPath(left),
            NormalizeMarkerPath(right),
            StringComparison.OrdinalIgnoreCase
        );

    internal static string AndroidAppPrivatePathAlias(string path, string dataDir)
    {
        path = NormalizePath(path);
        var normalizedDataDir = NormalizePath(dataDir);
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(normalizedDataDir))
            return string.Empty;

        if (normalizedDataDir.StartsWith(DataUserPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return AndroidAppPrivatePathAlias(
                path,
                normalizedDataDir,
                DataUserPrefix,
                DataDataPrefix
            );
        }

        if (normalizedDataDir.StartsWith(DataDataPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return AndroidAppPrivatePathAlias(
                path,
                normalizedDataDir,
                DataDataPrefix,
                DataUserPrefix
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
            packageEnd = normalizedDataDir.Length;

        var packageName = normalizedDataDir.Substring(
            sourceRootPrefix.Length,
            packageEnd - sourceRootPrefix.Length
        );
        var sourceRoot = sourceRootPrefix + packageName;
        var aliasRoot = aliasRootPrefix + packageName;
        return path.StartsWith(sourceRoot, StringComparison.OrdinalIgnoreCase)
            ? aliasRoot + path.Substring(sourceRoot.Length)
            : string.Empty;
    }
}
