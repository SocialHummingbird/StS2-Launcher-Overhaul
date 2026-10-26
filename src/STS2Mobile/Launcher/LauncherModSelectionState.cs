using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using STS2Mobile.Steam;
using STS2Mobile.Steam.Workshop;

namespace STS2Mobile.Launcher;

internal enum LauncherModPlayMode
{
    Vanilla,
    Modded,
}

internal sealed class LauncherModSelectionDocument
{
    public int Version { get; set; } = LauncherModSelectionState.CurrentVersion;
    public string PlayMode { get; set; } = LauncherModSelectionState.VanillaModeName;
    public Dictionary<string, bool> EnabledMods { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string UpdatedAtUtc { get; set; } = "";
    // Only normal loads carry write provenance; background observations migrate in memory.
    internal string StoragePath { get; set; }
}

internal sealed class LauncherKnownMod
{
    internal string Key { get; init; } = "";
    internal string Id { get; init; } = "";
    internal string Title { get; init; } = "";
    internal string Source { get; init; } = "";
    internal string Path { get; init; } = "";
    internal string ManifestPath { get; init; } = "";
    internal string ManifestIdentity { get; init; } = "";
    internal string DiscoveryError { get; init; } = "";
    internal LauncherModDiscoveryError SelectionError { get; init; }
    internal bool HasPck { get; init; }
    internal bool IsDependency { get; init; }
    internal bool IsRequiredDependency { get; init; }
    internal bool IsUnsupported { get; init; }
    internal ulong? WorkshopId { get; init; }
    internal SteamWorkshopInstallation Installed { get; init; }
    internal ImmutableArray<ulong> RequiredWorkshopIds { get; init; } = ImmutableArray<ulong>.Empty;
    internal bool Enabled { get; init; }
}

internal static class LauncherModSelectionState
{
    internal const int CurrentVersion = 2;
    internal const string VanillaModeName = "vanilla";
    internal const string ModdedModeName = "modded";
    private const string LegacyManualPrefix = "legacy-manual:";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    internal static LauncherModPlayMode PlayMode
    {
        get
        {
            return PlayModeFor(Load());
        }
    }

    internal static bool IsModdedMode => PlayMode == LauncherModPlayMode.Modded;

    internal static bool IsModdedModeFor(LauncherModSelectionDocument document)
        => PlayModeFor(document) == LauncherModPlayMode.Modded;

    internal static int EnabledModCount()
        => EnabledModCount(Load());

    internal static int EnabledModCount(LauncherModSelectionDocument document)
        => IsModdedModeFor(document)
            ? KnownMods(document).Count(mod => mod.Enabled && !mod.IsUnsupported)
            : 0;

    internal static int EnabledModCount(IReadOnlyList<LauncherKnownMod> knownMods)
        => knownMods?.Count(mod => mod.Enabled && !mod.IsUnsupported) ?? 0;

    internal static string SelectionFingerprint(LauncherModSelectionDocument document)
    {
        document ??= DefaultDocument();
        var canonical = new StringBuilder();
        canonical.AppendLine(
            IsModdedModeFor(document) ? ModdedModeName : VanillaModeName
        );
        var selectedKeys = IsModdedModeFor(document)
            ? (document.EnabledMods ?? new Dictionary<string, bool>())
                .Where(pair => pair.Value)
                .Select(pair => pair.Key?.Trim().ToLowerInvariant() ?? string.Empty)
                .Where(key => key.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(key => key, StringComparer.Ordinal)
            : Enumerable.Empty<string>();
        foreach (var key in selectedKeys)
        {
            canonical.AppendLine(key);
        }

        return Convert.ToHexString(
            AndroidJavaCrypto.Sha256HashData(Encoding.UTF8.GetBytes(canonical.ToString()))
        ).ToLowerInvariant();
    }

    internal static int InstalledModCount()
        => KnownMods().Count(mod => !mod.IsUnsupported);

    internal static IReadOnlyList<LauncherKnownMod> KnownMods()
        => KnownMods(Load());

    internal static IReadOnlyList<LauncherKnownMod> KnownMods(LauncherModSelectionDocument document)
        => KnownMods(document, AppPaths.ExternalModsDir);

    internal static IReadOnlyList<LauncherKnownMod> KnownMods(LauncherModSelectionDocument document, string manualModsRoot)
    {
        SteamWorkshopSyncManifest manifest;
        try
        {
            manifest = new SteamWorkshopStager().LoadManifest();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] Failed to read Workshop sources for selector: {ex.Message}");
            manifest = null;
        }
        return DiscoverKnownMods(document, manifest, manualModsRoot);
    }

