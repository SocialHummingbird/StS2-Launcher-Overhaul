using System;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private void Log(string msg)
    {
        PatchHelper.Log($"[Depot] {msg}");
        try
        {
            LogMessage?.Invoke(msg);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Depot] Log callback failed: {ex.Message}");
        }
    }
}
