using System;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal readonly record struct DownloadValidationChunkEvent(string AttemptId, DateTimeOffset Utc, uint DepotId, string ChunkSha1,
    ulong Offset, uint UncompressedBytes, uint CompressedBytes, string Event, string ErrorType,
    int ActiveRequests, int PeakConcurrency, int StartedRequests, int CompletedRequests, long CompletedPayloadBytes);

internal sealed partial class DepotDownloader
{
    internal event Action<DownloadValidationChunkEvent> ValidationChunkChanged;
    internal event Action<uint, DepotChunkTransferProgress> ValidationRawProgressChanged;
    internal string ValidationAttemptId { get; set; }
    private int _validationActiveRequests, _validationPeakConcurrency, _validationStartedRequests, _validationCompletedRequests;
    private long _validationCompletedPayload;

    private async Task<int> ValidationFetchAsync(uint depotId, DepotManifest.ChunkData chunk, CancellationToken ct, Func<Task<int>> fetch)
    {
        ct.ThrowIfCancellationRequested();
        Interlocked.Increment(ref _validationStartedRequests);
        var active = Interlocked.Increment(ref _validationActiveRequests);
        int previous;
        do { previous = Volatile.Read(ref _validationPeakConcurrency); }
        while (active > previous && Interlocked.CompareExchange(ref _validationPeakConcurrency, active, previous) != previous);
        void Emit(string stage, string error = null) => ValidationChunkChanged?.Invoke(new(
            ValidationAttemptId, DateTimeOffset.UtcNow, depotId, Convert.ToHexString(chunk.ChunkID!), chunk.Offset,
            chunk.UncompressedLength, chunk.CompressedLength, stage, error,
            Volatile.Read(ref _validationActiveRequests), Volatile.Read(ref _validationPeakConcurrency),
            Volatile.Read(ref _validationStartedRequests), Volatile.Read(ref _validationCompletedRequests),
            Interlocked.Read(ref _validationCompletedPayload)));
        Emit("start");
        try
        {
            var count = await fetch().ConfigureAwait(false);
            Interlocked.Increment(ref _validationCompletedRequests);
            Interlocked.Add(ref _validationCompletedPayload, chunk.CompressedLength);
            Emit("complete");
            return count;
        }
        catch (Exception error) { Emit(error is OperationCanceledException ? "cancelled" : "failed", error.GetType().Name); throw; }
        finally { Interlocked.Decrement(ref _validationActiveRequests); }
    }
}
