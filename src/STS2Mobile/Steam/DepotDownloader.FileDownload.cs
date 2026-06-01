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
        if (!TryGetManifestFileName(file, out var fileName))
            return;

        var target = CreateDepotFileTarget(fileName);
        var writeLock = GetDepotFileWriteLock(target);

        await writeLock.WaitAsync(ct);
        try
        {
            _progress.CurrentFile = target.FileName;
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
        DepotFileTarget target
    )
    {
        if (!file.Flags.HasFlag(EDepotFileFlag.Directory))
            return false;

        Directory.CreateDirectory(target.FilePath);
        return true;
    }

    private bool TryUseExistingFile(DepotManifest.FileData file, DepotFileTarget target)
    {
        // Validate existing file against manifest SHA-1 hash. A size-only check
        // would miss corruption from interrupted writes (SetLength pre-allocates).
        if (!File.Exists(target.FilePath) || !VerifyFileHash(target.FilePath, file))
            return false;

        Interlocked.Add(ref _progress.DownloadedBytes, (long)file.TotalSize);
        ForceReportProgress();
        return true;
    }

    private void PrepareDepotFileDownload(
        DepotManifest.FileData file,
        DepotFileTarget target
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
