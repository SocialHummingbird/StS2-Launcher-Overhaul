using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private async Task RunWithDepotConnectionAsync(
        DepotConnectionAction action
    )
    {
        LauncherLaunchMarkers.RecordPhase("steam depot connection requested");
        var connection = await GetDepotConnectionAsync();
        if (connection == null)
        {
            LauncherLaunchMarkers.RecordPhase("steam depot connection unavailable");
            action.FailNotConnected();
            return;
        }

        LauncherLaunchMarkers.RecordPhase("steam depot connection ready");
        await action.RunAsync(connection).ConfigureAwait(false);
    }

    private async Task<SteamConnection> GetDepotConnectionAsync()
    {
        LauncherLaunchMarkers.RecordPhase("steam ensure connected");
        await EnsureConnectedAsync();
        return IsLoggedIn && _steamSession.TryGetConnection(out var connection)
            ? connection
            : null;
    }
}
