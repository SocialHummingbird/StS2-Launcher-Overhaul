using System;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class SteamAuth
{
    private async Task EnsureConnectedForLoginAsync()
    {
        if (_connectedGate.IsSet)
            return;

        Connect();
        if (!await WaitForConnectAsync())
            throw new TimeoutException(
                "Could not connect to Steam. Check your internet connection."
            );
    }
}
