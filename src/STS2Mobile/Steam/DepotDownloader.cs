using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.CDN;

namespace STS2Mobile.Steam;

// Downloads game files from Steam CDN using SteamKit2. Supports delta updates
// by comparing manifests, concurrent chunk downloads, and server rotation with
// retry logic. Also patches the PCK to disable Android-incompatible plugin startup.
internal sealed class DepotDownloader : IDisposable
{
    internal sealed class DownloadProgress
    {
        internal long TotalBytes;
        internal long DownloadedBytes;
        internal int TotalFiles;
        internal int CompletedFiles;
        internal string CurrentFile;

        internal double Percentage => TotalBytes > 0 ? (double)DownloadedBytes / TotalBytes * 100.0 : 0;
    }

    private const int MaxRetries = 5;
    private const int DesktopMaxConcurrentDownloads = 8;
    private const int AndroidMaxConcurrentDownloads = 1;
    private const long AndroidMinimumFreeSpaceBytes = 256L * 1024L * 1024L;
    private const long MaxDepotChunkBytes = 64L * 1024L * 1024L;
    private const long MaxDepotFileBytes = 32L * 1024L * 1024L * 1024L;
    private static readonly TimeSpan CdnAuthTokenTtl = TimeSpan.FromMinutes(20);

    private readonly SteamConnection _connection;
    private readonly string _gameDir;
    private readonly string _stateDir;
    private readonly Client _cdnClient;
    private readonly DownloadProgress _progress = new();
    private long _lastProgressReportTicks;
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileWriteLocks =
        new();

    private IReadOnlyList<Server> _servers;
    private int _serverIndex;
    private readonly ConcurrentDictionary<(uint, string), (string Token, DateTime Expiry)> _cdnAuthTokens = new();
    private readonly ConcurrentDictionary<
        (uint DepotId, ulong ManifestId, string Branch),
        (ulong Code, DateTime Expiry)
    > _manifestRequestCodes = new();
    private readonly ConcurrentDictionary<
        uint,
        SteamApps.PICSProductInfoCallback.PICSProductInfo
    > _appInfoCache = new();

    internal event Action<DownloadProgress> ProgressChanged;
    internal event Action<string> LogMessage;

    internal DepotDownloader(SteamConnection connection, string dataDir)
    {
        _connection = connection;
        _gameDir = Path.Combine(dataDir, "game");
        _stateDir = Path.Combine(dataDir, "download_state");
        _cdnClient = new Client(connection.Client);
    }

    // Returns true if any depot has a newer manifest than what's cached locally.
    internal async Task<bool> CheckForUpdatesAsync(CancellationToken ct = default)
    {
        _connection.SuspendIdleTimeout();
        try
        {
            Directory.CreateDirectory(_stateDir);
            CleanupStateTempFiles();

            ulong accessToken = await GetAppAccessTokenAsync();
            var infoResult = await _connection.Apps.PICSGetProductInfo(
                new[] { new SteamApps.PICSRequest(SteamCloudApp.AppId, accessToken) },
                Enumerable.Empty<SteamApps.PICSRequest>()
            );

            SteamApps.PICSProductInfoCallback.PICSProductInfo appInfo = null;
            foreach (var cb in infoResult.Results)
            {
                if (cb.Apps.TryGetValue(SteamCloudApp.AppId, out var info))
                {
                    appInfo = info;
                    break;
                }
            }

            if (appInfo == null)
                throw new Exception("Failed to get app info from Steam");

            _appInfoCache[SteamCloudApp.AppId] = appInfo;
            var depots = await ParseDepotsAsync(GetDepotsSection(appInfo, SteamCloudApp.AppId));

            foreach (var (depotId, manifestId) in depots)
            {
                ct.ThrowIfCancellationRequested();
                if (LoadCachedManifestId(depotId) != manifestId)
                {
                    Log($"Update available: depot {depotId} manifest changed");
                    return true;
                }
            }

            Log("Game is up to date");
            return false;
        }
        finally
        {
            _connection.ResumeIdleTimeout();
        }
    }

    internal async Task DownloadAsync(CancellationToken ct = default)
    {
        _connection.SuspendIdleTimeout();
        try
        {
            Directory.CreateDirectory(_gameDir);
            Directory.CreateDirectory(_stateDir);
            CleanupStateTempFiles();

            Log(
                $"Downloader mode: android={OperatingSystem.IsAndroid()}, "
                    + $"maxConcurrency={MaxConcurrentDownloads}"
            );
            Log("Fetching app info...");

            ulong accessToken = await GetAppAccessTokenAsync();
            var infoResult = await _connection.Apps.PICSGetProductInfo(
                new[] { new SteamApps.PICSRequest(SteamCloudApp.AppId, accessToken) },
                Enumerable.Empty<SteamApps.PICSRequest>()
            );

            SteamApps.PICSProductInfoCallback.PICSProductInfo appInfo = null;
            foreach (var cb in infoResult.Results)
            {
                if (cb.Apps.TryGetValue(SteamCloudApp.AppId, out var info))
                {
                    appInfo = info;
                    break;
                }
            }

            if (appInfo == null)
                throw new Exception("Failed to get app info from Steam");

            _appInfoCache[SteamCloudApp.AppId] = appInfo;
            var depotSection = GetDepotsSection(appInfo, SteamCloudApp.AppId);
            var depots = await ParseDepotsAsync(depotSection);
            if (depots.Count == 0)
                throw new Exception("No downloadable depots found");

            Log("Getting CDN servers...");
            var allServers = await ContentServerDirectoryService.LoadAsync(
                _connection.Configuration,
                ct
            );
            if (allServers == null || allServers.Count == 0)
                throw new Exception("No CDN servers available");

            _servers = allServers
                .Where(s => s.Type == "SteamCache" || s.Type == "CDN")
                .OrderBy(s => s.WeightedLoad)
                .ToList();

            if (_servers.Count == 0)
                _servers = allServers.ToList();

            Log($"Using {_servers.Count} CDN servers");

            foreach (var (depotId, manifestId) in depots)
            {
                ct.ThrowIfCancellationRequested();
                await DownloadDepotAsync(depotId, manifestId, ct);
            }

            Log("All game files downloaded!");

            // Remove Sentry plugin references (no android.arm64 build exists).
            PatchGamePck(Path.Combine(_gameDir, "SlayTheSpire2.pck"));
        }
        finally
        {
            _connection.ResumeIdleTimeout();
        }
    }

