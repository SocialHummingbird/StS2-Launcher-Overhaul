using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileWriteLocks =
        new();

    private readonly struct DepotFileTarget
    {
        private DepotFileTarget(
            string fileName,
            string filePath,
            string? fileDir,
            string tempPath,
            string lockKey
        )
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
            var filePath = owner.ResolveGamePath(fileName);
            var fileDir = Path.GetDirectoryName(filePath);
            if (fileDir != null)
                Directory.CreateDirectory(fileDir);

            return new DepotFileTarget(
                fileName,
                filePath,
                fileDir,
                filePath + ".downloading",
                Path.GetFullPath(filePath)
            );
        }

        internal async Task DownloadAsync(
            DepotDownloader owner,
            DepotManifest.FileData file,
            uint depotId,
            byte[] depotKey,
            CancellationToken ct
        )
        {
            var writeLock = GetWriteLock();
            await writeLock.WaitAsync(ct);
            try
            {
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

        private SemaphoreSlim GetWriteLock()
            => _fileWriteLocks.GetOrAdd(LockKey, _ => new SemaphoreSlim(1, 1));

        private bool TryCreateDirectoryTarget(DepotManifest.FileData file)
        {
            if (!file.Flags.HasFlag(EDepotFileFlag.Directory))
                return false;

            Directory.CreateDirectory(FilePath);
            return true;
        }

        private bool ExistingFileMatches(DepotManifest.FileData file)
            => File.Exists(FilePath) && VerifyFileHash(FilePath, file);

        private void PrepareDownload(DepotDownloader owner, DepotManifest.FileData file)
        {
            var fileSize = checked((long)file.TotalSize);
            owner.EnsureEnoughFreeSpaceForFile(FileDir ?? owner._gameDir, fileSize, FileName);
            ValidateFileChunks(FileName, file);
            DeleteTempFile();
        }

        private bool TryUseExistingFile(
            DepotDownloader owner,
            DepotManifest.FileData file
        )
        {
            // Validate existing file against manifest SHA-1 hash. A size-only check
            // would miss corruption from interrupted writes (SetLength pre-allocates).
            if (!ExistingFileMatches(file))
                return false;

            Interlocked.Add(ref owner._downloadedBytes, (long)file.TotalSize);
            owner.ForceReportProgress();
            return true;
        }

        private async Task WriteChunksAsync(
            DepotDownloader owner,
            DepotManifest.FileData file,
            uint depotId,
            byte[] depotKey,
            CancellationToken ct
        )
        {
            using var fs = CreateTempFile();
            foreach (var chunk in file.Chunks.OrderBy(c => c.Offset))
            {
                ct.ThrowIfCancellationRequested();
                if (
                    file.TotalSize == 0
                    && chunk.Offset == 0
                    && chunk.UncompressedLength == 0
                )
                    continue;

                ValidateChunk(file, chunk);
                await owner.DownloadAndWriteChunkAsync(
                    fs,
                    depotId,
                    chunk,
                    depotKey,
                    FileName
                );
            }
        }

        private FileStream CreateTempFile()
            => System.IO.File.Create(TempPath);

        private void DeleteTempFile()
            => DeleteQuietly(TempPath);

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

            owner.CommitDownloadedFile(TempPath, FilePath, FileName);
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
}
