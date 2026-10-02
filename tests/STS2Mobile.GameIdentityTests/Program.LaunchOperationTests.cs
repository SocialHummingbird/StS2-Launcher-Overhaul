using System;
using System.Threading.Tasks;
using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void LaunchSaveCleanupTimeout()
    {
        var source = new TaskCompletionSource<bool>();
        True(!LauncherSaveCleanup.WaitAsync(source.Task, TimeSpan.FromMilliseconds(10)).GetAwaiter().GetResult(),
            "A stuck save writer must block launch and return control to the launcher.");
        source.SetResult(true);
        True(LauncherSaveCleanup.WaitAsync(source.Task, TimeSpan.FromSeconds(1)).GetAwaiter().GetResult(),
            "A later explicit retry can proceed after cleanup really finishes.");
    }

    private static void LaunchOperationTimeout()
    {
        var operation = new LauncherStartupOperation("attempt-timeout");
        var source = new TaskCompletionSource<bool>();
        var task = operation.StartGame(() => source.Task);
        try
        {
            operation.WaitAsync(task, new LauncherAsyncSignalWaiter(new LaunchTestSignals()), LauncherMonotonicDeadline.Start(TimeSpan.FromMilliseconds(10)))
                .GetAwaiter().GetResult();
            throw new Exception("A stuck task was accepted as complete.");
        }
        catch (TimeoutException) { }
        True(operation.RequiresRestart, "Timeout must fail the operation without starting another game task.");
        source.SetResult(true);
        True(!operation.Complete(), "Timed-out work cannot revive the operation.");
    }

    private sealed class LaunchTestSignals : ILauncherAsyncSignalSource
    {
        public Task WaitForProcessFrameAsync() => Task.CompletedTask;
        public Task WaitForFramePostDrawAsync() => Task.CompletedTask;
        public Task WaitForDelayAsync(int milliseconds, System.Threading.CancellationToken token) => Task.Delay(milliseconds, token);
    }

    private static void LaunchOperationLateSuccess()
    {
        var operation = new LauncherStartupOperation("attempt-a");
        var source = new TaskCompletionSource<bool>();
        operation.StartGame(() => source.Task);
        operation.Fail();
        source.SetResult(true);
        operation.Observation.GetAwaiter().GetResult();
        True(!operation.Complete(), "A late successful task must not complete a failed attempt.");
        True(operation.RequiresRestart, "An abandoned game startup requires a fresh process.");
        Equal(LauncherStartupStage.Failed, operation.Stage, "Failure is terminal.");
    }

    private static void LaunchOperationLateFault()
    {
        Exception? observed = null;
        var operation = new LauncherStartupOperation("attempt-b", ex => observed = ex);
        var source = new TaskCompletionSource<bool>();
        operation.StartGame(() => source.Task);
        operation.Fail();
        var failure = new InvalidOperationException("late initialization failure");
        source.SetException(failure);
        operation.Observation.GetAwaiter().GetResult();
        True(ReferenceEquals(failure, observed), "The original task's late exception must be observed.");
        Equal(LauncherStartupStage.Failed, operation.Stage, "Late failure cannot change the terminal outcome.");
    }

    private static void LaunchOperationRejectsOverlap()
    {
        var operation = new LauncherStartupOperation("attempt-c");
        var calls = 0;
        operation.StartGame(() => { calls++; return Task.CompletedTask; });
        try
        {
            operation.StartGame(() => { calls++; return Task.CompletedTask; });
            throw new Exception("Second startup was accepted.");
        }
        catch (InvalidOperationException) { }
        Equal(1, calls, "A second factory must never run.");
        True(operation.Complete(), "A completed owned task can finish the attempt.");
        operation.Fail();
        Equal(LauncherStartupStage.Running, operation.Stage, "Successful completion is terminal.");
    }

    private static void LaunchOperationFactoryDoesNotHoldStateLock()
    {
        var operation = new LauncherStartupOperation("factory-lock");
        Task<LauncherStartupStage>? inspection = null;
        var work = operation.StartGame(() =>
        {
            inspection = Task.Factory.StartNew(() => operation.Stage,
                System.Threading.CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            True(inspection.Wait(TimeSpan.FromSeconds(2)),
                "Game code must not hold the operation lock while waiting for another thread to inspect launch state.");
            Equal(LauncherStartupStage.Starting, inspection.Result, "Ownership must be reserved before invoking game code.");
            return Task.CompletedTask;
        });
        try { work.GetAwaiter().GetResult(); }
        finally { inspection?.GetAwaiter().GetResult(); }
        True(operation.Complete(), "Moving invocation outside the lock must preserve normal completion.");
    }

    private static void LaunchOperationCanFailDuringFactory()
    {
        var operation = new LauncherStartupOperation("factory-failure");
        Task? failure = null;
        var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var work = operation.StartGame(() =>
        {
            failure = Task.Factory.StartNew(operation.Fail,
                System.Threading.CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            True(failure.Wait(TimeSpan.FromSeconds(2)),
                "A failure notification must not wait for synchronous game initialization to return.");
            return source.Task;
        });
        try
        {
            if (work.IsFaulted) work.GetAwaiter().GetResult();
            True(ReferenceEquals(source.Task, work), "Keep observing the original game task after a concurrent failure.");
            source.SetResult(true);
            operation.Observation.GetAwaiter().GetResult();
            True(operation.RequiresRestart && !operation.Complete(), "Late success cannot revive a failed launch.");
        }
        finally { failure?.GetAwaiter().GetResult(); }
    }
}
