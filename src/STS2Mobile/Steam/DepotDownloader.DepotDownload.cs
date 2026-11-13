using System;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Launcher;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private async Task DownloadDepotAsync(DepotManifestReference depot, CancellationToken ct)
    {
        Log($"Processing depot {depot.DepotId} for '{depot.Branch}' manifest={depot.ManifestId}");
        var loaded = await LoadDepotManifestAsync(depot, ct).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
        var old = _stateStore.LoadManifest(depot.DepotId);
        ValidateDownloadFileSizes(loaded.Manifest.Files);
        RequireUpdatingBeforeInstalledMutation();
        var commit = CreateDepotCommit(RequireUpdatingBeforeInstalledMutation,
            () => DeleteObsoleteFiles(GetFilesToDelete(old, loaded.Manifest)));
        DepotChunkTransferProgress last = default;
        var transfer = new DepotChunkTransfer(
            _gameDir, SteamGameInstallPaths.DownloadStateDirectoryPath(_dataDir, _branch),
            depot.DepotId, depot.ManifestId, loaded.Manifest,
            ReadInstalledDonors(depot.DepotId, old), MaxConcurrentDownloads,
            (chunk, buffer, token) => DownloadChunkWithRetriesAsync(depot.DepotId, chunk, buffer, loaded.DepotKey, "game content", token),
            progress => { last = progress; ReportChunkProgress(progress); },
            file =>
            {
                var keep = CanKeepPreparedPck(_previousReady, old, file, _previousIdentity);
                if (string.Equals(NormalizeManifestPath(file.FileName), "SlayTheSpire2.pck", StringComparison.Ordinal))
                    _retainedPreparedPck = keep;
                return keep;
            },
            needed => EnsureEnoughFreeSpaceForFile(_gameDir, needed, "staged game files"),
            beforePromote: commit);
        await transfer.RunAsync(ct).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
        commit();
        _stateStore.SaveManifest(depot, loaded.Manifest);
        Log($"Depot {depot.DepotId} complete: reused={last.ReusedBytes} resumed={last.ResumedBytes} downloadedPayload={last.DownloadedBytes} plannedPayload={last.PlannedDownloadBytes}");
    }

    // The engine invokes this only after the whole staged batch verifies. The final
    // invocation also handles deletion-only updates with no files to promote.
    internal static Action CreateDepotCommit(Action requireUpdating, Action deleteObsolete)
    {
        var cleaned = false;
        return () =>
        {
            requireUpdating();
            if (cleaned) return;
            deleteObsolete();
            cleaned = true;
        };
    }

    private readonly record struct LoadedDepotManifest(DepotManifest Manifest, byte[] DepotKey);

    private async Task<LoadedDepotManifest> LoadDepotManifestAsync(DepotManifestReference depot, CancellationToken ct)
    {
        var key = await _connection.GetDepotDecryptionKeyAsync(depot.DepotId).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
        var request = await GetManifestRequestCodeAsync(depot.DepotId, depot.ManifestId, depot.ManifestRequestBranch).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
        var manifest = await DownloadManifestWithRetriesAsync(new ManifestDownloadRequest(depot.DepotId, depot.ManifestId, request, key), ct).ConfigureAwait(false);
        return new LoadedDepotManifest(manifest, key);
    }
}
