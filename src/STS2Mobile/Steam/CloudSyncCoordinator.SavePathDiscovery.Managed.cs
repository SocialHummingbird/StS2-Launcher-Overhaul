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
                foreach (bool modded in ManagedSaveModes())
                    CollectProfilePathsForMode(paths, store, modded);
            }
            finally
            {
                UserDataPathProvider.IsRunningModded = wasModded;
            }
        }

        private static IEnumerable<bool> ManagedSaveModes()
        {
            yield return false;
            yield return true;
        }

        private static void CollectProfilePathsForMode(
            List<string> paths,
            ISaveStore store,
            bool modded
        )
        {
            UserDataPathProvider.IsRunningModded = modded;
            foreach (var profileId in ProfileIds())
                AddManagedProfilePaths(paths, store, profileId);
        }

        private static void AddManagedProfilePaths(
            List<string> paths,
            ISaveStore store,
            int profileId
        )
        {
            paths.Add(ProgressSaveManager.GetProgressPathForProfile(profileId));
            paths.Add(RunSaveManager.GetRunSavePath(profileId, CurrentRunSaveFile));
            paths.Add(RunSaveManager.GetRunSavePath(profileId, CurrentMultiplayerRunSaveFile));
            paths.Add(PrefsSaveManager.GetPrefsPath(profileId));
            AddHistoryFiles(paths, store, profileId);
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
                .Where(f =>
                    f.EndsWith(RunHistoryExtension)
                    && !f.EndsWith(BackupExtension)
                    && !f.EndsWith(TempExtension)
                )
                .OrderByDescending(f => f)
                .Take(RunHistoryLimit);
    }
}
