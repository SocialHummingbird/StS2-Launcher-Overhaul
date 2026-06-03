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

        private Task DownloadFilesAsync(
            DepotDownloader owner,
            DepotFilePlan filePlan,
            uint depotId,
            CancellationToken ct
        )
            => filePlan.DownloadAsync(owner, depotId, Key, ct);

        internal async Task ApplyFilePlanAsync(
            DepotDownloader owner,
            DepotFilePlan filePlan,
            uint depotId,
            CancellationToken ct
        )
        {
            filePlan.ApplyDeletes(owner);
            filePlan.ResetProgress(owner);
            await DownloadFilesAsync(owner, filePlan, depotId, ct);
        }
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

        await access.ApplyFilePlanAsync(this, filePlan, depotId, ct);

        _stateStore.SaveManifest(depotId, manifest, manifestId);
        Log($"Depot {depotId} complete");
    }

    private async Task<DepotAccess> GetDepotAccessAsync(uint depotId, ulong manifestId)
        => DepotAccess.WithManifest(
            await _connection.GetDepotDecryptionKeyAsync(depotId),
            await GetManifestRequestCodeAsync(depotId, manifestId)
        );
}
