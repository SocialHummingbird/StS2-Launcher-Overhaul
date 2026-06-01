using System.Collections.Generic;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private sealed partial class CloudFileCache
    {
        internal string[] GetFilesInDirectory(string directoryPath)
        {
            EnsureLoaded();
            var result = new List<string>();

            foreach (var remainder in EnumerateDirectoryEntries(directoryPath))
            {
                if (!remainder.Contains('/'))
                    result.Add(remainder);
            }

            return result.ToArray();
        }

        internal string[] GetDirectoriesInDirectory(string directoryPath)
        {
            EnsureLoaded();
            var dirs = new HashSet<string>();

            foreach (var remainder in EnumerateDirectoryEntries(directoryPath))
            {
                var slashIndex = remainder.IndexOf('/');
                if (slashIndex >= 0)
                    dirs.Add(remainder.Substring(0, slashIndex));
            }

            return [.. dirs];
        }

        private IEnumerable<string> EnumerateDirectoryEntries(string directoryPath)
        {
            var normalizedDirectory = CacheKey(directoryPath);
            var prefix = normalizedDirectory.Length > 0 ? normalizedDirectory + "/" : "";

            foreach (var key in _files.Keys)
            {
                if (!key.StartsWith(prefix, System.StringComparison.Ordinal)
                    || key.Length <= prefix.Length)
                    continue;

                yield return key.Substring(prefix.Length);
            }
        }
    }
}
