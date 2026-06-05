using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
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

        internal async Task<T> RunAsync(
            DepotDownloader owner,
            IEnumerable<CdnServerAttempt> attempts
        )
        {
            foreach (var attempt in attempts)
            {
                var result = await TryAsync(owner, attempt);
                if (result.TryGetValue(out var value))
                    return value;
            }

            throw CreateFailure();
        }

        private async Task<CdnDownloadResult<T>> TryAsync(
            DepotDownloader owner,
            CdnServerAttempt attempt
        )
        {
            try
            {
                return await DownloadAsync(attempt);
            }
            catch (SteamKitWebRequestException ex)
                when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                return await DownloadWithAuthAsync(attempt);
            }
            catch (Exception ex) when (attempt.CanRetry())
            {
                return RetryAfterFailure(owner, attempt, ex);
            }
        }

        private Task<CdnDownloadResult<T>> DownloadAsync(CdnServerAttempt attempt)
            => _downloadAsync(attempt);

        private Task<CdnDownloadResult<T>> DownloadWithAuthAsync(
            CdnServerAttempt attempt
        )
            => _downloadWithAuthAsync(attempt);

        private CdnDownloadResult<T> RetryAfterFailure(
            DepotDownloader owner,
            CdnServerAttempt attempt,
            Exception ex
        )
        {
            attempt.HandleDownloadRetryFailure(owner, Name, ex);
            return CdnDownloadResult<T>.Retry();
        }

        private Exception CreateFailure()
            => _createFailure();
    }
}
