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

    private static async Task ResolveContentConflictAsync(
        AutoSyncContext sync,
        string localContent,
        string cloudContent
    )
    {
        switch (sync.GetExplicitWinner(localContent, cloudContent))
        {
            case SaveComparison.SaveWinner.Cloud:
                await sync.PullCloudOverLocalAsync(
                    cloudContent,
                    SyncCloudWins
                );
                return;

            case SaveComparison.SaveWinner.Local:
                sync.PushLocalContent(
                    localContent,
                    cloudContent,
                    SyncLocalWinsUploading
                );
                return;

            // Cloud wins on equal progress or non-progress files to preserve PC as primary.
            default:
                await sync.PullCloudOverLocalAsync(
                    cloudContent,
                    SyncContentsDifferCloudWins
                );
                return;
        }
    }

    // Save files are JSON; a non-JSON opener indicates corruption, e.g. unencrypted write.
    private static bool IsCorrupt(string content)
    {
        if (string.IsNullOrEmpty(content))
            return false;
        return content[0] != '{' && content[0] != '[';
    }
}
