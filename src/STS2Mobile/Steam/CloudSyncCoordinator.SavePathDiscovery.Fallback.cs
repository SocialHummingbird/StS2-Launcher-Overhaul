using System.Collections.Generic;
using System;
using System.Linq;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SavePathDiscovery
    {
        private static readonly string[] FallbackHistoryDirectories =
        {
            "saves/history",
        };

        private static readonly string[] FallbackProfileFiles =
        {
            $"saves/{ProgressSaveFile}",
            $"saves/{ProgressSaveFile}{BackupExtension}",
            $"saves/{CurrentRunSaveFile}",
            $"saves/{CurrentRunSaveFile}{BackupExtension}",
            $"saves/{CurrentMultiplayerRunSaveFile}",
            $"saves/{CurrentMultiplayerRunSaveFile}{BackupExtension}",
            $"saves/{PrefsFile}",
            $"saves/{PrefsFile}{BackupExtension}",
            $"saves/{PrefsSaveFile}",
            $"saves/{PrefsSaveFile}{BackupExtension}",
        };

        private static readonly string[] FallbackRootFiles =
        {
        };

        private static readonly string[] FallbackProfilePrefixes =
        {
            "",
            "modded/",
        };

        private const int EnumeratedPathLimit = 1000;
        private const int EnumeratedDirectoryDepthLimit = 8;

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
            paths.AddRange(FallbackRootFiles);
            foreach (var profile in FallbackProfiles())
                profile.AddTo(paths, store);
            AddEnumeratedSavePaths(paths, store);
        }

        private static void AddEnumeratedSavePaths(List<string> paths, ISaveStore store)
        {
            try
            {
                var discovered = new HashSet<string>(paths);
                EnumerateSavePaths(discovered, store, string.Empty, depth: 0);

                foreach (var path in discovered.OrderBy(path => path))
                {
                    if (!paths.Contains(path))
                        paths.Add(path);
                }

                PatchHelper.Log($"[Cloud] Save path discovery found {discovered.Count} candidate paths");
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Cloud] Save path enumeration failed, using fallback paths only: {ex.Message}");
            }
        }

        private static void EnumerateSavePaths(
            HashSet<string> paths,
            ISaveStore store,
            string directory,
            int depth
        )
        {
            if (paths.Count >= EnumeratedPathLimit || depth > EnumeratedDirectoryDepthLimit)
                return;

            foreach (var file in SafeGetFiles(store, directory))
            {
                var path = CombineCloudPath(directory, file);
                if (IsDiscoveredSavePath(path))
                    paths.Add(path);
                if (paths.Count >= EnumeratedPathLimit)
                    return;
            }

            foreach (var child in SafeGetDirectories(store, directory))
            {
                EnumerateSavePaths(
                    paths,
                    store,
                    CombineCloudPath(directory, child),
                    depth + 1
                );
                if (paths.Count >= EnumeratedPathLimit)
                    return;
            }
        }

        private static IEnumerable<string> SafeGetFiles(ISaveStore store, string directory)
        {
            try
            {
                return store.GetFilesInDirectory(directory);
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Cloud] Could not enumerate files in '{directory}': {ex.Message}");
                return Array.Empty<string>();
            }
        }

        private static IEnumerable<string> SafeGetDirectories(ISaveStore store, string directory)
        {
            try
            {
                return store.GetDirectoriesInDirectory(directory);
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Cloud] Could not enumerate directories in '{directory}': {ex.Message}");
                return Array.Empty<string>();
            }
        }

        private static string CombineCloudPath(string directory, string name)
            => string.IsNullOrWhiteSpace(directory)
                ? name
                : $"{directory.TrimEnd('/')}/{name}";

        private static bool IsDiscoveredSavePath(string path)
        {
            var lower = CloudSavePath.CanonicalizeLower(path);
            return lower.EndsWith(".save")
                || lower.EndsWith(".save.backup")
                || lower.EndsWith(".run")
                || lower.EndsWith(".bak")
                || lower.EndsWith("/prefs")
                || lower.EndsWith("/prefs.save")
                || lower.EndsWith("/prefs.backup")
                || lower.EndsWith("/prefs.save.backup");
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
