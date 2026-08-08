using System;
using System.Globalization;
using System.IO;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBackupEvidence
{
    private static string LatestBackupUtc(string source)
    {
        DateTime? latest = null;
        foreach (var backupPath in EnumerateBackups(source))
        {
            if (!TryReadBackupUtc(backupPath, source, out var backupUtc))
                continue;

            if (!latest.HasValue || backupUtc > latest.Value)
                latest = backupUtc;
        }

        return latest.HasValue
            ? latest.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
            : "<none>";
    }

    private static bool TryReadBackupUtc(string backupPath, string source, out DateTime utc)
    {
        utc = default;

        var fileName = Path.GetFileName(backupPath);
        var suffix = $".{source}.bak";
        if (fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            var withoutSuffix = fileName.Substring(0, fileName.Length - suffix.Length);
            var timestampSeparator = withoutSuffix.LastIndexOf('.');
            if (timestampSeparator >= 0
                && long.TryParse(
                    withoutSuffix.Substring(timestampSeparator + 1),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var unixSeconds
                ))
            {
                utc = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
                return true;
            }
        }

        try
        {
            utc = File.GetLastWriteTimeUtc(backupPath);
            return utc != default;
        }
        catch
        {
            return false;
        }
    }
}
