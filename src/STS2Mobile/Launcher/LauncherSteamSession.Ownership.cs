using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSteamSession
{
    private const string OwnershipDeniedMessage =
        "You don't own Slay the Spire 2. Purchase on Steam to play.";
    private const string AppAccessTokenDeniedMessage =
        "Steam denied app access token; ownership/session may be invalid";

    internal bool HasOwnershipMarker()
        => HasOwnershipMarkerForCurrentAccount();

    private async Task<string?> UseConnectionAndVerifyOwnershipAsync(
        SteamConnection connection,
        Action verifyingOwnership,
        string accountName = null,
        Func<Task<string?>> beforeAdoptAsync = null,
        bool saveOwnershipMarker = true
    )
        => await AdoptConnectionAfterVerificationAsync(
            connection,
            verifiedConnection => VerifyOwnershipForSessionAsync(
                verifiedConnection,
                verifyingOwnership,
                accountName,
                saveOwnershipMarker
            ),
            beforeAdoptAsync
        );

    private SteamConnection UseConnection(SteamConnection connection)
    {
        _connection = connection;
        return connection;
    }

    private async Task<string?> VerifyOwnershipForSessionAsync(
        SteamConnection connection,
        Action verifyingOwnership,
        string accountNameOverride = null,
        bool saveOwnershipMarker = true
    )
    {
        verifyingOwnership?.Invoke();
        var accountName = accountNameOverride ?? GetAccountNameForOwnershipVerification();
        var owns = await HasAppAccessTokenAsync(connection);
        return CompleteOwnershipVerification(accountName, owns, saveOwnershipMarker);
    }

    private string CompleteOwnershipVerification(
        string accountName,
        bool owns,
        bool saveOwnershipMarker
    )
    {
        if (owns && saveOwnershipMarker)
            SaveOwnershipMarker(accountName);
        PatchHelper.Log(owns ? "[Launcher] Ownership verified" : "[Launcher] Ownership denied");
        return owns ? null : OwnershipDeniedMessage;
    }

    private string GetAccountNameForOwnershipVerification()
    {
        if (_credentialStore.TryGetAccountName(out var accountName))
            return accountName;

        throw new InvalidOperationException("Cannot verify ownership without an account");
    }

    private static async Task EnsureAppAccessTokenNotDeniedAsync(SteamConnection connection)
    {
        await connection.GetAppAccessTokenOrPublicAsync(
            SteamCloudApp.AppId,
            AppAccessTokenDeniedMessage
        );
    }

    private static async Task<bool> HasAppAccessTokenAsync(SteamConnection connection)
    {
        try
        {
            await connection.GetAppAccessTokenOrPublicAsync(
                SteamCloudApp.AppId,
                OwnershipDeniedMessage
            );
            return true;
        }
        catch (InvalidOperationException ex)
            when (ex.Message == OwnershipDeniedMessage)
        {
            return false;
        }
    }
}
