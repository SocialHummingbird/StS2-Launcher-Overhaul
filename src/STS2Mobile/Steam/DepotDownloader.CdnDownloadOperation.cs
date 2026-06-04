using System;
using System.Threading.Tasks;

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

        internal Task<CdnDownloadResult<T>> DownloadAsync(CdnServerAttempt attempt)
            => _downloadAsync(attempt);

        internal Task<CdnDownloadResult<T>> DownloadWithAuthAsync(
            CdnServerAttempt attempt
        )
            => _downloadWithAuthAsync(attempt);

        internal CdnDownloadResult<T> RetryAfterFailure(
            DepotDownloader owner,
            CdnServerAttempt attempt,
            Exception ex
        )
        {
            attempt.HandleDownloadRetryFailure(owner, Name, ex);
            return CdnDownloadResult<T>.Retry();
        }

        internal Exception CreateFailure()
            => _createFailure();
    }
}
