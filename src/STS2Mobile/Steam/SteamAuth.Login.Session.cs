using System.Threading.Tasks;
using SteamKit2.Authentication;

namespace STS2Mobile.Steam;

internal sealed partial class SteamAuth
{
    private Task<CredentialsAuthSession> BeginCredentialAuthSessionAsync(
        string username,
        string password,
        string guardData
    )
        => _client.Authentication.BeginAuthSessionViaCredentialsAsync(
            new AuthSessionDetails
            {
                Username = username,
                Password = password,
                IsPersistentSession = true,
                GuardData = guardData,
                Authenticator = new AuthAuthenticator(this),
            }
        );
}
