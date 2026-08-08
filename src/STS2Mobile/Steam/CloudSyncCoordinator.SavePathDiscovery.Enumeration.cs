using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SavePathDiscovery
    {
        private const int EnumeratedPathLimit = 1000;
        private const int EnumeratedDirectoryDepthLimit = 8;
        private static readonly string[] IgnoredEnumerationDirectories =
        {
            ".godot",
            ".launcher_backups",
            LocalSaveBackupPlan.LauncherMetadataDirectoryName,
            "cache",
            "game",
            "tmp",
        };

        private static void AddEnumeratedSavePaths(
            List<string> paths,
            ISaveStore store,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var discovered = new HashSet<string>(paths);
                EnumerateSavePaths(
                    discovered,
                    store,
                    string.Empty,
                    depth: 0,
                    cancellationToken
                );

                foreach (var path in discovered.OrderBy(path => path))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!paths.Contains(path))
                        paths.Add(path);
                }

                PatchHelper.Log($"[Cloud] Save path discovery found {discovered.Count} candidate paths");
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
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
            int depth,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (paths.Count >= EnumeratedPathLimit || depth > EnumeratedDirectoryDepthLimit)
                return;

            foreach (
                var file in SafeGetFiles(
                    store,
                    directory,
                    cancellationToken
                )
            )
            {
                cancellationToken.ThrowIfCancellationRequested();
                var path = CombineCloudPath(directory, file);
                if (IsDiscoveredSavePath(path))
                    paths.Add(path);
                if (paths.Count >= EnumeratedPathLimit)
                    return;
            }

            foreach (
                var child in SafeGetDirectories(
                    store,
                    directory,
                    cancellationToken
                )
            )
            {
                cancellationToken.ThrowIfCancellationRequested();
                var childPath = CombineCloudPath(directory, child);
                if (ShouldSkipEnumeratedDirectory(childPath))
                    continue;

                EnumerateSavePaths(
                    paths,
                    store,
                    childPath,
                    depth + 1,
                    cancellationToken
                );
                if (paths.Count >= EnumeratedPathLimit)
                    return;
            }
        }

        private static IEnumerable<string> SafeGetFiles(
            ISaveStore store,
            string directory,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return store.GetFilesInDirectory(directory);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Cloud] Could not enumerate files in '{directory}': {ex.Message}");
                return Array.Empty<string>();
            }
        }

        private static IEnumerable<string> SafeGetDirectories(
            ISaveStore store,
            string directory,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return store.GetDirectoriesInDirectory(directory);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
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

        private static bool ShouldSkipEnumeratedDirectory(string path)
        {
            var lower = CloudSavePath.CanonicalizeLower(path);
            foreach (var ignored in IgnoredEnumerationDirectories)
            {
                if (lower == ignored || lower.StartsWith($"{ignored}/"))
                    return true;
            }

            return false;
        }
    }
}
