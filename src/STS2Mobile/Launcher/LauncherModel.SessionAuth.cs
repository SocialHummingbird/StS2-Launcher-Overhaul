using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    // Connects on-demand and verifies ownership. Used when we have saved
    // credentials but no ownership marker.
    internal async Task ConnectAsync()
    {
        SetSessionState(SessionState.Connecting);

        var error = await _steamSession.ConnectSavedCredentialsAndVerifyAsync(
            BeginOwnershipVerification
        );
        CompleteConnectionAttempt(error);
    }

    // Performs interactive login, then verifies ownership.
    internal async Task LoginAsync(string username, string password)
    {
        SetSessionState(SessionState.Authenticating);

        var error = await _steamSession.LoginAndVerifyAsync(
            username,
            password,
            RaiseLogReceived,
            RaiseCodeNeeded,
            BeginOwnershipVerification
        );
        CompleteConnectionAttempt(error);
    }

    internal void SubmitCode(string code) => _steamSession.SubmitCode(code);

    // Creates or reuses a SteamConnection for depot operations.
    internal async Task EnsureConnectedAsync()
    {
        if (IsLoggedIn && _steamSession.Connection != null)
            return;

        SetSessionState(SessionState.Connecting);

        var error = await _steamSession.EnsureConnectedAsync();
        CompleteConnectionAttempt(error);
    }

    private void BeginOwnershipVerification()
    {
        SetSessionState(SessionState.VerifyingOwnership);
    }

    private void CompleteConnectionAttempt(string error)
    {
        if (error == null)
        {
            _connectionResolved = true;
            SetSessionState(SessionState.LoggedIn);
            return;
        }

        _connectionResolved = false;
        SetSessionState(SessionState.Failed, error);
    }
}
