using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Platform.Steam;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile;
using STS2Mobile.Launcher;
using STS2Mobile.Patches;
using STS2Mobile.Steam.Workshop;

namespace LauncherUiPreview;

internal static class ModFrameworkContractTest
{
    private const string EntryId = "FixtureEntry";
    private const string ResourceId = "FixtureResources";
    private const string ManualId = "FixtureManual";
    private const string DisabledId = "FixtureDisabled";
    private const ulong WorkshopId = 9000000001;
    private static readonly Dictionary<Mod, string> NativeManifests = new(ReferenceEqualityComparer.Instance);
    private static readonly List<Mod> DiscoveryOrder = new();
    private static readonly List<Mod> DetectionOrder = new();
    private static Func<Task> _nestedInitialize;
    private static Task _nestedInitialization;
    internal static int ParsedManifestCount => NativeManifests.Count;

    internal static async Task RunAsync(string runtimeDataDir, string baseGamePckPath, string gameAssemblyPath, string scenario)
    {
        Require(scenario is "contracts" or "fault" or "native-lifecycle", "Unknown original-framework contract scenario.");
        runtimeDataDir = Path.GetFullPath(runtimeDataDir);
        System.Environment.SetEnvironmentVariable(AppPaths.LauncherPreviewDataDirEnvironmentVariable, runtimeDataDir);
        Require(Path.GetFullPath(AppPaths.AppPrivateDataDir) == runtimeDataDir,
            "The contract fixture escaped its isolated launcher data.");
        Require(Path.GetFullPath(ProjectSettings.GlobalizePath("user://")).StartsWith(runtimeDataDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
            "The contract fixture escaped its isolated Godot user data.");
        using (var selected = Mono.Cecil.ModuleDefinition.ReadModule(gameAssemblyPath))
            Require(selected.Mvid == typeof(ModManager).Assembly.ManifestModule.ModuleVersionId,
                "The contract process loaded a different game DLL from its selected input.");
        Require(ModManager.Mods.Count == 0 && NativeManifests.Count == 0,
            "Native contracts require a fresh process, without resetting manager state.");
        Require(ProjectSettings.LoadResourcePack(baseGamePckPath), "The game PCK did not mount.");
        var initialize = ModLoaderPatches.FindInitializeTarget(typeof(ModManager))
            ?? throw new MissingMethodException("The selected game has no supported initialization signature.");
        var current = initialize.ReturnType == typeof(Task);
        var input = CreateInputs(runtimeDataDir, current);
        var manifest = SteamWorkshopSyncManifest.Empty(AppPaths.AppPrivateWorkshopDownloadsDir, AppPaths.AppPrivateWorkshopStagedModsDir);
        var files = SteamWorkshopPackageInventory.Capture(input.WorkshopRoot);
        var generation = SteamWorkshopPackageInventory.ContentSha256(files);
        manifest.Items.Add(new SteamWorkshopSyncManifestItem
        {
            PublishedFileId = WorkshopId, Title = "Original framework contract package", Status = "staged",
            StagedDirectory = input.WorkshopRoot,
            Installed = new SteamWorkshopInstallation
            {
                Directory = input.WorkshopRoot, Generation = generation, ContentSha256 = generation,
                Files = files, SelectedGameBranch = "public-beta", RevisionProvenance = "generated-contract-fixture",
            },
        });
        new SteamWorkshopSyncStateStore(AppPaths.AppPrivateWorkshopManifestPath).Save(manifest);
        var selectedManualKey = $"manual:{input.ManualRoot}:selected";
        var disabledManualKey = $"manual:{input.ManualRoot}:disabled";
        var selection = new LauncherModSelectionDocument
        {
            PlayMode = LauncherModSelectionState.ModdedModeName,
            EnabledMods = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            {
                [$"workshop:{WorkshopId}"] = true, [selectedManualKey] = true, [disabledManualKey] = false,
            },
        };
        LauncherModSelectionState.Save(AppPaths.AppPrivateModSelectionPath, selection);
        WorkshopModConsent.Accept("isolated native contract fixture");
        var resolution = LauncherModLaunchPlan.Resolve(selection,
            LauncherModSelectionState.DiscoverKnownMods(selection, manifest, input.ManualRoot));
        Require(resolution.Success, resolution.Error?.Message ?? "Contract source preparation failed.");
        var plan = resolution.Plan;
        Require(plan.Sources.Length == 2 && plan.Sources.Single(source => source.WorkshopId.HasValue).ManifestPath == null
            && plan.Sources.Single(source => !source.WorkshopId.HasValue).ManifestPath == Path.Combine(input.ManualRoot, "selected.json"),
            "Preparation did not retain the whole Workshop package and exact manual manifest.");
        var sourceId = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(gameAssemblyPath)));
        var runtimeId = "contract-runtime:" + typeof(Harmony).Assembly.ManifestModule.ModuleVersionId;
        var attempt = "original-framework-" + scenario;
        var releasePath = current ? Path.Combine(Path.GetDirectoryName(baseGamePckPath)!, "release_info.json") : "";
        ModLoaderPatches.Prepare(plan, attempt, sourceId, runtimeId, releasePath, "public-beta");
        var harmony = new Harmony("sts2-launcher.original-contract-fixture");
        ModLoaderPatches.Apply(harmony);
        PlatformPatches.PatchPlatformBranch(harmony);
        LauncherPatches.ApplySaveManagerPatches(harmony);
        VerifyNativeSavePathNormalization(runtimeDataDir);
        new Harmony("sts2-launcher.original-contract-observer").Patch(
            AccessTools.Method(typeof(ModManager), "ReadModManifest"),
            postfix: new HarmonyMethod(typeof(ModFrameworkContractTest), nameof(ObserveManifest)));
        var io = new ContractFileIo(scenario == "fault" ? input.WorkshopRoot : null);
        var saveProbe = typeof(IModManagerFileIo).GetMethod("CopyFile") != null && scenario != "fault"
            ? PrepareSaveProbe(runtimeDataDir, io) : null;
        var settings = new ModSettings { PlayerAgreedToModLoading = false };
        Require(settings.ModList.Count == 0, "First-time bookkeeping must start with the original empty settings.");
        Task reentrantInitialization = null;
        if (scenario == "contracts")
            io.BeforeFirstDirectoryRead = () =>
            {
                io.FailNextDirectoryCheck = true;
                try { reentrantInitialization = Initialize(initialize, io, settings); }
                finally { io.FailNextDirectoryCheck = false; }
            };
        ModManager.OnModDetected += ObserveDetection;
        try
        {
            if (scenario == "fault")
            {
                Exception observed = null;
                try { await Initialize(initialize, io, settings); }
                catch (Exception error) { observed = error; }
                Require(ReferenceEquals(observed, io.DiscoveryFailure), "The original discovery exception was swallowed, replaced or wrapped.");
                var failed = RequireResult(plan, attempt, sourceId, runtimeId);
                Require(failed.InitializationError?.Contains(io.DiscoveryFailure.Message, StringComparison.Ordinal) == true
                    && failed.Mods.All(mod => mod.NativeState != nameof(ModLoadState.Loaded)),
                    "A failed initialization did not retain its error or claimed a loaded mod.");
                Require(!AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == EntryId),
                    "The entry DLL loaded after original discovery faulted.");
                RequireCurrentScopeCleared();
                GD.Print($"MOD_FRAMEWORK_CONTRACT_PASS scenario=fault shape={initialize.ReturnType.Name} exception=preserved completion=failed");
                return;
            }

