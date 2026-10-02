using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.CDN;
using System.Linq;
using System.Threading;
using System.Buffers;
using System.IO;

namespace STS2Mobile.Steam;
internal sealed partial class DepotDownloader
{
    private sealed class CdnDownloadOperation<T>
    {
        private readonly Func<CdnServerAttempt, Task<CdnDownloadResult<T>>> _downloadAsync;
        private readonly Func<CdnServerAttempt, Task<CdnDownloadResult<T>>> _downloadWithAuthAsync;
        private readonly Func<Exception> _createFailure;
        private CdnDownloadOperation(string name, Func<CdnServerAttempt, Task<CdnDownloadResult<T>>> downloadAsync, Func<CdnServerAttempt, Task<CdnDownloadResult<T>>> downloadWithAuthAsync, Func<Exception> createFailure)
        {
            Name = name;
            _downloadAsync = downloadAsync;
            _downloadWithAuthAsync = downloadWithAuthAsync;
            _createFailure = createFailure;
        }

        private string Name { get; }

        internal static CdnDownloadOperation<T> AcrossServersWithAuthRetry(string name, Func<CdnServerAttempt, Task<CdnDownloadResult<T>>> downloadAsync, Func<CdnServerAttempt, Task<CdnDownloadResult<T>>> downloadWithAuthAsync, Func<Exception> createFailure) => new(name, downloadAsync, downloadWithAuthAsync, createFailure);
        internal async Task<T> RunAsync(DepotDownloader owner, IEnumerable<CdnServerAttempt> attempts)
        {
            foreach (var attempt in attempts)
            {
                var result = await TryAsync(owner, attempt);
                if (result.TryGetValue(out var value))
                    return value;
            }

            throw CreateFailure();
        }

        private async Task<CdnDownloadResult<T>> TryAsync(DepotDownloader owner, CdnServerAttempt attempt)
        {
            try
            {
                return await DownloadAsync(attempt);
            }
            catch (SteamKitWebRequestException ex)when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                return await DownloadWithAuthAsync(attempt);
            }
            catch (Exception ex)when (attempt.CanRetry())
            {
                return RetryAfterFailure(owner, attempt, ex);
            }
        }

        private Task<CdnDownloadResult<T>> DownloadAsync(CdnServerAttempt attempt) => _downloadAsync(attempt);
        private Task<CdnDownloadResult<T>> DownloadWithAuthAsync(CdnServerAttempt attempt) => _downloadWithAuthAsync(attempt);
        private CdnDownloadResult<T> RetryAfterFailure(DepotDownloader owner, CdnServerAttempt attempt, Exception ex)
        {
            attempt.HandleDownloadRetryFailure(owner, Name, ex);
            return CdnDownloadResult<T>.Retry();
        }

