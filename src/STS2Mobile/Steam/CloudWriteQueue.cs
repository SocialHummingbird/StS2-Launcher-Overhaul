using System;
using System.Collections.Concurrent;
using System.Threading;

namespace STS2Mobile.Steam;

// Background thread work queue for cloud write operations. Processes actions sequentially
// to avoid mid-game stutters. Flush waits for queued and in-flight writes to complete.
public class CloudWriteQueue : IDisposable
{
    private const int MaxQueuedWrites = 256;
    private const int EnqueueTimeoutMs = 2500;

    private readonly BlockingCollection<Action> _queue = new BlockingCollection<Action>(
        new ConcurrentQueue<Action>(),
        MaxQueuedWrites
    );
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _drainSignal = new(initialState: true);
    private long _pendingWrites;
    private long _droppedWrites;
    private bool _isDisposed;

    public int Count => _queue.Count;

    public CloudWriteQueue()
    {
        _thread = new Thread(ProcessLoop)
        {
            IsBackground = true,
            Name = "CloudSaveWriter",
        };
        _thread.Start();
    }

    public void Enqueue(Action action)
    {
        if (_isDisposed)
        {
            PatchHelper.Log("[Cloud] Write queue is disposed; dropping queued write action");
            return;
        }

        if (action == null)
        {
            PatchHelper.Log("[Cloud] Attempted to enqueue null write action; dropping");
            return;
        }

        _drainSignal.Reset();
        try
        {
            if (!_queue.TryAdd(action, EnqueueTimeoutMs))
            {
                var dropped = Interlocked.Increment(ref _droppedWrites);
                PatchHelper.Log(
                    $"[Cloud] Write queue full ({MaxQueuedWrites}); dropped write action (total dropped: {dropped})"
                );
                if (Volatile.Read(ref _pendingWrites) == 0)
                    _drainSignal.Set();
                return;
            }
        }
        catch (InvalidOperationException)
        {
            PatchHelper.Log("[Cloud] Write queue closing; dropping write action");
            return;
        }

        Interlocked.Increment(ref _pendingWrites);
    }

    // Waits for pending work (queued + in-flight) to complete, up to timeoutMs.
    public bool Flush(int timeoutMs = 5000)
    {
        if (_isDisposed)
            return true;

        var pending = Volatile.Read(ref _pendingWrites);
        if (pending <= 0 && _queue.Count == 0)
            return true;

        // Ensure any race that added work between read and wait is handled.
        if (Volatile.Read(ref _pendingWrites) == 0 && _queue.Count == 0)
            return true;

        PatchHelper.Log($"[Cloud] Flushing {pending} pending writes...");

        if (_drainSignal.Wait(timeoutMs))
        {
            PatchHelper.Log("[Cloud] Flush completed");
            return true;
        }

        PatchHelper.Log(
            $"[Cloud] Flush timed out, {_queue.Count} queued + {Volatile.Read(ref _pendingWrites)} total pending writes"
        );
        if (Volatile.Read(ref _droppedWrites) > 0)
            PatchHelper.Log($"[Cloud] Flush warning: {_droppedWrites} actions were previously dropped");
        return false;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        var completed = Flush(5000);
        if (!completed)
            PatchHelper.Log("[Cloud] Flush timed out during dispose");

        _queue.CompleteAdding();
        if (!_thread.Join(3000))
            PatchHelper.Log("[Cloud] Cloud write thread did not stop in time");
        else
            PatchHelper.Log("[Cloud] Cloud write thread stopped");

        if (Volatile.Read(ref _droppedWrites) > 0)
            PatchHelper.Log($"[Cloud] Total dropped write actions: {_droppedWrites}");

        _queue.Dispose();
        _drainSignal.Dispose();
    }

    private void ProcessLoop()
    {
        foreach (var action in _queue.GetConsumingEnumerable())
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Cloud] Background write failed: {ex.Message}");
            }
            finally
            {
                if (Interlocked.Decrement(ref _pendingWrites) <= 0)
                {
                    _drainSignal.Set();
                }
            }
        }
    }
}
