using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Launcher;
using SteamKit2.CDN;

namespace STS2Mobile.Steam;

// Downloads game files from Steam CDN using SteamKit2. Supports delta updates
// by comparing manifests, concurrent chunk downloads, and server rotation with
// retry logic. Also patches the PCK to disable Android-incompatible plugin startup.
internal sealed partial class DepotDownloader : IDisposable
{
    private const int MaxRetries = 5;
    private const int DesktopMaxConcurrentDownloads = 8;
    private const int AndroidMaxConcurrentDownloads = 3;
    private const long AndroidMinimumFreeSpaceBytes = 256L * 1024L * 1024L;
    private const long MaxDepotChunkBytes = 64L * 1024L * 1024L;
    private const long MaxDepotFileBytes = 32L * 1024L * 1024L * 1024L;
    internal const string AndroidPckPreparationVersion = "android-pck-v2";

    private readonly SteamConnection _connection;
    private readonly string _dataDir;
    private readonly string _branch;
    private readonly string _gameDir;
    private readonly DownloadStateStore _stateStore;
    private readonly Client _cdnClient;
    private BranchInstallUpdate _installUpdate;

    internal DepotDownloader(SteamConnection connection, string dataDir, string branch = SteamGameBranch.Public)
    {
        _connection = connection;
        _dataDir = dataDir;
        _branch = SteamGameBranch.Normalize(branch);
        _gameDir = SteamGameInstallPaths.GameDirectory(dataDir, _branch);
        _stateStore = new DownloadStateStore(this, SteamGameInstallPaths.DownloadStateDirectoryPath(dataDir, _branch));
        _cdnClient = connection.CreateCdnClient();
    }

    internal Task<BranchInstallCompletion> DownloadAsync(CancellationToken ct = default)
        => RunWithSuspendedIdleTimeoutAsync(() => DownloadCoreAsync(ct));

    private async Task<BranchInstallCompletion> DownloadCoreAsync(CancellationToken ct)
    {
        Log(
            $"Downloader mode: android={OperatingSystem.IsAndroid()}, "
                + $"maxConcurrency={MaxConcurrentDownloads}, "
                + $"branch={_branch}"
        );
        Log("Fetching app info...");

        var depots = await PrepareAndGetMainAppDepotsAsync(requireAny: true);
        using var installUpdate = BranchInstallUpdate.StartOrResume(
            _dataDir,
            _branch,
            depots
                .Select(depot => new BranchInstallDepot(
                    depot.DepotId,
                    depot.ManifestId,
                    depot.ManifestSource
                ))
                .ToArray()
        );
        _installUpdate = installUpdate;

        try
        {
            installUpdate.RequireUpdatingBeforeInstalledMutation();
            Directory.CreateDirectory(_gameDir);
            installUpdate.UpdatePhase("loading-cdn-servers");
            _servers = await LoadCdnServersAsync(ct);

            installUpdate.UpdatePhase("downloading");
            foreach (var depot in depots)
            {
                ct.ThrowIfCancellationRequested();
                await DownloadDepotAsync(depot, ct);
            }

            Log("All game files downloaded!");

            installUpdate.UpdatePhase("preparing-android-pck");
            // Remove Android-incompatible startup references while keeping the FMOD extension registered for script types.
            PatchGamePck(Path.Combine(_gameDir, "SlayTheSpire2.pck"));

            installUpdate.UpdatePhase("publishing-completed-steam-generation");
            WriteBranchMarker(depots);

            var completion = installUpdate.CompleteInstalledFiles(
                AndroidPckPreparationVersion
            );
            Log(
                $"Published ready installation state for '{_branch}' identity={completion.GameIdentity.Id} transaction={completion.TransactionId:D}"
            );
            return completion;
        }
        catch (Exception ex)
        {
            installUpdate.RecordFailure(
                ex is OperationCanceledException ? "cancelled" : "failed",
                ex
            );
            throw;
        }
        finally
        {
            _installUpdate = null;
        }
    }

