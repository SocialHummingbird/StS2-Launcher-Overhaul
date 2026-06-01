using System;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    private void Teardown()
    {
        RunTeardownStep("LogOff", () => _steamUser?.LogOff());
        RunTeardownStep("Disconnect", () => _client?.Disconnect());
        _callbackPump.Stop(2000, clearThread: true);
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
