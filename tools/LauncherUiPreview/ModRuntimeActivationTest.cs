using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Platform.Steam;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile;
using STS2Mobile.Launcher;
using STS2Mobile.Patches;
using STS2Mobile.Steam.Workshop;

namespace LauncherUiPreview;

// Focused desktop-host contracts. Stage 7 exercises one representative mod;
// the opt-in Stage 9 scenario adds only BaseLib to that same fresh-process path.
internal static class ModRuntimeActivationTest
{
    private const string MobileHarmonyId = "com.sts2mobile";
    private const string ManifestId = "ImportVanillaSaves";
    private const ulong WorkshopId = 3747503308;
    private const string BaseLibManifestId = "BaseLib";
    private const ulong BaseLibWorkshopId = 3737335127;
    private const string Stage9ChainScenario = "stage9-chain";
    private const string PckResourcePath =
        "res://ImportVanillaSaves/import_button.tscn";
    private const string BaseLibPckResourcePath = "res://BaseLib/mod_image.png";

    internal static Task RunAsync(
        string runtimeDataDir,
        string baseGamePckPath,
        string importVanillaSavesRoot,
        string baseLibRoot,
        string scenario
    )
    {
        scenario = NormalizeScenario(scenario);
        runtimeDataDir = Path.GetFullPath(runtimeDataDir);
        baseGamePckPath = ValidateFile(baseGamePckPath, "base-game PCK");
        importVanillaSavesRoot = ValidateRoot(importVanillaSavesRoot, ManifestId);
        baseLibRoot = string.Equals(scenario, Stage9ChainScenario, StringComparison.Ordinal)
            ? ValidateRoot(baseLibRoot, BaseLibManifestId)
            : string.Empty;
        Directory.CreateDirectory(runtimeDataDir);

        ModManagerCompatibilityTest.Run();

        Require(
            Directory.EnumerateFileSystemEntries(runtimeDataDir).All(path =>
                string.Equals(Path.GetFileName(path), "Godot", StringComparison.Ordinal)
            ),
            $"Runtime data directory was not isolated: {runtimeDataDir}"
        );
        Require(
            PathIsWithin(ProjectSettings.GlobalizePath("user://"), runtimeDataDir),
            $"Godot user data escaped the isolated root: {ProjectSettings.GlobalizePath("user://")}"
        );

        System.Environment.SetEnvironmentVariable(
            AppPaths.LauncherPreviewDataDirEnvironmentVariable,
            runtimeDataDir
        );
        try
        {
            Require(
                PathsMatch(AppPaths.AppPrivateDataDir, runtimeDataDir),
                $"Preview data resolved outside the isolated root: {AppPaths.AppPrivateDataDir}"
            );
            Require(
                ProjectSettings.LoadResourcePack(baseGamePckPath, true, 0),
                $"The explicit base-game PCK did not mount: {baseGamePckPath}"
            );

            PrepareOfflineState(importVanillaSavesRoot, baseLibRoot, scenario);
            var selection = LauncherModSelectionState.Load();
            var resolution = LauncherModLaunchPlan.Resolve(selection);
            ValidatePlan(scenario, resolution);

            Require(
                !SteamInitializer.Initialized,
                "The offline fixture initialized Steam before mod loading."
            );
            ModLoaderPatches.Apply(new Harmony(MobileHarmonyId));
            Require(
                ModLoaderPatches.IsInitializePostfixInstalled(),
                "The production ModManager.Initialize postfix was not installed."
            );

            var runtimeLogs = new List<string>();
            void CaptureRuntimeLog(string message) => runtimeLogs.Add(message);
            PatchHelper.LogEmitted += CaptureRuntimeLog;
            try
            {
                ModManager.Initialize(
                    new ModManagerFileIo(),
                    new ModSettings { PlayerAgreedToModLoading = false }
                );
            }
            finally
            {
                PatchHelper.LogEmitted -= CaptureRuntimeLog;
            }
            Require(
                !SteamInitializer.Initialized,
                "The offline fixture initialized Steam during mod loading."
            );

            var matches = ModManager.Mods
                .Where(mod => string.Equals(
                    mod.manifest?.id,
                    ManifestId,
                    StringComparison.Ordinal
                ))
                .ToArray();
            if (string.Equals(scenario, Stage9ChainScenario, StringComparison.Ordinal))
            {
                AssertStage9Chain(
                    selection,
                    resolution.Plan,
                    importVanillaSavesRoot,
                    baseLibRoot,
                    matches,
                    runtimeLogs
                );
                GD.Print(
                    "MOD_RUNTIME_STAGE9_CHAIN=BaseLib:Partial,ImportVanillaSaves:Active"
                );
                return Task.CompletedTask;
            }

            if (!string.Equals(scenario, "active", StringComparison.Ordinal))
            {
                AssertNotLoaded(scenario, selection, matches);
                GD.Print($"MOD_RUNTIME_{scenario.ToUpperInvariant()}=passed");
                return Task.CompletedTask;
            }

            Require(
                matches.Length == 1,
                $"Expected one discovered {ManifestId} runtime entry; found {matches.Length}."
            );
            var importer = matches[0];
            Require(
                PathsMatch(importer.path, importVanillaSavesRoot),
                $"The runtime loaded the wrong root: {importer.path}"
            );
            Require(
                importer.assembly != null
                    && string.Equals(
                        importer.assembly.GetName().Name,
                        ManifestId,
                        StringComparison.Ordinal
                    )
                    && ResourceLoader.Exists(PckResourcePath),
                "The representative DLL or PCK payload did not load."
            );
            Require(
                importer.state == ModLoadState.Loaded
                    && (importer.errors == null || importer.errors.Count == 0),
                $"The representative initializer failed with state {importer.state} and {importer.errors?.Count ?? 0} error(s)."
            );

            var activationTarget = AccessTools.PropertyGetter(
                typeof(UserDataPathProvider),
                "IsRunningModded"
            );
            Require(
                activationTarget != null
                    && Harmony.GetPatchInfo(activationTarget)?.Owners.Contains(ManifestId)
                        == true,
                "The representative initializer did not install its required Harmony patch."
            );

            var marker = LauncherModsPresentationState.ReadMarker(
                AppPaths.AppPrivateLastModLaunchPath
            );
            Require(
                marker != null
                    && marker.LaunchMode == LauncherModSelectionState.ModdedModeName
                    && marker.SelectionFingerprint
                        == LauncherModSelectionState.SelectionFingerprint(selection)
                    && marker.Discovered == 1
                    && marker.Loaded == 1
                    && marker.Active == 1
                    && marker.Partial == 0
                    && marker.Failed == 0
                    && marker.Mods.Length == 1
                    && marker.Mods[0].Id == ManifestId
                    && marker.Mods[0].Result == "Active",
                "The activated fixture did not overwrite last_mod_launch.json with one matching Active result."
            );

            GD.Print("MOD_RUNTIME_ACTIVATION=passed");
            return Task.CompletedTask;
        }
        finally
        {
            LauncherModSelectionState.ClearKnownModsCache(
                "focused offline mod runtime completed"
            );
            System.Environment.SetEnvironmentVariable(
                AppPaths.LauncherPreviewDataDirEnvironmentVariable,
                null
            );
        }
    }

