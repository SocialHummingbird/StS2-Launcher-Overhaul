using System;
using System.Threading.Tasks;
using SteamKit2.CDN;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private static readonly TimeSpan CdnAuthTokenTtl = TimeSpan.FromMinutes(20);
    private readonly ExpiringCache<(uint DepotId, string Host), string?> _cdnAuthTokens = new();

    private async Task<string?> GetCdnAuthToken(uint depotId, Server server)
    {
        var key = (DepotId: depotId, Host: server.Host);
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
        (Server Server, int Index) attempt,
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
        catch (Exception ex) when (HasRetryRemaining(attempt))
        {
            HandleCdnAuthRetryFailure(depotId, attempt, operation, ex);
            return failed;
        }
    }

    private void InvalidateCdnAuthToken(uint depotId, Server server)
    {
        if (server != null)
            _cdnAuthTokens.Invalidate((DepotId: depotId, Host: server.Host));
    }

    private void HandleCdnAuthRetryFailure(
        uint depotId,
        (Server Server, int Index) attempt,
        string operation,
        Exception ex
    )
    {
        Log(
            $"{operation} CDN auth retry failed (attempt {DisplayNumber(attempt)}): {ex.Message}"
        );
        InvalidateCdnAuthToken(depotId, attempt.Server);
        MarkServerFailed(attempt.Server);
    }
}
