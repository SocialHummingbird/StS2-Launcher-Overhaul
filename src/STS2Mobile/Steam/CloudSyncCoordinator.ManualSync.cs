using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private const int ManualSyncPerPathTimeoutMs = 45_000;
    private const int ManualSyncOverallTimeoutMs = 180_000;
    private const string ManualPullDownloadOperation = "ManualPull download";

    internal static Task<ManualCloudSyncResult> ManualPushAllAsync(
        string accountName,
        string refreshToken
    )
        => ManualPushAllAsync(
            accountName,
            refreshToken,
            CancellationToken.None
        );

    internal static Task<ManualCloudSyncResult> ManualPushAllAsync(
        string accountName,
        string refreshToken,
        CancellationToken cancellationToken
    )
        => ManualPushAllAsync(
            accountName,
            refreshToken,
            new CloudOperationProgressTracker(CloudOperationKind.Push),
            cancellationToken
        );

    internal static Task<ManualCloudSyncResult> ManualPushAllAsync(
        ISaveStore local,
        ICloudSaveStore cloud,
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken
    )
        => RunManualSyncAsync(
            local,
            cloud,
            ManualSyncPlan.Push,
            progress,
            cancellationToken
        );

    internal static Task<ManualCloudSyncResult> ManualPushAllAsync(
        string accountName,
        string refreshToken,
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken
    )
        => RunManualSyncAsync(
            accountName,
            refreshToken,
            ManualSyncPlan.Push,
            progress,
            cancellationToken
        );

    internal static Task<ManualCloudSyncResult> ManualPullAllAsync(
        string accountName,
        string refreshToken
    )
        => ManualPullAllAsync(
            accountName,
            refreshToken,
            new CloudOperationProgressTracker(CloudOperationKind.Pull),
            CancellationToken.None
        );

    internal static Task<ManualCloudSyncResult> ManualPullAllAsync(
        string accountName,
        string refreshToken,
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken = default
    )
        => RunManualSyncAsync(
            accountName,
            refreshToken,
            ManualSyncPlan.Pull,
            progress,
            cancellationToken
        );

    internal static Task<ManualCloudSyncResult> ManualPullAllAsync(
        ISaveStore local,
        ICloudSaveStore cloud,
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken
    )
        => RunManualSyncAsync(
            local,
            cloud,
            ManualSyncPlan.Pull,
            progress,
            cancellationToken
        );
}
