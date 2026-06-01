using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct DepotAccess
    {
        internal DepotAccess(byte[] key, ulong manifestRequestCode)
        {
            Key = key;
            ManifestRequestCode = manifestRequestCode;
        }

        internal byte[] Key { get; }
        internal ulong ManifestRequestCode { get; }
    }

    private async Task DownloadDepotAsync(uint depotId, ulong manifestId, CancellationToken ct)
    {
        Log($"Processing depot {depotId}...");

        bool isUpdate = _stateStore.LoadManifestId(depotId) != manifestId;

        var access = await GetDepotAccessAsync(depotId, manifestId);

        var manifest = await DownloadManifestWithRetriesAsync(
            depotId,
            manifestId,
            access.ManifestRequestCode,
            access.Key
        );

        var oldManifest = _stateStore.LoadManifest(depotId);

        CleanupStaleDownloadTemps();

        var filePlan = BuildDepotFilePlan(oldManifest, manifest, isUpdate);

        DeleteObsoleteFiles(filePlan.Deletes);
        ResetDepotProgress(filePlan.Downloads);

        if (filePlan.Downloads.Count == 0)
        {
            Log($"Depot {depotId}: already up to date");
        }
        else
        {
            Log(
                $"Downloading {filePlan.Downloads.Count} files ({FormatBytes(_progress.TotalBytes)}) with {MaxConcurrentDownloads} threads..."
            );

            await DownloadDepotFilesAsync(filePlan.Downloads, depotId, access.Key, ct);
        }

        _stateStore.SaveManifest(depotId, manifest, manifestId);
        Log($"Depot {depotId} complete");
    }

    private async Task<DepotAccess> GetDepotAccessAsync(uint depotId, ulong manifestId)
        => new(
            await _connection.GetDepotDecryptionKeyAsync(depotId),
            await GetManifestRequestCodeAsync(depotId, manifestId)
        );
}
