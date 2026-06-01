using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static async Task PushFileAsync(AutoSyncContext sync)
    {
        var local = sync.ReadLocalContent();
        if (local == null)
            return;

        var push = await GetPushDecisionAsync(sync, local);
        if (push.Skip)
            return;

        sync.PushLocalContent(local, push.CloudContentToBackUp, PushUploaded(sync.Path));
    }

    private static async Task PullFileAsync(AutoSyncContext sync)
    {
        if (!sync.CloudFileExists())
            return;

        string cloudContent = await sync.ReadCloudContentAsync(PullCloudFileOperation);

        var pull = GetPullDecision(sync, cloudContent);
        if (pull.Skip)
            return;

        await sync.PullCloudContentAsync(
            cloudContent,
            PullDownloaded(sync.Path),
            pull.BackUpLocal
        );
    }

    private static async Task<(bool Skip, string? CloudContentToBackUp)> GetPushDecisionAsync(
        AutoSyncContext sync,
        string localContent
    )
    {
        if (!sync.CloudFileExists())
            return (Skip: false, CloudContentToBackUp: null);

        string cloudContent = await sync.ReadCloudContentAsync(ReadCloudFileOperation);
        if (localContent == cloudContent)
        {
            PatchHelper.Log(PushSkippingIdentical(sync.Path));
            return (Skip: true, CloudContentToBackUp: null);
        }

        return (Skip: false, CloudContentToBackUp: cloudContent);
    }

    private static (bool Skip, bool BackUpLocal) GetPullDecision(
        AutoSyncContext sync,
        string cloudContent
    )
    {
        var local = sync.ReadLocalContent();
        if (local == null)
            return (Skip: false, BackUpLocal: false);

        if (local == cloudContent)
        {
            PatchHelper.Log(PullSkippingIdentical(sync.Path));
            return (Skip: true, BackUpLocal: false);
        }

        return (Skip: false, BackUpLocal: true);
    }
}
