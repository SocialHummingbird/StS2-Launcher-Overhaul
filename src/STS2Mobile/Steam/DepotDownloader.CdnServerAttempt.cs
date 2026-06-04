using System;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.CDN;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct CdnServerAttempt
    {
        internal CdnServerAttempt(Server server, int index)
        {
            Server = server;
            Index = index;
        }

        private Server Server { get; }
        private int Index { get; }
        private int DisplayNumber => Index + 1;
        private bool HasRetryRemaining => Index < MaxRetries - 1;

        internal Task<int> DownloadChunkAsync(
            DepotDownloader owner,
            uint depotId,
            DepotManifest.ChunkData chunk,
            byte[] buffer,
            byte[] depotKey,
            string? cdnAuthToken = null
        )
            => owner._cdnClient.DownloadDepotChunkAsync(
                depotId,
                chunk,
                Server,
                buffer,
                depotKey,
                cdnAuthToken: cdnAuthToken
            );

        internal Task<DepotManifest> DownloadManifestAsync(
            DepotDownloader owner,
            uint depotId,
            ulong manifestId,
            ulong manifestRequestCode,
            byte[] depotKey,
            string? cdnAuthToken = null
        )
            => owner._cdnClient.DownloadManifestAsync(
                depotId,
                manifestId,
                manifestRequestCode,
                Server,
                depotKey,
                cdnAuthToken: cdnAuthToken
            );

        internal async Task<string?> GetAuthTokenForRetryAsync(
            DepotDownloader owner,
            uint depotId
        )
        {
            var token = await owner.GetCdnAuthToken(depotId, Server);
            if (token == null)
                MarkFailed(owner);

            return token;
        }

        internal bool CanRetry()
            => HasRetryRemaining;

        internal void HandleAuthRetryFailure(
            DepotDownloader owner,
            uint depotId,
            string operation,
            Exception ex
        )
        {
            owner.Log(
                $"{operation} CDN auth retry failed (attempt {DisplayNumber}): {ex.Message}"
            );
            owner.InvalidateCdnAuthToken(depotId, Server);
            MarkFailed(owner);
        }

        internal void HandleDownloadRetryFailure(
            DepotDownloader owner,
            string operation,
            Exception ex
        )
        {
            owner.Log($"{operation} failed (attempt {DisplayNumber}): {ex.Message}");
            MarkFailed(owner);
        }

        private void MarkFailed(DepotDownloader owner)
            => owner.MarkServerFailed(Server);
    }
}
