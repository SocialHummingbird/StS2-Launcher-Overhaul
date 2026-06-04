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

        private CdnDownloadOperation(
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

        internal static CdnDownloadOperation<T> AcrossServersWithAuthRetry(
            string name,
            Func<CdnServerAttempt, Task<CdnDownloadResult<T>>> downloadAsync,
            Func<CdnServerAttempt, Task<CdnDownloadResult<T>>> downloadWithAuthAsync,
            Func<Exception> createFailure
        )
            => new(name, downloadAsync, downloadWithAuthAsync, createFailure);

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

        internal bool TryGetValue(out T value)
        {
            value = Value;
            return Succeeded;
        }

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

    private readonly struct CdnAuthRetryRequest<T>
    {
        private readonly Func<string, Task<CdnDownloadResult<T>>> _retryAsync;

        internal CdnAuthRetryRequest(
            uint depotId,
            CdnServerAttempt attempt,
            string operation,
            Func<string, Task<CdnDownloadResult<T>>> retryAsync
        )
        {
            DepotId = depotId;
            Attempt = attempt;
            Operation = operation;
            _retryAsync = retryAsync;
        }

        internal uint DepotId { get; }
        internal CdnServerAttempt Attempt { get; }
        private string Operation { get; }

        internal Task<CdnDownloadResult<T>> RetryAsync(string token)
            => _retryAsync(token);

        internal void HandleFailure(DepotDownloader owner, Exception ex)
            => Attempt.HandleAuthRetryFailure(owner, DepotId, Operation, ex);
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
                if ((await operation.DownloadAsync(attempt)).TryGetValue(out var value))
                    return value;
            }
            catch (SteamKitWebRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                if ((await operation.DownloadWithAuthAsync(attempt)).TryGetValue(out var value))
                    return value;
            }
            catch (Exception ex) when (attempt.CanRetry())
            {
                operation.HandleRetryFailure(this, attempt, ex);
            }
        }

        throw operation.CreateFailure();
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
