using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace STS2Mobile.Steam.Workshop;

internal sealed class SteamWorkshopStager
{
    private readonly string _downloadsDirectory;
    private readonly string _stagedDirectory;
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
        var activeDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items ?? Array.Empty<SteamWorkshopItem>())
        {
            var entry = StageItem(item);
            manifest.Items.Add(entry);

            if (IsActiveStagedStatus(entry.Status) && !string.IsNullOrWhiteSpace(item.DirectoryName))
                activeDirectories.Add(item.DirectoryName);
        }

        RemoveInactiveStagedDirectories(activeDirectories);
        RemoveInactiveStagedRootFiles();
        _stateStore.Save(manifest);

        PatchHelper.Log(
            $"[Workshop] Staged {manifest.Items.Count} Workshop item(s) into {_stagedDirectory}"
        );
        return manifest;
    }

    internal SteamWorkshopSyncManifest LoadManifest()
        => _stateStore.Load(_downloadsDirectory, _stagedDirectory);

    internal SteamWorkshopSyncManifest SaveFailure(SteamWorkshopSyncMetadata metadata)
    {
        Directory.CreateDirectory(_downloadsDirectory);
        Directory.CreateDirectory(_stagedDirectory);

        var manifest = SteamWorkshopSyncManifest.Empty(_downloadsDirectory, _stagedDirectory);
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

        var removedDirectories = 0;
        foreach (var directory in Directory.EnumerateDirectories(_stagedDirectory).ToArray())
        {
            var fullDirectory = Path.GetFullPath(directory);
            if (!IsPathInsideDirectory(fullDirectory, _stagedDirectory))
                throw new InvalidOperationException(
                    $"Workshop staged directory escapes staged root: {fullDirectory}"
                );

            DeleteDirectoryQuietly(fullDirectory);
            removedDirectories++;
        }

        var removedFiles = 0;
        foreach (var file in Directory.EnumerateFiles(_stagedDirectory).ToArray())
        {
            var fullFile = Path.GetFullPath(file);
            if (!IsPathInsideDirectory(fullFile, _stagedDirectory))
                throw new InvalidOperationException(
                    $"Workshop staged file escapes staged root: {fullFile}"
                );

            DeleteFileQuietly(fullFile);
            removedFiles++;
        }

        var clearedAtUtc = DateTime.UtcNow.ToString("O");
        var manifest = SteamWorkshopSyncManifest.Empty(_downloadsDirectory, _stagedDirectory);
        manifest.ApplyMetadata(new SteamWorkshopSyncMetadata
        {
            ClearedAtUtc = clearedAtUtc,
            ClearReason = reason,
        });
        _stateStore.Save(manifest);
        WriteClearMarker(clearedAtUtc, reason, removedDirectories, removedFiles);

        PatchHelper.Log(
            $"[Workshop] Cleared {removedDirectories} staged Workshop mod directories and {removedFiles} staged root files; downloads were preserved"
        );
        return removedDirectories + removedFiles;
    }

    private SteamWorkshopSyncManifestItem StageItem(SteamWorkshopItem item)
    {
        var sourceDirectory = ResolveSourceDirectory(item);
        var targetDirectory = Path.Combine(_stagedDirectory, item.DirectoryName);

        var entry = new SteamWorkshopSyncManifestItem
        {
            PublishedFileId = item.PublishedFileId,
            Title = item.Title,
            TimeUpdated = item.TimeUpdated,
            SourceDirectory = sourceDirectory,
            StagedDirectory = targetDirectory,
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

            ReplaceStagedDirectory(sourceDirectory, targetDirectory);

            var files = StagedFiles(targetDirectory).ToArray();
            entry.FileCount = files.Length;
            entry.HasPck = files.Any(file => file.EndsWith(".pck", StringComparison.OrdinalIgnoreCase));
            entry.ContentSha256 = DirectoryContentSha256(targetDirectory, files);
            entry.Status = entry.HasPck ? "staged" : "staged-no-pck";

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

    private static bool IsActiveStagedStatus(string status)
        => string.Equals(status, "staged", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "staged-no-pck", StringComparison.OrdinalIgnoreCase);

    private static bool IsPathInsideDirectory(string path, string directory)
    {
        var fullPath = Path.GetFullPath(path);
        var fullDirectory = Path.GetFullPath(directory);
        if (!fullDirectory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            fullDirectory += Path.DirectorySeparatorChar;

        return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static void ReplaceStagedDirectory(string sourceDirectory, string targetDirectory)
    {
        var parent = Path.GetDirectoryName(targetDirectory);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        var tempDirectory = targetDirectory + ".tmp-" + Guid.NewGuid().ToString("N");
        var backupDirectory = targetDirectory + ".old-" + Guid.NewGuid().ToString("N");

        CopyDirectory(sourceDirectory, tempDirectory);

        try
        {
            var backupCreated = TryMoveDirectory(targetDirectory, backupDirectory);

            Directory.Move(tempDirectory, targetDirectory);
            if (backupCreated)
                DeleteDirectoryQuietly(backupDirectory);
        }
        catch
        {
            DeleteDirectoryQuietly(tempDirectory);
            TryMoveDirectory(backupDirectory, targetDirectory);
            throw;
        }
    }

    private static bool TryMoveDirectory(string sourceDirectory, string targetDirectory)
    {
        try
        {
            Directory.Move(sourceDirectory, targetDirectory);
            return true;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
        catch (IOException ex) when (IsMissingPathFailure(ex))
        {
            return false;
        }
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

    private void RemoveInactiveStagedDirectories(HashSet<string> activeDirectories)
    {
        foreach (var directory in Directory.EnumerateDirectories(_stagedDirectory))
        {
            var name = Path.GetFileName(directory);
            if (string.IsNullOrWhiteSpace(name) || name.StartsWith(".", StringComparison.Ordinal))
                continue;

            if (activeDirectories.Contains(name))
                continue;

            DeleteDirectoryQuietly(directory);
            PatchHelper.Log($"[Workshop] Removed inactive staged Workshop item {name}");
        }
    }

    private void RemoveInactiveStagedRootFiles()
    {
        foreach (var file in Directory.EnumerateFiles(_stagedDirectory).ToArray())
        {
            var fullFile = Path.GetFullPath(file);
            if (!IsPathInsideDirectory(fullFile, _stagedDirectory))
                throw new InvalidOperationException(
                    $"Workshop staged file escapes staged root: {fullFile}"
                );

            DeleteFileQuietly(fullFile);
            PatchHelper.Log($"[Workshop] Removed inactive staged Workshop root file {Path.GetFileName(fullFile)}");
        }
    }

    private static IEnumerable<string> StagedFiles(string directory)
        => Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .OrderBy(path => Path.GetRelativePath(directory, path), StringComparer.OrdinalIgnoreCase);

    private static string DirectoryContentSha256(string directory, IReadOnlyCollection<string> files)
    {
        if (OperatingSystem.IsAndroid())
            return "";

        return DirectoryByteContentSha256(directory, files);
    }

    private static string DirectoryByteContentSha256(string directory, IReadOnlyCollection<string> files)
    {
        using var sha = SHA256.Create();
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(directory, file).Replace('\\', '/');
            var fileHash = FileSha256(file, out var length);
            var header = Encoding.UTF8.GetBytes(relativePath + "\n" + length + "\n");
            sha.TransformBlock(header, 0, header.Length, null, 0);
            sha.TransformBlock(fileHash, 0, fileHash.Length, null, 0);
        }

        sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha.Hash).ToLowerInvariant();
    }

    private static byte[] FileSha256(string path, out long length)
    {
        using var stream = File.OpenRead(path);
        length = stream.Length;
        return SHA256.HashData(stream);
    }

    private static void DeleteDirectoryQuietly(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            return;

        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Workshop] Failed to delete directory {directory}: {ex.Message}");
        }
    }

    private static void DeleteFileQuietly(string file)
    {
        if (string.IsNullOrWhiteSpace(file))
            return;

        try
        {
            File.Delete(file);
        }
        catch (FileNotFoundException)
        {
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Workshop] Failed to delete file {file}: {ex.Message}");
        }
    }

    private static bool IsMissingPathFailure(IOException ex)
        => ex.Message.IndexOf("not find", StringComparison.OrdinalIgnoreCase) >= 0
            || ex.Message.IndexOf("does not exist", StringComparison.OrdinalIgnoreCase) >= 0
            || ex.Message.IndexOf("no such file", StringComparison.OrdinalIgnoreCase) >= 0;

    private static void WriteClearMarker(
        string clearedAtUtc,
        string reason,
        int removedDirectories,
        int removedFiles
    )
    {
        try
        {
            var markerPath = AppPaths.AppPrivateWorkshopClearMarkerPath;
            var parent = Path.GetDirectoryName(markerPath);
            if (!string.IsNullOrWhiteSpace(parent))
                Directory.CreateDirectory(parent);

            File.WriteAllLines(markerPath, new[]
            {
                $"clearedAtUtc={clearedAtUtc}",
                $"reason={SanitizeMarkerValue(reason)}",
                $"removedStagedDirectoryCount={removedDirectories}",
                $"removedStagedRootFileCount={removedFiles}",
                $"steamCloudPushPerformed=false",
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
