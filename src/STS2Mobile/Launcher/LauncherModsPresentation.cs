using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace STS2Mobile.Launcher;

internal enum LauncherModLastLaunchState
{
    LoadedLastLaunch, LoadedWithErrors, Failed, Skipped, NotRun, NotTestedYet,
}

internal sealed record LauncherModPresentationItem(string Key, string Id, string Title, string Source,
    bool Installed, bool Enabled, bool CanChange, LauncherModLastLaunchState LastLaunchState,
    bool IsLastLaunchStale, string Detail);

internal sealed class LauncherModsPresentation
{
    internal LauncherModsPresentation(LauncherModPlayMode mode, string selectionFingerprint, string saveNamespaceLabel,
        IReadOnlyList<LauncherModPresentationItem> mods, int installedCount, int enabledCount, bool hasStaleLastLaunchResult)
    {
        Mode = mode; SelectionFingerprint = selectionFingerprint; SaveNamespaceLabel = saveNamespaceLabel;
        Mods = mods; InstalledCount = installedCount; EnabledCount = enabledCount; HasStaleLastLaunchResult = hasStaleLastLaunchResult;
    }
    internal LauncherModPlayMode Mode { get; }
    internal string SelectionFingerprint { get; }
    internal string SaveNamespaceLabel { get; }
    internal IReadOnlyList<LauncherModPresentationItem> Mods { get; }
    internal int InstalledCount { get; }
    internal int EnabledCount { get; }
    internal bool HasStaleLastLaunchResult { get; }
}

internal static class LauncherModsPresentationState
{
    internal const string PendingSaveNamespaceLabel = "Saves determined at launch";
    internal static LauncherModsPresentation ReadCurrent(string manualModsRoot = AppPaths.ExternalModsDir)
    {
        var selection = LauncherModSelectionState.Load(AppPaths.AppPrivateModSelectionPath, persistMigration: false);
        var known = LauncherModSelectionState.KnownMods(selection, manualModsRoot);
        var dataDir = AppPaths.AppPrivateDataDir;
        var branch = LauncherPreferences.ReadGameBranch();
        BranchInstallState state = null;
        try { state = BranchInstallStateStore.Current.Read(dataDir, branch); }
        catch (BranchInstallStateException error) { PatchHelper.Log($"[Mods] Current installation identity unavailable: {error.Message}"); }
        var attempt = LauncherLaunchMarkers.ReadLastLaunchAttempt();
        var result = LauncherModLaunchResultStore.Read(AppPaths.AppPrivateLastModLaunchPath);
        // Runtime packs commit independently; normal installation completion leaves state.RuntimePack null.
        // Read the committed runtime identity without rehashing its payload.
        var runtime = LauncherMarkerFile.ReadJsonSnapshot(Path.Combine(GameRuntimeSlot.RuntimePackDirectoryPath(dataDir, branch), "compatibility.json"));
        var runtimePackId = runtime.ReadString("packId");
        var runtimeCurrent = state?.IsReady == true && state.GameIdentity != null
            && runtime.ReadString("gameIdentityId") == state.GameIdentity.Id
            && runtime.ReadString("patchSetVersion") == PatchCompatibilityValidator.PatchSetVersion
            && LauncherMarkerFile.HasConcreteValue(runtimePackId);
        var observed = LauncherModLaunchPlan.Resolve(selection, known);
        var configurationId = runtimeCurrent && observed.Success
            ? LauncherModLaunchResultStore.ConfigurationId(observed.Plan, state.GameIdentity.Id, runtimePackId)
            : null;
        return Build(selection, known, result,
            runtimeCurrent ? state.GameIdentity?.Id : null,
            runtimeCurrent ? runtimePackId : null,
            result?.ConfigurationId == attempt.ModConfigurationId && result?.RuntimePackId == attempt.RuntimePackId ? attempt.AttemptId : null,
            configurationId);
    }

    internal static LauncherModsPresentation Build(LauncherModSelectionDocument selection,
        IReadOnlyList<LauncherKnownMod> mods, string markerPath, string gameIdentityId = null,
        string runtimePackId = null, string currentAttemptId = null, string configurationId = null)
        => Build(selection, mods, LauncherModLaunchResultStore.Read(markerPath), gameIdentityId, runtimePackId, currentAttemptId, configurationId);

