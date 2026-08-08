using System;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct ManifestDownloadRequest
    {
        internal ManifestDownloadRequest(
            uint depotId,
            ulong manifestId,
            ulong requestCode,
            byte[] depotKey
        )
        {
            DepotId = depotId;
            ManifestId = manifestId;
            RequestCode = requestCode;
            DepotKey = depotKey;
        }

        internal uint DepotId { get; }
        private ulong ManifestId { get; }
        private ulong RequestCode { get; }
        private byte[] DepotKey { get; }

        internal string DownloadLogMessage()
            => $"Downloading manifest for depot {DepotId}...";

        internal string FailureMessage()
            => $"Failed to download manifest for depot {DepotId} after {MaxRetries} attempts";

        internal Task<DepotManifest> DownloadAsync(
            DepotDownloader owner,
            CdnServerAttempt attempt,
            string? cdnAuthToken = null
        )
            => attempt.DownloadManifestAsync(
                owner,
                DepotId,
                ManifestId,
                RequestCode,
                DepotKey,
                cdnAuthToken
            );
    }

    private Task<DepotManifest> DownloadManifestWithRetriesAsync(
        ManifestDownloadRequest request
    )
    {
        Log(request.DownloadLogMessage());
        return RunCdnDownloadWithRetriesAsync(
            CreateManifestDownloadOperation(request)
        );
    }

    private CdnDownloadOperation<DepotManifest> CreateManifestDownloadOperation(
        ManifestDownloadRequest request
    )
        => CdnDownloadOperation<DepotManifest>.AcrossServersWithAuthRetry(
            CdnManifestDownloadOperation,
            attempt => CdnDownloadResult<DepotManifest>.FromAsync(
                () => request.DownloadAsync(this, attempt)
            ),
            attempt => RunCdnAuthRetryAsync(
                new CdnAuthRetry<DepotManifest>(
                    request.DepotId,
                    attempt,
                    CdnManifestAuthRetryOperation,
                    token => CdnDownloadResult<DepotManifest>.FromAsync(
                        () => request.DownloadAsync(this, attempt, token)
                    )
                )
            ),
            () => new Exception(request.FailureMessage())
        );
}
