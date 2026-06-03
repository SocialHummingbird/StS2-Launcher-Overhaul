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
            Func<string, string>? transferMessage,
            string? existingContentToBackUp,
            bool backUpExisting
        )
        {
            _action = action;
            TransferMessage = transferMessage;
            ExistingContentToBackUp = existingContentToBackUp;
            BackUpExisting = backUpExisting;
        }

        private readonly Action _action;
        private bool ShouldTransfer => _action == Action.Transfer;
        private Func<string, string>? TransferMessage { get; }
        private string? ExistingContentToBackUp { get; }
        private bool BackUpExisting { get; }

        internal static TransferDecision Transfer(
            Func<string, string> transferMessage,
            string? existingContentToBackUp = null,
            bool backUpExisting = false
        )
            => new(
                Action.Transfer,
                transferMessage,
                existingContentToBackUp,
                backUpExisting
            );

        internal static TransferDecision Skip()
            => new(
                Action.Skip,
                transferMessage: null,
                existingContentToBackUp: null,
                backUpExisting: false
            );

        internal async Task ApplyPullAsync(
            AutoSyncContext sync,
            string cloudContent
        )
        {
            if (!ShouldTransfer)
                return;

            await sync.PullCloudContentAsync(
                cloudContent,
                TransferMessageOrThrow(),
                BackUpExisting
            );
        }

        internal void ApplyPush(
            AutoSyncContext sync,
            string localContent
        )
        {
            if (!ShouldTransfer)
                return;

            sync.PushLocalContent(
                localContent,
                ExistingContentToBackUp,
                TransferMessageOrThrow()
            );
        }

        private Func<string, string> TransferMessageOrThrow()
            => TransferMessage
                ?? throw new InvalidOperationException(
                    "Transfer decision has no transfer message"
                );
    }

    private static async Task PushFileAsync(AutoSyncContext sync)
    {
        var local = sync.ReadLocalContent();
        if (local == null)
            return;

        var push = await GetPushDecisionAsync(sync, local);
        push.ApplyPush(sync, local);
    }

    private static async Task PullFileAsync(AutoSyncContext sync)
    {
        if (!sync.CloudFileExists())
            return;

        string cloudContent = await sync.ReadCloudContentAsync(PullCloudFileOperation);

        var pull = GetPullDecision(sync, cloudContent);
        await pull.ApplyPullAsync(sync, cloudContent);
    }

    private static async Task<TransferDecision> GetPushDecisionAsync(
        AutoSyncContext sync,
        string localContent
    )
    {
        if (!sync.CloudFileExists())
            return TransferDecision.Transfer(PushUploaded);

        string cloudContent = await sync.ReadCloudContentAsync(ReadCloudFileOperation);
        if (localContent == cloudContent)
        {
            sync.Log(PushSkippingIdentical);
            return TransferDecision.Skip();
        }

        return TransferDecision.Transfer(
            PushUploaded,
            existingContentToBackUp: cloudContent
        );
    }

    private static TransferDecision GetPullDecision(
        AutoSyncContext sync,
        string cloudContent
    )
    {
        var local = sync.ReadLocalContent();
        if (local == null)
            return TransferDecision.Transfer(PullDownloaded);

        if (local == cloudContent)
        {
            sync.Log(PullSkippingIdentical);
            return TransferDecision.Skip();
        }

        return TransferDecision.Transfer(PullDownloaded, backUpExisting: true);
    }
}
