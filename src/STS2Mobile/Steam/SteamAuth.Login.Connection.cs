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
            ContinueWebApiAuthWithoutPersistentConnection(
                "Starting Android Steam WebAPI credential auth without Steam CM connection"
            );
            return;
        }

        if (!await ConnectWithRetriesAsync())
            throw new TimeoutException(
                "Could not establish a Steam auth connection. Check Steam status and try again."
            );
    }
}
