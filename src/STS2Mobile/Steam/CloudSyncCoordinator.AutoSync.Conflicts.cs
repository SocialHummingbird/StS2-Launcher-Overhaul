using System;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static async Task SyncExistingFileAsync(
        AutoSyncContext sync,
        string localContent
    )
    {
        string cloudContent = await sync.ReadCloudContentAsync(ReadCloudFileOperation);

        if (IsCorrupt(localContent))
        {
            await sync.PullCloudOverLocalAsync(
                cloudContent,
                SyncLocalCorruptPulling
            );
            return;
        }

        if (localContent == cloudContent)
        {
            sync.Log(SyncIdenticalSkipping);
            return;
        }

        await ResolveContentConflictAsync(sync, localContent, cloudContent);
    }

    private static Task ResolveContentConflictAsync(
        AutoSyncContext sync,
        string localContent,
        string cloudContent
    )
    {
        var winner = sync.GetExplicitWinner(localContent, cloudContent);
        if (winner == SaveComparison.SaveWinner.Local)
        {
            sync.PushLocalContent(
                localContent,
                cloudContent,
                SyncLocalWinsUploading
            );
            return Task.CompletedTask;
        }

        Func<string, string> message = winner == SaveComparison.SaveWinner.Cloud
            ? SyncCloudWins
            : SyncContentsDifferCloudWins;
        return sync.PullCloudOverLocalAsync(cloudContent, message);
    }

    // Save files are JSON; a non-JSON opener indicates corruption, e.g. unencrypted write.
    private static bool IsCorrupt(string content)
    {
        if (string.IsNullOrEmpty(content))
            return false;
        return content[0] != '{' && content[0] != '[';
    }
}
