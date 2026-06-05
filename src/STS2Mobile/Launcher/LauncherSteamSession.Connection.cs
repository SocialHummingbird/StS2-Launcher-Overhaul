using System;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSteamSession
{
    internal async Task<string?> ConnectSavedCredentialsAndVerifyAsync(Action verifyingOwnership)
    {
        try
        {
            return await UseConnectionAndVerifyOwnershipAsync(
                CreateSavedCredentialConnection(),
                verifyingOwnership
            );
        }
        catch (Exception ex)
        {
            return SessionFailure(
                "Connection failed",
                ex,
                "Could not connect to Steam"
            );
        }
    }

    internal async Task<string?> EnsureConnectedAsync()
    {
        if (!TryGetOrCreateSavedConnection(out var connection))
            return "No saved credentials";

        UseConnection(connection);
        try
        {
            await EnsureAppAccessTokenNotDeniedAsync(connection);
            return null;
        }
        catch (Exception ex)
        {
            return SessionFailure("Connection failed", ex, "Connection failed");
        }
    }

    private SteamConnection CreateSavedCredentialConnection()
    {
        if (_credentialStore.TryCreateConnection(out var connection))
            return connection;

        throw new InvalidOperationException("No saved credentials");
    }

    private bool TryGetOrCreateSavedConnection(out SteamConnection connection)
    {
        connection = _connection;
        return connection != null
            || _credentialStore.TryCreateConnection(out connection);
    }
}
