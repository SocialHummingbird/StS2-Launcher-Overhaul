using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private readonly struct TransferDecision
    {
        private enum Action
        {
            Skip,
            Transfer,
        }

        private TransferDecision(
            Action action,
            string? existingContentToBackUp,
            bool backUpExisting
        )
        {
            _action = action;
            ExistingContentToBackUp = existingContentToBackUp;
            BackUpExisting = backUpExisting;
        }

        private readonly Action _action;
        internal bool ShouldTransfer => _action == Action.Transfer;
        internal string? ExistingContentToBackUp { get; }
        internal bool BackUpExisting { get; }

        internal static TransferDecision Transfer(
            string? existingContentToBackUp = null,
            bool backUpExisting = false
        )
            => new(Action.Transfer, existingContentToBackUp, backUpExisting);

        internal static TransferDecision Skip()
            => new(Action.Skip, existingContentToBackUp: null, backUpExisting: false);
    }

    private static async Task PushFileAsync(AutoSyncContext sync)
    {
        var local = sync.ReadLocalContent();
        if (local == null)
            return;

        var push = await GetPushDecisionAsync(sync, local);
        if (!push.ShouldTransfer)
            return;

        sync.PushLocalContent(local, push.ExistingContentToBackUp, PushUploaded(sync.Path));
    }

    private static async Task PullFileAsync(AutoSyncContext sync)
    {
        if (!sync.CloudFileExists())
            return;

        string cloudContent = await sync.ReadCloudContentAsync(PullCloudFileOperation);

        var pull = GetPullDecision(sync, cloudContent);
        if (!pull.ShouldTransfer)
            return;

        await sync.PullCloudContentAsync(
            cloudContent,
            PullDownloaded(sync.Path),
            pull.BackUpExisting
        );
    }

    private static async Task<TransferDecision> GetPushDecisionAsync(
        AutoSyncContext sync,
        string localContent
    )
    {
        if (!sync.CloudFileExists())
            return TransferDecision.Transfer();

        string cloudContent = await sync.ReadCloudContentAsync(ReadCloudFileOperation);
        if (localContent == cloudContent)
        {
            PatchHelper.Log(PushSkippingIdentical(sync.Path));
            return TransferDecision.Skip();
        }

        return TransferDecision.Transfer(existingContentToBackUp: cloudContent);
    }

    private static TransferDecision GetPullDecision(
        AutoSyncContext sync,
        string cloudContent
    )
    {
        var local = sync.ReadLocalContent();
        if (local == null)
            return TransferDecision.Transfer();

        if (local == cloudContent)
        {
            PatchHelper.Log(PullSkippingIdentical(sync.Path));
            return TransferDecision.Skip();
        }

        return TransferDecision.Transfer(backUpExisting: true);
    }
}