    private async Task<List<(uint DepotId, ulong ManifestId)>> ParseDepotsAsync(
        KeyValue depotSection
    )
    {
        var result = new List<(uint, ulong)>();

        foreach (var depot in depotSection.Children)
        {
            if (!uint.TryParse(depot.Name, out var depotId))
                continue;

            // Skip non-Windows depots.
            var config = depot["config"];
            if (config != KeyValue.Invalid)
            {
                var oslist = config["oslist"]?.Value;
                if (oslist != null && oslist.Length > 0 && !oslist.Contains("windows"))
                {
                    Log($"Skipping depot {depotId} (OS: {oslist})");
                    continue;
                }
            }

            var manifests = depot["manifests"];

            // Manifest may be defined under a different app via depotfromapp.
            if (manifests == KeyValue.Invalid)
            {
                var depotFromApp = depot["depotfromapp"];
                if (
                    depotFromApp != KeyValue.Invalid
                    && depotFromApp.Value != null
                    && uint.TryParse(depotFromApp.Value, out var otherAppId)
                )
                {
                    Log($"Depot {depotId} references app {otherAppId}, fetching...");
                    var otherAppInfo = await GetAppInfoAsync(otherAppId);
                    if (otherAppInfo != null)
                    {
                        var otherDepots = GetDepotsSection(otherAppInfo, otherAppId);
                        var otherDepot = otherDepots[depotId.ToString()];
                        if (otherDepot != KeyValue.Invalid)
                            manifests = otherDepot["manifests"];
                    }
                }

                if (manifests == KeyValue.Invalid)
                    continue;
            }

            var gidNode = manifests["public"]["gid"];
            if (gidNode == KeyValue.Invalid || gidNode.Value == null)
                continue;

            if (!ulong.TryParse(gidNode.Value, out var manifestId))
                continue;

            Log($"Found depot {depotId} manifest {manifestId}");
            result.Add((depotId, manifestId));
        }

        return result;
    }

    private async Task<SteamApps.PICSProductInfoCallback.PICSProductInfo> GetAppInfoAsync(
        uint appId
    )
    {
        if (_appInfoCache.TryGetValue(appId, out var cached))
            return cached;

        var tokenResult = await _connection.Apps.PICSGetAccessTokens(
            new[] { appId },
            Enumerable.Empty<uint>()
        );
        ulong token = 0;
        if (tokenResult.AppTokens != null && tokenResult.AppTokens.TryGetValue(appId, out var appToken))
        {
            token = appToken;
        }
        else if (tokenResult.AppTokensDenied != null && tokenResult.AppTokensDenied.Contains(appId))
        {
            throw new InvalidOperationException(
                $"Steam denied app access token for referenced app {appId}"
            );
        }
        else
        {
            Log($"Steam returned no app access token for referenced app {appId}; continuing with public token 0");
        }

        var infoResult = await _connection.Apps.PICSGetProductInfo(
            new[] { new SteamApps.PICSRequest(appId, token) },
            Enumerable.Empty<SteamApps.PICSRequest>()
        );

        foreach (var cb in infoResult.Results)
        {
            if (cb.Apps.TryGetValue(appId, out var info))
            {
                _appInfoCache[appId] = info;
                return info;
            }
        }

        return null;
    }

    private static KeyValue GetDepotsSection(
        SteamApps.PICSProductInfoCallback.PICSProductInfo appInfo,
        uint appId
    )
    {
        var depots = appInfo?.KeyValues?["depots"];
        if (depots == null || depots == KeyValue.Invalid)
            throw new InvalidOperationException($"Steam app info for {appId} has no depots section");

        return depots;
    }

    private async Task<ulong> GetAppAccessTokenAsync()
    {
        if (_connection.AppAccessToken != 0)
            return _connection.AppAccessToken;

        var tokenResult = await _connection.Apps.PICSGetAccessTokens(
            new[] { SteamCloudApp.AppId },
            Enumerable.Empty<uint>()
        );
        if (tokenResult.AppTokens != null && tokenResult.AppTokens.TryGetValue(SteamCloudApp.AppId, out var token))
        {
            _connection.AppAccessToken = token;
            return token;
        }

        if (tokenResult.AppTokensDenied != null && tokenResult.AppTokensDenied.Contains(SteamCloudApp.AppId))
            throw new InvalidOperationException(
                $"Steam denied app access token for {SteamCloudApp.Name} ({SteamCloudApp.AppId}); ownership/session may be invalid"
            );

        Log($"Steam returned no app access token for {SteamCloudApp.Name} ({SteamCloudApp.AppId}); continuing with public token 0");
        return _connection.AppAccessToken;
    }

    private static int MaxConcurrentDownloads =>
        OperatingSystem.IsAndroid() ? AndroidMaxConcurrentDownloads : DesktopMaxConcurrentDownloads;

    private Server GetCurrentServer()
    {
        var idx = Volatile.Read(ref _serverIndex);
        return _servers[((idx % _servers.Count) + _servers.Count) % _servers.Count];
    }

    private void MarkServerFailed(Server server)
    {
        if (_servers == null || _servers.Count <= 1 || server == null)
            return;

        var current = GetCurrentServer();
        if (string.Equals(current.Host, server.Host, StringComparison.OrdinalIgnoreCase))
            Interlocked.Increment(ref _serverIndex);
    }

