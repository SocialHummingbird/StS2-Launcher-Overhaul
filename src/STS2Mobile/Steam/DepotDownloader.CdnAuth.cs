using System;
using System.Threading.Tasks;
using SteamKit2.CDN;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private const string CdnChunkAuthRetryOperation = "Chunk";
    private const string CdnChunkDownloadOperation = "Chunk download";
    private const string CdnManifestAuthRetryOperation = "Manifest";
    private const string CdnManifestDownloadOperation = "Manifest download";
    private static readonly TimeSpan CdnAuthTokenTtl = TimeSpan.FromMinutes(20);

    private readonly ExpiringCache<CdnAuthTokenKey, string?> _cdnAuthTokens = new();

    private readonly struct CdnAuthRetry<T>
    {
        internal CdnAuthRetry(
            uint depotId,
            CdnServerAttempt attempt,
            string operation,
            Func<string, Task<CdnDownloadResult<T>>> retryAsync
        )
        {
            DepotId = depotId;
            Attempt = attempt;
            Operation = operation;
            RetryAsync = retryAsync;
        }

        private uint DepotId { get; }
        private CdnServerAttempt Attempt { get; }
        private string Operation { get; }
        private Func<string, Task<CdnDownloadResult<T>>> RetryAsync { get; }

        internal async Task<CdnDownloadResult<T>> RunAsync(DepotDownloader owner)
        {
            var token = await Attempt.GetAuthTokenForRetryAsync(owner, DepotId);
            if (token == null)
                return CdnDownloadResult<T>.Retry();

            try
            {
                return await RetryAsync(token);
            }
            catch (Exception ex) when (Attempt.CanRetry())
            {
                Attempt.HandleAuthRetryFailure(
                    owner,
                    DepotId,
                    Operation,
                    ex
                );
                return CdnDownloadResult<T>.Retry();
            }
        }
    }

    private async Task<string?> GetCdnAuthToken(uint depotId, Server server)
    {
        var key = new CdnAuthTokenKey(depotId, server.Host);
        return await _cdnAuthTokens.GetOrAddAsync(
            key,
            CdnAuthTokenTtl,
            () => _connection.GetCdnAuthTokenAsync(depotId, server.Host),
            token => token != null
        );
    }

    private Task<T> RunCdnDownloadWithRetriesAsync<T>(
        CdnDownloadOperation<T> operation
    )
        => operation.RunAsync(this, CdnDownloadAttempts());

    private Task<CdnDownloadResult<T>> RunCdnAuthRetryAsync<T>(
        CdnAuthRetry<T> retry
    )
        => retry.RunAsync(this);

    private void InvalidateCdnAuthToken(uint depotId, Server server)
    {
        if (server != null)
            _cdnAuthTokens.Invalidate(new CdnAuthTokenKey(depotId, server.Host));
    }
}
