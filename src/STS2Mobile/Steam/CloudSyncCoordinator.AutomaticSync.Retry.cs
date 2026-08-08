#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static async Task<AutomaticSyncResult> ResumePendingTransferAsync(
        ManualSyncContext sync,
        ISaveStore local,
        SaveContext context,
        AutomaticSyncPendingDocument pending
    )
    {
        var direction = pending.Phase == AutomaticSyncUploadingPhase
            ? CloudOperationKind.Push
            : CloudOperationKind.Pull;
        var sourceTarget = pending.SourceManifest
            ?? throw new InvalidDataException("Automatic transfer has no source manifest");
        var destinationTarget = pending.ExpectedDestinationManifest
            ?? throw new InvalidDataException("Automatic transfer has no destination manifest");
        var marker = await ProbeRemoteContextAsync(sync, context)
            .ConfigureAwait(false);
        if (marker.Status == AutomaticRemoteContextStatus.Mismatch)
            return AutomaticConflict(marker.Problem);

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
        var source = direction == CloudOperationKind.Push
            ? localSnapshot.Manifest
            : remoteSnapshot.Manifest;
        var destination = direction == CloudOperationKind.Push
            ? remoteSnapshot.Manifest
            : localSnapshot.Manifest;
        var destinationBaseline = direction == CloudOperationKind.Push
            ? pending.RemoteBaseline
            : pending.LocalBaseline;

        if (!source.ContentEquals(sourceTarget))
        {
            return AutomaticConflict(
                "The automatic transfer source changed after its intent was persisted"
            );
        }
        if (!destination.IsPathwiseBlendOf(
                destinationBaseline,
                destinationTarget
            ))
        {
            return AutomaticConflict(
                "The automatic transfer destination contains an independent change"
            );
        }

        if (direction == CloudOperationKind.Push)
        {
            var markerProblem = ValidatePendingUploadMarker(
                marker,
                pending,
                destination,
                destinationBaseline,
                destinationTarget
            );
            if (!string.IsNullOrEmpty(markerProblem))
                return AutomaticConflict(markerProblem);
        }
        else if (marker.Status == AutomaticRemoteContextStatus.Missing
            && !pending.RemoteMarkerBaseline.Exists)
        {
            await RequireRemoteBaselineAsync(
                sync,
                context.MarkerPath,
                FileBaseline.Missing,
                "Steam save context"
            ).ConfigureAwait(false);
            await WriteAndVerifyRemoteAsync(
                sync,
                context.MarkerPath,
                context.SerializeMarker()
            ).ConfigureAwait(false);
            marker = await ProbeRemoteContextAsync(sync, context)
                .ConfigureAwait(false);
            if (marker.Status != AutomaticRemoteContextStatus.Exact)
                return AutomaticConflict(RemoteContextProblem(marker));
        }
        else if (marker.Status != AutomaticRemoteContextStatus.Exact)
        {
            return AutomaticConflict(RemoteContextProblem(marker));
        }

        var incompletePullExists = direction == CloudOperationKind.Pull
            && await sync.LocalFileExistsAsync(
                IncompletePullMarkerPath(context.Namespace)
            ).ConfigureAwait(false);
        var alreadyTransferred = !incompletePullExists
            && destination.ContentEquals(destinationTarget)
            && (direction != CloudOperationKind.Push
                || marker.Status == AutomaticRemoteContextStatus.Exact);
        if (!alreadyTransferred)
        {
            await RunTransferAsync(
                sync,
                direction,
                context.Namespace,
                context.RuntimeIdentity,
                context.ModSetFingerprint,
                expectedSourceManifest: sourceTarget,
                allowUnmarkedPush: direction == CloudOperationKind.Push
                    && (marker.Status == AutomaticRemoteContextStatus.Missing
                        || pending.AllowUnreadableMarkerAdoption),
                allowEmptySource: true
            ).ConfigureAwait(false);
        }

        var finalMarker = await ProbeRemoteContextAsync(sync, context)
            .ConfigureAwait(false);
        if (finalMarker.Status != AutomaticRemoteContextStatus.Exact)
            return AutomaticConflict(RemoteContextProblem(finalMarker));
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
        var finalDestination = direction == CloudOperationKind.Push
            ? finalRemote.Manifest
            : finalLocal.Manifest;
        var finalSource = direction == CloudOperationKind.Push
            ? finalLocal.Manifest
            : finalRemote.Manifest;
        if (!finalSource.ContentEquals(sourceTarget))
        {
            return AutomaticConflict(
                "Automatic transfer source changed before final verification"
            );
        }
        if (!finalDestination.ContentEquals(destinationTarget))
        {
            return AutomaticConflict(
                "Automatic transfer completed without producing its persisted destination manifest"
            );
        }

        return await CompleteAutomaticSyncAsync(
            local,
            context,
            finalLocal.Manifest,
            finalRemote.Manifest,
            sync
        ).ConfigureAwait(false);
    }

    private static string ValidatePendingUploadMarker(
        AutomaticRemoteContextProbe marker,
        AutomaticSyncPendingDocument pending,
        AutomaticSaveManifest destination,
        AutomaticSaveManifest destinationBaseline,
        AutomaticSaveManifest destinationTarget
    )
    {
        if (marker.Status == AutomaticRemoteContextStatus.Exact)
        {
            if (destination.ContentEquals(destinationBaseline)
                || destination.ContentEquals(destinationTarget))
            {
                return "";
            }

            return "Steam context marker was committed while its save files were only partially updated";
        }

        if (marker.Status == AutomaticRemoteContextStatus.Missing)
            return "";

        if (marker.Status == AutomaticRemoteContextStatus.Unreadable
            && pending.AllowUnreadableMarkerAdoption
            && marker.MarkerState.Equals(pending.RemoteMarkerBaseline)
            && destination.ContentEquals(destinationBaseline))
        {
            return "";
        }

        return RemoteContextProblem(marker);
    }

    private static async Task<AutomaticRemoteContextProbe>
        ProbeRemoteContextAsync(
            ManualSyncContext sync,
            SaveContext context
        )
    {
        sync.PrepareCloudMetadata();
        if (!await sync.RemoteFileExistsAsync(context.MarkerPath)
                .ConfigureAwait(false))
        {
            return new AutomaticRemoteContextProbe(
                AutomaticRemoteContextStatus.Missing,
                AutomaticFileState.Missing,
                "Steam Cloud saves have no launcher save-context marker"
            );
        }

        var content = await sync.ReadRemoteFileForVerificationAsync(
            context.MarkerPath
        ).ConfigureAwait(false);
        var state = new AutomaticFileState(
            true,
            AutomaticSyncHash.Compute(content)
        );
        SaveContext remote;
        try
        {
            remote = SaveContext.ParseMarker(content);
        }
        catch (InvalidDataException ex)
        {
            return new AutomaticRemoteContextProbe(
                AutomaticRemoteContextStatus.Unreadable,
                state,
                ex.Message
            );
        }

        try
        {
            remote.RequireExactMatch(context);
            return new AutomaticRemoteContextProbe(
                AutomaticRemoteContextStatus.Exact,
                state,
                ""
            );
        }
        catch (InvalidOperationException ex)
        {
            return new AutomaticRemoteContextProbe(
                AutomaticRemoteContextStatus.Mismatch,
                state,
                ex.Message
            );
        }
    }

    private static string RemoteContextProblem(
        AutomaticRemoteContextProbe marker
    )
        => marker.Status switch
        {
            AutomaticRemoteContextStatus.Missing
                => "Steam Cloud save context is missing",
            AutomaticRemoteContextStatus.Unreadable
                => "Steam Cloud save context is unreadable",
            AutomaticRemoteContextStatus.Mismatch
                => marker.Problem,
            _ => "Steam Cloud save context could not be verified",
        };
}
