namespace STS2Mobile.Launcher;

internal static class LauncherCloudSaveState
{
    private static bool _cloudSyncEnabled = true;
    private static string _savedAccountName;
    private static string _savedRefreshToken;

    internal static string StatusSummary
        => $"HasToken={HasSavedCredentials}, CloudSync={_cloudSyncEnabled}";

    internal static bool TryGetSavedCredentials(
        out string accountName,
        out string refreshToken
    )
    {
        accountName = _savedAccountName;
        refreshToken = _savedRefreshToken;
        return accountName != null && refreshToken != null;
    }

    internal static bool TryGetEnabledCredentials(
        out string accountName,
        out string refreshToken,
        out string unavailableReason
    )
    {
        if (!_cloudSyncEnabled)
        {
            accountName = null;
            refreshToken = null;
            unavailableReason = "[Cloud] Cloud sync disabled by user - using local-only SaveManager";
            return false;
        }

        if (!TryGetSavedCredentials(out accountName, out refreshToken))
        {
            unavailableReason = "[Cloud] No saved credentials - using local-only SaveManager";
            return false;
        }

        unavailableReason = null;
        return true;
    }

    internal static void SetCloudSyncEnabled(bool enabled)
    {
        _cloudSyncEnabled = enabled;
    }

    internal static void DisableCloudSyncForLaunch()
    {
        _cloudSyncEnabled = false;
    }

    internal static void SaveCredentials(string accountName, string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(accountName))
            return;

        _savedAccountName = accountName;
        _savedRefreshToken = refreshToken;
    }

    private static bool HasSavedCredentials
        => _savedAccountName != null && _savedRefreshToken != null;
}

