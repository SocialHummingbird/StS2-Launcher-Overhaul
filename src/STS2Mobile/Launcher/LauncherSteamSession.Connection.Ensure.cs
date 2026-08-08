using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSteamSession
{
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
}
