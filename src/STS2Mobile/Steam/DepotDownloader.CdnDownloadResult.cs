using System;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct CdnDownloadResult<T>
    {
        private CdnDownloadResult(bool succeeded, T value)
        {
            Succeeded = succeeded;
            Value = value;
        }

        private bool Succeeded { get; }
        private T Value { get; }

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
}
