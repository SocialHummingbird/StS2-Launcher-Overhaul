using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSteamSession
{
    internal async Task<string> ConnectSavedCredentialsAndVerifyAsync(Action verifyingOwnership)
    {
        try
        {
            _connection = CreateSavedCredentialConnection();
            return await VerifyOwnershipForSessionAsync(_connection, verifyingOwnership);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Connection failed: {ex.Message}");
            return $"Could not connect to Steam: {ex.Message}";
        }
    }

    internal async Task<string> EnsureConnectedAsync()
    {
        if (!_credentialStore.TryCreateConnection(out var savedConnection))
            return "No saved credentials";

        var connection = _connection
            ?? savedConnection;

        try
        {
            await EnsureAppAccessTokenNotDeniedAsync(connection);
            _connection = connection;
            return null;
        }
        catch (Exception ex)
        {
            _connection = connection;
            return $"Connection failed: {ex.Message}";
        }
    }

    private SteamConnection CreateSavedCredentialConnection()
    {
        if (_credentialStore.TryCreateConnection(out var connection))
            return connection;

        throw new InvalidOperationException("No saved credentials");
    }
}
