using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Localization;
using STS2Mobile.Launcher;
using STS2Mobile.Steam.Workshop;

namespace STS2Mobile.Patches;

// Supplies physical Android sources to original discovery. All manifest, load,
// dependency and completion decisions remain with the selected game's manager.
internal static class ModLoaderPatches
{
    private const BindingFlags Static = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private static readonly object Gate = new();
    private static readonly object ScriptGate = new();
    private static readonly HashSet<Assembly> RegisteredScripts = new(ReferenceEqualityComparer.Instance);
    private static readonly AsyncLocal<ScriptScope> Scripts = new();
    private static PreparedLaunch _prepared;
    private static LoadScope _current;
    private static Harmony _harmony;
    private static MethodInfo _scan;
    private static bool _startupCleanupComplete;
    internal static string PreparedGameBranch => _prepared?.GameBranch;

    internal static void Apply(Harmony harmony)
    {
        if (OperatingSystem.IsAndroid() && !_startupCleanupComplete)
        {
            try { new SteamWorkshopStager().CleanupBeforeFirstLoad(); }
            catch (SteamWorkshopInventoryException error)
            {
                PatchHelper.Log($"[Workshop] Deferred cleanup because the committed inventory is unavailable: {error.Message}");
            }
            _startupCleanupComplete = true;
        }
        _harmony = harmony;
        var initialize = FindInitializeTarget(typeof(ModManager))
            ?? throw new MissingMethodException("No supported ModManager.Initialize signature.");
        _scan = RequireMethod(typeof(ModManager), "ReadModsInDirRecursive", typeof(string), typeof(ModSource), typeof(List<Mod>));
        Patch(harmony, initialize, nameof(InitializePrefix), initialize.ReturnType == typeof(Task) ? nameof(InitializeTaskPostfix) : nameof(InitializePostfix), nameof(InitializeFinalizer));
        Patch(harmony, _scan, nameof(DiscoveryPrefix));
        Patch(harmony, RequireMethod(typeof(ModManager), "ReadModManifest", typeof(string), typeof(ModSource)), nameof(ManifestPrefix), nameof(ManifestPostfix));
        Patch(harmony, RequireMethod(typeof(ModManager), "ReadSteamMods"), nameof(SteamDiscoveryPrefix));
        var branchCheck = typeof(ModManager).GetMethod("CheckSteamBranchSupport", Static);
        if (branchCheck != null) Patch(harmony, branchCheck, nameof(BranchSupportPrefix));
        Patch(harmony, RequireMethod(typeof(ModManager), "TryLoadMod", typeof(Mod)), nameof(ModLoadPrefix), finalizer: nameof(ModLoadFinalizer));
        Patch(harmony, typeof(AssemblyLoadContext).GetMethod(nameof(AssemblyLoadContext.LoadFromAssemblyPath), new[] { typeof(string) }), postfix: nameof(AssemblyLoadedPostfix));
        Patch(harmony, typeof(Godot.Bridge.ScriptManagerBridge).GetMethod(nameof(Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly)), nameof(ScriptRegistrationPrefix), nameof(ScriptRegistrationPostfix), nameof(ScriptRegistrationFinalizer));
        Patch(harmony, RequireMethod(typeof(ReleaseInfoManager), "GetPossibleReleaseInfoPaths"), nameof(ReleasePathsPrefix));
        if (!IsIntegrationInstalled()) throw new InvalidOperationException("Original mod framework integration is not installed.");
    }

    internal static MethodInfo FindInitializeTarget(Type type)
        => type.GetMethods(Static).SingleOrDefault(method =>
        {
            var parameters = method.GetParameters();
            return method.Name == "Initialize" && parameters.Length >= 2
                && parameters[0].ParameterType.FullName == typeof(IModManagerFileIo).FullName
                && parameters[1].ParameterType.FullName == typeof(ModSettings).FullName
                && ((method.ReturnType == typeof(void) && parameters.Length == 2)
                    || (method.ReturnType == typeof(Task) && parameters.Length == 3
                        && parameters[2].ParameterType.FullName == "MegaCrit.Sts2.Core.Debug.SemanticVersion"));
        });

    internal static bool IsIntegrationInstalled()
        => Harmony.GetPatchInfo(FindInitializeTarget(typeof(ModManager)))?.Prefixes.Any(patch => patch.owner == _harmony?.Id && patch.PatchMethod.Name == nameof(InitializePrefix)) == true;

