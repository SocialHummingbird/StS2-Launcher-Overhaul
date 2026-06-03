using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.CDN;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct CdnServerAttempt
    {
        private CdnServerAttempt(Server server, int index)
        {
            Server = server;
            Index = index;
        }

        private Server Server { get; }
        private int Index { get; }
        private int DisplayNumber => Index + 1;
        private bool HasRetryRemaining => Index < MaxRetries - 1;

        internal static CdnServerAttempt Create(Server server, int index)
            => new(server, index);

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

    private IReadOnlyList<Server> _servers = Array.Empty<Server>();
    private int _serverIndex;

    private async Task<IReadOnlyList<Server>> LoadCdnServersAsync(CancellationToken ct)
    {
        Log("Getting CDN servers...");
        var allServers = await _connection.LoadCdnServersAsync(ct);
        if (allServers == null || allServers.Count == 0)
            throw new Exception("No CDN servers available");

        var servers = allServers
            .Where(s => s.Type == "SteamCache" || s.Type == "CDN")
            .OrderBy(s => s.WeightedLoad)
            .ToList();

        if (servers.Count == 0)
            servers = allServers.ToList();

        Log($"Using {servers.Count} CDN servers");
        return servers;
    }

    private Server GetCurrentServer()
    {
        if (_servers.Count == 0)
            throw new InvalidOperationException("No Steam CDN servers are available");

        var idx = Volatile.Read(ref _serverIndex);
        return _servers[((idx % _servers.Count) + _servers.Count) % _servers.Count];
    }

    private IEnumerable<CdnServerAttempt> CdnDownloadAttempts()
    {
        for (int attemptIndex = 0; attemptIndex < MaxRetries; attemptIndex++)
            yield return CdnServerAttempt.Create(GetCurrentServer(), attemptIndex);
    }

    private void MarkServerFailed(Server server)
    {
        if (_servers.Count <= 1 || server == null)
            return;

        var current = GetCurrentServer();
        if (string.Equals(current.Host, server.Host, StringComparison.OrdinalIgnoreCase))
            Interlocked.Increment(ref _serverIndex);
    }

}
