namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private const string CloudLogPrefix = "[Cloud]";
    private const string PullOperation = "Pull";
    private const string PushOperation = "Push";
    private const string SyncOperation = "Sync";

    private static string PullMessage(string message)
        => CloudOperationMessage(PullOperation, message);

    private static string PushMessage(string message)
        => CloudOperationMessage(PushOperation, message);

    private static string SyncMessage(string message)
        => CloudOperationMessage(SyncOperation, message);

    private static string CloudOperationMessage(string operation, string message)
        => CloudMessage($"{operation}: {message}");

    private static string CloudMessage(string message)
        => $"{CloudLogPrefix} {message}";
}
