using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace STS2Mobile.Steam.Workshop;

internal sealed class SteamWorkshopStager
{
    private readonly string _downloadsDirectory;
    private readonly string _stagedDirectory;
    private readonly string _clearMarkerPath;
    private readonly SteamWorkshopSyncStateStore _stateStore;

    internal SteamWorkshopStager()
        : this(
            AppPaths.AppPrivateWorkshopDownloadsDir,
            AppPaths.AppPrivateWorkshopStagedModsDir,
            AppPaths.AppPrivateWorkshopManifestPath
        )
    {
    }

    internal SteamWorkshopStager(
        string downloadsDirectory,
        string stagedDirectory,
        string manifestPath
    )
    {
        _downloadsDirectory = Path.GetFullPath(downloadsDirectory);
        _stagedDirectory = Path.GetFullPath(stagedDirectory);
        _clearMarkerPath = Path.Combine(
            Path.GetDirectoryName(Path.GetFullPath(manifestPath)) ?? _stagedDirectory,
            Path.GetFileName(AppPaths.WorkshopClearMarkerPath(""))
        );
        _stateStore = new SteamWorkshopSyncStateStore(manifestPath);
    }

    internal SteamWorkshopSyncManifest Stage(
        IReadOnlyCollection<SteamWorkshopItem> items,
        SteamWorkshopSyncMetadata metadata = null
    )
    {
        Directory.CreateDirectory(_downloadsDirectory);
        Directory.CreateDirectory(_stagedDirectory);

        var manifest = SteamWorkshopSyncManifest.Empty(_downloadsDirectory, _stagedDirectory);
        manifest.ApplyMetadata(metadata);
        Dictionary<ulong, SteamWorkshopSyncManifestItem> previous;
        SteamWorkshopInventoryException inventoryError = null;
        try
        {
            previous = LoadManifest().Items.ToDictionary(item => item.PublishedFileId);
        }
        catch (SteamWorkshopInventoryException ex)
        {
            if (!string.Equals(metadata?.SyncStatus, "complete", StringComparison.Ordinal))
                throw;
            inventoryError = ex;
            previous = new();
        }

        foreach (var item in items ?? Array.Empty<SteamWorkshopItem>())
        {
            previous.TryGetValue(item.PublishedFileId, out var installed);
            var entry = StageItem(item, installed);
            manifest.Items.Add(entry);
        }

        // Only a complete authoritative discovery may retire an installation.
        if (!string.Equals(metadata?.SyncStatus, "complete", StringComparison.Ordinal))
        {
            var observed = manifest.Items.Select(item => item.PublishedFileId).ToHashSet();
            manifest.Items.AddRange(previous.Values.Where(item => !observed.Contains(item.PublishedFileId)));
        }
        PreserveCommittedDependencies(manifest, previous);
        if (inventoryError != null && manifest.Items.Any(item => item.Installed == null || item.Status != "staged"))
        {
            var failures = string.Join("; ", manifest.Items.Where(item => item.Installed == null || item.Status != "staged")
                .Take(3).Select(item => $"{item.PublishedFileId}: {item.Status} {item.Error}"));
            throw new SteamWorkshopInventoryException(inventoryError.InventoryPath,
                $"Repair preparation failed and the existing inventory was preserved: {failures}", inventoryError);
        }
        _stateStore.Save(manifest);

        PatchHelper.Log(
            $"[Workshop] Staged {manifest.Items.Count} Workshop item(s) into {_stagedDirectory}"
        );
        return manifest;
    }

    private static void PreserveCommittedDependencies(
        SteamWorkshopSyncManifest manifest,
        IReadOnlyDictionary<ulong, SteamWorkshopSyncManifestItem> previous
    )
    {
        var retained = manifest.Items.ToDictionary(item => item.PublishedFileId);
        var pending = new Queue<ulong>(retained.Keys);
        var visited = new HashSet<ulong>();
        while (pending.Count > 0)
        {
            var id = pending.Dequeue();
            if (!visited.Add(id) || retained[id].Installed == null)
                continue;
            foreach (var required in retained[id].Installed.RequiredPublishedFileIds ?? new List<ulong>())
            {
                if (!retained.ContainsKey(required) && previous.TryGetValue(required, out var dependency))
                {
                    retained.Add(required, dependency);
                    manifest.Items.Add(dependency);
                }
                if (retained.ContainsKey(required))
                    pending.Enqueue(required);
            }
        }
    }

    internal SteamWorkshopSyncManifest LoadManifest()
        => _stateStore.Load(_downloadsDirectory, _stagedDirectory);

