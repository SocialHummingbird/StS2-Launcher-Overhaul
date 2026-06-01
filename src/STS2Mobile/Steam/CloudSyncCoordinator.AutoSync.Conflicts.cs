using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static async Task SyncExistingFileAsync(AutoSyncContext sync)
    {
        string localContent = sync.ReadLocalFile();
        string cloudContent = await sync.ReadCloudContentAsync("ReadCloudFile");

        if (IsCorrupt(localContent))
        {
            await ApplyCloudWinsAsync(
                sync,
                cloudContent,
                SyncLocalCorruptPulling(sync.Path)
            );
            return;
        }

        if (localContent == cloudContent)
        {
            PatchHelper.Log(SyncIdenticalSkipping(sync.Path));
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
        switch (SaveComparison.GetExplicitWinner(sync.Path, localContent, cloudContent))
        {
            case SaveComparison.Result.CloudWins:
                await ApplyCloudWinsAsync(
                    sync,
                    cloudContent,
                    SyncCloudWins(sync.Path)
                );
                return;

            case SaveComparison.Result.LocalWins:
                ApplyLocalWins(
                    sync,
                    localContent,
                    cloudContent,
                    SyncLocalWinsUploading(sync.Path)
                );
                return;
        }

        // Cloud wins on equal progress or non-progress files to preserve PC as primary.
        await ApplyCloudWinsAsync(
            sync,
            cloudContent,
            SyncContentsDifferCloudWins(sync.Path)
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
