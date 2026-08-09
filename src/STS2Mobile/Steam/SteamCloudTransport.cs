using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal interface ICloudMessageClient : IDisposable
{
    void BeginOperation();

    void EndOperation();

    Task<TResult> SendCloudAsync<TRequest, TResult>(
        string method,
        TRequest request,
        CancellationToken cancellationToken
    )
        where TRequest : ProtoBuf.IExtensible, new()
        where TResult : ProtoBuf.IExtensible, new();
}

// The synchronization service depends on this small file-transfer boundary.
// SteamCloudTransport is the only runtime implementation; tests use one
// in-memory fake to exercise deterministic synchronization behavior.
internal interface ISaveRemote : IDisposable
{
    Task<IReadOnlyList<SteamCloudTransport.RemoteFile>> EnumerateAsync(
        CancellationToken cancellationToken
    );

    Task<byte[]> DownloadBytesAsync(
        SteamCloudTransport.RemoteFile file,
        CancellationToken cancellationToken
    );

    Task<bool> UploadAsync(
        SteamCloudTransport.UploadFile file,
        CancellationToken cancellationToken
    );

    Task DeleteAsync(string path, CancellationToken cancellationToken);
}

// The sole Steam Cloud protocol boundary. It contains transport only: no sync
// policy, save-store implementation, cache, recovery, or launcher state.
internal sealed class SteamCloudTransport : ISaveRemote
{
    private const int PageSize = 500;
    private const int ConnectionIdleTimeoutMs = 90_000;
    private static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromMinutes(3);
    private const string ZipEntryName = "data";

    internal readonly record struct RemoteFile(
        string Path,
        long Size,
        DateTimeOffset ModifiedAt,
        string ContentHash
    );

    internal readonly record struct UploadFile(
        string Path,
        byte[] Bytes,
        DateTimeOffset ModifiedAt
    );

    private enum UploadOutcome
    {
        Uploaded,
        AlreadyCurrent,
    }

    private sealed class SteamConnectionClient : ICloudMessageClient
    {
        private readonly SteamConnection _connection;

        internal SteamConnectionClient(string accountName, string refreshToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
            ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
            _connection = new SteamConnection(
                accountName,
                refreshToken,
                ConnectionIdleTimeoutMs
            );
        }

        public void BeginOperation()
            => _connection.SuspendIdleTimeout();

        public void EndOperation()
            => _connection.ResumeIdleTimeout();

        public Task<TResult> SendCloudAsync<TRequest, TResult>(
            string method,
            TRequest request,
            CancellationToken cancellationToken
        )
            where TRequest : ProtoBuf.IExtensible, new()
            where TResult : ProtoBuf.IExtensible, new()
            => _connection.SendCloudAsync<TRequest, TResult>(
                method,
                request,
                cancellationToken
            );

        public void Dispose()
            => _connection.Dispose();
    }

    private readonly ICloudMessageClient _connection;
    private readonly HttpClient _http;
    private readonly TimeSpan _operationTimeout;
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private bool _poisoned;
    private bool _disposed;

    internal SteamCloudTransport(string accountName, string refreshToken)
        : this(
            new SteamConnectionClient(accountName, refreshToken),
            OperatingSystem.IsAndroid()
                ? AndroidJavaHttpMessageHandler.CreateCdnClient()
                : new HttpClient { Timeout = TimeSpan.FromMinutes(2) },
            DefaultOperationTimeout
        )
    {
    }

    internal SteamCloudTransport(
        ICloudMessageClient connection,
        HttpClient http,
        TimeSpan operationTimeout
    )
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(http);
        if (operationTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(operationTimeout));