    private static void PrepareOfflineState(
        string importVanillaSavesRoot,
        string baseLibRoot,
        string scenario
    )
    {
        Directory.CreateDirectory(AppPaths.AppPrivateWorkshopDownloadsDir);
        Directory.CreateDirectory(AppPaths.AppPrivateWorkshopStagedModsDir);

        var manifest = SteamWorkshopSyncManifest.Empty(
            AppPaths.AppPrivateWorkshopDownloadsDir,
            AppPaths.AppPrivateWorkshopStagedModsDir
        );
        manifest.SyncStatus = "offline-runtime-fixture";
        if (string.Equals(scenario, Stage9ChainScenario, StringComparison.Ordinal))
        {
            manifest.Items.Add(new SteamWorkshopSyncManifestItem
            {
                PublishedFileId = BaseLibWorkshopId,
                Title = BaseLibManifestId,
                SourceDirectory = baseLibRoot,
                StagedDirectory = baseLibRoot,
                FileCount = 3,
                HasPck = true,
                Status = "staged",
            });
        }
        manifest.Items.Add(new SteamWorkshopSyncManifestItem
        {
            PublishedFileId = WorkshopId,
            Title = "Import Vanilla Saves",
            SourceDirectory = importVanillaSavesRoot,
            StagedDirectory = importVanillaSavesRoot,
            FileCount = 3,
            HasPck = true,
            Status = "staged",
        });
        manifest.SubscribedItemCount = manifest.Items.Count;
        manifest.TotalItemCount = manifest.Items.Count;
        new SteamWorkshopSyncStateStore(
            AppPaths.AppPrivateWorkshopManifestPath
        ).Save(manifest);

        var enabledMods = new Dictionary<string, bool>(
            StringComparer.OrdinalIgnoreCase
        )
        {
            [$"workshop:{WorkshopId}"] = !string.Equals(
                scenario,
                "disabled",
                StringComparison.Ordinal
            ),
        };
        if (string.Equals(scenario, Stage9ChainScenario, StringComparison.Ordinal))
            enabledMods[$"workshop:{BaseLibWorkshopId}"] = true;

        LauncherModSelectionState.Save(
            AppPaths.AppPrivateModSelectionPath,
            new LauncherModSelectionDocument
            {
                PlayMode = string.Equals(scenario, "vanilla", StringComparison.Ordinal)
                    ? LauncherModSelectionState.VanillaModeName
                    : LauncherModSelectionState.ModdedModeName,
                EnabledMods = enabledMods,
            }
        );
        File.WriteAllText(
            AppPaths.AppPrivateLastModLaunchPath,
            "{\"obsoleteMarker\":true}"
        );
        WorkshopModConsent.Accept("focused offline mod runtime fixture");
        LauncherModSelectionState.ClearKnownModsCache(
            "focused offline mod fixture staged"
        );
    }