    private void WriteBranchMarker(IReadOnlyList<DepotManifestReference> depots)
    {
        var markerPath = SteamGameInstallPaths.BranchMarkerPath(_dataDir, _branch);
        var publicMatchCount = 0;
        var publicDifferentCount = 0;
        var publicMissingCount = 0;
        var publicInheritedCount = 0;
        var selectedMissingCount = 0;
        foreach (var depot in depots)
        {
            if (depot.InheritedFromPublic)
                publicInheritedCount++;

            if (!depot.HasSelectedBranchManifest)
                selectedMissingCount++;

            if (!depot.HasPublicManifest)
            {
                publicMissingCount++;
                continue;
            }

            if (depot.EffectiveMatchesPublicManifest)
                publicMatchCount++;
            else
                publicDifferentCount++;
        }

        var text =
            $"Branch: {_branch}\n"
                + $"Display name: {SteamGameBranch.DisplayName(_branch)}\n"
                + $"Directory name: {SteamGameBranch.StateDirectoryName(_branch)}\n"
                + $"Install slot kind: {SteamGameInstallPaths.VersionSlotKind(_branch)}\n"
                + $"Install slot directory: {SteamGameInstallPaths.VersionSlotDirectory(_dataDir, _branch)}\n"
                + $"Updated UTC: {DateTime.UtcNow:O}\n"
                + $"Depot manifest count: {depots.Count}\n";
        if (!IsPublicBranch)
        {
            text += $"Depot manifests matching public count: {publicMatchCount}\n"
                + $"Depot manifests differing from public count: {publicDifferentCount}\n"
                + $"Depot manifests without public comparison count: {publicMissingCount}\n"
                + $"Depot manifests inherited from public count: {publicInheritedCount}\n"
                + $"Depot manifests missing selected branch manifest count: {selectedMissingCount}\n";
        }

        foreach (var depot in depots)
        {
            var publicManifest = depot.PublicManifestId.HasValue
                ? depot.PublicManifestId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : "missing";
            var selectedBranchManifest = depot.SelectedBranchManifestId.HasValue
                ? depot.SelectedBranchManifestId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : "missing";
            var selectedMatchesPublic = depot.SelectedBranchManifestId.HasValue && depot.PublicManifestId.HasValue
                ? depot.SelectedBranchManifestMatchesPublic.ToString().ToLowerInvariant()
                : "unknown";
            var effectiveMatchesPublic = depot.PublicManifestId.HasValue
                ? depot.EffectiveMatchesPublicManifest.ToString().ToLowerInvariant()
                : "unknown";
            text += $"Depot manifest: depot={depot.DepotId} manifest={depot.ManifestId} branch={depot.Branch} selectedBranchManifest={selectedBranchManifest} publicManifest={publicManifest} manifestSource={depot.ManifestSource} manifestRequestBranch={depot.ManifestRequestBranch} selectedMatchesPublic={selectedMatchesPublic} effectiveMatchesPublic={effectiveMatchesPublic}\n";
        }

        RequireUpdatingBeforeInstalledMutation();
        new AtomicFileWriter().WriteAllBytes(
            markerPath,
            Encoding.UTF8.GetBytes(text)
        );
        Log($"Wrote Steam branch marker: {markerPath}");
    }

    private async Task RunWithSuspendedIdleTimeoutAsync(Func<Task> action)
    {
        _connection.SuspendIdleTimeout();
        try
        {
            await action();
        }
        finally
        {
            _connection.ResumeIdleTimeout();
        }
    }

    private async Task<T> RunWithSuspendedIdleTimeoutAsync<T>(Func<Task<T>> action)
    {
        _connection.SuspendIdleTimeout();
        try
        {
            return await action();
        }
        finally
        {
            _connection.ResumeIdleTimeout();
        }
    }

    private static int MaxConcurrentDownloads =>
        OperatingSystem.IsAndroid() ? AndroidMaxConcurrentDownloads : DesktopMaxConcurrentDownloads;


    void IDisposable.Dispose()
        => Dispose();

    internal void Dispose()
    {
        _cdnClient?.Dispose();
    }
}

