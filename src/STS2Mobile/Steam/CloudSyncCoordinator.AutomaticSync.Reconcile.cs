#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static async Task<AutomaticSyncResult> ReconcileWithoutPendingAsync(
        ManualSyncContext sync,
        ISaveStore local,
        SaveContext context,
        AutomaticSyncSourceChoice? sourceChoice
    )
    {
        var marker = await ProbeRemoteContextAsync(sync, context)
            .ConfigureAwait(false);
        if (marker.Status == AutomaticRemoteContextStatus.Mismatch)
            return AutomaticConflict(marker.Problem, marker.Detail);

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
            sync.CancellationToken
        ).ConfigureAwait(false);

        if (baseline is null)
        {
            if (sourceChoice is null)
            {
                return AutomaticResult(
                    AutomaticSyncOutcome.SourceChoiceRequired,
                    "No trusted automatic-sync baseline exists. Choose Local or Steam once before Play."
                );
            }

            if (sourceChoice == AutomaticSyncSourceChoice.Local)
            {
                return await StartAutomaticTransferAsync(
                    sync,
                    local,
                    context,
                    CloudOperationKind.Push,
                    localSnapshot.Manifest,
                    remoteSnapshot.Manifest,
                    localSnapshot.Manifest,
                    marker,
                    allowUnreadableMarkerAdoption:
                        marker.Status is AutomaticRemoteContextStatus.Missing
                            or AutomaticRemoteContextStatus.Unreadable
                ).ConfigureAwait(false);
            }

            if (marker.Status is not (
                    AutomaticRemoteContextStatus.Exact
                    or AutomaticRemoteContextStatus.Missing
                ))
            {
                return AutomaticConflict(
                    "Steam cannot be chosen because its save-context marker is unreadable",
                    marker.Detail
                );
            }
            return await StartAutomaticTransferAsync(
                sync,
                local,
                context,
                CloudOperationKind.Pull,
                localSnapshot.Manifest,
                remoteSnapshot.Manifest,
                remoteSnapshot.Manifest,
                marker,
                allowUnreadableMarkerAdoption: false
            ).ConfigureAwait(false);
        }

        if (marker.Status != AutomaticRemoteContextStatus.Exact)
            return AutomaticConflict(
                RemoteContextProblem(marker),
                marker.Detail
            );

        var localChanged = !localSnapshot.Manifest.ContentEquals(
            baseline.LocalManifest
        );
        var remoteChanged = !remoteSnapshot.Manifest.ContentEquals(
            baseline.RemoteManifest
        );
        if (localSnapshot.Manifest.ContentEquals(remoteSnapshot.Manifest)
            || (!localChanged && !remoteChanged))
        {
            await WriteAutomaticBaselineAsync(
                local,
                context,
                localSnapshot.Manifest,
                remoteSnapshot.Manifest,
                sync.CancellationToken
            ).ConfigureAwait(false);
            return AutomaticResult(
                AutomaticSyncOutcome.Synchronized,
                "Local and Steam saves match their trusted baseline",
                remoteVerified: true
            );
        }

        if (localChanged && !remoteChanged)
        {
            return await StartAutomaticTransferAsync(
                sync,
                local,
                context,
                CloudOperationKind.Push,
                baseline.LocalManifest,
                baseline.RemoteManifest,
                localSnapshot.Manifest,
                marker,
                allowUnreadableMarkerAdoption: false
            ).ConfigureAwait(false);
        }

        if (!localChanged && remoteChanged)
        {
            return await StartAutomaticTransferAsync(
                sync,
                local,
                context,
                CloudOperationKind.Pull,
                baseline.LocalManifest,
                baseline.RemoteManifest,
                remoteSnapshot.Manifest,
                marker,
                allowUnreadableMarkerAdoption: false
            ).ConfigureAwait(false);
        }

        return AutomaticConflict(
            "Local and Steam saves changed differently from their trusted baseline",
            AutomaticSyncEvidenceDetail.LocalAndRemoteDiverged
        );
    }

    private static async Task<AutomaticSyncResult> ReconcilePendingGameAsync(
        ManualSyncContext sync,
        ISaveStore local,
        SaveContext context,
        AutomaticSyncPendingDocument pending
    )
    {
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
        var localChanged = !localSnapshot.Manifest.ContentEquals(
            pending.LocalBaseline
        );
        var remoteChanged = !remoteSnapshot.Manifest.ContentEquals(
            pending.RemoteBaseline
        );
        if (localSnapshot.Manifest.ContentEquals(remoteSnapshot.Manifest)
            || (!localChanged && !remoteChanged))
        {
            return await CompleteAutomaticSyncAsync(
                local,
                context,
                localSnapshot.Manifest,
                remoteSnapshot.Manifest,
                sync
            ).ConfigureAwait(false);
        }

        if (localChanged && !remoteChanged)
        {
            return await StartAutomaticTransferAsync(
                sync,
                local,
                context,
                CloudOperationKind.Push,
                pending.LocalBaseline,
                pending.RemoteBaseline,
                localSnapshot.Manifest,
                marker,
                allowUnreadableMarkerAdoption: false,
                beforeGameSnapshotPath: pending.BeforeGameSnapshotPath,
                beforeGameSnapshotSha256: pending.BeforeGameSnapshotSha256
            ).ConfigureAwait(false);
        }

        if (!localChanged && remoteChanged)
        {
            return await StartAutomaticTransferAsync(
                sync,
                local,
                context,
                CloudOperationKind.Pull,
                pending.LocalBaseline,
                pending.RemoteBaseline,
                remoteSnapshot.Manifest,
                marker,
                allowUnreadableMarkerAdoption: false,
                beforeGameSnapshotPath: pending.BeforeGameSnapshotPath,
                beforeGameSnapshotSha256: pending.BeforeGameSnapshotSha256
            ).ConfigureAwait(false);
        }

        return AutomaticConflict(
            "Local and Steam saves changed differently while the game session was active",
            AutomaticSyncEvidenceDetail.LocalAndRemoteDiverged
        );
    }

    private static async Task<AutomaticSyncResult> StartAutomaticTransferAsync(
        ManualSyncContext sync,
        ISaveStore local,
        SaveContext context,
        CloudOperationKind direction,
        AutomaticSaveManifest localBaseline,
        AutomaticSaveManifest remoteBaseline,
        AutomaticSaveManifest source,
        AutomaticRemoteContextProbe marker,
        bool allowUnreadableMarkerAdoption,
        string beforeGameSnapshotPath = "",
        string beforeGameSnapshotSha256 = ""
    )
    {
        var destinationBaseline = direction == CloudOperationKind.Push
            ? remoteBaseline
            : localBaseline;
        var expectedDestination = ProjectTransferDestination(
            source,
            destinationBaseline
        );
        var pending = new AutomaticSyncPendingDocument
        {
            Phase = direction == CloudOperationKind.Push
                ? AutomaticSyncUploadingPhase
                : AutomaticSyncDownloadingPhase,
            ContextMarker = context.SerializeMarker(),
            LocalBaseline = AutomaticSaveManifest.Create(localBaseline.Entries),
            RemoteBaseline = AutomaticSaveManifest.Create(remoteBaseline.Entries),
            SourceManifest = AutomaticSaveManifest.Create(source.Entries),
            ExpectedDestinationManifest = expectedDestination,
            RemoteMarkerBaseline = marker.MarkerState,
            BeforeGameSnapshotPath = beforeGameSnapshotPath ?? "",
            BeforeGameSnapshotSha256 = beforeGameSnapshotSha256 ?? "",
            AllowUnreadableMarkerAdoption = allowUnreadableMarkerAdoption,
        };
        await WritePendingAutomaticSyncAsync(
            local,
            pending,
            sync.CancellationToken
        ).ConfigureAwait(false);
        return await ResumePendingTransferAsync(
            sync,
            local,
            context,
            pending
        ).ConfigureAwait(false);
    }

    private static async Task<AutomaticSyncResult> CompleteAutomaticSyncAsync(
        ISaveStore local,
        SaveContext context,
        AutomaticSaveManifest localManifest,
        AutomaticSaveManifest remoteManifest,
        ManualSyncContext sync
    )
    {
        await WriteAutomaticBaselineAsync(
            local,
            context,
            localManifest,
            remoteManifest,
            sync.CancellationToken
        ).ConfigureAwait(false);
        await DeletePendingAutomaticSyncAsync(
            local,
            sync.CancellationToken
        ).ConfigureAwait(false);
        return AutomaticResult(
            AutomaticSyncOutcome.Synchronized,
            "Local and Steam saves were synchronized and verified",
            remoteVerified: true
        );
    }
}
