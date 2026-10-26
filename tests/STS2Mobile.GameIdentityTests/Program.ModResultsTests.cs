using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using STS2Mobile.Launcher;
using STS2Mobile.Steam.Workshop;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void ModResultsColdManualUsesObservedConfiguration()
    {
        using var fixture = new ModResultsCurrentStateFixture(manual: true);
        var presentation = LauncherModsPresentationState.ReadCurrent(fixture.ManualRoot);
        True(presentation.Mods.Single().LastLaunchState == LauncherModLastLaunchState.LoadedLastLaunch
            && !presentation.HasStaleLastLaunchResult,
            "An unchanged manual package must recover its completed native outcome after cold reopen without a prepared launch.");
        File.WriteAllText(Path.Combine(fixture.ManualRoot, "companion.cfg"), "changed configuration");
        True(LauncherModsPresentationState.ReadCurrent(fixture.ManualRoot).HasStaleLastLaunchResult,
            "A changed manual companion must invalidate the previously completed configuration.");
    }

    private static void ModResultsColdLegacyUsesObservedConfiguration()
    {
        using var fixture = new ModResultsCurrentStateFixture(legacy: true);
        var presentation = LauncherModsPresentationState.ReadCurrent();
        True(presentation.Mods.All(mod => mod.LastLaunchState == LauncherModLastLaunchState.LoadedLastLaunch)
            && !presentation.HasStaleLastLaunchResult,
            "Unchanged legacy packages must recover completed outcomes from a fresh observed configuration after cold reopen.");
    }

    private static void ModResultsMutableWorkshopCompanionsInvalidateConfiguration()
    {
        foreach (var companion in new[] { "Cursor.cfg", Path.Combine("Cursors", "custom.png") })
        {
            using var fixture = new ModResultsCurrentStateFixture();
            True(!LauncherModsPresentationState.ReadCurrent().HasStaleLastLaunchResult,
                "The unchanged Workshop configuration must match before a companion changes.");
            var installation = new SteamWorkshopStager().LoadManifest().Items.First().Installed;
            var generation = installation.Generation;
            var path = Path.Combine(installation.Directory, companion);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "mod-written companion");
            var changed = LauncherModsPresentationState.ReadCurrent();
            True(changed.HasStaleLastLaunchResult && changed.Mods.All(mod => mod.LastLaunchState == LauncherModLastLaunchState.NotTestedYet),
                "Changed Workshop settings or asset companions must invalidate the completed live configuration even within one committed generation.");
            Equal(generation, new SteamWorkshopStager().LoadManifest().Items.First().Installed.Generation,
                "Observing a mutable companion must not rewrite the committed downloaded revision.");
        }
    }

    private static void ModResultsReadCurrentRetainsCompletedWorkshopLaunch()
    {
        using var fixture = new ModResultsCurrentStateFixture();
        True(BranchInstallStateStore.Current.Read(fixture.DataDir, fixture.Branch).RuntimePack == null,
            "Normal install completion stores runtime-pack identity separately from installation state.");
        var paths = new[] { fixture.RuntimeManifest, fixture.InstallationState, AppPaths.AppPrivateLastModLaunchPath,
            AppPaths.AppPrivateModSelectionPath, AppPaths.AppPrivateWorkshopManifestPath, fixture.AttemptPath };
        var before = paths.ToDictionary(path => path, File.ReadAllBytes);
        for (var read = 0; read < 2; read++)
        {
            var presentation = LauncherModsPresentationState.ReadCurrent();
            Equal(2, presentation.Mods.Count, "Both selected verified Workshop packages must remain visible after reopening.");
            True(presentation.Mods.All(mod => mod.LastLaunchState == LauncherModLastLaunchState.LoadedLastLaunch && !mod.IsLastLaunchStale),
                "An unchanged completed Workshop launch must retain its original native Loaded results after reopening.");
            Equal("Modded saves", presentation.SaveNamespaceLabel, "An unchanged completion must retain the actual native save flag.");
            True(!presentation.HasStaleLastLaunchResult, "Reopening alone must not invalidate completed framework evidence.");
        }
        True(paths.All(path => before[path].SequenceEqual(File.ReadAllBytes(path))),
            "Reading completed outcomes must not rewrite installation, package, attempt, result or selection evidence.");
    }

    private static void ModResultsReadCurrentInvalidatesChangedRuntimeManifest()
    {
        using var fixture = new ModResultsCurrentStateFixture();
        var original = File.ReadAllText(fixture.RuntimeManifest);
        foreach (var property in new[] { "packId", "gameIdentityId", "patchSetVersion" })
        {
            var changed = JsonNode.Parse(original)!;
            changed[property] = "changed-" + property;
            File.WriteAllText(fixture.RuntimeManifest, changed.ToJsonString());
            AssertStale(property);
        }
        File.WriteAllText(fixture.RuntimeManifest, "{");
        AssertStale("unreadable committed manifest");
        File.Delete(fixture.RuntimeManifest);
        AssertStale("missing committed manifest");

        void AssertStale(string changed)
        {
            var presentation = LauncherModsPresentationState.ReadCurrent();
            True(presentation.HasStaleLastLaunchResult && presentation.Mods.All(mod => mod.LastLaunchState == LauncherModLastLaunchState.NotTestedYet),
                $"Changed {changed} must invalidate the old native results.");
            Equal(LauncherModsPresentationState.PendingSaveNamespaceLabel, presentation.SaveNamespaceLabel,
                $"Changed {changed} must not carry forward a historical native save flag.");
        }
    }

    private static void ModResultsSaveLabelUsesNativeFlag()
    {
        using var fixture = new ModResultsFixture();
        Equal("Saves determined at launch", fixture.Presentation().SaveNamespaceLabel,
            "Selected packages must not guarantee the native save choice before initialization.");
        fixture.Write(new[] { fixture.Result("fixture.json", "fixture", "None") }, nativeIsRunningModded: false);
        Equal("Vanilla saves", fixture.Presentation().SaveNamespaceLabel,
            "The recorded native flag must determine saves rather than the selected Modded mode.");
        fixture.Write(new[] { fixture.Result("fixture.json", "fixture", "Skipped") }, nativeIsRunningModded: false);
        Equal("Vanilla saves", fixture.Presentation().SaveNamespaceLabel,
            "All-native-skipped sources must not claim Modded saves from the selected mode.");
        fixture.Write(new[] { fixture.Result("fixture.json", "fixture", "Loaded") }, nativeIsRunningModded: true);
        Equal("Modded saves", fixture.Presentation().SaveNamespaceLabel, "A matching native true flag must select Modded saves.");
        var stale = LauncherModsPresentationState.Build(fixture.Selection, fixture.Known,
            LauncherModLaunchResultStore.Read(fixture.Marker), "game", "changed-runtime", "attempt",
            LauncherModLaunchResultStore.ConfigurationId(fixture.Plan, "game", "runtime"));
        Equal("Saves determined at launch", stale.SaveNamespaceLabel, "A stale native flag must not determine the current save choice.");
        fixture.Write(new[] { fixture.Result("fixture.json", "fixture", "Loaded") });
        Equal("Saves determined at launch", fixture.Presentation().SaveNamespaceLabel,
            "A valid older writer without a native flag must remain truthful.");
    }

    private static void ModResultsSaveLabelEmptySelectionUsesVanilla()
    {
        var selection = new LauncherModSelectionDocument { PlayMode = LauncherModSelectionState.ModdedModeName };
        var presentation = LauncherModsPresentationState.Build(selection, Array.Empty<LauncherKnownMod>(), marker: null);
        Equal(LauncherModPlayMode.Modded, presentation.Mode, "An empty selection must preserve the requested play mode.");
        Equal("Vanilla saves", presentation.SaveNamespaceLabel, "Zero enabled sources must show Vanilla saves before completion.");
    }

    private static void ModResultsRetainInitializationFailureAfterDiscoveredRecords()
    {
        using var fixture = new ModResultsFixture();
        fixture.Write(new[] { fixture.Result("fixture.json", "fixture", "Loaded") }, "original-first-save-copy-failure");
        var item = fixture.Presentation().Mods.Single();
        True(item.Detail.Contains("original-first-save-copy-failure", StringComparison.Ordinal),
            "A framework failure after native loading must remain visible beside the original native states.");
        True(item.Detail.Contains("Loaded", StringComparison.Ordinal), "Reporting initialization failure must not replace the original mod state.");
    }
    private static void ModResultsInvalidateChangedFrameworkImplementation()
    {
        using var fixture = new ModResultsFixture();
        fixture.Write(new[] { fixture.Result("fixture.json", "fixture", "Loaded") });
        var json = JsonNode.Parse(File.ReadAllText(fixture.Marker))!;
        json["frameworkIdentity"] = "different-implementation";
        File.WriteAllText(fixture.Marker, json.ToJsonString());
        var presentation = fixture.Presentation();
        True(presentation.HasStaleLastLaunchResult, "An in-place framework update must invalidate old outcomes even if the game/runtime pack is unchanged.");
        Equal(LauncherModLastLaunchState.NotTestedYet, presentation.Mods.Single().LastLaunchState, "Old implementation evidence must not claim current loading.");
    }

    private static void ModResultsRejectMalformedNativeErrorRecords()
    {
        using var fixture = new ModResultsFixture();
        var result = fixture.Result("fixture.json", "fixture", "Loaded");
        result.NativeErrors = new LauncherModNativeError[] { null! };
        var rejected = false;
        try { fixture.Write(new[] { result }); }
        catch (InvalidDataException) { rejected = true; }
        True(rejected, "A malformed persisted error must be rejected before it can crash the Mods UI.");
    }

    private static void ModResultsRetainAllManifestsAndNativeErrors()
    {
        using var fixture = new ModResultsFixture();
        var results = Enumerable.Range(0, 150).Select(index => fixture.Result($"nested/{index}.json", index < 2 ? "shared-id" : "id-" + index,
            index == 1 ? "Failed" : "Loaded")).ToArray();
        results[1].NativeErrors = new[] { new LauncherModNativeError { LocTable = "main_menu_ui", LocEntryKey = "MOD_ERROR.DUPLICATE_ID", Variables = new() { ["id"] = "shared-id" } } };
        fixture.Write(results);
        var stored = LauncherModLaunchResultStore.Read(fixture.Marker)!;
        True(LauncherModLaunchResultStore.IsValid(stored), "Diagnostic size or shared manifest ID must not discard valid native outcomes.");
        Equal(150, stored.Mods.Length, "Every native manifest outcome must survive persistence.");
        Equal("MOD_ERROR.DUPLICATE_ID", stored.Mods[1].NativeErrors.Single().LocEntryKey, "Native structured errors must survive without early localization.");
        Equal(LauncherModLastLaunchState.Failed, fixture.Presentation().Mods.Single().LastLaunchState, "A package containing a native failure must display it.");
    }

    private static void ModResultsInvalidateChangedGameRuntimeAndAttempt()
    {
        using var fixture = new ModResultsFixture();
        fixture.Write(new[] { fixture.Result("fixture.json", "fixture", "Loaded") });
        foreach (var changed in new[] { ("different-game", "runtime", "attempt"), ("game", "different-runtime", "attempt"), ("game", "runtime", "different-attempt") })
        {
            var presentation = LauncherModsPresentationState.Build(fixture.Selection, fixture.Known,
                LauncherModLaunchResultStore.Read(fixture.Marker), changed.Item1, changed.Item2, changed.Item3,
                LauncherModLaunchResultStore.ConfigurationId(fixture.Plan, "game", "runtime"));
            True(presentation.HasStaleLastLaunchResult, "Game, runtime and launch attempt must independently invalidate stale outcomes.");
        }
    }

    private sealed class ModResultsCurrentStateFixture : IDisposable
    {
        private readonly GameInstallFixture _game = GameInstallFixture.Create();
        private readonly string? _previousData = Environment.GetEnvironmentVariable(AppPaths.LauncherPreviewDataDirEnvironmentVariable);
        private readonly bool _manual;
        private readonly bool _legacy;
        internal string DataDir => _game.DataDir;
        internal string Branch => _game.Branch;
        internal string ManualRoot => Path.Combine(DataDir, "manual-fixture");
        internal string RuntimeManifest => Path.Combine(GameRuntimeSlot.RuntimePackDirectoryPath(DataDir, Branch), "compatibility.json");
        internal string InstallationState => BranchInstallStateStore.PathFor(DataDir, Branch);
        internal string AttemptPath => Path.Combine(DataDir, LauncherStorageNames.LaunchAttempt);

        internal ModResultsCurrentStateFixture(bool manual = false, bool legacy = false)
        {
            _manual = manual;
            _legacy = legacy;
            Environment.SetEnvironmentVariable(AppPaths.LauncherPreviewDataDirEnvironmentVariable, DataDir);
            try { Initialize(); }
            catch { Dispose(); throw; }
        }

        private void Initialize()
        {
            var identity = _game.WriteMatchingRuntimePack();
            PublishInitialReady(_game, identity, 1001);
            LauncherPreferences.SaveGameBranch(Branch);
            var selection = new LauncherModSelectionDocument
            {
                PlayMode = "modded", EnabledMods = _manual
                    ? new() { [$"manual:{ManualRoot}:fixture"] = true }
                    : new() { ["workshop:42"] = true, ["workshop:43"] = true },
            };
            LauncherModSelectionState.Save(AppPaths.AppPrivateModSelectionPath, selection);
            if (_manual)
            {
                Directory.CreateDirectory(ManualRoot);
                File.WriteAllText(Path.Combine(ManualRoot, "fixture.json"), "{\"id\":\"manual-fixture\"}");
            }
            var items = new List<SteamWorkshopItem>();
            foreach (var id in _manual ? Array.Empty<ulong>() : new ulong[] { 42, 43 })
            {
                var source = Path.Combine(AppPaths.AppPrivateWorkshopDownloadsDir, id.ToString());
                Directory.CreateDirectory(source);
                File.WriteAllText(Path.Combine(source, "fixture.json"), $"{{\"id\":\"fixture-{id}\"}}");
                items.Add(new SteamWorkshopItem(id, "Fixture " + id, source, 1, status: "downloaded"));
            }
            var manifest = new SteamWorkshopStager().Stage(items);
            if (_legacy)
            {
                foreach (var item in manifest.Items) item.Installed.LegacyUnverified = true;
                new SteamWorkshopSyncStateStore(AppPaths.AppPrivateWorkshopManifestPath).Save(manifest);
            }
            var resolution = LauncherModLaunchPlan.Resolve(selection,
                LauncherModSelectionState.DiscoverKnownMods(selection, manifest, ManualRoot));
            True(resolution.Success, resolution.Error?.Message ?? "Cold presentation fixture preparation failed.");
            var plan = resolution.Plan;
            var runtimePack = RuntimePackManifest.Inspect(RuntimeManifest, identity);
            True(runtimePack.Usable, "The completed result fixture must have a separately committed usable runtime pack.");
            var configurationId = LauncherModLaunchResultStore.ConfigurationId(plan, identity.Id, runtimePack.PackId);
            LauncherModLaunchResultStore.WriteCompletion(plan, "completed-attempt", identity.Id, runtimePack.PackId,
                LauncherModSelectionState.SelectionFingerprint(selection), plan.Sources.Select(source => new LauncherModLaunchResultDocumentItem
                {
                    SelectionKey = source.SelectionKey, PackageGeneration = source.InstalledGeneration,
                    RelativeManifestPath = "fixture.json", Id = source.SelectionKey, Name = source.DisplayTitle, NativeState = "Loaded",
                }).ToArray(), nativeIsRunningModded: true);
            File.WriteAllText(AttemptPath, "Phase: in-process signalled\nAttempt ID: completed-attempt\n"
                + $"Game identity ID: {identity.Id}\nRuntime pack ID: {runtimePack.PackId}\nMod configuration ID: {configurationId}\n");
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(AppPaths.LauncherPreviewDataDirEnvironmentVariable, _previousData);
            _game.Dispose();
        }
    }

    private sealed class ModResultsFixture : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "sts2-mod-result-tests", Guid.NewGuid().ToString("N"));
        internal string Marker => Path.Combine(_root, "result.json");
        internal LauncherModSelectionDocument Selection { get; } = new() { PlayMode = "modded", EnabledMods = new() { ["workshop:42"] = true } };
        internal LauncherKnownMod[] Known { get; }
        internal LauncherModLaunchPlan Plan { get; }
        internal ModResultsFixture()
        {
            var package = Path.Combine(_root, "package");
            Directory.CreateDirectory(package);
            File.WriteAllText(Path.Combine(package, "fixture.json"), "{\"id\":\"fixture\"}");
            var files = SteamWorkshopPackageInventory.Capture(package);
            Known = new[] { new LauncherKnownMod { Key = "workshop:42", Id = "42", Title = "Fixture", WorkshopId = 42, Path = package, Enabled = true,
                Installed = new SteamWorkshopInstallation { Generation = "generation", Directory = package, Files = files, ContentSha256 = SteamWorkshopPackageInventory.ContentSha256(files) } } };
            var resolution = LauncherModLaunchPlan.Resolve(Selection, Known);
            True(resolution.Success, resolution.Error?.Message ?? "Result fixture source preparation failed.");
            Plan = resolution.Plan;
        }
        internal LauncherModLaunchResultDocumentItem Result(string path, string id, string state) => new()
        { SelectionKey = "workshop:42", PackageGeneration = "generation", RelativeManifestPath = path, Id = id, Name = id, NativeState = state };
        internal void Write(LauncherModLaunchResultDocumentItem[] results, string initializationError = null, bool? nativeIsRunningModded = null)
            => LauncherModLaunchResultStore.WriteCompletion(Plan, "attempt", "game", "runtime", LauncherModSelectionState.SelectionFingerprint(Selection), results, initializationError,
                markerPath: Marker, nativeIsRunningModded: nativeIsRunningModded);
        internal LauncherModsPresentation Presentation()
        {
            var observed = LauncherModLaunchPlan.Resolve(Selection, Known);
            True(observed.Success, "The result fixture must observe the current physical source.");
            return LauncherModsPresentationState.Build(Selection, Known,
                LauncherModLaunchResultStore.Read(Marker), "game", "runtime", "attempt",
                LauncherModLaunchResultStore.ConfigurationId(observed.Plan, "game", "runtime"));
        }
        public void Dispose() => Directory.Delete(_root, recursive: true);
    }
}