        private Exception CreateFailure() => _createFailure();
    }

    private readonly struct CdnDownloadResult<T>
    {
        private CdnDownloadResult(bool succeeded, T value)
        {
            Succeeded = succeeded;
            Value = value;
        }

        private bool Succeeded { get; }
        private T Value { get; }

        internal bool TryGetValue(out T value)
        {
            value = Value;
            return Succeeded;
        }

        internal static CdnDownloadResult<T> Success(T value) => new(true, value);
        internal static CdnDownloadResult<T> Retry() => new(false, default !);
        internal static async Task<CdnDownloadResult<T>> FromAsync(Func<Task<T>> downloadAsync) => Success(await downloadAsync());
        internal static async Task<CdnDownloadResult<T>> FromValidatedAsync(Func<Task<T>> downloadAsync, Func<T, bool> isValid)
        {
            var value = await downloadAsync();
            return isValid(value) ? Success(value) : Retry();
        }
    }

    private readonly struct CdnServerAttempt
    {
        internal CdnServerAttempt(Server server, int index)
        {
            Server = server;
            Index = index;
        }

        private Server Server { get; }
        private int Index { get; }
        private int DisplayNumber => Index + 1;
        private bool HasRetryRemaining => Index < MaxRetries - 1;

        internal Task<int> DownloadChunkAsync(DepotDownloader owner, uint depotId, DepotManifest.ChunkData chunk, byte[] buffer, byte[] depotKey, string? cdnAuthToken = null) => owner._cdnClient.DownloadDepotChunkAsync(depotId, chunk, Server, buffer, depotKey, cdnAuthToken: cdnAuthToken);
        internal Task<DepotManifest> DownloadManifestAsync(DepotDownloader owner, uint depotId, ulong manifestId, ulong manifestRequestCode, byte[] depotKey, string? cdnAuthToken = null) => owner._cdnClient.DownloadManifestAsync(depotId, manifestId, manifestRequestCode, Server, depotKey, cdnAuthToken: cdnAuthToken);
        internal async Task<string?> GetAuthTokenForRetryAsync(DepotDownloader owner, uint depotId)
        {
            var token = await owner.GetCdnAuthToken(depotId, Server);
            if (token == null)
                MarkFailed(owner);
            return token;
        }

        internal bool CanRetry() => HasRetryRemaining;
        internal void HandleAuthRetryFailure(DepotDownloader owner, uint depotId, string operation, Exception ex)
        {
            owner.Log($"{operation} CDN auth retry failed (attempt {DisplayNumber}): {ex.Message}");
            owner.InvalidateCdnAuthToken(depotId, Server);
            MarkFailed(owner);
        }

        internal void HandleDownloadRetryFailure(DepotDownloader owner, string operation, Exception ex)
        {
            owner.Log($"{operation} failed (attempt {DisplayNumber}): {ex.Message}");
            MarkFailed(owner);
        }

        private void MarkFailed(DepotDownloader owner) => owner.MarkServerFailed(Server);
    }

    private IReadOnlyList<Server> _servers = Array.Empty<Server>();
    private int _serverIndex;
    private async Task<IReadOnlyList<Server>> LoadCdnServersAsync(CancellationToken ct)
    {
        Log("Getting CDN servers...");
        var allServers = await _connection.LoadCdnServersAsync(ct);
        if (allServers == null || allServers.Count == 0)
            throw new Exception("No CDN servers available");
        var servers = allServers.Where(s => s.Type == "SteamCache" || s.Type == "CDN").OrderBy(s => s.WeightedLoad).ToList();
        if (servers.Count == 0)
            servers = allServers.ToList();
        Log($"Using {servers.Count} CDN servers");
        return servers;
    }

    private Server GetCurrentServer()
    {
        if (_servers.Count == 0)
            throw new InvalidOperationException("No Steam CDN servers are available");
        var idx = Volatile.Read(ref _serverIndex);
        return _servers[((idx % _servers.Count) + _servers.Count) % _servers.Count];
    }

    private IEnumerable<CdnServerAttempt> CdnDownloadAttempts()
    {
        for (int attemptIndex = 0; attemptIndex < MaxRetries; attemptIndex++)
            yield return new CdnServerAttempt(GetCurrentServer(), attemptIndex);
    }

    private void MarkServerFailed(Server server)
    {
        if (_servers.Count <= 1 || server == null)
            return;
        var current = GetCurrentServer();
        if (string.Equals(current.Host, server.Host, StringComparison.OrdinalIgnoreCase))
            Interlocked.Increment(ref _serverIndex);
    }

    private Task<CdnDownloadResult<int>> TryDownloadChunkWithAuthAsync(uint depotId, DepotManifest.ChunkData chunk, byte[] buffer, byte[] depotKey, string fileName, CdnServerAttempt attempt) => RunCdnAuthRetryAsync(new CdnAuthRetry<int>(depotId, attempt, CdnChunkAuthRetryOperation, token => CdnDownloadResult<int>.FromValidatedAsync(() => attempt.DownloadChunkAsync(this, depotId, chunk, buffer, depotKey, token), written => ChunkHashVerifiedOrRetry(fileName, chunk, buffer, written, attempt))));
    private readonly struct ChunkWriteRequest
    {
        private ChunkWriteRequest(FileStream stream, uint depotId, DepotManifest.ChunkData chunk, byte[] depotKey, string fileName)
        {
            Stream = stream;
            DepotId = depotId;
            Chunk = chunk;
            DepotKey = depotKey;
            FileName = fileName;
        }

        private FileStream Stream { get; }
        private uint DepotId { get; }
        private DepotManifest.ChunkData Chunk { get; }
        private byte[] DepotKey { get; }
        private string FileName { get; }
        private int Length => checked((int)Chunk.UncompressedLength);

        internal static ChunkWriteRequest Create(FileStream stream, uint depotId, DepotManifest.ChunkData chunk, byte[] depotKey, string fileName) => new(stream, depotId, chunk, depotKey, fileName);
        internal async Task RunAsync(DepotDownloader owner)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(Length);
            try
            {
                var written = await owner.DownloadChunkWithRetriesAsync(DepotId, Chunk, buffer, DepotKey, FileName);
                Write(buffer, written);
                owner.RecordChunkWritten(written);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private void Write(byte[] buffer, int written)
        {
            Stream.Seek((long)Chunk.Offset, SeekOrigin.Begin);
            Stream.Write(buffer, 0, written);
        }
    }

    private async Task DownloadAndWriteChunkAsync(FileStream fs, uint depotId, DepotManifest.ChunkData chunk, byte[] depotKey, string fileName) => await ChunkWriteRequest.Create(fs, depotId, chunk, depotKey, fileName).RunAsync(this);
    private void RecordChunkWritten(int written)
    {
        Interlocked.Add(ref _downloadedBytes, written);
        ReportProgress();
    }

    private static void ValidateChunkSize(string fileName, DepotManifest.ChunkData chunk)
    {
        if (chunk.UncompressedLength <= (ulong)MaxDepotChunkBytes)
            return;
        throw new IOException($"Depot chunk is unexpectedly large for {fileName}: " + $"{chunk.UncompressedLength} bytes");
    }

    private Task<int> DownloadChunkWithRetriesAsync(uint depotId, DepotManifest.ChunkData chunk, byte[] buffer, byte[] depotKey, string fileName) => RunCdnDownloadWithRetriesAsync(CreateChunkDownloadOperation(depotId, chunk, buffer, depotKey, fileName));
    private CdnDownloadOperation<int> CreateChunkDownloadOperation(uint depotId, DepotManifest.ChunkData chunk, byte[] buffer, byte[] depotKey, string fileName) => CdnDownloadOperation<int>.AcrossServersWithAuthRetry(CdnChunkDownloadOperation, attempt => TryDownloadChunkAsync(depotId, chunk, buffer, depotKey, fileName, attempt), attempt => TryDownloadChunkWithAuthAsync(depotId, chunk, buffer, depotKey, fileName, attempt), () => new Exception($"Failed to download chunk for {fileName} after {MaxRetries} attempts"));
    private Task<CdnDownloadResult<int>> TryDownloadChunkAsync(uint depotId, DepotManifest.ChunkData chunk, byte[] buffer, byte[] depotKey, string fileName, CdnServerAttempt attempt) => CdnDownloadResult<int>.FromValidatedAsync(() => attempt.DownloadChunkAsync(this, depotId, chunk, buffer, depotKey), written => ChunkHashVerifiedOrRetry(fileName, chunk, buffer, written, attempt));
    private bool ChunkHashVerifiedOrRetry(string fileName, DepotManifest.ChunkData chunk, byte[] buffer, int written, CdnServerAttempt attempt)
    {
        if (VerifyChunkHash(buffer, written, chunk))
            return true;
        if (attempt.CanRetry())
        {
            Log($"Chunk SHA-1 mismatch at offset {chunk.Offset}, retrying...");
            return false;
        }

        throw new Exception($"Chunk SHA-1 verification failed for {fileName} " + $"at offset {chunk.Offset} after {MaxRetries} attempts");
    }
}
