using System;
using System.IO;
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
        try
        {
            var manifest = JsonSerializer.Deserialize<SteamWorkshopSyncManifest>(
                File.ReadAllText(_manifestPath)
            );
            if (manifest == null || manifest.Version != SteamWorkshopSyncManifest.CurrentVersion)
                return SteamWorkshopSyncManifest.Empty(downloadsDirectory, stagedDirectory);

            return manifest;
        }
        catch (FileNotFoundException)
        {
            return SteamWorkshopSyncManifest.Empty(downloadsDirectory, stagedDirectory);
        }
        catch (DirectoryNotFoundException)
        {
            return SteamWorkshopSyncManifest.Empty(downloadsDirectory, stagedDirectory);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Workshop] Failed to read sync manifest {_manifestPath}: {ex.Message}");
            return SteamWorkshopSyncManifest.Empty(downloadsDirectory, stagedDirectory);
        }
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
