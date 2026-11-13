using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;
using STS2Mobile.Steam;
using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void ChunkTransferPreparedPckRequiresReadyAuthority()
    {
        using var install = GameInstallFixture.Create();
        var identity = GameIdentityReader.ReadInstalled(install.DataDir, install.Branch);
        PublishInitialReady(install, identity, 1001);
        var ready = BranchInstallStateStore.Current.Read(install.DataDir, install.Branch);
        using var chunks = new ChunkFixture();
        var old = chunks.Manifest(1001, ("SlayTheSpire2.pck", new[] { "aaaa" }));
        old.DepotID = 123; // Match the authoritative install fixture's depot.
        var target = chunks.Manifest(1002, ("SlayTheSpire2.pck", new[] { "aaaa" })).Files![0];
        True(DepotDownloader.CanKeepPreparedPck(ready, old, target, identity), "A validated prepared PCK with unchanged Steam file identity should be retained.");
        target.FileHash = SHA1.HashData(Encoding.UTF8.GetBytes("bbbb"));
        True(!DepotDownloader.CanKeepPreparedPck(ready, old, target, identity), "A changed Steam file identity must reconstruct raw content.");
        target.FileHash = old.Files![0].FileHash;
        var gameDir = SteamGameInstallPaths.GameDirectory(install.DataDir, install.Branch);
        var preparedBytes = File.ReadAllBytes(Path.Combine(gameDir, "SlayTheSpire2.pck"));
        var unchanged = new DepotManifest { DepotID = 123, ManifestGID = 1002, Files = new() { target } };
        var retained = new DepotChunkTransfer(gameDir, chunks.StateRoot, 123, 1002, unchanged,
            new[] { new DepotChunkDonor(gameDir, old, Selected: true) }, 3,
            (_, _, _) => Task.FromException<int>(new Exception("An unchanged authoritative prepared PCK must not be downloaded.")),
            keepPreparedFile: file => DepotDownloader.CanKeepPreparedPck(ready, old, file, identity));
        retained.RunAsync(CancellationToken.None).GetAwaiter().GetResult();
        True(preparedBytes.SequenceEqual(File.ReadAllBytes(Path.Combine(gameDir, "SlayTheSpire2.pck"))), "Retaining the prepared archive must leave its bytes unchanged.");
        var wrong = new GameIdentity(identity.Branch, identity.InstallGeneration, new string('f', 64), identity.SourceAssemblySha256);
        True(!DepotDownloader.CanKeepPreparedPck(ready, old, target, wrong), "A stale prepared digest cannot replace authoritative identity validation.");
        True(!DepotDownloader.CanKeepPreparedPck(null!, old, target, identity), "Pending metadata must not authorize retaining a prepared archive.");

        var previous = chunks.Manifest(10, ("SlayTheSpire2.pck", new[] { "aaaa", "bbbb" }));
        chunks.Install(previous);
        File.WriteAllText(Path.Combine(chunks.InstallRoot, "SlayTheSpire2.pck"), "ptchbbbb");
        var changed = chunks.Manifest(11, ("SlayTheSpire2.pck", new[] { "aaaa", "cccc", "bbbb" }));
        chunks.Run(changed, new[] { new DepotChunkDonor(chunks.InstallRoot, previous, Selected: true) });
        Equal(2, chunks.Requests.Count, "A changed PCK must fetch modified ranges and reuse intact ranges.");
        True(DepotDownloader.VerifyFileHash(Path.Combine(chunks.InstallRoot, "SlayTheSpire2.pck"), changed.Files![0]), "Changed PCK reconstruction must pass its raw Steam file hash before preparation.");
    }

    private static void ChunkDeltaReusesMovedAndRepeatedContent()
    {
        using var f = new ChunkFixture();
        var old = f.Manifest(1, ("archive", new[] { "aaaa", "bbbb", "cccc" }));
        f.Install(old);
        var target = f.Manifest(2, ("archive", new[] { "cccc", "aaaa", "xxxx", "xxxx" }));
        f.Run(target, new[] { new DepotChunkDonor(f.InstallRoot, old) });
        Equal("ccccaaaaxxxxxxxx", File.ReadAllText(Path.Combine(f.InstallRoot, "archive")), "Moved and repeated chunks must reconstruct the exact target.");
        Equal(1, f.Requests.Count, "Only the new unique chunk should be fetched.");
        Equal(Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes("xxxx"))), f.Requests.Single(), "The fetched identity must be the missing chunk.");
        Equal(8L, f.LastProgress.ReusedBytes, "Local reuse and repeated fetched destinations must not be confused.");
    }

    private static void ChunkTransferDeduplicatesAcrossFiles()
    {
        using var f = new ChunkFixture();
        var target = f.Manifest(1, ("one", new[] { "aaaa", "bbbb" }), ("nested/two", new[] { "bbbb", "aaaa" }));
        f.Run(target);
        Equal(2, f.Requests.Count, "Identical chunks across files must share requests.");
        Equal("aaaabbbb", File.ReadAllText(Path.Combine(f.InstallRoot, "one")), "First file bytes differ.");
        Equal("bbbbaaaa", File.ReadAllText(Path.Combine(f.InstallRoot, "nested/two")), "Second file bytes differ.");
    }

    private static void ChunkTransferRejectsCorruptDonors()
    {
        using var f = new ChunkFixture();
        var old = f.Manifest(1, ("archive", new[] { "aaaa", "bbbb" }));
        f.Install(old);
        File.WriteAllText(Path.Combine(f.InstallRoot, "archive"), "aaaaBAD!");
        var target = f.Manifest(2, ("archive", new[] { "bbbb", "aaaa" }));
        f.Run(target, new[] { new DepotChunkDonor(f.InstallRoot, old) });
        Equal(1, f.Requests.Count, "Only the corrupt donor range should need downloading.");
        Equal("bbbbaaaa", File.ReadAllText(Path.Combine(f.InstallRoot, "archive")), "Corrupt donor bytes must not be promoted.");
    }

    private static void ChunkTransferCountsCompressedPayloadSeparately()
    {
        using var f = new ChunkFixture();
        var target = f.Manifest(1, ("archive", new[] { "aaaa", "bbbb", "bbbb" }));
        var file = target.Files![0];
        for (var i = 0; i < file.Chunks.Count; i++)
        {
            var chunk = file.Chunks[i];
            file.Chunks[i] = new DepotManifest.ChunkData(chunk.ChunkID!, chunk.Checksum, chunk.Offset,
                comp_length: i == 0 ? 2u : 3u, uncomp_length: chunk.UncompressedLength);
        }
        f.Run(target);
        Equal(12L, f.LastProgress.TotalContentBytes, "Logical content includes repeated destinations.");
        Equal(5L, f.LastProgress.PlannedDownloadBytes, "Planned compressed payload counts each missing identity once.");
        Equal(5L, f.LastProgress.DownloadedBytes, "Completed compressed payload must not count decompressed or repeated bytes.");
        Equal(2, f.Requests.Count, "Repeated destinations must not create extra network requests.");
        True(DepotDownloader.VerifyFileHash(Path.Combine(f.InstallRoot, "archive"), file), "Payload accounting must not alter assembled bytes.");
    }

    private static void ChunkTransferReusesOtherVersionWithoutWritingIt()
    {
        foreach (var corrupt in new[] { false, true })
        {
            using var f = new ChunkFixture();
            var donorRoot = Path.Combine(f.Root, "other-version");
            Directory.CreateDirectory(donorRoot);
            var donor = f.Manifest(1, ("archive", new[] { "aaaa", "bbbb" }));
            var donorBytes = corrupt ? "aaaaBAD!" : "aaaabbbb";
            File.WriteAllText(Path.Combine(donorRoot, "archive"), donorBytes);
            var target = f.Manifest(2, ("archive", new[] { "bbbb", "aaaa" }));
            f.Run(target, new[] { new DepotChunkDonor(donorRoot, donor) });
            Equal(corrupt ? 1 : 0, f.Requests.Count, "Another installed version must supply only ranges that pass verification.");
            Equal(donorBytes, File.ReadAllText(Path.Combine(donorRoot, "archive")), "Donor versions must remain byte-identical.");
            Equal("bbbbaaaa", File.ReadAllText(Path.Combine(f.InstallRoot, "archive")), "Target offsets must follow the new manifest.");
        }
    }

    private static void ChunkTransferParallelizesOneFile()
    {
        using var f = new ChunkFixture();
        var target = f.Manifest(1, ("archive", new[] { "aaaa", "bbbb", "cccc", "dddd", "eeee" }));
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var active = 0;
        var peak = 0;
        f.FetchOverride = async (chunk, buffer, ct) =>
        {
            var count = Interlocked.Increment(ref active);
            int before;
            do { before = Volatile.Read(ref peak); } while (count > before && Interlocked.CompareExchange(ref peak, count, before) != before);
            if (count == 3) entered.TrySetResult(true);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
            // Complete chunks out of offset order while the real writer handles offsets.
            if (chunk.Offset == 0) await Task.Delay(20, ct);
            var result = f.CopyChunk(chunk, buffer);
            Interlocked.Decrement(ref active);
            return result;
        };
        f.Run(target);
        Equal(3, peak, "One archive must use the three-worker pool and never exceed it.");
        Equal("aaaabbbbccccddddeeee", File.ReadAllText(Path.Combine(f.InstallRoot, "archive")), "Out-of-order completion must not corrupt offsets.");
    }

    private static void ChunkTransferPauseRetainsVerifiedProgress()
    {
        using var f = new ChunkFixture();
        File.WriteAllText(Path.Combine(f.InstallRoot, "archive"), "original");
        var target = f.Manifest(1, ("archive", new[] { "aaaa", "bbbb", "cccc" }));
        using var cancellation = new CancellationTokenSource();
        f.OnProgress = p => { if (p.DownloadedBytes >= 4) cancellation.Cancel(); };
        ExpectChunkFailure<OperationCanceledException>(() => f.Run(target, workers: 1, ct: cancellation.Token));
        Equal("original", File.ReadAllText(Path.Combine(f.InstallRoot, "archive")), "Pause must not replace installed files.");
        True(File.Exists(f.StagePath(1, "archive")), "Verified partial content must remain after pause.");
        var firstRequests = f.Requests.Count;
        f.OnProgress = null;
        f.Run(target, workers: 1);
        Equal(3, f.Requests.Count, "Resume must not refetch the completed first chunk.");
        Equal(1, firstRequests, "Pause should stop scheduling after the first completed chunk.");
        Equal(4L, f.LastProgress.ResumedBytes, "Resume progress must identify verified staged bytes.");
    }

    private static void ChunkTransferSharesWorkerLimitAcrossFiles()
    {
        foreach (var workers in new[] { 3, 8 })
        {
            using var f = new ChunkFixture();
            var target = f.Manifest(1,
                ("one", Enumerable.Range(0, 6).Select(i => $"{i:D4}").ToArray()),
                ("two", Enumerable.Range(6, 6).Select(i => $"{i:D4}").ToArray()));
            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var active = 0;
            var peak = 0;
            f.FetchOverride = async (chunk, buffer, ct) =>
            {
                var count = Interlocked.Increment(ref active);
                int before;
                do { before = Volatile.Read(ref peak); } while (count > before && Interlocked.CompareExchange(ref peak, count, before) != before);
                if (count == workers) entered.TrySetResult(true);
                try
                {
                    await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), ct);
                    await Task.Yield();
                    return f.CopyChunk(chunk, buffer);
                }
                finally { Interlocked.Decrement(ref active); }
            };
            f.Run(target, workers: workers);
            Equal(workers, peak, "Multiple files must share one configured worker limit.");
            True(target.Files!.All(file => DepotDownloader.VerifyFileHash(Path.Combine(f.InstallRoot, file.FileName), file)), "Every file must match despite concurrent completion.");
        }
    }

    private static void ChunkTransferDrainsRequestsAfterFailure()
    {
        using var f = new ChunkFixture();
        var target = f.Manifest(1, ("archive", new[] { "aaaa", "bbbb" }));
        var slowStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var failureStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        f.FetchOverride = async (chunk, buffer, ct) =>
        {
            if (chunk.Offset == 0)
            {
                await slowStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
                failureStarted.TrySetResult(true);
                throw new IOException("Fixture failed request");
            }
            slowStarted.TrySetResult(true);
            // SteamKit's in-flight request is allowed to finish after cancellation.
            await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
            return f.CopyChunk(chunk, buffer);
        };
        var run = f.Transfer(target, workers: 2).RunAsync(CancellationToken.None);
        failureStarted.Task.WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
        True(!run.IsCompleted, "A failed worker cannot close streams while another request is outstanding.");
        release.TrySetResult(true);
        ExpectChunkFailure<IOException>(() => run.GetAwaiter().GetResult());
        f.Requests.Clear();
        f.FetchOverride = null;
        f.Run(target);
        Equal(1, f.Requests.Count, "The successful in-flight chunk must be staged before failure completes.");
        Equal(4L, f.LastProgress.ResumedBytes, "Resume must recover the request drained after failure.");
    }

    private static void ChunkTransferPreservesManifestDirectories()
    {
        using var f = new ChunkFixture();
        var target = f.Manifest(1, ("nested/empty-file", Array.Empty<string>()));
        target.Files!.Add(new DepotManifest.FileData { FileName = "explicit/empty-directory", Flags = EDepotFileFlag.Directory });
        f.Run(target);
        True(Directory.Exists(Path.Combine(f.InstallRoot, "explicit/empty-directory")), "Explicit empty manifest directories must exist after commit.");
        True(File.Exists(Path.Combine(f.InstallRoot, "nested/empty-file")), "Zero-byte manifest files must still be created.");
        Equal(0, f.Requests.Count, "Directories and empty files do not require network chunks.");
    }

    private static void ChunkTransferStagingWriteFailurePreservesInstallation()
    {
        using var f = new ChunkFixture();
        var target = f.Manifest(1, ("nested/archive", new[] { "aaaa", "bbbb" }));
        var installed = Path.Combine(f.InstallRoot, "nested/archive");
        Directory.CreateDirectory(Path.GetDirectoryName(installed)!);
        File.WriteAllText(installed, "original");
        var blockingPath = Path.GetDirectoryName(f.StagePath(1, "nested/archive"))!;
        Directory.CreateDirectory(Path.GetDirectoryName(blockingPath)!);
        File.WriteAllText(blockingPath, "blocked staging directory");
        ExpectChunkFailure<IOException>(() => f.Run(target));
        Equal("original", File.ReadAllText(installed), "A staging filesystem error must not mutate the installed file.");
        Equal(0, f.Requests.Count, "Unwritable staging must fail before consuming transfer payload.");
        True(DepotDownloadStaging.HasPending(f.StateRoot), "The pending manifest must survive the filesystem failure.");
    }

    private static void ChunkTransferIgnoresCorruptPartialManifests()
    {
        using var f = new ChunkFixture();
        var prior = f.Manifest(1, ("archive", new[] { "aaaa", "bbbb" }));
        var generation = DepotDownloadStaging.GenerationRoot(f.StateRoot, 1, 1);
        Directory.CreateDirectory(generation);
        using var serialized = new MemoryStream();
        prior.Serialize(serialized);
        File.WriteAllBytes(Path.Combine(generation, "manifest.bin"), serialized.ToArray().Take(10).ToArray());
        var badMagic = DepotDownloadStaging.GenerationRoot(f.StateRoot, 1, 3);
        Directory.CreateDirectory(badMagic);
        File.WriteAllBytes(Path.Combine(badMagic, "manifest.bin"), new byte[32]);
        var target = f.Manifest(2, ("archive", new[] { "bbbb", "aaaa" }));
        f.Run(target);
        Equal(2, f.Requests.Count, "An unreadable superseded descriptor must not block fetching the valid target.");
        Equal("bbbbaaaa", File.ReadAllText(Path.Combine(f.InstallRoot, "archive")), "Corrupt optional metadata must not affect target verification.");
    }

    private static void ChunkTransferRechecksTruncationAndHoles()
    {
        foreach (var preallocate in new[] { false, true })
        {
            using var f = new ChunkFixture();
            var target = f.Manifest(1, ("archive", new[] { "aaaa", "bbbb", "cccc" }));
            var path = f.StagePath(1, "archive");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using (var stream = File.Create(path)) { stream.Write(Encoding.UTF8.GetBytes("aaaa")); if (preallocate) stream.SetLength(12); }
            f.Run(target);
            Equal(2, f.Requests.Count, "Truncated ranges and preallocated holes must not count as downloaded chunks.");
            Equal("aaaabbbbcccc", File.ReadAllText(Path.Combine(f.InstallRoot, "archive")), "Resume must reconstruct unverified ranges.");
        }
    }

    private static void ChunkTransferRetargetsPartialContent()
    {
        using var f = new ChunkFixture();
        var first = f.Manifest(1, ("archive", new[] { "aaaa", "bbbb", "cccc" }));
        f.FetchOverride = (chunk, buffer, ct) => chunk.Offset == 8
            ? Task.FromException<int>(new IOException("Fixture connection lost"))
            : Task.FromResult(f.CopyChunk(chunk, buffer));
        ExpectChunkFailure<IOException>(() => f.Run(first, workers: 1));
        f.Requests.Clear();
        f.FetchOverride = null;
        var next = f.Manifest(2, ("archive", new[] { "bbbb", "xxxx", "aaaa" }));
        f.Run(next);
        Equal(1, f.Requests.Count, "A new target must reuse matching chunks from the interrupted generation.");
        Equal("bbbbxxxxaaaa", File.ReadAllText(Path.Combine(f.InstallRoot, "archive")), "Retargeting must use target offsets.");
    }

    private static void ChunkTransferDoesNotPromoteFailedVerification()
    {
        using var f = new ChunkFixture();
        File.WriteAllText(Path.Combine(f.InstallRoot, "archive"), "original");
        File.WriteAllText(Path.Combine(f.InstallRoot, "first"), "first-original");
        var target = f.Manifest(1, ("first", new[] { "cccc" }), ("archive", new[] { "aaaa", "bbbb" }));
        target.Files![1].FileHash = SHA1.HashData(Encoding.UTF8.GetBytes("different"));
        ExpectChunkFailure<IOException>(() => f.Run(target));
        Equal("first-original", File.ReadAllText(Path.Combine(f.InstallRoot, "first")), "A verified first file must not be promoted before the later file verifies.");
        Equal("original", File.ReadAllText(Path.Combine(f.InstallRoot, "archive")), "Failed final verification must leave the old installation intact.");
        True(File.Exists(f.StagePath(1, "archive")), "Failure must retain recoverable staging.");
    }

    private static void ChunkTransferSpaceFailureRetainsSources()
    {
        using var f = new ChunkFixture();
        File.WriteAllText(Path.Combine(f.InstallRoot, "archive"), "original");
        var target = f.Manifest(1, ("archive", new[] { "aaaa", "bbbb" }));
        var transfer = f.Transfer(target, ensureSpace: _ => throw new IOException("Fixture disk full"));
        ExpectChunkFailure<IOException>(() => transfer.RunAsync(CancellationToken.None).GetAwaiter().GetResult());
        Equal(0, f.Requests.Count, "Insufficient staging space must stop before network requests.");
        Equal("original", File.ReadAllText(Path.Combine(f.InstallRoot, "archive")), "Space failure must preserve installed content.");
    }

    private static void ChunkTransferRejectsEscapingPaths()
    {
        using var f = new ChunkFixture();
        var target = f.Manifest(1, ("../outside", new[] { "aaaa" }));
        ExpectChunkFailure<IOException>(() => f.Run(target));
        True(!File.Exists(Path.Combine(f.Root, "outside")), "A manifest must not escape the installation root.");
    }

    private static void ChunkTransferRepairsCorruptStagedRange()
    {
        using var f = new ChunkFixture();
        var target = f.Manifest(1, ("archive", new[] { "aaaa", "bbbb", "cccc" }));
        var path = f.StagePath(1, "archive");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "aaaaBAD!cccc");
        f.Run(target);
        Equal(1, f.Requests.Count, "Only the corrupt staged range should be fetched.");
        Equal(8L, f.LastProgress.ResumedBytes, "Valid staged ranges must survive corruption elsewhere.");
    }

    private static void ChunkTransferSalvagesLegacyPartial()
    {
        using var f = new ChunkFixture();
        var target = f.Manifest(1, ("archive", new[] { "aaaa", "bbbb" }));
        File.WriteAllText(Path.Combine(f.InstallRoot, "archive.downloading"), "aaaa");
        f.Run(target);
        Equal(1, f.Requests.Count, "A legacy partial must supply only ranges that pass target SHA-1 verification.");
        Equal("aaaabbbb", File.ReadAllText(Path.Combine(f.InstallRoot, "archive")), "Legacy content must reconstruct a verified file.");
    }

    private static void ChunkTransferRecoversInterruptedPromotion()
    {
        using var f = new ChunkFixture();
        var target = f.Manifest(1, ("one", new[] { "aaaa" }), ("two", new[] { "bbbb" }));
        var commits = 0;
        var transfer = f.Transfer(target, beforePromote: () => { if (++commits == 2) throw new IOException("Fixture interrupted commit"); });
        ExpectChunkFailure<IOException>(() => transfer.RunAsync(CancellationToken.None).GetAwaiter().GetResult());
        True(File.Exists(Path.Combine(f.InstallRoot, "one")), "The first complete file should already be committed.");
        True(!File.Exists(Path.Combine(f.InstallRoot, "two")), "The second file must remain in staging.");
        f.Requests.Clear();
        f.Run(target);
        Equal(0, f.Requests.Count, "Recovery must use committed files and verified staging without downloading them again.");
        Equal("bbbb", File.ReadAllText(Path.Combine(f.InstallRoot, "two")), "Recovery must complete the remaining promotion.");
    }

    private static void DepotCommitRemovesObsoleteAncestorsAfterVerification()
    {
        foreach (var invalidReplacement in new[] { false, true })
        {
            using var f = new ChunkFixture();
            var oldPath = Path.Combine(f.InstallRoot, "archive");
            File.WriteAllText(oldPath, "old!");
            var target = f.Manifest(2, ("archive/part", new[] { "bbbb" }), ("other", new[] { "cccc" }));
            if (invalidReplacement) target.Files![1].FileHash = SHA1.HashData(Encoding.UTF8.GetBytes("bad!"));
            var deletions = 0;
            Action requireUpdating = () => { };
            Action deleteObsolete = () => { deletions++; File.Delete(oldPath); };
            var commit = DepotDownloader.CreateDepotCommit(requireUpdating, deleteObsolete);
            f.FetchOverride = (chunk, buffer, _) =>
            {
                Equal("old!", File.ReadAllText(oldPath), "Obsolete ancestors must remain intact during transfer.");
                return Task.FromResult(f.CopyChunk(chunk, buffer));
            };
            var transfer = f.Transfer(target, beforePromote: commit);
            if (invalidReplacement)
            {
                ExpectChunkFailure<IOException>(() => transfer.RunAsync(CancellationToken.None).GetAwaiter().GetResult());
                Equal(0, deletions, "A later failed file hash must prevent all obsolete cleanup and promotion.");
                Equal("old!", File.ReadAllText(oldPath), "Failed verification must preserve the old file.");
            }
            else
            {
                transfer.RunAsync(CancellationToken.None).GetAwaiter().GetResult();
                commit();
                Equal(1, deletions, "Commit cleanup must execute once despite multiple promotions and final manifest commit.");
                Equal("bbbb", File.ReadAllText(Path.Combine(f.InstallRoot, "archive", "part")), "An obsolete regular file must not block an implicit target directory.");
            }
        }
        using var deletionOnly = new ChunkFixture();
        var obsolete = Path.Combine(deletionOnly.InstallRoot, "obsolete");
        File.WriteAllText(obsolete, "old");
        var finish = DepotDownloader.CreateDepotCommit(() => { }, () => File.Delete(obsolete));
        deletionOnly.Transfer(deletionOnly.Manifest(3), beforePromote: finish).RunAsync(CancellationToken.None).GetAwaiter().GetResult();
        finish();
        True(!File.Exists(obsolete), "A deletion-only depot update must still execute its verified commit.");
    }

    private static void ExpectChunkFailure<T>(Action run) where T : Exception
    {
        try { run(); }
        catch (T) { return; }
        throw new Exception($"Expected {typeof(T).Name}.");
    }

    private sealed class ChunkFixture : IDisposable
    {
        internal readonly string Root = Path.Combine(Path.GetTempPath(), "sts2-chunks-" + Guid.NewGuid().ToString("N"));
        internal string InstallRoot => Path.Combine(Root, "game");
        internal string StateRoot => Path.Combine(Root, "download_state");
        internal readonly List<string> Requests = new();
        private readonly Dictionary<string, byte[]> _content = new(StringComparer.Ordinal);
        internal Func<DepotManifest.ChunkData, byte[], CancellationToken, Task<int>>? FetchOverride;
        internal Action<DepotChunkTransferProgress>? OnProgress;
        internal DepotChunkTransferProgress LastProgress;
        internal ChunkFixture() => Directory.CreateDirectory(InstallRoot);

        internal DepotManifest Manifest(ulong id, params (string Name, string[] Chunks)[] files)
        {
            var manifest = new DepotManifest { DepotID = 1, ManifestGID = id, Files = new(), CreationTime = DateTime.UnixEpoch };
            foreach (var (name, chunks) in files)
            {
                var all = Encoding.UTF8.GetBytes(string.Concat(chunks));
                var file = new DepotManifest.FileData { FileName = name, TotalSize = (ulong)all.Length, FileHash = SHA1.HashData(all) };
                ulong offset = 0;
                foreach (var text in chunks)
                {
                    var bytes = Encoding.UTF8.GetBytes(text);
                    var hash = SHA1.HashData(bytes);
                    _content[Convert.ToHexString(hash)] = bytes;
                    file.Chunks.Add(new DepotManifest.ChunkData(hash, 0, offset, comp_length: (uint)bytes.Length, uncomp_length: (uint)bytes.Length));
                    offset += (uint)bytes.Length;
                }
                manifest.Files.Add(file);
            }
            return manifest;
        }

        internal void Install(DepotManifest manifest)
        {
            foreach (var file in manifest.Files!)
            {
                var path = Path.Combine(InstallRoot, file.FileName);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, file.Chunks.SelectMany(c => _content[Convert.ToHexString(c.ChunkID!)]).ToArray());
            }
        }

        internal string StagePath(ulong id, string name) => Path.Combine(StateRoot, "staging-v1", "1", id.ToString(), "files", name + ".downloading");
        internal int CopyChunk(DepotManifest.ChunkData chunk, byte[] buffer)
        {
            var data = _content[Convert.ToHexString(chunk.ChunkID!)];
            data.CopyTo(buffer, 0);
            return data.Length;
        }

        internal DepotChunkTransfer Transfer(DepotManifest manifest, IReadOnlyList<DepotChunkDonor>? donors = null, int workers = 3, Action<long>? ensureSpace = null, Action? beforePromote = null)
            => new(InstallRoot, StateRoot, 1, manifest.ManifestGID, manifest, donors ?? Array.Empty<DepotChunkDonor>(), workers,
                async (chunk, buffer, ct) =>
                {
                    lock (Requests) Requests.Add(Convert.ToHexString(chunk.ChunkID!));
                    return FetchOverride != null ? await FetchOverride(chunk, buffer, ct) : CopyChunk(chunk, buffer);
                }, p => { LastProgress = p; OnProgress?.Invoke(p); }, ensureSpace: ensureSpace, beforePromote: beforePromote);

        internal void Run(DepotManifest manifest, IReadOnlyList<DepotChunkDonor>? donors = null, int workers = 3, CancellationToken ct = default)
            => Transfer(manifest, donors, workers).RunAsync(ct).GetAwaiter().GetResult();
        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
