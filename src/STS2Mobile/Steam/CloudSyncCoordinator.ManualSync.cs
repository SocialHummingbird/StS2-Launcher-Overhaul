using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private const int ManualSyncPerPathTimeoutMs = 45_000;
    private const int ManualSyncOverallTimeoutMs = 180_000;

    internal static bool HasTransferableLocalSaveContent(
        SaveNamespace saveNamespace
    )
    {
        try
        {
            return SaveTransferAllowlist.HasAnyContent(
                CloudSaveStoreFactory.CreateLocalStore(),
                saveNamespace
            );
        }
        catch (System.Exception ex)
        {
            PatchHelper.Log(
                $"[Cloud] Could not inspect the local transfer allowlist: "
                    + ex.Message
            );
            return false;
        }
    }

    internal static bool HasIncompletePullMarker()
    {
        try
        {
            var local = CloudSaveStoreFactory.CreateLocalStore();
            return local.FileExists(
                    IncompletePullMarkerPath(SaveNamespace.Vanilla)
                )
                || local.FileExists(
                    IncompletePullMarkerPath(SaveNamespace.Modded)
                );
        }
        catch (System.Exception ex)
        {
            PatchHelper.Log(
                "[Cloud] Could not verify incomplete Pull state: "
                    + ex.Message
            );
            return true;
        }
    }

    internal static Task<ManualCloudSyncResult> ManualPushAllAsync(
        string accountName,
        string refreshToken,
        SaveNamespace saveNamespace,
        string runtimeIdentity,
        string modSetFingerprint,
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken
    )
        => RunManualSyncAsync(
            accountName,
            refreshToken,
            CloudOperationKind.Push,
            saveNamespace,
            runtimeIdentity,
            modSetFingerprint,
            progress,
            cancellationToken
        );

    internal static Task<ManualCloudSyncResult> ManualPushAllAsync(
        ISaveStore local,
        ICloudSaveStore cloud,
        SaveNamespace saveNamespace,
        string runtimeIdentity,
        string modSetFingerprint,
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken
    )
        => RunManualSyncAsync(
            local,
            cloud,
            CloudOperationKind.Push,
            saveNamespace,
            runtimeIdentity,
            modSetFingerprint,
            progress,
            cancellationToken
        );

    internal static Task<ManualCloudSyncResult> ManualPullAllAsync(
        string accountName,
        string refreshToken,
        SaveNamespace saveNamespace,
        string runtimeIdentity,
        string modSetFingerprint,
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken = default
    )
        => RunManualSyncAsync(
            accountName,
            refreshToken,
            CloudOperationKind.Pull,
            saveNamespace,
            runtimeIdentity,
            modSetFingerprint,
            progress,
            cancellationToken
        );

    internal static Task<ManualCloudSyncResult> ManualPullAllAsync(
        ISaveStore local,
        ICloudSaveStore cloud,
        SaveNamespace saveNamespace,
        string runtimeIdentity,
        string modSetFingerprint,
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken
    )
        => RunManualSyncAsync(
            local,
            cloud,
            CloudOperationKind.Pull,
            saveNamespace,
            runtimeIdentity,
            modSetFingerprint,
            progress,
            cancellationToken
        );
}
