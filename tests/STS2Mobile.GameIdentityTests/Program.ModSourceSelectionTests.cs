using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using STS2Mobile.Launcher;
using STS2Mobile.Steam.Workshop;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void ModSourceEmptySelectionPreservesModeAndChoices()
    {
        var selection = new LauncherModSelectionDocument
        {
            PlayMode = LauncherModSelectionState.ModdedModeName,
            EnabledMods = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                ["manual:disabled"] = false,
            },
        };
        var resolution = LauncherModLaunchPlan.Resolve(selection, new[]
        {
            new LauncherKnownMod { Key = "manual:disabled", Path = "missing-source", ManifestPath = "missing-source/fixture.json" },
        });
        True(resolution.Success, "Modded with no enabled sources must reach normal framework initialization.");
        Equal(LauncherModPlayMode.Modded, resolution.Plan.Mode, "An empty selection must retain the user's play mode.");
        True(resolution.Plan.Sources.IsEmpty && resolution.Plan.Roots.IsEmpty, "Disabled sources must not be prepared or loaded.");
        Equal(1, selection.EnabledMods.Count, "Preparation must preserve disabled choices.");
        True(!selection.EnabledMods["manual:disabled"], "Preparation must not enable a source to satisfy an inherited policy.");
    }

    private static void ModSourceWorkshopPackageReachesNativeDiscovery()
    {
        using var fixture = new ModSourceFixture();
        fixture.Write("first.json", "{\"id\":\"first\",\"name\":\"First\",\"version\":\"1\",\"has_dll\":true,\"has_pck\":false}");
        fixture.Write("first.dll", "first");
        fixture.Write("second.json", "{\"id\":\"second\",\"name\":\"Second\",\"version\":\"1\",\"has_dll\":true,\"has_pck\":false}");
        fixture.Write("second.dll", "second");
        var selection = ModSourceSelection("workshop:42");
        var manifest = fixture.Stage();
        var resolution = LauncherModLaunchPlan.Resolve(selection,
            LauncherModSelectionState.DiscoverKnownMods(selection, manifest, fixture.ManualRoot));
        True(resolution.Success, $"A complete Workshop package must reach native manifest discovery: {resolution.Error?.Message}");
        Equal(1, resolution.Plan.Roots.Length, "One Workshop key must select its complete package root.");
        Equal((ulong?)42, resolution.Plan.Sources.Single().WorkshopId, "Native Workshop discovery needs the actual Workshop item identity.");
        True(resolution.Plan.Sources.Single().ManifestPath == null, "Workshop selection must leave every package manifest to native discovery.");
    }

    private static void ModSourceCommittedCompanionEditsChangeConfiguration()
    {
        using var fixture = new ModSourceFixture();
        fixture.WriteValidManifest();
        fixture.Write("translations/en.txt", "before");
        var manifest = fixture.Stage();
        var selection = ModSourceSelection("workshop:42");
        var before = LauncherModLaunchPlan.Resolve(selection,
            LauncherModSelectionState.DiscoverKnownMods(selection, manifest, fixture.ManualRoot));
        File.WriteAllText(Path.Combine(manifest.Items.Single().StagedDirectory, "translations", "en.txt"), "after!");
        var resolution = LauncherModLaunchPlan.Resolve(selection,
            LauncherModSelectionState.DiscoverKnownMods(selection, manifest, fixture.ManualRoot));
        True(resolution.Success, "A writable Workshop companion must remain available to native loading.");
        True(before.Plan.Fingerprint != resolution.Plan.Fingerprint,
            "A changed Workshop companion must invalidate the prior configuration.");
    }

    private static void ModSourceManualDiscoveryHasNoArbitraryLimit()
    {
        using var fixture = new ModSourceFixture();
        Directory.CreateDirectory(fixture.ManualRoot);
        for (var i = 0; i < 70; i++)
            File.WriteAllText(Path.Combine(fixture.ManualRoot, $"manual{i}.json"), "{\"name\":\"Native defaults\"}");
        var discovered = LauncherModSelectionState.DiscoverKnownMods(
            new LauncherModSelectionDocument(), new SteamWorkshopSyncManifest(), fixture.ManualRoot);
        Equal(70, discovered.Count, "Manual source discovery must leave schema/defaults and inventory size to native discovery.");
    }

    private static void ModSourceWorkshopDependenciesUseCommittedEdges()
    {
        using var fixture = new ModSourceFixture();
        fixture.WriteValidManifest();
        var manifest = fixture.Stage();
        manifest.Items[0].Installed.RequiredPublishedFileIds.Add(43);
        manifest.Items.Add(new SteamWorkshopSyncManifestItem
        {
            PublishedFileId = 43,
            Title = "Subscribed library",
            IsDependency = false,
            Status = "staged",
            Installed = new SteamWorkshopInstallation { Directory = fixture.Source, LegacyUnverified = true },
            StagedDirectory = fixture.Source,
        });
        var selection = ModSourceSelection("workshop:42");
        var discovered = LauncherModSelectionState.DiscoverKnownMods(selection, manifest, fixture.ManualRoot);
        var library = discovered.Single(mod => mod.Key == "workshop:43");
        True(library.Enabled && library.IsRequiredDependency,
            "A subscribed library must follow its selected parent's committed required edge regardless of IsDependency.");
        selection.EnabledMods["workshop:43"] = true;
        library = LauncherModSelectionState.DiscoverKnownMods(selection, manifest, fixture.ManualRoot)
            .Single(mod => mod.Key == "workshop:43");
        True(library.Enabled && !library.IsRequiredDependency,
            "An explicitly selected required library must remain independently toggleable.");
    }

    private static void ModSourceManualSelectionPreservesExactPathAndNativeErrors()
    {
        using var fixture = new ModSourceFixture();
        Directory.CreateDirectory(fixture.ManualRoot);
        var selectedManifest = Path.Combine(fixture.ManualRoot, "UnifiedSavePath.json");
        File.WriteAllText(selectedManifest, "{ malformed");
        File.WriteAllText(Path.Combine(fixture.ManualRoot, "other.json"), "{\"id\":\"unselected\"}");
        var key = $"manual:{Path.GetFullPath(fixture.ManualRoot)}:UnifiedSavePath";
        var selection = ModSourceSelection(key);
        var sources = LauncherModSelectionState.DiscoverKnownMods(selection, null!, fixture.ManualRoot);
        var resolution = LauncherModLaunchPlan.Resolve(selection, sources);
        True(resolution.Success, "Selection must pass an available manual file through to native parsing, including its schema errors.");
        Equal(1, resolution.Plan.Sources.Length, "Selecting one JSON must not select its sibling manifest.");
        Equal(selectedManifest, resolution.Plan.Sources.Single().ManifestPath, "The frozen manual source must retain its exact JSON path.");
        Equal(key, resolution.Plan.Sources.Single().SelectionKey, "Version-one manual keys must retain the filename stem.");
    }

    private static void ModSourceMutableCompanionsChangeFreshFingerprint()
    {
        using var fixture = new ModSourceFixture();
        Directory.CreateDirectory(fixture.ManualRoot);
        File.WriteAllText(Path.Combine(fixture.ManualRoot, "fixture.json"), "{}");
        var companion = Path.Combine(fixture.ManualRoot, "localization.txt");
        File.WriteAllText(companion, "before");
        var timestamp = File.GetLastWriteTimeUtc(companion);
        var selection = ModSourceSelection($"manual:{Path.GetFullPath(fixture.ManualRoot)}:fixture");
        var known = LauncherModSelectionState.DiscoverKnownMods(selection, null!, fixture.ManualRoot);
        var first = LauncherModLaunchPlan.Resolve(selection, known);
        True(first.Success, "The first manual source must prepare.");
        var firstGeneration = first.Plan.Sources.Single().InstalledGeneration;
        File.WriteAllText(companion, "after!");
        File.SetLastWriteTimeUtc(companion, timestamp);
        var second = LauncherModLaunchPlan.Resolve(selection, known);
        True(second.Success, "The edited mutable manual source must prepare afresh.");
        True(first.Plan.Fingerprint != second.Plan.Fingerprint,
            "A same-size, same-time companion edit must invalidate last launch configuration.");
        Equal(firstGeneration, first.Plan.Sources.Single().InstalledGeneration,
            "Preparing a later launch must not mutate a prior frozen source.");
        True(firstGeneration != second.Plan.Sources.Single().InstalledGeneration,
            "Mutable source generations must come from the complete launch-time content inventory.");
    }

    private static void ModSourceFailedUpdateRetainsInstalledSelection()
    {
        using var fixture = new ModSourceFixture();
        fixture.WriteValidManifest();
        var manifest = fixture.Stage();
        var generation = manifest.Items.Single().Installed.Generation;
        manifest.Items.Single().Status = "download-failed";
        manifest.Items.Single().Error = "offline";
        var selection = ModSourceSelection("workshop:42");
        var discovered = LauncherModSelectionState.DiscoverKnownMods(selection, manifest, fixture.ManualRoot);
        True(discovered.Single().Enabled && !discovered.Single().IsUnsupported,
            "Latest sync failure must not hide a retained committed installation.");
        var resolution = LauncherModLaunchPlan.Resolve(selection, discovered);
        True(resolution.Success, "An intact installed generation must remain selectable after a failed update.");
        Equal(generation, resolution.Plan.Sources.Single().InstalledGeneration,
            "Selection must preserve the installed generation rather than attempted sync metadata.");
    }

    private static void ModSourcePreparationPreservesCompletionEvidence()
    {
        using var fixture = new ModSourceFixture();
        fixture.WriteValidManifest();
        var manifest = fixture.Stage();
        var manifestPath = AppPaths.WorkshopManifestPath(fixture.DataRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest));
        var markerPath = AppPaths.LastModLaunchPath(fixture.DataRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(markerPath)!);
        const string previousCompletion = "previous completed launch evidence";
        File.WriteAllText(markerPath, previousCompletion);
        var previousPreview = Environment.GetEnvironmentVariable("STS2_LAUNCHER_PREVIEW");
        var previousData = Environment.GetEnvironmentVariable(AppPaths.LauncherPreviewDataDirEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable("STS2_LAUNCHER_PREVIEW", "1");
            Environment.SetEnvironmentVariable(AppPaths.LauncherPreviewDataDirEnvironmentVariable, fixture.DataRoot);
            Equal(manifestPath, AppPaths.AppPrivateWorkshopManifestPath, "The fixture must use the isolated Workshop manifest.");
            Equal(1, new SteamWorkshopStager().LoadManifest().Items.Count, "The isolated readiness fixture must have one installed source.");
            LauncherModLaunchReadiness.Evaluate("source preparation fixture", ModSourceSelection("workshop:42"));
            Equal(previousCompletion, File.ReadAllText(markerPath), "Successful preparation must preserve the previous completed launch.");
            LauncherModLaunchReadiness.Evaluate("source preparation fixture", new LauncherModSelectionDocument());
            Equal(previousCompletion, File.ReadAllText(markerPath), "Vanilla preparation must preserve the previous completed launch.");
            var failed = false;
            try
            {
                LauncherModLaunchReadiness.Evaluate("source preparation fixture", ModSourceSelection("workshop:missing"));
            }
            catch (InvalidDataException)
            {
                failed = true;
            }
            True(failed, "An unavailable selected source must fail preparation.");
            Equal(previousCompletion, File.ReadAllText(markerPath), "Failed preparation must preserve the previous completed launch.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("STS2_LAUNCHER_PREVIEW", previousPreview);
            Environment.SetEnvironmentVariable(AppPaths.LauncherPreviewDataDirEnvironmentVariable, previousData);
        }
    }

    private static LauncherModSelectionDocument ModSourceSelection(params string[] keys)
        => new()
        {
            PlayMode = LauncherModSelectionState.ModdedModeName,
            EnabledMods = keys.ToDictionary(key => key, _ => true, StringComparer.OrdinalIgnoreCase),
        };

    private sealed class ModSourceFixture : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "sts2-mod-sources", Guid.NewGuid().ToString("N"));
        internal string DataRoot => _root;
        internal string Source { get; }
        internal string ManualRoot => Path.Combine(_root, "manual");
        private SteamWorkshopStager Stager { get; }

        internal ModSourceFixture()
        {
            var downloads = Path.Combine(_root, "downloads");
            Source = Path.Combine(downloads, "42");
            Directory.CreateDirectory(Source);
            Stager = new SteamWorkshopStager(downloads, Path.Combine(_root, "staged"), Path.Combine(_root, "workshop.json"));
        }

        internal void Write(string path, string content)
        {
            var fullPath = Path.Combine(Source, path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, content);
        }

        internal void WriteValidManifest()
        {
            Write("fixture.json", "{\"id\":\"fixture\",\"name\":\"Fixture\",\"version\":\"1\",\"has_dll\":true,\"has_pck\":false}");
            Write("fixture.dll", "payload");
        }

        internal SteamWorkshopSyncManifest Stage()
            => Stager.Stage(new[] { new SteamWorkshopItem(42, "Fixture", Source, 1, status: "downloaded") });

        public void Dispose() => Directory.Delete(_root, recursive: true);
    }
}
