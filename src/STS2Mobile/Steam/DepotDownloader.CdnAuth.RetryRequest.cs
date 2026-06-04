using System;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
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
}
