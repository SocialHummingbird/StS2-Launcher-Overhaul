using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using SteamKit2;

namespace STS2Mobile.Steam;

internal readonly record struct DepotChunkTransferProgress(long TotalContentBytes, long ReusedBytes, long ResumedBytes, long PlannedDownloadBytes, long DownloadedBytes, string Phase);

// Reconstructs raw Steam files. Android preparation and ready publication remain
// the install transaction's responsibility, after every replacement is verified.
internal sealed partial class DepotChunkTransfer
{
    private readonly string _installRoot;
    private readonly string _stateRoot;
    private readonly uint _depotId;
    private readonly ulong _manifestId;
    private readonly DepotManifest _manifest;
    private readonly IReadOnlyList<DepotChunkDonor> _donors;
    private readonly int _workers;
    private readonly Func<DepotManifest.ChunkData, byte[], CancellationToken, Task<int>> _fetchVerified;
    private readonly Action<DepotChunkTransferProgress> _report;
    private readonly Func<DepotManifest.FileData, bool> _keepPreparedFile;
    private readonly Action<long> _ensureSpace;
    private readonly Action _beforePromote;
    private readonly object _progressLock = new();
    private long _total, _reused, _resumed, _planned, _downloaded;
    private string _phase = "Checking local data";

    internal DepotChunkTransfer(string installRoot, string stateRoot, uint depotId, ulong manifestId,
        DepotManifest manifest, IReadOnlyList<DepotChunkDonor> donors, int workers,
        Func<DepotManifest.ChunkData, byte[], CancellationToken, Task<int>> fetchVerified,
        Action<DepotChunkTransferProgress> report = null, Func<DepotManifest.FileData, bool> keepPreparedFile = null,
        Action<long> ensureSpace = null, Action beforePromote = null)
    {
        _installRoot = installRoot;
        _stateRoot = stateRoot;
        _depotId = depotId;
        _manifestId = manifestId;
        _manifest = manifest;
        _donors = donors;
        _workers = Math.Max(1, workers);
        _fetchVerified = fetchVerified ?? throw new ArgumentNullException(nameof(fetchVerified));
        _report = report;
        _keepPreparedFile = keepPreparedFile;
        _ensureSpace = ensureSpace;
        _beforePromote = beforePromote;
    }

