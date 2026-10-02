using System;
using System.Collections.Generic;
using System.IO;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;
internal sealed partial class DepotDownloader
{
    private IEnumerable<string> EnumerateDownloadingTempFiles()
    {
        try
        {
            return Directory.GetFiles(_gameDir, "*.downloading", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            Log($"Could not enumerate stale temp downloads: {ex.Message}");
            return Array.Empty<string>();
        }
    }

    private string ResolveGamePath(string manifestPath)
    {
        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            throw new IOException("Depot manifest contained an empty file path");
        }

        var normalized = NormalizeManifestPath(manifestPath).Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        var root = Path.GetFullPath(_gameDir);
        var fullPath = Path.GetFullPath(Path.Combine(root, normalized));
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar.ToString()) ? root : root + Path.DirectorySeparatorChar;
        if (!string.Equals(fullPath, root, StringComparison.Ordinal) && !fullPath.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            throw new IOException($"Depot path escapes game directory: {manifestPath}");
        }

        return fullPath;
    }

    private static string NormalizeManifestPath(string manifestPath)
    {
        if (manifestPath == null)
        {
            return string.Empty;
        }

        var nullIndex = manifestPath.IndexOf('\0');
        if (nullIndex >= 0)
        {
            manifestPath = manifestPath[..nullIndex];
        }

        return manifestPath.Replace('\\', '/');
    }

    private void EnsureEnoughFreeSpaceForFile(string directory, long fileSize, string fileName)
    {
        if (!OperatingSystem.IsAndroid())
            return;
        long availableFreeSpace;
        try
        {
            Directory.CreateDirectory(directory);
            availableFreeSpace = GetAndroidAvailableFreeSpace(directory);
        }
        catch (Exception ex)
        {
            Log($"Could not check free space for {fileName}: {ex.Message}");
            return;
        }

        var required = fileSize + AndroidMinimumFreeSpaceBytes;
        if (availableFreeSpace < required)
        {
            throw new IOException($"Not enough storage for {fileName}: need {FormatBytes(required)}, " + $"available {FormatBytes(availableFreeSpace)}");
        }
    }

    private static long GetAndroidAvailableFreeSpace(string directory)
    {
        var usableBytes = AndroidGodotAppBridge.GetUsableSpaceBytes(directory);
        if (usableBytes > 0)
            return usableBytes;
        var root = Path.GetPathRoot(Path.GetFullPath(directory));
        if (string.IsNullOrWhiteSpace(root))
            return long.MaxValue;
        var drive = new DriveInfo(root);
        return drive.AvailableFreeSpace;
    }

    private void RequireUpdatingBeforeInstalledMutation()
    {
        if (_installUpdate == null)
        {
            throw new IOException($"Refusing to mutate installed files for branch '{_branch}' without an updating installation transaction.");
        }

        _installUpdate.RequireUpdatingBeforeInstalledMutation();
    }

    private void CommitInstalledGameFile(string tempPath, string filePath, string fileName)
    {
        RequireUpdatingBeforeInstalledMutation();
        CommitDownloadedFile(tempPath, filePath, fileName);
    }

    private void CommitDownloadedFile(string tempPath, string filePath, string fileName)
    {
        try
        {
            File.Move(tempPath, filePath, overwrite: true);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to commit downloaded file {fileName}: {ex.Message}", ex);
        }
    }

    private bool TryDeleteFileIfExists(string path, string successMessage, Func<Exception, string> failureMessage)
    {
        var deleted = TryDeleteFileIfExists(path, failureMessage);
        if (deleted)
            Log(successMessage);
        return deleted;
    }

    private bool TryDeleteFileIfExists(string path, Func<Exception, string> failureMessage)
    {
        if (!File.Exists(path))
            return false;
        try
        {
            File.Delete(path);
            return true;
        }
        catch (Exception ex)
        {
            Log(failureMessage(ex));
            return false;
        }
    }

    private static void DeleteQuietly(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Depot] Failed to delete temporary file {path}: {ex.Message}");
        }
    }

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileWriteLocks = new();
    private readonly struct DepotFileTarget
    {
        private DepotFileTarget(string fileName, string filePath, string? fileDir, string tempPath, string lockKey)
        {
            FileName = fileName;
            FilePath = filePath;
            FileDir = fileDir;
            TempPath = tempPath;
            LockKey = lockKey;
        }

        private string FileName { get; }
        private string FilePath { get; }
        private string? FileDir { get; }
        private string TempPath { get; }
        private string LockKey { get; }

        internal static DepotFileTarget Create(DepotDownloader owner, string fileName)
        {
            owner.RequireUpdatingBeforeInstalledMutation();
            var filePath = owner.ResolveGamePath(fileName);
            var fileDir = Path.GetDirectoryName(filePath);
            if (fileDir != null)
                Directory.CreateDirectory(fileDir);
            return new DepotFileTarget(fileName, filePath, fileDir, filePath + ".downloading", Path.GetFullPath(filePath));
        }

        internal async Task DownloadAsync(DepotDownloader owner, DepotManifest.FileData file, uint depotId, byte[] depotKey, CancellationToken ct)
        {
            var writeLock = GetWriteLock();
            await writeLock.WaitAsync(ct);
            try
            {
                owner.RequireUpdatingBeforeInstalledMutation();
                owner._currentDownloadFile = FileName;
                owner.ForceReportProgress();
                if (TryCreateDirectoryTarget(file))
                    return;
                if (TryUseExistingFile(owner, file))
                    return;
                PrepareDownload(owner, file);
                await WriteChunksAsync(owner, file, depotId, depotKey, ct);
                CommitVerified(owner, file);
            }
            finally
            {
                DeleteTempFile();
                writeLock.Release();
            }
        }

        private SemaphoreSlim GetWriteLock() => _fileWriteLocks.GetOrAdd(LockKey, _ => new SemaphoreSlim(1, 1));
        private bool TryCreateDirectoryTarget(DepotManifest.FileData file)
        {
            if (!file.Flags.HasFlag(EDepotFileFlag.Directory))
                return false;
            Directory.CreateDirectory(FilePath);
            return true;
        }

        private bool ExistingFileMatches(DepotManifest.FileData file) => File.Exists(FilePath) && VerifyFileHash(FilePath, file);
        private void PrepareDownload(DepotDownloader owner, DepotManifest.FileData file)
        {
            var fileSize = checked((long)file.TotalSize);
            owner.EnsureEnoughFreeSpaceForFile(FileDir ?? owner._gameDir, fileSize, FileName);
            ValidateFileChunks(FileName, file);
            DeleteTempFile();
        }

        private bool TryUseExistingFile(DepotDownloader owner, DepotManifest.FileData file)
        {
            // Validate existing file against manifest SHA-1 hash. A size-only check
            // would miss corruption from interrupted writes (SetLength pre-allocates).
            if (!ExistingFileMatches(file))
                return false;
            Interlocked.Add(ref owner._downloadedBytes, (long)file.TotalSize);
            owner.ForceReportProgress();
            return true;
        }

        private async Task WriteChunksAsync(DepotDownloader owner, DepotManifest.FileData file, uint depotId, byte[] depotKey, CancellationToken ct)
        {
            using var fs = CreateTempFile();
            foreach (var chunk in file.Chunks.OrderBy(c => c.Offset))
            {
                ct.ThrowIfCancellationRequested();
                if (file.TotalSize == 0 && chunk.Offset == 0 && chunk.UncompressedLength == 0)
                    continue;
                ValidateChunk(file, chunk);
                await owner.DownloadAndWriteChunkAsync(fs, depotId, chunk, depotKey, FileName);
            }
        }

        private FileStream CreateTempFile() => System.IO.File.Create(TempPath);
        private void DeleteTempFile() => DeleteQuietly(TempPath);
        private void ValidateChunk(DepotManifest.FileData file, DepotManifest.ChunkData chunk)
        {
            ValidateChunkBounds(FileName, file.TotalSize, chunk);
            ValidateChunkSize(FileName, chunk);
        }

        private void CommitVerified(DepotDownloader owner, DepotManifest.FileData file)
        {
            if (!VerifyFileHash(TempPath, file))
            {
                DeleteTempFile();
                throw new IOException($"SHA-1 verification failed for {FileName} after download");
            }

            owner.CommitInstalledGameFile(TempPath, FilePath, FileName);
        }
    }

    private string? GetManifestFileName(DepotManifest.FileData file)
    {
        var fileName = NormalizeManifestPath(file.FileName);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            Log("Skipping depot file with an empty manifest path");
            return null;
        }

        return fileName;
    }

    // Computes SHA-1 of a decompressed chunk and compares it to the manifest ChunkID.
    private static bool VerifyChunkHash(byte[] buffer, int length, DepotManifest.ChunkData chunk)
    {
        if (chunk.ChunkID == null || chunk.ChunkID.Length == 0)
            return false;
        var hash = ComputeSha1(buffer.AsSpan(0, length));
        return HashesEqual(hash, chunk.ChunkID);
    }

    // Computes SHA-1 of a file on disk and compares it to the manifest hash.
    private static bool VerifyFileHash(string path, DepotManifest.FileData file)
    {
        try
        {
            var info = new FileInfo(path);
            if (info.Length != (long)file.TotalSize)
                return false;
            if (file.FileHash == null || file.FileHash.Length == 0)
                return file.TotalSize == 0;
            var hash = ComputeFileSha1(path);
            return HashesEqual(hash, file.FileHash);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] ComputeSha1(ReadOnlySpan<byte> data)
    {
        if (!OperatingSystem.IsAndroid())
            return System.Security.Cryptography.SHA1.HashData(data);
        return AndroidJavaCrypto.Sha1HashData(data.ToArray());
    }

    private static byte[] ComputeFileSha1(string path)
    {
        if (!OperatingSystem.IsAndroid())
        {
            using var fs = File.OpenRead(path);
            return System.Security.Cryptography.SHA1.HashData(fs);
        }

        return AndroidJavaCrypto.Sha1FileHashData(path);
    }

    private static bool HashesEqual(byte[]? left, byte[]? right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        return left.SequenceEqual(right);
    }

    private static void ValidateChunkBounds(string fileName, ulong fileSize, DepotManifest.ChunkData chunk)
    {
        if (chunk.UncompressedLength == 0)
        {
            throw new IOException($"Depot chunk has zero length for {fileName} at offset {chunk.Offset}");
        }

        if (chunk.Offset > fileSize || chunk.UncompressedLength > fileSize - chunk.Offset)
        {
            throw new IOException($"Depot chunk is outside file bounds for {fileName}: " + $"offset={chunk.Offset}, length={chunk.UncompressedLength}, fileSize={fileSize}");
        }
    }

    private static void ValidateFileChunks(string fileName, DepotManifest.FileData file)
    {
        if (file.TotalSize == 0)
        {
            return;
        }

        if (file.Chunks == null || !file.Chunks.Any())
        {
            throw new IOException($"Depot file has no chunks: {fileName}");
        }
    }
}
