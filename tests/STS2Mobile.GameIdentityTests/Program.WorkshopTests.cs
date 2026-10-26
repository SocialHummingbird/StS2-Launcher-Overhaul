using System;
using System.IO;
using System.Linq;
using STS2Mobile.Steam.Workshop;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void WorkshopFailedUpdatePreservesCommittedPackage()
    {
        using var fixture = new WorkshopFixture();
        var first = fixture.Stage("first");
        var failed = fixture.Stager.Stage(new[] { new SteamWorkshopItem(42, "Fixture", fixture.Source, 2, status: "download-failed", error: "interrupted") });
        Equal("first", File.ReadAllText(Path.Combine(first.StagedDirectory, "payload.txt")), "A failed update must not delete the committed package.");
        Equal(first.StagedDirectory, failed.Items.Single().StagedDirectory, "Selection must retain its committed physical path.");
        Equal("download-failed", failed.Items.Single().Status, "The failed update must remain visible.");
    }

    private static void WorkshopDiscoveryFailurePreservesCommittedPackage()
    {
        using var fixture = new WorkshopFixture();
        var first = fixture.Stage("first");
        fixture.Stager.SaveFailure(new SteamWorkshopSyncMetadata { SyncStatus = "discovery-failed", SyncError = "offline" });
        var loaded = fixture.Stager.LoadManifest();
        Equal(1, loaded.Items.Count, "Discovery failure must not erase the installed inventory.");
        Equal(first.StagedDirectory, loaded.Items.Single().StagedDirectory, "Discovery failure must preserve the selected generation.");
        Equal("discovery-failed", loaded.SyncStatus, "Failure must be reported separately from installation.");
    }

    private static void WorkshopReplacementKeepsPreviousGenerationReadable()
    {
        using var fixture = new WorkshopFixture();
        var first = fixture.Stage("first");
        Directory.CreateDirectory(Path.Combine(fixture.Source, "versions", "v2"));
        File.WriteAllText(Path.Combine(fixture.Source, "versions", "v2", "implementation.dll"), "secondary");
        var second = fixture.Stage("second");
        True(first.StagedDirectory != second.StagedDirectory, "An update must commit a distinct generation directory.");
        Equal("first", File.ReadAllText(Path.Combine(first.StagedDirectory, "payload.txt")), "Loaded mods may still need the previous generation's companion files.");
        Equal("second", File.ReadAllText(Path.Combine(second.StagedDirectory, "payload.txt")), "New launches must see the committed update.");
        Equal("secondary", File.ReadAllText(Path.Combine(second.StagedDirectory, "versions", "v2", "implementation.dll")), "Nested companion files must survive staging.");
    }

    private sealed class WorkshopFixture : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "sts2-workshop-tests", Guid.NewGuid().ToString("N"));
        internal string Source { get; }
        internal SteamWorkshopStager Stager { get; }
        internal WorkshopFixture()
        {
            var downloads = Path.Combine(_root, "downloads");
            Source = Path.Combine(downloads, "42");
            Directory.CreateDirectory(Source);
            Stager = new SteamWorkshopStager(downloads, Path.Combine(_root, "staged"), Path.Combine(_root, "workshop.json"));
        }
        internal SteamWorkshopSyncManifestItem Stage(string contents)
        {
            File.WriteAllText(Path.Combine(Source, "payload.txt"), contents);
            return Stager.Stage(new[] { new SteamWorkshopItem(42, "Fixture", Source, 1, status: "downloaded") }).Items.Single();
        }
        public void Dispose() => Directory.Delete(_root, recursive: true);
    }
}