        _connection = connection;
        _http = http;
        _operationTimeout = operationTimeout;
    }

    public Task<IReadOnlyList<RemoteFile>> EnumerateAsync(
        CancellationToken cancellationToken
    )
        => RunOperationAsync(EnumerateCoreAsync, cancellationToken);

    private async Task<IReadOnlyList<RemoteFile>> EnumerateCoreAsync(
        CancellationToken cancellationToken
    )
    {
        var files = new List<RemoteFile>();
        var filesWithHashes = 0;
        uint startIndex = 0;

        while (true)
        {
            var result = await _connection.SendCloudAsync<
                CCloud_EnumerateUserFiles_Request,
                CCloud_EnumerateUserFiles_Response
            >(
                "EnumerateUserFiles",
                new CCloud_EnumerateUserFiles_Request
                {
                    appid = SteamGameApp.AppId,
                    start_index = startIndex,
                    count = PageSize,
                    extended_details = true,
                },
                cancellationToken
            ).ConfigureAwait(false);

            if (result.files == null || result.files.Count == 0)
                break;

            foreach (var file in result.files)
            {
                var contentHash = file.file_sha ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(contentHash))
                    filesWithHashes++;
                files.Add(
                    new RemoteFile(
                        CanonicalizePath(file.filename),
                        file.file_size,
                        UnixTimestamp(file.timestamp),
                        contentHash
                    )
                );
            }

            startIndex += (uint)result.files.Count;
            if (result.files.Count < PageSize)
                break;
        }

        PatchHelper.Log(
            $"[Cloud] Enumerated {files.Count} Steam save files ({filesWithHashes} content hashes)"
        );
        return files;
    }

    public Task<byte[]> DownloadBytesAsync(
        RemoteFile file,
        CancellationToken cancellationToken
    )
        => RunOperationAsync(
            token => DownloadBytesCoreAsync(file.Path, token),
            cancellationToken
        );

    private async Task<byte[]> DownloadBytesCoreAsync(
        string remotePath,
        CancellationToken cancellationToken
    )
    {
        var path = CanonicalizePath(remotePath);

        var result = await _connection.SendCloudAsync<
            CCloud_ClientFileDownload_Request,
            CCloud_ClientFileDownload_Response
        >(
            "ClientFileDownload",
            new CCloud_ClientFileDownload_Request
            {
                appid = SteamGameApp.AppId,
                filename = path,
            },
            cancellationToken
        ).ConfigureAwait(false);

        if (result.appid != SteamGameApp.AppId)
            throw new InvalidOperationException($"Cloud download failed for {path}");
        if (string.IsNullOrWhiteSpace(result.url_host))
        {
            if (result.file_size == 0 && result.raw_file_size == 0)
                return Array.Empty<byte>();

            throw new InvalidOperationException($"Cloud download failed for {path}");
        }

        using var request = CreateHttpRequest(
            HttpMethod.Get,
            result.use_https,
            result.url_host,
            result.url_path
        );
        foreach (var header in result.request_headers)
            request.Headers.TryAddWithoutValidation(header.name, header.value);
        var bytes = await ReadHttpBytesAsync(request, cancellationToken)
            .ConfigureAwait(false);

        if (ShouldDecompress(result, bytes))
            bytes = Decompress(bytes);

        PatchHelper.Log($"[Cloud] Downloaded {path} ({bytes.Length} bytes)");
        return bytes;
    }

    public Task<bool> UploadAsync(
        UploadFile file,
        CancellationToken cancellationToken
    )
        => RunOperationAsync(
            token => UploadCoreAsync(file, token),
            cancellationToken
        );

    private async Task<bool> UploadCoreAsync(
        UploadFile file,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(file.Bytes);
        var canonicalFile = file with
        {
            Path = CanonicalizePath(file.Path),
        };
        var outcome = await UploadFileAsync(canonicalFile, cancellationToken)
            .ConfigureAwait(false);
        var readBack = await DownloadBytesCoreAsync(
            canonicalFile.Path,
            cancellationToken
        ).ConfigureAwait(false);
        if (
            !CryptographicOperations.FixedTimeEquals(
                ManagedSha1.Hash(canonicalFile.Bytes),
                ManagedSha1.Hash(readBack)
            )
        )
            throw new InvalidDataException(
                $"Steam read-back hash mismatch for {canonicalFile.Path}"
            );

        PatchHelper.Log(
            $"[Cloud] Verified Steam read-back for {canonicalFile.Path}"
        );
        return outcome == UploadOutcome.Uploaded;
    }

    public Task DeleteAsync(
        string path,
        CancellationToken cancellationToken
    )
        => RunOperationAsync(
            token => DeleteCoreAsync(path, token),
            cancellationToken
        );

    private async Task DeleteCoreAsync(
        string path,
        CancellationToken cancellationToken
    )
    {
        var canonical = CanonicalizePath(path);
        await _connection.SendCloudAsync<
            CCloud_ClientDeleteFile_Request,
            CCloud_ClientDeleteFile_Response
        >(
            "ClientDeleteFile",
            new CCloud_ClientDeleteFile_Request
            {
                appid = SteamGameApp.AppId,
                filename = canonical,
                is_explicit_delete = true,
            },
            cancellationToken
        ).ConfigureAwait(false);

        var tombstone = await _connection.SendCloudAsync<
            CCloud_ClientFileDownload_Request,
            CCloud_ClientFileDownload_Response
        >(
            "ClientFileDownload",
            new CCloud_ClientFileDownload_Request
            {
                appid = SteamGameApp.AppId,
                filename = canonical,
            },
            cancellationToken
        ).ConfigureAwait(false);
        if (!tombstone.is_explicit_delete)
            throw new InvalidDataException(
                $"Steam delete read-back failed for {canonical}"
            );
    }

    private async Task<UploadOutcome> UploadFileAsync(
        UploadFile file,
        CancellationToken cancellationToken
    )
    {
        var upload = PreparedUpload.Create(file);
        CCloud_ClientBeginFileUpload_Response begin;
        try
        {
            begin = await _connection.SendCloudAsync<
                CCloud_ClientBeginFileUpload_Request,
                CCloud_ClientBeginFileUpload_Response
            >(
                "ClientBeginFileUpload",
                upload.CreateBeginRequest(),
                cancellationToken
            ).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
            when (ex.Message.Contains("DuplicateRequest", StringComparison.Ordinal))
        {
            PatchHelper.Log($"[Cloud] Already current: {file.Path}");
            return UploadOutcome.AlreadyCurrent;
        }

        try
        {
            await SendUploadBlocksAsync(begin, upload.WireBytes, cancellationToken)
                .ConfigureAwait(false);

            var commit = await _connection.SendCloudAsync<
                CCloud_ClientCommitFileUpload_Request,
                CCloud_ClientCommitFileUpload_Response
            >(
                "ClientCommitFileUpload",
                upload.CreateCommitRequest(transferSucceeded: true),
                cancellationToken
            ).ConfigureAwait(false);
            if (!commit.file_committed)
                throw new InvalidOperationException(
                    $"Steam did not commit uploaded file {file.Path}"
                );
        }
        catch (Exception ex)
            when (ex is not TimeoutException and not OperationCanceledException)
        {
            await TryCommitFailedFileAsync(upload, cancellationToken)
                .ConfigureAwait(false);
            throw;
        }

        PatchHelper.Log($"[Cloud] Uploaded {file.Path} ({file.Bytes.Length} bytes)");
        return UploadOutcome.Uploaded;
    }

    private async Task TryCommitFailedFileAsync(
        PreparedUpload upload,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await _connection.SendCloudAsync<
                CCloud_ClientCommitFileUpload_Request,
                CCloud_ClientCommitFileUpload_Response
            >(
                "ClientCommitFileUpload",
                upload.CreateCommitRequest(transferSucceeded: false),
                cancellationToken
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Cloud] Failed to abort upload for {upload.Path}: {ex.Message}");
        }
    }

    private async Task SendUploadBlocksAsync(
        CCloud_ClientBeginFileUpload_Response begin,
        byte[] wireBytes,
        CancellationToken cancellationToken
    )
    {
        foreach (var block in begin.block_requests)
        {
            var body = block.explicit_body_data is { Length: > 0 }
                ? block.explicit_body_data
                : Slice(
                    wireBytes,
                    checked((int)block.block_offset),
                    checked((int)block.block_length)
                );
            using var request = CreateHttpRequest(
                block.http_method == 2 ? HttpMethod.Post : HttpMethod.Put,
                block.use_https,
                block.url_host,
                block.url_path
            );
            request.Content = new ByteArrayContent(body);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(
                "application/octet-stream"
            );
            request.Content.Headers.ContentLength = body.Length;
            foreach (var header in block.request_headers)
                request.Headers.TryAddWithoutValidation(header.name, header.value);
            await SendHttpAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<byte[]> ReadHttpBytesAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        using var response = await _http.SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task SendHttpAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        using var response = await _http.SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private static HttpRequestMessage CreateHttpRequest(
        HttpMethod method,
        bool useHttps,
        string host,
        string path
    )
        => new(method, $"{(useHttps ? "https" : "http")}://{host}{path}");

    private readonly record struct PreparedUpload(
        string Path,
        byte[] RawBytes,
        byte[] WireBytes,
        byte[] Sha1,
        ulong Timestamp
    )
    {
        internal static PreparedUpload Create(UploadFile file)
        {
            var zipped = Compress(file.Bytes);
            var wireBytes = zipped.Length < file.Bytes.Length ? zipped : file.Bytes;
            var modifiedAt = file.ModifiedAt <= DateTimeOffset.UnixEpoch
                ? DateTimeOffset.UtcNow
                : file.ModifiedAt;
            return new PreparedUpload(
                file.Path,
                file.Bytes,
                wireBytes,
                ManagedSha1.Hash(file.Bytes),
                (ulong)modifiedAt.ToUnixTimeSeconds()
            );
        }

        internal CCloud_ClientBeginFileUpload_Request CreateBeginRequest()
        {
            var request = new CCloud_ClientBeginFileUpload_Request
            {
                appid = SteamGameApp.AppId,
                filename = Path,
                file_size = checked((uint)WireBytes.Length),
                raw_file_size = checked((uint)RawBytes.Length),
                file_sha = Sha1,
                time_stamp = Timestamp,
                can_encrypt = false,
                is_shared_file = false,
            };
            return request;
        }

        internal CCloud_ClientCommitFileUpload_Request CreateCommitRequest(
            bool transferSucceeded
        )
            => new()
            {
                transfer_succeeded = transferSucceeded,
                appid = SteamGameApp.AppId,
                file_sha = Sha1,
                filename = Path,
            };
    }

    private static byte[] Compress(byte[] bytes)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(ZipEntryName, CompressionLevel.Optimal);
            using var stream = entry.Open();
            stream.Write(bytes, 0, bytes.Length);
        }

        return output.ToArray();
    }

    private static byte[] Decompress(byte[] bytes)
    {
        using var archive = new ZipArchive(
            new MemoryStream(bytes),
            ZipArchiveMode.Read
        );
        if (archive.Entries.Count == 0)
            throw new InvalidDataException("Cloud ZIP archive contains no entries");

        using var stream = archive.Entries[0].Open();
        using var output = new MemoryStream();
        stream.CopyTo(output);
        return output.ToArray();
    }

    private static bool ShouldDecompress(
        CCloud_ClientFileDownload_Response response,
        byte[] bytes
    )
        => response.raw_file_size > 0
            && response.raw_file_size != response.file_size
            && bytes.Length >= 4
            && bytes[0] == 0x50
            && bytes[1] == 0x4B
            && bytes[2] == 0x03
            && bytes[3] == 0x04;

    private static byte[] Slice(byte[] bytes, int offset, int length)
    {
        var slice = new byte[length];
        Buffer.BlockCopy(bytes, offset, slice, 0, length);
        return slice;
    }

    internal static string CanonicalizePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var normalized = path.Replace('\\', '/');
        if (normalized.StartsWith("user://", StringComparison.Ordinal))
            normalized = normalized["user://".Length..];
        if (
            normalized.StartsWith("/", StringComparison.Ordinal)
            || Path.IsPathRooted(normalized)
            || normalized.Contains(':')
        )
            throw new IOException($"Steam save path must be relative: {path}");

        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            throw new IOException("Steam save path is empty");
        foreach (var part in parts)
        {
            if (part is "." or "..")
                throw new IOException($"Steam save path is unsafe: {path}");
        }

        return string.Join('/', parts);
    }

    private static DateTimeOffset UnixTimestamp(ulong value)
    {
        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(checked((long)value));
        }
        catch (Exception ex)
            when (ex is ArgumentOutOfRangeException or OverflowException)
        {
            return DateTimeOffset.MinValue;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SteamCloudTransport));
        if (_poisoned)
            throw new InvalidOperationException(
                "The Steam Cloud connection was discarded after a failed operation"
            );
    }

    private async Task<TResult> RunOperationAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(operation);
        ThrowIfDisposed();
        await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        var failed = true;
        var operationStarted = false;
        try
        {
            ThrowIfDisposed();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken
            );
            timeout.CancelAfter(_operationTimeout);
            _connection.BeginOperation();
            operationStarted = true;
            try
            {
                var result = await operation(timeout.Token).ConfigureAwait(false);
                failed = false;
                return result;
            }
            catch (OperationCanceledException ex)
                when (
                    !cancellationToken.IsCancellationRequested
                    && timeout.IsCancellationRequested
                )
            {
                throw new TimeoutException(
                    $"Steam Cloud operation timed out after {_operationTimeout.TotalSeconds:0} seconds",
                    ex
                );
            }
        }
        finally
        {
            if (operationStarted)
                _connection.EndOperation();
            if (failed)
                DiscardConnection();
            _operationLock.Release();
        }
    }

    private async Task RunOperationAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken
    )
    {
        await RunOperationAsync(
            async token =>
            {
                await operation(token).ConfigureAwait(false);
                return true;
            },
            cancellationToken
        ).ConfigureAwait(false);
    }

    private void DiscardConnection()
    {
        _poisoned = true;
        PatchHelper.Log("[Cloud] Discarding failed Steam Cloud connection");
        try
        {
            _connection.Dispose();
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"[Cloud] Steam connection discard failed: {ex.GetType().Name}"
            );
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _connection.Dispose();
        _http.Dispose();
        _operationLock.Dispose();
    }
}
