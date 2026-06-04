using System.Collections.Generic;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private static async Task<int> RunManualBackupsAsync(
            ManualSyncContext sync,
            IEnumerable<string> paths,
            ManualBackupPlan plan
        )
        {
            var backedUp = 0;
            foreach (var path in paths)
            {
                if (await plan.TryBackupAsync(sync, path))
                    backedUp++;
            }

            return backedUp;
        }
    }
}
