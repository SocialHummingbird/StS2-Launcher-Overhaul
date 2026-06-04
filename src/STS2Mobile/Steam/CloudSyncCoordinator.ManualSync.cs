using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private const int ManualSyncPerPathTimeoutMs = 45_000;
    private const int ManualSyncOverallTimeoutMs = 180_000;
    private const string ManualPullDownloadOperation = "ManualPull download";

    internal static Task ManualPushAllAsync(string accountName, string refreshToken)
        => ManualPushPlan.RunAsync(accountName, refreshToken);

    internal static Task ManualPullAllAsync(string accountName, string refreshToken)
        => ManualPullPlan.RunAsync(accountName, refreshToken);
}
