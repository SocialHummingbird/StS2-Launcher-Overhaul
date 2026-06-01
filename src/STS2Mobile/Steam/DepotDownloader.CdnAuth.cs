using System;
using System.Threading.Tasks;
using SteamKit2.CDN;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private static readonly TimeSpan CdnAuthTokenTtl = TimeSpan.FromMinutes(20);
    private readonly ExpiringCache<(uint DepotId, string Host), string> _cdnAuthTokens = new();

    private async Task<string?> GetCdnAuthToken(uint depotId, Server server)
    {
        var key = (DepotId: depotId, Host: server.Host);
        if (_cdnAuthTokens.TryGetFresh(key, out var cached))
            return cached;

        var token = await _connection.GetCdnAuthTokenAsync(depotId, server.Host);
        if (token != null)
        {
            _cdnAuthTokens.SetFor(key, token, CdnAuthTokenTtl);
            return token;
        }

        return null;
    }

    private async Task<string?> GetCdnAuthTokenForRetryAsync(uint depotId, Server server)
    {
        var token = await GetCdnAuthToken(depotId, server);
        if (token == null)
            MarkServerFailed(server);

        return token;
    }

    private void InvalidateCdnAuthToken(uint depotId, Server server)
    {
        if (server != null)
            _cdnAuthTokens.Invalidate((depotId, server.Host));
    }

    private void HandleCdnAuthRetryFailure(
        uint depotId,
        Server server,
        string operation,
        int attempt,
        Exception ex
    )
    {
        Log($"{operation} CDN auth retry failed (attempt {attempt + 1}): {ex.Message}");
        InvalidateCdnAuthToken(depotId, server);
        MarkServerFailed(server);
    }
}
