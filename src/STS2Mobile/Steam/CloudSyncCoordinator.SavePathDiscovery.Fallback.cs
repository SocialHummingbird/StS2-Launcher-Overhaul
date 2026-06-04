using System.Collections.Generic;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SavePathDiscovery
    {
        private static readonly string[] FallbackHistoryDirectories =
        {
            "runs",
            "run_history",
            "history",
        };

        private static readonly string[] FallbackProfileFiles =
        {
            ProgressSaveFile,
            CurrentRunSaveFile,
            CurrentMultiplayerRunSaveFile,
            PrefsFile,
            PrefsSaveFile,
        };

        private static readonly string[] FallbackProfilePrefixes =
        {
            "",
            "modded/",
        };

        private static void AddFallbackProfilePaths(
            List<string> paths,
            ISaveStore store
        )
        {
            foreach (var profile in FallbackProfiles())
                AddFallbackProfilePaths(paths, store, profile);
        }

        private static IEnumerable<string> FallbackProfiles()
        {
            foreach (var prefix in FallbackProfilePrefixes)
            {
                foreach (var profileId in ProfileIds())
                    yield return $"{prefix}profile{profileId}";
            }
        }

        private static void AddFallbackProfilePaths(
            List<string> paths,
            ISaveStore store,
            string profile
        )
        {
            AddFallbackProfileFiles(paths, profile);
            AddFallbackProfileHistory(paths, store, profile);
        }

        private static void AddFallbackProfileFiles(List<string> paths, string profile)
        {
            foreach (var file in FallbackProfileFiles)
                paths.Add($"{profile}/{file}");
        }

        private static void AddFallbackProfileHistory(
            List<string> paths,
            ISaveStore store,
            string profile
        )
        {
            foreach (var historyName in FallbackHistoryDirectories)
            {
                var historyDir = $"{profile}/{historyName}";
                AddSelectedHistoryFilePaths(
                    paths,
                    store,
                    historyDir,
                    files => LimitRunHistory(SelectRunHistoryFiles(files))
                );
            }
        }
    }
}
