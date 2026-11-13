using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2.Internal;
using STS2Mobile.Steam;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static SteamCloudTransport.UploadFile CloudFile(string path = "profile.save", int size = 4096)
        => new(path, Enumerable.Repeat((byte)42, size).ToArray(), DateTimeOffset.UtcNow);

    private static Task<int> CloudBatch(SteamCloudTransport transport, IReadOnlyList<SteamCloudTransport.UploadFile> files, CancellationToken token = default)
        => transport.UploadBatchAsync(files, token);

    private static void CloudUploadMetadataAvoidsReadBack()
    {
        foreach (var size in new[] { 0, 4096 })
        {
            using var fixture = new CloudUploadFixture();
            Equal(1, CloudBatch(fixture.Transport, new[] { CloudFile(size: size) }).GetAwaiter().GetResult(), "A committed upload should count once.");
            Equal(0, fixture.Gets, "Matching Steam metadata should avoid a body read-back.");
            Equal(1, fixture.MetadataCalls, "Verification should request metadata once.");
        }
    }

    private static void CloudUploadFallsBackForMissingMetadata()
    {
        foreach (var mode in new[] { "missing-hash", "bad-hash", "missing-size" })
        {
            using var fixture = new CloudUploadFixture { MetadataMode = mode };
            Equal(1, CloudBatch(fixture.Transport, new[] { CloudFile() }).GetAwaiter().GetResult(), "Fallback should verify uploaded contents.");
            Equal(1, fixture.Gets, "Fallback should download once.");
            Equal(1, fixture.MetadataCalls, "Fallback must reuse the metadata response.");
        }
    }

    private static void CloudUploadRejectsContradictions()
    {
        foreach (var mode in new[] { "wrong-hash", "wrong-size", "wrong-app", "deleted", "wrong-size-missing-hash" })
        {
            using var fixture = new CloudUploadFixture { MetadataMode = mode };
            Throws<InvalidDataException>(() => CloudBatch(fixture.Transport, new[] { CloudFile() }).GetAwaiter().GetResult());
            Equal(0, fixture.Gets, "Contradictory metadata must fail without fallback.");
        }
        using var corrupt = new CloudUploadFixture { MetadataMode = "missing-hash", CorruptReadBack = true };
        Throws<InvalidDataException>(() => CloudBatch(corrupt.Transport, new[] { CloudFile() }).GetAwaiter().GetResult());
    }

    private static void CloudUploadDuplicatesAreVerified()
    {
        using var fixture = new CloudUploadFixture { Duplicate = true };
        Equal(0, CloudBatch(fixture.Transport, new[] { CloudFile() }).GetAwaiter().GetResult(), "Already-current files must not count as new uploads.");
        Equal(1, fixture.MetadataCalls, "Duplicate requests still need verification.");
        Equal(0, fixture.Puts, "Duplicate requests should not upload content.");
        fixture.MetadataMode = "wrong-hash";
        Throws<InvalidDataException>(() => CloudBatch(fixture.Transport, new[] { CloudFile() }).GetAwaiter().GetResult());
    }

    private static void CloudUploadFailuresDoNotVerify()
    {
        foreach (var failHttp in new[] { false, true })
        {
            using var fixture = new CloudUploadFixture { FailHttp = failHttp, RejectCommit = !failHttp };
            Throws<Exception>(() => CloudBatch(fixture.Transport, new[] { CloudFile() }).GetAwaiter().GetResult());
            Equal(0, fixture.MetadataCalls, "Failed uploads must not proceed to verification.");
            if (failHttp) Equal(0, fixture.SuccessCommits, "Failed HTTP must not commit success.");
            True(fixture.FailedCommits > 0, "A failed upload should retain the failed-commit notification.");
        }
    }

    private static void CloudUploadNormalizesBeforeAnyWrite()
    {
        using var fixture = new CloudUploadFixture();
        Throws<IOException>(() => CloudBatch(fixture.Transport, new[] { CloudFile("a\\b"), CloudFile("a/b") }).GetAwaiter().GetResult());
        Equal(0, fixture.Begins, "Duplicate canonical paths must fail before writes.");
        Throws<IOException>(() => CloudBatch(fixture.Transport, new[] { CloudFile(), CloudFile("../escape") }).GetAwaiter().GetResult());
        Equal(0, fixture.Begins, "All paths must be validated before writes.");
    }

    private static void CloudUploadUsesThreeBlockingWorkers()
    {
        using var fixture = new CloudUploadFixture();
        using var entered = new CountdownEvent(3);
        using var release = new ManualResetEventSlim();
        fixture.BlockHttp = () => { if (entered.CurrentCount > 0) entered.Signal(); if (!release.Wait(TimeSpan.FromSeconds(8))) throw new TimeoutException("Three synchronous HTTP workers did not overlap."); };
        var files = Enumerable.Range(0, 9).Select(i => CloudFile($"file-{i}")).ToArray();
        var batch = Task.Run(() => CloudBatch(fixture.Transport, files));
        try
        {
            True(entered.Wait(TimeSpan.FromSeconds(5)), "Three workers must reach blocking HTTP concurrently.");
            Equal(3, fixture.PeakHttp, "The shared HTTP peak should be three.");
        }
        finally { release.Set(); }
        Equal(9, batch.GetAwaiter().GetResult(), "All nine files must complete.");
        Equal(3, fixture.PeakHttp, "The worker pool must remain bounded.");
    }

    private static void CloudUploadCancellationDrainsBeforeDiscard()
    {
        using var fixture = new CloudUploadFixture();
        using var entered = new CountdownEvent(3);
        using var release = new ManualResetEventSlim();
        using var cancel = new CancellationTokenSource();
        fixture.BlockHttp = () => { entered.Signal(); release.Wait(TimeSpan.FromSeconds(8)); };
        var batch = Task.Run(() => CloudBatch(fixture.Transport, Enumerable.Range(0, 9).Select(i => CloudFile($"cancel-{i}")).ToArray(), cancel.Token));
        try
        {
            True(entered.Wait(TimeSpan.FromSeconds(5)), "Cancellation fixture needs three active workers.");
            cancel.Cancel();
            True(!batch.IsCompleted, "Cancellation must drain non-cooperative requests before returning.");
            True(!fixture.ConnectionDisposed, "The connection must remain owned until drain.");
        }
        finally { release.Set(); }
        Throws<OperationCanceledException>(() => batch.GetAwaiter().GetResult());
        Equal(3, fixture.Begins, "Cancellation must not schedule queued files.");
        Equal(0, fixture.SuccessCommits, "Cancellation must prevent subsequent commits.");
        True(!fixture.DisposedWithHttp, "Transport discard must follow HTTP worker exit.");
    }

    private static void CloudUploadPerFileTimeoutAndExclusiveBatch()
    {
        using var fixture = new CloudUploadFixture(TimeSpan.FromSeconds(2));
        fixture.BlockHttp = () => Thread.Sleep(500);
        var batch = Task.Run(() => CloudBatch(fixture.Transport, Enumerable.Range(0, 15).Select(i => CloudFile($"budget-{i}")).ToArray()));
        True(SpinWait.SpinUntil(() => Volatile.Read(ref fixture.ActiveHttp) > 0, TimeSpan.FromSeconds(5)), "Batch must begin.");
        var enumeration = fixture.Transport.EnumerateAsync(default);
        Equal(0, fixture.Enumerations, "Other transport operations cannot enter the upload batch.");
        Equal(15, batch.GetAwaiter().GetResult(), "The batch must outlive one timeout budget while every active file meets it.");
        enumeration.GetAwaiter().GetResult();
        Equal(1, fixture.Enumerations, "Queued enumeration should run after the batch.");
        using var timeout = new CloudUploadFixture(TimeSpan.FromMilliseconds(50));
        timeout.BlockHttp = () => Thread.Sleep(150);
        Throws<TimeoutException>(() => CloudBatch(timeout.Transport, new[] { CloudFile() }).GetAwaiter().GetResult());
        True(!timeout.DisposedWithHttp, "A file timeout must drain before discard.");
    }

    private static void CloudUploadBlocksPreserveOrderAndHeaders()
    {
        using var fixture = new CloudUploadFixture { BlockCount = 2, MetadataMode = "missing-hash" };
        Equal(1, CloudBatch(fixture.Transport, new[] { CloudFile() }).GetAwaiter().GetResult(), "Multi-block content must survive read-back hashing.");
        Equal(2, fixture.Puts, "Both blocks must be sent.");
        True(fixture.BlockOffsets.ToArray().SequenceEqual(new[] { 0, fixture.SecondOffset }), "Blocks must retain server ordering and offsets.");
        True(fixture.SawPost, "The server's POST method must be preserved.");
    }

    private static void CloudUploadFallbackKeepsRawZipPrefix()
    {
        using var fixture = new CloudUploadFixture { MetadataMode = "missing-hash-wire-size" };
        var bytes = new byte[4096];
        new Random(123).NextBytes(bytes);
        bytes[0] = 0x50; bytes[1] = 0x4b; bytes[2] = 0x03; bytes[3] = 0x04;
        Equal(1, CloudBatch(fixture.Transport, new[] { new SteamCloudTransport.UploadFile("raw.save", bytes, DateTimeOffset.UtcNow) }).GetAwaiter().GetResult(), "An uncompressed raw file with ZIP magic must not be decompressed when wire-size metadata is missing.");
    }

    private static void CloudUploadLateMetadataCannotComplete()
    {
        using var fixture = new CloudUploadFixture(TimeSpan.FromMilliseconds(30)) { MetadataDelayMs = 100 };
        Throws<TimeoutException>(() => CloudBatch(fixture.Transport, new[] { CloudFile() }).GetAwaiter().GetResult());
    }

    private static void CloudUploadCannotInventEmptyReadBack()
    {
        using var fixture = new CloudUploadFixture { MetadataMode = "missing-hash-size-url" };
        Throws<InvalidDataException>(() => CloudBatch(fixture.Transport, new[] { CloudFile(size: 0) }).GetAwaiter().GetResult());
        Equal(0, fixture.Gets, "Missing body metadata must fail rather than invent an empty response.");
        using var explicitEmpty = new CloudUploadFixture { MetadataMode = "missing-hash-no-url" };
        Equal(1, CloudBatch(explicitEmpty.Transport, new[] { CloudFile(size: 0) }).GetAwaiter().GetResult(), "An explicitly reported empty body remains valid without a hash or body URL.");
        Equal(0, explicitEmpty.Gets, "An explicitly empty body does not require a URL.");
    }

    private static void CloudUploadFailureKeepsRootAndDrains()
    {
        using var fixture = new CloudUploadFixture();
        using var entered = new CountdownEvent(3);
        using var release = new ManualResetEventSlim();
        var order = 0;
        fixture.BlockHttp = () =>
        {
            var mine = Interlocked.Increment(ref order);
            entered.Signal();
            if (!entered.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Workers did not overlap.");
            if (mine == 1) throw new IOException("original-upload-failure");
            release.Wait(TimeSpan.FromSeconds(8));
        };
        var batch = Task.Run(() => CloudBatch(fixture.Transport, Enumerable.Range(0, 9).Select(i => CloudFile($"failure-{i}")).ToArray()));
        try
        {
            True(entered.Wait(TimeSpan.FromSeconds(5)), "Failure fixture needs active siblings.");
            True(SpinWait.SpinUntil(() => Volatile.Read(ref fixture.FailedCommits) > 0, TimeSpan.FromSeconds(3)), "The root failure must reach abort handling.");
            True(!batch.IsCompleted && !fixture.ConnectionDisposed, "Failure cannot return or dispose before sibling drain.");
        }
        finally { release.Set(); }
        var error = Throws<IOException>(() => batch.GetAwaiter().GetResult());
        Equal("original-upload-failure", error.Message, "Sibling cancellations must not replace the root failure.");
        Equal(3, fixture.Begins, "No queued file may start after failure.");
        True(!fixture.DisposedWithHttp, "Failure must drain HTTP before discard.");
    }

    private static void CloudUploadStopsBeforeSlowFailureNotice()
    {
        using var fixture = new CloudUploadFixture();
        using var entered = new CountdownEvent(3);
        using var releaseHttp = new ManualResetEventSlim();
        using var noticeEntered = new ManualResetEventSlim();
        using var releaseNotice = new ManualResetEventSlim();
        var order = 0;
        fixture.BlockHttp = () =>
        {
            var mine = Interlocked.Increment(ref order);
            if (mine <= 3) entered.Signal();
            if (!entered.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Workers did not overlap.");
            if (mine == 1) throw new IOException("upload-failed-before-notice");
            if (mine <= 3) releaseHttp.Wait(TimeSpan.FromSeconds(8));
        };
        fixture.BlockFailedNotice = () => { noticeEntered.Set(); releaseNotice.Wait(TimeSpan.FromSeconds(8)); };
        var batch = Task.Run(() => CloudBatch(fixture.Transport, Enumerable.Range(0, 9).Select(i => CloudFile($"notice-{i}")).ToArray()));
        try
        {
            True(noticeEntered.Wait(TimeSpan.FromSeconds(5)), "Failure notification must be entered.");
            releaseHttp.Set();
            SpinWait.SpinUntil(() => Volatile.Read(ref fixture.Begins) > 3, TimeSpan.FromMilliseconds(400));
            Equal(3, fixture.Begins, "A slow failed-upload notification must not allow more files to start.");
            True(!batch.IsCompleted && !fixture.ConnectionDisposed, "Failed-upload notification remains inside the owned lifetime.");
        }
        finally { releaseHttp.Set(); releaseNotice.Set(); }
        var error = Throws<IOException>(() => batch.GetAwaiter().GetResult());
        Equal("upload-failed-before-notice", error.Message, "Failure notification must retain the original failure.");
    }

    private sealed class CloudUploadFixture : ICloudMessageClient, IDisposable
    {
        private readonly ConcurrentDictionary<string, CCloud_ClientBeginFileUpload_Request> _uploads = new();
        private readonly ConcurrentDictionary<string, byte[]> _wire = new();
        private readonly object _blockGate = new();
        internal readonly SteamCloudTransport Transport;
        internal string MetadataMode = "valid";
        internal bool Duplicate, RejectCommit, FailHttp, CorruptReadBack;
        internal int Begins, MetadataCalls, Gets, Puts, SuccessCommits, FailedCommits, ActiveHttp, PeakHttp, Enumerations;
        internal int BlockCount = 1, SecondOffset, MetadataDelayMs;
        internal readonly ConcurrentQueue<int> BlockOffsets = new();
        internal bool SawPost, DisposedWithHttp;
        internal Action? BlockHttp, BlockFailedNotice;
        internal bool ConnectionDisposed;
        internal CloudUploadFixture(TimeSpan? timeout = null)
            => Transport = new SteamCloudTransport(this, new HttpClient(new CloudHandler(this)), timeout ?? TimeSpan.FromSeconds(10));

        public void BeginOperation() { }
        public void EndOperation() { }
        void IDisposable.Dispose() { DisposedWithHttp |= ActiveHttp != 0; ConnectionDisposed = true; }
        public void Dispose() => Transport.Dispose();

        public Task<TResult> SendCloudAsync<TRequest, TResult>(string method, TRequest request, CancellationToken token)
            where TRequest : ProtoBuf.IExtensible, new()
            where TResult : ProtoBuf.IExtensible, new()
        {
            token.ThrowIfCancellationRequested();
            object result;
            if (request is CCloud_ClientBeginFileUpload_Request begin)
            {
                Interlocked.Increment(ref Begins);
                _uploads[begin.filename] = begin;
                if (Duplicate) throw new InvalidOperationException("Cloud.ClientBeginFileUpload failed: DuplicateRequest");
                var response = new CCloud_ClientBeginFileUpload_Response();
                for (var i = 0; i < BlockCount; i++)
                {
                    var offset = i == 0 ? 0 : (int)begin.file_size / 2;
                    var length = BlockCount == 1 ? (int)begin.file_size : i == 0 ? (int)begin.file_size / 2 : (int)begin.file_size - offset;
                    var block = new ClientCloudFileUploadBlockDetails { url_host = "cloud.invalid", url_path = "/" + begin.filename, use_https = true, http_method = i == 1 ? 2 : 1, block_offset = (ulong)offset, block_length = (uint)length };
                    block.request_headers.Add(new ClientCloudFileUploadBlockDetails.HTTPHeaders { name = "X-Test-Offset", value = offset.ToString() });
                    response.block_requests.Add(block);
                    if (i == 1) SecondOffset = offset;
                }
                result = response;
            }
            else if (request is CCloud_ClientCommitFileUpload_Request commit)
            {
                if (commit.transfer_succeeded) Interlocked.Increment(ref SuccessCommits); else Interlocked.Increment(ref FailedCommits);
                if (!commit.transfer_succeeded) BlockFailedNotice?.Invoke();
                result = new CCloud_ClientCommitFileUpload_Response { file_committed = !RejectCommit };
            }
            else if (request is CCloud_ClientFileDownload_Request download)
            {
                Interlocked.Increment(ref MetadataCalls);
                if (MetadataDelayMs > 0) Thread.Sleep(MetadataDelayMs);
                var original = _uploads[download.filename];
                var response = new CCloud_ClientFileDownload_Response { appid = MetadataMode == "wrong-app" ? 0u : original.appid, raw_file_size = original.raw_file_size, file_size = original.file_size, sha_file = original.file_sha.ToArray(), url_host = "cloud.invalid", url_path = "/" + download.filename, use_https = true, is_explicit_delete = MetadataMode == "deleted" };
                if (MetadataMode.Contains("missing-hash")) response.sha_file = null;
                if (MetadataMode == "bad-hash") response.sha_file = new byte[3];
                if (MetadataMode == "missing-size") response.Resetraw_file_size();
                if (MetadataMode == "missing-hash-size-url") { response.Resetraw_file_size(); response.url_host = ""; }
                if (MetadataMode == "missing-hash-no-url") response.url_host = "";
                if (MetadataMode == "missing-hash-wire-size") response.Resetfile_size();
                if (MetadataMode.Contains("wrong-size")) response.raw_file_size++;
                if (MetadataMode == "wrong-hash") response.sha_file![0] ^= 255;
                result = response;
            }
            else if (request is CCloud_EnumerateUserFiles_Request)
            {
                Interlocked.Increment(ref Enumerations);
                result = new CCloud_EnumerateUserFiles_Response();
            }
            else throw new NotSupportedException(method);
            return Task.FromResult((TResult)result);
        }

        private sealed class CloudHandler(CloudUploadFixture owner) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                var active = Interlocked.Increment(ref owner.ActiveHttp);
                lock (owner._blockGate) owner.PeakHttp = Math.Max(owner.PeakHttp, active);
                try
                {
                    if (owner.ConnectionDisposed) throw new ObjectDisposedException("Cloud connection");
                    var path = request.RequestUri!.AbsolutePath.TrimStart('/');
                    if (request.Method == HttpMethod.Get)
                    {
                        Interlocked.Increment(ref owner.Gets);
                        var bytes = owner._wire[path].ToArray();
                        if (owner.CorruptReadBack) bytes = new byte[] { 1, 2, 3 };
                        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) });
                    }
                    Interlocked.Increment(ref owner.Puts);
                    owner.BlockHttp?.Invoke();
                    token.ThrowIfCancellationRequested();
                    var offset = int.Parse(request.Headers.GetValues("X-Test-Offset").Single());
                    owner.BlockOffsets.Enqueue(offset);
                    owner.SawPost |= request.Method == HttpMethod.Post;
                    var body = request.Content!.ReadAsByteArrayAsync(token).GetAwaiter().GetResult();
                    var wire = owner._wire.GetOrAdd(path, _ => new byte[owner._uploads[path].file_size]);
                    Buffer.BlockCopy(body, 0, wire, offset, body.Length);
                    return Task.FromResult(new HttpResponseMessage(owner.FailHttp ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK));
                }
                finally { Interlocked.Decrement(ref owner.ActiveHttp); }
            }
        }
    }
}