    internal static void Prepare(LauncherModLaunchPlan plan, string attemptId, string gameIdentityId, string runtimePackId, string releaseInfoPath, string gameBranch = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        lock (Gate)
        {
            if (_current != null) throw new InvalidOperationException("Mod initialization is already running.");
            var support = plan.Sources.Any(source => source.WorkshopId.HasValue)
                ? new SteamWorkshopStager().LoadManifest().Items.Where(item => item.Installed?.BranchSupport != null)
                    .GroupBy(item => item.PublishedFileId).ToDictionary(group => group.Key, group => group.First().Installed)
                : new Dictionary<ulong, SteamWorkshopInstallation>();
            _prepared = new PreparedLaunch(plan, attemptId, gameIdentityId, runtimePackId, releaseInfoPath,
                LauncherModSelectionState.SelectionFingerprint(LauncherModSelectionState.Load()),
                gameBranch ?? LauncherPreferences.ReadGameBranch(), support);
        }
    }

    private static bool ReleasePathsPrefix(ref string[] __result)
    {
        if (_prepared == null)
        {
            if (OperatingSystem.IsAndroid()) throw new InvalidOperationException("Release information was requested before an installation was prepared.");
            return true;
        }
        if (string.IsNullOrWhiteSpace(_prepared.ReleaseInfoPath)) return true;
        __result = new[] { _prepared.ReleaseInfoPath };
        return false;
    }

    private static void InitializePrefix(ref IModManagerFileIo __0, ref ModSettings __1, out LoadScope __state)
    {
        __state = null;
        lock (Gate)
        {
            if (_current != null) return;
            // Capture only the initial native lifecycle. Later calls still run
            // the original body and its version-specific state guards.
            if (typeof(ModManager).GetField("_initialized", Static)?.GetValue(null) is true
                || (typeof(ModManager).GetProperty("State", Static)?.GetValue(null) is Enum state && Convert.ToInt32(state) != 0))
                return;
            var prepared = _prepared ?? throw new InvalidOperationException("Mod initialization has no prepared source configuration.");
            if (prepared.Plan.Mode == LauncherModPlayMode.Modded)
            {
                __1 ??= new ModSettings();
                __1.PlayerAgreedToModLoading = true;
            }
            __state = new LoadScope(prepared, __1);
            _current = __state;
            __0 = new LauncherModManagerFileIo(__0, __state.Trigger);
        }
    }

    private static void InitializePostfix(LoadScope __state) => Complete(__state, null);
    private static void InitializeTaskPostfix(ref Task __result, LoadScope __state)
    {
        if (__state != null)
            __result = CompleteAsync(__result ?? throw new InvalidOperationException("ModManager returned a null Task."), __state);
    }

    private static async Task CompleteAsync(Task task, LoadScope scope)
    {
        Exception error = null;
        try { await task; }
        catch (Exception ex) { error = ex; throw; }
        finally { Complete(scope, error); }
    }

    private static Exception InitializeFinalizer(Exception __exception, LoadScope __state)
    {
        if (__exception != null) Complete(__state, __exception);
        return __exception;
    }

    private static void Complete(LoadScope scope, Exception error)
    {
        if (scope == null || Interlocked.Exchange(ref scope.Completed, 1) != 0) return;
        try
        {
            var results = scope.Bindings.Select(pair => new LauncherModLaunchResultDocumentItem
            {
                SelectionKey = pair.Value.Source.SelectionKey,
                PackageGeneration = pair.Value.Source.InstalledGeneration,
                RelativeManifestPath = Path.GetRelativePath(pair.Value.Source.RootPath, pair.Value.Filename).Replace('\\', '/'),
                Id = pair.Key.manifest?.id ?? "",
                Name = pair.Key.manifest?.name ?? pair.Key.manifest?.id ?? "",
                NativeState = pair.Key.state.ToString(),
                NativeErrors = pair.Key.errors?.Select(LauncherModNativeError.From).ToArray() ?? Array.Empty<LauncherModNativeError>(),
            }).ToArray();
            LauncherModLaunchResultStore.WriteCompletion(scope.Prepared.Plan, scope.Prepared.AttemptId,
                scope.Prepared.GameIdentityId, scope.Prepared.RuntimePackId, scope.Prepared.SelectionFingerprint,
                results, error == null ? null : error.ToString(), nativeIsRunningModded: ModManager.IsRunningModded());
        }
        finally
        {
            lock (Gate) { if (ReferenceEquals(_current, scope)) _current = null; }
        }
    }

    private static bool DiscoveryPrefix(string __0, ModSource __1, List<Mod> __2)
    {
        var scope = _current;
        if (scope == null) return true;
        if (scope.DispatchDepth != 0) return true;
        if (!PathEquals(__0, scope.Trigger) || scope.Dispatched) return false;
        scope.Dispatched = true;
        foreach (var source in scope.Prepared.Plan.Sources)
        {
            scope.Source = source;
            scope.DispatchDepth++;
            try { Invoke(_scan, source.RootPath, source.WorkshopId.HasValue ? ModSource.SteamWorkshop : ModSource.ModsDirectory, __2); }
            finally { scope.DispatchDepth--; scope.Source = null; }
        }
        return false;
    }

