using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private readonly struct PushDecision
    {
        private enum Action
        {
            Skip,
            Upload,
        }

        private PushDecision(Action action, string? cloudContentToBackUp)
        {
            _action = action;
            CloudContentToBackUp = cloudContentToBackUp;
        }

        private readonly Action _action;
        internal bool ShouldUpload => _action == Action.Upload;
        internal string? CloudContentToBackUp { get; }

        internal static PushDecision Upload(string? cloudContentToBackUp)
            => new(Action.Upload, cloudContentToBackUp);

        internal static PushDecision SkipUpload()
            => new(Action.Skip, cloudContentToBackUp: null);
    }

    private readonly struct PullDecision
    {
        private enum Action
        {
            Skip,
            Download,
        }

        private PullDecision(Action action, bool backUpLocal)
        {
            _action = action;
            BackUpLocal = backUpLocal;
        }

        private readonly Action _action;
        internal bool ShouldDownload => _action == Action.Download;
        internal bool BackUpLocal { get; }

        internal static PullDecision Download(bool backUpLocal)
            => new(Action.Download, backUpLocal);

        internal static PullDecision SkipDownload()
            => new(Action.Skip, backUpLocal: false);
    }

    private static async Task PushFileAsync(AutoSyncContext sync)
    {
        var local = sync.ReadLocalContent();
        if (local == null)
            return;

        var push = await GetPushDecisionAsync(sync, local);
        if (!push.ShouldUpload)
            return;

        sync.PushLocalContent(local, push.CloudContentToBackUp, PushUploaded(sync.Path));
    }

    private static async Task PullFileAsync(AutoSyncContext sync)
    {
        if (!sync.CloudFileExists())
            return;

        string cloudContent = await sync.ReadCloudContentAsync(PullCloudFileOperation);

        var pull = GetPullDecision(sync, cloudContent);
        if (!pull.ShouldDownload)
            return;

        await sync.PullCloudContentAsync(
            cloudContent,
            PullDownloaded(sync.Path),
            pull.BackUpLocal
        );
    }

    private static async Task<PushDecision> GetPushDecisionAsync(
        AutoSyncContext sync,
        string localContent
    )
    {
        if (!sync.CloudFileExists())
            return PushDecision.Upload(cloudContentToBackUp: null);

        string cloudContent = await sync.ReadCloudContentAsync(ReadCloudFileOperation);
        if (localContent == cloudContent)
        {
            PatchHelper.Log(PushSkippingIdentical(sync.Path));
            return PushDecision.SkipUpload();
        }

        return PushDecision.Upload(cloudContent);
    }

    private static PullDecision GetPullDecision(
        AutoSyncContext sync,
        string cloudContent
    )
    {
        var local = sync.ReadLocalContent();
        if (local == null)
            return PullDecision.Download(backUpLocal: false);

        if (local == cloudContent)
        {
            PatchHelper.Log(PullSkippingIdentical(sync.Path));
            return PullDecision.SkipDownload();
        }

        return PullDecision.Download(backUpLocal: true);
    }
}
