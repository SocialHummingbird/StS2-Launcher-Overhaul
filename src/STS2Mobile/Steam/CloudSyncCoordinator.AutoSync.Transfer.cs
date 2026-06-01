using System;
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
        private bool ShouldTransfer => _action == Action.Transfer;
        private string? ExistingContentToBackUp { get; }
        private bool BackUpExisting { get; }

        internal static TransferDecision Transfer(
            string? existingContentToBackUp = null,
            bool backUpExisting = false
        )
            => new(Action.Transfer, existingContentToBackUp, backUpExisting);

        internal static TransferDecision Skip()
            => new(Action.Skip, existingContentToBackUp: null, backUpExisting: false);

        internal async Task ApplyPullAsync(
            AutoSyncContext sync,
            string cloudContent,
            Func<string, string> message
        )
        {
            if (!ShouldTransfer)
                return;

            await sync.PullCloudContentAsync(
                cloudContent,
                message,
                BackUpExisting
            );
        }

        internal void ApplyPush(
            AutoSyncContext sync,
            string localContent,
            Func<string, string> message
        )
        {
            if (!ShouldTransfer)
                return;

            sync.PushLocalContent(localContent, ExistingContentToBackUp, message);
        }
    }

    private static async Task PushFileAsync(AutoSyncContext sync)
    {
        var local = sync.ReadLocalContent();
        if (local == null)
            return;

        var push = await GetPushDecisionAsync(sync, local);
        push.ApplyPush(sync, local, PushUploaded);
    }

    private static async Task PullFileAsync(AutoSyncContext sync)
    {
        if (!sync.CloudFileExists())
            return;

        string cloudContent = await sync.ReadCloudContentAsync(PullCloudFileOperation);

        var pull = GetPullDecision(sync, cloudContent);
        await pull.ApplyPullAsync(sync, cloudContent, PullDownloaded);
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
            sync.Log(PushSkippingIdentical);
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
            sync.Log(PullSkippingIdentical);
            return TransferDecision.Skip();
        }

        return TransferDecision.Transfer(backUpExisting: true);
    }
}
