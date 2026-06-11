using System;
using System.Globalization;
using System.IO;

namespace STS2Mobile.Launcher;

internal static class LauncherBackupEvidence
{
    private const int MaxBackupFilesToInspect = 500;
    private const string LocalPrePushSource = "local-pre-push";
    private const string CloudPrePushSource = "cloud-pre-push";

    internal static string BackupDirectory
        => STS2Mobile.AppPaths.ExternalSaveBackupsDir;

    internal static int LocalPrePushBackupCount()
        => CountBackups(LocalPrePushSource);

    internal static int CloudPrePushBackupCount()
        => CountBackups(CloudPrePushSource);

    internal static string LatestLocalPrePushBackupUtc()
        => LatestBackupUtc(LocalPrePushSource);

    internal static string LatestCloudPrePushBackupUtc()
        => LatestBackupUtc(CloudPrePushSource);

    internal static bool HasLocalPrePushBackupAfterBranchSwitch(string dataDir)
        => HasBackupAfterBranchSwitch(dataDir, LocalPrePushSource);

    internal static bool HasCloudPrePushBackupAfterBranchSwitch(string dataDir)
        => HasBackupAfterBranchSwitch(dataDir, CloudPrePushSource);

    internal static bool HasPrePushBackupEvidenceAfterBranchSwitch(string dataDir)
        => HasLocalPrePushBackupAfterBranchSwitch(dataDir)
            && HasCloudPrePushBackupAfterBranchSwitch(dataDir);

    private static int CountBackups(string source)
    {
        var count = 0;
        foreach (var _ in EnumerateBackups(source))
            count++;

        return count;
    }

    private static bool HasBackupAfterBranchSwitch(string dataDir, string source)
    {
        if (!TryReadBranchSwitchUtc(dataDir, out var branchSwitchUtc))
            return false;

        foreach (var backupPath in EnumerateBackups(source))
        {
            if (TryReadBackupUtc(backupPath, source, out var backupUtc)
                && backupUtc >= branchSwitchUtc)
                return true;
        }

        return false;
    }

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

    private static System.Collections.Generic.IReadOnlyList<string> EnumerateBackups(string source)
    {
        var inspected = 0;
        if (!Directory.Exists(BackupDirectory))
            return System.Array.Empty<string>();

        var paths = new System.Collections.Generic.List<string>();
        try
        {
            foreach (var file in Directory.EnumerateFiles(
                BackupDirectory,
                $"*.{source}.bak",
                SearchOption.AllDirectories
            ))
            {
                if (inspected >= MaxBackupFilesToInspect)
                    break;

                inspected++;
                paths.Add(file);
            }
        }
        catch
        {
            return System.Array.Empty<string>();
        }

        return paths;
    }

    private static bool TryReadBranchSwitchUtc(string dataDir, out DateTime utc)
        => DateTime.TryParse(
            LauncherBranchSwitchSafety.MarkerUtc(dataDir),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal,
            out utc
        );

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
