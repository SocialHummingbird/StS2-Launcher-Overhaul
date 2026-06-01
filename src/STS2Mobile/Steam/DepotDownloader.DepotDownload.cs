using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct DepotAccess
    {
        private DepotAccess(byte[] key, ulong manifestRequestCode)
        {
            Key = key;
            ManifestRequestCode = manifestRequestCode;
        }

        private byte[] Key { get; }
        private ulong ManifestRequestCode { get; }

        internal static DepotAccess WithManifest(byte[] key, ulong manifestRequestCode)
            => new(key, manifestRequestCode);

        internal Task<SteamKit2.DepotManifest> DownloadManifestAsync(
            DepotDownloader owner,
            uint depotId,
            ulong manifestId
        )
            => owner.DownloadManifestWithRetriesAsync(
                depotId,
                manifestId,
                ManifestRequestCode,
                Key
            );

        internal Task DownloadFilesAsync(
            DepotDownloader owner,
            DepotFilePlan filePlan,
            uint depotId,
            CancellationToken ct
        )
            => filePlan.DownloadAsync(owner, depotId, Key, ct);
    }

    private async Task DownloadDepotAsync(uint depotId, ulong manifestId, CancellationToken ct)
    {
        Log($"Processing depot {depotId}...");

        bool isUpdate = _stateStore.LoadManifestId(depotId) != manifestId;

        var access = await GetDepotAccessAsync(depotId, manifestId);

        var manifest = await access.DownloadManifestAsync(this, depotId, manifestId);

        var oldManifest = _stateStore.LoadManifest(depotId);

        CleanupStaleDownloadTemps();

        var filePlan = BuildDepotFileLists(oldManifest, manifest, isUpdate);

        filePlan.ApplyDeletes(this);
        filePlan.ResetProgress(this);
        await access.DownloadFilesAsync(this, filePlan, depotId, ct);

        _stateStore.SaveManifest(depotId, manifest, manifestId);
        Log($"Depot {depotId} complete");
    }

    private async Task<DepotAccess> GetDepotAccessAsync(uint depotId, ulong manifestId)
        => DepotAccess.WithManifest(
            await _connection.GetDepotDecryptionKeyAsync(depotId),
            await GetManifestRequestCodeAsync(depotId, manifestId)
        );
}
