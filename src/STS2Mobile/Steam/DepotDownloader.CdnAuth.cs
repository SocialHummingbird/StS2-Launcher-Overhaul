using System;
using System.Net;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.CDN;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private static readonly TimeSpan CdnAuthTokenTtl = TimeSpan.FromMinutes(20);
    private readonly struct CdnAuthTokenKey : IEquatable<CdnAuthTokenKey>
    {
        internal CdnAuthTokenKey(uint depotId, string host)
        {
            DepotId = depotId;
            Host = host;
        }

        private uint DepotId { get; }
        private string Host { get; }

        public bool Equals(CdnAuthTokenKey other)
            => DepotId == other.DepotId && Host == other.Host;

        public override bool Equals(object obj)
            => obj is CdnAuthTokenKey other && Equals(other);

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

    private readonly ExpiringCache<CdnAuthTokenKey, string?> _cdnAuthTokens = new();

    private sealed class CdnDownloadOperation<T>
    {
        private readonly Func<CdnServerAttempt, Task<CdnDownloadResult<T>>> _downloadAsync;
        private readonly Func<CdnServerAttempt, Task<CdnDownloadResult<T>>>
            _downloadWithAuthAsync;
        private readonly Func<Exception> _createFailure;

        internal CdnDownloadOperation(
            string name,
            Func<CdnServerAttempt, Task<CdnDownloadResult<T>>> downloadAsync,
            Func<CdnServerAttempt, Task<CdnDownloadResult<T>>> downloadWithAuthAsync,
            Func<Exception> createFailure
        )
        {
            Name = name;
            _downloadAsync = downloadAsync;
            _downloadWithAuthAsync = downloadWithAuthAsync;
            _createFailure = createFailure;
        }

        private string Name { get; }

        internal Task<CdnDownloadResult<T>> DownloadAsync(CdnServerAttempt attempt)
            => _downloadAsync(attempt);

        internal Task<CdnDownloadResult<T>> DownloadWithAuthAsync(
            CdnServerAttempt attempt
        )
            => _downloadWithAuthAsync(attempt);

        internal void HandleRetryFailure(
            DepotDownloader owner,
            CdnServerAttempt attempt,
            Exception ex
        )
            => attempt.HandleDownloadRetryFailure(owner, Name, ex);

        internal Exception CreateFailure()
            => _createFailure();
    }

    private readonly struct CdnDownloadResult<T>
    {
        private CdnDownloadResult(bool succeeded, T value)
        {
            Succeeded = succeeded;
            Value = value;
        }

        internal bool Succeeded { get; }
        internal T Value { get; }

        internal static CdnDownloadResult<T> Success(T value)
            => new(true, value);

        internal static CdnDownloadResult<T> Retry()
            => new(false, default!);

        internal static async Task<CdnDownloadResult<T>> FromAsync(
            Func<Task<T>> downloadAsync
        )
            => Success(await downloadAsync());

        internal static async Task<CdnDownloadResult<T>> FromValidatedAsync(
            Func<Task<T>> downloadAsync,
            Func<T, bool> isValid
        )
        {
            var value = await downloadAsync();
            return isValid(value) ? Success(value) : Retry();
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

    private async Task<T> RunCdnDownloadWithRetriesAsync<T>(
        CdnDownloadOperation<T> operation
    )
    {
        foreach (var attempt in CdnDownloadAttempts())
        {
            try
            {
                var result = await operation.DownloadAsync(attempt);
                if (result.Succeeded)
                    return result.Value;
            }
            catch (SteamKitWebRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                var result = await operation.DownloadWithAuthAsync(attempt);
                if (result.Succeeded)
                    return result.Value;
            }
            catch (Exception ex) when (attempt.CanRetry())
            {
                operation.HandleRetryFailure(this, attempt, ex);
            }
        }

        throw operation.CreateFailure();
    }

    private async Task<CdnDownloadResult<T>> RunCdnAuthRetryAsync<T>(
        uint depotId,
        CdnServerAttempt attempt,
        string operation,
        Func<string, Task<CdnDownloadResult<T>>> retryAsync
    )
    {
        var token = await attempt.GetAuthTokenForRetryAsync(this, depotId);
        if (token == null)
            return CdnDownloadResult<T>.Retry();

        try
        {
            return await retryAsync(token);
        }
        catch (Exception ex) when (attempt.CanRetry())
        {
            attempt.HandleAuthRetryFailure(this, depotId, operation, ex);
            return CdnDownloadResult<T>.Retry();
        }
    }

    private void InvalidateCdnAuthToken(uint depotId, Server server)
    {
        if (server != null)
            _cdnAuthTokens.Invalidate(new CdnAuthTokenKey(depotId, server.Host));
    }

}
