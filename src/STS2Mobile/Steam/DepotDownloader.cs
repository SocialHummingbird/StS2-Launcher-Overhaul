using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2.CDN;

namespace STS2Mobile.Steam;

// Downloads game files from Steam CDN using SteamKit2. Supports delta updates
// by comparing manifests, concurrent chunk downloads, and server rotation with
// retry logic. Also patches the PCK to disable Android-incompatible plugin startup.
internal sealed partial class DepotDownloader : IDisposable
{
    private const int MaxRetries = 5;
    private const int DesktopMaxConcurrentDownloads = 8;
    private const int AndroidMaxConcurrentDownloads = 1;
    private const long AndroidMinimumFreeSpaceBytes = 256L * 1024L * 1024L;
    private const string ChunkAuthRetryOperation = "Chunk";
    private const string ChunkDownloadOperation = "Chunk download";
    private const string ManifestAuthRetryOperation = "Manifest";
    private const string ManifestDownloadOperation = "Manifest download";
    private const long MaxDepotChunkBytes = 64L * 1024L * 1024L;
    private const long MaxDepotFileBytes = 32L * 1024L * 1024L * 1024L;

    private readonly SteamConnection _connection;
    private readonly string _gameDir;
    private readonly DownloadStateStore _stateStore;
    private readonly Client _cdnClient;

    internal DepotDownloader(SteamConnection connection, string dataDir)
    {
        _connection = connection;
        _gameDir = Path.Combine(dataDir, "game");
        _stateStore = DownloadStateStore.Create(this, Path.Combine(dataDir, "download_state"));
        _cdnClient = connection.CreateCdnClient();
    }

    internal async Task DownloadAsync(CancellationToken ct = default)
        => await RunWithSuspendedIdleTimeoutAsync(() => DownloadCoreAsync(ct));

    private async Task DownloadCoreAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(_gameDir);
        _stateStore.Prepare();

        Log(
            $"Downloader mode: android={OperatingSystem.IsAndroid()}, "
                + $"maxConcurrency={MaxConcurrentDownloads}"
        );
        Log("Fetching app info...");

        var depots = await GetMainAppDepotsAsync();
        if (depots.Count == 0)
            throw new Exception("No downloadable depots found");

        _servers = await LoadCdnServersAsync(ct);

        foreach (var depot in depots)
        {
            ct.ThrowIfCancellationRequested();
            await depot.DownloadAsync(this, ct);
        }

        Log("All game files downloaded!");

        // Remove Sentry plugin references (no android.arm64 build exists).
        PatchGamePck(Path.Combine(_gameDir, "SlayTheSpire2.pck"));
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

