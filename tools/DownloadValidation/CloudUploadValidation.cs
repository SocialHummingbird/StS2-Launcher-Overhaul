using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2.Internal;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

// Compiled only by DownloadValidation.targets. Normal startup never reaches this harness.
internal static class CloudUploadValidation
{
    private sealed record PreservedFile(string Path, long Size, string Sha256, string MetadataHash, long ModifiedAt);
    private sealed class Phase
    {
        public string Name { get; set; }
        public int Begins, Commits, Duplicates, Metadata, Puts, Gets, ActiveHttp, PeakHttp;
        public long UploadPayloadBytes, DownloadPayloadBytes, ElapsedMs;
        public int? Uploaded;
    }

    internal static async Task RunAsync(SteamCredentialStore credentials, string run, string output, Action<string> status, bool cleanupOnly)
    {
        var privateDir = Path.Combine(run, "cloud-private");
        Directory.CreateDirectory(privateDir);
        var ledgerPath = Path.Combine(privateDir, "owned-files.json");
        var baselinePath = Path.Combine(privateDir, "before.json");
        var ledger = File.Exists(ledgerPath) ? JsonSerializer.Deserialize<string[]>(File.ReadAllText(ledgerPath)) : null;
        if (ledger == null)
        {
            if (cleanupOnly) throw new IOException("No owned-file ledger exists.");
            var prefix = "sts2-launcher-validation/" + Guid.NewGuid().ToString("N") + "/";
            ledger = Enumerable.Range(0, 9).Select(i => prefix + $"upload-{i:D2}.bin").ToArray();
            DownloadValidationRunner.Write(ledgerPath, ledger);
        }
        if (ledger.Length != 9 || ledger.Distinct(StringComparer.Ordinal).Count() != 9
            || ledger.Any(path => !System.Text.RegularExpressions.Regex.IsMatch(path, "^sts2-launcher-validation/[a-f0-9]{32}/upload-0[0-8]\\.bin$"))
            || ledger.Select(path => path.Split('/')[1]).Distinct().Count() != 1)
            throw new IOException("Invalid owned Cloud ledger.");
        DownloadValidationRunner.Write(Path.Combine(output, "cloud-owned-files.json"), ledger);
        var allowed = new HashSet<string>(ledger, StringComparer.Ordinal);
        var phases = new List<Phase>();
        var report = new Dictionary<string, object> { ["status"] = "running", ["existingCloudWrites"] = 0, ["ownedFiles"] = ledger.Length };
        var events = Path.Combine(output, "cloud-events.jsonl");
        var logGate = new object();
        void Event(object entry) { lock (logGate) File.AppendAllText(events, JsonSerializer.Serialize(entry, new JsonSerializerOptions { IncludeFields = true }) + "\n"); }
        void Save() => DownloadValidationRunner.Write(Path.Combine(output, "cloud-report.json"), report);
        var writesAllowed = File.Exists(Path.Combine(privateDir, "backup-complete"));
        string account = null, token = null;
        if (!credentials.TryUseCredentials((name, refresh) => { account = name; token = refresh; })) throw new IOException("Usable credentials unavailable.");
        ValidationJava.Call("configureDownloadValidationHttp", Path.GetFileName(run));
        Session Open() => new(account, token, allowed, () => writesAllowed, Event);
        using var initial = Open();
        report["phases"] = phases;
        Save();
        Exception failure = null;
        try
        {
            if (!cleanupOnly)
            {
                if (File.Exists(Path.Combine(privateDir, "writes-started"))) throw new IOException("Use cleanup-only after an interrupted campaign; do not overwrite its baseline.");
                status("Backing up existing Steam Cloud files read-only…");
                initial.SetPhase("existing-backup", phases);
                var before = await SnapshotAsync(initial.Transport, allowed, privateDir, true).ConfigureAwait(false);
                DownloadValidationRunner.Write(baselinePath, before);
                File.WriteAllText(Path.Combine(privateDir, "backup-complete"), "Existing Cloud bytes and metadata captured before all mutations.");
                writesAllowed = true;
                var files = MakeFiles(ledger);
                File.WriteAllText(Path.Combine(privateDir, "writes-started"), DateTimeOffset.UtcNow.ToString("O"));

                status("Testing isolated Cloud upload batch…");
                await UploadAndAuditAsync(initial, "fresh", files, phases).ConfigureAwait(false);
                await UploadAndAuditAsync(initial, "unchanged", files, phases).ConfigureAwait(false);
                var replacements = files.Take(3).Select(file => file with { Bytes = Changed(file.Bytes) }).ToArray();
                await UploadAndAuditAsync(initial, "replacement", replacements, phases).ConfigureAwait(false);

                if (phases.Where(phase => phase.Name is "fresh" or "replacement").Max(phase => phase.PeakHttp) < 3)
                {
                    initial.Http.GateFirstThree = true;
                    await UploadAndAuditAsync(initial, "gated-parallelism", replacements.Select(file => file with { Bytes = Changed(file.Bytes) }).ToArray(), phases).ConfigureAwait(false);
                    initial.Http.GateFirstThree = false;
                    if (initial.LastUploadPeak != 3) throw new IOException("Three gated upload workers did not overlap.");
                }

                initial.SetPhase("cancel-after-commit", phases);
                using var cancel = new CancellationTokenSource();
                initial.Messages.CancelAfterCommit = cancel;
                var retryFiles = files.Select(file => file with { Bytes = Changed(file.Bytes) }).ToArray();
                try
                {
                    await initial.Transport.UploadBatchAsync(retryFiles, cancel.Token).ConfigureAwait(false);
                    throw new IOException("Requested upload cancellation incorrectly reported success.");
                }
                catch (OperationCanceledException) when (cancel.IsCancellationRequested) { report["cancellationObserved"] = true; }
                initial.Messages.CancelAfterCommit = null;
                await WaitForJavaDrainAsync(output, "after-cancel").ConfigureAwait(false);
                using var retry = Open();
                await UploadAndAuditAsync(retry, "retry", retryFiles, phases).ConfigureAwait(false);
            }
            report["status"] = "passed";
        }
        catch (Exception error)
        {
            failure = error;
            report["status"] = "failed";
            report["error"] = DownloadValidationRunner.SafeError(error);
        }
        finally
        {
            try
            {
                await WaitForJavaDrainAsync(output, "before-cleanup").ConfigureAwait(false);
                if (writesAllowed)
                {
                    using var cleanup = Open();
                    cleanup.SetPhase("existing-after", phases);
                    var after = await SnapshotAsync(cleanup.Transport, allowed, privateDir, false).ConfigureAwait(false);
                    DownloadValidationRunner.Write(Path.Combine(privateDir, "after.json"), after);
                    var before = JsonSerializer.Deserialize<PreservedFile[]>(File.ReadAllText(baselinePath));
                    var unchanged = before.SequenceEqual(after);
                    report["existingCloudFiles"] = before.Length;
                    report["existingCloudUnchanged"] = unchanged;
                    cleanup.SetPhase("owned-cleanup", phases);
                    foreach (var path in ledger) await cleanup.Transport.DeleteAsync(path, CancellationToken.None).ConfigureAwait(false);
                    report["ownedCleanupVerified"] = true;
                    if (!unchanged) throw new IOException("Existing Cloud data changed; no restoration or overwrite was attempted.");
                }
                report["javaDrained"] = true;
            }
            catch (Exception error)
            {
                report["status"] = "failed";
                report["cleanupError"] = DownloadValidationRunner.SafeError(error);
                failure ??= error;
            }
            Save();
        }
        if (failure != null) throw new IOException("Cloud validation failed; inspect the retained report and owned-file ledger.", failure);
    }