    internal SteamWorkshopSyncManifest SaveFailure(SteamWorkshopSyncMetadata metadata)
    {
        Directory.CreateDirectory(_downloadsDirectory);
        Directory.CreateDirectory(_stagedDirectory);

        SteamWorkshopSyncManifest manifest;
        try
        {
            manifest = LoadManifest();
        }
        catch (SteamWorkshopInventoryException ex)
        {
            PatchHelper.Log($"[Workshop] Preserved unavailable inventory while reporting {metadata?.SyncStatus}: {metadata?.SyncError}. {ex.Message}");
            throw;
        }
        manifest.ApplyMetadata(metadata);
        _stateStore.Save(manifest);
        PatchHelper.Log(
            $"[Workshop] Saved failed Workshop sync manifest: {manifest.SyncStatus} {manifest.SyncError}"
        );
        return manifest;
    }

    internal int ClearStagedMods(string reason = "manual no-mods clear")
    {
        Directory.CreateDirectory(_downloadsDirectory);
        Directory.CreateDirectory(_stagedDirectory);
        var retired = LoadManifest().Items.Count(item => item.Installed != null);
        var clearedAtUtc = DateTime.UtcNow.ToString("O");
        var manifest = SteamWorkshopSyncManifest.Empty(_downloadsDirectory, _stagedDirectory);
        manifest.ApplyMetadata(new SteamWorkshopSyncMetadata
        {
            ClearedAtUtc = clearedAtUtc,
            ClearReason = reason,
        });
        _stateStore.Save(manifest);
        WriteClearMarker(clearedAtUtc, reason, retired);

        PatchHelper.Log($"[Workshop] Retired {retired} Workshop installation(s); physical cleanup is deferred until fresh startup");
        return retired;
    }

