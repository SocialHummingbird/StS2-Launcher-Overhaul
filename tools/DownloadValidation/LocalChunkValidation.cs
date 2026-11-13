using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class LocalChunkValidation
{
    internal static async Task<object> RunAsync(string dataDir, string branch, string run, string output, string attemptId,
        long cancelAfterBytes, CancellationTokenSource cancellation, Action<string> status)
    {
        using var input = File.OpenRead(Path.Combine(SteamGameInstallPaths.DownloadStateDirectoryPath(dataDir, branch), "2868841.manifest"));
        var sourceManifest = DepotManifest.Deserialize(input);
        var sourceFile = sourceManifest.Files!.Single(file => DepotDownloader.NormalizeManifestPath(file.FileName) == "data_sts2_windows_x86_64/System.Private.CoreLib.dll");
        var sourcePath = DepotDownloadStaging.Resolve(SteamGameInstallPaths.GameDirectory(dataDir, branch), sourceFile.FileName);
        if (!DepotDownloader.VerifyFileHash(sourcePath, sourceFile)) throw new IOException("The local fixture's captured source does not match its Steam SHA-1.");
        var raw = File.ReadAllBytes(sourcePath);
        var targetBytes = new byte[raw.Length];
        var targetFile = new DepotManifest.FileData { FileName = "rotated/corelib-fixture.bin", TotalSize = sourceFile.TotalSize };
        ulong offset = 0;
        foreach (var chunk in sourceFile.Chunks.Skip(1).Concat(sourceFile.Chunks.Take(1)))
        {
            Buffer.BlockCopy(raw, checked((int)chunk.Offset), targetBytes, checked((int)offset), checked((int)chunk.UncompressedLength));
            targetFile.Chunks.Add(new DepotManifest.ChunkData(chunk.ChunkID!, chunk.Checksum, offset,
                comp_length: chunk.CompressedLength, uncomp_length: chunk.UncompressedLength));
            offset += chunk.UncompressedLength;
        }
        targetFile.FileHash = ManagedSha1.Hash(targetBytes);
        var target = new DepotManifest
        {
            DepotID = sourceManifest.DepotID,
            ManifestGID = sourceManifest.ManifestGID ^ 0x2000000000000000UL,
            CreationTime = sourceManifest.CreationTime,
            Files = new List<DepotManifest.FileData> { targetFile }
        };
        var chunks = sourceFile.Chunks.GroupBy(chunk => Convert.ToHexString(chunk.ChunkID!)).ToDictionary(group => group.Key, group => group.First());
        var writeLock = new object();
        int active = 0, peak = 0, started = 0, completed = 0;
        long logicalPayload = 0;
        long localSourceBytes = 0;
        DepotChunkTransferProgress last = default;
        void Emit(DepotManifest.ChunkData chunk, string phase, string error = null)
        {
            var value = new DownloadValidationChunkEvent(attemptId, DateTimeOffset.UtcNow, target.DepotID,
                Convert.ToHexString(chunk.ChunkID!), chunk.Offset, chunk.UncompressedLength, chunk.CompressedLength,
                phase, error, Volatile.Read(ref active), Volatile.Read(ref peak), Volatile.Read(ref started),
                Volatile.Read(ref completed), Interlocked.Read(ref logicalPayload));
            lock (writeLock)
            {
                File.AppendAllText(Path.Combine(output, "chunk-events.jsonl"), JsonSerializer.Serialize(value) + "\n");
                DownloadValidationRunner.Write(Path.Combine(output, "chunk-summary.json"), value);
            }
        }
        var install = Path.Combine(run, "local-target");
        var transfer = new DepotChunkTransfer(install, Path.Combine(run, "local-state"), target.DepotID, target.ManifestGID,
            target, Array.Empty<DepotChunkDonor>(), 3, async (chunk, buffer, token) =>
            {
                Interlocked.Increment(ref started);
                var concurrency = Interlocked.Increment(ref active);
                int previous;
                do { previous = Volatile.Read(ref peak); } while (concurrency > previous && Interlocked.CompareExchange(ref peak, concurrency, previous) != previous);
                Emit(chunk, "start");
                try
                {
                    // A test-only delay exposes cancellation/process-death windows; this is not a network benchmark.
                    await Task.Delay(3000, token).ConfigureAwait(false);
                    var source = chunks[Convert.ToHexString(chunk.ChunkID!)];
                    using var file = File.OpenRead(sourcePath);
                    file.Position = checked((long)source.Offset);
                    file.ReadExactly(buffer.AsSpan(0, checked((int)source.UncompressedLength)));
                    Interlocked.Increment(ref completed);
                    Interlocked.Add(ref localSourceBytes, source.UncompressedLength);
                    Interlocked.Add(ref logicalPayload, chunk.CompressedLength);
                    Emit(chunk, "complete");
                    return checked((int)source.UncompressedLength);
                }
                catch (Exception error) { Emit(chunk, error is OperationCanceledException ? "cancelled" : "failed", error.GetType().Name); throw; }
                finally { Interlocked.Decrement(ref active); }
            }, progress =>
            {
                last = progress;
                lock (writeLock)
                {
                    DownloadValidationRunner.Write(Path.Combine(output, "transfer.json"), progress);
                    File.AppendAllText(Path.Combine(output, "raw-progress.jsonl"), JsonSerializer.Serialize(new { attemptId, utc = DateTimeOffset.UtcNow, depotId = target.DepotID, progress }) + "\n");
                }
                status($"Local fixture: resumed {progress.ResumedBytes}, verified local fetch {progress.DownloadedBytes} logical compressed bytes");
                if (cancelAfterBytes > 0 && progress.DownloadedBytes >= cancelAfterBytes) cancellation.Cancel();
            }, beforePromote: () =>
            {
                lock (writeLock) File.AppendAllText(Path.Combine(output, "promotions.jsonl"), JsonSerializer.Serialize(new { attemptId, utc = DateTimeOffset.UtcNow, previousTargetExists = File.Exists(DepotDownloadStaging.Resolve(install, targetFile.FileName)) }) + "\n");
            });
        await transfer.RunAsync(cancellation.Token).ConfigureAwait(false);
        if (!DepotDownloader.VerifyFileHash(DepotDownloadStaging.Resolve(install, targetFile.FileName), targetFile)) throw new IOException("Local Android fixture final SHA-1 mismatch.");
        return new
        {
            sourceManifest = sourceManifest.ManifestGID,
            syntheticTargetManifest = target.ManifestGID,
            verifiedRawFile = true,
            expectedSha1 = Convert.ToHexString(targetFile.FileHash),
            contentBytes = targetFile.TotalSize,
            peakConcurrency = peak,
            startedRequests = started,
            completedRequests = completed,
            networkPayloadBytes = 0,
            verifiedLocalFetchBytes = localSourceBytes,
            logicalCompressedPayloadBytes = logicalPayload,
            progress = last
        };
    }
}
