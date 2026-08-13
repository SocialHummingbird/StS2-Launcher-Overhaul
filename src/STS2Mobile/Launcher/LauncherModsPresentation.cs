using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace STS2Mobile.Launcher;

internal enum LauncherModLastLaunchState
{
    LoadedLastLaunch,
    Partial,
    Failed,
    NotTestedYet,
}

internal sealed record LauncherModPresentationItem(
    string Key,
    string Id,
    string Title,
    string Source,
    bool Installed,
    bool Enabled,
    bool CanChange,
    LauncherModLastLaunchState LastLaunchState,
    bool IsLastLaunchStale,
    string Detail
);

internal sealed class LauncherModsPresentation
{
    internal LauncherModsPresentation(
        LauncherModPlayMode mode,
        string selectionFingerprint,
        string saveNamespaceLabel,
        IReadOnlyList<LauncherModPresentationItem> mods,
        int installedCount,
        int enabledCount,
        bool hasStaleLastLaunchResult
    )
    {
        Mode = mode;
        SelectionFingerprint = selectionFingerprint ?? string.Empty;
        SaveNamespaceLabel = saveNamespaceLabel ?? string.Empty;
        Mods = mods ?? Array.Empty<LauncherModPresentationItem>();
        InstalledCount = Math.Max(0, installedCount);
        EnabledCount = Math.Max(0, enabledCount);
        HasStaleLastLaunchResult = hasStaleLastLaunchResult;
    }

    internal LauncherModPlayMode Mode { get; }
    internal string SelectionFingerprint { get; }
    internal string SaveNamespaceLabel { get; }
    internal IReadOnlyList<LauncherModPresentationItem> Mods { get; }
    internal int InstalledCount { get; }
    internal int EnabledCount { get; }
    internal bool HasStaleLastLaunchResult { get; }
}

// Produces the single truthful launcher view of current mod selection, local discovery,
// and the previous launch result. Marker data is never allowed to imply a current load
// when the selection fingerprint differs or the marker cannot be validated.
internal static class LauncherModsPresentationState
{
    private const long MaxMarkerBytes = 256 * 1024;
    private const int MaxMarkerMods = 128;

