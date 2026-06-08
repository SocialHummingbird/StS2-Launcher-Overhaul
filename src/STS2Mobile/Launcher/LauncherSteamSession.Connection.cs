using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSteamSession
{
    private const int SavedConnectionVerifyAttempts = 3;

    internal async Task<string?> ConnectSavedCredentialsAndVerifyAsync(Action verifyingOwnership)
    {
        Exception lastFailure = null;

        for (var attempt = 1; attempt <= SavedConnectionVerifyAttempts; attempt++)
        {
            try
            {
                if (attempt > 1)
                    PatchHelper.Log($"[Launcher] Retrying saved Steam connection ({attempt}/{SavedConnectionVerifyAttempts})");

                return await UseConnectionAndVerifyOwnershipAsync(
                    CreateSavedCredentialConnection(),
                    verifyingOwnership
                );
            }
            catch (Exception ex)
            {
                lastFailure = ex;
                PatchHelper.Log($"[Launcher] Saved Steam connection attempt {attempt}/{SavedConnectionVerifyAttempts} failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        return SessionFailure(
            "Connection failed",
            lastFailure,
            "Could not connect to Steam"
        );
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
    {
        Exception lastFailure = null;

        for (var attempt = 1; attempt <= SavedConnectionVerifyAttempts; attempt++)
        {
            var attemptConnection = attempt == 1 ? connection : CreateSavedCredentialConnection();
            try
            {
                if (attempt > 1)
                    PatchHelper.Log($"[Launcher] Retrying Steam access check ({attempt}/{SavedConnectionVerifyAttempts})");

                return await AdoptConnectionAfterVerificationAsync(
                    attemptConnection,
                    async verifiedConnection =>
                    {
                        await EnsureAppAccessTokenNotDeniedAsync(verifiedConnection);
                        return null;
                    }
                );
            }
            catch (Exception ex)
            {
                lastFailure = ex;
                PatchHelper.Log($"[Launcher] Steam access check attempt {attempt}/{SavedConnectionVerifyAttempts} failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        return SessionFailure("Connection failed", lastFailure, "Connection failed");
    }

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