    internal async Task RunAsync(CancellationToken ct)
    {
        var generation = DepotDownloadStaging.GenerationRoot(_stateRoot, _depotId, _manifestId);
        var targets = new List<FileTarget>();
        Exception operationError = null;
        try
        {
            var work = BuildTargets(generation, targets, ct);
            await DepotDownloadStaging.SaveManifestAsync(generation, _manifest, ct).ConfigureAwait(false);
            Report();
            var existingBytes = CheckStagedRanges(work, ct);
            _ensureSpace?.Invoke(Math.Max(0, targets.Sum(file => checked((long)file.Manifest.TotalSize)) - existingBytes));
            foreach (var file in targets) file.Open();
            var sources = BuildSources();
            var pending = ReuseLocalContent(work, sources, ct);
            // Local copies must be durable before superseded sources can ever be retired.
            foreach (var file in targets) file.Flush();
            RetireSupersededPartials();
            lock (_progressLock) { _phase = "Downloading"; _planned = pending.Sum(item => (long)item.Chunk.CompressedLength); }
            Report();
            await DownloadAsync(pending, ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            lock (_progressLock) _phase = "Verifying files";
            Report();
            foreach (var file in targets)
            {
                ct.ThrowIfCancellationRequested();
                file.Close();
                if (!DepotDownloader.VerifyFileHash(file.StagePath, file.Manifest))
                    throw new IOException($"Steam file SHA-1 verification failed: {file.Manifest.FileName}");
            }
            // Verify the entire batch before changing any installed file.
            foreach (var directory in _manifest.Files.Where(file => file.Flags.HasFlag(EDepotFileFlag.Directory)))
            {
                ct.ThrowIfCancellationRequested();
                _beforePromote?.Invoke();
                Directory.CreateDirectory(DepotDownloadStaging.Resolve(_installRoot, directory.FileName));
            }
            foreach (var file in targets)
            {
                ct.ThrowIfCancellationRequested();
                _beforePromote?.Invoke();
                Directory.CreateDirectory(Path.GetDirectoryName(file.FinalPath)!);
                File.Move(file.StagePath, file.FinalPath, overwrite: true);
            }
            Report();
        }
        catch (Exception ex) { operationError = ex; throw; }
        finally
        {
            // Completed ranges survive errors and cancellation. A later run verifies
            // the actual bytes again; preallocated length is never completion evidence.
            List<Exception> closeErrors = null;
            foreach (var file in targets)
            {
                try { file.Dispose(); }
                catch (IOException ex) { (closeErrors ??= new()).Add(ex); }
            }
            if (closeErrors != null)
            {
                if (operationError != null) closeErrors.Insert(0, operationError);
                throw new IOException("Could not flush download staging; saved ranges will be reverified on resume.", new AggregateException(closeErrors));
            }
        }
    }

    private List<ChunkWork> BuildTargets(string generation, List<FileTarget> targets, CancellationToken ct)
    {
        if (_manifest.Files == null || _manifest.FilenamesEncrypted || _manifest.DepotID != _depotId || _manifest.ManifestGID != _manifestId)
            throw new IOException("The target manifest does not match this depot download.");
        var uniquePaths = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        var chunks = new Dictionary<ChunkKey, ChunkWork>();
        foreach (var file in _manifest.Files)
        {
            ct.ThrowIfCancellationRequested();
            var finalPath = DepotDownloadStaging.Resolve(_installRoot, file.FileName);
            if (!uniquePaths.Add(finalPath)) throw new IOException("Duplicate depot target path.");
            if (file.Flags.HasFlag(EDepotFileFlag.Directory)) continue;
            if (file.TotalSize > 32UL * 1024 * 1024 * 1024) throw new IOException("Depot file exceeds the supported size.");
            _total = checked(_total + (long)file.TotalSize);
            var prior = _donors.Where(donor => donor.Selected && donor.Manifest.Files != null)
                .SelectMany(donor => donor.Manifest.Files)
                .FirstOrDefault(old => string.Equals(DepotDownloader.NormalizeManifestPath(old.FileName), DepotDownloader.NormalizeManifestPath(file.FileName), StringComparison.Ordinal));
            var knownChanged = prior != null && (prior.TotalSize != file.TotalSize || !prior.FileHash.SequenceEqual(file.FileHash));
            if (_keepPreparedFile?.Invoke(file) == true || !knownChanged && DepotDownloader.VerifyFileHash(finalPath, file))
            {
                _reused += checked((long)file.TotalSize);
                continue;
            }
            var path = DepotDownloadStaging.Resolve(Path.Combine(generation, "files"), file.FileName) + ".downloading";
            var target = new FileTarget(file, path, finalPath);
            targets.Add(target);
            ulong end = 0;
            foreach (var chunk in file.Chunks.OrderBy(chunk => chunk.Offset))
            {
                if (file.TotalSize == 0 && chunk.UncompressedLength == 0) continue;
                if (chunk.Offset != end || chunk.UncompressedLength == 0 || chunk.UncompressedLength > 64L * 1024 * 1024
                    || chunk.Offset > file.TotalSize || chunk.UncompressedLength > file.TotalSize - chunk.Offset
                    || chunk.ChunkID?.Length != 20)
                    throw new IOException("Depot chunks have invalid bounds or content identities.");
                end = checked(end + chunk.UncompressedLength);
                var key = Key(chunk);
                if (!chunks.TryGetValue(key, out var item)) chunks.Add(key, item = new ChunkWork(chunk));
                item.Destinations.Add(new Destination(target, chunk));
            }
            if (end != file.TotalSize) throw new IOException("Depot chunks do not cover the target file.");
        }
        return chunks.Values.ToList();
    }

    private long CheckStagedRanges(List<ChunkWork> work, CancellationToken ct)
    {
        long existing = 0;
        using var reader = new VerifiedChunkReader();
        foreach (var item in work)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(checked((int)item.Chunk.UncompressedLength));
            try
            {
                foreach (var destination in item.Destinations)
                {
                    ct.ThrowIfCancellationRequested();
                    if (!reader.TryRead(destination.File.StagePath, destination.Chunk, buffer)) continue;
                    destination.Valid = true;
                    existing += destination.Chunk.UncompressedLength;
                    _resumed += destination.Chunk.UncompressedLength;
                    item.StagedSource ??= new Source(destination.File.StagePath, destination.Chunk);
                }
            }
            finally { ArrayPool<byte>.Shared.Return(buffer); }
        }
        return existing;
    }