    private static async Task UploadAndAuditAsync(Session session, string name, IReadOnlyList<SteamCloudTransport.UploadFile> files, List<Phase> phases)
    {
        var phase = session.SetPhase(name, phases);
        var watch = Stopwatch.StartNew();
        phase.Uploaded = await session.Transport.UploadBatchAsync(files, default).ConfigureAwait(false);
        phase.ElapsedMs = watch.ElapsedMilliseconds;
        session.LastUploadPeak = phase.PeakHttp;
        if (phase.Gets != 0) throw new IOException("Live metadata fast-path unavailable: verification required body downloads.");
        if (phase.PeakHttp > 3) throw new IOException("HTTP upload concurrency exceeded three.");
        session.SetPhase(name + "-audit", phases);
        foreach (var file in files)
        {
            var actual = await session.Transport.DownloadBytesAsync(new(file.Path, file.Bytes.LongLength, file.ModifiedAt, ""), default).ConfigureAwait(false);
            if (!SHA256.HashData(actual).SequenceEqual(SHA256.HashData(file.Bytes))) throw new IOException("Owned Cloud content audit failed.");
        }
    }

    private static SteamCloudTransport.UploadFile[] MakeFiles(string[] paths)
        => paths.Select((path, index) =>
        {
            var bytes = new byte[index == 8 ? 0 : 1024 * 1024];
            if (index < 6) new Random(0x7320 + index).NextBytes(bytes);
            else Array.Fill(bytes, (byte)(40 + index));
            return new SteamCloudTransport.UploadFile(path, bytes, DateTimeOffset.UtcNow);
        }).ToArray();