    internal static IReadOnlyList<LauncherKnownMod> DiscoverKnownMods(
        LauncherModSelectionDocument document,
        SteamWorkshopSyncManifest workshopManifest,
        string manualModsRoot
    )
    {
        document ??= DefaultDocument();
        document.EnabledMods = new Dictionary<string, bool>(
            document.EnabledMods ?? new Dictionary<string, bool>(),
            StringComparer.OrdinalIgnoreCase
        );
        var manualSources = ReadManualMods(manualModsRoot);
        MigrateManualSelections(document, manualSources, persist: true);
        var mods = new List<LauncherKnownMod>();
        mods.AddRange(WorkshopMods(document, workshopManifest));
        mods.AddRange(ManualMods(document, manualSources));
        return NormalizeKnownMods(mods);
    }

    private static LauncherKnownMod[] NormalizeKnownMods(
        IEnumerable<LauncherKnownMod> mods
    )
        => (mods ?? Array.Empty<LauncherKnownMod>())
            .GroupBy(mod => mod.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderByDescending(mod => mod.Enabled)
            .ThenByDescending(mod => mod.IsRequiredDependency)
            .ThenByDescending(mod => mod.IsDependency)
            .ThenBy(mod => mod.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static LauncherModPlayMode PlayModeFor(LauncherModSelectionDocument document)
        => string.Equals(document?.PlayMode, ModdedModeName, StringComparison.OrdinalIgnoreCase)
            ? LauncherModPlayMode.Modded
            : LauncherModPlayMode.Vanilla;

    internal static void SetPlayMode(LauncherModPlayMode mode)
    {
        var document = Load();
        document.PlayMode = mode == LauncherModPlayMode.Vanilla
            ? VanillaModeName
            : ModdedModeName;
        Save(document);
    }

    internal static void SetModEnabled(string key, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        var document = Load();
        document.EnabledMods[key] = enabled;
        Save(document);
    }

    internal static LauncherModSelectionDocument Load()
        => Load(AppPaths.AppPrivateModSelectionPath);

    internal static LauncherModSelectionDocument Load(string selectionPath, bool persistMigration = true)
    {
        try
        {
            if (!File.Exists(selectionPath))
                return DefaultDocument();

            var document = JsonSerializer.Deserialize<LauncherModSelectionDocument>(
                File.ReadAllText(selectionPath)
            );
            if (document == null || document.Version is not (1 or CurrentVersion))
                return DefaultDocument();

            if (!string.Equals(document.PlayMode, VanillaModeName, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(document.PlayMode, ModdedModeName, StringComparison.OrdinalIgnoreCase))
            {
                return DefaultDocument();
            }

            document.PlayMode = string.Equals(
                document.PlayMode,
                ModdedModeName,
                StringComparison.OrdinalIgnoreCase
            )
                ? ModdedModeName
                : VanillaModeName;
            document.EnabledMods = new Dictionary<string, bool>(
                document.EnabledMods ?? new Dictionary<string, bool>(),
                StringComparer.OrdinalIgnoreCase
            );
            document.StoragePath = persistMigration ? Path.GetFullPath(selectionPath) : null;
            if (NeedsManualMigration(document))
                MigrateManualSelections(document, ReadManualMods(AppPaths.ExternalModsDir), persist: persistMigration);
            return document;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] Failed to read mod selection state: {ex.Message}");
            return DefaultDocument();
        }
    }

    private static LauncherModSelectionDocument DefaultDocument()
        => new()
        {
            Version = CurrentVersion,
            PlayMode = VanillaModeName,
            UpdatedAtUtc = DateTime.UtcNow.ToString("O"),
        };

    private static void Save(LauncherModSelectionDocument document)
    {
        try
        {
            Save(AppPaths.AppPrivateModSelectionPath, document);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] Failed to save mod selection state: {ex.Message}");
        }
    }

    internal static void Save(string selectionPath, LauncherModSelectionDocument document)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectionPath);
        ArgumentNullException.ThrowIfNull(document);

