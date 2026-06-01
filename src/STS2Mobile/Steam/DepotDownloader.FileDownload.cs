using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private async Task DownloadFileAsync(
        DepotManifest.FileData file,
        uint depotId,
        byte[] depotKey,
        CancellationToken ct
    )
    {
        var fileName = GetManifestFileName(file);
        if (fileName == null)
            return;

        var target = CreateDepotFilePaths(fileName);
        var writeLock = GetDepotFileWriteLock(target);

        await writeLock.WaitAsync(ct);
        try
        {
            _currentDownloadFile = target.FileName;
            ForceReportProgress();

            if (TryHandleDirectoryTarget(file, target))
                return;

            if (TryUseExistingFile(file, target))
                return;

            PrepareDepotFileDownload(file, target);
            await WriteDepotFileChunksAsync(file, depotId, depotKey, target, ct);
            CommitVerifiedDepotFile(file, target);
        }
        finally
        {
            DeleteQuietly(target.TempPath);
            writeLock.Release();
        }
    }

    private bool TryHandleDirectoryTarget(
        DepotManifest.FileData file,
        (
            string FileName,
            string FilePath,
            string? FileDir,
            string TempPath,
            string LockKey
        ) target
    )
    {
        if (!file.Flags.HasFlag(EDepotFileFlag.Directory))
            return false;

        Directory.CreateDirectory(target.FilePath);
        return true;
    }

    private bool TryUseExistingFile(
        DepotManifest.FileData file,
        (
            string FileName,
            string FilePath,
            string? FileDir,
            string TempPath,
            string LockKey
        ) target
    )
    {
        // Validate existing file against manifest SHA-1 hash. A size-only check
        // would miss corruption from interrupted writes (SetLength pre-allocates).
        if (!File.Exists(target.FilePath) || !VerifyFileHash(target.FilePath, file))
            return false;

        Interlocked.Add(ref _downloadedBytes, (long)file.TotalSize);
        ForceReportProgress();
        return true;
    }

    private void PrepareDepotFileDownload(
        DepotManifest.FileData file,
        (
            string FileName,
            string FilePath,
            string? FileDir,
            string TempPath,
            string LockKey
        ) target
    )
    {
        var fileSize = checked((long)file.TotalSize);
        EnsureEnoughFreeSpaceForFile(target.FileDir ?? _gameDir, fileSize, target.FileName);
        ValidateFileChunks(target.FileName, file);

        // Write to a temp file, verify hash, then move into place. This prevents
        // a partially-written file from being mistaken as complete on retry.
        DeleteQuietly(target.TempPath);
    }
}
