using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        internal Server Server { get; }
        private int Index { get; }
        internal int DisplayNumber => Index + 1;
        internal bool HasRetryRemaining => Index < MaxRetries - 1;

        internal static CdnServerAttempt Create(Server server, int index)
            => new(server, index);
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

    private void HandleDownloadRetryFailure(
        CdnServerAttempt attempt,
        string operation,
        Exception ex
    )
    {
        Log($"{operation} failed (attempt {attempt.DisplayNumber}): {ex.Message}");
        MarkServerFailed(attempt.Server);
    }
}
