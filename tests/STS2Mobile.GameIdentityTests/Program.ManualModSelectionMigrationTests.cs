using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void ManualModReadOnlySnapshotPreservesNewerChoices()
    {
        using var fixture = new ModSourceFixture();
        Directory.CreateDirectory(fixture.ManualRoot);
        File.WriteAllText(Path.Combine(fixture.ManualRoot, "metadata.json"), "{\"id\":\"foo\"}");
        var prefix = $"manual:{Path.GetFullPath(fixture.ManualRoot)}:";
        var legacyKey = "legacy-" + prefix + "foo";
        var path = Path.Combine(fixture.DataRoot, "selection.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new LauncherModSelectionDocument
        {
            Version = 2, PlayMode = "modded", EnabledMods = new() { [legacyKey] = true, ["workshop:42"] = true },
        }));

        // Pause the worker after reading its old snapshot. A user then changes
        // both the mode and choices before the worker discovers a returning file.
        var staleWorker = LauncherModSelectionState.Load(path, persistMigration: false);
        var latest = LauncherModSelectionState.Load(path);
        latest.PlayMode = "vanilla";
        latest.EnabledMods[legacyKey] = false;
        latest.EnabledMods["workshop:42"] = false;
        LauncherModSelectionState.Save(path, latest);
        var latestBytes = File.ReadAllBytes(path);
        var known = LauncherModSelectionState.DiscoverKnownMods(staleWorker, null!, fixture.ManualRoot);
        True(known.Single().Enabled && staleWorker.EnabledMods[prefix + "metadata"],
            "A read-only worker must still resolve its own snapshot for observed configuration matching.");
        True(latestBytes.SequenceEqual(File.ReadAllBytes(path)),
            "Late discovery from an old background snapshot must not persist migration over newer user choices.");
        var retained = LauncherModSelectionState.Load(path);
        Equal("vanilla", retained.PlayMode, "A stale worker must not revert the user's new play mode.");
        True(!retained.EnabledMods[legacyKey] && !retained.EnabledMods["workshop:42"],
            "A stale worker must not restore either enabled choice after the user disabled it.");
    }

    private static void ManualModReadOnlyLoadLeavesMigrationForNormalPaths()
    {
        using var fixture = new ModSourceFixture();
        Directory.CreateDirectory(fixture.ManualRoot);
        File.WriteAllText(Path.Combine(fixture.ManualRoot, "metadata.json"), "{\"id\":\"foo\"}");
        var prefix = $"manual:{Path.GetFullPath(fixture.ManualRoot)}:";
        var path = Path.Combine(fixture.DataRoot, "selection.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new LauncherModSelectionDocument
        {
            Version = 1, PlayMode = "modded", EnabledMods = new() { [prefix + "foo"] = true },
        }));
        var original = File.ReadAllBytes(path);
        var readOnly = LauncherModSelectionState.Load(path, persistMigration: false);
        LauncherModSelectionState.DiscoverKnownMods(readOnly, null!, fixture.ManualRoot);
        True(readOnly.Version == 2 && readOnly.EnabledMods[prefix + "metadata"],
            "Read-only acquisition must understand legacy identities without changing their selection semantics.");
        True(original.SequenceEqual(File.ReadAllBytes(path)), "Read-only acquisition must not persist even the schema upgrade.");

        var normal = LauncherModSelectionState.Load(path);
        LauncherModSelectionState.DiscoverKnownMods(normal, null!, fixture.ManualRoot);
        var persisted = LauncherModSelectionState.Load(path);
        True(persisted.Version == 2 && persisted.EnabledMods[prefix + "metadata"],
            "The normal load/discovery path must still transparently persist the unambiguous migration.");
    }

    private static void ManualModLegacyKeysMigrateEnabledAndDisabledChoices()
    {
        using var fixture = new ModSourceFixture();
        Directory.CreateDirectory(fixture.ManualRoot);
        var metadata = Path.Combine(fixture.ManualRoot, "metadata.json");
        var foo = Path.Combine(fixture.ManualRoot, "foo.json");
        const string originalManifest = "{\"id\":\"foo\",\"name\":\"Future schema\",\"extra\":{\"preserved\":true}}";
        File.WriteAllText(metadata, originalManifest);
        File.WriteAllText(foo, "{\"id\":\"bar\"}");
        var prefix = $"manual:{Path.GetFullPath(fixture.ManualRoot)}:";
        var legacy = new LauncherModSelectionDocument
        {
            Version = 1,
            PlayMode = LauncherModSelectionState.ModdedModeName,
            EnabledMods = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                [prefix + "foo"] = true,
                [prefix + "bar"] = false,
                ["workshop:42"] = false,
            },
        };
        var path = Path.Combine(fixture.DataRoot, "selection.json");
        File.WriteAllText(path, JsonSerializer.Serialize(legacy));
        var loaded = LauncherModSelectionState.Load(path);
        var known = LauncherModSelectionState.DiscoverKnownMods(loaded, null!, fixture.ManualRoot);
        var plan = LauncherModLaunchPlan.Resolve(loaded, known);
        True(plan.Success, "A legacy identity choice must prepare without reselecting the mod.");
        Equal(metadata, plan.Plan.Sources.Single().ManifestPath,
            "A v1 identity must select its original manifest even when another filename matches that identity.");
        Equal(2, loaded.Version, "Physical manual keys must have an explicit schema boundary.");
        True(loaded.EnabledMods[prefix + "metadata"] && !loaded.EnabledMods[prefix + "foo"],
            "Migration must preserve both enabled and disabled choices while replacing identity keys.");
        True(!loaded.EnabledMods.ContainsKey(prefix + "bar") && !loaded.EnabledMods["workshop:42"],
            "Migration must remove superseded manual keys and preserve Workshop choices.");
        Equal(originalManifest, File.ReadAllText(metadata), "Migration must leave the complete native JSON untouched.");
        var persisted = LauncherModSelectionState.Load(path);
        Equal(2, persisted.Version, "The transparent migration must persist once.");
        True(persisted.EnabledMods[prefix + "metadata"] && !persisted.EnabledMods[prefix + "foo"],
            "Migrated physical choices must survive restart.");
        var afterMigration = File.ReadAllText(path);
        LauncherModSelectionState.DiscoverKnownMods(persisted, null!, fixture.ManualRoot);
        Equal(afterMigration, File.ReadAllText(path), "A migrated selection must not be rewritten on each discovery.");
    }

    private static void ManualModLegacyAmbiguityPreservesChoiceWithoutGuessing()
    {
        using var fixture = new ModSourceFixture();
        Directory.CreateDirectory(fixture.ManualRoot);
        File.WriteAllText(Path.Combine(fixture.ManualRoot, "one.json"), "{\"id\":\"foo\"}");
        File.WriteAllText(Path.Combine(fixture.ManualRoot, "two.json"), "{\"id\":\"foo\"}");
        File.WriteAllText(Path.Combine(fixture.ManualRoot, "foo.json"), "{\"id\":\"other\"}");
        var key = $"manual:{Path.GetFullPath(fixture.ManualRoot)}:foo";
        foreach (var enabled in new[] { true, false })
        {
            var path = Path.Combine(fixture.DataRoot, $"ambiguous-{enabled}.json");
            File.WriteAllText(path, JsonSerializer.Serialize(new LauncherModSelectionDocument
            {
                Version = 1,
                PlayMode = LauncherModSelectionState.ModdedModeName,
                EnabledMods = new Dictionary<string, bool> { [key] = enabled },
            }));
            var loaded = LauncherModSelectionState.Load(path);
            var known = LauncherModSelectionState.DiscoverKnownMods(loaded, null!, fixture.ManualRoot);
            var plan = LauncherModLaunchPlan.Resolve(loaded, known);
            if (enabled)
            {
                True(!plan.Success, "An ambiguous legacy identity must not guess a physical source.");
                Equal("SelectionAmbiguous", plan.Error.Code.ToString(), "Ambiguity must be reported separately from a missing source.");
            }
            else
            {
                True(plan.Success && plan.Plan.Sources.IsEmpty, "A disabled ambiguous choice must remain disabled.");
            }
            Equal(enabled, loaded.EnabledMods["legacy-" + key], "Migration must retain an unresolved enabled or disabled choice.");
            True(!loaded.EnabledMods.ContainsKey(key), "An unresolved ID must never be reinterpreted as a v2 filename.");
            Equal(2, loaded.Version, "Unresolved legacy choices must not block the schema upgrade.");
            var persisted = LauncherModSelectionState.Load(path);
            Equal(enabled, persisted.EnabledMods["legacy-" + key], "An ambiguous migration must durably preserve its original choice.");
            True(known.Any(mod => mod.Key == "legacy-" + key && !string.IsNullOrWhiteSpace(mod.DiscoveryError)),
                "The selector must show why the legacy identity could not be resolved.");
        }
    }

    private static void ManualModPhysicalV2KeysIgnoreLegacyIdentityAliases()
    {
        using var fixture = new ModSourceFixture();
        Directory.CreateDirectory(fixture.ManualRoot);
        File.WriteAllText(Path.Combine(fixture.ManualRoot, "metadata.json"), "{\"id\":\"foo\"}");
        var foo = Path.Combine(fixture.ManualRoot, "foo.json");
        File.WriteAllText(foo, "{\"id\":\"bar\"}");
        var key = $"manual:{Path.GetFullPath(fixture.ManualRoot)}:foo";
        var path = Path.Combine(fixture.DataRoot, "physical.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new LauncherModSelectionDocument
        {
            Version = 2,
            PlayMode = LauncherModSelectionState.ModdedModeName,
            EnabledMods = new Dictionary<string, bool> { [key] = true },
        }));
        var loaded = LauncherModSelectionState.Load(path);
        var known = LauncherModSelectionState.DiscoverKnownMods(loaded, null!, fixture.ManualRoot);
        var plan = LauncherModLaunchPlan.Resolve(loaded, known);
        True(plan.Success && plan.Plan.Sources.Length == 1, "A v2 physical source must remain selectable.");
        Equal(foo, plan.Plan.Sources.Single().ManifestPath, "Version2 must use the exact filename rather than another manifest's ID hint.");
    }

    private static void ManualModMissingDisabledLegacyChoiceDoesNotBlockSelection()
    {
        using var fixture = new ModSourceFixture();
        Directory.CreateDirectory(fixture.ManualRoot);
        var metadata = Path.Combine(fixture.ManualRoot, "metadata.json");
        File.WriteAllText(metadata, "{\"id\":\"present\"}");
        var prefix = $"manual:{Path.GetFullPath(fixture.ManualRoot)}:";
        var path = Path.Combine(fixture.DataRoot, "missing.json");
        File.WriteAllText(path, JsonSerializer.Serialize(new LauncherModSelectionDocument
        {
            Version = 1,
            PlayMode = LauncherModSelectionState.ModdedModeName,
            EnabledMods = new Dictionary<string, bool>
            {
                [prefix + "present"] = true,
                [prefix + "missing"] = false,
            },
        }));
        var loaded = LauncherModSelectionState.Load(path);
        var known = LauncherModSelectionState.DiscoverKnownMods(loaded, null!, fixture.ManualRoot);
        var plan = LauncherModLaunchPlan.Resolve(loaded, known);
        True(plan.Success && plan.Plan.Sources.Length == 1, "A disabled missing legacy choice must not prevent another source from preparing.");
        Equal(metadata, plan.Plan.Sources.Single().ManifestPath, "The available legacy choice must migrate to its exact file.");
        True(!loaded.EnabledMods["legacy-" + prefix + "missing"], "The missing disabled choice must remain retained and disabled.");
        Equal(2, loaded.Version, "Missing legacy sources must not keep new choices on old key semantics.");

        // A newly present filename is not evidence that an old identity choice meant it.
        var wrong = Path.Combine(fixture.ManualRoot, "missing.json");
        File.WriteAllText(wrong, "{\"id\":\"different-id\"}");
        loaded.EnabledMods[prefix + "metadata"] = false;
        loaded.EnabledMods["legacy-" + prefix + "missing"] = true;
        known = LauncherModSelectionState.DiscoverKnownMods(loaded, null!, fixture.ManualRoot);
        plan = LauncherModLaunchPlan.Resolve(loaded, known);
        True(!plan.Success && plan.Error.Code.ToString() == "SourceUnavailable",
            "An unresolved legacy identity must never activate a newly matching filename.");

        File.WriteAllText(wrong, "{\"id\":\"missing\"}");
        known = LauncherModSelectionState.DiscoverKnownMods(loaded, null!, fixture.ManualRoot);
        plan = LauncherModLaunchPlan.Resolve(loaded, known);
        True(plan.Success, "A returning unambiguous legacy identity must migrate automatically.");
        Equal(wrong, plan.Plan.Sources.Single().ManifestPath, "The returning identity must bind its actual physical manifest.");
        True(!loaded.EnabledMods.ContainsKey("legacy-" + prefix + "missing"), "A resolved legacy marker must be removed after successful migration.");
    }

    private static void ManualModLegacyConflictPreservesBothChoiceValues()
    {
        using var fixture = new ModSourceFixture();
        Directory.CreateDirectory(fixture.ManualRoot);
        var metadata = Path.Combine(fixture.ManualRoot, "metadata.json");
        File.WriteAllText(metadata, "{\"id\":\"foo\"}");
        var prefix = $"manual:{Path.GetFullPath(fixture.ManualRoot)}:";
        var legacyKey = "legacy-" + prefix + "foo";
        var physicalKey = prefix + "metadata";
        var document = new LauncherModSelectionDocument
        {
            Version = 2,
            PlayMode = LauncherModSelectionState.ModdedModeName,
            EnabledMods = new Dictionary<string, bool>
            {
                [legacyKey] = true,
                [physicalKey] = false,
            },
        };
        var known = LauncherModSelectionState.DiscoverKnownMods(document, null!, fixture.ManualRoot);
        var plan = LauncherModLaunchPlan.Resolve(document, known);
        True(!plan.Success && plan.Error.Code.ToString() == "SelectionAmbiguous",
            "Conflicting legacy and physical choices must be reported rather than silently overridden.");
        True(document.EnabledMods[legacyKey] && !document.EnabledMods[physicalKey],
            "Neither conflicting choice may be erased or have its value changed.");

        document.EnabledMods[legacyKey] = false;
        document.EnabledMods[physicalKey] = true;
        known = LauncherModSelectionState.DiscoverKnownMods(document, null!, fixture.ManualRoot);
        plan = LauncherModLaunchPlan.Resolve(document, known);
        True(plan.Success, "A disabled unresolved choice must not block a new explicit physical selection.");
        Equal(metadata, plan.Plan.Sources.Single().ManifestPath, "The new explicit choice must keep its physical source.");
    }
}