    private static bool ManifestPrefix(string __0)
    {
        var scope = _current;
        var source = scope?.Source;
        return source == null || ((source.ManifestPath == null || PathEquals(source.ManifestPath, __0))
            && scope.Manifests.Add(source.SelectionKey + "\n" + Path.GetFullPath(__0)));
    }

    private static void ManifestPostfix(string __0, Mod __result)
    {
        var scope = _current;
        if (scope?.Source == null || __result == null) return;
        scope.Bindings.Add(__result, new ManifestBinding(scope.Source, __0));
        var workshop = typeof(Mod).GetField("workshopId");
        if (scope.Source.WorkshopId.HasValue) workshop?.SetValue(__result, scope.Source.WorkshopId);
        // Do not add rows: native completion uses the initially empty list to
        // decide whether the first modded launch should copy vanilla saves.
        if (__result.manifest?.id is string id && scope.Settings?.ModList != null)
            foreach (var row in scope.Settings.ModList.Where(row => row.Id == id)) row.IsEnabled = true;
    }

    private static bool SteamDiscoveryPrefix() => _current == null;

    private static bool BranchSupportPrefix(ref Task __result)
    {
        if (_current == null) return true;
        // The native transport requires a running desktop Steam client. Unknown
        // advisory data remains unknown and nonfatal, as in upstream query failure.
        var scope = _current;
        var extensions = typeof(ModManager).Assembly.GetType("MegaCrit.Sts2.Core.Platform.PlatformBranchExtensions");
        var branch = extensions?.GetMethod("FromName")?.Invoke(null, new object[] { scope.Prepared.GameBranch });
        var evaluate = typeof(ModManager).GetMethod("EvaluateBranchSupport", Static);
        foreach (var source in scope.Prepared.Plan.Sources.Where(source => source.WorkshopId.HasValue))
        {
            scope.Prepared.BranchSupport.TryGetValue(source.WorkshopId.Value, out var installation);
            var support = installation?.Generation == source.InstalledGeneration ? installation.BranchSupport : null;
            if (support == null || string.IsNullOrWhiteSpace(support.Provenance) || branch == null || evaluate == null)
            {
                PatchHelper.Log($"[Mods] Steam branch advisory unavailable for {source.SelectionKey}; native loading continues.");
                continue;
            }
            var ranges = support.Ranges.Select(range => (range.MinimumBranch, range.MaximumBranch)).ToList();
            var result = evaluate.Invoke(null, new object[] { branch, ranges });
            if ((bool)result.GetType().GetField("Item1").GetValue(result)) continue;
            var maximum = result.GetType().GetField("Item2").GetValue(result);
            var maximumName = maximum == null ? "<null>" : (string)extensions.GetMethod("ToName").Invoke(null, new[] { maximum });
            foreach (var binding in scope.Bindings.Where(pair => pair.Value.Source.SelectionKey == source.SelectionKey))
            {
                var error = new LocString("main_menu_ui", "MOD_ERROR.STEAM_BRANCH_UNSUPPORTED");
                error.Add("id", binding.Key.manifest?.id ?? "<null>");
                error.Add("currentBranch", scope.Prepared.GameBranch);
                error.Add("supportedBranch", maximumName);
                (binding.Key.errors ??= new List<LocString>()).Add(error);
            }
        }
        __result = Task.CompletedTask;
        return false;
    }

    private static void ModLoadPrefix(Mod __0, out ScriptScope __state)
    {
        __state = null;
        var current = _current;
        if (current?.Bindings.ContainsKey(__0) != true) return;
        __state = new ScriptScope(current.Prepared.Plan.Roots, Scripts.Value);
        Scripts.Value = __state;
    }
    private static Exception ModLoadFinalizer(Exception __exception, ScriptScope __state)
    {
        if (__state != null) { __state.Active = false; Scripts.Value = __state.Parent; }
        return __exception;
    }
    private static void AssemblyLoadedPostfix(Assembly __result)
    {
        if (IsSelectedScriptAssembly(__result)) Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(__result);
    }
    private static bool IsSelectedScriptAssembly(Assembly assembly)
        => Scripts.Value is { Active: true } scope && !assembly.IsDynamic
            && scope.Roots.Any(root => IsInside(assembly.Location, root))
            && assembly.GetCustomAttributesData().Any(attribute => attribute.AttributeType.FullName == "Godot.AssemblyHasScriptsAttribute");