    private async Task DownloadAsync(List<ChunkWork> pending, CancellationToken ct)
    {
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var queue = Channel.CreateBounded<ChunkWork>(new BoundedChannelOptions(Math.Max(1, _workers * 2)) { SingleWriter = true, FullMode = BoundedChannelFullMode.Wait });
        ExceptionDispatchInfo failure = null;
        async Task Produce()
        {
            try
            {
                foreach (var item in pending) await queue.Writer.WriteAsync(item, stop.Token).ConfigureAwait(false);
                queue.Writer.TryComplete();
            }
            catch (Exception ex) { queue.Writer.TryComplete(ex); throw; }
        }
        async Task Worker()
        {
            try
            {
                await foreach (var item in queue.Reader.ReadAllAsync(stop.Token).ConfigureAwait(false))
                {
                    stop.Token.ThrowIfCancellationRequested();
                    var buffer = ArrayPool<byte>.Shared.Rent(checked((int)item.Chunk.UncompressedLength));
                    try
                    {
                        // The production source verifies SHA-1 inside the existing
                        // CDN retry loop. Avoid hashing every network chunk twice.
                        var written = await _fetchVerified(item.Chunk, buffer, stop.Token).ConfigureAwait(false);
                        if (written != item.Chunk.UncompressedLength)
                            throw new IOException("Downloaded depot chunk length did not match its manifest.");
                        // A completed request is committed to staging even if Pause was
                        // requested meanwhile. Scheduling stops at the next boundary.
                        await WriteDestinations(item, buffer).ConfigureAwait(false);
                        lock (_progressLock) _downloaded += item.Chunk.CompressedLength;
                        Report();
                    }
                    finally { ArrayPool<byte>.Shared.Return(buffer); }
                }
            }
            catch (Exception ex)
            {
                if (ex is not OperationCanceledException) Interlocked.CompareExchange(ref failure, ExceptionDispatchInfo.Capture(ex), null);
                stop.Cancel();
                throw;
            }
        }
        var tasks = Enumerable.Range(0, Math.Min(_workers, pending.Count)).Select(_ => Task.Run(Worker, CancellationToken.None)).Append(Produce()).ToArray();
        try { await Task.WhenAll(tasks).ConfigureAwait(false); }
        catch { failure?.Throw(); throw; }
    }

    private async Task WriteDestinations(ChunkWork item, byte[] buffer)
    {
        foreach (var destination in item.Destinations)
        {
            if (destination.Valid) continue;
            await destination.File.Gate.WaitAsync().ConfigureAwait(false);
            try
            {
                var stream = destination.File.Stream!;
                stream.Position = checked((long)destination.Chunk.Offset);
                await stream.WriteAsync(buffer.AsMemory(0, checked((int)destination.Chunk.UncompressedLength))).ConfigureAwait(false);
                destination.Valid = true;
            }
            finally { destination.File.Gate.Release(); }
        }
    }

    private void Report()
    {
        DepotChunkTransferProgress snapshot;
        lock (_progressLock) snapshot = new(_total, _reused, _resumed, _planned, _downloaded, _phase);
        _report?.Invoke(snapshot);
    }

    private ChunkKey Key(DepotManifest.ChunkData chunk) => new(_depotId, Convert.ToHexString(chunk.ChunkID!), chunk.UncompressedLength);
    private readonly record struct ChunkKey(uint Depot, string Sha1, uint Length);
    private sealed record Source(string Path, DepotManifest.ChunkData Chunk);
    private sealed class ChunkWork(DepotManifest.ChunkData chunk)
    {
        internal DepotManifest.ChunkData Chunk { get; } = chunk;
        internal List<Destination> Destinations { get; } = new();
        internal Source StagedSource;
    }
    private sealed class Destination(FileTarget file, DepotManifest.ChunkData chunk)
    {
        internal FileTarget File { get; } = file;
        internal DepotManifest.ChunkData Chunk { get; } = chunk;
        internal bool Valid;
    }
    private sealed class FileTarget(DepotManifest.FileData manifest, string stagePath, string finalPath) : IDisposable
    {
        internal DepotManifest.FileData Manifest { get; } = manifest;
        internal string StagePath { get; } = stagePath;
        internal string FinalPath { get; } = finalPath;
        internal SemaphoreSlim Gate { get; } = new(1, 1);
        internal FileStream Stream;
        internal void Open()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StagePath)!);
            // An acknowledged small chunk must reach the OS rather than remain
            // solely in FileStream's managed buffer when the process is killed.
            Stream = new FileStream(StagePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, bufferSize: 1, FileOptions.Asynchronous);
            Stream.SetLength(checked((long)Manifest.TotalSize));
        }
        internal void Flush() => Stream?.Flush(flushToDisk: true);
        internal void Close()
        {
            var stream = Stream;
            Stream = null;
            try { stream?.Flush(flushToDisk: true); }
            finally { stream?.Dispose(); }
        }
        public void Dispose() { try { Close(); } finally { Gate.Dispose(); } }
    }
}
