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

    private async Task<T> RunCdnAuthRetryAsync<T>(
        uint depotId,
        CdnServerAttempt attempt,
        string operation,
        Func<string, Task<T>> retryAsync,
        T failed
    )
    {
        var token = await attempt.GetAuthTokenForRetryAsync(this, depotId);
        if (token == null)
            return failed;

        try
        {
            return await retryAsync(token);
        }
        catch (Exception ex) when (attempt.CanRetryAfter(ex))
        {
            attempt.HandleAuthRetryFailure(this, depotId, operation, ex);
            return failed;
        }
    }

    private void InvalidateCdnAuthToken(uint depotId, Server server)
    {
        if (server != null)
            _cdnAuthTokens.Invalidate(CdnAuthTokenKey.ForServer(depotId, server));
    }

}
