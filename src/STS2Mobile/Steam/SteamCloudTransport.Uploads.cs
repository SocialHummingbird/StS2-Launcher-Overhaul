using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class SteamCloudTransport
{
    private const int UploadWorkerCount = 3;

    public Task<int> UploadBatchAsync(IReadOnlyList<UploadFile> files, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(files);
        var canonical = new List<UploadFile>(files.Count);
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            ArgumentNullException.ThrowIfNull(file.Bytes);
            var path = CanonicalizePath(file.Path);
            if (!paths.Add(path)) throw new IOException($"Duplicate Steam upload destination: {path}");
            canonical.Add(file with { Path = path });
        }
        if (canonical.Count == 0) return Task.FromResult(0);
        // One externally exclusive operation owns all workers. Each active file gets
        // the existing timeout; queueing a large batch must not consume that budget.
        return RunOperationAsync(token => UploadBatchCoreAsync(canonical, token), cancellationToken, applyOperationTimeout: false);
    }

    private async Task<int> UploadBatchCoreAsync(IReadOnlyList<UploadFile> files, CancellationToken cancellationToken)
    {
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var gate = new object();
        var next = 0;
        var uploaded = 0;
        var alreadyCurrent = 0;
        var metadataVerified = 0;
        var readBackVerified = 0;
        ExceptionDispatchInfo failure = null;
        var workers = new Task[Math.Min(UploadWorkerCount, files.Count)];
        for (var i = 0; i < workers.Length; i++)
            workers[i] = Task.Run(WorkerAsync);
        try { await Task.WhenAll(workers).ConfigureAwait(false); }
        catch { failure?.Throw(); throw; }
        failure?.Throw();
        cancellationToken.ThrowIfCancellationRequested();
        PatchHelper.Log($"[Cloud] Upload batch verified: uploaded={uploaded}, already-current={alreadyCurrent}, metadata={metadataVerified}, read-back={readBackVerified}");
        return uploaded;

        void Fail(Exception error)
        {
            lock (gate) failure ??= ExceptionDispatchInfo.Capture(error);
            stop.Cancel();
        }

        async Task WorkerAsync()
        {
            while (true)
            {
                UploadFile file;
                lock (gate)
                {
                    if (stop.IsCancellationRequested || failure != null || next == files.Count) return;
                    file = files[next++];
                }
                using var budget = new CancellationTokenSource(_operationTimeout);
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stop.Token, budget.Token);
                // Failed-upload notification may finish after sibling cancellation,
                // but still obeys the caller and this file's original time budget.
                using var failureNotice = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, budget.Token);
                try
                {
                    deadline.Token.ThrowIfCancellationRequested();
                    var prepared = PreparedUpload.Create(file);
                    var outcome = await UploadFileAsync(prepared, deadline.Token, Fail, failureNotice.Token).ConfigureAwait(false);
                    var usedMetadata = await VerifyUploadedFileAsync(prepared, deadline.Token).ConfigureAwait(false);
                    deadline.Token.ThrowIfCancellationRequested();
                    lock (gate)
                    {
                        if (outcome == UploadOutcome.Uploaded) uploaded++; else alreadyCurrent++;
                        if (usedMetadata) metadataVerified++; else readBackVerified++;
                    }
                }
                catch (Exception error)
                {
                    if (error is OperationCanceledException && !stop.IsCancellationRequested && budget.IsCancellationRequested)
                        error = new TimeoutException($"Steam Cloud operation timed out after {_operationTimeout.TotalSeconds:0} seconds", error);
                    Fail(error);
                    return;
                }
            }
        }
    }

    private async Task<bool> VerifyUploadedFileAsync(PreparedUpload upload, CancellationToken cancellationToken)
    {
        var metadata = await GetDownloadMetadataAsync(upload.Path, cancellationToken).ConfigureAwait(false);
        if (metadata.appid != SteamGameApp.AppId || metadata.is_explicit_delete)
            throw new InvalidDataException($"Steam upload verification identity mismatch for {upload.Path}");

        var hasHash = metadata.sha_file is { Length: 20 };
        var hasSize = metadata.ShouldSerializeraw_file_size();
        if ((hasHash && !CryptographicOperations.FixedTimeEquals(upload.Sha1, metadata.sha_file))
            || (hasSize && metadata.raw_file_size != upload.RawBytes.LongLength))
            throw new InvalidDataException($"Steam upload verification metadata mismatch for {upload.Path}");
        if (hasHash && hasSize) return true;

        // A missing body URL can describe an empty file only when Steam explicitly
        // reports its raw size. Omitted fields must not manufacture empty contents.
        if (string.IsNullOrWhiteSpace(metadata.url_host) && (!hasSize || metadata.raw_file_size != 0))
            throw new InvalidDataException($"Steam read-back body is unavailable for {upload.Path}");
        var bytes = await ReadDownloadBodyAsync(upload.Path, metadata, cancellationToken, upload.RawBytes.LongLength).ConfigureAwait(false);
        if (bytes.LongLength != upload.RawBytes.LongLength
            || !CryptographicOperations.FixedTimeEquals(upload.Sha1, ManagedSha1.Hash(bytes)))
            throw new InvalidDataException($"Steam read-back hash mismatch for {upload.Path}");
        return false;
    }
}
