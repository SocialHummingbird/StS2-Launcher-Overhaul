using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private const long MaxWorkshopDepotDownloadBytes = 2L * 1024L * 1024L * 1024L;

    private readonly struct WorkshopDepotManifestReference
    {
        internal WorkshopDepotManifestReference(uint depotId, ulong manifestId, string manifestRequestBranch)
        {
            DepotId = depotId;
            ManifestId = manifestId;
            ManifestRequestBranch = SteamGameBranch.Normalize(manifestRequestBranch);
        }

        internal uint DepotId { get; }
        internal ulong ManifestId { get; }
        internal string ManifestRequestBranch { get; }
    }

    internal Task DownloadWorkshopDepotManifestAsync(
        ulong manifestId,
        string targetDirectory,
        string displayName,
        CancellationToken ct = default
    )
        => RunWithSuspendedIdleTimeoutAsync(
            () => DownloadWorkshopDepotManifestCoreAsync(
                manifestId,
                targetDirectory,
                displayName,
                ct
            )
        );

    private async Task DownloadWorkshopDepotManifestCoreAsync(
        ulong manifestId,
        string targetDirectory,
        string displayName,
        CancellationToken ct
    )
    {
        if (manifestId == 0)
            throw new ArgumentException("Workshop depot manifest id is missing", nameof(manifestId));

        var targetRoot = Path.GetFullPath(targetDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(targetRoot) ?? _dataDir);

        _servers = await LoadCdnServersAsync(ct).ConfigureAwait(false);
        var depot = await ResolveWorkshopDepotManifestAsync(manifestId).ConfigureAwait(false);
        Log(
            $"Workshop depot manifest {manifestId} for {displayName} maps to depot {depot.DepotId} branch '{depot.ManifestRequestBranch}'"
        );

        var depotKey = await _connection.GetDepotDecryptionKeyAsync(depot.DepotId)
            .ConfigureAwait(false);
        var manifestRequestCode = await GetManifestRequestCodeAsync(
            depot.DepotId,
            depot.ManifestId,
            depot.ManifestRequestBranch
        ).ConfigureAwait(false);
        var manifest = await DownloadManifestWithRetriesAsync(
            new ManifestDownloadRequest(
                depot.DepotId,
                depot.ManifestId,
                manifestRequestCode,
                depotKey
            )
        ).ConfigureAwait(false);

        ValidateWorkshopDepotManifest(manifest);
        await DownloadWorkshopDepotFilesAtomicallyAsync(
            manifest,
            depot.DepotId,
            depotKey,
            targetRoot,
            displayName,
            ct
        ).ConfigureAwait(false);
    }

    private async Task<WorkshopDepotManifestReference> ResolveWorkshopDepotManifestAsync(ulong manifestId)
    {
        var depots = await ProductInfoApp.GetMainDepotsSectionAsync(this).ConfigureAwait(false);
        foreach (var depot in depots.Children)
        {
            if (!uint.TryParse(depot.Name, out var depotId))
                continue;

            var reference = await TryResolveWorkshopDepotManifestAsync(
                depot,
                depotId,
                manifestId
            ).ConfigureAwait(false);
            if (reference.HasValue)
                return reference.Value;
        }

        Log(
            $"Steam app metadata did not expose Workshop manifest {manifestId}; using SteamPipe Workshop depot {SteamCloudApp.AppId}"
        );
        return new WorkshopDepotManifestReference(
            SteamCloudApp.AppId,
            manifestId,
            SteamGameBranch.Public
        );
    }

    private async Task<WorkshopDepotManifestReference?> TryResolveWorkshopDepotManifestAsync(
        KeyValue depot,
        uint depotId,
        ulong manifestId
    )
    {
        if (TryFindManifestBranch(depot["manifests"], manifestId, out var branch))
            return new WorkshopDepotManifestReference(depotId, manifestId, branch);

        var otherAppId = ReadKeyValueUInt32(depot["depotfromapp"]);
        if (!otherAppId.HasValue)
            return null;

        var referencedManifests = await ProductInfoApp.TryGetReferencedManifestSectionAsync(
            this,
            otherAppId.Value,
            depotId
        ).ConfigureAwait(false);
        return TryFindManifestBranch(referencedManifests, manifestId, out branch)
            ? new WorkshopDepotManifestReference(depotId, manifestId, branch)
            : null;
    }

    private static bool TryFindManifestBranch(KeyValue manifests, ulong manifestId, out string branch)
    {
        branch = SteamGameBranch.Public;
        if (manifests == KeyValue.Invalid)
            return false;

        foreach (var candidate in manifests.Children)
        {
            var gid = ReadKeyValueUInt64(candidate["gid"]);
            if (gid.HasValue && gid.Value == manifestId)
            {
                branch = string.IsNullOrWhiteSpace(candidate.Name)
                    ? SteamGameBranch.Public
                    : candidate.Name;
                return true;
            }
        }

        return false;
    }

    private static void ValidateWorkshopDepotManifest(DepotManifest manifest)
    {
        ValidateDownloadFileSizes(manifest.Files);
        var totalBytes = ComputeTotalDownloadBytes(
            manifest.Files.Where(file => !file.Flags.HasFlag(EDepotFileFlag.Directory))
        );
        if (totalBytes > MaxWorkshopDepotDownloadBytes)
            throw new IOException(
                $"Workshop depot manifest is too large ({FormatBytes(totalBytes)})"
            );
    }

    private async Task DownloadWorkshopDepotFilesAtomicallyAsync(
        DepotManifest manifest,
        uint depotId,
        byte[] depotKey,
        string targetRoot,
        string displayName,
        CancellationToken ct
    )
    {
        var tempRoot = targetRoot + ".tmp-" + Guid.NewGuid().ToString("N");
        var backupRoot = targetRoot + ".old-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(tempRoot);

        try
        {
            foreach (var file in manifest.Files)
            {
                ct.ThrowIfCancellationRequested();
                await DownloadWorkshopDepotFileAsync(
                    tempRoot,
                    file,
                    depotId,
                    depotKey,
                    ct
                ).ConfigureAwait(false);
            }

            var backupCreated = TryMoveDirectory(targetRoot, backupRoot);

            Directory.Move(tempRoot, targetRoot);
            if (backupCreated)
                DeleteDirectoryQuietly(backupRoot);
            Log($"Workshop depot download complete for {displayName}: {manifest.Files.Count} manifest entries");
        }
        catch
        {
            DeleteDirectoryQuietly(tempRoot);
            TryMoveDirectory(backupRoot, targetRoot);
            throw;
        }
    }

    private async Task DownloadWorkshopDepotFileAsync(
        string root,
        DepotManifest.FileData file,
        uint depotId,
        byte[] depotKey,
        CancellationToken ct
    )
    {
        var fileName = GetManifestFileName(file);
        if (fileName == null)
            return;

        var filePath = ResolveWorkshopDepotPath(root, fileName);
        if (file.Flags.HasFlag(EDepotFileFlag.Directory))
        {
            Directory.CreateDirectory(filePath);
            return;
        }

        var fileDir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(fileDir))
            Directory.CreateDirectory(fileDir);

        EnsureEnoughFreeSpaceForFile(fileDir ?? root, checked((long)file.TotalSize), fileName);
        ValidateFileChunks(fileName, file);

        var tempPath = filePath + ".downloading";
        DeleteQuietly(tempPath);
        try
        {
            using var fs = File.Create(tempPath);
            foreach (var chunk in file.Chunks.OrderBy(c => c.Offset))
            {
                ct.ThrowIfCancellationRequested();
                if (file.TotalSize == 0 && chunk.Offset == 0 && chunk.UncompressedLength == 0)
                    continue;

                ValidateChunkBounds(fileName, file.TotalSize, chunk);
                ValidateChunkSize(fileName, chunk);
                await DownloadAndWriteChunkAsync(
                    fs,
                    depotId,
                    chunk,
                    depotKey,
                    fileName
                ).ConfigureAwait(false);
            }
        }
        catch
        {
            DeleteQuietly(tempPath);
            throw;
        }

        if (!VerifyFileHash(tempPath, file))
        {
            DeleteQuietly(tempPath);
            throw new IOException($"SHA-1 verification failed for Workshop depot file {fileName}");
        }

        CommitDownloadedFile(tempPath, filePath, fileName);
    }

    private static string ResolveWorkshopDepotPath(string rootDirectory, string manifestPath)
    {
        if (string.IsNullOrWhiteSpace(manifestPath))
            throw new IOException("Workshop depot manifest contained an empty file path");

        var normalized = NormalizeManifestPath(manifestPath)
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
        var root = Path.GetFullPath(rootDirectory);
        var fullPath = Path.GetFullPath(Path.Combine(root, normalized));
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar.ToString())
            ? root
            : root + Path.DirectorySeparatorChar;

        if (!string.Equals(fullPath, root, StringComparison.Ordinal)
            && !fullPath.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            throw new IOException($"Workshop depot path escapes item directory: {manifestPath}");
        }

        return fullPath;
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
        catch (IOException ex) when (IsMissingPathFailure(ex))
        {
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Depot] Failed to delete temporary Workshop directory {directory}: {ex.Message}");
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

    private static bool IsMissingPathFailure(IOException ex)
        => ex.Message.IndexOf("not find", StringComparison.OrdinalIgnoreCase) >= 0
            || ex.Message.IndexOf("does not exist", StringComparison.OrdinalIgnoreCase) >= 0
            || ex.Message.IndexOf("no such file", StringComparison.OrdinalIgnoreCase) >= 0;
}
