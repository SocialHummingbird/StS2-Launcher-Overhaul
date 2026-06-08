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
        private const string BackupExtension = ".backup";
        private const string CurrentMultiplayerRunSaveFile = "current_run_mp.save";
        private const string CurrentRunSaveFile = "current_run.save";
        private const string PrefsFile = "prefs";
        private const string PrefsSaveFile = "prefs.save";
        private const string ProgressSaveFile = "progress.save";
        private const string RunHistoryExtension = ".run";
        private const string TempExtension = ".tmp";

        internal static IReadOnlyCollection<string> Get(ISaveStore store)
        {
            var paths = new List<string>();
            CollectProfilePathsSafe(paths, store);
            return Deduplicate(paths);
        }

        private static void CollectProfilePathsSafe(
            List<string> paths,
            ISaveStore store
        )
        {
            var fallbackAdded = false;
            try
            {
                CollectProfilePaths(paths, store);
            }
            catch (TypeInitializationException ex)
            {
                PatchHelper.Log(SavePathManagerFallback(ex));
                AddFallbackProfilePaths(paths, store);
                fallbackAdded = true;
            }
            catch (Exception ex) when (OperatingSystem.IsAndroid())
            {
                PatchHelper.Log(SavePathManagerFallback(ex));
            }

            if (OperatingSystem.IsAndroid() && !fallbackAdded)
                AddFallbackProfilePaths(paths, store);
        }

        private static IReadOnlyCollection<string> Deduplicate(List<string> paths)
        {
            var deduped = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in paths)
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                var canonical = CloudSavePath.Canonicalize(path);
                if (seen.Add(canonical))
                    deduped.Add(canonical);
            }

            if (deduped.Count != paths.Count)
                PatchHelper.Log($"[Cloud] Save path discovery deduped {paths.Count} candidates to {deduped.Count}");

            return deduped;
        }

        private static string SavePathManagerFallback(Exception ex) =>
            $"[Cloud] Save path manager failed, using Android fallback paths: {ex.Message}";

        private static IEnumerable<int> ProfileIds()
        {
            for (var profileId = FirstProfileId; profileId <= LastProfileId; profileId++)
                yield return profileId;
        }
    }
}
