using System.IO;
using System.Linq;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private const int MaxProgressBackups = 50;

        private static void PruneProgressBackups(string backupDir)
        {
            var backups = Directory
                .GetFiles(backupDir, "progress.save.*.bak")
                .OrderByDescending(f => f)
                .Skip(MaxProgressBackups)
                .ToArray();

            foreach (var old in backups)
            {
                try
                {
                    File.Delete(old);
                }
                catch
                {
                }
            }
        }
    }
}
