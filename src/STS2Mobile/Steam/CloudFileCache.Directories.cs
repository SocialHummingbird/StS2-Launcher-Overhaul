using System.Collections.Generic;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private sealed partial class CloudFileCache
    {
        private string[] GetFilesInDirectory(string directoryPath)
        {
            EnsureLoaded();
            var result = new List<string>();

            foreach (var remainder in EnumerateDirectoryEntries(_files.Keys, directoryPath))
            {
                if (!remainder.Contains('/') && !remainder.Contains('\\'))
                    result.Add(remainder);
            }

            return result.ToArray();
        }

        private string[] GetDirectoriesInDirectory(string directoryPath)
        {
            EnsureLoaded();
            var dirs = new HashSet<string>();

            foreach (var remainder in EnumerateDirectoryEntries(_files.Keys, directoryPath))
            {
                var slashIndex = remainder.IndexOf('/');
                if (slashIndex >= 0)
                    dirs.Add(remainder.Substring(0, slashIndex));
            }

            return [.. dirs];
        }

        private static IEnumerable<string> EnumerateDirectoryEntries(
            IEnumerable<string> paths,
            string directoryPath
        )
        {
            var normalizedDirectory = CacheKey(directoryPath);
            var prefix = normalizedDirectory.Length > 0 ? normalizedDirectory + "/" : "";

            foreach (var key in paths)
            {
                var normalizedKey = CacheKey(key);
                if (!normalizedKey.StartsWith(prefix) || normalizedKey.Length <= prefix.Length)
                    continue;

                yield return normalizedKey.Substring(prefix.Length);
            }
        }
    }
}
