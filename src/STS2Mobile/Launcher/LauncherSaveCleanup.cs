using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal static class LauncherSaveCleanup
{
    internal static async Task<bool> WaitAsync(Task cleanup, TimeSpan budget)
    {
        try { await cleanup.WaitAsync(budget).ConfigureAwait(false); return true; }
        catch (TimeoutException) { return false; }
    }
}
