using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

// ICloudSaveStore backed by SteamKit2 CCloud unified messages.
internal sealed class SteamKit2CloudSaveStore : ICloudSaveStore, ISaveStore, IDisposable
{
    private const string CloudZipDataEntryName = "data";

    private static SteamKit2CloudSaveStore Instance { get; set; }

    private readonly SteamConnection _connection;
    private readonly CloudFileCache _cache;
    private readonly CloudWriteQueue _writeQueue;
    private readonly object _batchLock = new();
    private readonly List<(string path, byte[] bytes)> _pendingBatchFiles = new();
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private bool _collectingBatch;

    internal SteamKit2CloudSaveStore(string accountName, string refreshToken)
    {
        _connection = new SteamConnection(accountName, refreshToken);
        _cache = new CloudFileCache(_connection);
        _writeQueue = new CloudWriteQueue();

        Instance = this;
    }

    internal static SteamKit2CloudSaveStore GetOrCreate(string accountName, string refreshToken)
        => Instance ?? new SteamKit2CloudSaveStore(accountName, refreshToken);

    internal static bool FlushActive(int timeoutMs)
        => Instance?.Flush(timeoutMs) ?? true;

    internal bool Flush(int timeoutMs = 5000)
    {
        var queueFlushed = true;
        try
        {
            queueFlushed = _writeQueue.Flush(timeoutMs);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(CloudRuntimeMessage.QueueFlushFailed(ex));
            queueFlushed = false;
        }

        try
        {
            _connection.Flush();
        }
        catch (Exception ex)
        {
            PatchHelper.Log(CloudRuntimeMessage.ConnectionFlushFailed(ex));
            return false;
        }

        return queueFlushed;
    }

    void IDisposable.Dispose()
        => Dispose();

    internal void Dispose()
    {
        _writeQueue.Dispose();
        _connection.Dispose();
        _http.Dispose();
        if (Instance == this)
            Instance = null;
    }

    public string ReadFile(string path)
    {
        return ReadFileAsync(path).GetAwaiter().GetResult();
    }

    public async Task<string> ReadFileAsync(string path)
    {
        path = CloudFileCache.CanonicalizePath(path);

        if (!_cache.FileExists(path))
            throw new FileNotFoundException($"Cloud file not found: {path}");

        if (_cache.GetFileSize(path) == 0)
            return string.Empty;

        var result = await _connection
            .SendCloud<CCloud_ClientFileDownload_Request, CCloud_ClientFileDownload_Response>(
                "ClientFileDownload",
                new CCloud_ClientFileDownload_Request { appid = SteamCloudApp.AppId, filename = path }
            )
            .ConfigureAwait(false);

        if (result.appid != SteamCloudApp.AppId || string.IsNullOrEmpty(result.url_host))
            throw new InvalidOperationException($"Cloud download failed for {path}");

        var scheme = result.use_https ? "https" : "http";
        var url = $"{scheme}://{result.url_host}{result.url_path}";

        var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);
        foreach (var header in result.request_headers)
            httpRequest.Headers.TryAddWithoutValidation(header.name, header.value);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var httpResponse = await _http.SendAsync(httpRequest, cts.Token).ConfigureAwait(false);
        httpResponse.EnsureSuccessStatusCode();
        var data = await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        PatchHelper.Log(CloudRuntimeMessage.Downloaded(
            path,
            data.Length,
            result.encrypted,
            result.file_size,
            result.raw_file_size));

        // Only decompress if ZIP magic header present (PK\x03\x04).
        if (
            result.raw_file_size > 0
            && result.raw_file_size != result.file_size
            && data.Length >= 4
            && data[0] == 0x50
            && data[1] == 0x4B
            && data[2] == 0x03
            && data[3] == 0x04
        )
        {
            var compressedSize = data.Length;
            data = DecompressCloudFile(data);
            PatchHelper.Log(CloudRuntimeMessage.Unzipped(path, compressedSize, data.Length));
        }

