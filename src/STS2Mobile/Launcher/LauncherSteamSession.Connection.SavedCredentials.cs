using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSteamSession
{
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
}
