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

        private readonly struct FallbackProfile
        {
            internal FallbackProfile(string name)
            {
                Name = name;
            }

            private string Name { get; }

            internal void AddTo(List<string> paths, ISaveStore store)
            {
                AddFiles(paths);
                AddHistory(paths, store);
            }

            private void AddFiles(List<string> paths)
            {
                foreach (var file in FallbackProfileFiles)
                    paths.Add($"{Name}/{file}");
            }

            private void AddHistory(List<string> paths, ISaveStore store)
            {
                foreach (var historyName in FallbackHistoryDirectories)
                {
                    var historyDir = $"{Name}/{historyName}";
                    new HistoryFileSelection(
                        historyDir,
                        SelectFallbackRunHistoryFiles
                    ).AddTo(paths, store);
                }
            }
        }

        private static void AddFallbackProfilePaths(
            List<string> paths,
            ISaveStore store
        )
        {
            foreach (var profile in FallbackProfiles())
                profile.AddTo(paths, store);
        }

        private static IEnumerable<FallbackProfile> FallbackProfiles()
        {
            foreach (var prefix in FallbackProfilePrefixes)
            {
                foreach (var profileId in ProfileIds())
                    yield return new FallbackProfile($"{prefix}profile{profileId}");
            }
        }

        private static IEnumerable<string> SelectFallbackRunHistoryFiles(
            IEnumerable<string> files
        )
            => LimitRunHistory(SelectRunHistoryFiles(files));
    }
}
