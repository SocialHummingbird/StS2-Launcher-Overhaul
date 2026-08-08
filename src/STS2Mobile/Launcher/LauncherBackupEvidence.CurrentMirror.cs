using System;
using System.IO;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBackupEvidence
{
    internal static int CurrentMirrorSaveCount()
    {
        if (!Directory.Exists(CurrentMirrorDirectory))
            return 0;

        var count = 0;
        try
        {
            foreach (
                var path in Directory.EnumerateFiles(
                    CurrentMirrorDirectory,
                    "*",
                    SearchOption.AllDirectories
                )
            )
            {
                if (count >= LocalSaveBackupPlan.MaxFiles)
                    break;

                var relativePath = Path.GetRelativePath(CurrentMirrorDirectory, path);
                if (
                    LocalSaveBackupPlan.IsBackupEligible(relativePath)
                    && new FileInfo(path).Length > 0
                )
                    count++;
            }
        }
        catch
        {
            return count;
        }

        return count;
    }

    internal static string LatestCurrentMirrorWriteUtc()
    {
        if (!Directory.Exists(CurrentMirrorDirectory))
            return "<none>";

        var latest = DateTime.MinValue;
        try
        {
            foreach (
                var path in Directory.EnumerateFiles(
                    CurrentMirrorDirectory,
                    "*",
                    SearchOption.AllDirectories
                )
            )
            {
                var modified = File.GetLastWriteTimeUtc(path);
                if (modified > latest)
                    latest = modified;
            }
        }
        catch
        {
        }

        return latest == DateTime.MinValue
            ? "<none>"
            : new DateTimeOffset(latest, TimeSpan.Zero).ToString("O");
    }
}
