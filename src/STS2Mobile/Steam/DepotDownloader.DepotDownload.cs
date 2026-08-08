using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct DepotDownloadPlan
    {
        private DepotDownloadPlan(
            DepotDownloader owner,
            DepotManifestReference depot,
            DepotManifest manifest,
            DepotFileChanges changes
        )
        {
            Owner = owner;
            Depot = depot;
            Manifest = manifest;
            Changes = changes;
        }

        private DepotDownloader Owner { get; }
        private DepotManifestReference Depot { get; }
        private DepotManifest Manifest { get; }
        private DepotFileChanges Changes { get; }

        internal static DepotDownloadPlan Create(
            DepotDownloader owner,
            DepotManifestReference depot,
            DepotManifest manifest,
            DepotFileChanges changes
        )
            => new(owner, depot, manifest, changes);

        internal async Task ExecuteAsync()
        {
            await Changes.ExecuteAsync();
            Owner._stateStore.SaveManifest(Depot, Manifest);
            Owner.Log($"Depot {Depot.DepotId} complete");
        }
    }

    private readonly struct DepotFileChanges
    {
        private DepotFileChanges(
            DepotDownloader owner,
            IReadOnlyList<DepotManifest.FileData> downloads,
            IReadOnlyList<string> deletes,
            uint depotId,
            byte[] depotKey,
            CancellationToken ct
        )
        {
            Owner = owner;
            Downloads = downloads;
            Deletes = deletes;
            DepotId = depotId;
            DepotKey = depotKey;
            Cancellation = ct;
        }

        private DepotDownloader Owner { get; }
        private IReadOnlyList<DepotManifest.FileData> Downloads { get; }
        private IReadOnlyList<string> Deletes { get; }
        private uint DepotId { get; }
        private byte[] DepotKey { get; }
        private CancellationToken Cancellation { get; }

        internal static DepotFileChanges Create(
            DepotDownloader owner,
            IReadOnlyList<DepotManifest.FileData> downloads,
            IReadOnlyList<string> deletes,
            uint depotId,
            byte[] depotKey,
            CancellationToken ct
        )
            => new(owner, downloads, deletes, depotId, depotKey, ct);

        internal async Task ExecuteAsync()
        {
            Owner.DeleteObsoleteFiles(Deletes);
            Owner.ResetDepotProgress(Downloads);
            await DownloadOrLogCurrentAsync();
        }

        private async Task DownloadOrLogCurrentAsync()
        {
            if (Downloads.Count == 0)
            {
                Owner.Log($"Depot {DepotId}: already up to date");
                return;
            }

            var totalBytes = DepotDownloader.FormatBytes(Owner._totalDownloadBytes);
            var threadCount = DepotDownloader.MaxConcurrentDownloads;
            Owner.Log(
                $"Downloading {Downloads.Count} files ({totalBytes}) with {threadCount} threads..."
            );

            await Owner.DownloadDepotFilesAsync(
                Downloads,
                DepotId,
                DepotKey,
                Cancellation
            );
        }
    }

    private readonly struct LoadedDepotManifest
    {
        private LoadedDepotManifest(
            DepotManifest manifest,
            byte[] depotKey
        )
        {
            Manifest = manifest;
            DepotKey = depotKey;
        }

        internal DepotManifest Manifest { get; }
        internal byte[] DepotKey { get; }

        internal static LoadedDepotManifest Create(
            DepotManifest manifest,
            byte[] depotKey
        )
            => new(manifest, depotKey);
    }

    private async Task DownloadDepotAsync(DepotManifestReference depot, CancellationToken ct)
    {
        var plan = await CreateDepotDownloadPlanAsync(depot, ct);
        await plan.ExecuteAsync();
    }

    private async Task<DepotDownloadPlan> CreateDepotDownloadPlanAsync(
        DepotManifestReference depot,
        CancellationToken ct
    )
    {
        var depotId = depot.DepotId;

        Log($"Processing depot {depotId} for branch '{depot.Branch}' source={depot.ManifestSource} requestBranch='{depot.ManifestRequestBranch}'...");

        bool isUpdate = _stateStore.ManifestChanged(depot);

        var loaded = await LoadDepotManifestAsync(depot);

        var oldManifest = _stateStore.LoadManifest(depotId);

        CleanupStaleDownloadTemps();

        var downloads = BuildDepotDownloads(oldManifest, loaded.Manifest, isUpdate);
        var deletes = GetFilesToDelete(oldManifest, loaded.Manifest);

        return DepotDownloadPlan.Create(
            this,
            depot,
            loaded.Manifest,
            DepotFileChanges.Create(
                this,
                downloads,
                deletes,
                depotId,
                loaded.DepotKey,
                ct
            )
        );
    }

    private async Task<LoadedDepotManifest> LoadDepotManifestAsync(
        DepotManifestReference depot
    )
    {
        var depotKey = await _connection.GetDepotDecryptionKeyAsync(depot.DepotId);
        var manifestRequestCode = await GetManifestRequestCodeAsync(
            depot.DepotId,
            depot.ManifestId,
            depot.ManifestRequestBranch
        );
        var manifest = await DownloadManifestWithRetriesAsync(
            new ManifestDownloadRequest(
                depot.DepotId,
                depot.ManifestId,
                manifestRequestCode,
                depotKey
            )
        );

        return LoadedDepotManifest.Create(manifest, depotKey);
    }
}
