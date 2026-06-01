using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SavePathDiscovery
    {
        private const int FirstProfileId = 1;
        private const int LastProfileId = 3;
        private const int RunHistoryLimit = 100;

        internal static List<string> Get(ISaveStore store)
        {
            var paths = new List<string>();
            CollectProfilePathsSafe(paths, store);
            return paths;
        }

        private static void CollectProfilePathsSafe(
            List<string> paths,
            ISaveStore store
        )
        {
            if (OperatingSystem.IsAndroid())
            {
                AddFallbackProfilePaths(paths, store);
                return;
            }

            try
            {
                CollectProfilePaths(paths, store);
            }
            catch (TypeInitializationException ex)
            {
                PatchHelper.Log(SavePathManagerFallback(ex));
                AddFallbackProfilePaths(paths, store);
            }
        }

        private static string SavePathManagerFallback(Exception ex) =>
            $"[Cloud] Save path manager failed, using Android fallback paths: {ex.Message}";

        private static IEnumerable<int> ProfileIds()
        {
            for (var profileId = FirstProfileId; profileId <= LastProfileId; profileId++)
                yield return profileId;
        }

        private static IEnumerable<string> GetHistoryFiles(string historyDir, ISaveStore store)
        {
            if (!store.DirectoryExists(historyDir))
                return Array.Empty<string>();

            return store.GetFilesInDirectory(historyDir);
        }

        private static void AddHistoryFilePaths(
            List<string> paths,
            string historyDir,
            IEnumerable<string> files
        )
        {
            foreach (var file in files)
                paths.Add($"{historyDir}/{file}");
        }
    }
}
