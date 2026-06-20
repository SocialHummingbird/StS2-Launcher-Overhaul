using System;
using System.IO;

namespace STS2Mobile.Launcher;

internal sealed partial class GameRuntimeSlot
{
    private static bool PathsEquivalent(string left, string right, string dataDir)
        => LauncherAndroidAppPrivatePath.PathMatchesOrLeftAliasMatches(left, right, dataDir);

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

    private static bool HasMarkerValue(string value)
        => !string.IsNullOrWhiteSpace(value) && !value.StartsWith("<", StringComparison.Ordinal);
}
