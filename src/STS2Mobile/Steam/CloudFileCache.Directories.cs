using System.Collections.Generic;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private sealed partial class CloudFileCache
    {
        private readonly struct CloudDirectorySnapshot
        {
            private readonly List<string> _files;
            private readonly HashSet<string> _directories;

            private CloudDirectorySnapshot(
                List<string> files,
                HashSet<string> directories
            )
            {
                _files = files;
                _directories = directories;
            }

            internal string[] Files()
                => _files.ToArray();

            internal string[] Directories()
                => [.. _directories];

            internal static CloudDirectorySnapshot From(
                IEnumerable<string> entries
            )
            {
                var files = new List<string>();
                var directories = new HashSet<string>();

                foreach (var remainder in entries)
                {
                    var slashIndex = remainder.IndexOf('/');
                    if (slashIndex >= 0)
                    {
                        directories.Add(remainder.Substring(0, slashIndex));
                        continue;
                    }

                    files.Add(remainder);
                }

                return new CloudDirectorySnapshot(files, directories);
            }
        }

        internal string[] GetFilesInDirectory(string directoryPath)
            => DirectorySnapshot(directoryPath).Files();

        internal string[] GetDirectoriesInDirectory(string directoryPath)
            => DirectorySnapshot(directoryPath).Directories();

        private CloudDirectorySnapshot DirectorySnapshot(string directoryPath)
        {
            EnsureLoaded();
            return CloudDirectorySnapshot.From(
                EnumerateDirectoryEntries(directoryPath)
            );
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
