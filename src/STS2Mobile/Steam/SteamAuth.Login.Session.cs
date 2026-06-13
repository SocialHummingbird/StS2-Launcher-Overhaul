using System;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.Authentication;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal sealed partial class SteamAuth
{
    private const string AndroidDeviceFriendlyName = "StS2 Mobile Android";
    private const string SteamClientWebsiteId = "Client";

    private Task<CredentialsAuthSession> BeginCredentialAuthSessionAsync(
        string username,
        string password,
        string guardData
    )
        => _client.Authentication.BeginAuthSessionViaCredentialsAsync(
            CreateAuthSessionDetails(
                username,
                password,
                guardData
            )
        );

    private AuthSessionDetails CreateAuthSessionDetails(
        string username,
        string password,
        string guardData
    )
    {
        var details = new AuthSessionDetails
            {
                Username = username,
                Password = password,
                IsPersistentSession = true,
                GuardData = guardData,
                Authenticator = this,
            };

        if (OperatingSystem.IsAndroid())
        {
            details.DeviceFriendlyName = AndroidDeviceFriendlyName;
            details.PlatformType = EAuthTokenPlatformType.k_EAuthTokenPlatformType_SteamClient;
            details.ClientOSType = EOSType.AndroidUnknown;
            details.WebsiteID = SteamClientWebsiteId;
        }

        return details;
    }
}
