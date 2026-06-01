using System;
using System.Collections.Concurrent;
using System.IO;
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

        internal static DepotFileTarget Create(
            string fileName,
            string filePath,
            string? fileDir,
            string tempPath,
            string lockKey
        )
            => new(fileName, filePath, fileDir, tempPath, lockKey);

        internal SemaphoreSlim GetWriteLock()
            => _fileWriteLocks.GetOrAdd(LockKey, _ => new SemaphoreSlim(1, 1));

        internal void SetCurrentDownloadFile(Action<string> setCurrentFile)
            => setCurrentFile(FileName);

        internal bool TryCreateDirectoryTarget(DepotManifest.FileData file)
        {
            if (!file.Flags.HasFlag(EDepotFileFlag.Directory))
                return false;

            Directory.CreateDirectory(FilePath);
            return true;
        }

        internal bool ExistingFileMatches(DepotManifest.FileData file)
            => File.Exists(FilePath) && VerifyFileHash(FilePath, file);

        internal void PrepareDownload(DepotDownloader owner, DepotManifest.FileData file)
        {
            var fileSize = checked((long)file.TotalSize);
            owner.EnsureEnoughFreeSpaceForFile(FileDir ?? owner._gameDir, fileSize, FileName);
            ValidateFileChunks(FileName, file);
            DeleteTempFile();
        }

        internal FileStream CreateTempFile()
            => System.IO.File.Create(TempPath);

        internal void DeleteTempFile()
            => DeleteQuietly(TempPath);

        internal void ValidateChunk(DepotManifest.FileData file, DepotManifest.ChunkData chunk)
        {
            ValidateChunkBounds(FileName, file.TotalSize, chunk);
            ValidateChunkSize(FileName, chunk);
        }

        internal Task WriteChunkAsync(
            DepotDownloader owner,
            FileStream stream,
            uint depotId,
            DepotManifest.ChunkData chunk,
            byte[] depotKey
        )
            => owner.DownloadAndWriteChunkAsync(stream, depotId, chunk, depotKey, FileName);

        internal void CommitVerified(DepotDownloader owner, DepotManifest.FileData file)
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

    private DepotFileTarget CreateDepotFileTarget(string fileName)
    {
        var filePath = ResolveGamePath(fileName);
        var fileDir = Path.GetDirectoryName(filePath);
        if (fileDir != null)
            Directory.CreateDirectory(fileDir);

        return DepotFileTarget.Create(
            fileName,
            filePath,
            fileDir,
            filePath + ".downloading",
            Path.GetFullPath(filePath)
        );
    }
}
