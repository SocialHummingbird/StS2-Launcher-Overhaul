using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SteamKit2.Internal;
using STS2Mobile.Launcher;
using STS2Mobile.Steam.Workshop;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void WorkshopBackgroundInventoryPreservesDigestContract()
    {
        var directory = Path.Combine(Path.GetTempPath(), "sts2-background-inventory", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(directory, "nested"));
        try
        {
            File.WriteAllBytes(Path.Combine(directory, "a.txt"), new byte[] { 97, 98, 99 });
            File.WriteAllBytes(Path.Combine(directory, "nested", "empty.dat"), Array.Empty<byte>());
            var observed = System.Threading.Tasks.Task.Run(() =>
            {
                var files = SteamWorkshopPackageInventory.Capture(directory);
                return (Files: files, Digest: SteamWorkshopPackageInventory.ContentSha256(files));
            }).GetAwaiter().GetResult();
            Equal("a.txt,nested/empty.dat", string.Join(",", observed.Files.Select(file => file.Path)),
                "Background inventory must preserve canonical relative paths and order.");
            Equal(3L, observed.Files[0].Length, "The streamed file inventory must retain the exact byte length.");
            Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", observed.Files[0].Sha256,
                "Background streamed SHA-256 must match the complete abc known vector.");
            Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", observed.Files[1].Sha256,
                "An empty companion must retain the standard SHA-256 identity.");
            Equal("e217fcbe47503f38f8ff59b7f141a0d4d66cc3a3e10bd4889e45d8f20c6ed8a7", observed.Digest,
                "Moving hashing off the Java bridge must preserve the existing package digest format.");
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static void WorkshopRuntimeSiblingWritesRemainLaunchable()
    {
        using var fixture = new WritableWorkshopFixture();
        var manifest = fixture.Stage();
        var installed = manifest.Items.Single().Installed;
        var publisherHash = installed.ContentSha256;
        var before = fixture.Resolve(manifest);
        True(before.Success, "The original complete download must prepare.");

        WritableWorkshopFixture.WriteRuntimeFiles(installed.Directory);
        var after = fixture.Resolve(manifest);
        True(after.Success, $"Ordinary sibling config and generated image writes must remain launchable: {after.Error?.Message}");
        Equal(installed.Generation, after.Plan.Sources.Single().InstalledGeneration,
            "Runtime writes must not change committed publisher revision identity.");
        Equal(publisherHash, installed.ContentSha256, "Preparation must not rewrite the downloaded publisher inventory.");
        True(before.Plan.Fingerprint != after.Plan.Fingerprint,
            "The complete observed writable package must participate in the launch fingerprint.");
        True(LauncherModLaunchResultStore.ConfigurationId(before.Plan, "game", "runtime")
            != LauncherModLaunchResultStore.ConfigurationId(after.Plan, "game", "runtime"),
            "Runtime writes must invalidate evidence for a different launch configuration.");

        var config = Path.Combine(installed.Directory, "Cursor.cfg");
        var timestamp = File.GetLastWriteTimeUtc(config);
        File.WriteAllText(config, "CursorScale=1.75");
        File.SetLastWriteTimeUtc(config, timestamp);
        var changed = fixture.Resolve(manifest);
        True(changed.Success && changed.Plan.Fingerprint != after.Plan.Fingerprint,
            "A same-size, same-time runtime config edit must be captured afresh.");
    }

    private static void WorkshopSameRevisionSyncRetainsRuntimeWrites()
    {
        using var fixture = new WritableWorkshopFixture();
        var initial = fixture.Stage().Items.Single().Installed;
        WritableWorkshopFixture.WriteRuntimeFiles(initial.Directory);
        File.WriteAllText(Path.Combine(initial.Directory, "changed-publisher.txt"), "runtime edit");
        var detail = new PublishedFileDetails { publishedfileid = 42, result = 1, title = "Fixture", time_updated = 12, hcontent_file = 100 };
        var info = new CPublishedFile_GetItemInfo_Response.WorkshopItemInfo { published_file_id = 42, time_updated = 12, manifest_id = 100 };
        using var downloader = new SteamWorkshopDownloader(null, fixture.Downloads);
        var downloaded = downloader.DownloadAsync(new[] { detail },
            new Dictionary<ulong, CPublishedFile_GetItemInfo_Response.WorkshopItemInfo> { [42] = info },
            new ulong[] { 42 }, null, "public", fixture.Stager.LoadManifest().Items.ToDictionary(item => item.PublishedFileId))
            .GetAwaiter().GetResult().Single();
        True(downloaded.ReusedCachedDownload,
            "A verified same-revision working directory must be reused after legitimate runtime writes.");
        var reused = fixture.Stager.Stage(new[] { downloaded }).Items.Single();
        Equal("staged", reused.Status, "A same-revision sync must succeed without replacing runtime state.");
        Equal(initial.Generation, reused.Installed.Generation, "Same-revision sync must retain the committed generation.");
        Equal("runtime edit", File.ReadAllText(Path.Combine(reused.Installed.Directory, "changed-publisher.txt")),
            "Same-revision sync must retain local edits to publisher files too.");
        WritableWorkshopFixture.AssertRuntimeFiles(reused.Installed.Directory);
    }

    private static void WorkshopSameRevisionRestagingRetainsRuntimeWrites()
    {
        using var fixture = new WritableWorkshopFixture();
        var initial = fixture.Stage().Items.Single().Installed;
        WritableWorkshopFixture.WriteRuntimeFiles(initial.Directory);
        var reused = fixture.Stage(timeUpdated: 13, hContentFile: 0).Items.Single().Installed;
        Equal(initial.Generation, reused.Generation, "The same authoritative depot manifest must retain its working generation even if metadata is refreshed.");
        Equal(initial.Directory, reused.Directory, "Restaging identical publisher content must not move writable files.");
        Equal(13L, reused.TimeUpdated, "A stable payload identity must still refresh coherent metadata.");
        Equal(0UL, reused.HContentFile, "Moving a current manifest to an author snapshot must refresh its transport metadata.");
        WritableWorkshopFixture.AssertRuntimeFiles(reused.Directory);
    }

    private static void WorkshopChangedRevisionMergesWritablePackageState()
    {
        using var fixture = new WritableWorkshopFixture();
        var first = fixture.Stage().Items.Single().Installed;
        WritableWorkshopFixture.WriteRuntimeFiles(first.Directory);
        File.WriteAllText(Path.Combine(first.Directory, "unchanged-publisher.txt"), "local unchanged-publisher edit");
        File.WriteAllText(Path.Combine(first.Directory, "changed-publisher.txt"), "local changed-publisher edit");
        File.WriteAllText(Path.Combine(first.Directory, "removed-publisher.txt"), "local removed-publisher edit");
        File.WriteAllText(Path.Combine(first.Directory, "future-publisher.txt"), "local added conflict");
        Directory.CreateDirectory(Path.Combine(first.Directory, "new-publisher-file"));
        File.WriteAllText(Path.Combine(first.Directory, "new-publisher-file", "local.txt"), "local directory conflict");
        File.WriteAllText(Path.Combine(first.Directory, "new-publisher-directory"), "local file conflict");
        fixture.Write("CookieCursor.dll", "publisher revision two");
        fixture.Write("changed-publisher.txt", "publisher changed value");
        fixture.Write("future-publisher.txt", "publisher newly owns this path");
        fixture.Write("new-publisher-file", "publisher file wins");
        fixture.Write("new-publisher-directory/child.txt", "publisher directory wins");
        File.Delete(Path.Combine(fixture.Source, "removed-publisher.txt"));

        var updated = fixture.Stage(manifestId: 101, timeUpdated: 13, hContentFile: 101).Items.Single().Installed;
        True(first.Generation != updated.Generation && first.Directory != updated.Directory,
            "A changed publisher revision must prepare a distinct committed directory.");
        WritableWorkshopFixture.AssertRuntimeFiles(updated.Directory);
        Equal("local unchanged-publisher edit", File.ReadAllText(Path.Combine(updated.Directory, "unchanged-publisher.txt")),
            "A local published-file edit must survive when its publisher hash is unchanged.");
        Equal("publisher changed value", File.ReadAllText(Path.Combine(updated.Directory, "changed-publisher.txt")),
            "Changed publisher content must win a conflict with a local edit.");
        Equal("publisher newly owns this path", File.ReadAllText(Path.Combine(updated.Directory, "future-publisher.txt")),
            "New publisher paths must win conflicts with previously untracked local files.");
        True(!File.Exists(Path.Combine(updated.Directory, "removed-publisher.txt")),
            "Removed publisher files must not be reintroduced as local extras.");
        Equal("publisher file wins", File.ReadAllText(Path.Combine(updated.Directory, "new-publisher-file")),
            "A publisher file must win an ancestor conflict with a local directory.");
        Equal("publisher directory wins", File.ReadAllText(Path.Combine(updated.Directory, "new-publisher-directory", "child.txt")),
            "A publisher directory must win a conflict with a local file.");
        True(!updated.Files.Any(file => file.Path == "Cursor.cfg" || file.Path.StartsWith("Cursors/", StringComparison.Ordinal)),
            "Downloaded provenance must exclude runtime files merged into the working directory.");
        Equal("local removed-publisher edit", File.ReadAllText(Path.Combine(first.Directory, "removed-publisher.txt")),
            "Preparing a new revision must leave the old working generation readable for rollback.");
    }

    private static void WorkshopLegacyUpdateDoesNotInventPublisherHistory()
    {
        using var fixture = new WritableWorkshopFixture();
        var legacy = Path.Combine(fixture.Staged, "42");
        Directory.CreateDirectory(legacy);
        File.WriteAllText(Path.Combine(legacy, "unknown.bin"), "unknown publisher or local content");
        WritableWorkshopFixture.WriteRuntimeFiles(legacy);
        File.WriteAllText(fixture.ManifestPath, JsonSerializer.Serialize(new SteamWorkshopSyncManifest
        {
            Version = 1,
            Items = new() { new() { PublishedFileId = 42, Title = "Legacy", StagedDirectory = legacy, Status = "staged" } },
        }));

        var updated = fixture.Stage().Items.Single().Installed;
        True(!updated.LegacyUnverified, "A complete downloaded replacement must establish publisher provenance.");
        True(!File.Exists(Path.Combine(updated.Directory, "unknown.bin"))
            && !File.Exists(Path.Combine(updated.Directory, "Cursor.cfg")),
            "Unknown legacy history cannot identify safe local overlays by extension or filename.");
        Equal("unknown publisher or local content", File.ReadAllText(Path.Combine(legacy, "unknown.bin")),
            "The unknown legacy generation must stay available as rollback until fresh-start cleanup.");
    }

    private sealed class WritableWorkshopFixture : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "sts2-writable-workshop", Guid.NewGuid().ToString("N"));
        internal string Downloads => Path.Combine(_root, "downloads");
        internal string Source => Path.Combine(Downloads, "42");
        internal string Staged => Path.Combine(_root, "staged");
        internal string ManifestPath => Path.Combine(_root, "workshop.json");
        internal SteamWorkshopStager Stager { get; }

        internal WritableWorkshopFixture()
        {
            Directory.CreateDirectory(Source);
            Stager = new SteamWorkshopStager(Downloads, Staged, ManifestPath);
            Write("CookieCursor.json", "{\"id\":\"FixtureCursor\",\"name\":\"Writable fixture\",\"has_dll\":true,\"has_pck\":false}");
            Write("CookieCursor.dll", "publisher revision one");
            Write("unchanged-publisher.txt", "publisher initial value");
            Write("changed-publisher.txt", "publisher initial value");
            Write("removed-publisher.txt", "publisher initial value");
        }

        internal void Write(string relativePath, string content)
        {
            var path = Path.Combine(Source, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
        }

        internal SteamWorkshopSyncManifest Stage(ulong manifestId = 100, long timeUpdated = 12, ulong hContentFile = 100)
            => Stager.Stage(new[] { new SteamWorkshopItem(42, "Writable fixture", Source, timeUpdated,
                manifestId: manifestId, status: "downloaded", hContentFile: hContentFile) });

        internal LauncherModLaunchPlanResolution Resolve(SteamWorkshopSyncManifest manifest)
        {
            var selection = new LauncherModSelectionDocument
            {
                PlayMode = LauncherModSelectionState.ModdedModeName,
                EnabledMods = new() { ["workshop:42"] = true },
            };
            return LauncherModLaunchPlan.Resolve(selection,
                LauncherModSelectionState.DiscoverKnownMods(selection, manifest, Path.Combine(_root, "manual")));
        }

        internal static void WriteRuntimeFiles(string directory)
        {
            File.WriteAllText(Path.Combine(directory, "Cursor.cfg"), "CursorScale=1.25");
            var images = Path.Combine(directory, "Cursors", "ironclad");
            Directory.CreateDirectory(images);
            File.WriteAllBytes(Path.Combine(images, "icon.png"), new byte[] { 137, 80, 78, 71, 1, 2, 3, 4 });
        }

        internal static void AssertRuntimeFiles(string directory)
        {
            Equal("CursorScale=1.25", File.ReadAllText(Path.Combine(directory, "Cursor.cfg")),
                "Sibling configuration written by a mod must survive sync.");
            True(File.ReadAllBytes(Path.Combine(directory, "Cursors", "ironclad", "icon.png"))
                .SequenceEqual(new byte[] { 137, 80, 78, 71, 1, 2, 3, 4 }),
                "Nested generated cursor assets must survive sync.");
        }

        public void Dispose() => Directory.Delete(_root, recursive: true);
    }
}
