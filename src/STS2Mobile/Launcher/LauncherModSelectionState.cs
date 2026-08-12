using System;
using System.Collections.Generic;
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
}

internal sealed class LauncherKnownMod
{
    internal string Key { get; init; } = "";
    internal string PortableIdentity { get; init; } = "";
    internal string Id { get; init; } = "";
    internal string Title { get; init; } = "";
    internal string Source { get; init; } = "";
    internal string Path { get; init; } = "";
    internal string ManifestPath { get; init; } = "";
    internal string ManifestIdentity { get; init; } = "";
    internal string DiscoveryError { get; init; } = "";
    internal LauncherModDiscoveryErrorCode DiscoveryErrorCode { get; init; }
    internal bool HasPck { get; init; }
    internal bool IsDependency { get; init; }
    internal bool IsRequiredDependency { get; init; }
    internal bool IsUnsupported { get; init; }
    internal bool IsDeprecated { get; init; }
    internal bool Enabled { get; init; }
}

internal sealed class LauncherKnownModsSnapshot
{
    internal LauncherKnownModsSnapshot(
        LauncherModSourceIdentity identity,
        IReadOnlyList<LauncherKnownMod> mods
    )
    {
        Identity = identity;
        Mods = mods ?? Array.Empty<LauncherKnownMod>();
    }

    internal LauncherModSourceIdentity Identity { get; }
    internal IReadOnlyList<LauncherKnownMod> Mods { get; }
}

internal static class LauncherModSelectionState
{
    internal const int CurrentVersion = 1;
    internal const string VanillaModeName = "vanilla";
    internal const string ModdedModeName = "modded";
    private const int MaxManualMods = 32;
    private const int MaxManualManifestCandidates = MaxManualMods * 4;
    private static readonly object KnownModsGate = new();
    private static KnownModsCacheEntry _knownModsCache;

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

    internal static string EnabledModSetFingerprint()
        => EnabledModSetFingerprint(Load());

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

    internal static string EnabledModSetFingerprint(
        LauncherModSelectionDocument document
    )
    {
        if (!IsModdedModeFor(document))
            return null;

        return EnabledModSetFingerprint(KnownMods(document));
    }

    internal static string EnabledModSetFingerprint(
        IReadOnlyList<LauncherKnownMod> knownMods
    )
    {
        var identities = (knownMods ?? Array.Empty<LauncherKnownMod>())
            .Where(mod => mod.Enabled && !mod.IsUnsupported)
            .Select(mod => mod.PortableIdentity)
            .Where(identity => !string.IsNullOrWhiteSpace(identity))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(identity => identity, StringComparer.Ordinal);
        var canonical = string.Join("\n", identities);
        return Convert.ToHexString(
            AndroidJavaCrypto.Sha256HashData(Encoding.UTF8.GetBytes(canonical))
        ).ToLowerInvariant();
    }

    internal static int InstalledModCount()
        => KnownMods().Count(mod => !mod.IsUnsupported);

    internal static IReadOnlyList<LauncherKnownMod> KnownMods()
        => KnownModsSnapshot().Mods;

    internal static IReadOnlyList<LauncherKnownMod> KnownMods(LauncherModSelectionDocument document)
        => KnownModsSnapshot(document).Mods;

    internal static LauncherKnownModsSnapshot KnownModsSnapshot()
        => KnownModsSnapshot(Load());

    internal static LauncherKnownModsSnapshot KnownModsSnapshot(LauncherModSelectionDocument document)
        => KnownModsSnapshot(document, LauncherModSourceIdentity.Create());