    private static bool ScriptRegistrationPrefix(Assembly __0, out bool __state)
    {
        __state = false;
        var selected = IsSelectedScriptAssembly(__0);
        Monitor.Enter(ScriptGate);
        if (RegisteredScripts.Contains(__0)) { Monitor.Exit(ScriptGate); return false; }
        if (!selected) { Monitor.Exit(ScriptGate); return true; }
        __state = true;
        return true;
    }
    private static void ScriptRegistrationPostfix(Assembly __0, bool __state)
    {
        if (__state) RegisteredScripts.Add(__0);
    }
    private static Exception ScriptRegistrationFinalizer(Exception __exception, bool __state)
    {
        if (__state) Monitor.Exit(ScriptGate);
        return __exception;
    }

    private static void Patch(Harmony harmony, MethodInfo target, string prefix = null, string postfix = null, string finalizer = null)
    {
        if (target == null) throw new MissingMethodException("Required mod framework method is absent.");
        HarmonyMethod Method(string name) => name == null ? null : new HarmonyMethod(typeof(ModLoaderPatches).GetMethod(name, Static));
        harmony.Patch(target, Method(prefix), Method(postfix), finalizer: Method(finalizer));
    }
    private static MethodInfo RequireMethod(Type type, string name, params Type[] arguments)
        => type.GetMethod(name, Static, null, arguments, null) ?? throw new MissingMethodException(type.FullName, name);
    private static void Invoke(MethodInfo method, params object[] arguments)
    {
        try { method.Invoke(null, arguments); }
        catch (TargetInvocationException ex) when (ex.InnerException != null) { ExceptionDispatchInfo.Capture(ex.InnerException).Throw(); }
    }
    private static bool PathEquals(string first, string second)
        => string.Equals(Path.GetFullPath(first), Path.GetFullPath(second), OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    private static bool IsInside(string path, string root)
        => Path.GetFullPath(path).StartsWith(Path.TrimEndingDirectorySeparator(Path.GetFullPath(root)) + Path.DirectorySeparatorChar,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private sealed record PreparedLaunch(LauncherModLaunchPlan Plan, string AttemptId, string GameIdentityId, string RuntimePackId, string ReleaseInfoPath, string SelectionFingerprint, string GameBranch, Dictionary<ulong, SteamWorkshopInstallation> BranchSupport);
    private sealed record ManifestBinding(LauncherModSource Source, string Filename);
    private sealed class LoadScope
    {
        internal readonly PreparedLaunch Prepared;
        internal readonly ModSettings Settings;
        internal readonly string Trigger = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath()) ?? "", "mods"));
        internal readonly Dictionary<Mod, ManifestBinding> Bindings = new(ReferenceEqualityComparer.Instance);
        internal readonly HashSet<string> Manifests = new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        internal LauncherModSource Source;
        internal int DispatchDepth;
        internal bool Dispatched;
        internal int Completed;
        internal LoadScope(PreparedLaunch prepared, ModSettings settings) { Prepared = prepared; Settings = settings; }
    }
    private sealed class ScriptScope
    {
        internal readonly IReadOnlyList<string> Roots;
        internal readonly ScriptScope Parent;
        internal volatile bool Active = true;
        internal ScriptScope(IReadOnlyList<string> roots, ScriptScope parent) { Roots = roots; Parent = parent; }
    }
}

// All seven methods must be virtual: a build against the five-method legacy
// interface must also satisfy the current game's two added interface slots.
internal class LauncherModManagerFileIo : IModManagerFileIo
{
    private readonly IModManagerFileIo _inner;
    private readonly string _trigger;
    internal LauncherModManagerFileIo(IModManagerFileIo inner, string trigger) { _inner = inner; _trigger = Path.GetFullPath(trigger); }
    public virtual string[] GetFilesAt(string path) => _inner.GetFilesAt(path);
    public virtual string[] GetDirectoriesAt(string path) => _inner.GetDirectoriesAt(path);
    public virtual bool FileExists(string path) => _inner.FileExists(path);
    public virtual bool DirectoryExists(string path)
        => string.Equals(Path.GetFullPath(path), _trigger, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal) || _inner.DirectoryExists(path);
    public virtual Stream OpenStream(string path, Godot.FileAccess.ModeFlags mode) => _inner.OpenStream(path, mode);
    public virtual void MakeDirRecursive(string path) => Forward(nameof(MakeDirRecursive), path);
    public virtual Error CopyFile(string sourcePath, string destinationPath) => (Error)Forward(nameof(CopyFile), sourcePath, destinationPath);
    private object Forward(string name, params object[] arguments)
    {
        var method = typeof(IModManagerFileIo).GetMethod(name) ?? throw new MissingMethodException(typeof(IModManagerFileIo).FullName, name);
        try { return method.Invoke(_inner, arguments); }
        catch (TargetInvocationException ex) when (ex.InnerException != null) { ExceptionDispatchInfo.Capture(ex.InnerException).Throw(); throw; }
    }
}
