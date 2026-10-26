using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace STS2Mobile.Steam.Workshop;

internal sealed class SteamWorkshopSyncStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _manifestPath;

    internal SteamWorkshopSyncStateStore(string manifestPath)
    {
        _manifestPath = manifestPath;
    }

    internal SteamWorkshopSyncManifest Load(string downloadsDirectory, string stagedDirectory)
    {
        SteamWorkshopSyncManifest manifest = null;
        try
        {
            var json = File.ReadAllText(_manifestPath);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty(nameof(SteamWorkshopSyncManifest.Version), out var version)
                || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var schema)
                || schema is not (1 or SteamWorkshopSyncManifest.CurrentVersion)
                || !root.TryGetProperty(nameof(SteamWorkshopSyncManifest.Items), out var items)
                || items.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("The inventory has an unsupported or incomplete schema");
            manifest = JsonSerializer.Deserialize<SteamWorkshopSyncManifest>(
                json
            );
            if (manifest?.Items == null || manifest.Items.Any(item => item == null || item.PublishedFileId == 0)
                || manifest.Items.Select(item => item.PublishedFileId).Distinct().Count() != manifest.Items.Count)
                throw new InvalidDataException("The inventory contains invalid or duplicate item records");
        }
        catch (FileNotFoundException)
        {
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            throw new SteamWorkshopInventoryException(_manifestPath, ex.Message, ex);
        }
        if (manifest == null)
        {
            var hasGenerations = false;
            try
            {
                hasGenerations = Directory.EnumerateFileSystemEntries(Path.Combine(stagedDirectory, ".generations")).Any();
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new SteamWorkshopInventoryException(_manifestPath, "Existing package generations cannot be inspected", ex);
            }
            if (hasGenerations)
                throw new SteamWorkshopInventoryException(_manifestPath, "The inventory is missing while package generations remain");
        }
        var migrateLegacy = manifest == null || manifest.Version == 1;
        manifest ??= SteamWorkshopSyncManifest.Empty(downloadsDirectory, stagedDirectory);
        manifest.Version = SteamWorkshopSyncManifest.CurrentVersion;
        manifest.Items ??= new();
        // Older failed-discovery manifests erased inventory while leaving these
        // directories intact. Adopt only legacy numeric roots, never staging work.
        if (migrateLegacy && Directory.Exists(stagedDirectory))
        {
            foreach (var directory in Directory.EnumerateDirectories(stagedDirectory))
            {
                if (!ulong.TryParse(Path.GetFileName(directory), out var id) || id == 0
                    || manifest.Items.Any(item => item.PublishedFileId == id)) continue;
                manifest.Items.Add(new SteamWorkshopSyncManifestItem
                {
                    PublishedFileId = id,
                    Title = id.ToString(),
                    StagedDirectory = directory,
                    Status = "installed-unverified",
                });
            }
        }
        foreach (var item in manifest.Items)
        {
            var legacyDirectory = Path.Combine(Path.GetFullPath(stagedDirectory), item.PublishedFileId.ToString());
            if (migrateLegacy && item.Installed == null && item.PublishedFileId != 0
                && !string.IsNullOrEmpty(item.StagedDirectory)
                && string.Equals(Path.GetFullPath(item.StagedDirectory), legacyDirectory, StringComparison.OrdinalIgnoreCase)
                && Directory.Exists(legacyDirectory))
            {
                item.Installed = new SteamWorkshopInstallation
                {
                    Generation = "legacy",
                    Directory = legacyDirectory,
                    ManifestId = item.ManifestId,
                    HContentFile = item.HContentFile,
                    TimeUpdated = item.TimeUpdated,
                    LegacyUnverified = true,
                };
            }
        }
        return manifest;
    }

    internal void Save(SteamWorkshopSyncManifest manifest)
    {
        var parent = Path.GetDirectoryName(_manifestPath);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        manifest.GeneratedAtUtc = DateTime.UtcNow.ToString("O");

        var tempPath = _manifestPath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(manifest, JsonOptions));
        File.Move(tempPath, _manifestPath, overwrite: true);
    }
}

internal sealed class SteamWorkshopInventoryException : IOException
{
    internal SteamWorkshopInventoryException(string inventoryPath, string reason, Exception innerException = null)
        : base($"Workshop inventory '{inventoryPath}' is unavailable: {reason}. A complete successful Workshop sync is required to repair it.", innerException)
    {
        InventoryPath = inventoryPath;
    }

    internal string InventoryPath { get; }
}
