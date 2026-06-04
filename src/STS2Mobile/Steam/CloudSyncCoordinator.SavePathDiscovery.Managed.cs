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
                {
                    UserDataPathProvider.IsRunningModded = modded;
                    foreach (var profileId in ProfileIds())
                        AddManagedProfilePaths(paths, store, profileId);
                }
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
            AddSelectedHistoryFilePaths(
                paths,
                store,
                RunHistorySaveManager.GetHistoryPath(profileId),
                SelectManagedRunHistoryFiles
            );
        }

        private static IEnumerable<string> SelectManagedRunHistoryFiles(IEnumerable<string> files)
            => LimitRunHistory(
                SelectRunHistoryFiles(files)
                    .Where(f =>
                        !f.EndsWith(BackupExtension)
                        && !f.EndsWith(TempExtension)
                    )
                    .OrderByDescending(f => f)
            );
    }
}
