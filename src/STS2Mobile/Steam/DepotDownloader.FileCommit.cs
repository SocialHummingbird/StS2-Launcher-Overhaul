using System;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private void CommitVerifiedDepotFile(DepotManifest.FileData file, DepotFileTarget target)
    {
        if (!VerifyFileHash(target.TempPath, file))
        {
            DeleteQuietly(target.TempPath);
            throw new Exception($"SHA-1 verification failed for {target.FileName} after download");
        }

        CommitDownloadedFile(target.TempPath, target.FilePath, target.FileName);
    }
}
