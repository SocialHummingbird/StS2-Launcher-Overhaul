using STS2Mobile.Steam;
using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSteamSession
{
    internal bool HasOwnershipMarker()
        => HasOwnershipMarkerForCurrentAccount();

    private async Task<string> VerifyOwnershipForSessionAsync(
        SteamConnection connection,
        Action verifyingOwnership
    )
    {
        verifyingOwnership?.Invoke();
        if (!_credentialStore.TryGetAccountName(out var accountName))
            throw new InvalidOperationException("Cannot verify ownership without an account");

        var owns = await connection.HasAppAccessTokenAsync(SteamCloudApp.AppId);
        if (owns)
            SaveOwnershipMarker(accountName);

        PatchHelper.Log(owns ? "[Launcher] Ownership verified" : "[Launcher] Ownership denied");

        return owns ? null : "You don't own Slay the Spire 2. Purchase on Steam to play.";
    }

    private static async Task EnsureAppAccessTokenNotDeniedAsync(SteamConnection connection)
    {
        await connection.EnsureAppAccessTokenNotDeniedAsync(
            SteamCloudApp.AppId,
            "Steam denied app access token; ownership/session may be invalid"
        );
    }

}
