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
        if (_connection != null)
            return await EnsureExistingConnectionAsync(_connection);

        if (!_credentialStore.TryCreateConnection(out var connection))
            return "No saved credentials";

        return await AdoptSavedConnectionAfterAccessCheckAsync(connection);
    }

    private async Task<string?> EnsureExistingConnectionAsync(SteamConnection connection)
    {
        try
        {
            await EnsureAppAccessTokenNotDeniedAsync(connection);
            return null;
        }
        catch (Exception ex)
        {
            DropConnection(connection);
            return SessionFailure("Connection failed", ex, "Connection failed");
        }
    }

    private async Task<string?> AdoptSavedConnectionAfterAccessCheckAsync(
        SteamConnection connection
    )
        => await AdoptConnectionAfterVerificationAsync(
            connection,
            async verifiedConnection =>
            {
                try
                {
                    await EnsureAppAccessTokenNotDeniedAsync(verifiedConnection);
                    return null;
                }
                catch (Exception ex)
                {
                    return SessionFailure("Connection failed", ex, "Connection failed");
                }
            }
        );

    private async Task<string?> AdoptConnectionAfterVerificationAsync(
        SteamConnection connection,
        Func<SteamConnection, Task<string?>> verifyAsync,
        Func<Task<string?>> beforeAdoptAsync = null
    )
    {
        var adopted = false;
        try
        {
            var failure = await verifyAsync(connection);
            if (failure != null)
                return failure;

            if (beforeAdoptAsync != null)
            {
                var beforeAdoptFailure = await beforeAdoptAsync();
                if (beforeAdoptFailure != null)
                    return beforeAdoptFailure;
            }

            UseConnection(connection);
            adopted = true;
            return null;
        }
        finally
        {
            if (!adopted)
                connection.Dispose();
        }
    }

    private void DropConnection(SteamConnection connection)
    {
        if (ReferenceEquals(_connection, connection))
            _connection = null;

        connection.Dispose();
    }

    private SteamConnection CreateSavedCredentialConnection()
    {
        if (_credentialStore.TryCreateConnection(out var connection))
            return connection;

        throw new InvalidOperationException("No saved credentials");
    }
}
