using System;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class SteamAuth
{
    private async Task EnsureConnectedForLoginAsync()
    {
        if (_connectedGate.IsSet)
            return;

        if (!RequiresPersistentAuthConnection)
        {
            TryStartBestEffortAuthConnection();
            return;
        }

        if (!await ConnectWithRetriesAsync())
            throw new TimeoutException(
                "Could not establish a Steam auth connection. Check Steam status and try again."
            );
    }

    private void TryStartBestEffortAuthConnection()
    {
        try
        {
            Connect();
        }
        catch (Exception ex)
        {
            Log($"Steam CM connection unavailable before Android WebAPI auth: {ex.Message}");
        }
    }
}
