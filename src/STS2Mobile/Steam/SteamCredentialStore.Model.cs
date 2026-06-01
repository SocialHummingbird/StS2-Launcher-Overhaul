using System.Text.Json.Serialization;

namespace STS2Mobile.Steam;

internal sealed partial class SteamCredentialStore
{
    private sealed class SteamCredentials
    {
        [JsonInclude]
        public string AccountName { get; set; }

        [JsonInclude]
        public string RefreshToken { get; set; }

        [JsonInclude]
        public string GuardData { get; set; }
    }
}
