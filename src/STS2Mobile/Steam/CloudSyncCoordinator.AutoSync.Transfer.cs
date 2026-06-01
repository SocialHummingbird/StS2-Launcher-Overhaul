using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static async Task PushFileAsync(AutoSyncContext sync)
    {
        if (!TryReadLocalContent(sync, out var content))
            return;

        if (await ShouldSkipPushAsync(sync, content))
            return;

        sync.WriteCloudFile(content);
        PatchHelper.Log(PushUploaded(sync.Path));
    }

    private static async Task PullFileAsync(AutoSyncContext sync)
    {
        if (!sync.CloudFileExists())
            return;

        string cloudContent = await sync.ReadCloudContentAsync("PullFile");

        if (ShouldSkipPull(sync, cloudContent))
            return;

        await sync.WriteCloudContentAsync(cloudContent);
        PatchHelper.Log(PullDownloaded(sync.Path));
    }

    private static bool TryReadLocalContent(AutoSyncContext sync, out string content)
    {
        content = string.Empty;
        if (!sync.LocalFileExists())
            return false;

        content = sync.ReadLocalFile();
        return true;
    }

    private static async Task<bool> ShouldSkipPushAsync(
        AutoSyncContext sync,
        string localContent
    )
    {
        if (!sync.CloudFileExists())
            return false;

        string cloudContent = await sync.ReadCloudContentAsync("ReadCloudFile");
        if (localContent == cloudContent)
        {
            PatchHelper.Log(PushSkippingIdentical(sync.Path));
            return true;
        }

        sync.BackUpCloudProgress(cloudContent);
        return false;
    }

    private static bool ShouldSkipPull(AutoSyncContext sync, string cloudContent)
    {
        if (!TryReadLocalContent(sync, out var localContent))
            return false;

        if (localContent == cloudContent)
        {
            PatchHelper.Log(PullSkippingIdentical(sync.Path));
            return true;
        }

        sync.BackUpLocalProgress();
        return false;
    }
}
