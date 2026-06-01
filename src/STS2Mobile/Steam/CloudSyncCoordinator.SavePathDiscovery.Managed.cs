using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SavePathDiscovery
    {
        private static void CollectProfilePaths(
            List<string> paths,
            ISaveStore store
        )
        {
            var wasModded = UserDataPathProvider.IsRunningModded;
            try
            {
                foreach (bool modded in new[] { false, true })
                {
                    UserDataPathProvider.IsRunningModded = modded;
                    for (int i = 1; i <= 3; i++)
                    {
                        paths.Add(ProgressSaveManager.GetProgressPathForProfile(i));
                        paths.Add(RunSaveManager.GetRunSavePath(i, "current_run.save"));
                        paths.Add(RunSaveManager.GetRunSavePath(i, "current_run_mp.save"));
                        paths.Add(PrefsSaveManager.GetPrefsPath(i));
                        AddHistoryFiles(paths, store, i);
                    }
                }
            }
            finally
            {
                UserDataPathProvider.IsRunningModded = wasModded;
            }
        }

        private static void AddHistoryFiles(
            List<string> paths,
            ISaveStore store,
            int profileId
        )
        {
            var historyDir = RunHistorySaveManager.GetHistoryPath(profileId);
            AddHistoryFilePaths(
                paths,
                historyDir,
                SelectManagedRunHistoryFiles(GetHistoryFiles(historyDir, store))
            );
        }

        private static IEnumerable<string> SelectManagedRunHistoryFiles(IEnumerable<string> files)
            => files
                .Where(f => f.EndsWith(".run") && !f.EndsWith(".backup") && !f.EndsWith(".tmp"))
                .OrderByDescending(f => f)
                .Take(RunHistoryLimit);
    }
}
