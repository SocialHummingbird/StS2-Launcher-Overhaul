using System;
using System.Threading.Tasks;
using SteamKit2.CDN;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct CdnAuthTokenKey
    {
        private CdnAuthTokenKey(uint depotId, string host)
        {
            DepotId = depotId;
            Host = host;
        }

        private uint DepotId { get; }
        private string Host { get; }

        internal static CdnAuthTokenKey ForServer(uint depotId, Server server)
            => new(depotId, server.Host);
    }

    private static readonly TimeSpan CdnAuthTokenTtl = TimeSpan.FromMinutes(20);
    private readonly ExpiringCache<CdnAuthTokenKey, string?> _cdnAuthTokens = new();

    private async Task<string?> GetCdnAuthToken(uint depotId, Server server)
    {
        var key = CdnAuthTokenKey.ForServer(depotId, server);
        return await _cdnAuthTokens.GetOrAddAsync(
            key,
            CdnAuthTokenTtl,
            () => _connection.GetCdnAuthTokenAsync(depotId, server.Host),
            token => token != null
        );
    }

    private async Task<string?> GetCdnAuthTokenForRetryAsync(uint depotId, Server server)
    {
        var token = await GetCdnAuthToken(depotId, server);
        if (token == null)
            MarkServerFailed(server);

        return token;
    }

    private async Task<T> RunCdnAuthRetryAsync<T>(
        uint depotId,
        CdnServerAttempt attempt,
        string operation,
        Func<string, Task<T>> retryAsync,
        T failed
    )
    {
        var token = await GetCdnAuthTokenForRetryAsync(depotId, attempt.Server);
        if (token == null)
            return failed;

        try
        {
            return await retryAsync(token);
        }
        catch (Exception ex) when (attempt.HasRetryRemaining)
        {
            HandleCdnAuthRetryFailure(depotId, attempt, operation, ex);
            return failed;
        }
    }

    private void InvalidateCdnAuthToken(uint depotId, Server server)
    {
        if (server != null)
            _cdnAuthTokens.Invalidate(CdnAuthTokenKey.ForServer(depotId, server));
    }

    private void HandleCdnAuthRetryFailure(
        uint depotId,
        CdnServerAttempt attempt,
        string operation,
        Exception ex
    )
    {
        Log(
            $"{operation} CDN auth retry failed (attempt {attempt.DisplayNumber}): {ex.Message}"
        );
        InvalidateCdnAuthToken(depotId, attempt.Server);
        MarkServerFailed(attempt.Server);
    }
}
