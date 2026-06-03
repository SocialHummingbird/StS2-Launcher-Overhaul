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

        var target = CreateDepotFileTarget(fileName);
        var writeLock = target.GetWriteLock();

        await writeLock.WaitAsync(ct);
        try
        {
            _currentDownloadFile = target.FileName;
            ForceReportProgress();

            if (target.TryCreateDirectoryTarget(file))
                return;

            if (TryUseExistingFile(file, target))
                return;

            target.PrepareDownload(this, file);
            await WriteDepotFileChunksAsync(file, depotId, depotKey, target, ct);
            target.CommitVerified(this, file);
        }
        finally
        {
            target.DeleteTempFile();
            writeLock.Release();
        }
    }

    private bool TryUseExistingFile(
        DepotManifest.FileData file,
        DepotFileTarget target
    )
    {
        // Validate existing file against manifest SHA-1 hash. A size-only check
        // would miss corruption from interrupted writes (SetLength pre-allocates).
        if (!target.ExistingFileMatches(file))
            return false;

        Interlocked.Add(ref _downloadedBytes, (long)file.TotalSize);
        ForceReportProgress();
        return true;
    }
}
