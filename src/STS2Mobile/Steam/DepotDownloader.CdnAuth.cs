using System;
using System.Net;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.CDN;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private static readonly TimeSpan CdnAuthTokenTtl = TimeSpan.FromMinutes(20);

    private readonly ExpiringCache<CdnAuthTokenKey, string?> _cdnAuthTokens = new();

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

    private async Task<T> RunCdnDownloadWithRetriesAsync<T>(
        CdnDownloadOperation<T> operation
    )
    {
        foreach (var attempt in CdnDownloadAttempts())
        {
            var result = await TryCdnDownloadAttemptAsync(operation, attempt);
            if (result.TryGetValue(out var value))
                return value;
        }

        throw operation.CreateFailure();
    }

    private async Task<CdnDownloadResult<T>> TryCdnDownloadAttemptAsync<T>(
        CdnDownloadOperation<T> operation,
        CdnServerAttempt attempt
    )
    {
        try
        {
            return await operation.DownloadAsync(attempt);
        }
        catch (SteamKitWebRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return await operation.DownloadWithAuthAsync(attempt);
        }
        catch (Exception ex) when (attempt.CanRetry())
        {
            operation.HandleRetryFailure(this, attempt, ex);
            return CdnDownloadResult<T>.Retry();
        }
    }

    private async Task<CdnDownloadResult<T>> RunCdnAuthRetryAsync<T>(
        CdnAuthRetryRequest<T> request
    )
    {
        var token = await request.Attempt.GetAuthTokenForRetryAsync(
            this,
            request.DepotId
        );
        if (token == null)
            return CdnDownloadResult<T>.Retry();

        try
        {
            return await request.RetryAsync(token);
        }
        catch (Exception ex) when (request.Attempt.CanRetry())
        {
            request.HandleFailure(this, ex);
            return CdnDownloadResult<T>.Retry();
        }
    }

    private void InvalidateCdnAuthToken(uint depotId, Server server)
    {
        if (server != null)
            _cdnAuthTokens.Invalidate(new CdnAuthTokenKey(depotId, server.Host));
    }
}
