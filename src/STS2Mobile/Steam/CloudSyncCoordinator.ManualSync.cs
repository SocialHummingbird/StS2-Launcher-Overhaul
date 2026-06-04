using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private const int ManualSyncPerPathTimeoutMs = 45_000;
    private const int ManualSyncOverallTimeoutMs = 180_000;
    private const string ManualPullDownloadOperation = "ManualPull download";

    internal static Task ManualPushAllAsync(string accountName, string refreshToken)
        => RunManualSyncAsync(
            accountName,
            refreshToken,
            sync => sync.DiscoverLocalPaths(),
            PushStarting,
            SaveBackups.CloudBeforeManualPushAsync,
            PushBackedUpCloudFiles,
            RunManualPushUploadsAsync
        );

    internal static Task ManualPullAllAsync(string accountName, string refreshToken)
        => RunManualSyncAsync(
            accountName,
            refreshToken,
            sync => sync.DiscoverCloudPaths(),
            PullStarting,
            SaveBackups.LocalBeforeManualPullAsync,
            PullBackedUpLocalFiles,
            RunManualPullDownloadsAsync
        );
}
