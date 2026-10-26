using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using STS2Mobile.Steam.Workshop;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void WorkshopInvalidInventoryCannotAuthorizeCleanup()
    {
        foreach (var invalid in new[] { "{broken", "{\"Version\":999,\"Items\":[]}", "{}", "null" })
        {
            using var fixture = new WorkshopRecoveryFixture();
            var installed = fixture.Seed(fixture.Item(42, "committed")).Items.Single().Installed;
            File.WriteAllText(fixture.ManifestPath, invalid);
            WorkshopInventoryMustFail(() => fixture.Stager.CleanupBeforeFirstLoad());
            Equal(invalid, File.ReadAllText(fixture.ManifestPath), "Startup cleanup must preserve invalid inventory bytes.");
            Equal("committed", File.ReadAllText(Path.Combine(installed.Directory, "mod.dll")), "Invalid inventory cannot authorize deletion of a committed generation.");
        }
    }

    private static void WorkshopMissingInventoryCannotEraseGenerations()
    {
        using var fixture = new WorkshopRecoveryFixture();
        var installed = fixture.Seed(fixture.Item(42, "committed")).Items.Single().Installed;
        File.Delete(fixture.ManifestPath);
        WorkshopInventoryMustFail(() => fixture.Stager.CleanupBeforeFirstLoad());
        True(!File.Exists(fixture.ManifestPath), "Missing inventory with existing generations must not become an invented empty inventory.");
        Equal("committed", File.ReadAllText(Path.Combine(installed.Directory, "mod.dll")), "A missing inventory cannot authorize generation deletion.");
    }

    private static void WorkshopReadFailureCannotAuthorizeCleanup()
    {
        using var fixture = new WorkshopRecoveryFixture();
        var installed = fixture.Seed(fixture.Item(42, "committed")).Items.Single().Installed;
        var original = File.ReadAllText(fixture.ManifestPath);
        // Deny reads while permitting replacement, reproducing the dangerous
        // distinction between an unreadable inventory and a failed save.
        using (var locked = new FileStream(fixture.ManifestPath, FileMode.Open, FileAccess.Read, FileShare.Delete))
            WorkshopInventoryMustFail(() => fixture.Stager.CleanupBeforeFirstLoad());
        Equal(original, File.ReadAllText(fixture.ManifestPath), "A read failure must not publish an empty replacement.");
        Equal("committed", File.ReadAllText(Path.Combine(installed.Directory, "mod.dll")), "A read failure cannot authorize deletion of installed files.");
    }

    private static void WorkshopSaveFailurePreservesInvalidInventory()
    {
        using var fixture = new WorkshopRecoveryFixture();
        var installed = fixture.Seed(fixture.Item(42, "committed")).Items.Single().Installed;
        const string invalid = "{\"Version\":999,\"Items\":[]}";
        File.WriteAllText(fixture.ManifestPath, invalid);
        WorkshopInventoryMustFail(() => fixture.Stager.SaveFailure(new SteamWorkshopSyncMetadata { SyncStatus = "discovery-failed", SyncError = "offline" }));
        Equal(invalid, File.ReadAllText(fixture.ManifestPath), "Reporting a sync failure must preserve unreadable inventory bytes.");
        Equal("committed", File.ReadAllText(Path.Combine(installed.Directory, "mod.dll")), "Reporting a failure must preserve committed packages.");
    }

    private static void WorkshopInventoryRepairRequiresSuccessfulAuthoritativePreparation()
    {
        using var fixture = new WorkshopRecoveryFixture();
        var installed = fixture.Seed(fixture.Item(42, "committed")).Items.Single().Installed;
        const string invalid = "{broken";
        File.WriteAllText(fixture.ManifestPath, invalid);
        WorkshopInventoryMustFail(() => fixture.Stager.Stage(new[] { new SteamWorkshopItem(42, "A", fixture.Source(42), 2, status: "download-failed", error: "offline") }, WorkshopRecoveryFixture.Complete));
        Equal(invalid, File.ReadAllText(fixture.ManifestPath), "Failed repair must preserve invalid inventory bytes.");
        WorkshopInventoryMustFail(() => fixture.Stager.Stage(new[] { fixture.Item(42, "partial") }, new SteamWorkshopSyncMetadata { SyncStatus = "dependencies-incomplete" }));
        Equal(invalid, File.ReadAllText(fixture.ManifestPath), "Partial discovery cannot replace an unknown inventory.");
        var empty = fixture.Item(42, "empty");
        File.Delete(Path.Combine(fixture.Source(42), "mod.dll"));
        WorkshopInventoryMustFail(() => fixture.Stager.Stage(new[] { empty }, WorkshopRecoveryFixture.Complete));
        Equal(invalid, File.ReadAllText(fixture.ManifestPath), "A staging failure cannot publish inventory repair.");
        Equal("committed", File.ReadAllText(Path.Combine(installed.Directory, "mod.dll")), "Every failed repair must preserve the old committed payload.");
        var repaired = fixture.Seed(fixture.Item(42, "repaired"));
        Equal("repaired", File.ReadAllText(Path.Combine(repaired.Items.Single().Installed.Directory, "mod.dll")), "A fully successful authoritative preparation must repair inventory without user file edits.");
        Equal(1, fixture.Stager.LoadManifest().Items.Count, "The repaired inventory must be readable.");
        True(Directory.Exists(installed.Directory), "Repair must defer old generation deletion until fresh startup.");
        fixture.Stager.CleanupBeforeFirstLoad();
        True(!Directory.Exists(installed.Directory), "A committed successful repair may retire old unreferenced generations on the next startup.");
    }

    private static void WorkshopFailedUpdatePreservesTransitiveCommittedDependencies()
    {
        using var fixture = new WorkshopRecoveryFixture();
        var original = fixture.Seed(fixture.Item(42, "A-old", 84), fixture.Item(84, "B-old", 126), fixture.Item(126, "D-old"));
        var oldB = original.Items.Single(item => item.PublishedFileId == 84).Installed.Directory;
        var oldD = original.Items.Single(item => item.PublishedFileId == 126).Installed.Directory;
        var failedA = new SteamWorkshopItem(42, "A", fixture.Source(42), 2, status: "download-failed", error: "interrupted", requiredPublishedFileIds: new ulong[] { 168 });
        var update = fixture.Seed(failedA, fixture.Item(168, "C-new"));
        Equal("42,84,126,168", string.Join(",", update.Items.Select(item => item.PublishedFileId).Order()), "Retirement must retain the transitive dependency closure of A's committed generation.");
        fixture.Stager.CleanupBeforeFirstLoad();
        Equal("B-old", File.ReadAllText(Path.Combine(oldB, "mod.dll")), "A failed A update must keep B readable after startup cleanup.");
        Equal("D-old", File.ReadAllText(Path.Combine(oldD, "mod.dll")), "B's transitive committed dependency must also survive cleanup.");
        var replacement = fixture.Seed(fixture.Item(42, "A-new", 168), fixture.Item(168, "C-new"));
        Equal("42,168", string.Join(",", replacement.Items.Select(item => item.PublishedFileId).Order()), "A successful A replacement may retire dependencies no committed source needs.");
        fixture.Stager.CleanupBeforeFirstLoad();
        True(!Directory.Exists(oldB) && !Directory.Exists(oldD), "Unreferenced B and D may be deleted only after the replacement commits.");
    }

    private static void WorkshopInventoryMustFail(Action action)
    {
        var failed = false;
        try { action(); }
        catch (IOException) { failed = true; }
        True(failed, "An unreadable or unknown inventory must report a failure instead of becoming known-empty.");
    }

    private sealed class WorkshopRecoveryFixture : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "sts2-workshop-recovery", Guid.NewGuid().ToString("N"));
        internal static SteamWorkshopSyncMetadata Complete => new() { SyncStatus = "complete" };
        internal string Downloads { get; }
        internal string ManifestPath { get; }
        internal SteamWorkshopStager Stager { get; }
        internal WorkshopRecoveryFixture()
        {
            Downloads = Path.Combine(_root, "downloads");
            ManifestPath = Path.Combine(_root, "workshop.json");
            Directory.CreateDirectory(Downloads);
            Stager = new SteamWorkshopStager(Downloads, Path.Combine(_root, "staged"), ManifestPath);
        }
        internal string Source(ulong id) => Path.Combine(Downloads, id.ToString());
        internal SteamWorkshopItem Item(ulong id, string content, params ulong[] required)
        {
            Directory.CreateDirectory(Source(id));
            File.WriteAllText(Path.Combine(Source(id), "mod.dll"), content);
            return new SteamWorkshopItem(id, id.ToString(), Source(id), 1, status: "downloaded", requiredPublishedFileIds: required);
        }
        internal SteamWorkshopSyncManifest Seed(params SteamWorkshopItem[] items) => Stager.Stage(items, Complete);
        public void Dispose() => Directory.Delete(_root, recursive: true);
    }
}
