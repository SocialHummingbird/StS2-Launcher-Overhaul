using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    // Creates or reuses a SteamConnection for depot operations.
    internal async Task EnsureConnectedAsync()
    {
        if (IsLoggedIn && _steamSession.TryGetConnection(out _))
            return;

        await RunConnectionAttemptAsync(
            SessionState.Connecting,
            _ => _steamSession.EnsureConnectedAsync()
        );
    }
}
