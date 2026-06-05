using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SavePathDiscovery
    {
        private readonly struct HistoryFileSelection
        {
            internal HistoryFileSelection(
                string directory,
                Func<IEnumerable<string>, IEnumerable<string>> selectFiles
            )
            {
                Directory = directory;
                SelectFiles = selectFiles;
            }

            private string Directory { get; }
            private Func<IEnumerable<string>, IEnumerable<string>> SelectFiles { get; }

            internal void AddTo(List<string> paths, ISaveStore store)
            {
                foreach (var file in Select(store))
                    paths.Add($"{Directory}/{file}");
            }

            private IEnumerable<string> Select(ISaveStore store)
                => SelectFiles(GetFiles(store));

            private IEnumerable<string> GetFiles(ISaveStore store)
            {
                if (!store.DirectoryExists(Directory))
                    return Array.Empty<string>();

                return store.GetFilesInDirectory(Directory);
            }
        }

        private static IEnumerable<string> SelectRunHistoryFiles(IEnumerable<string> files)
            => files
                .Where(f => f.EndsWith(RunHistoryExtension));

        private static IEnumerable<string> LimitRunHistory(
            IEnumerable<string> files
        )
            => files.Take(RunHistoryLimit);
    }
}
