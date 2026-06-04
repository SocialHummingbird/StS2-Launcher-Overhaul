using System.Text.Json.Serialization;
using System;

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

        internal bool HasUsableTokens()
            => RefreshToken != null && AccountName != null;

        internal string AccountNameOrEmpty()
            => AccountName ?? string.Empty;

        internal bool HasAccount()
            => AccountName != null;

        internal bool IsAccount(string accountName)
            => AccountName == accountName;

        internal bool TryGetAccountName(out string accountName)
        {
            accountName = AccountName;
            return accountName != null;
        }

        internal SteamConnection CreateConnection()
            => new(AccountName, RefreshToken);

        internal void Use(Action<string, string> useCredentials)
            => useCredentials(AccountName, RefreshToken);

        internal string GuardDataOrEmpty()
            => GuardData ?? string.Empty;
    }
}
