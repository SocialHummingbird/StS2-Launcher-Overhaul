namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private const string CloudLogPrefix = "[Cloud]";
    private const string PullOperation = "Pull";
    private const string PushOperation = "Push";
    private const string SyncOperation = "Sync";

    private static readonly CloudOperationMessages PullMessages = new(PullOperation);
    private static readonly CloudOperationMessages PushMessages = new(PushOperation);
    private static readonly CloudOperationMessages SyncMessages = new(SyncOperation);

    private readonly struct CloudOperationMessages
    {
        private readonly string _operation;

        internal CloudOperationMessages(string operation)
        {
            _operation = operation;
        }

        internal string Format(string message)
            => CloudMessage($"{_operation}: {message}");
    }

    private static string CloudMessage(string message)
        => $"{CloudLogPrefix} {message}";
}