            await Initialize(initialize, io, settings);
            if (scenario == "contracts")
            {
                Exception reentrantError = null;
                try { await (reentrantInitialization ?? throw new Exception("The active-scope reentry did not execute.")); }
                catch (Exception error) { reentrantError = error; }
                Require(ReferenceEquals(reentrantError, io.ReentrantFailure),
                    "A reentrant call during native discovery replaced its original I/O error or captured the outer completion.");
            }
            RequireCurrentScopeCleared();
            Require(!SteamInitializer.Initialized, "The original discovery boundary initialized desktop Steam.");
            var mods = ModManager.Mods.ToArray();
            Require(mods.Length == 3 && mods.Select(mod => mod.manifest?.id).ToHashSet().SetEquals(new[] { EntryId, ResourceId, ManualId }),
                "One Workshop package's two manifests or the exact manual selection was lost or expanded.");
            Require(mods.All(mod => NativeManifests.ContainsKey(mod) && mod.state == ModLoadState.Loaded && (mod.errors == null || mod.errors.Count == 0)),
                "Native objects were reconstructed or original loading reported a failure.");
            Require(io.ManifestStreams.Contains(Path.Combine(input.WorkshopRoot, "notes.json"))
                && !io.ManifestStreams.Contains(Path.Combine(input.ManualRoot, "disabled.json")),
                "The original companion JSON path or exact manual selection boundary changed.");
            var entry = mods.Single(mod => mod.manifest.id == EntryId);
            var resources = mods.Single(mod => mod.manifest.id == ResourceId);
            var manual = mods.Single(mod => mod.manifest.id == ManualId);
            Require(DiscoveryOrder.IndexOf(entry) < DiscoveryOrder.IndexOf(resources),
                "The fixture did not discover its dependent before the resource dependency.");
            Require(Array.IndexOf(mods, resources) < Array.IndexOf(mods, entry) && Array.IndexOf(mods, manual) < Array.IndexOf(mods, entry)
                && DetectionOrder.SequenceEqual(mods), "Original topological ordering or completion notifications changed.");
            Require(!resources.manifest.hasDll && resources.manifest.hasPck && resources.manifest.version == null
                && resources.manifest.affectsGameplay && entry.manifest.affectsGameplay && !manual.manifest.affectsGameplay,
                "Original manifest defaults or explicit booleans were rewritten.");
            AssertDependencies(entry, manual, current);
            Require(Path.GetFullPath(entry.path) == input.WorkshopRoot && Path.GetFullPath(resources.path) == input.WorkshopRoot
                && Path.GetFullPath(manual.path) == input.ManualRoot,
                "Native mod paths no longer identify the selected physical source directories.");
            Require(entry.modSource == ModSource.SteamWorkshop && resources.modSource == ModSource.SteamWorkshop
                && manual.modSource == ModSource.ModsDirectory, "Original source kinds were lost.");
            var workshopField = typeof(Mod).GetField("workshopId");
            if (workshopField != null)
                Require((ulong?)workshopField.GetValue(entry) == WorkshopId && (ulong?)workshopField.GetValue(resources) == WorkshopId,
                    "A multi-manifest Workshop package lost its native item identity.");
            VerifyEntryAndSecondary(entry, mods, input.WorkshopRoot, input.ManualRoot);
            VerifyUnrelatedScriptCollision(runtimeDataDir, input.ManualRoot);
            var result = RequireResult(plan, attempt, sourceId, runtimeId);
            Require(result.InitializationError == null && result.Mods.Length == 3 && result.Sources.Length == 2
                && result.Mods.All(mod => mod.NativeState == nameof(ModLoadState.Loaded) && mod.NativeErrors.Length == 0)
                && result.Mods.Count(mod => mod.SelectionKey == $"workshop:{WorkshopId}") == 2,
                "Completion did not report actual native outcomes for multiple manifests per source.");
            Require(settings.PlayerAgreedToModLoading && settings.ModList.Count == 3,
                "The native manager did not complete its original settings update.");
            if (saveProbe != null) VerifySaveProbe(io, saveProbe);
            await VerifyRepeatedAndNestedInitialization(initialize, io, settings, harmony, scenario == "native-lifecycle", current);
            GD.Print($"MOD_FRAMEWORK_CONTRACT_PASS scenario=contracts shape={initialize.ReturnType.Name} sources=2 nativeMods=3 defaults=preserved dependencies=native earlySecondary=instantiated owners=independent");
        }
        finally { ModManager.OnModDetected -= ObserveDetection; }
    }

    private static async Task Initialize(MethodInfo initialize, IModManagerFileIo io, ModSettings settings)
    {
        object[] arguments = initialize.GetParameters().Length == 2
            ? new object[] { io, settings }
            : new object[] { io, settings, typeof(ReleaseInfoManager).GetProperty("SemVer")!.GetValue(ReleaseInfoManager.Instance) };
        try { if (initialize.Invoke(null, arguments) is Task task) await task; }
        catch (TargetInvocationException error) when (error.InnerException != null)
        { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); }
    }

    private static async Task VerifyRepeatedAndNestedInitialization(MethodInfo initialize, ContractFileIo io,
        ModSettings settings, Harmony integration, bool nativeControl, bool current)
    {
        var mods = ModManager.Mods.ToArray();
        var parsed = ParsedManifestCount;
        var completion = File.ReadAllBytes(AppPaths.AppPrivateLastModLaunchPath);
        if (nativeControl)
            integration.Unpatch(initialize, HarmonyPatchType.All, integration.Id);
        var observer = new Harmony("sts2-launcher.original-contract-lifecycle");
        var advisory = typeof(ModManager).GetMethod("CheckSteamBranchSupport", BindingFlags.Static | BindingFlags.NonPublic);
        // The lifecycle control has no desktop Steam client. Keep the advisory
        // transport outside both probes, after its real integration was tested above.
        if (advisory != null)
            observer.Patch(advisory, prefix: new HarmonyMethod(typeof(ModFrameworkContractTest), nameof(SkipOfflineLifecycleAdvisory))
            {
                priority = Priority.First,
            });
        observer.Patch(initialize, postfix: new HarmonyMethod(typeof(ModFrameworkContractTest), nameof(InitializeAgainPostfix))
        {
            priority = Priority.First,
        });
        _nestedInitialize = () => Initialize(initialize, io, settings);
        try
        {
            await Initialize(initialize, io, settings);
            Require(_nestedInitialization != null, "The bounded native lifecycle probe did not reenter Initialize.");
            await _nestedInitialization;
        }
        finally
        {
            _nestedInitialize = null;
            observer.Unpatch(initialize, HarmonyPatchType.All, observer.Id);
            if (advisory != null) observer.Unpatch(advisory, HarmonyPatchType.All, observer.Id);
        }
        RequireCurrentScopeCleared();
        Require(ParsedManifestCount == parsed && ModManager.Mods.SequenceEqual(mods),
            "Repeated initialization injected a second selected-source discovery instead of preserving the native lifecycle.");
        var expected = current ? ModLoadState.Loaded : ModLoadState.AddedAtRuntime;
        Require(mods.All(mod => mod.state == expected),
            $"Repeated initialization changed the native {initialize.ReturnType.Name} state guard: expected {expected}.");
        var initializer = AppDomain.CurrentDomain.GetAssemblies().Single(assembly => assembly.GetName().Name == EntryId)
            .GetType("Sts2Launcher.ModFrameworkFixtures.EntryInitializer", throwOnError: true)!;
        Require((int)initializer.GetField("InitializerCalls")!.GetValue(null) == 1,
            "Repeated initialization bypassed the native guard and ran an entry initializer again.");
        Require(completion.SequenceEqual(File.ReadAllBytes(AppPaths.AppPrivateLastModLaunchPath)),
            "Repeated or nested initialization replaced the first completed launch's native outcomes.");
        GD.Print($"MOD_FRAMEWORK_LIFECYCLE_PASS control={nativeControl} shape={initialize.ReturnType.Name} nativeState={expected} repeat=preserved nested=preserved completion=unchanged");
    }

    private static void InitializeAgainPostfix()
    {
        var initialize = _nestedInitialize;
        if (initialize == null) return;
        _nestedInitialize = null;
        _nestedInitialization = initialize();
    }

    private static bool SkipOfflineLifecycleAdvisory(ref Task __result)
    {
        __result = Task.CompletedTask;
        return false;
    }

    private static void ObserveManifest(string __0, Mod __result)
    {
        if (__result == null) return;
        NativeManifests.Add(__result, __0);
        DiscoveryOrder.Add(__result);
    }
    private static void ObserveDetection(Mod mod) => DetectionOrder.Add(mod);
    private static void RequireCurrentScopeCleared()
        => Require(typeof(ModLoaderPatches).GetField("_current", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null) == null,
            "Original initialization completion leaked its load scope.");

    private static LauncherModLaunchResultDocument RequireResult(LauncherModLaunchPlan plan, string attempt, string sourceId, string runtimeId)
    {
        var result = LauncherModLaunchResultStore.Read(AppPaths.AppPrivateLastModLaunchPath);
        Require(LauncherModLaunchResultStore.IsValid(result) && result.Version == 2 && result.AttemptId == attempt
            && result.GameIdentityId == sourceId && result.RuntimePackId == runtimeId
            && result.ConfigurationId == LauncherModLaunchResultStore.ConfigurationId(plan, sourceId, runtimeId),
            "The schema-2 result lost its exact source/runtime/attempt identity.");
        return result;
    }

    private static void AssertDependencies(Mod entry, Mod manual, bool current)
    {
        var dependencies = ((IEnumerable)typeof(ModManifest).GetField("dependencies")!.GetValue(entry.manifest)).Cast<object>().ToArray();
        Require(dependencies.Length == 2, "Native parsing lost a dependency.");
        if (!current)
        {
            Require(dependencies.Cast<string>().SequenceEqual(new[] { ResourceId, ManualId }), "Legacy string dependencies were rewritten.");
            return;
        }
        Require((string)dependencies[0].GetType().GetField("id")!.GetValue(dependencies[0]) == ResourceId
            && dependencies[0].GetType().GetField("minVersion")!.GetValue(dependencies[0]) == null
            && (string)dependencies[1].GetType().GetField("minVersion")!.GetValue(dependencies[1]) == "1.2.3-beta.2",
            "Current dependency objects lost null/min-version semantics.");
        var parsed = typeof(Mod).GetField("version")!.GetValue(manual);
        Require(parsed?.ToString() == "v1.2.3-beta.10", "The current native semantic parser did not retain the prerelease version.");
    }

    private static void VerifyEntryAndSecondary(Mod entry, Mod[] mods, string root, string secondaryRoot)
    {
        var assemblies = typeof(Mod).GetField("assembly")?.GetValue(entry) is Assembly legacy
            ? new[] { legacy }
            : ((IEnumerable)typeof(Mod).GetField("assemblies")!.GetValue(entry)).Cast<Assembly>().ToArray();
        var main = assemblies.Single(assembly => assembly.GetName().Name == EntryId);
        Require(Path.GetFullPath(main.Location) == Path.Combine(root, EntryId + ".dll"), "The main mod DLL lost its physical assembly path.");
        Require(!main.GetCustomAttributesData().Any(attribute => attribute.AttributeType.FullName == "Godot.AssemblyHasScriptsAttribute"),
            "The ordinary entry fixture unexpectedly supplied Godot script metadata.");
        var initializer = main.GetType("Sts2Launcher.ModFrameworkFixtures.EntryInitializer", throwOnError: true)!;
        Require((int)initializer.GetField("InitializerCalls")!.GetValue(null) == 1
            && (bool)initializer.GetField("InitializerReturned")!.GetValue(null), "The original entry initializer did not complete once.");
        var originals = (object[])initializer.GetField("NativeObjects")!.GetValue(null);
        Require(originals.Length == mods.Length && mods.All(mod => originals.Any(original => ReferenceEquals(original, mod))),
            "The initializer saw replacement mod objects instead of the actual native list.");
        var registry = (IDictionary)initializer.GetField("Registry")!.GetValue(null);
        Require((string)registry["resources"] == "resource-only-contract" && (string)registry["manual"] == "manual-contract",
            "Original resource-only/dependency PCKs were not mounted before the combined initializer.");
        var secondary = (Assembly)registry["secondary"];
        Require(Path.GetFullPath(secondary.Location) == Path.Combine(secondaryRoot, "nested", "FixtureSecondary.dll"),
            "The dependency's secondary assembly lost its physical source path.");
        var instance = (Node)registry["instance"];
        try
        {
            Require(instance.GetType().Assembly == secondary
                && (int)instance.GetType().GetField("ConstructorCalls")!.GetValue(null) > 0
                && (bool)instance.GetType().GetField("ConstructedBeforeInitializerReturned")!.GetValue(null),
                "The real Godot SDK script did not instantiate while the entry initializer was still running.");
        }
        finally { instance.Free(); }
        Require((int)registry["patched-value"] == 32 && (int)initializer.GetMethod("OriginalValue")!.Invoke(null, null) == 32,
            "The unrelated Harmony owner did not install its functional patch.");
        Require((int)registry["duplicate-lookups"] == 2, "Repeated script registration prevented early instantiation.");
        Require((string)initializer.GetField("HarmonyOwner")!.GetRawConstantValue() != entry.manifest.id,
            "The fixture owner accidentally matched its manifest ID.");
    }

    private static void VerifyUnrelatedScriptCollision(string runtimeDataDir, string sourceRoot)
    {
        // A different assembly object outside the prepared source owns the same
        // generated script path. The original Godot collision must still fail.
        var directory = Path.Combine(runtimeDataDir, "unrelated-script-collision");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "FixtureSecondary.dll");
        File.Copy(Path.Combine(sourceRoot, "nested", "FixtureSecondary.dll"), path);
        var context = new AssemblyLoadContext("unrelated-script-collision");
        context.Resolving += (_, name) => name.Name == typeof(Node).Assembly.GetName().Name ? typeof(Node).Assembly : null;
        var unrelated = context.LoadFromAssemblyPath(path);
        try
        {
            Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(unrelated);
            throw new Exception("An unrelated assembly's duplicate script path was silently accepted.");
        }
        catch (ArgumentException error)
        {
            Require(error.StackTrace?.Contains("Godot", StringComparison.Ordinal) == true,
                "The fixture observed an argument failure before the original Godot registration.");
        }
    }

    private static Inputs CreateInputs(string runtimeDataDir, bool current)
    {
        var fixtureRoot = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "ModFrameworkFixtures"));
        var main = Path.Combine(fixtureRoot, "Entry", "bin", "Release", "net9.0", "FixtureEntry.dll");
        var secondary = Path.Combine(fixtureRoot, "Secondary", ".godot", "mono", "temp", "bin", "Release", "FixtureSecondary.dll");
        Require(File.Exists(main) && File.Exists(secondary), "Build the Entry and Secondary ModFrameworkFixtures projects in Release first.");
        using (var module = Mono.Cecil.ModuleDefinition.ReadModule(secondary))
        {
            Require(module.Assembly.CustomAttributes.Any(attribute => attribute.AttributeType.FullName == "Godot.AssemblyHasScriptsAttribute"),
                "The secondary fixture is missing normal Godot SDK script metadata.");
            var script = module.GetType("Sts2Launcher.ModFrameworkFixtures.Secondary.FrameworkSecondaryNode");
            Require(script.CustomAttributes.Any(attribute => attribute.AttributeType.FullName == "Godot.ScriptPathAttribute"
                && (string)attribute.ConstructorArguments[0].Value == "res://FixtureSecondary/FrameworkSecondaryNode.cs"),
                "The secondary fixture's generated script path does not match its PackedScene.");
        }
        var root = Path.Combine(runtimeDataDir, "contract-inputs");
        var workshop = Path.Combine(root, "workshop-package");
        var manual = Path.Combine(root, "manual-package");
        var parts = Path.Combine(root, "parts");
        Directory.CreateDirectory(workshop);
        Directory.CreateDirectory(Path.Combine(manual, "nested"));
        Directory.CreateDirectory(parts);
        File.Copy(main, Path.Combine(workshop, EntryId + ".dll"));
        File.Copy(secondary, Path.Combine(manual, "nested", "FixtureSecondary.dll"));
        object dependencies = current
            ? new[] { new { id = ResourceId, min_version = (string)null }, new { id = ManualId, min_version = "1.2.3-beta.2" } }
            : new[] { ResourceId, ManualId };
        WriteJson(Path.Combine(workshop, "00-entry.json"), new { id = EntryId, name = "Combined entry", version = "2.0.0-rc.1", has_dll = true, has_pck = true, dependencies });
        WriteJson(Path.Combine(workshop, "99-resources.json"), new { id = ResourceId, name = "Resource only", has_pck = true });
        WriteJson(Path.Combine(workshop, "notes.json"), new { companion = "not a mod manifest" });
        WriteJson(Path.Combine(manual, "disabled.json"), new { id = DisabledId, has_dll = true });
        WriteJson(Path.Combine(manual, "selected.json"), new { id = ManualId, name = "Exact manual source", version = "1.2.3-beta.10", has_pck = true, affects_gameplay = false });
        var resourceMarker = Path.Combine(parts, "resources.txt");
        var manualMarker = Path.Combine(parts, "manual.txt");
        File.WriteAllText(resourceMarker, "resource-only-contract");
        File.WriteAllText(manualMarker, "manual-contract");
        WritePack(Path.Combine(workshop, ResourceId + ".pck"), new Dictionary<string, string> { ["res://FixtureResources/marker.txt"] = resourceMarker });
        WritePack(Path.Combine(manual, ManualId + ".pck"), new Dictionary<string, string> { ["res://FixtureManual/marker.txt"] = manualMarker });
        WritePack(Path.Combine(workshop, EntryId + ".pck"), new Dictionary<string, string>
        {
            ["res://FixtureEntry/early.tscn"] = Path.Combine(fixtureRoot, "Secondary", "FixtureSecondary", "early.tscn"),
            ["res://FixtureSecondary/FrameworkSecondaryNode.cs"] = Path.Combine(fixtureRoot, "Secondary", "FixtureSecondary", "FrameworkSecondaryNode.cs"),
        });
        return new Inputs(workshop, manual);
    }

    private static void WriteJson(string path, object value) => File.WriteAllBytes(path, JsonSerializer.SerializeToUtf8Bytes(value));
    private static void WritePack(string path, IReadOnlyDictionary<string, string> files)
    {
        using var pack = new PckPacker();
        Require(pack.PckStart(path) == Error.Ok, "The fixture PCK could not be created.");
        foreach (var pair in files) Require(pack.AddFile(pair.Key, pair.Value) == Error.Ok, "A fixture PCK resource could not be added.");
        Require(pack.Flush() == Error.Ok, "The fixture PCK could not be finalized.");
    }

    private static SaveProbe PrepareSaveProbe(string runtimeDataDir, ContractFileIo io)
    {
        var game = typeof(ModManager).Assembly;
        var provider = game.GetType("MegaCrit.Sts2.Core.Saves.UserDataPathProvider", throwOnError: true)!;
        var account = provider.GetMethod("GetAccountScopedBasePath")!;
        var arguments = new object[account.GetParameters().Length];
        var basePath = (string)account.Invoke(null, arguments);
        arguments[0] = "modded/";
        var moddedPath = (string)account.Invoke(null, arguments);
        var profiles = game.GetType("MegaCrit.Sts2.Core.Saves.Managers.ProfileSaveManager", throwOnError: true)!;
        var profilePath = profiles.GetMethod("GetProfileSavePath")!;
        var source = basePath.TrimEnd('/') + "/" + (string)profilePath.Invoke(null, new object[] { (bool?)false });
        var target = basePath.TrimEnd('/') + "/" + (string)profilePath.Invoke(null, new object[] { (bool?)true });
        string Physical(string path)
        {
            var physical = Path.GetFullPath(ProjectSettings.GlobalizePath(path));
            Require(physical.StartsWith(runtimeDataDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
                "Native save bookkeeping escaped the isolated user directory.");
            return physical;
        }
        var physicalSource = Physical(source);
        var physicalTarget = Physical(target);
        var physicalModded = Physical(moddedPath);
        Directory.CreateDirectory(Path.GetDirectoryName(physicalSource)!);
        Directory.CreateDirectory(physicalModded);
        File.WriteAllText(physicalSource, "native-first-time-profile");
        File.WriteAllText(physicalTarget, "existing-modded-profile");
        typeof(ModManager).GetMethod("CopyUnmoddedSaveFilesIfNeeded")!.Invoke(null, new object[] { io });
        Require(File.ReadAllText(physicalTarget) == "existing-modded-profile" && io.Copies.Count == 0,
            "The native existing-destination guard overwrote a modded profile.");
        File.Delete(physicalTarget);
        Directory.Delete(physicalModded, recursive: false);
        return new SaveProbe(physicalSource, physicalTarget, io.Copies.Count);
    }

    private static void VerifyNativeSavePathNormalization(string runtimeDataDir)
    {
        var platform = PlatformUtil.PrimaryPlatform;
        var userId = PlatformUtil.GetLocalPlayerId(platform);
        var prefix = $"user://{UserDataPathProvider.GetPlatformDirectoryName(platform)}/{userId}";
        var account = UserDataPathProvider.GetAccountScopedBasePath(null);
        Require(account == prefix, "The Android-only postfix changed desktop provider output.");
        var profileDirectory = UserDataPathProvider.GetProfileDir(1);
        var cases = new (string Original, string Expected)[]
        {
            (account, "user://"),
            (UserDataPathProvider.GetAccountScopedBasePath("modded/"), "user://modded/"),
            (UserDataPathProvider.GetAccountScopedBasePath("profile.save"), "user://profile.save"),
            (UserDataPathProvider.GetAccountScopedBasePath("modded/profile1/progress.save"), "user://modded/profile1/progress.save"),
            (UserDataPathProvider.GetProfileScopedBasePath(1), "user://" + profileDirectory),
            (UserDataPathProvider.GetProfileScopedPath(1, "contract.log"), "user://" + profileDirectory + "/contract.log"),
        };
        var userRoot = Path.GetFullPath(OS.GetUserDataDir());
        Require(userRoot.StartsWith(runtimeDataDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
            "The native save-path fixture escaped its isolated user directory.");
        foreach (var test in cases)
        {
            var normalized = AndroidSavePathNormalizer.Normalize(test.Original, prefix);
            Require(normalized == test.Expected, "The original provider suffix was lost during Android normalization: " + test.Original);
            Require(Path.TrimEndingDirectorySeparator(Path.GetFullPath(ProjectSettings.GlobalizePath(normalized)))
                == Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Combine(userRoot, test.Expected.Substring("user://".Length)))),
                "The normalized native path does not map to AndroidLocalSaveStore's user-data root.");
        }
        var otherUser = userId == ulong.MaxValue ? userId - 1 : userId + 1;
        var otherUserPath = UserDataPathProvider.GetAccountScopedBasePath("profile.save", userIdOverride: otherUser);
        Require(AndroidSavePathNormalizer.Normalize(otherUserPath, prefix, hasOtherAccountOverride: true) == otherUserPath,
            "An explicit native user override was normalized into the default store.");
        var otherPlatform = Enum.GetValues<PlatformType>().First(value => value != platform);
        var otherPlatformPath = UserDataPathProvider.GetAccountScopedBasePath("profile.save", otherPlatform, userId);
        Require(AndroidSavePathNormalizer.Normalize(otherPlatformPath, prefix, hasOtherAccountOverride: true) == otherPlatformPath,
            "An explicit native platform override was normalized into the default store.");
        foreach (var name in new[] { "GetAccountScopedBasePath", "GetProfileScopedBasePath", "GetProfileScopedPath" })
            Require(Harmony.GetPatchInfo(typeof(UserDataPathProvider).GetMethod(name))?.Postfixes
                .Any(patch => patch.PatchMethod.Name == "NormalizeUserDataPath") == true,
                "The required provider postfix was not installed: " + name);
    }

    private static void VerifySaveProbe(ContractFileIo io, SaveProbe probe)
    {
        Require(io.Copies.Count > probe.CopyCount && File.ReadAllText(probe.Target) == File.ReadAllText(probe.Source)
            && (bool)typeof(ModManager).GetProperty("UnmoddedSavesWereCopied")!.GetValue(null),
            "Actual initialization with empty settings did not retain native first-time save copying.");
    }

    private sealed record Inputs(string WorkshopRoot, string ManualRoot);
    private sealed record SaveProbe(string Source, string Target, int CopyCount);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    private class ContractFileIo : IModManagerFileIo
    {
        private readonly IModManagerFileIo _inner = new ModManagerFileIo();
        private readonly string _failureRoot;
        internal readonly IOException DiscoveryFailure = new("original-native-discovery-fault");
        internal readonly IOException ReentrantFailure = new("original-native-reentrant-directory-fault");
        internal readonly HashSet<string> ManifestStreams = new(StringComparer.OrdinalIgnoreCase);
        internal readonly List<(string Source, string Target)> Copies = new();
        internal Action BeforeFirstDirectoryRead;
        internal bool FailNextDirectoryCheck;
        internal ContractFileIo(string failureRoot) { _failureRoot = failureRoot; }
        public virtual string[] GetFilesAt(string path)
        {
            var before = BeforeFirstDirectoryRead;
            BeforeFirstDirectoryRead = null;
            before?.Invoke();
            if (_failureRoot != null && Path.GetFullPath(path) == _failureRoot) throw DiscoveryFailure;
            return _inner.GetFilesAt(path);
        }
        public virtual string[] GetDirectoriesAt(string path) => _inner.GetDirectoriesAt(path);
        public virtual bool FileExists(string path) => _inner.FileExists(path);
        public virtual bool DirectoryExists(string path)
        {
            if (FailNextDirectoryCheck)
            {
                FailNextDirectoryCheck = false;
                throw ReentrantFailure;
            }
            return _inner.DirectoryExists(path);
        }
        public virtual Stream OpenStream(string path, Godot.FileAccess.ModeFlags mode)
        {
            if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) ManifestStreams.Add(path);
            return _inner.OpenStream(path, mode);
        }
        public virtual void MakeDirRecursive(string path)
            => DirAccess.MakeDirRecursiveAbsolute(path);
        public virtual Error CopyFile(string source, string target)
        {
            Copies.Add((source, target));
            return DirAccess.CopyAbsolute(source, target);
        }
    }
}
