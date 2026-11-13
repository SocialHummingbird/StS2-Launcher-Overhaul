using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;
using STS2Mobile.Launcher;
using STS2Mobile.Steam;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void ChunkTransferNormalizesSteamFilenames()
    {
        using var f = new ChunkFixture();
        var target = f.Manifest(1,
            ("controller_config\\game_actions.vdf\0\0", new[] { "aaaa" }),
            ("ordinary", new[] { "bbbb" }));
        target.Files!.Insert(0, new DepotManifest.FileData { FileName = "controller_config\0", Flags = EDepotFileFlag.Directory });
        target.Files.Add(new DepotManifest.FileData { FileName = "nested\\empty\0", Flags = EDepotFileFlag.Directory });
        f.Run(target);
        var config = Path.Combine(f.InstallRoot, "controller_config", "game_actions.vdf");
        Equal("aaaa", File.ReadAllText(config), "Steam filename padding must not enter physical paths.");
        Equal("bbbb", File.ReadAllText(Path.Combine(f.InstallRoot, "ordinary")), "Unpadded names must keep working.");
        True(Directory.Exists(Path.Combine(f.InstallRoot, "nested", "empty")), "Padded empty directory entries must be committed.");
        True(DepotDownloader.VerifyFileHash(config, target.Files[1]), "Canonical naming must preserve the target Steam hash.");
        Equal(2, f.Requests.Count, "Only the two distinct content chunks should be fetched.");
    }

    private static void ChunkTransferNulNamesPauseAndResume()
    {
        using var f = new ChunkFixture();
        var target = f.Manifest(1, ("nested\\archive\0", new[] { "aaaa", "bbbb", "cccc" }));
        using var cancellation = new CancellationTokenSource();
        f.OnProgress = progress => { if (progress.DownloadedBytes >= 4) cancellation.Cancel(); };
        ExpectChunkFailure<OperationCanceledException>(() => f.Run(target, workers: 1, ct: cancellation.Token));
        var staged = f.StagePath(1, "nested/archive");
        True(File.Exists(staged), "Normalisation must happen before adding the .downloading suffix.");
        True(!File.Exists(Path.Combine(Path.GetDirectoryName(staged)!, "archive")), "Staging must not lose its partial-file suffix.");
        DepotManifest restart;
        using (var input = File.OpenRead(Path.Combine(f.StateRoot, "staging-v1", "1", "1", "manifest.bin")))
            restart = DepotManifest.Deserialize(input);
        Equal("nested\\archive\0", restart.Files![0].FileName, "Pending descriptors must retain the original manifest name.");
        f.OnProgress = null;
        f.Requests.Clear();
        f.Run(restart, workers: 1);
        Equal(2, f.Requests.Count, "Restart must fetch only the two ranges absent from staging.");
        Equal(4L, f.LastProgress.ResumedBytes, "The verified padded-name range must be recognised as resumed.");
        Equal("aaaabbbbcccc", File.ReadAllText(Path.Combine(f.InstallRoot, "nested", "archive")), "Resume must promote the exact target bytes.");
    }

    private static void ChunkTransferNulNamesReuseInstalledDonors()
    {
        foreach (var selected in new[] { false, true })
            foreach (var corrupt in new[] { false, true })
            {
                using var f = new ChunkFixture();
                var donorRoot = selected ? f.InstallRoot : Path.Combine(f.Root, "other-version");
                Directory.CreateDirectory(Path.Combine(donorRoot, "nested"));
                var donorPath = Path.Combine(donorRoot, "nested", "archive");
                var original = corrupt ? "aaaaBAD!" : "aaaabbbb";
                File.WriteAllText(donorPath, original);
                var donor = f.Manifest(1, ("nested\\archive\0", new[] { "aaaa", "bbbb" }));
                var target = f.Manifest(2, ("nested/archive\0\0", new[] { "bbbb", "aaaa", "xxxx" }));
                var transfer = f.Transfer(target, new[] { new DepotChunkDonor(donorRoot, donor, Selected: selected) },
                    beforePromote: () => Equal(original, File.ReadAllText(donorPath), "Donor bytes must remain intact until verified promotion."));
                transfer.RunAsync(CancellationToken.None).GetAwaiter().GetResult();
                Equal(corrupt ? 2 : 1, f.Requests.Count, "Only new and corrupt ranges should be fetched from padded-name donors.");
                Equal("bbbbaaaaxxxx", File.ReadAllText(Path.Combine(f.InstallRoot, "nested", "archive")), "Moved donor ranges must use the target offsets.");
                if (!selected) Equal(original, File.ReadAllText(donorPath), "Cross-version donors must never be changed.");
            }
    }

    private static void ChunkTransferNulNamesReuseSupersededPartials()
    {
        using var f = new ChunkFixture();
        var first = f.Manifest(1, ("old\\archive\0", new[] { "aaaa", "bbbb", "cccc" }));
        f.FetchOverride = (chunk, buffer, _) => chunk.Offset == 8
            ? Task.FromException<int>(new IOException("Fixture connection lost"))
            : Task.FromResult(f.CopyChunk(chunk, buffer));
        ExpectChunkFailure<IOException>(() => f.Run(first, workers: 1));
        True(File.Exists(f.StagePath(1, "old/archive")), "Interrupted padded-name content must survive in the existing staging layout.");
        f.Requests.Clear();
        f.FetchOverride = null;
        var next = f.Manifest(2, ("new\\archive\0", new[] { "bbbb", "xxxx", "aaaa" }));
        f.Run(next);
        Equal(1, f.Requests.Count, "A changed manifest must reuse moved ranges from padded-name partials.");
        Equal("bbbbxxxxaaaa", File.ReadAllText(Path.Combine(f.InstallRoot, "new", "archive")), "Retargeted partial bytes differ.");
    }

    private static void ChunkTransferNulNamesSalvageLegacyPartials()
    {
        using var f = new ChunkFixture();
        Directory.CreateDirectory(Path.Combine(f.InstallRoot, "nested"));
        File.WriteAllText(Path.Combine(f.InstallRoot, "nested", "archive.downloading"), "aaaa");
        var target = f.Manifest(1, ("nested\\archive\0", new[] { "aaaa", "bbbb" }));
        f.Run(target);
        Equal(1, f.Requests.Count, "The legacy partial suffix must survive filename normalisation.");
        Equal("aaaabbbb", File.ReadAllText(Path.Combine(f.InstallRoot, "nested", "archive")), "Legacy padded-name recovery failed.");
    }

    private static void ChunkTransferNulNamesRejectCorruptStaging()
    {
        using var f = new ChunkFixture();
        var staged = f.StagePath(1, "nested/archive");
        Directory.CreateDirectory(Path.GetDirectoryName(staged)!);
        File.WriteAllText(staged, "aaaaBAD!cccc");
        var target = f.Manifest(1, ("nested\\archive\0", new[] { "aaaa", "bbbb", "cccc" }));
        f.Run(target);
        Equal(1, f.Requests.Count, "Padded-name staging must still reject a corrupt range.");
        Equal(8L, f.LastProgress.ResumedBytes, "Only the two verified staged ranges may count as resumed.");
        Equal("aaaabbbbcccc", File.ReadAllText(Path.Combine(f.InstallRoot, "nested", "archive")), "Corrupt staging must be repaired before promotion.");
    }

    private static void ChunkTransferNulNamesRetainPreparedPck()
    {
        using var install = GameInstallFixture.Create();
        var identity = GameIdentityReader.ReadInstalled(install.DataDir, install.Branch);
        PublishInitialReady(install, identity, 1001);
        var ready = BranchInstallStateStore.Current.Read(install.DataDir, install.Branch);
        using var chunks = new ChunkFixture();
        var old = chunks.Manifest(1001, ("SlayTheSpire2.pck\0", new[] { "aaaa" }));
        old.DepotID = 123;
        var target = chunks.Manifest(1002, ("SlayTheSpire2.pck\0\0", new[] { "aaaa" }));
        target.DepotID = 123;
        var game = SteamGameInstallPaths.GameDirectory(install.DataDir, install.Branch);
        var pck = Path.Combine(game, "SlayTheSpire2.pck");
        var before = File.ReadAllBytes(pck);
        var transfer = new DepotChunkTransfer(game, chunks.StateRoot, 123, 1002, target,
            new[] { new DepotChunkDonor(game, old, Selected: true) }, 3,
            (_, _, _) => Task.FromException<int>(new Exception("An authoritative unchanged prepared PCK must not be fetched.")),
            keepPreparedFile: file => DepotDownloader.CanKeepPreparedPck(ready, old, file, identity));
        transfer.RunAsync(CancellationToken.None).GetAwaiter().GetResult();
        True(before.SequenceEqual(File.ReadAllBytes(pck)), "Padded manifest metadata must not invalidate an unchanged authoritative prepared PCK.");
    }

    private static void ChunkTransferNormalizedPathsFailClosed()
    {
        foreach (var names in new[]
        {
            new[] { "\0padding", "archive" }, new[] { "../outside\0", "archive" },
            new[] { "/outside\0", "archive" }, new[] { "nested\\..\\..\\outside\0", "archive" },
            new[] { "archive\0ignored", "archive" }, new[] { "nested\\archive\0", "nested/archive\0\0" },
        })
        {
            using var f = new ChunkFixture();
            File.WriteAllText(Path.Combine(f.InstallRoot, "archive"), "original");
            var target = f.Manifest(1, (names[0], new[] { "aaaa" }), (names[1], new[] { "bbbb" }));
            ExpectChunkFailure<IOException>(() => f.Run(target));
            Equal(0, f.Requests.Count, "Invalid or colliding normalised names must fail before fetching content.");
            Equal("original", File.ReadAllText(Path.Combine(f.InstallRoot, "archive")), "Path validation failure must preserve the installed file.");
            True(!File.Exists(Path.Combine(f.Root, "outside")), "Normalised traversal must not escape its root.");
        }
    }

    private static void ValidationStagingInspectionChecksActualRangesReadOnly()
    {
        foreach (var content in new[] { "aaaaBAD!\0\0\0\0", "aaaa" })
        {
            using var f = new ChunkFixture();
            var target = f.Manifest(1, ("nested\\archive\0", new[] { "aaaa", "bbbb", "cccc" }));
            var generation = Path.Combine(f.StateRoot, "staging-v1", "1", "1");
            Directory.CreateDirectory(generation);
            using (var descriptor = File.Create(Path.Combine(generation, "manifest.bin"))) target.Serialize(descriptor);
            var path = f.StagePath(1, "nested/archive");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
            var before = File.ReadAllBytes(path);
            var snapshot = ValidationStagingInspection.Read(f.Root, SteamGameBranch.Public);
            Equal(1, snapshot.VerifiedChunks.Count, "Inspection must reject corrupt, preallocated and missing ranges.");
            Equal(4L, snapshot.VerifiedLogicalBytes, "Only actual SHA-1-verified bytes may count as retained.");
            Equal(2, snapshot.InvalidRanges, "Both unavailable target ranges must remain unverified.");
            True(!snapshot.InstallationReady, "A valid partial must never count as an installed game.");
            True(before.SequenceEqual(File.ReadAllBytes(path)), "Inspection must leave staging bytes unchanged.");
        }
    }
}
