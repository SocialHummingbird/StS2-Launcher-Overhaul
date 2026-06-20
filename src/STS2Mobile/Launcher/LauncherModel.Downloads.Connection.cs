using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private async Task RunWithDepotConnectionAsync(
        DepotConnectionAction action
    )
    {
        var connection = await GetDepotConnectionAsync();
        if (connection == null)
        {
            action.FailNotConnected();
            return;
        }

        await action.RunAsync(connection).ConfigureAwait(false);
    }

    private async Task<SteamConnection> GetDepotConnectionAsync()
    {
        await EnsureConnectedAsync();
        return IsLoggedIn && _steamSession.TryGetConnection(out var connection)
            ? connection
            : null;
    }
}