    private async Task<string> GetCdnAuthToken(uint depotId, Server server)
    {
        var key = (depotId, server.Host);
        if (_cdnAuthTokens.TryGetValue(key, out var cached))
        {
            if (DateTime.UtcNow < cached.Expiry)
                return cached.Token;

            _cdnAuthTokens.TryRemove(key, out _);
        }

        var result = await _connection.Content.GetCDNAuthToken(SteamCloudApp.AppId, depotId, server.Host);
        if (result.Result == EResult.OK)
        {
            _cdnAuthTokens[key] = (result.Token, DateTime.UtcNow.Add(CdnAuthTokenTtl));
            return result.Token;
        }

        return null;
    }

    private void InvalidateCdnAuthToken(uint depotId, Server server)
    {
        if (server != null)
            _cdnAuthTokens.TryRemove((depotId, server.Host), out _);
    }

    private async Task<ulong> GetManifestRequestCodeAsync(uint depotId, ulong manifestId)
    {
        var key = (depotId, manifestId, "public");
        if (
            _manifestRequestCodes.TryGetValue(key, out var cached)
            && DateTime.UtcNow < cached.Expiry
        )
        {
            return cached.Code;
        }

        var code = await _connection.Content.GetManifestRequestCode(
            depotId,
            SteamCloudApp.AppId,
            manifestId,
            "public"
        );
        if (code == 0)
            throw new Exception(
                $"Failed to get manifest request code for depot {depotId}. "
                    + "Ensure the account owns this app."
            );

        _manifestRequestCodes[key] = (code, DateTime.UtcNow.AddMinutes(5));
        return code;
    }

