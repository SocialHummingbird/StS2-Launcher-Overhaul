using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Platform.Steam;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile;
using STS2Mobile.Launcher;
using STS2Mobile.Patches;
using STS2Mobile.Steam.Workshop;

namespace LauncherUiPreview;

internal static class ModRuntimeActivationTest
{
    private const string Importer = "ImportVanillaSaves";
    private static int _nativeManifestReads;
    private static readonly HashSet<object> NativeObjects = new(ReferenceEqualityComparer.Instance);
    private static void ObserveNativeManifest(object __result)
    {
        _nativeManifestReads++;
        if (__result != null) NativeObjects.Add(__result);
    }

    internal static async Task RunAsync(string runtimeDataDir, string baseGamePckPath,
        string importVanillaSavesRoot, string baseLibRoot, string scenario, string gameAssemblyPath,
        string aramRoot, string quickRestartRoot, string additionalModRoot)
    {
        runtimeDataDir = Path.GetFullPath(runtimeDataDir);
        System.Environment.SetEnvironmentVariable(AppPaths.LauncherPreviewDataDirEnvironmentVariable, runtimeDataDir);
        Require(Path.GetFullPath(AppPaths.AppPrivateDataDir) == runtimeDataDir,
            "The native framework fixture escaped its isolated app data.");
        Require(Path.GetFullPath(ProjectSettings.GlobalizePath("user://")).StartsWith(runtimeDataDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
            "Godot save paths escaped the isolated app data.");
        Require(ProjectSettings.LoadResourcePack(baseGamePckPath), "The supplied game PCK did not mount.");
        ModManagerCompatibilityTest.Run(runtimeDataDir);
        PlatformBranchCompatibilityTest.Run();
        var manifest = SteamWorkshopSyncManifest.Empty(AppPaths.AppPrivateWorkshopDownloadsDir, AppPaths.AppPrivateWorkshopStagedModsDir);
        AddSource(manifest, 3747503308, importVanillaSavesRoot, Importer);
        if (scenario is "stage9-chain" or "representative") AddSource(manifest, 3737335127, baseLibRoot, "BaseLib");
        if (scenario == "representative")
        {
            AddSource(manifest, 3747501308, aramRoot, "ARAM: Mayhem");
            AddSource(manifest, 3737322022, quickRestartRoot, "Quick Restart 2");
            AddSource(manifest, 3749027556, additionalModRoot, "CookieCursor");
        }
        new SteamWorkshopSyncStateStore(AppPaths.AppPrivateWorkshopManifestPath).Save(manifest);
        var selection = new LauncherModSelectionDocument
        {
            PlayMode = scenario == "vanilla" ? "vanilla" : "modded",
            EnabledMods = manifest.Items.ToDictionary(item => "workshop:" + item.PublishedFileId,
                _ => scenario != "disabled", StringComparer.OrdinalIgnoreCase),
        };
        LauncherModSelectionState.Save(AppPaths.AppPrivateModSelectionPath, selection);
        WorkshopModConsent.Accept("isolated original-framework fixture");
        var resolution = LauncherModLaunchPlan.Resolve(selection);
        Require(resolution.Success, resolution.Error?.Message ?? "Source preparation failed.");
        var plan = resolution.Plan;
        Require(plan.Sources.Length == (scenario is "vanilla" or "disabled" ? 0 : manifest.Items.Count),
            "Disabled/Vanilla sources leaked into native discovery.");
        using (var module = Mono.Cecil.ModuleDefinition.ReadModule(gameAssemblyPath))
            Require(module.Mvid == typeof(ModManager).Assembly.ManifestModule.ModuleVersionId,
                "The process loaded a different game assembly from the selected fixture.");
        var sourceIdentity = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(gameAssemblyPath)));
        var packIdentity = "desktop-fixture:" + typeof(Harmony).Assembly.ManifestModule.ModuleVersionId;
        var releasePath = Path.Combine(Path.GetDirectoryName(baseGamePckPath)!, "release_info.json");
        // The legacy fixture has no version parameter and no paired release JSON.
        // Current-game checks use the real selected installation's release file.
        ModLoaderPatches.Prepare(plan, "offline-framework-fixture", sourceIdentity, packIdentity,
            ModLoaderPatches.FindInitializeTarget(typeof(ModManager))!.ReturnType == typeof(Task) ? releasePath : "", "public-beta");
        var harmony = new Harmony("sts2-launcher.framework-fixture");
        ModLoaderPatches.Apply(harmony);
        PlatformBranchCompatibilityTest.VerifyCapturedBranch("public-beta");
        new Harmony("sts2-launcher.native-discovery-observer").Patch(
            AccessTools.Method(typeof(ModManager), "ReadModManifest"),
            postfix: new HarmonyMethod(typeof(ModRuntimeActivationTest), nameof(ObserveNativeManifest)));
        Require(!SteamInitializer.Initialized, "The fixture unexpectedly initialized desktop Steam.");
        var settings = new ModSettings { PlayerAgreedToModLoading = false };
        var initialize = ModLoaderPatches.FindInitializeTarget(typeof(ModManager))!;
        object[] arguments = initialize.GetParameters().Length == 2
            ? new object[] { new ModManagerFileIo(), settings }
            : new object[] { new ModManagerFileIo(), settings, typeof(ReleaseInfoManager).GetProperty("SemVer")!.GetValue(ReleaseInfoManager.Instance) };
        try
        {
            if (initialize.Invoke(null, arguments) is Task task) await task;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        { ExceptionDispatchInfo.Capture(ex.InnerException).Throw(); }
        Require(!SteamInitializer.Initialized, "Android source discovery called desktop Steam.");
        var loaded = ModManager.Mods.ToArray();
        if (plan.Sources.Length == 0)
        {
            Require(loaded.Length == 0 && _nativeManifestReads == 0, "Unselected packages reached the game loader.");
        }
        else
        {
            Require(_nativeManifestReads > 0 && loaded.All(mod => NativeObjects.Contains(mod)),
                "Selected packages bypassed original manifest discovery or reconstructed native objects.");
            Require(loaded.Length == plan.Sources.Length, "The original framework did not discover all selected packages.");
            foreach (var mod in loaded)
            {
                Require(mod.state == ModLoadState.Loaded && (mod.errors == null || mod.errors.Count == 0),
                    $"Native initialization failed for {mod.manifest?.id}: {mod.state}; {FormatErrors(mod)}");
                var source = plan.Sources.Where(item => Path.GetFullPath(mod.path).StartsWith(Path.GetFullPath(item.RootPath), StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(item => item.RootPath.Length).FirstOrDefault();
                Require(source != null, "Native mod directory escaped the selected physical package: " + mod.path);
                Require(Assemblies(mod).Any(assembly => Path.GetFullPath(assembly.Location).StartsWith(source.RootPath, StringComparison.OrdinalIgnoreCase)),
                    "The framework lost the physical mod assembly path.");
                var workshopId = typeof(Mod).GetField("workshopId");
                if (workshopId != null)
                    Require((ulong?)workshopId.GetValue(mod) == source.WorkshopId, "Workshop identity was not preserved.");
            }
            Require(settings.PlayerAgreedToModLoading && settings.ModList.Count == loaded.Length,
                "Original completion did not update the provided settings.");
            Require(ResourceLoader.Exists("res://ImportVanillaSaves/import_button.tscn"), "Importer resources were not mounted.");
            var scene = ResourceLoader.Load<PackedScene>("res://ImportVanillaSaves/import_button.tscn");
            using var button = scene.Instantiate();
            Require(button.GetType().Assembly.GetName().Name == Importer,
                "Godot did not instantiate the mod's registered C# script.");
            var target = AccessTools.PropertyGetter(typeof(UserDataPathProvider), "IsRunningModded");
            Require(Harmony.GetPatchInfo(target)?.Owners.Contains(Importer) == true,
                "The original importer initializer did not install its functional patch.");
            if (scenario is "stage9-chain" or "representative")
            {
                Require(ResourceLoader.Exists("res://BaseLib/mod_image.png"), "BaseLib resources did not mount.");
                Require(!loaded.Single(mod => mod.manifest?.id == "BaseLib").errors?.Any() ?? true,
                    "Full BaseLib initialization reported errors.");
            }
        }
        if (scenario == "representative") VerifyModelRegistry();
        var result = LauncherModLaunchResultStore.Read(AppPaths.AppPrivateLastModLaunchPath);
        Require(result != null && result.Version == 2 && result.AttemptId == "offline-framework-fixture"
            && result.GameIdentityId == sourceIdentity && result.RuntimePackId == packIdentity
            && result.ConfigurationId == LauncherModLaunchResultStore.ConfigurationId(plan, sourceIdentity, packIdentity)
            && result.Mods.Length == loaded.Length && result.Mods.All(mod => mod.NativeState == "Loaded" && mod.NativeErrors.Length == 0),
            "Framework completion was not reported against the exact launch configuration.");
        Require(typeof(ModLoaderPatches).GetField("_current", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null) == null,
            "Initialization scope leaked after completion.");
        File.WriteAllText(Path.Combine(runtimeDataDir, "framework-result.json"), JsonSerializer.Serialize(new
        {
            passed = true, scenario, gameAssemblySha256 = sourceIdentity,
            shape = initialize.ReturnType.Name, discovered = loaded.Length, nativeManifestReads = _nativeManifestReads,
        }));
        GD.Print($"MOD_RUNTIME_FRAMEWORK_PASS scenario={scenario} shape={initialize.ReturnType.Name} discovered={loaded.Length} scripts=instantiated");
    }

    private static void VerifyModelRegistry()
    {
        typeof(SaveManager).GetMethod("InitSettingsData")!.Invoke(SaveManager.Instance, null);
        MegaCrit.Sts2.Core.Localization.LocManager.Initialize();
        typeof(ModManager).Assembly.GetType("MegaCrit.Sts2.Core.Modding.AssemblyInfo", throwOnError: true)!
            .GetMethod("Init", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(null, null);
        var initialize = typeof(MegaCrit.Sts2.Core.Models.ModelDb).GetMethod("Init")!;
        initialize.Invoke(null, initialize.GetParameters().Length == 0 ? null : new object[] { null });
        MegaCrit.Sts2.Core.Multiplayer.Serialization.ModelIdSerializationCache.Init();
        MegaCrit.Sts2.Core.Models.ModelDb.InitIds();
        var acts = MegaCrit.Sts2.Core.Models.ModelDb.Acts;
        Require(acts.Any(), "Native model construction produced no acts.");
        var encounters = MegaCrit.Sts2.Core.Models.ModelDb.AllEncounters.ToArray();
        Require(encounters.Length > 0 && ReferenceEquals(acts, MegaCrit.Sts2.Core.Models.ModelDb.Acts),
            "Repeated native model access lost the cached acts or encounters.");
        Require(MegaCrit.Sts2.Core.Models.ModelDb.Act<MegaCrit.Sts2.Core.Models.Acts.Overgrowth>() != null,
            "Native model lookup lost an initialized act.");
        GD.Print("MOD_MODEL_REGISTRY_PASS construction=original acts=cached encounters=readable");
    }

    private static void AddSource(SteamWorkshopSyncManifest manifest, ulong id, string root, string title)
    {
        root = Path.GetFullPath(root);
        Require(Directory.Exists(root), "Explicit mod package does not exist: " + root);
        var files = SteamWorkshopPackageInventory.Capture(root);
        var digest = SteamWorkshopPackageInventory.ContentSha256(files);
        manifest.Items.Add(new SteamWorkshopSyncManifestItem
        {
            PublishedFileId = id, Title = title, Status = "staged", StagedDirectory = root,
            Installed = new SteamWorkshopInstallation { Generation = digest, Directory = root, Files = files,
                ContentSha256 = digest, SelectedGameBranch = "public-beta", RevisionProvenance = "explicit-desktop-fixture" },
        });
    }
    private static IEnumerable<Assembly> Assemblies(Mod mod)
    {
        var single = typeof(Mod).GetField("assembly")?.GetValue(mod) as Assembly;
        if (single != null) yield return single;
        if (typeof(Mod).GetField("assemblies")?.GetValue(mod) is IEnumerable values)
            foreach (var value in values) if (value is Assembly assembly) yield return assembly;
    }
    private static string FormatErrors(Mod mod)
        => string.Join(", ", mod.errors?.Select(error => error.LocTable + "/" + error.LocEntryKey) ?? Array.Empty<string>());
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