    private static byte[] Changed(byte[] bytes)
    {
        var result = bytes.Length == 0 ? new byte[32] : bytes.ToArray();
        result[0] ^= 127;
        return result;
    }

    private static async Task<PreservedFile[]> SnapshotAsync(SteamCloudTransport transport, HashSet<string> owned, string privateDir, bool backup)
    {
        var files = await transport.EnumerateAsync(default).ConfigureAwait(false);
        if (backup && files.Any(file => owned.Contains(file.Path))) throw new IOException("Owned namespace already exists; refusing to overwrite it.");
        var records = new List<PreservedFile>();
        foreach (var file in files.Where(file => !owned.Contains(file.Path)).OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            var bytes = await transport.DownloadBytesAsync(file, default).ConfigureAwait(false);
            var sha = Convert.ToHexString(SHA256.HashData(bytes));
            if (backup) File.WriteAllBytes(Path.Combine(privateDir, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(file.Path))) + ".bin"), bytes);
            records.Add(new(file.Path, bytes.LongLength, sha, file.ContentHash, file.ModifiedAt.ToUnixTimeSeconds()));
        }
        return records.ToArray();
    }

    private static async Task WaitForJavaDrainAsync(string output, string name)
    {
        var deadline = Environment.TickCount64 + 130_000;
        while (true)
        {
            var json = ValidationJava.Call("getDownloadValidationHttpState");
            File.WriteAllText(Path.Combine(output, "java-cloud-" + name + ".json"), json);
            using var state = JsonDocument.Parse(json);
            if (state.RootElement.GetProperty("activeJavaWorkers").GetInt32() == 0) return;
            if (Environment.TickCount64 > deadline) throw new TimeoutException("Java HTTP workers did not exit within the existing request deadline and completion grace.");
            await Task.Delay(250).ConfigureAwait(false);
        }
    }

    private sealed class Session : IDisposable
    {
        internal readonly Messages Messages;
        internal readonly TrackingHttp Http;
        internal readonly SteamCloudTransport Transport;
        internal int LastUploadPeak;
        internal Session(string account, string token, HashSet<string> owned, Func<bool> mayWrite, Action<object> log)
        {
            Messages = new Messages(new SteamConnection(account, token, 90_000), owned, mayWrite, log);
            Http = new TrackingHttp(log);
            Transport = new SteamCloudTransport(Messages, AndroidJavaHttpMessageHandler.CreateCloudValidationClient(Http), TimeSpan.FromMinutes(3));
        }
        internal Phase SetPhase(string name, List<Phase> phases)
        {
            var phase = new Phase { Name = name };
            phases.Add(phase); Messages.Phase = phase; Http.Phase = phase;
            return phase;
        }
        public void Dispose() => Transport.Dispose();
    }

    private sealed class Messages(SteamConnection connection, HashSet<string> owned, Func<bool> mayWrite, Action<object> log) : ICloudMessageClient
    {
        internal Phase Phase;
        internal CancellationTokenSource CancelAfterCommit;
        public void BeginOperation() => connection.SuspendIdleTimeout();
        public void EndOperation() => connection.ResumeIdleTimeout();
        public void Dispose() => connection.Dispose();
        public async Task<TResult> SendCloudAsync<TRequest, TResult>(string method, TRequest request, CancellationToken cancellationToken)
            where TRequest : ProtoBuf.IExtensible, new() where TResult : ProtoBuf.IExtensible, new()
        {
            var mutatingPath = request switch
            {
                CCloud_ClientBeginFileUpload_Request begin => begin.filename,
                CCloud_ClientCommitFileUpload_Request commit => commit.filename,
                CCloud_ClientDeleteFile_Request delete => delete.filename,
                _ => null,
            };
            if (mutatingPath != null && (!mayWrite() || !owned.Contains(mutatingPath))) throw new IOException("Cloud mutation escaped the backed-up test ledger.");
            if (mutatingPath == null && method is not "EnumerateUserFiles" and not "ClientFileDownload") throw new IOException("Unexpected Cloud operation.");
            if (request is CCloud_ClientBeginFileUpload_Request) Interlocked.Increment(ref Phase.Begins);
            if (request is CCloud_ClientFileDownload_Request) Interlocked.Increment(ref Phase.Metadata);
            var watch = Stopwatch.StartNew();
            try
            {
                var response = await connection.SendCloudAsync<TRequest, TResult>(method, request, cancellationToken).ConfigureAwait(false);
                if (request is CCloud_ClientCommitFileUpload_Request commit && commit.transfer_succeeded && response is CCloud_ClientCommitFileUpload_Response { file_committed: true })
                {
                    Interlocked.Increment(ref Phase.Commits);
                    CancelAfterCommit?.Cancel();
                }
                log(new { phase = Phase.Name, kind = "rpc", method, elapsedMs = watch.ElapsedMilliseconds, success = true });
                return response;
            }
            catch (Exception error)
            {
                if (error.Message.Contains("DuplicateRequest", StringComparison.Ordinal)) Interlocked.Increment(ref Phase.Duplicates);
                log(new { phase = Phase.Name, kind = "rpc", method, elapsedMs = watch.ElapsedMilliseconds, success = false, error = DownloadValidationRunner.SafeError(error) });
                throw;
            }
        }
    }

    private sealed class TrackingHttp(Action<object> log) : DelegatingHandler
    {
        internal Phase Phase;
        internal bool GateFirstThree;
        private int _gated;
        private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var phase = Phase;
            var isGet = request.Method == HttpMethod.Get;
            if (isGet) Interlocked.Increment(ref phase.Gets); else Interlocked.Increment(ref phase.Puts);
            if (GateFirstThree && !isGet)
            {
                if (Interlocked.Increment(ref _gated) == 3) _gate.TrySetResult();
                await _gate.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
            }
            var active = Interlocked.Increment(ref phase.ActiveHttp);
            lock (phase) phase.PeakHttp = Math.Max(phase.PeakHttp, active);
            var watch = Stopwatch.StartNew();
            try
            {
                var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (isGet) Interlocked.Add(ref phase.DownloadPayloadBytes, response.Content.Headers.ContentLength ?? 0);
                else if (response.IsSuccessStatusCode) Interlocked.Add(ref phase.UploadPayloadBytes, request.Content?.Headers.ContentLength ?? 0);
                log(new { phase = phase.Name, kind = "http", method = request.Method.Method, status = (int)response.StatusCode, elapsedMs = watch.ElapsedMilliseconds });
                return response;
            }
            finally { Interlocked.Decrement(ref phase.ActiveHttp); }
        }
    }
}
