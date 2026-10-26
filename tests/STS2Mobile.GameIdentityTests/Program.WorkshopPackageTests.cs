using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using SteamKit2.Internal;
using STS2Mobile.Steam.Workshop;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void WorkshopRetiredLegacyPackageStaysRetired()
    {
        using var fixture = new WorkshopPackageFixture();
        var legacy = Path.Combine(fixture.Staged, "42");
        Directory.CreateDirectory(legacy);
        File.WriteAllText(Path.Combine(legacy, "mod.dll"), "legacy");
        fixture.WriteLegacyManifest(legacy);
        Equal(1, fixture.Stager.LoadManifest().Items.Count, "Legacy packages must remain available during migration.");
        fixture.Stager.Stage(Array.Empty<SteamWorkshopItem>(), new SteamWorkshopSyncMetadata { SyncStatus = "complete" });
        Equal(0, fixture.Stager.LoadManifest().Items.Count, "A complete discovery must retire a legacy package without re-adopting it on the next read.");
        True(Directory.Exists(legacy), "Retirement must not delete files a running mod may still need.");
    }

    private static void WorkshopLegacyMigrationInventoriesCompanionFiles()
    {
        using var fixture = new WorkshopPackageFixture();
        var legacy = Path.Combine(fixture.Staged, "42");
        Directory.CreateDirectory(Path.Combine(legacy, "versions", "v2"));
        File.WriteAllText(Path.Combine(legacy, "mod.json"), "{}");
        File.WriteAllText(Path.Combine(legacy, "versions", "v2", "mod.dll"), "companion");
        fixture.WriteLegacyManifest(legacy);
        Equal(0, fixture.Stager.LoadManifest().Items.Single().Installed.Files.Count, "Reading inventory metadata for UI must not hash legacy packages.");
        fixture.Stager.CleanupBeforeFirstLoad();
        var installed = fixture.Stager.LoadManifest().Items.Single().Installed;
        True(installed.LegacyUnverified, "Migration cannot invent remote revision provenance.");
        Equal("mod.json,versions/v2/mod.dll", string.Join(",", installed.Files.Select(file => file.Path)), "Migration must inventory all companion files.");
        True(installed.Files.All(file => file.Sha256.Length == 64), "Migration must hash every file.");
        Equal(64, installed.ContentSha256.Length, "Migration must produce a package identity from the actual content.");
    }

    private static void WorkshopPartialDiscoveryRetainsAbsentInstallation()
    {
        using var fixture = new WorkshopPackageFixture();
        var first = fixture.Stage("first");
        var incomplete = fixture.Stager.Stage(Array.Empty<SteamWorkshopItem>(), new SteamWorkshopSyncMetadata { SyncStatus = "dependencies-incomplete" });
        Equal(first.Installed.Directory, incomplete.Items.Single().Installed.Directory, "Partial dependency discovery cannot retire an installed package.");
    }

    private static void WorkshopEmptyUpdatePreservesInstallation()
    {
        using var fixture = new WorkshopPackageFixture();
        var first = fixture.Stage("first");
        File.Delete(Path.Combine(fixture.Source, "mod.dll"));
        var failed = fixture.Stager.Stage(new[] { new SteamWorkshopItem(42, "Fixture", fixture.Source, 2, status: "downloaded") }).Items.Single();
        Equal(first.Installed.Directory, failed.Installed.Directory, "An empty update must not replace a usable package.");
        True(failed.Status.EndsWith("failed", StringComparison.Ordinal), "Empty package preparation must report a failure.");
    }

    private static void WorkshopClearDefersPhysicalDeletion()
    {
        using var fixture = new WorkshopPackageFixture();
        var first = fixture.Stage("first");
        fixture.Stager.ClearStagedMods("test retirement");
        Equal(0, fixture.Stager.LoadManifest().Items.Count, "A clear must immediately retire installed selections.");
        Equal("first", File.ReadAllText(Path.Combine(first.Installed.Directory, "mod.dll")), "A clear must leave companion files available until a fresh process starts.");
        True(File.Exists(Path.Combine(Path.GetDirectoryName(fixture.ManifestPath)!, "last_workshop_mod_clear.txt")), "Custom staging paths must keep their clear marker beside their inventory.");
    }

    private static void WorkshopStartupCleanupKeepsOnlyCommittedGeneration()
    {
        using var fixture = new WorkshopPackageFixture();
        var first = fixture.Stage("first");
        var second = fixture.Stage("second");
        var manual = Path.Combine(fixture.Staged, "manual-content");
        Directory.CreateDirectory(manual);
        File.WriteAllText(Path.Combine(manual, "readme.txt"), "user content");
        fixture.Stager.CleanupBeforeFirstLoad();
        True(!Directory.Exists(first.Installed.Directory), "Fresh-start cleanup must remove an unreferenced old generation.");
        Equal("second", File.ReadAllText(Path.Combine(second.Installed.Directory, "mod.dll")), "Fresh-start cleanup must retain the committed generation.");
        Equal("user content", File.ReadAllText(Path.Combine(manual, "readme.txt")), "Cleanup must leave unmanaged folders alone.");
    }

    private static void WorkshopRevisionRejectsMixedMetadata()
    {
        var detail = new PublishedFileDetails { publishedfileid = 42, result = 1, time_updated = 12, hcontent_file = 100 };
        var info = new CPublishedFile_GetItemInfo_Response.WorkshopItemInfo { published_file_id = 42, time_updated = 13, manifest_id = 101 };
        var rejected = false;
        try { SteamWorkshopRevisionResolver.Resolve(detail, info, "public-beta"); }
        catch (InvalidOperationException) { rejected = true; }
        True(rejected, "A preparation must not combine latest details with an independently fetched different revision.");
    }

    private static void WorkshopRevisionRejectsUnprovenVersionedSelection()
    {
        var detail = new PublishedFileDetails { publishedfileid = 42, result = 1, time_updated = 12, hcontent_file = 100 };
        detail.author_snapshots.Add(new PublishedFileAuthorSnapshot { timestamp = 10, manifestid = 99, game_branch_max = "public" });
        var info = new CPublishedFile_GetItemInfo_Response.WorkshopItemInfo { published_file_id = 42, time_updated = 12, manifest_id = 100 };
        var rejected = false;
        try { SteamWorkshopRevisionResolver.Resolve(detail, info, "public-beta"); }
        catch (InvalidOperationException ex) { rejected = ex.Message.Contains("author snapshot", StringComparison.OrdinalIgnoreCase); }
        True(rejected, "Mismatched author snapshot metadata must fail preparation instead of combining different query revisions.");
    }

    private static void WorkshopRevisionUsesCoherentCurrentMetadata()
    {
        var detail = new PublishedFileDetails { publishedfileid = 42, result = 1, time_updated = 12, hcontent_file = 100 };
        detail.children.Add(new PublishedFileDetails.Child { publishedfileid = 84 });
        var info = new CPublishedFile_GetItemInfo_Response.WorkshopItemInfo { published_file_id = 42, time_updated = 12, manifest_id = 100 };
        var revision = SteamWorkshopRevisionResolver.Resolve(detail, info, "public-beta");
        Equal(100UL, revision.ManifestId, "The selected exact depot manifest must match both responses.");
        Equal(12L, revision.TimeUpdated, "The timestamp must describe the selected content revision.");
        Equal("public-beta", revision.SelectedGameBranch, "Preparation must retain the requested game branch.");
        Equal("84", string.Join(",", revision.RequiredPublishedFileIds), "Dependencies must come from the selected coherent metadata.");
        True(revision.BranchSupport != null && revision.BranchSupport.Ranges.Count == 0
            && !string.IsNullOrWhiteSpace(revision.BranchSupport.Provenance), "Verified empty native range metadata must retain its provenance.");
        True(!string.IsNullOrWhiteSpace(revision.Provenance), "The selected revision must retain its remote evidence provenance.");
    }

    private static void WorkshopReuseUsesCommittedRevisionAfterFailedAttempt()
    {
        using var fixture = new WorkshopPackageFixture();
        File.WriteAllText(Path.Combine(fixture.Source, "mod.dll"), "committed");
        var first = fixture.Stager.Stage(new[] { new SteamWorkshopItem(42, "Fixture", fixture.Source, 12, manifestId: 100, status: "downloaded", hContentFile: 100) }).Items.Single();
        fixture.Stager.Stage(new[] { new SteamWorkshopItem(42, "Fixture", fixture.Source, 13, manifestId: 101, status: "download-failed") });
        var detail = new PublishedFileDetails { publishedfileid = 42, result = 1, title = "Fixture", time_updated = 12, hcontent_file = 100 };
        detail.children.Add(new PublishedFileDetails.Child { publishedfileid = 84 });
        var info = new CPublishedFile_GetItemInfo_Response.WorkshopItemInfo { published_file_id = 42, time_updated = 12, manifest_id = 100 };
        using var downloader = new SteamWorkshopDownloader(null, fixture.Downloads);
        var result = downloader.DownloadAsync(new[] { detail }, new System.Collections.Generic.Dictionary<ulong, CPublishedFile_GetItemInfo_Response.WorkshopItemInfo> { [42] = info }, new ulong[] { 42 }, null,
            "public", fixture.Stager.LoadManifest().Items.ToDictionary(item => item.PublishedFileId)).GetAwaiter().GetResult().Single();
        True(result.ReusedCachedDownload, "A failed last attempt must not prevent reuse of the matching verified committed revision.");
        var reused = fixture.Stager.Stage(new[] { result }).Items.Single().Installed;
        Equal(first.Installed.Directory, reused.Directory, "Reusing the exact committed package must preserve its generation.");
        Equal("84", string.Join(",", reused.RequiredPublishedFileIds), "A successful verified reuse must commit the coherent current dependency metadata.");
    }

    private static void WorkshopRevisionMatchesNativeImplicitCurrent()
    {
        var fixture = WorkshopVersionedMetadata(3787302680, 1789667582, 2965744898046202549,
            new PublishedFileAuthorSnapshot { timestamp = 1789327453, manifestid = 3139509326087545788, game_branch_min = "public", game_branch_max = "public-beta" });
        var revision = SteamWorkshopRevisionResolver.Resolve(fixture.Detail, fixture.Info, "public-beta");
        Equal(2965744898046202549UL, revision.ManifestId, "Native Steam selected Skin Changer's implicit current manifest over its older compatible snapshot.");
        Equal(1789667582L, revision.TimeUpdated, "Implicit current selection must retain the current payload timestamp.");
        Equal(1, revision.BranchSupport.Ranges.Count, "Advisory data must mirror the declared native range list, without adding a synthetic unrestricted range.");
        Equal("public-beta", revision.BranchSupport.Ranges[0].MaximumBranch, "The native range's maximum must remain intact.");
    }

    private static void WorkshopRevisionMatchesNativeHistoricalChoice()
    {
        var fixture = WorkshopVersionedMetadata(3747526116, 1792925268, 8980979945762567250,
            new PublishedFileAuthorSnapshot { timestamp = 1792925067, manifestid = 2942973195224522384, game_branch_min = "public-beta", game_branch_max = "public-beta" },
            new PublishedFileAuthorSnapshot { timestamp = 1792925268, manifestid = 8980979945762567250, game_branch_min = "public", game_branch_max = "public" },
            new PublishedFileAuthorSnapshot { timestamp = 1792905758, manifestid = 7679835603836050487, game_branch_min = "public-beta", game_branch_max = "public-beta" },
            new PublishedFileAuthorSnapshot { timestamp = 1792905827, manifestid = 5620824005888730669, game_branch_min = "public", game_branch_max = "public" });
        fixture.Detail.children.Add(new PublishedFileDetails.Child { publishedfileid = 84 });
        var revision = SteamWorkshopRevisionResolver.Resolve(fixture.Detail, fixture.Info, "public-beta");
        Equal(2942973195224522384UL, revision.ManifestId, "Native Steam selected Watcher's newest compatible beta snapshot, independently of list order.");
        Equal(1792925067L, revision.TimeUpdated, "Historical selection must use the selected snapshot timestamp, as native install info does.");
        Equal(0UL, revision.HContentFile, "Historical selection must not retain the latest payload's UGC content handle.");
        Equal("84", string.Join(",", revision.RequiredPublishedFileIds), "Workshop dependency edges belong to published items and remain authoritative for historical payloads.");
    }

    private static void WorkshopRevisionCachesNativeMappedRanges()
    {
        var fixture = WorkshopVersionedMetadata(3747595039, 1789493401, 7570153834481887614,
            new PublishedFileAuthorSnapshot { timestamp = 1789493167, manifestid = 3764235348225200361, game_branch_min = "public", game_branch_max = "public" },
            new PublishedFileAuthorSnapshot { timestamp = 1789493401, manifestid = 7570153834481887614, game_branch_min = "public-beta", game_branch_max = "public-beta" });
        var revision = SteamWorkshopRevisionResolver.Resolve(fixture.Detail, fixture.Info, "public-beta");
        Equal(7570153834481887614UL, revision.ManifestId, "An explicit compatible current snapshot must remain selectable.");
        Equal(2, revision.BranchSupport.Ranges.Count, "Cached advisory ranges must match native GetSupportedGameVersionData's count.");
        Equal("public", revision.BranchSupport.Ranges[0].MinimumBranch, "Advisory ranges must preserve native source order.");
        Equal("public-beta", revision.BranchSupport.Ranges[1].MaximumBranch, "Advisory ranges must preserve native bounds.");
        True(revision.BranchSupport.Provenance.Contains("native", StringComparison.OrdinalIgnoreCase), "Cached ranges must identify the verified native protocol mapping.");
    }

    private static void WorkshopRevisionRejectsNoCompatibleSnapshot()
    {
        var fixture = WorkshopVersionedMetadata(42, 20, 100,
            new PublishedFileAuthorSnapshot { timestamp = 20, manifestid = 100, game_branch_min = "public", game_branch_max = "public" });
        var rejected = false;
        try { SteamWorkshopRevisionResolver.Resolve(fixture.Detail, fixture.Info, "public-beta"); }
        catch (InvalidOperationException ex) { rejected = ex.Message.Contains("compatible", StringComparison.OrdinalIgnoreCase); }
        True(rejected, "An explicitly restricted current payload must not become an unrestricted latest fallback.");
    }

    private static void WorkshopRevisionUsesVerifiedNativeBranchOrder()
    {
        var fixture = WorkshopVersionedMetadata(42, 20, 100,
            new PublishedFileAuthorSnapshot { timestamp = 20, manifestid = 100, game_branch_min = "version_z", game_branch_max = "version_a" });
        var revision = SteamWorkshopRevisionResolver.Resolve(fixture.Detail, fixture.Info, "version_m",
            new SteamWorkshopBranchOrder(new[] { "version_z", "version_m", "version_a" }, "test:verified-publisher-order", isVerified: true));
        Equal(100UL, revision.ManifestId, "Inclusive bounds must follow the supplied verified publisher order, including a nonalphabetical order.");
        Equal("version_z,version_m,version_a", string.Join(",", revision.BranchSupport.BranchOrder), "Cached branch evidence must preserve the supplied verified order.");
        True(revision.BranchSupport.Provenance.Contains("test:verified-publisher-order", StringComparison.Ordinal), "Provenance must retain provider verification evidence.");
    }

    private static void WorkshopRevisionRejectsUnknownRangeOrder()
    {
        var fixture = WorkshopVersionedMetadata(42, 20, 100,
            new PublishedFileAuthorSnapshot { timestamp = 20, manifestid = 100, game_branch_min = "version_a", game_branch_max = "version_z" });
        var rejected = false;
        try { SteamWorkshopRevisionResolver.Resolve(fixture.Detail, fixture.Info, "version_m",
            new SteamWorkshopBranchOrder(new[] { "version_a", "version_m", "version_z" }, "test:raw-order", isVerified: false)); }
        catch (InvalidOperationException ex) { rejected = ex.Message.Contains("order", StringComparison.OrdinalIgnoreCase); }
        True(rejected, "Unverified raw or alphabetical branch order cannot establish an interval's compatibility.");
        fixture = WorkshopVersionedMetadata(42, 20, 100,
            new PublishedFileAuthorSnapshot { timestamp = 20, manifestid = 100, game_branch_min = "version_a", game_branch_max = "missing_bound" });
        rejected = false;
        try { SteamWorkshopRevisionResolver.Resolve(fixture.Detail, fixture.Info, "version_m",
            new SteamWorkshopBranchOrder(new[] { "version_a", "version_m", "version_z" }, "test:verified-order", isVerified: true)); }
        catch (InvalidOperationException ex) { rejected = ex.Message.Contains("unknown bounds", StringComparison.OrdinalIgnoreCase); }
        True(rejected, "Verified ordering cannot supply a missing range bound.");
    }

    private static void WorkshopRevisionRejectsAmbiguousSnapshotTimestamp()
    {
        var fixture = WorkshopVersionedMetadata(42, 21, 100,
            new PublishedFileAuthorSnapshot { timestamp = 21, manifestid = 100, game_branch_min = "public", game_branch_max = "public" },
            new PublishedFileAuthorSnapshot { timestamp = 20, manifestid = 98, game_branch_min = "public-beta", game_branch_max = "public-beta" },
            new PublishedFileAuthorSnapshot { timestamp = 20, manifestid = 99, game_branch_min = "public-beta", game_branch_max = "public-beta" });
        var rejected = false;
        try { SteamWorkshopRevisionResolver.Resolve(fixture.Detail, fixture.Info, "public-beta"); }
        catch (InvalidOperationException ex) { rejected = ex.Message.Contains("ambiguous", StringComparison.OrdinalIgnoreCase); }
        True(rejected, "Equal newest timestamps for different compatible payloads must fail explicitly.");
    }

    private static (PublishedFileDetails Detail, CPublishedFile_GetItemInfo_Response.WorkshopItemInfo Info) WorkshopVersionedMetadata(
        ulong id, uint timestamp, ulong manifest, params PublishedFileAuthorSnapshot[] snapshots)
    {
        var detail = new PublishedFileDetails { publishedfileid = id, result = 1, time_updated = timestamp, hcontent_file = manifest };
        var info = new CPublishedFile_GetItemInfo_Response.WorkshopItemInfo { published_file_id = id, time_updated = timestamp, manifest_id = manifest };
        foreach (var snapshot in snapshots)
        {
            detail.author_snapshots.Add(snapshot);
            info.author_snapshots.Add(new PublishedFileAuthorSnapshot { timestamp = snapshot.timestamp, manifestid = snapshot.manifestid, game_branch_min = snapshot.game_branch_min, game_branch_max = snapshot.game_branch_max });
        }
        return (detail, info);
    }

    private sealed class WorkshopPackageFixture : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "sts2-workshop-packages", Guid.NewGuid().ToString("N"));
        internal string Downloads { get; }
        internal string Source { get; }
        internal string Staged { get; }
        internal string ManifestPath { get; }
        internal SteamWorkshopStager Stager { get; }
        internal WorkshopPackageFixture()
        {
            Downloads = Path.Combine(_root, "downloads");
            Source = Path.Combine(Downloads, "42");
            Staged = Path.Combine(_root, "staged");
            ManifestPath = Path.Combine(_root, "workshop.json");
            Directory.CreateDirectory(Source);
            Directory.CreateDirectory(Staged);
            Stager = new SteamWorkshopStager(Downloads, Staged, ManifestPath);
        }
        internal SteamWorkshopSyncManifestItem Stage(string contents)
        {
            File.WriteAllText(Path.Combine(Source, "mod.dll"), contents);
            return Stager.Stage(new[] { new SteamWorkshopItem(42, "Fixture", Source, 1, status: "downloaded") }).Items.Single();
        }
        internal void WriteLegacyManifest(string directory)
            => File.WriteAllText(ManifestPath, JsonSerializer.Serialize(new SteamWorkshopSyncManifest
            {
                Version = 1,
                Items = new() { new() { PublishedFileId = 42, Title = "Legacy", StagedDirectory = directory, Status = "staged" } },
            }));
        public void Dispose() => Directory.Delete(_root, recursive: true);
    }
}