    private static string NormalizeScenario(string scenario)
    {
        scenario = string.IsNullOrWhiteSpace(scenario)
            ? "active"
            : scenario.Trim().ToLowerInvariant();
        if (scenario is "active" or "vanilla" or "disabled" or Stage9ChainScenario)
            return scenario;

        throw new ArgumentOutOfRangeException(
            nameof(scenario),
            scenario,
            "Expected active, vanilla, disabled, or stage9-chain."
        );
    }

    private static void ValidatePlan(
        string scenario,
        LauncherModLaunchPlanResolution resolution
    )
    {
        if (string.Equals(scenario, Stage9ChainScenario, StringComparison.Ordinal))
        {
            Require(
                resolution.Success
                    && resolution.Plan.Mode == LauncherModPlayMode.Modded
                    && resolution.Plan.SaveNamespace == LauncherModSaveNamespace.Modded
                    && resolution.Plan.EnabledMods.Select(mod => mod.ManifestId)
                        .SequenceEqual(new[] { BaseLibManifestId, ManifestId })
                    && resolution.Plan.Dependencies.IsEmpty
                    && resolution.Plan.Roots.Length == 2,
                resolution.Error?.Message
                    ?? "The Stage 9 chain did not resolve to exact BaseLib then ImportVanillaSaves roots."
            );
            return;
        }

        if (string.Equals(scenario, "active", StringComparison.Ordinal))
        {
            Require(
                resolution.Success
                    && resolution.Plan.Mode == LauncherModPlayMode.Modded
                    && resolution.Plan.EnabledMods.Length == 1
                    && resolution.Plan.Dependencies.IsEmpty
                    && resolution.Plan.EnabledMods[0].ManifestId == ManifestId,
                resolution.Error?.Message
                    ?? "The representative fixture did not resolve to one modded plan."
            );
            return;
        }

        if (string.Equals(scenario, "vanilla", StringComparison.Ordinal))
        {
            Require(
                resolution.Success
                    && resolution.Plan.Mode == LauncherModPlayMode.Vanilla
                    && resolution.Plan.EnabledMods.IsEmpty,
                resolution.Error?.Message
                    ?? "Vanilla selection did not resolve to an empty plan."
            );
            return;
        }

        Require(
            !resolution.Success
                && resolution.Error?.Code
                    == LauncherModDiscoveryErrorCode.SelectionRequired,
            "Disabled Modded selection did not fail with SelectionRequired."
        );
    }

