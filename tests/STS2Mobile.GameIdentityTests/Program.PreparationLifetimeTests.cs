using System;
using System.Threading.Tasks;
using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void PreparationTimeoutDrainsLateSuccess()
    {
        var source = new TaskCompletionSource<int>();
        var timeout = new TimeoutException("preparation expired");
        var draining = false;
        var preparation = LauncherPreparationLifetime.RunAsync(source.Task,
            _ => Task.FromException(timeout), () => draining = true, _ => { });
        True(draining, "A timed-out preparation must report that mutations are still draining.");
        True(!preparation.IsCompleted, "The caller must retain its interaction lock until preparation settles.");
        source.SetResult(42);
        try
        {
            preparation.GetAwaiter().GetResult();
            throw new Exception("Late successful preparation incorrectly authorized a launch.");
        }
        catch (TimeoutException error)
        {
            True(ReferenceEquals(timeout, error), "Drain completion must preserve the original timeout.");
        }
    }

    private static void PreparationTimeoutObservesLateFailure()
    {
        var source = new TaskCompletionSource<int>();
        Exception? observed = null;
        var preparation = LauncherPreparationLifetime.RunAsync(source.Task,
            _ => Task.FromException(new TimeoutException()), () => { }, error => observed = error);
        True(!preparation.IsCompleted, "Retry must remain blocked while abandoned work can mutate files.");
        var failure = new InvalidOperationException("late preparation failure");
        source.SetException(failure);
        try { preparation.GetAwaiter().GetResult(); }
        catch (TimeoutException) { }
        True(ReferenceEquals(failure, observed), "Late preparation exceptions must be observed before releasing controls.");
    }

    private static void PreparationSuccessDoesNotDrain()
    {
        var result = LauncherPreparationLifetime.RunAsync(Task.FromResult(42), task => task,
            () => throw new Exception("Successful work should not drain."), _ => { }).GetAwaiter().GetResult();
        Equal(42, result, "Successful preparation should return its result normally.");
    }
}
