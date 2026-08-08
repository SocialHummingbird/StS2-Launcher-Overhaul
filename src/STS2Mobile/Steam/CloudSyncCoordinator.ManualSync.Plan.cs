using System;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static async Task<ManualCloudSyncResult> RunManualSyncAsync(
        string accountName,
        string refreshToken,
        CloudOperationKind direction,
        SaveNamespace saveNamespace,
        string runtimeIdentity,
        string modSetFingerprint,
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken
    )
    {
        try
        {
            RequireSaveRecoverySyncReleased(
                CloudSaveStoreFactory.CreateLocalStore()
            );
            var stores = CloudSaveStoreFactory.CreateTransferCloudSaveStore(
                accountName,
                refreshToken
            );
            return await RunManualSyncAsync(
                stores.LocalStore,
                stores.CloudStore,
                direction,
                saveNamespace,
                runtimeIdentity,
                modSetFingerprint,
                progress,
                cancellationToken
            ).ConfigureAwait(false);
        }
        finally
        {
            SteamKit2CloudSaveStore.DisposeActive(
                "[Cloud] Closing launcher manual-sync store"
            );
        }
    }

    private static Task<ManualCloudSyncResult> RunManualSyncAsync(
        ISaveStore local,
        ICloudSaveStore cloud,
        CloudOperationKind direction,
        SaveNamespace saveNamespace,
        string runtimeIdentity,
        string modSetFingerprint,
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken
    )
        => RunWithSaveTransferAndRecoveryGateAsync(
            () => RunManualSyncCoreAsync(
                local,
                cloud,
                direction,
                saveNamespace,
                runtimeIdentity,
                modSetFingerprint,
                progress,
                cancellationToken
            ),
            cancellationToken
        );

    private static async Task<ManualCloudSyncResult> RunManualSyncCoreAsync(
        ISaveStore local,
        ICloudSaveStore cloud,
        CloudOperationKind direction,
        SaveNamespace saveNamespace,
        string runtimeIdentity,
        string modSetFingerprint,
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(local);
        ArgumentNullException.ThrowIfNull(cloud);
        ArgumentNullException.ThrowIfNull(progress);

        cancellationToken.ThrowIfCancellationRequested();
        await RequireSaveRecoverySyncReadyAsync(local, cancellationToken)
            .ConfigureAwait(false);
        progress.Preparing("Authenticating with Steam Cloud");
        try
        {
            var sync = new ManualSyncContext(
                local,
                cloud,
                ManualSyncBudget.StartingNow(),
                progress,
                cancellationToken
            );
            var result = await RunTransferAsync(
                sync,
                direction,
                saveNamespace,
                runtimeIdentity,
                modSetFingerprint
            ).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            progress.Completed("All save files were transferred and verified");
            return result;
        }
        catch (Exception ex)
        {
            progress.Failed(ex.Message);
            throw;
        }
    }
}
