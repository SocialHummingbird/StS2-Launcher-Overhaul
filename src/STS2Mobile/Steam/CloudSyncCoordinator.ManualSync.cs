using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private const int ManualSyncPerPathTimeoutMs = 45_000;
    private const int ManualSyncOverallTimeoutMs = 180_000;
    private const string ManualPullDownloadOperation = "ManualPull download";

    internal static Task<string> ManualPushAllAsync(string accountName, string refreshToken)
        => RunManualSyncAsync(
            accountName,
            refreshToken,
            ManualSyncPlan.Push
        );

    internal static Task<string> ManualPullAllAsync(string accountName, string refreshToken)
        => RunManualSyncAsync(
            accountName,
            refreshToken,
            ManualSyncPlan.Pull
        );
}
