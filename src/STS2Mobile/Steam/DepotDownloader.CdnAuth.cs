using System;
using System.Threading.Tasks;
using SteamKit2.CDN;
using System.Collections.Concurrent;

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
        internal CdnAuthRetry(uint depotId, CdnServerAttempt attempt, string operation, Func<string, Task<CdnDownloadResult<T>>> retryAsync)
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
            catch (Exception ex)when (Attempt.CanRetry())
            {
                Attempt.HandleAuthRetryFailure(owner, DepotId, Operation, ex);
                return CdnDownloadResult<T>.Retry();
            }
        }
    }

    private async Task<string?> GetCdnAuthToken(uint depotId, Server server)
    {
        var key = new CdnAuthTokenKey(depotId, server.Host);
        return await _cdnAuthTokens.GetOrAddAsync(key, CdnAuthTokenTtl, () => _connection.GetCdnAuthTokenAsync(depotId, server.Host), token => token != null);
    }

    private Task<T> RunCdnDownloadWithRetriesAsync<T>(CdnDownloadOperation<T> operation) => operation.RunAsync(this, CdnDownloadAttempts());
    private Task<CdnDownloadResult<T>> RunCdnAuthRetryAsync<T>(CdnAuthRetry<T> retry) => retry.RunAsync(this);
    private void InvalidateCdnAuthToken(uint depotId, Server server)
    {
        if (server != null)
            _cdnAuthTokens.Invalidate(new CdnAuthTokenKey(depotId, server.Host));
    }

    private readonly struct CdnAuthTokenKey : IEquatable<CdnAuthTokenKey>
    {
        internal CdnAuthTokenKey(uint depotId, string host)
        {
            DepotId = depotId;
            Host = host;
        }

        private uint DepotId { get; }
        private string Host { get; }

        public bool Equals(CdnAuthTokenKey other) => DepotId == other.DepotId && Host == other.Host;
        public override bool Equals(object obj) => obj is CdnAuthTokenKey other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + DepotId.GetHashCode();
                hash = hash * 31 + Host.GetHashCode();
                return hash;
            }
        }
    }

    private sealed class ExpiringCache<TKey, TValue>
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, (TValue Value, DateTime Expiry)> _entries = new();
        private bool TryGetFresh(TKey key, out TValue value)
        {
            if (_entries.TryGetValue(key, out var entry))
            {
                if (IsFresh(entry.Expiry))
                {
                    value = entry.Value;
                    return true;
                }

                _entries.TryRemove(key, out _);
            }

            value = default !;
            return false;
        }

        private static bool IsFresh(DateTime expiry) => DateTime.UtcNow < expiry;
        private void SetFor(TKey key, TValue value, TimeSpan ttl) => _entries[key] = (Value: value, Expiry: DateTime.UtcNow.Add(ttl));
        internal async Task<TValue> GetOrAddAsync(TKey key, TimeSpan ttl, Func<Task<TValue>> fetch, Func<TValue, bool>? shouldCache = null)
        {
            if (TryGetFresh(key, out var cached))
                return cached;
            var value = await fetch();
            if (shouldCache?.Invoke(value) != false)
                SetFor(key, value, ttl);
            return value;
        }

        internal void Invalidate(TKey key) => _entries.TryRemove(key, out _);
    }
}
