using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private async Task DownloadDepotAsync(DepotManifestReference depot, CancellationToken ct)
    {
        var depotId = depot.DepotId;
        var manifestId = depot.ManifestId;

        Log($"Processing depot {depotId}...");

        bool isUpdate = _stateStore.LoadManifestId(depotId) != manifestId;

        var depotKey = await _connection.GetDepotDecryptionKeyAsync(depotId);
        var manifestRequestCode = await GetManifestRequestCodeAsync(depotId, manifestId);
        var manifest = await DownloadManifestWithRetriesAsync(
            depotId,
            manifestId,
            manifestRequestCode,
            depotKey
        );

        var oldManifest = _stateStore.LoadManifest(depotId);

        CleanupStaleDownloadTemps();

        var downloads = BuildDepotDownloads(oldManifest, manifest, isUpdate);
        var deletes = GetFilesToDelete(oldManifest, manifest);

        await ExecuteDepotFileChangesAsync(downloads, deletes, depotId, depotKey, ct);

        _stateStore.SaveManifest(depotId, manifest, manifestId);
        Log($"Depot {depotId} complete");
    }

    private async Task ExecuteDepotFileChangesAsync(
        IReadOnlyList<DepotManifest.FileData> downloads,
        IReadOnlyList<string> deletes,
        uint depotId,
        byte[] depotKey,
        CancellationToken ct
    )
    {
        DeleteObsoleteFiles(deletes);
        ResetDepotProgress(downloads);
        await DownloadDepotFilesOrLogCurrentAsync(
            downloads,
            depotId,
            depotKey,
            ct
        );
    }

    private async Task DownloadDepotFilesOrLogCurrentAsync(
        IReadOnlyList<DepotManifest.FileData> downloads,
        uint depotId,
        byte[] depotKey,
        CancellationToken ct
    )
    {
        if (downloads.Count == 0)
        {
            Log($"Depot {depotId}: already up to date");
            return;
        }

        Log(
            $"Downloading {downloads.Count} files ({FormatBytes(_totalDownloadBytes)}) with {MaxConcurrentDownloads} threads..."
        );

        await DownloadDepotFilesAsync(downloads, depotId, depotKey, ct);
    }
}