    private static readonly JsonSerializerOptions MarkerJsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
    };

    internal static LauncherModsPresentation ReadCurrent()
    {
        var selection = LauncherModSelectionState.Load();
        var knownMods = LauncherModSelectionState.KnownMods(selection);
        return Build(selection, knownMods, AppPaths.AppPrivateLastModLaunchPath);
    }

    internal static LauncherModsPresentation Build(
        LauncherModSelectionDocument selection,
        IReadOnlyList<LauncherKnownMod> discoveredMods,
        string markerPath
    )
        => Build(selection, discoveredMods, ReadMarker(markerPath));

    internal static LauncherModsPresentation Build(
        LauncherModSelectionDocument selection,
        IReadOnlyList<LauncherKnownMod> discoveredMods,
        LauncherModLaunchResultDocument marker
    )
    {
        selection ??= new LauncherModSelectionDocument();
        selection.EnabledMods ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        discoveredMods ??= Array.Empty<LauncherKnownMod>();

        var mode = LauncherModSelectionState.IsModdedModeFor(selection)
            ? LauncherModPlayMode.Modded
            : LauncherModPlayMode.Vanilla;
        var fingerprint = LauncherModSelectionState.SelectionFingerprint(selection);
        var markerIsCurrent = MarkerIsValid(marker)
            && string.Equals(
                marker.LaunchMode,
                mode == LauncherModPlayMode.Modded
                    ? LauncherModSelectionState.ModdedModeName
                    : LauncherModSelectionState.VanillaModeName,
                StringComparison.Ordinal
            )
            && string.Equals(marker.SelectionFingerprint, fingerprint, StringComparison.Ordinal);
        var markerIsStale = MarkerIsValid(marker) && !markerIsCurrent;
        var markerResults = markerIsCurrent
            ? marker.Mods
            : Array.Empty<LauncherModLaunchResultDocumentItem>();
        var resultIdentities = ResultIdentities(selection, discoveredMods);

        var items = new List<LauncherModPresentationItem>();
        var knownKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mod in discoveredMods.Where(mod => mod != null))
        {
            if (!string.IsNullOrWhiteSpace(mod.Key))
                knownKeys.Add(mod.Key);

            var explicitlySelected = !string.IsNullOrWhiteSpace(mod.Key)
                && selection.EnabledMods.TryGetValue(mod.Key, out var selected)
                && selected;
            var enabled = mod.Enabled || (mod.IsUnsupported && explicitlySelected);
            var hasCurrentFailure = mod.IsUnsupported
                || !string.IsNullOrWhiteSpace(mod.DiscoveryError);
            var result = enabled && mode == LauncherModPlayMode.Modded
                ? FindUnambiguousResult(markerResults, ResultCandidates(mod, resultIdentities))
                : null;
            items.Add(new LauncherModPresentationItem(
                mod.Key ?? string.Empty,
                mod.Id ?? string.Empty,
                DisplayTitle(mod),
                string.IsNullOrWhiteSpace(mod.Source) ? "Local" : mod.Source.Trim(),
                IsInstalled(mod),
                enabled,
                !mod.IsRequiredDependency && (!mod.IsUnsupported || explicitlySelected),
                hasCurrentFailure
                    ? LauncherModLastLaunchState.Failed
                    : LastLaunchState(result),
                markerIsStale && !hasCurrentFailure,
                ResultDetail(mod, result, markerIsStale)
            ));
        }

        // Keep persisted entries visible even when their staged/manual files disappeared.
        // Otherwise clearing or moving a selected mod would silently erase the user's choice.
        foreach (var selected in selection.EnabledMods
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && !knownKeys.Contains(pair.Key))
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            var result = selected.Value && mode == LauncherModPlayMode.Modded
                ? FindUnambiguousResult(markerResults, new[] { selected.Key })
                : null;
            items.Add(new LauncherModPresentationItem(
                selected.Key,
                selected.Key,
                selected.Key,
                SourceForMissingSelection(selected.Key),
                Installed: false,
                Enabled: selected.Value,
                CanChange: true,
                LastLaunchState(result),
                markerIsStale,
                markerIsStale
                    ? "Last launch used a different mod selection."
                    : result?.Detail?.Trim() is { Length: > 0 } detail
                        ? detail
                        : "Selected mod files were not discovered on this device."
            ));
        }

        var orderedItems = items
            .OrderByDescending(item => item.Enabled)
            .ThenByDescending(item => item.Installed)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var installedCount = orderedItems.Count(item => item.Installed);
        var enabledCount = orderedItems.Count(item => item.Enabled);
        var modded = mode == LauncherModPlayMode.Modded;
        var saveNamespace = modded ? "Modded saves" : "Vanilla saves";
        return new LauncherModsPresentation(
            mode,
            fingerprint,
            saveNamespace,
            orderedItems,
            installedCount,
            enabledCount,
            markerIsStale
        );
    }

    internal static LauncherModLaunchResultDocument ReadMarker(string markerPath)
    {
        if (string.IsNullOrWhiteSpace(markerPath))
            return null;

        try
        {
            var info = new FileInfo(markerPath);
            if (!info.Exists || info.Length <= 0 || info.Length > MaxMarkerBytes)
                return null;

            var marker = JsonSerializer.Deserialize<LauncherModLaunchResultDocument>(
                File.ReadAllText(markerPath),
                MarkerJsonOptions
            );
            return MarkerIsValid(marker) ? marker : null;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] Last launch result is unavailable: {ex.Message}");
            return null;
        }
    }

    private static bool MarkerIsValid(LauncherModLaunchResultDocument marker)
    {
        if (marker == null
            || string.IsNullOrWhiteSpace(marker.SelectionFingerprint)
            || marker.Mods == null
            || marker.Mods.Length > MaxMarkerMods
            || marker.Discovered < 0
            || marker.Loaded < 0
            || marker.Active < 0
            || marker.Partial < 0
            || marker.Failed < 0
            || !DateTimeOffset.TryParse(marker.TimestampUtc, out _)
            || (!string.Equals(marker.LaunchMode, LauncherModSelectionState.VanillaModeName, StringComparison.Ordinal)
                && !string.Equals(marker.LaunchMode, LauncherModSelectionState.ModdedModeName, StringComparison.Ordinal)))
        {
            return false;
        }

        if (!marker.Mods.All(item => item != null
            && !string.IsNullOrWhiteSpace(item.Id)
            && (string.Equals(item.Result, "Active", StringComparison.Ordinal)
                || string.Equals(item.Result, "Partial", StringComparison.Ordinal)
                || string.Equals(item.Result, "Failed", StringComparison.Ordinal))))
        {
            return false;
        }

        var uniqueIds = marker.Mods
            .Select(item => item.Id)
            .Distinct(StringComparer.Ordinal)
            .Count();
        var active = marker.Mods.Count(item =>
            string.Equals(item.Result, "Active", StringComparison.Ordinal));
        var partial = marker.Mods.Count(item =>
            string.Equals(item.Result, "Partial", StringComparison.Ordinal));
        var failed = marker.Mods.Count(item =>
            string.Equals(item.Result, "Failed", StringComparison.Ordinal));
        var vanilla = string.Equals(
            marker.LaunchMode,
            LauncherModSelectionState.VanillaModeName,
            StringComparison.Ordinal
        );

        return uniqueIds == marker.Mods.Length
            && marker.Active == active
            && marker.Partial == partial
            && marker.Failed == failed
            && marker.Loaded <= marker.Discovered
            && marker.Active + marker.Partial <= marker.Loaded
            && (!vanilla
                || (marker.Discovered == 0
                    && marker.Loaded == 0
                    && marker.Mods.Length == 0));
    }

    private static Dictionary<string, string> ResultIdentities(
        LauncherModSelectionDocument selection,
        IReadOnlyList<LauncherKnownMod> discoveredMods
    )
    {
        var identities = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var resolution = LauncherModLaunchPlan.Resolve(selection, discoveredMods);
        if (!resolution.Success || resolution.Plan == null)
            return identities;

        foreach (var mod in resolution.Plan.EnabledMods)
        {
            if (!string.IsNullOrWhiteSpace(mod.SelectionKey)
                && !string.IsNullOrWhiteSpace(mod.ManifestId))
            {
                identities[mod.SelectionKey] = mod.ManifestId;
            }
        }

        return identities;
    }

    private static IEnumerable<string> ResultCandidates(
        LauncherKnownMod mod,
        IReadOnlyDictionary<string, string> resultIdentities
    )
    {
        if (!string.IsNullOrWhiteSpace(mod.Key)
            && resultIdentities.TryGetValue(mod.Key, out var resolvedIdentity))
        {
            yield return resolvedIdentity;
        }

        if (!string.IsNullOrWhiteSpace(mod.ManifestIdentity))
            yield return mod.ManifestIdentity;
        if (!string.IsNullOrWhiteSpace(mod.Id))
            yield return mod.Id;
        if (!string.IsNullOrWhiteSpace(mod.Key))
            yield return mod.Key;
    }

    private static LauncherModLaunchResultDocumentItem FindUnambiguousResult(
        IReadOnlyList<LauncherModLaunchResultDocumentItem> results,
        IEnumerable<string> candidateIds
    )
    {
        if (results == null || candidateIds == null)
            return null;

        var candidates = candidateIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        foreach (var candidate in candidates)
        {
            var matches = results
                .Where(result => string.Equals(result.Id, candidate, StringComparison.Ordinal))
                .Take(2)
                .ToArray();
            if (matches.Length == 1)
                return matches[0];
            if (matches.Length > 1)
                return null;
        }

        return null;
    }

    private static LauncherModLastLaunchState LastLaunchState(
        LauncherModLaunchResultDocumentItem result
    )
        => result?.Result switch
        {
            "Active" => LauncherModLastLaunchState.LoadedLastLaunch,
            "Partial" => LauncherModLastLaunchState.Partial,
            "Failed" => LauncherModLastLaunchState.Failed,
            _ => LauncherModLastLaunchState.NotTestedYet,
        };

    private static string ResultDetail(
        LauncherKnownMod mod,
        LauncherModLaunchResultDocumentItem result,
        bool markerIsStale
    )
    {
        if (!string.IsNullOrWhiteSpace(mod.DiscoveryError))
            return mod.DiscoveryError.Trim();
        if (mod.IsUnsupported)
        {
            return mod.IsDeprecated
                ? "Installed, but rejected by the current save-path mod policy."
                : "Installed, but not supported by the current mod loader.";
        }
        if (markerIsStale)
            return "Last launch used a different mod selection.";
        if (!string.IsNullOrWhiteSpace(result?.Detail))
            return result.Detail.Trim();
        return "No matching result from the last launch.";
    }

    private static string DisplayTitle(LauncherKnownMod mod)
    {
        if (!string.IsNullOrWhiteSpace(mod.Title))
            return mod.Title.Trim();
        if (!string.IsNullOrWhiteSpace(mod.Id))
            return mod.Id.Trim();
        return mod.Key?.Trim() ?? "Unknown mod";
    }

    private static bool IsInstalled(LauncherKnownMod mod)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(mod.ManifestPath)
                && File.Exists(mod.ManifestPath))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(mod.Path)
                && Directory.Exists(mod.Path);
        }
        catch
        {
            return false;
        }
    }

    private static string SourceForMissingSelection(string key)
    {
        if (key.StartsWith("workshop:", StringComparison.OrdinalIgnoreCase))
            return "Workshop";
        if (key.StartsWith("manual:", StringComparison.OrdinalIgnoreCase))
            return "Manual";
        return "Local";
    }
}

internal sealed class LauncherModLaunchResultDocument
{
    [JsonPropertyName("timestampUtc")]
    public string TimestampUtc { get; set; } = string.Empty;

    [JsonPropertyName("launchMode")]
    public string LaunchMode { get; set; } = string.Empty;

    [JsonPropertyName("selectionFingerprint")]
    public string SelectionFingerprint { get; set; } = string.Empty;

    [JsonPropertyName("discovered")]
    public int Discovered { get; set; }

    [JsonPropertyName("loaded")]
    public int Loaded { get; set; }

    [JsonPropertyName("active")]
    public int Active { get; set; }

    [JsonPropertyName("partial")]
    public int Partial { get; set; }

    [JsonPropertyName("failed")]
    public int Failed { get; set; }

    [JsonPropertyName("mods")]
    public LauncherModLaunchResultDocumentItem[] Mods { get; set; } = Array.Empty<LauncherModLaunchResultDocumentItem>();
}

internal sealed class LauncherModLaunchResultDocumentItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;

    [JsonPropertyName("detail")]
    public string Detail { get; set; } = string.Empty;
}