    internal static LauncherModsPresentation Build(LauncherModSelectionDocument selection,
        IReadOnlyList<LauncherKnownMod> mods, LauncherModLaunchResultDocument marker, string gameIdentityId = null,
        string runtimePackId = null, string currentAttemptId = null, string configurationId = null)
    {
        selection ??= new LauncherModSelectionDocument();
        selection.EnabledMods ??= new(StringComparer.OrdinalIgnoreCase);
        mods ??= Array.Empty<LauncherKnownMod>();
        var modded = LauncherModSelectionState.IsModdedModeFor(selection);
        var fingerprint = LauncherModSelectionState.SelectionFingerprint(selection);
        var valid = LauncherModLaunchResultStore.IsValid(marker);
        var current = valid && !string.IsNullOrWhiteSpace(gameIdentityId) && !string.IsNullOrWhiteSpace(runtimePackId)
            && !string.IsNullOrWhiteSpace(currentAttemptId)
            && marker.GameIdentityId == gameIdentityId && marker.RuntimePackId == runtimePackId && marker.AttemptId == currentAttemptId
            && marker.FrameworkIdentity == LauncherModLaunchResultStore.FrameworkIdentity
            && marker.SelectionFingerprint == fingerprint
            && !string.IsNullOrWhiteSpace(configurationId) && marker.ConfigurationId == configurationId
            && marker.LaunchMode == (modded ? LauncherModSelectionState.ModdedModeName : LauncherModSelectionState.VanillaModeName)
            && marker.Sources.Length == (modded ? mods.Count(mod => mod.Enabled) : 0)
            && marker.Sources.All(source => mods.Any(mod => mod.Enabled && mod.Key == source.SelectionKey
                && (mod.Installed is { LegacyUnverified: false } installed
                    ? installed.Generation == source.InstalledGeneration
                    : true)));
        var stale = marker != null && !current;
        var items = new List<LauncherModPresentationItem>();
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mod in mods.Where(mod => mod != null))
        {
            known.Add(mod.Key);
            var explicitlySelected = selection.EnabledMods.TryGetValue(mod.Key, out var selected) && selected;
            var enabled = mod.Enabled || explicitlySelected;
            var results = current && modded && enabled
                ? marker.Mods.Where(result => result.SelectionKey == mod.Key).ToArray()
                : Array.Empty<LauncherModLaunchResultDocumentItem>();
            var preparationFailure = !string.IsNullOrWhiteSpace(mod.DiscoveryError) || (enabled && mod.IsUnsupported);
            var state = preparationFailure ? LauncherModLastLaunchState.Failed
                : !current ? LauncherModLastLaunchState.NotTestedYet : State(results);
            if (current && !string.IsNullOrWhiteSpace(marker.InitializationError)
                && state == LauncherModLastLaunchState.LoadedLastLaunch)
                state = LauncherModLastLaunchState.LoadedWithErrors;
            var detail = preparationFailure ? mod.DiscoveryError ?? "Selected package is unavailable during preparation."
                : stale ? "Last launch used a different game, runtime, package or selection."
                : !current ? "No matching framework result from a completed initialization."
                : Detail(results, marker.InitializationError);
            items.Add(new(mod.Key, mod.Id, string.IsNullOrWhiteSpace(mod.Title) ? mod.Id : mod.Title,
                mod.Source, IsInstalled(mod), enabled, !mod.IsRequiredDependency && (!mod.IsUnsupported || explicitlySelected), state, stale, detail));
        }
        foreach (var entry in selection.EnabledMods.Where(pair => !known.Contains(pair.Key)))
            items.Add(new(entry.Key, entry.Key, entry.Key, entry.Key.StartsWith("workshop:") ? "Workshop" : "Manual",
                false, entry.Value, true, LauncherModLastLaunchState.NotRun, stale, "Selected package was not found during preparation."));
        var ordered = items.OrderByDescending(item => item.Enabled).ThenByDescending(item => item.Installed)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase).ToArray();
        var enabledCount = ordered.Count(item => item.Enabled);
        var saveNamespace = current && marker.NativeIsRunningModded.HasValue
            ? marker.NativeIsRunningModded.Value ? "Modded saves" : "Vanilla saves"
            : !modded || enabledCount == 0 ? "Vanilla saves" : PendingSaveNamespaceLabel;
        return new(modded ? LauncherModPlayMode.Modded : LauncherModPlayMode.Vanilla, fingerprint,
            saveNamespace, ordered, ordered.Count(item => item.Installed), enabledCount, stale);
    }

    private static LauncherModLastLaunchState State(LauncherModLaunchResultDocumentItem[] results)
    {
        if (results.Length == 0 || results.All(result => result.NativeState is "None" or "NotRun")) return LauncherModLastLaunchState.NotRun;
        if (results.Any(result => result.NativeState != "Loaded" && !Skipped(result.NativeState) && result.NativeState is not ("None" or "NotRun"))) return LauncherModLastLaunchState.Failed;
        if (results.Any(result => result.NativeState == "Loaded"))
            return results.All(result => result.NativeState == "Loaded" && result.NativeErrors.Length == 0)
                ? LauncherModLastLaunchState.LoadedLastLaunch : LauncherModLastLaunchState.LoadedWithErrors;
        return LauncherModLastLaunchState.Skipped;
    }
    private static bool Skipped(string state) => state.StartsWith("Disabled", StringComparison.Ordinal) || state == "Skipped";
    private static string Detail(LauncherModLaunchResultDocumentItem[] results, string initializationError)
    {
        if (results.Length == 0) return string.IsNullOrEmpty(initializationError)
            ? "The framework did not discover a mod in this selected source. See the game log for discovery errors."
            : "Not run because game mod initialization failed: " + initializationError;
        var initializationFailure = string.IsNullOrEmpty(initializationError) ? "" : "Game mod initialization failed: " + initializationError + "\n";
        return initializationFailure + string.Join("\n", results.Select(result =>
            $"{(string.IsNullOrWhiteSpace(result.Name) ? result.Id : result.Name)}: {result.NativeState}"
            + string.Concat(result.NativeErrors.Select(error => $"\n{error.LocTable}/{error.LocEntryKey}: {JsonSerializer.Serialize(error.Variables)}"))));
    }
    private static bool IsInstalled(LauncherKnownMod mod)
        => !string.IsNullOrWhiteSpace(mod.Path) && Directory.Exists(mod.Path)
            && (string.IsNullOrWhiteSpace(mod.ManifestPath) || File.Exists(mod.ManifestPath));
}

