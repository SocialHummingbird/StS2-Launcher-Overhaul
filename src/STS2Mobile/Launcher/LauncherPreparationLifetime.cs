using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal static class LauncherPreparationLifetime
{
    internal static async Task<T> RunAsync<T>(Task<T> preparation, Func<Task, Task> wait,
        Action draining, Action<Exception> observeLateFailure)
    {
        try
        {
            await wait(preparation);
            return await preparation;
        }
        catch
        {
            // A deadline ends the launch attempt, not uncancellable filesystem work.
            // Do not release the caller's interaction lock until all mutations stop.
            if (!preparation.IsCompleted)
            {
                try { draining(); } catch { }
            }
            try { await preparation; }
            catch (Exception lateFailure)
            {
                try { observeLateFailure(lateFailure); } catch { }
            }
            // A late successful result must never resume the automatic handoff.
            throw;
        }
    }
}
