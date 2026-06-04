using System;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private Task<DepotManifest> DownloadManifestWithRetriesAsync(
        uint depotId,
        ulong manifestId,
        ulong manifestRequestCode,
        byte[] depotKey
    )
    {
        Log($"Downloading manifest for depot {depotId}...");
        return RunCdnDownloadWithRetriesAsync(
            CreateManifestDownloadOperation(
                depotId,
                manifestId,
                manifestRequestCode,
                depotKey
            )
        );
    }

    private CdnDownloadOperation<DepotManifest> CreateManifestDownloadOperation(
        uint depotId,
        ulong manifestId,
        ulong manifestRequestCode,
        byte[] depotKey
    )
        => CdnDownloadOperation<DepotManifest>.AcrossServersWithAuthRetry(
            ManifestDownloadOperation,
            attempt => CdnDownloadResult<DepotManifest>.FromAsync(
                () => attempt.DownloadManifestAsync(
                    this,
                    depotId,
                    manifestId,
                    manifestRequestCode,
                    depotKey
                )
            ),
            attempt => RunCdnAuthRetryAsync(
                new CdnAuthRetryRequest<DepotManifest>(
                    depotId,
                    attempt,
                    ManifestAuthRetryOperation,
                    token => CdnDownloadResult<DepotManifest>.FromAsync(
                        () => attempt.DownloadManifestAsync(
                            this,
                            depotId,
                            manifestId,
                            manifestRequestCode,
                            depotKey,
                            token
                        )
                    )
                )
            ),
            () => new Exception(
                $"Failed to download manifest for depot {depotId} after {MaxRetries} attempts"
            )
        );
}
