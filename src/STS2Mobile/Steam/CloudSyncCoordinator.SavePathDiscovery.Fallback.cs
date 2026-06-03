using System.Collections.Generic;
using System.Linq;
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
            foreach (var prefix in FallbackProfilePrefixes)
            {
                foreach (var profileId in ProfileIds())
                {
                    var profile = $"{prefix}profile{profileId}";
                    foreach (var file in FallbackProfileFiles)
                        paths.Add($"{profile}/{file}");

                    foreach (var historyName in FallbackHistoryDirectories)
                    {
                        var historyDir = $"{profile}/{historyName}";
                        AddSelectedHistoryFilePaths(
                            paths,
                            store,
                            historyDir,
                            files => SelectRunHistoryFiles(files).Take(RunHistoryLimit)
                        );
                    }
                }
            }
        }
    }
}
