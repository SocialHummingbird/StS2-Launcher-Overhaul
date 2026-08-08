using System;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherCloudSaveState
{
    private readonly struct SavedSteamCredentials
    {
        private SavedSteamCredentials(string accountName, string refreshToken)
        {
            AccountName = accountName;
            RefreshToken = refreshToken;
        }

        private string AccountName { get; }
        private string RefreshToken { get; }

        internal static SavedSteamCredentials FromLogin(
            string accountName,
            string refreshToken
        )
            => new(accountName, refreshToken);

        internal Task<ManualCloudSyncResult> RunManualSyncAsync(
            Func<
                string,
                string,
                CancellationToken,
                Task<ManualCloudSyncResult>
            > sync,
            CancellationToken cancellationToken
        )
            => sync(AccountName, RefreshToken, cancellationToken);

        internal Task<AutomaticSyncResult> RunAutomaticSyncAsync(
            Func<
                string,
                string,
                CancellationToken,
                Task<AutomaticSyncResult>
            > sync,
            CancellationToken cancellationToken
        )
            => sync(AccountName, RefreshToken, cancellationToken);
    }

    internal static bool SaveCredentials(SteamCredentialStore credentialStore)
    {
        var saved = false;
        credentialStore.TryUseCredentials(
            (accountName, refreshToken) =>
            {
                saved = SaveCredentials(accountName, refreshToken);
            }
        );
        if (!saved)
            _savedCredentials = null;

        PatchHelper.Log(saved
            ? "[Cloud] Saved Steam credentials available for cloud sync"
            : "[Cloud] Saved Steam credentials unavailable for cloud sync"
        );
        return saved;
    }

    internal static void ClearCredentials()
    {
        _savedCredentials = null;
    }

    private static bool SaveCredentials(string accountName, string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(accountName))
            return false;

        _savedCredentials = SavedSteamCredentials.FromLogin(accountName, refreshToken);
        return true;
    }
}