        if (NeedsManualMigration(document))
            MigrateManualSelections(document, ReadManualMods(AppPaths.ExternalModsDir), persist: false);
        document.Version = CurrentVersion;
        document.PlayMode = IsModdedModeFor(document)
            ? ModdedModeName
            : VanillaModeName;
        document.UpdatedAtUtc = DateTime.UtcNow.ToString("O");
        document.EnabledMods = new Dictionary<string, bool>(
            document.EnabledMods ?? new Dictionary<string, bool>(),
            StringComparer.OrdinalIgnoreCase
        );
        var parent = Path.GetDirectoryName(selectionPath);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        var tempPath = selectionPath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(document, JsonOptions));
        File.Move(tempPath, selectionPath, overwrite: true);
        document.StoragePath = Path.GetFullPath(selectionPath);
    }

    private static IEnumerable<LauncherKnownMod> WorkshopMods(
        LauncherModSelectionDocument document,
        SteamWorkshopSyncManifest manifest
    )
    {
        if (manifest?.Items == null)
            yield break;

        var items = manifest.Items.Where(item => item != null && item.PublishedFileId != 0)
            .GroupBy(item => item.PublishedFileId).Select(group => group.First()).ToArray();
        var requiredByParent = items.ToDictionary(item => item.PublishedFileId, item =>
        {
            var installed = item.Installed;
            if (installed != null && (!installed.LegacyUnverified || installed.RequiredPublishedFileIds?.Count > 0))
                return (installed.RequiredPublishedFileIds ?? new List<ulong>())
                    .Where(id => id != 0).Distinct().OrderBy(id => id).ToImmutableArray();

            // Only legacy inventory lacks a committed forward dependency list.
            return manifest.Version == 1 || installed?.LegacyUnverified == true
                ? items.Where(dependency => dependency.RequiredByPublishedFileIds?.Contains(item.PublishedFileId) == true)
                    .Select(dependency => dependency.PublishedFileId).OrderBy(id => id).ToImmutableArray()
                : ImmutableArray<ulong>.Empty;
        });
        var enabled = items.Where(item => IsEnabled(document, WorkshopKey(item.PublishedFileId)))
            .Select(item => item.PublishedFileId).ToHashSet();
        var pending = new Queue<ulong>(enabled);
        while (pending.TryDequeue(out var id))
        {
            if (!requiredByParent.TryGetValue(id, out var required))
                continue;
            foreach (var dependency in required)
            {
                if (enabled.Add(dependency))
                    pending.Enqueue(dependency);
            }
        }

        foreach (var item in items)
        {
            var key = WorkshopKey(item.PublishedFileId);
            var installed = item.Installed;
            if (installed == null && manifest.Version == 1 && !string.IsNullOrWhiteSpace(item.StagedDirectory))
            {
                installed = new SteamWorkshopInstallation
                {
                    Generation = "legacy",
                    Directory = item.StagedDirectory,
                    LegacyUnverified = true,
                };
            }
            var path = installed?.Directory ?? string.Empty;
            var available = !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);
            yield return new LauncherKnownMod
            {
                Key = key,
                Id = item.PublishedFileId.ToString(),
                Title = string.IsNullOrWhiteSpace(item.Title) ? item.PublishedFileId.ToString() : item.Title.Trim(),
                Source = item.IsDependency ? "Workshop dependency" : "Workshop",
                Path = path,
                WorkshopId = item.PublishedFileId,
                Installed = installed,
                RequiredWorkshopIds = requiredByParent[item.PublishedFileId],
                HasPck = installed?.Files?.Any(file =>
                    string.Equals(Path.GetExtension(file.Path), ".pck", StringComparison.OrdinalIgnoreCase)) == true
                    || (installed?.LegacyUnverified == true && item.HasPck),
                IsDependency = item.IsDependency,
                IsRequiredDependency = enabled.Contains(item.PublishedFileId) && !IsEnabled(document, key),
                IsUnsupported = !available,
                DiscoveryError = available ? string.Empty : "Workshop source has no available committed installation.",
                Enabled = enabled.Contains(item.PublishedFileId),
            };
        }
    }

    private sealed record ManualModCandidate(string RootPath, string ManifestPath, string Stem, string Id, string Name)
    {
        internal string Key => ManualKey(RootPath, Stem);
        internal string LegacyKey => string.IsNullOrWhiteSpace(Id) ? null : ManualKey(RootPath, Id);
    }

    private static IReadOnlyList<ManualModCandidate> ReadManualMods(string manualModsRoot)
    {
        if (string.IsNullOrWhiteSpace(manualModsRoot) || !Directory.Exists(manualModsRoot))
            return Array.Empty<ManualModCandidate>();
        try
        {
            return Directory.EnumerateFiles(manualModsRoot, "*", SearchOption.AllDirectories)
                .Where(path => string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(path =>
                {
                    var (id, name) = ReadDisplayHints(path);
                    return new ManualModCandidate(Path.GetDirectoryName(path) ?? string.Empty,
                        Path.GetFullPath(path), Path.GetFileNameWithoutExtension(path), id, name);
                }).ToArray();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] Failed to read manual sources for selector: {ex.Message}");
            return Array.Empty<ManualModCandidate>();
        }
    }

    private static bool NeedsManualMigration(LauncherModSelectionDocument document)
        => document.Version == 1 || document.EnabledMods?.Keys.Any(IsLegacyManualKey) == true;

    private static bool IsLegacyManualKey(string key)
        => key.StartsWith(LegacyManualPrefix, StringComparison.OrdinalIgnoreCase);

    private static ManualModCandidate[] LegacyMatches(string legacyKey, IReadOnlyList<ManualModCandidate> sources)
        => sources.Where(source => string.Equals(source.LegacyKey, legacyKey["legacy-".Length..],
            StringComparison.OrdinalIgnoreCase)).ToArray();

    private static void MigrateManualSelections(LauncherModSelectionDocument document,
        IReadOnlyList<ManualModCandidate> sources, bool persist)
    {
        document.EnabledMods ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var changed = document.Version == 1;
        if (changed)
        {
            // Version 1 keys name declared IDs; version 2 keys name physical JSON
            // files. Tag unresolved IDs so they can never turn into filename choices.
            document.EnabledMods = document.EnabledMods.ToDictionary(
                entry => entry.Key.StartsWith("manual:", StringComparison.OrdinalIgnoreCase)
                    ? "legacy-" + entry.Key : entry.Key,
                entry => entry.Value, StringComparer.OrdinalIgnoreCase);
            document.Version = CurrentVersion;
        }
        foreach (var legacy in document.EnabledMods.Where(entry => IsLegacyManualKey(entry.Key)).ToArray())
        {
            var matches = LegacyMatches(legacy.Key, sources);
            if (matches.Length != 1)
                continue;
            var target = matches[0].Key;
            if (document.EnabledMods.TryGetValue(target, out var physicalChoice) && physicalChoice != legacy.Value)
                continue;
            document.EnabledMods[target] = legacy.Value;
            document.EnabledMods.Remove(legacy.Key);
            changed = true;
        }
        if (changed && persist && !string.IsNullOrWhiteSpace(document.StoragePath))
        {
            try { Save(document.StoragePath, document); }
            catch (Exception ex) { PatchHelper.Log($"[Mods] Could not persist manual selection migration: {ex.Message}"); }
        }
    }

    private static IEnumerable<LauncherKnownMod> ManualMods(LauncherModSelectionDocument document,
        IReadOnlyList<ManualModCandidate> sources)
    {
        foreach (var source in sources)
        {
            yield return new LauncherKnownMod
            {
                Key = source.Key,
                Id = string.IsNullOrWhiteSpace(source.Id) ? source.Stem : source.Id,
                Title = string.IsNullOrWhiteSpace(source.Name) ? source.Stem : source.Name,
                Source = "Manual",
                Path = source.RootPath,
                ManifestPath = source.ManifestPath,
                ManifestIdentity = source.Id,
                HasPck = File.Exists(Path.ChangeExtension(source.ManifestPath, ".pck")),
                Enabled = IsEnabled(document, source.Key),
            };
        }
        foreach (var legacy in document.EnabledMods.Where(entry => IsLegacyManualKey(entry.Key)))
        {
            var matches = LegacyMatches(legacy.Key, sources);
            var error = new LauncherModDiscoveryError(
                matches.Length == 0 ? LauncherModDiscoveryErrorCode.SourceUnavailable : LauncherModDiscoveryErrorCode.SelectionAmbiguous,
                legacy.Key,
                matches.Length == 0
                    ? $"Saved manual identity '{legacy.Key["legacy-".Length..]}' has no available matching manifest. Its selection is retained."
                    : matches.Length > 1
                        ? $"Saved manual identity '{legacy.Key["legacy-".Length..]}' matches multiple manifests: {string.Join(", ", matches.Select(source => Path.GetFileName(source.ManifestPath)))}. Its selection is retained; no source was chosen."
                        : $"Saved manual identity '{legacy.Key["legacy-".Length..]}' conflicts with the physical choice for '{matches[0].ManifestPath}'. Both choices are retained.");
            yield return new LauncherKnownMod
            {
                Key = legacy.Key,
                Id = legacy.Key[LegacyManualPrefix.Length..],
                Title = "Unresolved manual selection: " + legacy.Key[(legacy.Key.LastIndexOf(':') + 1)..],
                Source = "Manual",
                DiscoveryError = error.Message,
                SelectionError = error,
                Enabled = legacy.Value,
            };
        }
    }

    private static (string Id, string Name) ReadDisplayHints(string path)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                return (Hint("id"), Hint("name"));
                string Hint(string property)
                    => root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                        ? value.GetString()?.Trim() ?? string.Empty
                        : string.Empty;
            }
        }
        catch (JsonException)
        {
            // Invalid JSON still has a selectable source; native discovery reports its error.
        }
        catch (IOException)
        {
            // The UI can list an unavailable source; launch preparation checks its files.
        }
        catch (UnauthorizedAccessException)
        {
        }
        return (string.Empty, string.Empty);
    }

    private static bool IsEnabled(LauncherModSelectionDocument document, string key)
        => document.EnabledMods.TryGetValue(key, out var enabled) && enabled;

    private static string WorkshopKey(ulong publishedFileId)
        => $"workshop:{publishedFileId}";

    private static string ManualKey(string directory, string stem)
        => $"manual:{NormalizePath(directory)}:{stem}";

    private static string NormalizePath(string path)
        => string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, '/', '\\');
}