    private static void AssertNotLoaded(
        string scenario,
        LauncherModSelectionDocument selection,
        Mod[] matches
    )
    {
        Require(
            matches.Length == 0,
            $"{scenario} selection loaded {ManifestId} into ModManager."
        );
        Require(
            !AppDomain.CurrentDomain.GetAssemblies().Any(assembly =>
                string.Equals(assembly.GetName().Name, ManifestId, StringComparison.Ordinal)
            ),
            $"{scenario} selection loaded the {ManifestId} assembly."
        );
        Require(
            !ResourceLoader.Exists(PckResourcePath),
            $"{scenario} selection mounted the {ManifestId} PCK."
        );

        var activationTarget = AccessTools.PropertyGetter(
            typeof(UserDataPathProvider),
            "IsRunningModded"
        );
        Require(
            activationTarget == null
                || Harmony.GetPatchInfo(activationTarget)?.Owners.Contains(ManifestId)
                    != true,
            $"{scenario} selection ran the {ManifestId} initializer."
        );

        var marker = LauncherModsPresentationState.ReadMarker(
            AppPaths.AppPrivateLastModLaunchPath
        );
        var expectedMode = string.Equals(scenario, "vanilla", StringComparison.Ordinal)
            ? LauncherModSelectionState.VanillaModeName
            : LauncherModSelectionState.ModdedModeName;
        Require(
            marker != null
                && marker.LaunchMode == expectedMode
                && marker.SelectionFingerprint
                    == LauncherModSelectionState.SelectionFingerprint(selection)
                && marker.Discovered == 0
                && marker.Loaded == 0
                && marker.Active == 0
                && marker.Partial == 0
                && marker.Failed == 0
                && marker.Mods.Length == 0,
            $"{scenario} selection did not persist an honest zero-activation result."
        );
    }

    private static void AssertStage9Chain(
        LauncherModSelectionDocument selection,
        LauncherModLaunchPlan plan,
        string importVanillaSavesRoot,
        string baseLibRoot,
        Mod[] importerMatches,
        IReadOnlyList<string> runtimeLogs
    )
    {
        var runtimeMods = ModManager.Mods.ToArray();
        var baseLibMatches = runtimeMods
            .Where(mod => string.Equals(
                mod.manifest?.id,
                BaseLibManifestId,
                StringComparison.Ordinal
            ))
            .ToArray();
        Require(
            runtimeMods.Length == 2
                && baseLibMatches.Length == 1
                && importerMatches.Length == 1,
            $"The Stage 9 chain produced {runtimeMods.Length} runtime mod(s), "
                + $"including {baseLibMatches.Length} BaseLib and "
                + $"{importerMatches.Length} ImportVanillaSaves entries."
        );
        Require(
            plan.EnabledMods.Length == 2
                && PathsMatch(plan.EnabledMods[0].RootPath, baseLibRoot)
                && PathsMatch(
                    plan.EnabledMods[1].RootPath,
                    importVanillaSavesRoot
                ),
            "The Stage 9 chain plan did not retain the exact two explicit roots."
        );

        var baseLib = baseLibMatches[0];
        var importer = importerMatches[0];
        AssertLoadedRuntimeMod(
            baseLib,
            BaseLibManifestId,
            baseLibRoot,
            BaseLibPckResourcePath
        );
        AssertLoadedRuntimeMod(
            importer,
            ManifestId,
            importVanillaSavesRoot,
            PckResourcePath
        );

        var importerActivationTarget = AccessTools.PropertyGetter(
            typeof(UserDataPathProvider),
            "IsRunningModded"
        );
        Require(
            importerActivationTarget != null
                && Harmony.GetPatchInfo(importerActivationTarget)?.Owners.Contains(
                    ManifestId
                ) == true,
            "ImportVanillaSaves did not install its exact-owner Harmony patch."
        );
        var baseLibHarmonyTargets = Harmony.GetAllPatchedMethods()
            .Where(method => Harmony.GetPatchInfo(method)?.Owners.Contains(
                BaseLibManifestId
            ) == true)
            .ToArray();
        Require(
            baseLibHarmonyTargets.Length == 1
                && string.Equals(
                    baseLibHarmonyTargets[0].DeclaringType?.FullName,
                    "MegaCrit.Sts2.Core.Models.Badges.BadgePool",
                    StringComparison.Ordinal
                )
                && string.Equals(
                    baseLibHarmonyTargets[0].Name,
                    "CreateAll",
                    StringComparison.Ordinal
                ),
            "BaseLib's Android-safe initializer did not retain exactly its "
                + "BadgePool.CreateAll activation target."
        );

        const string expectedActivationLog =
            "[Mods] Activation evidence: selected=2 discovered=2 payloadLoaded=2 "
            + "initialized=2 activated=2 partial=1 failed=0 "
            + "uiRegistration=not-exercised";
        Require(
            runtimeLogs.Count(line => string.Equals(
                line,
                expectedActivationLog,
                StringComparison.Ordinal
            )) == 1,
            "Production did not emit the exact two-mod Stage 9 activation summary."
        );

        var marker = LauncherModsPresentationState.ReadMarker(
            AppPaths.AppPrivateLastModLaunchPath
        );
        Require(
            marker != null
                && marker.LaunchMode == LauncherModSelectionState.ModdedModeName
                && marker.SelectionFingerprint
                    == LauncherModSelectionState.SelectionFingerprint(selection)
                && marker.Discovered == 2
                && marker.Loaded == 2
                && marker.Active == 1
                && marker.Partial == 1
                && marker.Failed == 0
                && marker.Mods.Length == 2
                && marker.Mods[0].Id == BaseLibManifestId
                && marker.Mods[0].Result == "Partial"
                && marker.Mods[1].Id == ManifestId
                && marker.Mods[1].Result == "Active",
            "The Stage 9 marker was not exactly BaseLib Partial plus ImportVanillaSaves Active."
        );
    }

