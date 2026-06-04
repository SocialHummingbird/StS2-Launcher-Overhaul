using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SavePathDiscovery
    {
        private static IEnumerable<string> GetHistoryFiles(string historyDir, ISaveStore store)
        {
            if (!store.DirectoryExists(historyDir))
                return Array.Empty<string>();

            return store.GetFilesInDirectory(historyDir);
        }

        private static void AddSelectedHistoryFilePaths(
            List<string> paths,
            ISaveStore store,
            string historyDir,
            Func<IEnumerable<string>, IEnumerable<string>> selectFiles
        )
            => AddHistoryFilePaths(
                paths,
                historyDir,
                selectFiles(GetHistoryFiles(historyDir, store))
            );

        private static void AddHistoryFilePaths(
            List<string> paths,
            string historyDir,
            IEnumerable<string> files
        )
        {
            foreach (var file in files)
                paths.Add($"{historyDir}/{file}");
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
