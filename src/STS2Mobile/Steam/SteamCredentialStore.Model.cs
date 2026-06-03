using System.Text.Json.Serialization;

namespace STS2Mobile.Steam;

internal sealed partial class SteamCredentialStore
{
    private sealed class SteamCredentials
    {
        public SteamCredentials()
        {
        }

        internal SteamCredentials(
            string accountName,
            string refreshToken,
            string guardData
        )
        {
            AccountName = accountName;
            RefreshToken = refreshToken;
            GuardData = guardData;
        }

        [JsonInclude]
        public string AccountName { get; private set; }

        [JsonInclude]
        public string RefreshToken { get; private set; }

        [JsonInclude]
        public string GuardData { get; private set; }
    }
}