    private static void AssertLoadedRuntimeMod(
        Mod mod,
        string expectedId,
        string expectedRoot,
        string expectedPckResource
    )
    {
        Require(
            mod != null
                && PathsMatch(mod.path, expectedRoot)
                && mod.assembly != null
                && string.Equals(
                    mod.assembly.GetName().Name,
                    expectedId,
                    StringComparison.Ordinal
                )
                && ResourceLoader.Exists(expectedPckResource),
            $"{expectedId} did not load its exact root, DLL, and PCK payload."
        );
        Require(
            mod.state == ModLoadState.Loaded
                && (mod.errors == null || mod.errors.Count == 0),
            $"{expectedId} did not finish initialization in exact Loaded state with zero errors."
        );
    }

    private static string ValidateRoot(string root, string expectedManifestId)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException(
                $"Explicit {expectedManifestId} root is required.",
                nameof(root)
            );

        root = Path.GetFullPath(root);
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException(
                $"Explicit {expectedManifestId} root was not found: {root}"
            );

        var expectedFileName = expectedManifestId + ".json";
        var probes = Directory
            .EnumerateFiles(root, "*.json", SearchOption.AllDirectories)
            .Where(path => string.Equals(
                Path.GetFileName(path),
                expectedFileName,
                StringComparison.Ordinal
            ))
            .Select(LauncherModLaunchPlan.InspectManifest)
            .ToArray();
        var matches = probes
            .Where(probe => probe.Kind == LauncherModManifestProbeKind.Valid)
            .Where(probe => string.Equals(
                probe.Manifest?.Id,
                expectedManifestId,
                StringComparison.Ordinal
            ))
            .ToArray();
        if (matches.Length != 1)
        {
            var detail = probes.Length == 1
                ? probes[0].ErrorMessage
                : $"found {matches.Length} valid exact manifests";
            throw new InvalidDataException(
                $"Explicit root is not the expected {expectedManifestId} fixture: {root}; {detail}."
            );
        }

        return Path.GetDirectoryName(matches[0].ManifestPath)
            ?? throw new InvalidDataException(
                $"The {expectedManifestId} manifest has no payload directory."
            );
    }

    private static string ValidateFile(string path, string label)
    {
        path = Path.GetFullPath(path);
        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"Explicit {label} was not found: {path}",
                path
            );
        return path;
    }

    private static bool PathsMatch(string left, string right)
        => string.Equals(
            Path.GetFullPath(left),
            Path.GetFullPath(right),
            StringComparison.OrdinalIgnoreCase
        );

    private static bool PathIsWithin(string candidate, string root)
    {
        var normalizedCandidate = Path.GetFullPath(candidate);
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
        );
        return PathsMatch(normalizedCandidate, normalizedRoot)
            || normalizedCandidate.StartsWith(
                normalizedRoot + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase
            );
    }

    private static void Require(bool condition, string failure)
    {
        if (!condition)
            throw new InvalidOperationException(failure);
    }
}
