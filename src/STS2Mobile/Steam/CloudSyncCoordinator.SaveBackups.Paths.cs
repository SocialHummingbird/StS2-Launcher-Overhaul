using System;
using System.IO;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private static string NormalizeSavePath(string path)
        {
            return CloudSavePath.Canonicalize(path);
        }

        private static string GetProfileDir(string canonPath)
        {
            var parts = canonPath.Split('/');
            foreach (var part in parts)
            {
                if (part.StartsWith("profile"))
                    return part;
            }

            return "default";
        }

        private static string BuildBackupPath(string profileDir, string fileName, string source)
        {
            var backupDir = Path.Combine(AppPaths.ExternalSaveBackupsDir, profileDir);
            Directory.CreateDirectory(backupDir);

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return Path.Combine(backupDir, $"{fileName}.{timestamp}.{source}.bak");
        }
    }
}
