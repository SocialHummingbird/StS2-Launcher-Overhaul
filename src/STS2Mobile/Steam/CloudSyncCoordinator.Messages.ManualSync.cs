namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static string ManualPushBudgetExceeded() =>
        PushMessage("manual timeout: exceeded overall manual sync budget");

    private static string ManualPullBudgetExceeded() =>
        PullMessage("manual timeout: exceeded overall manual sync budget");
}
