using System;
using System.Threading;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    private bool Teardown()
    {
        if (Interlocked.Exchange(ref _teardownStarted, 1) != 0)
        {
            _teardownComplete.Wait(2000);
            return _callbackPumpStopped;
        }

        try
        {
            RunTeardownStep("LogOff", () => _steamUser?.LogOff());
            RunTeardownStep("Disconnect", () => _client?.Disconnect());
            var stopped = _callbackPump.Stop(2000, clearThread: true);
            _callbackPumpStopped = stopped;
            if (!stopped)
                PatchHelper.Log("[Connection] Callback pump did not stop before teardown completed");
        }
        finally
        {
            _teardownComplete.Set();
        }

        return _callbackPumpStopped;
    }

    private static void RunTeardownStep(string operation, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"[Connection] {operation} failed during teardown: {ex.Message}"
            );
        }
    }
}
