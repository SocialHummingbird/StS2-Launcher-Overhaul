using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static async Task RunManualSyncAsync(
        string accountName,
        string refreshToken,
        Func<ManualSyncContext, IReadOnlyCollection<string>> discoverPaths,
        Func<int, string> startingMessage,
        Func<ManualSyncContext, IEnumerable<string>, Task<int>> backupAsync,
        Func<int, string> backedUpMessage,
        Func<
            ManualSyncContext,
            IReadOnlyCollection<string>,
            Task<string>
        > transferAsync
    )
    {
        var sync = CreateManualSyncContext(accountName, refreshToken);
        var paths = discoverPaths(sync);
        PatchHelper.Log(startingMessage(paths.Count));

        var backedUp = await backupAsync(sync, paths);
        if (backedUp > 0)
            PatchHelper.Log(backedUpMessage(backedUp));

        PatchHelper.Log(await transferAsync(sync, paths));
    }
}
