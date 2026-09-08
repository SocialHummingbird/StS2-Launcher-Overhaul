using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void PreloadSaveSyncKeepsCallerResponsive()
    {
        var callerThread = Environment.CurrentManagedThreadId;
        var syncThread = callerThread;
        var loadThread = -1;
        var syncFinished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var syncEntered = new ManualResetEventSlim();
        using var context = new PreloadCallerContext();

        var boundary = LauncherStartupFlow.RunPreloadSaveBoundaryAsync(
            () =>
            {
                // Real Steam connection setup runs synchronously before its first await.
                syncThread = Environment.CurrentManagedThreadId;
                syncEntered.Set();
                return syncFinished.Task;
            },
            () => loadThread = Environment.CurrentManagedThreadId
        );

        True(syncEntered.Wait(TimeSpan.FromSeconds(5)), "Pre-load synchronization never started.");
        Equal(-1, loadThread, "No settings/profile read may start while synchronization is still running.");
        True(!boundary.IsCompleted, "The save boundary must wait for all synchronization work.");
        syncFinished.SetResult(true);
        context.RunUntilCompleted(boundary);

        NotEqual(callerThread, syncThread, "Synchronous Steam connection setup must execute on a worker, leaving the Godot caller responsive.");
        Equal(callerThread, loadThread, "Loading settings must resume on the caller's captured Godot context.");
    }

    private sealed class PreloadCallerContext : SynchronizationContext, IDisposable
    {
        private readonly SynchronizationContext? _previous = Current;
        private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _work = new();
        private readonly AutoResetEvent _available = new(false);

        internal PreloadCallerContext() => SetSynchronizationContext(this);

        public override void Post(SendOrPostCallback callback, object? state)
        {
            _work.Enqueue((callback, state));
            _available.Set();
        }

        internal void RunUntilCompleted(Task task)
        {
            var deadline = Environment.TickCount64 + 5000;
            while (!task.IsCompleted)
            {
                while (_work.TryDequeue(out var item)) item.Callback(item.State);
                if (task.IsCompleted) break;
                if (Environment.TickCount64 >= deadline)
                    throw new TimeoutException("The pre-load save boundary did not settle on its caller context.");
                _available.WaitOne(10);
            }
            task.GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            SetSynchronizationContext(_previous);
            _available.Dispose();
        }
    }
}
