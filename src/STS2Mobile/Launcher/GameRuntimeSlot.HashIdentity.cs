using System;
using System.IO;

namespace STS2Mobile.Launcher;

internal sealed partial class GameRuntimeSlot
{
    private static bool PathsEquivalent(string left, string right, string dataDir)
    {
        left = NormalizePath(left);
        right = NormalizePath(right);
        if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
            return true;

        var leftAlias = AndroidAppPrivatePathAlias(left, dataDir);
        return !string.IsNullOrWhiteSpace(leftAlias)
            && string.Equals(leftAlias, right, StringComparison.OrdinalIgnoreCase);
    }

    private static bool FileIdentityMatches(string cachedIdentity, string path)
    {
        if (!HasMarkerValue(cachedIdentity) || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        return string.Equals(cachedIdentity, FileIdentity(path), StringComparison.OrdinalIgnoreCase);
    }

    private static string FileIdentity(string path)
    {
        var info = new FileInfo(path);
        var mtime = new DateTimeOffset(info.LastWriteTimeUtc).ToUnixTimeMilliseconds();
        return $"bytes={info.Length},mtime={mtime}";
    }

    private static string AndroidAppPrivatePathAlias(string path, string dataDir)
    {
        var normalizedDataDir = NormalizePath(dataDir);
        var packageName = Path.GetFileName(normalizedDataDir);
        if (string.IsNullOrWhiteSpace(packageName))
            return null;

        var dataDataPrefix = $"/data/data/{packageName}";
        var dataUserPrefix = $"/data/user/0/{packageName}";
        if (path.StartsWith(dataDataPrefix, StringComparison.OrdinalIgnoreCase))
            return dataUserPrefix + path.Substring(dataDataPrefix.Length);
        if (path.StartsWith(dataUserPrefix, StringComparison.OrdinalIgnoreCase))
            return dataDataPrefix + path.Substring(dataUserPrefix.Length);

        return null;
    }

    private static string NormalizePath(string path)
        => string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : path.Trim().Replace('\\', '/').TrimEnd('/');

    private static bool HasMarkerValue(string value)
        => !string.IsNullOrWhiteSpace(value) && !value.StartsWith("<", StringComparison.Ordinal);
}
