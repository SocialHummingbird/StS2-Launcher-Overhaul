#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private enum AutomaticRemoteContextStatus
    {
        Exact,
        Missing,
        Unreadable,
        Mismatch,
    }

    private readonly record struct AutomaticRemoteContextProbe(
        AutomaticRemoteContextStatus Status,
        AutomaticFileState MarkerState,
        string Problem,
        AutomaticSyncEvidenceDetail Detail
    );

    private sealed class AutomaticSyncEvidenceScope
    {
        internal string ContextSha256 { get; private set; } = "";

        internal void Bind(SaveContext context)
            => ContextSha256 = SaveEvidenceEvents.ContextSha256(context);
    }

    internal static async Task<AutomaticSyncResult>
        RecoverAutomaticSyncAsync(
            string accountName,
            string refreshToken,
            CloudOperationProgressTracker progress,
            CancellationToken cancellationToken
    )
    {
        RequireSaveRecoverySyncReleased(
            CloudSaveStoreFactory.CreateLocalStore()
        );
        var stores = CloudSaveStoreFactory.CreateTransferCloudSaveStore(
            accountName,
            refreshToken
        );
        try
        {
            return await RecoverAutomaticSyncAsync(
                stores.LocalStore,
                stores.CloudStore,
                progress,
                cancellationToken
            ).ConfigureAwait(false);
        }
        finally
        {
            SteamKit2CloudSaveStore.DisposeActive(
                "[Cloud] Closing launcher automatic-sync store"
            );
        }
    }

    internal static Task<AutomaticSyncResult>
        RecoverAutomaticSyncAsync(
            ISaveStore local,
            ICloudSaveStore cloud,
            CloudOperationProgressTracker progress,
            CancellationToken cancellationToken
        )
        => RunAutomaticSyncWithEvidenceAsync(
            AutomaticSyncEvidenceOperation.Recover,
            evidence => RunWithSaveTransferAndRecoveryGateAsync(
                () => RecoverAutomaticSyncCoreAsync(
                    local,
                    cloud,
                    progress,
                    cancellationToken,
                    evidence
                ),
                cancellationToken
            )
        );

    private static async Task<AutomaticSyncResult>
        RecoverAutomaticSyncCoreAsync(
            ISaveStore local,
            ICloudSaveStore cloud,
            CloudOperationProgressTracker progress,
            CancellationToken cancellationToken,
            AutomaticSyncEvidenceScope evidence
        )
    {
        await RequireSaveRecoverySyncReadyAsync(local, cancellationToken)
            .ConfigureAwait(false);
        var sync = CreateAutomaticSyncContext(
            local,
            cloud,
            progress,
            cancellationToken
        );
        var pending = await ReadPendingAutomaticSyncAsync(
            local,
            cancellationToken
        ).ConfigureAwait(false);
        if (pending is null)
        {
            return AutomaticResult(
                AutomaticSyncOutcome.Synchronized,
                "No automatic save synchronization is pending",
                AutomaticSyncEvidenceDetail.NoPendingWork
            );
        }

        NormalizePending(pending);
        var recordedContext = SaveContext.ParseMarker(pending.ContextMarker);
        var context = await AuthenticateAutomaticContextAsync(
            sync,
            recordedContext.Namespace,
            recordedContext.RuntimeIdentity,
            recordedContext.ModSetFingerprint,
            evidence
        ).ConfigureAwait(false);
        try
        {
            recordedContext.RequireExactMatch(context);
        }
        catch (InvalidOperationException ex)
        {
            return AutomaticConflict(
                ex.Message,
                SaveEvidenceEvents.ContextMismatch(
                    recordedContext,
                    context
                )
            );
        }

        await VerifyPendingBeforeGameSnapshotAsync(
            local,
            context,
            pending,
            cancellationToken
        ).ConfigureAwait(false);

        return pending.Phase switch
        {
            AutomaticSyncGameRunningPhase
                => await ReconcilePendingGameAsync(
                    sync,
                    local,
                    context,
                    pending
                ).ConfigureAwait(false),
            AutomaticSyncUploadingPhase or AutomaticSyncDownloadingPhase
                => await ResumePendingTransferAsync(
                    sync,
                    local,
                    context,
                    pending
                ).ConfigureAwait(false),
            _ => throw new InvalidDataException(
                "Automatic sync pending record has an invalid phase"
            ),
        };
    }

    internal static async Task<AutomaticSyncResult>
        ReconcileAutomaticSyncAsync(
            string accountName,
            string refreshToken,
            SaveNamespace saveNamespace,
            string runtimeIdentity,
            string modSetFingerprint,
            AutomaticSyncSourceChoice? sourceChoice,
            CloudOperationProgressTracker progress,
            CancellationToken cancellationToken
    )
    {
        RequireSaveRecoverySyncReleased(
            CloudSaveStoreFactory.CreateLocalStore()
        );
        var stores = CloudSaveStoreFactory.CreateTransferCloudSaveStore(
            accountName,
            refreshToken
        );
        try
        {
            return await ReconcileAutomaticSyncAsync(
                stores.LocalStore,
                stores.CloudStore,
                saveNamespace,
                runtimeIdentity,
                modSetFingerprint,
                sourceChoice,
                progress,
                cancellationToken
            ).ConfigureAwait(false);
        }
        finally
        {
            SteamKit2CloudSaveStore.DisposeActive(
                "[Cloud] Closing launcher automatic-sync store"
            );
        }
    }

    internal static Task<AutomaticSyncResult>
        ReconcileAutomaticSyncAsync(
            ISaveStore local,
            ICloudSaveStore cloud,
            SaveNamespace saveNamespace,
            string runtimeIdentity,
            string modSetFingerprint,
            AutomaticSyncSourceChoice? sourceChoice,
            CloudOperationProgressTracker progress,
            CancellationToken cancellationToken
        )
        => RunAutomaticSyncWithEvidenceAsync(
            AutomaticSyncEvidenceOperation.Reconcile,
            evidence => RunWithSaveTransferAndRecoveryGateAsync(
                () => ReconcileAutomaticSyncCoreAsync(
                    local,
                    cloud,
                    saveNamespace,
                    runtimeIdentity,
                    modSetFingerprint,
                    sourceChoice,
                    progress,
                    cancellationToken,
                    evidence
                ),
                cancellationToken
            )
        );

    private static async Task<AutomaticSyncResult>
        ReconcileAutomaticSyncCoreAsync(
            ISaveStore local,
            ICloudSaveStore cloud,
            SaveNamespace saveNamespace,
            string runtimeIdentity,
            string modSetFingerprint,
            AutomaticSyncSourceChoice? sourceChoice,
            CloudOperationProgressTracker progress,
            CancellationToken cancellationToken,
            AutomaticSyncEvidenceScope evidence
        )
    {
        await RequireSaveRecoverySyncReadyAsync(local, cancellationToken)
            .ConfigureAwait(false);
        if (HasPendingAutomaticSync(local))
        {
            return AutomaticResult(
                AutomaticSyncOutcome.PendingRecoveryRequired,
                "An earlier automatic save operation must be recovered before reconciliation"
            );
        }

        var sync = CreateAutomaticSyncContext(
            local,
            cloud,
            progress,
            cancellationToken
        );
        var context = await AuthenticateAutomaticContextAsync(
            sync,
            saveNamespace,
            runtimeIdentity,
            modSetFingerprint,
            evidence
        ).ConfigureAwait(false);
        return await ReconcileWithoutPendingAsync(
            sync,
            local,
            context,
            sourceChoice
        ).ConfigureAwait(false);
    }

    internal static async Task<AutomaticSyncResult>
        BeginAutomaticGameSessionAsync(
            string accountName,
            string refreshToken,
            SaveNamespace saveNamespace,
            string runtimeIdentity,
            string modSetFingerprint,
            CloudOperationProgressTracker progress,
            CancellationToken cancellationToken
    )
    {
        RequireSaveRecoverySyncReleased(
            CloudSaveStoreFactory.CreateLocalStore()
        );
        var stores = CloudSaveStoreFactory.CreateTransferCloudSaveStore(
            accountName,
            refreshToken
        );
        try
        {
            return await BeginAutomaticGameSessionAsync(
                stores.LocalStore,
                stores.CloudStore,
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
                "[Cloud] Closing launcher automatic-sync store"
            );
        }
    }

    internal static Task<AutomaticSyncResult>
        BeginAutomaticGameSessionAsync(
            ISaveStore local,
            ICloudSaveStore cloud,
            SaveNamespace saveNamespace,
            string runtimeIdentity,
            string modSetFingerprint,
            CloudOperationProgressTracker progress,
            CancellationToken cancellationToken
        )
        => RunAutomaticSyncWithEvidenceAsync(
            AutomaticSyncEvidenceOperation.BeginGame,
            evidence => RunWithSaveTransferAndRecoveryGateAsync(
                () => BeginAutomaticGameSessionCoreAsync(
                    local,
                    cloud,
                    saveNamespace,
                    runtimeIdentity,
                    modSetFingerprint,
                    progress,
                    cancellationToken,
                    evidence
                ),
                cancellationToken
            )
        );

    private static async Task<AutomaticSyncResult>
        BeginAutomaticGameSessionCoreAsync(
            ISaveStore local,
            ICloudSaveStore cloud,
            SaveNamespace saveNamespace,
            string runtimeIdentity,
            string modSetFingerprint,
            CloudOperationProgressTracker progress,
            CancellationToken cancellationToken,
            AutomaticSyncEvidenceScope evidence
        )
    {
        await RequireSaveRecoverySyncReadyAsync(local, cancellationToken)
            .ConfigureAwait(false);
        if (HasPendingAutomaticSync(local))
        {
            return AutomaticResult(
                AutomaticSyncOutcome.PendingRecoveryRequired,
                "A second game session cannot begin while automatic save reconciliation is pending"
            );
        }

        var sync = CreateAutomaticSyncContext(
            local,
            cloud,
            progress,
            cancellationToken
        );
        var context = await AuthenticateAutomaticContextAsync(
            sync,
            saveNamespace,
            runtimeIdentity,
            modSetFingerprint,
            evidence
        ).ConfigureAwait(false);
        var reconciled = await ReconcileWithoutPendingAsync(
            sync,
            local,
            context,
            sourceChoice: null
        ).ConfigureAwait(false);
        if (reconciled.Outcome != AutomaticSyncOutcome.Synchronized)
            return reconciled;

        var marker = await ProbeRemoteContextAsync(sync, context)
            .ConfigureAwait(false);
        if (marker.Status != AutomaticRemoteContextStatus.Exact)
            return AutomaticConflict(
                RemoteContextProblem(marker),
                marker.Detail
            );

        var localSnapshot = await CaptureAutomaticSnapshotAsync(
            sync,
            CloudOperationKind.Push,
            context
        ).ConfigureAwait(false);
        var remoteSnapshot = await CaptureAutomaticSnapshotAsync(
            sync,
            CloudOperationKind.Pull,
            context
        ).ConfigureAwait(false);
        var baseline = await ReadAutomaticBaselineAsync(
            local,
            context,
            cancellationToken
        ).ConfigureAwait(false) ?? throw new InvalidDataException(
            "Automatic sync baseline disappeared before game launch"
        );
        if (!localSnapshot.Manifest.ContentEquals(baseline.LocalManifest)
            || !remoteSnapshot.Manifest.ContentEquals(baseline.RemoteManifest))
        {
            return AutomaticConflict(
                "Local or Steam saves changed while the game session was being prepared"
            );
        }

        localSnapshot.SourceLabel = "before-game";
        var retainedSnapshot = await WriteRetainedSnapshotAsync(
            local,
            context,
            localSnapshot,
            cancellationToken
        ).ConfigureAwait(false);

        // Re-read both sides after the immutable snapshot write. The pending record
        // is the final persisted transition before gameplay may start.
        var finalLocal = await CaptureAutomaticSnapshotAsync(
            sync,
            CloudOperationKind.Push,
            context
        ).ConfigureAwait(false);
        var finalRemote = await CaptureAutomaticSnapshotAsync(
            sync,
            CloudOperationKind.Pull,
            context
        ).ConfigureAwait(false);
        var finalMarker = await ProbeRemoteContextAsync(sync, context)
            .ConfigureAwait(false);
        if (finalMarker.Status != AutomaticRemoteContextStatus.Exact
            || !finalLocal.Manifest.ContentEquals(localSnapshot.Manifest)
            || !finalRemote.Manifest.ContentEquals(remoteSnapshot.Manifest))
        {
            return AutomaticConflict(
                "Local or Steam saves changed before the game-session record could be committed"
            );
        }

        await WritePendingAutomaticSyncAsync(
            local,
            new AutomaticSyncPendingDocument
            {
                Phase = AutomaticSyncGameRunningPhase,
                ContextMarker = context.SerializeMarker(),
                LocalBaseline = finalLocal.Manifest,
                RemoteBaseline = finalRemote.Manifest,
                RemoteMarkerBaseline = finalMarker.MarkerState,
                BeforeGameSnapshotPath = retainedSnapshot.Path,
                BeforeGameSnapshotSha256 = retainedSnapshot.Sha256,
            },
            cancellationToken
        ).ConfigureAwait(false);
        await PruneRetainedSnapshotsAsync(
            local,
            context,
            new[] { retainedSnapshot.Path },
            cancellationToken
        ).ConfigureAwait(false);
        return AutomaticResult(
            AutomaticSyncOutcome.GameSessionPrepared,
            "Automatic save reconciliation completed and the before-game snapshot was verified"
        );
    }

    private static ManualSyncContext CreateAutomaticSyncContext(
        ISaveStore local,
        ICloudSaveStore cloud,
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(local);
        ArgumentNullException.ThrowIfNull(cloud);
        ArgumentNullException.ThrowIfNull(progress);
        return new ManualSyncContext(
            local,
            cloud,
            ManualSyncBudget.StartingNow(),
            progress,
            cancellationToken
        );
    }

    private static async Task<SaveContext> AuthenticateAutomaticContextAsync(
        ManualSyncContext sync,
        SaveNamespace saveNamespace,
        string runtimeIdentity,
        string modSetFingerprint,
        AutomaticSyncEvidenceScope evidence
    )
    {
        var context = SaveContext.Create(
            await sync.AuthenticateAsync().ConfigureAwait(false),
            saveNamespace,
            runtimeIdentity,
            modSetFingerprint
        );
        RequireApprovedRecoveryTransferContext(sync.LocalStore, context);
        evidence.Bind(context);
        return context;
    }

    private static AutomaticSyncResult AutomaticResult(
        AutomaticSyncOutcome outcome,
        string message,
        AutomaticSyncEvidenceDetail detail =
            AutomaticSyncEvidenceDetail.Unspecified,
        bool remoteVerified = false
    )
        => new(
            outcome,
            message,
            detail == AutomaticSyncEvidenceDetail.Unspecified
                ? outcome switch
                {
                    AutomaticSyncOutcome.Synchronized
                        => AutomaticSyncEvidenceDetail.Verified,
                    AutomaticSyncOutcome.SourceChoiceRequired
                        => AutomaticSyncEvidenceDetail.SourceChoiceRequired,
                    AutomaticSyncOutcome.Conflict
                        => AutomaticSyncEvidenceDetail.IndependentChange,
                    AutomaticSyncOutcome.GameSessionPrepared
                        => AutomaticSyncEvidenceDetail.SessionPrepared,
                    AutomaticSyncOutcome.PendingRecoveryRequired
                        => AutomaticSyncEvidenceDetail.PendingRecovery,
                    _ => AutomaticSyncEvidenceDetail.Unspecified,
                }
                : detail,
            remoteVerified
        );

    private static AutomaticSyncResult AutomaticConflict(
        string message,
        AutomaticSyncEvidenceDetail detail =
            AutomaticSyncEvidenceDetail.IndependentChange
    )
        => AutomaticResult(AutomaticSyncOutcome.Conflict, message, detail);

    private static async Task<AutomaticSyncResult>
        RunAutomaticSyncWithEvidenceAsync(
            AutomaticSyncEvidenceOperation operation,
            Func<AutomaticSyncEvidenceScope, Task<AutomaticSyncResult>> action
        )
    {
        var evidence = new AutomaticSyncEvidenceScope();
        try
        {
            var result = await action(evidence).ConfigureAwait(false);
            SaveEvidenceEvents.AutomaticSyncTerminal(
                operation,
                result.Outcome,
                result.Detail,
                evidence.ContextSha256,
                result.RemoteVerified
            );
            return result;
        }
        catch (Exception ex)
        {
            SaveEvidenceEvents.AutomaticSyncFailure(
                operation,
                ex,
                evidence.ContextSha256
            );
            throw;
        }
    }
}