    private async Task DownloadDepotAsync(uint depotId, ulong manifestId, CancellationToken ct)
    {
        Log($"Processing depot {depotId}...");

        bool isUpdate = LoadCachedManifestId(depotId) != manifestId;

        var keyResult = await _connection.Apps.GetDepotDecryptionKey(depotId, SteamCloudApp.AppId);
        if (keyResult.Result != EResult.OK)
            throw new Exception($"Failed to get depot key for {depotId}: {keyResult.Result}");
        var depotKey = keyResult.DepotKey;

        var manifestRequestCode = await GetManifestRequestCodeAsync(depotId, manifestId);

        Log($"Downloading manifest for depot {depotId}...");
        DepotManifest? manifest = null;
        for (int attempt = 0; attempt < MaxRetries && manifest == null; attempt++)
        {
            var server = GetCurrentServer();
            try
            {
                manifest = await _cdnClient.DownloadManifestAsync(
                    depotId,
                    manifestId,
                    manifestRequestCode,
                    server,
                    depotKey
                );
            }
            catch (SteamKitWebRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                var token = await GetCdnAuthToken(depotId, server);
                if (token != null)
                {
                    try
                    {
                        manifest = await _cdnClient.DownloadManifestAsync(
                            depotId,
                            manifestId,
                            manifestRequestCode,
                            server,
                            depotKey,
                            cdnAuthToken: token
                        );
                    }
                    catch (Exception tokenEx) when (attempt < MaxRetries - 1)
                    {
                        Log(
                            $"Manifest CDN auth retry failed (attempt {attempt + 1}): "
                                + tokenEx.Message
                        );
                        InvalidateCdnAuthToken(depotId, server);
                        MarkServerFailed(server);
                    }
                }
                else
                {
                    MarkServerFailed(server);
                }
            }
            catch (Exception ex) when (attempt < MaxRetries - 1)
            {
                Log($"Manifest download failed (attempt {attempt + 1}): {ex.Message}");
                MarkServerFailed(server);
            }
        }

        if (manifest == null)
            throw new Exception(
                $"Failed to download manifest for depot {depotId} after {MaxRetries} attempts"
            );

        var oldManifest = LoadCachedManifest(depotId);

        // Clean up temp files from interrupted previous downloads.
        foreach (var temp in EnumerateDownloadingTempFiles())
        {
            try
            {
                File.Delete(temp);
            }
            catch (Exception ex)
            {
                Log($"Could not delete stale temp file {temp}: {ex.Message}");
            }
        }

        // Determine which files need downloading: new/changed files from the
        // manifest diff, plus any existing files that fail on-disk SHA-1 verification.
        var filesToDownload = GetFilesNeedingDownload(oldManifest, manifest, isUpdate);
        filesToDownload = DeduplicateDownloads(filesToDownload);
        ValidateDownloadFileSizes(filesToDownload);
        var filesToDelete = GetFilesToDelete(oldManifest, manifest);

        foreach (var fileName in filesToDelete)
        {
            string path;
            try
            {
                path = ResolveGamePath(fileName);
            }
            catch (Exception ex)
            {
                Log($"Skipping obsolete file with invalid cached path {fileName}: {ex.Message}");
                continue;
            }

            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    Log($"Deleted: {fileName}");
                }
                catch (Exception ex)
                {
                    Log($"Could not delete obsolete file {fileName}: {ex.Message}");
                }
            }
        }

        _progress.TotalFiles = filesToDownload.Count;
        _progress.CompletedFiles = 0;
        _progress.TotalBytes = ComputeTotalDownloadBytes(filesToDownload);
            _progress.DownloadedBytes = 0;
            ForceReportProgress();

        if (filesToDownload.Count == 0)
        {
            Log($"Depot {depotId}: already up to date");
        }
        else
        {
            Log(
                $"Downloading {filesToDownload.Count} files ({FormatBytes(_progress.TotalBytes)}) with {MaxConcurrentDownloads} threads..."
            );

            var workerCount = Math.Min(MaxConcurrentDownloads, filesToDownload.Count);
            var nextFileIndex = -1;
            var workers = Enumerable
                .Range(0, workerCount)
                .Select(
                    _ =>
                        Task.Run(
                            async () =>
                            {
                                while (true)
                                {
                                    ct.ThrowIfCancellationRequested();
                                    var index = Interlocked.Increment(ref nextFileIndex);
                                    if (index >= filesToDownload.Count)
                                        break;

                                    await DownloadFileAsync(
                                        filesToDownload[index],
                                        depotId,
                                        depotKey,
                                        ct
                                    );
                                    Interlocked.Increment(ref _progress.CompletedFiles);
                                    ForceReportProgress();
                                }
                            },
                            ct
                        )
                )
                .ToArray();

            await Task.WhenAll(workers);
        }

        SaveManifest(depotId, manifest, manifestId);
        Log($"Depot {depotId} complete");
    }

    private async Task DownloadFileAsync(
        DepotManifest.FileData file,
        uint depotId,
        byte[] depotKey,
        CancellationToken ct
    )
    {
        if (string.IsNullOrWhiteSpace(file.FileName))
        {
            Log("Skipping depot file with an empty manifest path");
            return;
        }

        var fileName = NormalizeManifestPath(file.FileName);
        var filePath = ResolveGamePath(fileName);
        var fileDir = Path.GetDirectoryName(filePath);
        if (fileDir != null)
            Directory.CreateDirectory(fileDir);

        var tempPath = filePath + ".downloading";
        var lockKey = Path.GetFullPath(filePath);
        var writeLock = _fileWriteLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

        await writeLock.WaitAsync(ct);
        try
        {
            _progress.CurrentFile = fileName;
            ForceReportProgress();

            if (file.Flags.HasFlag(EDepotFileFlag.Directory))
            {
                Directory.CreateDirectory(filePath);
                return;
            }

            // Validate existing file against manifest SHA-1 hash. A size-only check
            // would miss corruption from interrupted writes (SetLength pre-allocates).
            if (File.Exists(filePath) && VerifyFileHash(filePath, file))
            {
                Interlocked.Add(ref _progress.DownloadedBytes, (long)file.TotalSize);
                ForceReportProgress();
                return;
            }

            var fileSize = checked((long)file.TotalSize);
            EnsureEnoughFreeSpaceForFile(fileDir ?? _gameDir, fileSize, fileName);
            ValidateFileChunks(fileName, file);

            // Write to a temp file, verify hash, then move into place. This prevents
            // a partially-written file from being mistaken as complete on retry.
            DeleteQuietly(tempPath);

            using (var fs = File.Create(tempPath))
            {
                foreach (var chunk in file.Chunks.OrderBy(c => c.Offset))
                {
                    ct.ThrowIfCancellationRequested();
                    if (file.TotalSize == 0 && chunk.Offset == 0 && chunk.UncompressedLength == 0)
                    {
                        continue;
                    }

                    ValidateChunkBounds(fileName, file.TotalSize, chunk);

                    if (chunk.UncompressedLength > (ulong)MaxDepotChunkBytes)
                    {
                        throw new IOException(
                            $"Depot chunk is unexpectedly large for {fileName}: "
                                + $"{chunk.UncompressedLength} bytes"
                        );
                    }

                    var chunkLength = checked((int)chunk.UncompressedLength);
                    var buffer = ArrayPool<byte>.Shared.Rent(chunkLength);
                    try
                    {
                        int written = 0;

                        for (int attempt = 0; attempt < MaxRetries; attempt++)
                        {
                            var server = GetCurrentServer();
                            try
                            {
                                written = await _cdnClient.DownloadDepotChunkAsync(
                                    depotId,
                                    chunk,
                                    server,
                                    buffer,
                                    depotKey
                                );

                                if (!VerifyChunkHash(buffer, written, chunk))
                                {
                                    if (attempt < MaxRetries - 1)
                                    {
                                        Log($"Chunk SHA-1 mismatch at offset {chunk.Offset}, retrying...");
                                        written = 0;
                                        continue;
                                    }
                                    throw new Exception(
                                        $"Chunk SHA-1 verification failed for {fileName} "
                                            + $"at offset {chunk.Offset} after {MaxRetries} attempts"
                                    );
                                }

                                break;
                            }
                            catch (SteamKitWebRequestException ex)
                                when (ex.StatusCode == HttpStatusCode.Forbidden)
                            {
                                var token = await GetCdnAuthToken(depotId, server);
                                if (token != null)
                                {
                                    try
                                    {
                                        written = await _cdnClient.DownloadDepotChunkAsync(
                                            depotId,
                                            chunk,
                                            server,
                                            buffer,
                                            depotKey,
                                            cdnAuthToken: token
                                        );

                                        if (!VerifyChunkHash(buffer, written, chunk))
                                        {
                                            if (attempt < MaxRetries - 1)
                                            {
                                                Log(
                                                    $"Chunk SHA-1 mismatch at offset {chunk.Offset}, retrying..."
                                                );
                                                written = 0;
                                                continue;
                                            }
                                            throw new Exception(
                                                $"Chunk SHA-1 verification failed for {fileName} "
                                                    + $"at offset {chunk.Offset} after {MaxRetries} attempts"
                                            );
                                        }

                                        break;
                                    }
                                    catch (Exception tokenEx) when (attempt < MaxRetries - 1)
                                    {
                                        written = 0;
                                        Log(
                                            $"Chunk CDN auth retry failed (attempt {attempt + 1}): "
                                                + tokenEx.Message
                                        );
                                        InvalidateCdnAuthToken(depotId, server);
                                        MarkServerFailed(server);
                                    }
                                }
                                else
                                {
                                    MarkServerFailed(server);
                                }
                            }
                            catch (Exception ex) when (attempt < MaxRetries - 1)
                            {
                                Log($"Chunk download failed (attempt {attempt + 1}): {ex.Message}");
                                MarkServerFailed(server);
                            }
                        }

                        if (written == 0 && chunk.UncompressedLength > 0)
                            throw new Exception(
                                $"Failed to download chunk for {fileName} after {MaxRetries} attempts"
                            );

                        fs.Seek((long)chunk.Offset, SeekOrigin.Begin);
                        fs.Write(buffer, 0, written);

                        Interlocked.Add(ref _progress.DownloadedBytes, written);
                        ReportProgress();
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
            }

            // Verify the completed file before committing it.
            if (!VerifyFileHash(tempPath, file))
            {
                DeleteQuietly(tempPath);
                throw new Exception($"SHA-1 verification failed for {fileName} after download");
            }

            CommitDownloadedFile(tempPath, filePath, fileName);
        }
        finally
        {
            DeleteQuietly(tempPath);
            writeLock.Release();
        }
    }

    private List<DepotManifest.FileData> DeduplicateDownloads(
        List<DepotManifest.FileData> filesToDownload
    )
    {
        if (filesToDownload.Count <= 1)
            return filesToDownload;

        var deduped = new Dictionary<string, DepotManifest.FileData>(StringComparer.Ordinal);
        foreach (var file in filesToDownload)
            deduped[file.FileName] = file;

        if (deduped.Count == filesToDownload.Count)
            return filesToDownload;

        Log(
            $"Deduplicated duplicate download queue entries: {filesToDownload.Count - deduped.Count}"
        );
        return deduped.Values.ToList();
    }

    private static void ValidateDownloadFileSizes(IEnumerable<DepotManifest.FileData> files)
    {
        foreach (var file in files)
        {
            if (file.TotalSize > (ulong)MaxDepotFileBytes)
            {
                throw new IOException(
                    $"Depot file is unexpectedly large for {file.FileName}: "
                        + $"{file.TotalSize} bytes"
                );
            }
        }
    }

    private static long ComputeTotalDownloadBytes(IEnumerable<DepotManifest.FileData> files)
    {
        long total = 0;
        foreach (var file in files)
        {
            try
            {
                total = checked(total + (long)file.TotalSize);
            }
            catch (OverflowException ex)
            {
                throw new IOException(
                    $"Depot download size is too large while adding {file.FileName}: "
                        + $"{file.TotalSize} bytes",
                    ex
                );
            }
        }

        return total;
    }

    private IEnumerable<string> EnumerateDownloadingTempFiles()
    {
        try
        {
            return Directory.GetFiles(_gameDir, "*.downloading", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            Log($"Could not enumerate stale temp downloads: {ex.Message}");
            return Array.Empty<string>();
        }
    }

    private string ResolveGamePath(string manifestPath)
    {
        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            throw new IOException("Depot manifest contained an empty file path");
        }

        var normalized = NormalizeManifestPath(manifestPath)
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
        var root = Path.GetFullPath(_gameDir);
        var fullPath = Path.GetFullPath(Path.Combine(root, normalized));
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar.ToString())
            ? root
            : root + Path.DirectorySeparatorChar;

        if (
            !string.Equals(fullPath, root, StringComparison.Ordinal)
            && !fullPath.StartsWith(rootWithSeparator, StringComparison.Ordinal)
        )
        {
            throw new IOException($"Depot path escapes game directory: {manifestPath}");
        }

        return fullPath;
    }

    private void CommitDownloadedFile(string tempPath, string filePath, string fileName)
    {
        try
        {
            File.Move(tempPath, filePath, overwrite: true);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to commit downloaded file {fileName}: {ex.Message}", ex);
        }
    }

    private static string NormalizeManifestPath(string manifestPath)
    {
        if (manifestPath == null)
        {
            return string.Empty;
        }

        var nullIndex = manifestPath.IndexOf('\0');
        if (nullIndex >= 0)
        {
            manifestPath = manifestPath[..nullIndex];
        }

        return manifestPath.Replace('\\', '/');
    }

    private void EnsureEnoughFreeSpaceForFile(string directory, long fileSize, string fileName)
    {
        if (!OperatingSystem.IsAndroid())
            return;

        long availableFreeSpace;
        try
        {
            Directory.CreateDirectory(directory);
            availableFreeSpace = GetAndroidAvailableFreeSpace(directory);
        }
        catch (Exception ex)
        {
            Log($"Could not check free space for {fileName}: {ex.Message}");
            return;
        }

        var required = fileSize + AndroidMinimumFreeSpaceBytes;
        if (availableFreeSpace < required)
        {
            throw new IOException(
                $"Not enough storage for {fileName}: need {FormatBytes(required)}, "
                    + $"available {FormatBytes(availableFreeSpace)}"
            );
        }
    }

    private static long GetAndroidAvailableFreeSpace(string directory)
    {
        var usableBytes = AndroidGodotAppBridge.GetUsableSpaceBytes(directory);
        if (usableBytes > 0)
            return usableBytes;

        var root = Path.GetPathRoot(Path.GetFullPath(directory));
        if (string.IsNullOrWhiteSpace(root))
            return long.MaxValue;

        var drive = new DriveInfo(root);
        return drive.AvailableFreeSpace;
    }

    private static void DeleteQuietly(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Depot] Failed to delete temporary file {path}: {ex.Message}");
        }
    }

    // Computes SHA-1 of a decompressed chunk and compares it to the manifest ChunkID.
    private static bool VerifyChunkHash(byte[] buffer, int length, DepotManifest.ChunkData chunk)
    {
        if (chunk.ChunkID == null || chunk.ChunkID.Length == 0)
            return false;

        var hash = ComputeSha1(buffer.AsSpan(0, length));
        return HashesEqual(hash, chunk.ChunkID);
    }

    private static void ValidateChunkBounds(
        string fileName,
        ulong fileSize,
        DepotManifest.ChunkData chunk
    )
    {
        if (chunk.UncompressedLength == 0)
        {
            throw new IOException($"Depot chunk has zero length for {fileName} at offset {chunk.Offset}");
        }

        if (chunk.Offset > fileSize || chunk.UncompressedLength > fileSize - chunk.Offset)
        {
            throw new IOException(
                $"Depot chunk is outside file bounds for {fileName}: "
                    + $"offset={chunk.Offset}, length={chunk.UncompressedLength}, fileSize={fileSize}"
            );
        }
    }

    private static void ValidateFileChunks(string fileName, DepotManifest.FileData file)
    {
        if (file.TotalSize == 0)
        {
            return;
        }

        if (file.Chunks == null || !file.Chunks.Any())
        {
            throw new IOException($"Depot file has no chunks: {fileName}");
        }
    }

    // Computes SHA-1 of a file on disk and compares it to the manifest hash.
    private static bool VerifyFileHash(string path, DepotManifest.FileData file)
    {
        try
        {
            var info = new FileInfo(path);
            if (info.Length != (long)file.TotalSize)
                return false;

            if (file.FileHash == null || file.FileHash.Length == 0)
                return file.TotalSize == 0;

            var hash = ComputeFileSha1(path);
            return HashesEqual(hash, file.FileHash);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] ComputeSha1(ReadOnlySpan<byte> data)
    {
        if (!OperatingSystem.IsAndroid())
            return System.Security.Cryptography.SHA1.HashData(data);

        return AndroidJavaCrypto.Sha1HashData(data.ToArray());
    }

    private static byte[] ComputeFileSha1(string path)
    {
        if (!OperatingSystem.IsAndroid())
        {
            using var fs = File.OpenRead(path);
            return System.Security.Cryptography.SHA1.HashData(fs);
        }

        return AndroidJavaCrypto.Sha1FileHashData(path);
    }

    // Builds the list of files that need downloading. For manifest changes, uses
    // the hash diff. For all files in the target manifest, verifies the on-disk
    // copy against the expected SHA-1 ??? catching corruption from interrupted
    // writes, disk errors, or missing files.
    private List<DepotManifest.FileData> GetFilesNeedingDownload(
        DepotManifest? oldManifest,
        DepotManifest newManifest,
        bool isUpdate
    )
    {
        var oldFiles = BuildManifestFileMap(oldManifest);
        var result = new List<DepotManifest.FileData>();
        int verified = 0;
        int corrupt = 0;

        foreach (var file in newManifest.Files)
        {
            var fileName = NormalizeManifestPath(file.FileName);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                Log("Skipping depot file with an empty manifest path");
                continue;
            }

            if (file.Flags.HasFlag(EDepotFileFlag.Directory))
                continue;

            // Manifest changed for this file ??? always re-download.
            if (isUpdate)
            {
                if (
                    !oldFiles.TryGetValue(fileName, out var oldFile)
                    || !HashesEqual(file.FileHash, oldFile.FileHash)
                )
                {
                    result.Add(file);
                    continue;
                }
            }

            // Verify on-disk file matches the manifest hash.
            var filePath = ResolveGamePath(fileName);
            if (VerifyFileHash(filePath, file))
            {
                verified++;
            }
            else
            {
                if (File.Exists(filePath))
                {
                    corrupt++;
                    Log($"File needs re-download (hash mismatch): {file.FileName}");
                }
                result.Add(file);
            }
        }

        if (verified > 0)
            Log($"Verified {verified} existing files");
        if (corrupt > 0)
            Log($"Found {corrupt} corrupt files requiring re-download");

        return result;
    }

    private static bool HashesEqual(byte[]? left, byte[]? right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        return left.SequenceEqual(right);
    }

    private static Dictionary<string, DepotManifest.FileData> BuildManifestFileMap(DepotManifest? manifest)
    {
        var files = new Dictionary<string, DepotManifest.FileData>(StringComparer.Ordinal);

        if (manifest == null)
        {
            return files;
        }

        foreach (var file in manifest.Files)
        {
            var fileName = NormalizeManifestPath(file.FileName);
            if (string.IsNullOrEmpty(fileName))
            {
                continue;
            }

            files[fileName] = file;
        }

        return files;
    }

    private static List<string> GetFilesToDelete(
        DepotManifest? oldManifest,
        DepotManifest newManifest
    )
    {
        if (oldManifest == null)
            return new List<string>();

        var newFiles = new HashSet<string>(
            newManifest.Files
                .Select(f => NormalizeManifestPath(f.FileName))
                .Where(f => !string.IsNullOrEmpty(f)),
            StringComparer.Ordinal);

        return oldManifest
            .Files.Select(f => NormalizeManifestPath(f.FileName))
            .Where(f => !string.IsNullOrEmpty(f) && !newFiles.Contains(f))
            .ToList();
    }

    private ulong LoadCachedManifestId(uint depotId)
    {
        var path = Path.Combine(_stateDir, $"{depotId}.id");
        if (!File.Exists(path))
            return 0;

        try
        {
            var raw = File.ReadAllText(path).Trim();
            if (ulong.TryParse(raw, out var id))
                return id;

            Log($"Ignoring malformed cached manifest id for depot {depotId}: {raw}");
            MoveBadStateFile(path);
            return 0;
        }
        catch (Exception ex)
        {
            Log($"Could not read cached manifest id for depot {depotId}: {ex.Message}");
            return 0;
        }
    }

    private DepotManifest? LoadCachedManifest(uint depotId)
    {
        var path = Path.Combine(_stateDir, $"{depotId}.manifest");
        if (!File.Exists(path))
            return null;

        try
        {
            using var fs = File.OpenRead(path);
            return DepotManifest.Deserialize(fs);
        }
        catch (Exception ex)
        {
            Log($"Cached manifest for depot {depotId} could not be loaded: {ex.Message}");
            MoveBadStateFile(path);
            return null;
        }
    }

    private void MoveBadStateFile(string path)
    {
        try
        {
            if (!File.Exists(path))
                return;

            var badPath = path + ".bad";
            if (File.Exists(badPath))
                File.Delete(badPath);

            File.Move(path, badPath);
        }
        catch (Exception ex)
        {
            Log($"Could not quarantine bad state file {Path.GetFileName(path)}: {ex.Message}");
        }
    }

    private void SaveManifest(uint depotId, DepotManifest manifest, ulong manifestId)
    {
        var manifestPath = Path.Combine(_stateDir, $"{depotId}.manifest");
        var manifestTempPath = manifestPath + ".tmp";
        using (var fs = File.Create(manifestTempPath))
        {
            manifest.Serialize(fs);
        }
        CommitStateFile(manifestTempPath, manifestPath);

        var idPath = Path.Combine(_stateDir, $"{depotId}.id");
        var idTempPath = idPath + ".tmp";
        File.WriteAllText(idTempPath, manifestId.ToString());
        CommitStateFile(idTempPath, idPath);

        DeleteQuietly(manifestPath + ".bad");
        DeleteQuietly(idPath + ".bad");
    }

    private void CleanupStateTempFiles()
    {
        try
        {
            foreach (var path in Directory.GetFiles(_stateDir, "*.tmp"))
                DeleteQuietly(path);
        }
        catch (Exception ex)
        {
            Log($"Could not clean download state temp files: {ex.Message}");
        }
    }

    private static void CommitStateFile(string tempPath, string targetPath)
    {
        try
        {
            File.Move(tempPath, targetPath, overwrite: true);
        }
        catch (Exception ex)
        {
            DeleteQuietly(tempPath);
            throw new IOException(
                $"Failed to commit downloader state file {Path.GetFileName(targetPath)}: {ex.Message}",
                ex
            );
        }
    }

    private void Log(string msg)
    {
        PatchHelper.Log($"[Depot] {msg}");
        try
        {
            LogMessage?.Invoke(msg);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Depot] Log callback failed: {ex.Message}");
        }
    }

    private void ReportProgress()
    {
        if (OperatingSystem.IsAndroid())
        {
            var now = DateTime.UtcNow.Ticks;
            var last = Interlocked.Read(ref _lastProgressReportTicks);
            if (now - last < TimeSpan.TicksPerMillisecond * 250)
                return;

            if (Interlocked.CompareExchange(ref _lastProgressReportTicks, now, last) != last)
                return;
        }

        InvokeProgressChanged(SnapshotProgress());
    }

    private void ForceReportProgress()
    {
        Interlocked.Exchange(ref _lastProgressReportTicks, DateTime.UtcNow.Ticks);
        InvokeProgressChanged(SnapshotProgress());
    }

    private void InvokeProgressChanged(DownloadProgress progress)
    {
        try
        {
            ProgressChanged?.Invoke(progress);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Depot] Progress callback failed: {ex.Message}");
        }
    }

    private DownloadProgress SnapshotProgress()
    {
        return new DownloadProgress
        {
            TotalBytes = Interlocked.Read(ref _progress.TotalBytes),
            DownloadedBytes = Interlocked.Read(ref _progress.DownloadedBytes),
            TotalFiles = _progress.TotalFiles,
            CompletedFiles = _progress.CompletedFiles,
            CurrentFile = _progress.CurrentFile,
        };
    }

    private static string FormatBytes(long bytes)
    {
        const long kilobyte = 1024L;
        const long megabyte = kilobyte * 1024L;
        const long gigabyte = megabyte * 1024L;

        if (bytes >= gigabyte)
            return $"{bytes / (double)gigabyte:F1} GB";

        if (bytes >= megabyte)
            return $"{bytes / (double)megabyte:F1} MB";

        if (bytes >= kilobyte)
            return $"{bytes / (double)kilobyte:F1} KB";

        return $"{bytes} B";
    }

    void IDisposable.Dispose()
        => Dispose();

    internal void Dispose()
    {
        _cdnClient?.Dispose();
    }

    private static void PatchGamePck(string pckPath)
    {
        const uint maxPckPathBytes = 4096;

        if (!File.Exists(pckPath))
            return;

        try
        {
            using var fs = new FileStream(pckPath, FileMode.Open, FileAccess.ReadWrite);
            using var reader = new BinaryReader(fs);

            uint magic = reader.ReadUInt32();
            if (magic != 0x43504447) // "GDPC"
                return;

            reader.ReadUInt32(); // format version
            reader.ReadUInt32(); // major
            reader.ReadUInt32(); // minor
            reader.ReadUInt32(); // patch
            uint flags = reader.ReadUInt32();
            long fileBase = reader.ReadInt64();
            long dirBase = reader.ReadInt64();
            fs.Seek(16 * 4, SeekOrigin.Current); // 16 reserved uint32s

            bool relativeOffsets = (flags & 0x02) != 0;

            fs.Position = dirBase;
            uint fileCount = reader.ReadUInt32();
            bool patched = false;

            for (uint i = 0; i < fileCount; i++)
            {
                uint pathLen = reader.ReadUInt32();
                if (pathLen == 0 || pathLen > maxPckPathBytes)
                {
                    PatchHelper.Log($"PCK patching skipped: invalid path length {pathLen}");
                    return;
                }

                byte[] pathBytes = reader.ReadBytes((int)pathLen);
                string path = Encoding.UTF8.GetString(pathBytes).TrimEnd('\0');
                long offset = reader.ReadInt64();
                long size = reader.ReadInt64();
                reader.ReadBytes(16); // MD5
                reader.ReadUInt32(); // flags

                long absOffset = relativeOffsets ? fileBase + offset : offset;

                patched |= PatchPckEntry(path, fs, absOffset, size);
            }

            if (patched)
                PatchHelper.Log("Patched game PCK: removed Android-incompatible plugin references");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"PCK patching failed (non-fatal): {ex.Message}");
        }
    }

    private static bool IsPckPath(string path, string expected)
    {
        return path == expected || path == $"res://{expected}";
    }

    private static bool PatchPckEntry(string path, FileStream fs, long offset, long size)
    {
        if (IsPckPath(path, "project.binary"))
            return PatchProjectBinary(fs, offset, size);
        if (IsPckPath(path, "project.godot"))
            return PatchProjectGodot(fs, offset, size);
        if (IsPckPath(path, ".godot/extension_list.cfg"))
            return PatchExtensionList(fs, offset, size);
        if (IsPckPath(path, "scenes/game.tscn"))
            return PatchGameScene(fs, offset, size);

        return false;
    }

    private static bool PatchProjectGodot(FileStream fs, long offset, long size)
        => ApplyPckEntryPatch(fs, offset, size, "project.godot", content =>
        {
            var patched = false;
            patched |= CommentOutProjectSetting(
                content,
                "SentryInit=\"*res://addons/sentry/SentryInit.gd\""
            );
            patched |= CommentOutProjectSetting(
                content,
                "FmodManager=\"*res://addons/fmod/FmodManager.gd\""
            );
            return patched;
        });

    private static bool PatchExtensionList(FileStream fs, long offset, long size)
        => ApplyPckEntryPatch(fs, offset, size, "extension_list.cfg", content =>
        {
            var patched = false;
            patched |= OverwriteEntryBytes(
                content,
                "res://addons/fmod/fmod.gdextension"
            );
            patched |= OverwriteEntryBytes(
                content,
                "res://addons/sentry/sentry.gdextension"
            );
            return patched;
        });

    private static bool PatchProjectBinary(FileStream fs, long offset, long size)
        => ApplyPckEntryPatch(fs, offset, size, "project.binary", content =>
        {
            var patched = false;
            patched |= ReplaceEntryBytes(
                content,
                "autoload/SentryInit",
                "disabled/SentryInit"
            );
            patched |= ReplaceEntryBytes(
                content,
                "autoload/FmodManager",
                "disabled/FmodManager"
            );
            return patched;
        });

    private static bool PatchGameScene(FileStream fs, long offset, long size)
        => ApplyPckEntryPatch(fs, offset, size, "scenes/game.tscn", content =>
        {
            var patched = false;
            patched |= CommentOutProjectSetting(
                content,
                "[ext_resource type=\"Script\" uid=\"uid://c6blhu0io0iwp\" path=\"res://src/gdscript/audio_manager_proxy.gd\" id=\"3_xfu11\"]"
            );
            patched |= CommentOutProjectSetting(
                content,
                "[node name=\"FmodBankLoader\" type=\"FmodBankLoader\" parent=\".\"]"
            );
            patched |= CommentOutProjectSetting(
                content,
                "bank_paths = [\"res://banks/desktop/Master.strings.bank\", \"res://banks/desktop/Master.bank\", \"res://banks/desktop/sfx.bank\", \"res://banks/desktop/temp_sfx.bank\", \"res://banks/desktop/ambience.bank\"]"
            );
            patched |= CommentOutProjectSetting(
                content,
                "script = ExtResource(\"3_xfu11\")"
            );
            patched |= CommentOutProjectSetting(
                content,
                "[node name=\"FmodListener2D\" type=\"FmodListener2D\" parent=\"AudioManager\"]"
            );
            return patched;
        });

    private static bool ApplyPckEntryPatch(
        FileStream fs,
        long offset,
        long size,
        string label,
        Func<byte[], bool> patch
    )
    {
        const long maxPatchablePckEntryBytes = 8L * 1024L * 1024L;

        if (offset < 0 || size < 0 || size > maxPatchablePckEntryBytes || offset + size > fs.Length)
        {
            PatchHelper.Log(
                $"PCK patching skipped for {label}: offset={offset}, size={size}, fileSize={fs.Length}"
            );
            return false;
        }

        long savedPos = fs.Position;
        fs.Position = offset;
        var content = new byte[(int)size];
        fs.ReadExactly(content, 0, (int)size);

        if (!patch(content))
        {
            fs.Position = savedPos;
            return false;
        }

        fs.Position = offset;
        fs.Write(content, 0, content.Length);
        fs.Position = savedPos;
        return true;
    }

    private static bool OverwriteEntryBytes(byte[] content, string entry)
    {
        var search = Encode(entry);
        int idx = FindBytes(content, search);
        if (idx < 0)
            return false;

        Fill(content, idx, search.Length, (byte)' ');
        return true;
    }

    private static bool ReplaceEntryBytes(byte[] content, string searchText, string replacementText)
    {
        var search = Encode(searchText);
        var replacement = Encode(replacementText);
        if (search.Length != replacement.Length)
            throw new InvalidOperationException($"PCK replacement length mismatch for {searchText}");

        var patched = false;
        int idx;
        while ((idx = FindBytes(content, search)) >= 0)
        {
            Array.Copy(replacement, 0, content, idx, replacement.Length);
            patched = true;
        }

        return patched;
    }

    private static bool CommentOutProjectSetting(byte[] content, string setting)
    {
        var search = Encode(setting);
        int idx = FindBytes(content, search);
        if (idx < 0)
            return false;

        content[idx] = (byte)';';
        return true;
    }

    private static byte[] Encode(string value) => Encoding.UTF8.GetBytes(value);

    private static void Fill(byte[] content, int offset, int length, byte value)
    {
        for (var i = 0; i < length; i++)
            content[offset + i] = value;
    }

    private static int FindBytes(byte[] haystack, byte[] needle)
    {
        for (int i = 0; i <= haystack.Length - needle.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }
            if (match)
                return i;
        }
        return -1;
    }
}