    // Call once at fresh process startup, before native mod discovery or loading.
    internal int CleanupBeforeFirstLoad()
    {
        Directory.CreateDirectory(_stagedDirectory);
        var manifest = LoadManifest();
        foreach (var item in manifest.Items.Where(item => item.Installed?.LegacyUnverified == true
                     && (item.Installed.Files == null || item.Installed.Files.Count == 0)))
        {
            try
            {
                var files = SteamWorkshopPackageInventory.Capture(item.Installed.Directory);
                item.Installed.Files = files;
                item.Installed.ContentSha256 = SteamWorkshopPackageInventory.ContentSha256(files);
                item.ContentSha256 = item.Installed.ContentSha256;
                item.FileCount = files.Count;
                item.HasPck = files.Any(file => file.Path.EndsWith(".pck", StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                PatchHelper.Log($"[Workshop] Could not inventory legacy installation {item.PublishedFileId}: {ex.Message}");
            }
        }
        _stateStore.Save(manifest);
        var retained = new HashSet<string>(manifest.Items
            .Where(item => item.Installed != null && !string.IsNullOrWhiteSpace(item.Installed.Directory))
            .Select(item => Path.GetFullPath(item.Installed.Directory)), StringComparer.OrdinalIgnoreCase);
        var removed = 0;
        foreach (var directory in Directory.EnumerateDirectories(_stagedDirectory))
        {
            var name = Path.GetFileName(directory);
            if (ulong.TryParse(name, out var id) && id != 0 && !retained.Contains(Path.GetFullPath(directory)))
                removed += DeleteManagedDirectory(directory) ? 1 : 0;
        }

        var generationRoot = Path.Combine(_stagedDirectory, ".generations");
        if (!Directory.Exists(generationRoot))
            return removed;
        if ((File.GetAttributes(generationRoot) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Workshop generation root must not be a linked directory");
        foreach (var itemRoot in Directory.EnumerateDirectories(generationRoot))
        {
            if (!ulong.TryParse(Path.GetFileName(itemRoot), out var id) || id == 0
                || (File.GetAttributes(itemRoot) & FileAttributes.ReparsePoint) != 0)
                continue;
            foreach (var directory in Directory.EnumerateDirectories(itemRoot))
            {
                if (!Guid.TryParseExact(Path.GetFileName(directory), "N", out _)
                    || retained.Contains(Path.GetFullPath(directory)))
                    continue;
                removed += DeleteManagedDirectory(directory) ? 1 : 0;
            }
        }
        return removed;
    }

    private bool DeleteManagedDirectory(string directory)
    {
        var path = Path.GetFullPath(directory);
        if (!IsPathInsideDirectory(path, _stagedDirectory))
            throw new IOException($"Workshop cleanup path escapes staged root: {path}");
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            return false;
        try
        {
            Directory.Delete(path, recursive: true);
            PatchHelper.Log($"[Workshop] Removed unreferenced package directory {path}");
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            PatchHelper.Log($"[Workshop] Could not remove unreferenced package directory {path}: {ex.Message}");
            return false;
        }
    }

    private SteamWorkshopSyncManifestItem StageItem(SteamWorkshopItem item, SteamWorkshopSyncManifestItem previous)
    {
        var sourceDirectory = ResolveSourceDirectory(item);
        var generation = Guid.NewGuid().ToString("N");
        var targetDirectory = Path.Combine(_stagedDirectory, ".generations", item.DirectoryName, generation);

        var entry = new SteamWorkshopSyncManifestItem
        {
            PublishedFileId = item.PublishedFileId,
            Title = item.Title,
            TimeUpdated = item.TimeUpdated,
            SourceDirectory = sourceDirectory,
            StagedDirectory = previous?.Installed?.Directory ?? previous?.StagedDirectory ?? "",
            Installed = previous?.Installed,
            ManifestId = item.ManifestId,
            DownloadUrlPresent = !string.IsNullOrWhiteSpace(item.DownloadUrl),
            DownloadUrlHost = DownloadUrlHost(item.DownloadUrl),
            DownloadSourceKind = item.DownloadSourceKind,
            ExpectedDownloadBytes = item.ExpectedDownloadBytes,
            HContentFile = item.HContentFile,
            ReusedCachedDownload = item.ReusedCachedDownload,
            IsDependency = item.IsDependency,
            RequiredByPublishedFileIds = item.RequiredByPublishedFileIds.ToList(),
        };

        try
        {
            if (!string.IsNullOrWhiteSpace(item.Status)
                && !string.Equals(item.Status, "downloaded", StringComparison.OrdinalIgnoreCase))
            {
                entry.Status = item.Status;
                entry.Error = item.Error;
                PatchHelper.Log(
                    $"[Workshop] Skipping stage for {item.PublishedFileId}: {entry.Status} {entry.Error}"
                );
                return entry;
            }

            var sameRevision = SteamWorkshopRevisionResolver.MatchesInstallation(previous?.Installed,
                new SteamWorkshopPreparedRevision { ManifestId = item.ManifestId, HContentFile = item.HContentFile })
                && !string.IsNullOrWhiteSpace(previous.Installed.Generation)
                && Directory.Exists(previous.Installed.Directory);
            if (item.ReusedCachedDownload && !sameRevision)
                throw new IOException("Committed Workshop revision is unavailable for reuse");
            if (sameRevision)
            {
                entry.FileCount = previous.Installed.Files.Count;
                entry.HasPck = previous.Installed.Files.Any(file => file.Path.EndsWith(".pck", StringComparison.OrdinalIgnoreCase));
                entry.ContentSha256 = previous.Installed.ContentSha256;
                entry.Installed = new SteamWorkshopInstallation
                {
                    Generation = previous.Installed.Generation,
                    Directory = previous.Installed.Directory,
                    ManifestId = item.ManifestId,
                    HContentFile = item.HContentFile,
                    TimeUpdated = item.TimeUpdated,
                    ContentSha256 = previous.Installed.ContentSha256,
                    Files = previous.Installed.Files,
                    RequiredPublishedFileIds = item.RequiredPublishedFileIds.ToList(),
                    SelectedGameBranch = item.SelectedGameBranch,
                    RevisionProvenance = item.RevisionProvenance,
                    BranchSupport = item.BranchSupport,
                };
                entry.Status = "staged";
                return entry;
            }

            CopyDirectory(sourceDirectory, targetDirectory);
            var inventory = SteamWorkshopPackageInventory.Capture(targetDirectory);
            if (inventory.Count == 0)
                throw new IOException("Workshop package contains no files");
            MergeRuntimeFiles(previous?.Installed, targetDirectory, inventory);
            entry.ContentSha256 = SteamWorkshopPackageInventory.ContentSha256(inventory);
            entry.FileCount = inventory.Count;
            entry.HasPck = inventory.Any(file => file.Path.EndsWith(".pck", StringComparison.OrdinalIgnoreCase));
            entry.Installed = new SteamWorkshopInstallation
            {
                Generation = generation,
                Directory = targetDirectory,
                ManifestId = item.ManifestId,
                HContentFile = item.HContentFile,
                TimeUpdated = item.TimeUpdated,
                ContentSha256 = entry.ContentSha256,
                Files = inventory,
                RequiredPublishedFileIds = item.RequiredPublishedFileIds.ToList(),
                SelectedGameBranch = item.SelectedGameBranch,
                RevisionProvenance = item.RevisionProvenance,
                BranchSupport = item.BranchSupport,
            };
            entry.StagedDirectory = targetDirectory;
            entry.Status = "staged";

            PatchHelper.Log(
                $"[Workshop] Staged {item.PublishedFileId} ({entry.FileCount} files, pck={entry.HasPck}) hash={entry.ContentSha256}"
            );
        }
        catch (Exception ex)
        {
            entry.Status = "failed";
            entry.Error = ex.Message;
            PatchHelper.Log($"[Workshop] Failed to stage {item.PublishedFileId}: {ex.Message}");
        }

        return entry;
    }

    private static void MergeRuntimeFiles(SteamWorkshopInstallation previous, string targetDirectory,
        IReadOnlyList<SteamWorkshopInstalledFile> publisherFiles)
    {
        if (previous == null || previous.LegacyUnverified || previous.Files == null || previous.Files.Count == 0
            || !Directory.Exists(previous.Directory))
            return;

        var oldPublisher = previous.Files.ToDictionary(file => file.Path, StringComparer.OrdinalIgnoreCase);
        var newPublisher = publisherFiles.ToDictionary(file => file.Path, StringComparer.OrdinalIgnoreCase);
        foreach (var live in SteamWorkshopPackageInventory.Capture(previous.Directory))
        {
            var wasPublished = oldPublisher.TryGetValue(live.Path, out var oldFile);
            if (newPublisher.TryGetValue(live.Path, out var newFile))
            {
                var publisherUnchanged = wasPublished && oldFile.Length == newFile.Length
                    && string.Equals(oldFile.Sha256, newFile.Sha256, StringComparison.OrdinalIgnoreCase);
                var locallyChanged = wasPublished && (live.Length != oldFile.Length
                    || !string.Equals(live.Sha256, oldFile.Sha256, StringComparison.OrdinalIgnoreCase));
                if (!publisherUnchanged || !locallyChanged)
                    continue;
            }
            else if (wasPublished)
            {
                continue;
            }

            // Publisher files and directories win path conflicts with local additions.
            var destination = Path.Combine(targetDirectory, (newFile?.Path ?? live.Path).Replace('/', Path.DirectorySeparatorChar));
            if (Directory.Exists(destination))
                continue;
            var parent = Path.GetDirectoryName(destination);
            var blocked = false;
            for (var ancestor = parent; ancestor != null
                 && !string.Equals(ancestor, targetDirectory, StringComparison.OrdinalIgnoreCase); ancestor = Path.GetDirectoryName(ancestor))
            {
                if (!File.Exists(ancestor))
                    continue;
                blocked = true;
                break;
            }
            if (blocked)
                continue;
            Directory.CreateDirectory(parent);
            File.Copy(Path.Combine(previous.Directory, live.Path.Replace('/', Path.DirectorySeparatorChar)), destination, overwrite: true);
        }
    }

    private string ResolveSourceDirectory(SteamWorkshopItem item)
    {
        var sourceDirectory = !string.IsNullOrWhiteSpace(item.SourceDirectory)
            ? Path.GetFullPath(item.SourceDirectory)
            : Path.GetFullPath(Path.Combine(_downloadsDirectory, item.DirectoryName));

        if (!IsPathInsideDirectory(sourceDirectory, _downloadsDirectory))
            throw new InvalidOperationException(
                $"Workshop source directory escapes downloads root: {sourceDirectory}"
            );

        return sourceDirectory;
    }

    private static bool IsPathInsideDirectory(string path, string directory)
    {
        var fullPath = Path.GetFullPath(path);
        var fullDirectory = Path.GetFullPath(directory);
        if (!fullDirectory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            fullDirectory += Path.DirectorySeparatorChar;

        return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);

        foreach (var directory in Directory.EnumerateDirectories(
                     sourceDirectory,
                     "*",
                     SearchOption.AllDirectories
                 ))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(Path.Combine(targetDirectory, relativePath));
        }

        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            var targetPath = Path.Combine(targetDirectory, relativePath);
            var parent = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(parent))
                Directory.CreateDirectory(parent);

            File.Copy(file, targetPath, overwrite: true);
        }
    }

    private void WriteClearMarker(
        string clearedAtUtc,
        string reason,
        int retiredInstallations
    )
    {
        try
        {
            var markerPath = _clearMarkerPath;
            var parent = Path.GetDirectoryName(markerPath);
            if (!string.IsNullOrWhiteSpace(parent))
                Directory.CreateDirectory(parent);

            File.WriteAllLines(markerPath, new[]
            {
                $"clearedAtUtc={clearedAtUtc}",
                $"reason={SanitizeMarkerValue(reason)}",
                $"retiredInstallationCount={retiredInstallations}",
                $"physicalCleanupDeferred=true",
                $"downloadsPreserved=true",
            });
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Workshop] Failed to write clear marker: {ex.Message}");
        }
    }

    private static string SanitizeMarkerValue(string value)
        => string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Replace('\r', ' ').Replace('\n', ' ').Trim();

    private static string DownloadUrlHost(string downloadUrl)
    {
        if (string.IsNullOrWhiteSpace(downloadUrl))
            return "";

        return Uri.TryCreate(downloadUrl, UriKind.Absolute, out var uri)
            ? uri.Host
            : "";
    }
}
