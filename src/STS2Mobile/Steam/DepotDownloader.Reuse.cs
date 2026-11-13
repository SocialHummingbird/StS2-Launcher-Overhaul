using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using STS2Mobile.Launcher;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private void CapturePreparedPckAuthority()
    {
        _previousReady = null;
        _previousIdentity = null;
        _retainedPreparedPck = false;
        if (!File.Exists(BranchInstallStateStore.PathFor(_dataDir, _branch))) return;
        try
        {
            var classification = LocalPckRepairOperation.ClassifyForLauncherRouting(_dataDir, _branch);
            if (classification != InstalledGameVersionReadiness.Ready && classification != InstalledGameVersionReadiness.AutomaticRepair) return;
            if (!LauncherGameFiles.TryReadReadyIdentity(_dataDir, _branch, out var identity, out _)) return;
            var state = BranchInstallStateStore.Current.Read(_dataDir, _branch);
            if (!SupportedPreparation(state.PckPreparationVersion) || !state.MatchesReadyIdentity(identity)) return;
            _previousReady = state;
            _previousIdentity = identity;
        }
        catch (Exception ex) when (ex is IOException or GameIdentityException or BranchInstallStateException)
        {
            Log($"Prepared PCK reuse is unavailable: {ex.GetType().Name}");
        }
    }

    internal static bool CanKeepPreparedPck(BranchInstallState ready, DepotManifest oldManifest, DepotManifest.FileData target, GameIdentity currentIdentity)
    {
        if (ready == null || currentIdentity == null || !ready.MatchesReadyIdentity(currentIdentity)
            || !SupportedPreparation(ready.PckPreparationVersion)
            || !string.Equals(NormalizeManifestPath(target.FileName), "SlayTheSpire2.pck", StringComparison.Ordinal)) return false;
        var old = oldManifest?.Files?.FirstOrDefault(file => string.Equals(NormalizeManifestPath(file.FileName), "SlayTheSpire2.pck", StringComparison.Ordinal));
        var depot = ready.Depots.FirstOrDefault(entry => entry.DepotId == oldManifest?.DepotID);
        return old != null && depot != null && depot.ManifestId == oldManifest.ManifestGID
            && old.TotalSize == target.TotalSize && HashesEqual(old.FileHash, target.FileHash);
    }

    private static bool SupportedPreparation(string version) => version == AndroidPckPreparationVersions.V1 || version == AndroidPckPreparationVersions.V2;

    private IReadOnlyList<DepotChunkDonor> ReadInstalledDonors(uint depotId, DepotManifest selectedManifest)
    {
        var result = new List<DepotChunkDonor>();
        if (selectedManifest?.Files != null) result.Add(new DepotChunkDonor(_gameDir, selectedManifest, Selected: true));
        var roots = new List<string> { _dataDir };
        var versions = Path.Combine(_dataDir, SteamGameInstallPaths.GameVersionsDirectory);
        if (Directory.Exists(versions)) roots.AddRange(Directory.EnumerateDirectories(versions));
        foreach (var root in roots)
        {
            var statePath = Path.Combine(root, SteamGameInstallPaths.InstallationStateFileName);
            if (!File.Exists(statePath)) continue;
            try
            {
                using var json = JsonDocument.Parse(File.ReadAllText(statePath));
                var branch = SteamGameBranch.StorageIdentity(json.RootElement.GetProperty("branch").GetString());
                if (branch == _branch || !string.Equals(Path.GetFullPath(statePath), Path.GetFullPath(BranchInstallStateStore.PathFor(_dataDir, branch)), OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) continue;
                var state = BranchInstallStateStore.Current.Read(_dataDir, branch);
                if (!state.IsReady || !SupportedPreparation(state.PckPreparationVersion)) continue;
                var depot = state.Depots.FirstOrDefault(entry => entry.DepotId == depotId);
                if (depot == null) continue;
                var identity = GameIdentityReader.ReadInstalledReadOnly(_dataDir, branch);
                if (!state.MatchesReadyIdentity(identity)) continue;
                // Read donor metadata without the selected state store's recovery
                // behaviour: another installed version is a read-only source.
                using var stream = File.OpenRead(Path.Combine(SteamGameInstallPaths.DownloadStateDirectoryPath(_dataDir, branch), $"{depotId}.manifest"));
                var manifest = DepotManifest.Deserialize(stream);
                if (manifest.DepotID == depotId && manifest.ManifestGID == depot.ManifestId && manifest.Files != null)
                    result.Add(new DepotChunkDonor(SteamGameInstallPaths.GameDirectory(_dataDir, branch), manifest));
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or InvalidOperationException or KeyNotFoundException or ArgumentException or GameIdentityException or BranchInstallStateException or ProtoBuf.ProtoException)
            {
                Log($"Installed donor unavailable: {ex.GetType().Name}");
            }
        }
        return result;
    }

    private void CleanupCompletedDownloadStaging()
    {
        var path = Path.Combine(SteamGameInstallPaths.DownloadStateDirectoryPath(_dataDir, _branch), DepotDownloadStaging.DirectoryName);
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
            foreach (var legacy in EnumerateDownloadingTempFiles()) DeleteQuietly(legacy);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log($"Completed installation retained staging for later cleanup: {ex.Message}");
        }
    }
}
