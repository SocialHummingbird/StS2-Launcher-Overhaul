using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static async Task SyncExistingFileAsync(AutoSyncContext sync)
    {
        var local = sync.ReadLocalContent();
        if (local == null)
        {
            await PullFileAsync(sync);
            return;
        }

        string cloudContent = await sync.ReadCloudContentAsync(ReadCloudFileOperation);

        if (IsCorrupt(local))
        {
            await sync.PullCloudContentAsync(
                cloudContent,
                SyncLocalCorruptPulling(sync.Path),
                backUpLocal: true
            );
            return;
        }

        if (local == cloudContent)
        {
            PatchHelper.Log(SyncIdenticalSkipping(sync.Path));
            return;
        }

        await ResolveContentConflictAsync(sync, local, cloudContent);
    }

    private static async Task ResolveContentConflictAsync(
        AutoSyncContext sync,
        string localContent,
        string cloudContent
    )
    {
        var winner = SaveComparison.GetExplicitWinner(sync.Path, localContent, cloudContent);
        if (winner.CloudWins)
        {
            await sync.PullCloudContentAsync(
                cloudContent,
                SyncCloudWins(sync.Path),
                backUpLocal: true
            );
            return;
        }

        if (winner.LocalWins)
        {
            sync.PushLocalContent(localContent, cloudContent, SyncLocalWinsUploading(sync.Path));
            return;
        }

        // Cloud wins on equal progress or non-progress files to preserve PC as primary.
        await sync.PullCloudContentAsync(
            cloudContent,
            SyncContentsDifferCloudWins(sync.Path),
            backUpLocal: true
        );
    }

    // Save files are JSON; a non-JSON opener indicates corruption, e.g. unencrypted write.
    private static bool IsCorrupt(string content)
    {
        if (string.IsNullOrEmpty(content))
            return false;
        return content[0] != '{' && content[0] != '[';
    }
}