        return Encoding.UTF8.GetString(data);
    }

    public void WriteFile(string path, string content)
    {
        WriteFile(path, Encoding.UTF8.GetBytes(content));
    }

    public void WriteFile(string path, byte[] bytes)
    {
        var canonPath = CloudFileCache.CanonicalizePath(path);
        var truncatedNow = DateTimeOffset.FromUnixTimeSeconds(
            DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        );
        _cache.Set(canonPath, bytes.Length, truncatedNow);

        if (TryAddToSaveBatch(path, bytes))
            return;

        var ts = truncatedNow;
        _writeQueue.Enqueue(() => UploadWithRetry(path, bytes, timestamp: ts));
    }

    public Task WriteFileAsync(string path, string content)
    {
        WriteFile(path, content);
        return Task.CompletedTask;
    }

    public Task WriteFileAsync(string path, byte[] bytes)
    {
        WriteFile(path, bytes);
        return Task.CompletedTask;
    }

    public bool FileExists(string path) => _cache.FileExists(path);

    public bool DirectoryExists(string path) => true;

    public void DeleteFile(string path)
    {
        var canonPath = CloudFileCache.CanonicalizePath(path);
        _cache.Remove(canonPath);

        _writeQueue.Enqueue(() =>
        {
            RunWithTooManyPendingRetry(
                "Delete",
                canonPath,
                maxAttempts: 3,
                _ => 1000,
                _ =>
                    _connection
                        .SendCloud<
                            CCloud_ClientDeleteFile_Request,
                            CCloud_ClientDeleteFile_Response
                        >(
                            "ClientDeleteFile",
                            new CCloud_ClientDeleteFile_Request
                            {
                                appid = SteamCloudApp.AppId,
                                filename = canonPath,
                            }
                        )
                        .GetAwaiter()
                        .GetResult()
            );
        });
    }

    public void RenameFile(string sourcePath, string destinationPath)
    {
        var content = ReadFile(sourcePath);
        WriteFile(destinationPath, content);
        try
        {
            DeleteFile(sourcePath);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(CloudRuntimeMessage.RenameDeleteFailed(
                CloudFileCache.CanonicalizePath(sourcePath),
                ex));
        }
    }

    public string[] GetFilesInDirectory(string directoryPath) =>
        _cache.GetFilesInDirectory(directoryPath);

    public string[] GetDirectoriesInDirectory(string directoryPath) =>
        _cache.GetDirectoriesInDirectory(directoryPath);

    public void CreateDirectory(string directoryPath) { }

    public void DeleteDirectory(string directoryPath) { }

    public void DeleteTemporaryFiles(string directoryPath) { }

    public DateTimeOffset GetLastModifiedTime(string path) => _cache.GetLastModifiedTime(path);

    public int GetFileSize(string path) => _cache.GetFileSize(path);

    public void SetLastModifiedTime(string path, DateTimeOffset time) =>
        throw new NotImplementedException();

    public string GetFullPath(string filename) => throw new NotImplementedException();

    public bool HasCloudFiles() => _cache.HasCloudFiles();

    public void ForgetFile(string path) => _cache.ForgetFile(path);

    public bool IsFilePersisted(string path) => _cache.IsFilePersisted(path);

    public void BeginSaveBatch()
    {
        lock (_batchLock)
        {
            _collectingBatch = true;
            _pendingBatchFiles.Clear();
        }
    }

    public void EndSaveBatch()
    {
        var files = EndSaveBatchBuffer();
        if (files.Count == 0)
            return;

        _writeQueue.Enqueue(() =>
            UploadSaveBatch(files));
    }

    private void UploadSaveBatch(List<(string path, byte[] bytes)> files)
    {
        ulong batchId;
        try
        {
            batchId = BeginUploadBatch(files);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(CloudRuntimeMessage.BeginSaveBatchFailed(ex));
            UploadBatchFilesIndividually(files);
            return;
        }

        foreach (var (path, bytes) in files)
            UploadWithRetry(path, bytes, batchId);

        CompleteUploadBatch(batchId);
    }

    private ulong BeginUploadBatch(List<(string path, byte[] bytes)> files)
    {
        var request = new CCloud_BeginAppUploadBatch_Request
        {
            appid = SteamCloudApp.AppId,
            machine_name = "android",
        };
        foreach (var (path, _) in files)
            request.files_to_upload.Add(CloudFileCache.CanonicalizePath(path));

        var result = _connection
            .SendCloud<
                CCloud_BeginAppUploadBatch_Request,
                CCloud_BeginAppUploadBatch_Response
            >("BeginAppUploadBatch", request)
            .GetAwaiter()
            .GetResult();
        return result.batch_id;
    }

    private void UploadBatchFilesIndividually(List<(string path, byte[] bytes)> files)
    {
        foreach (var (path, bytes) in files)
            UploadWithRetry(path, bytes);
    }

    private void CompleteUploadBatch(ulong batchId)
    {
        try
        {
            _connection
                .SendCloud<
                    CCloud_CompleteAppUploadBatch_Request,
                    CCloud_CompleteAppUploadBatch_Response
                >(
                    "CompleteAppUploadBatchBlocking",
                    new CCloud_CompleteAppUploadBatch_Request
                    {
                        appid = SteamCloudApp.AppId,
                        batch_id = batchId,
                        batch_eresult = (uint)SteamKit2.EResult.OK,
                    }
                )
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            PatchHelper.Log(CloudRuntimeMessage.EndSaveBatchFailed(ex));
        }
    }

    private void UploadWithRetry(
        string path,
        byte[] bytes,
        ulong batchId = 0,
        DateTimeOffset? timestamp = null
    )
    {
        RunWithTooManyPendingRetry(
            "Upload",
            CloudFileCache.CanonicalizePath(path),
            maxAttempts: 3,
            attempt => (attempt + 1) * 2000,
            attempt => UploadFileAsync(path, bytes, batchId, timestamp).GetAwaiter().GetResult()
        );
    }

    private bool TryAddToSaveBatch(string path, byte[] bytes)
    {
        lock (_batchLock)
        {
            if (!_collectingBatch)
                return false;

            _pendingBatchFiles.Add((path, bytes));
            return true;
        }
    }

    private List<(string path, byte[] bytes)> EndSaveBatchBuffer()
    {
        lock (_batchLock)
        {
            _collectingBatch = false;

            if (_pendingBatchFiles.Count == 0)
                return new List<(string path, byte[] bytes)>();

            var files = new List<(string path, byte[] bytes)>(_pendingBatchFiles);
            _pendingBatchFiles.Clear();
            return files;
        }
    }

    private static void RunWithTooManyPendingRetry(
        string operationName,
        string path,
        int maxAttempts,
        Func<int, int> retryDelayMs,
        Action<int> action
    )
    {
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                action(attempt);
                return;
            }
            catch (InvalidOperationException ex)
                when (ex.Message.Contains("TooManyPending") && attempt < maxAttempts - 1)
            {
                var delayMs = retryDelayMs(attempt);
                PatchHelper.Log(CloudRuntimeMessage.OperationThrottled(operationName, path, delayMs));
                System.Threading.Thread.Sleep(delayMs);
            }
            catch (Exception ex)
            {
                PatchHelper.Log(CloudRuntimeMessage.OperationFailed(operationName, path, ex));
                return;
            }
        }
    }

    private async Task UploadFileAsync(
        string path,
        byte[] bytes,
        ulong batchId,
        DateTimeOffset? timestamp = null
    )
    {
        path = CloudFileCache.CanonicalizePath(path);

        var fileHash = SHA1.HashData(bytes);
        var rawSize = (uint)bytes.Length;
        var (uploadBytes, compressed) = CompressCloudFile(bytes);

        if (compressed)
            PatchHelper.Log(CloudRuntimeMessage.Compressed(path, rawSize, uploadBytes.Length));
        else
            PatchHelper.Log(CloudRuntimeMessage.UploadingUncompressed(path, rawSize));

        var uploadTimestamp = timestamp.HasValue
            ? (ulong)timestamp.Value.ToUnixTimeSeconds()
            : (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var beginRequest = new CCloud_ClientBeginFileUpload_Request
        {
            appid = SteamCloudApp.AppId,
            filename = path,
            file_size = (uint)uploadBytes.Length,
            raw_file_size = rawSize,
            file_sha = fileHash,
            time_stamp = uploadTimestamp,
            can_encrypt = false,
            is_shared_file = false,
        };

        if (batchId != 0)
            beginRequest.upload_batch_id = batchId;

        CCloud_ClientBeginFileUpload_Response beginResult;
        try
        {
            beginResult = await _connection
                .SendCloud<
                    CCloud_ClientBeginFileUpload_Request,
                    CCloud_ClientBeginFileUpload_Response
                >("ClientBeginFileUpload", beginRequest)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("DuplicateRequest"))
        {
            PatchHelper.Log(CloudRuntimeMessage.UploadSkippedAlreadyUpToDate(path));
            return;
        }

        bool uploadSucceeded = false;
        try
        {
            foreach (var block in beginResult.block_requests)
            {
                var scheme = block.use_https ? "https" : "http";
                var url = $"{scheme}://{block.url_host}{block.url_path}";

                var method = block.http_method == 2 ? HttpMethod.Post : HttpMethod.Put;
                var request = new HttpRequestMessage(method, url);

                byte[] bodyData =
                    block.explicit_body_data?.Length > 0
                        ? block.explicit_body_data
                        : uploadBytes[
                            (int)block.block_offset..(
                                (int)block.block_offset + (int)block.block_length
                            )
                        ];

                request.Content = new ByteArrayContent(bodyData);
                request.Content.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                request.Content.Headers.ContentLength = bodyData.Length;

                foreach (var header in block.request_headers)
                    request.Headers.TryAddWithoutValidation(header.name, header.value);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var httpResponse = await _http.SendAsync(request, cts.Token).ConfigureAwait(false);
                httpResponse.EnsureSuccessStatusCode();
            }

            uploadSucceeded = true;
        }
        finally
        {
            try
            {
                var commitResult = await _connection
                    .SendCloud<
                        CCloud_ClientCommitFileUpload_Request,
                        CCloud_ClientCommitFileUpload_Response
                    >(
                        "ClientCommitFileUpload",
                        new CCloud_ClientCommitFileUpload_Request
                        {
                            transfer_succeeded = uploadSucceeded,
                            appid = SteamCloudApp.AppId,
                            file_sha = fileHash,
                            filename = path,
                        }
                    )
                    .ConfigureAwait(false);

                if (uploadSucceeded && !commitResult.file_committed)
                    PatchHelper.Log(CloudRuntimeMessage.CommitReturnedFalse(path));
            }
            catch (Exception ex)
            {
                PatchHelper.Log(CloudRuntimeMessage.CommitFailed(path, ex));
            }
        }

        if (!uploadSucceeded)
            throw new InvalidOperationException($"Cloud upload failed for {path}");

        PatchHelper.Log(CloudRuntimeMessage.Wrote(path, bytes.Length, compressed));
    }

    private static (byte[] data, bool compressed) CompressCloudFile(byte[] raw)
    {
        var zipped = CreateSingleEntryCloudZip(raw);
        if (zipped.Length >= raw.Length)
            return (raw, false);

        return (zipped, true);
    }

    private static byte[] DecompressCloudFile(byte[] zipData)
    {
        using var archive = new ZipArchive(new MemoryStream(zipData), ZipArchiveMode.Read);
        if (archive.Entries.Count == 0)
            throw new InvalidDataException("Cloud ZIP archive contains no entries");

        var entry = archive.Entries[0];
        using var stream = entry.Open();
        using var output = new MemoryStream();
        stream.CopyTo(output);
        return output.ToArray();
    }

    private static byte[] CreateSingleEntryCloudZip(byte[] raw)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(CloudZipDataEntryName, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            entryStream.Write(raw, 0, raw.Length);
        }

        return ms.ToArray();
    }

    private sealed class CloudWriteQueue : IDisposable
    {
        private const int EnqueueTimeoutMs = 2500;
        private const int MaxQueuedWrites = 256;

        private readonly BlockingCollection<Action> _queue = new(
            new ConcurrentQueue<Action>(),
            MaxQueuedWrites
        );
        private readonly ManualResetEventSlim _drainSignal = new(initialState: true);
        private readonly Thread _thread;
        private long _droppedWrites;
        private bool _isDisposed;
        private long _pendingWrites;

        private CloudWriteQueue()
        {
            _thread = new Thread(ProcessLoop)
            {
                IsBackground = true,
                Name = "CloudSaveWriter",
            };
            _thread.Start();
        }

        private void Enqueue(Action action)
        {
            if (_isDisposed)
            {
                PatchHelper.Log(CloudRuntimeMessage.WriteQueueDisposedDrop);
                return;
            }

            if (action == null)
            {
                PatchHelper.Log(CloudRuntimeMessage.WriteQueueNullActionDrop);
                return;
            }

            MarkQueued();
            try
            {
                if (!_queue.TryAdd(action, EnqueueTimeoutMs))
                {
                    var dropped = MarkDroppedQueuedWrite();
                    PatchHelper.Log(CloudRuntimeMessage.WriteQueueFull(MaxQueuedWrites, dropped));
                    return;
                }
            }
            catch (InvalidOperationException)
            {
                PatchHelper.Log(CloudRuntimeMessage.WriteQueueClosingDrop);
                MarkDroppedQueuedWrite();
            }
        }

        private bool Flush(int timeoutMs = 5000)
        {
            if (_isDisposed)
                return true;

            var pending = PendingWrites;
            if (pending <= 0 && _queue.Count == 0)
                return true;

            if (PendingWrites == 0 && _queue.Count == 0)
                return true;

            PatchHelper.Log(CloudRuntimeMessage.FlushingPendingWrites(pending));

            if (_drainSignal.Wait(timeoutMs))
            {
                PatchHelper.Log(CloudRuntimeMessage.FlushCompleted);
                return true;
            }

            PatchHelper.Log(CloudRuntimeMessage.FlushTimedOut(_queue.Count, PendingWrites));
            if (DroppedWrites > 0)
                PatchHelper.Log(CloudRuntimeMessage.FlushDroppedWriteWarning(DroppedWrites));
            return false;
        }

        void IDisposable.Dispose()
            => Dispose();

        private void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            var completed = Flush(5000);
            if (!completed)
                PatchHelper.Log(CloudRuntimeMessage.FlushTimedOutDuringDispose);

            _queue.CompleteAdding();
            if (!_thread.Join(3000))
                PatchHelper.Log(CloudRuntimeMessage.WriteThreadStopTimedOut);
            else
                PatchHelper.Log(CloudRuntimeMessage.WriteThreadStopped);

            if (DroppedWrites > 0)
                PatchHelper.Log(CloudRuntimeMessage.TotalDroppedWrites(DroppedWrites));

            _queue.Dispose();
            _drainSignal.Dispose();
        }

        private long DroppedWrites => Volatile.Read(ref _droppedWrites);

        private long PendingWrites => Volatile.Read(ref _pendingWrites);

        private void MarkQueued()
        {
            _drainSignal.Reset();
            Interlocked.Increment(ref _pendingWrites);
        }

        private void MarkCompleted()
        {
            if (Interlocked.Decrement(ref _pendingWrites) <= 0)
                _drainSignal.Set();
        }

        private long MarkDroppedQueuedWrite()
        {
            var dropped = Interlocked.Increment(ref _droppedWrites);
            MarkCompleted();
            return dropped;
        }

        private void ProcessLoop()
        {
            foreach (var action in _queue.GetConsumingEnumerable())
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    PatchHelper.Log(CloudRuntimeMessage.BackgroundWriteFailed(ex));
                }
                finally
                {
                    MarkCompleted();
                }
            }
        }
    }
}