    internal static LauncherKnownModsSnapshot KnownModsSnapshot(
        LauncherModSelectionDocument document,
        LauncherModSourceIdentity identity
    )
    {
        identity ??= LauncherModSourceIdentity.Create();
        lock (KnownModsGate)
        {
            if (_knownModsCache != null && _knownModsCache.Identity.Matches(identity))
                return _knownModsCache.Snapshot;
        }

        document ??= DefaultDocument();
        document.EnabledMods ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var mods = new List<LauncherKnownMod>();
        mods.AddRange(WorkshopMods(document));
        mods.AddRange(ManualMods(document));
        var knownMods = NormalizeKnownMods(mods);

        lock (KnownModsGate)
        {
            if (_knownModsCache == null || !_knownModsCache.Identity.Matches(identity))
                _knownModsCache = new KnownModsCacheEntry(identity, knownMods);

            return _knownModsCache.Snapshot;
        }
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
        var mods = new List<LauncherKnownMod>();
        mods.AddRange(WorkshopMods(document, workshopManifest));
        mods.AddRange(ManualMods(document, manualModsRoot));
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

    internal static void ClearKnownModsCache(string reason)
    {
        lock (KnownModsGate)
        {
            _knownModsCache = null;
        }

        LauncherModLaunchReadinessCache.Clear(reason);
    }

    private sealed class KnownModsCacheEntry
    {
        internal KnownModsCacheEntry(
            LauncherModSourceIdentity identity,
            IReadOnlyList<LauncherKnownMod> mods
        )
        {
            Snapshot = new LauncherKnownModsSnapshot(identity, mods);
        }

        internal LauncherModSourceIdentity Identity => Snapshot.Identity;
        internal LauncherKnownModsSnapshot Snapshot { get; }
    }

    internal static bool IsModEnabled(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        var document = Load();
        return document.EnabledMods.TryGetValue(key, out var enabled) && enabled;
    }

    internal static bool IsPathEnabled(string path)
        => IsPathEnabled(path, Load());

    internal static bool IsPathEnabled(string path, LauncherModSelectionDocument document)
        => IsModdedModeFor(document) && IsPathEnabled(path, KnownMods(document));

    internal static bool IsPathEnabled(string path, IReadOnlyList<LauncherKnownMod> knownMods)
    {
        var normalized = NormalizePath(path);
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        foreach (var mod in knownMods ?? Array.Empty<LauncherKnownMod>())
        {
            var modPath = NormalizePath(mod.Path);
            if (string.IsNullOrWhiteSpace(modPath))
                continue;

            if (normalized.Equals(modPath, StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith(modPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith(modPath + '/', StringComparison.OrdinalIgnoreCase))
            {
                return mod.Enabled && !mod.IsUnsupported;
            }
        }

        return false;
    }

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

    internal static LauncherModSelectionDocument Load(string selectionPath)
    {
        try
        {
            if (!File.Exists(selectionPath))
                return DefaultDocument();

            var document = JsonSerializer.Deserialize<LauncherModSelectionDocument>(
                File.ReadAllText(selectionPath)
            );
            if (document == null || document.Version != CurrentVersion)
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
            ClearKnownModsCache("mod selection changed");
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
    }

    private static IEnumerable<LauncherKnownMod> WorkshopMods(LauncherModSelectionDocument document)
    {
        SteamWorkshopSyncManifest manifest;
        try
        {
            manifest = new SteamWorkshopStager().LoadManifest();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] Failed to read Workshop mods for selector: {ex.Message}");
            yield break;
        }

        foreach (var mod in WorkshopMods(document, manifest))
            yield return mod;
    }

    private static IEnumerable<LauncherKnownMod> WorkshopMods(
        LauncherModSelectionDocument document,
        SteamWorkshopSyncManifest manifest
    )
    {
        if (manifest?.Items == null)
            yield break;

        var enabledWorkshopIds = ResolveEnabledWorkshopIds(manifest, document);
        foreach (var item in manifest.Items)
        {
            var key = WorkshopKey(item.PublishedFileId);
            var status = item.Status ?? "";
            var deprecated = IsDeprecatedSavePathMod(item.PublishedFileId.ToString(), item.Title);
            var unsupported = deprecated
                || string.Equals(status, "unsupported", StringComparison.OrdinalIgnoreCase);
            var requiredDependency = item.IsDependency
                && item.RequiredByPublishedFileIds.Any(parent => enabledWorkshopIds.Contains(parent));
            yield return new LauncherKnownMod
            {
                Key = key,
                PortableIdentity = key,
                Id = item.PublishedFileId.ToString(),
                Title = string.IsNullOrWhiteSpace(item.Title) ? item.PublishedFileId.ToString() : item.Title.Trim(),
                Source = item.IsDependency ? "Workshop dependency" : "Workshop",
                Path = item.StagedDirectory ?? "",
                HasPck = item.HasPck,
                IsDependency = item.IsDependency,
                IsRequiredDependency = requiredDependency,
                IsUnsupported = unsupported,
                IsDeprecated = deprecated,
                Enabled = !unsupported && enabledWorkshopIds.Contains(item.PublishedFileId),
            };
        }
    }

    private static HashSet<ulong> ResolveEnabledWorkshopIds(
        SteamWorkshopSyncManifest manifest,
        LauncherModSelectionDocument document
    )
    {
        var enabled = manifest.Items
            .Where(item => item.PublishedFileId != 0)
            .Where(item => !IsUnsupported(item))
            .Where(item =>
            {
                var key = WorkshopKey(item.PublishedFileId);
                return item.IsDependency
                    ? IsExplicitlyEnabled(document, key)
                    : IsEnabled(document, key);
            })
            .Select(item => item.PublishedFileId)
            .ToHashSet();

        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var dependency in manifest.Items)
            {
                if (dependency.PublishedFileId == 0
                    || !dependency.IsDependency
                    || IsUnsupported(dependency)
                    || enabled.Contains(dependency.PublishedFileId))
                {
                    continue;
                }

                if (!dependency.RequiredByPublishedFileIds.Any(parent => enabled.Contains(parent)))
                    continue;

                enabled.Add(dependency.PublishedFileId);
                changed = true;
            }
        }

        return enabled;
    }

    private static bool IsUnsupported(SteamWorkshopSyncManifestItem item)
        => string.Equals(item.Status, "unsupported", StringComparison.OrdinalIgnoreCase)
            || IsDeprecatedSavePathMod(item.PublishedFileId.ToString(), item.Title);

    private static IEnumerable<LauncherKnownMod> ManualMods(LauncherModSelectionDocument document)
        => ManualMods(document, AppPaths.ExternalModsDir);

    private static IEnumerable<LauncherKnownMod> ManualMods(
        LauncherModSelectionDocument document,
        string manualModsRoot
    )
    {
        if (string.IsNullOrWhiteSpace(manualModsRoot) || !Directory.Exists(manualModsRoot))
            yield break;

        IEnumerable<string> manifests;
        try
        {
            manifests = Directory.EnumerateFiles(
                    manualModsRoot,
                    "*.json",
                    SearchOption.AllDirectories
                )
                .Take(MaxManualManifestCandidates)
                .ToArray();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] Failed to read manual mods for selector: {ex.Message}");
            yield break;
        }

        var discovered = 0;
        foreach (var manifestPath in manifests)
        {
            if (discovered >= MaxManualMods)
                yield break;

            var directory = Path.GetDirectoryName(manifestPath) ?? "";
            var probe = LauncherModLaunchPlan.InspectManifest(manifestPath);
            if (probe.Kind == LauncherModManifestProbeKind.NotManifest)
                continue;

            discovered++;
            var manifestId = probe.Manifest?.Id
                ?? Path.GetFileNameWithoutExtension(manifestPath);
            var title = probe.Manifest?.Name ?? manifestId;
            var key = ManualKey(directory, manifestId);
            var deprecated = IsDeprecatedSavePathMod(manifestId, title);
            yield return new LauncherKnownMod
            {
                Key = key,
                PortableIdentity = ManualPortableIdentity(manifestId),
                Id = manifestId,
                Title = title,
                Source = "Manual",
                Path = directory,
                ManifestPath = manifestPath,
                ManifestIdentity = probe.Manifest?.Id ?? string.Empty,
                DiscoveryError = probe.Kind == LauncherModManifestProbeKind.Invalid
                    ? probe.ErrorMessage
                    : string.Empty,
                DiscoveryErrorCode = probe.ErrorCode,
                HasPck = probe.Manifest?.HasPck == true,
                IsUnsupported = deprecated,
                IsDeprecated = deprecated,
                Enabled = !deprecated && IsEnabled(document, key),
            };
        }
    }

    private static bool IsDeprecatedSavePathMod(string id, string title)
        => DeprecatedSavePathMod.IsMatch(id, title);

    private static bool IsEnabled(LauncherModSelectionDocument document, string key)
        => document.EnabledMods.TryGetValue(key, out var enabled) && enabled;

    private static bool IsExplicitlyEnabled(LauncherModSelectionDocument document, string key)
        => document.EnabledMods.TryGetValue(key, out var enabled) && enabled;

    private static string WorkshopKey(ulong publishedFileId)
        => $"workshop:{publishedFileId}";

    private static string ManualKey(string directory, string id)
        => $"manual:{NormalizePath(directory)}:{id}";

    private static string ManualPortableIdentity(string manifestId)
        => $"manual:{(manifestId ?? string.Empty).Trim().ToLowerInvariant()}";

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "";

        try
        {
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, '/', '\\');
        }
        catch
        {
            return path.TrimEnd(Path.DirectorySeparatorChar, '/', '\\');
        }
    }
}
