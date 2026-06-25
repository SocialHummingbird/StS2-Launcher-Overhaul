using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
    public string PlayMode { get; set; } = LauncherModSelectionState.ModdedModeName;
    public Dictionary<string, bool> EnabledMods { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string UpdatedAtUtc { get; set; } = "";
}

internal sealed class LauncherKnownMod
{
    internal string Key { get; init; } = "";
    internal string Id { get; init; } = "";
    internal string Title { get; init; } = "";
    internal string Source { get; init; } = "";
    internal string Path { get; init; } = "";
    internal bool HasPck { get; init; }
    internal bool IsDependency { get; init; }
    internal bool IsRequiredDependency { get; init; }
    internal bool IsUnsupported { get; init; }
    internal bool Enabled { get; init; }
}

internal static class LauncherModSelectionState
{
    internal const int CurrentVersion = 1;
    internal const string VanillaModeName = "vanilla";
    internal const string ModdedModeName = "modded";
    private const int MaxManualMods = 32;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    internal static LauncherModPlayMode PlayMode
    {
        get
        {
            var document = Load();
            return string.Equals(document.PlayMode, VanillaModeName, StringComparison.OrdinalIgnoreCase)
                ? LauncherModPlayMode.Vanilla
                : LauncherModPlayMode.Modded;
        }
    }

    internal static bool IsModdedMode => PlayMode == LauncherModPlayMode.Modded;

    internal static bool PushShouldBeLocked()
        => IsModdedMode && KnownMods().Any(mod => mod.Enabled && !mod.IsUnsupported);

    internal static int EnabledModCount()
        => IsModdedMode
            ? KnownMods().Count(mod => mod.Enabled && !mod.IsUnsupported)
            : 0;

    internal static int InstalledModCount()
        => KnownMods().Count(mod => !mod.IsUnsupported);

    internal static IReadOnlyList<LauncherKnownMod> KnownMods()
    {
        var document = Load();
        var mods = new List<LauncherKnownMod>();
        mods.AddRange(WorkshopMods(document));
        mods.AddRange(ManualMods(document));
        return mods
            .GroupBy(mod => mod.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderByDescending(mod => mod.Enabled)
            .ThenByDescending(mod => mod.IsRequiredDependency)
            .ThenByDescending(mod => mod.IsDependency)
            .ThenBy(mod => mod.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal static bool IsModEnabled(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        var document = Load();
        return !document.EnabledMods.TryGetValue(key, out var enabled) || enabled;
    }

    internal static bool IsPathEnabled(string path)
    {
        if (!IsModdedMode)
            return false;

        var normalized = NormalizePath(path);
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        foreach (var mod in KnownMods())
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
    {
        try
        {
            if (!File.Exists(AppPaths.AppPrivateModSelectionPath))
                return DefaultDocument();

            var document = JsonSerializer.Deserialize<LauncherModSelectionDocument>(
                File.ReadAllText(AppPaths.AppPrivateModSelectionPath)
            );
            if (document == null || document.Version != CurrentVersion)
                return DefaultDocument();

            document.EnabledMods ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
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
            PlayMode = ModdedModeName,
            UpdatedAtUtc = DateTime.UtcNow.ToString("O"),
        };

    private static void Save(LauncherModSelectionDocument document)
    {
        try
        {
            document.Version = CurrentVersion;
            document.UpdatedAtUtc = DateTime.UtcNow.ToString("O");
            var parent = Path.GetDirectoryName(AppPaths.AppPrivateModSelectionPath);
            if (!string.IsNullOrWhiteSpace(parent))
                Directory.CreateDirectory(parent);

            var tempPath = AppPaths.AppPrivateModSelectionPath + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(document, JsonOptions));
            File.Move(tempPath, AppPaths.AppPrivateModSelectionPath, overwrite: true);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] Failed to save mod selection state: {ex.Message}");
        }
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

        var enabledWorkshopIds = ResolveEnabledWorkshopIds(manifest, document);
        foreach (var item in manifest.Items)
        {
            var key = WorkshopKey(item.PublishedFileId);
            var status = item.Status ?? "";
            var unsupported = string.Equals(status, "unsupported", StringComparison.OrdinalIgnoreCase);
            var requiredDependency = item.IsDependency
                && item.RequiredByPublishedFileIds.Any(parent => enabledWorkshopIds.Contains(parent));
            yield return new LauncherKnownMod
            {
                Key = key,
                Id = item.PublishedFileId.ToString(),
                Title = string.IsNullOrWhiteSpace(item.Title) ? item.PublishedFileId.ToString() : item.Title.Trim(),
                Source = item.IsDependency ? "Workshop dependency" : "Workshop",
                Path = item.StagedDirectory ?? "",
                HasPck = item.HasPck,
                IsDependency = item.IsDependency,
                IsRequiredDependency = requiredDependency,
                IsUnsupported = unsupported,
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
        => string.Equals(item.Status, "unsupported", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<LauncherKnownMod> ManualMods(LauncherModSelectionDocument document)
    {
        if (!Directory.Exists(AppPaths.ExternalModsDir))
            yield break;

        IEnumerable<string> manifests;
        try
        {
            manifests = Directory.EnumerateFiles(
                    AppPaths.ExternalModsDir,
                    "*.json",
                    SearchOption.AllDirectories
                )
                .Take(MaxManualMods)
                .ToArray();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] Failed to read manual mods for selector: {ex.Message}");
            yield break;
        }

        foreach (var manifestPath in manifests)
        {
            var directory = Path.GetDirectoryName(manifestPath) ?? "";
            var id = Path.GetFileNameWithoutExtension(manifestPath);
            var key = ManualKey(directory, id);
            yield return new LauncherKnownMod
            {
                Key = key,
                Id = id,
                Title = id,
                Source = "Manual",
                Path = directory,
                HasPck = HasTopLevelPck(directory),
                Enabled = IsEnabled(document, key),
            };
        }
    }

    private static bool HasTopLevelPck(string directory)
    {
        try
        {
            return Directory.Exists(directory)
                && Directory.EnumerateFiles(directory, "*.pck", SearchOption.TopDirectoryOnly).Any();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Mods] Failed to inspect manual mod PCK files in {directory}: {ex.Message}");
            return false;
        }
    }

    private static bool IsEnabled(LauncherModSelectionDocument document, string key)
        => !document.EnabledMods.TryGetValue(key, out var enabled) || enabled;

    private static bool IsExplicitlyEnabled(LauncherModSelectionDocument document, string key)
        => document.EnabledMods.TryGetValue(key, out var enabled) && enabled;

    private static string WorkshopKey(ulong publishedFileId)
        => $"workshop:{publishedFileId}";

    private static string ManualKey(string directory, string id)
        => $"manual:{NormalizePath(directory)}:{id}";

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
