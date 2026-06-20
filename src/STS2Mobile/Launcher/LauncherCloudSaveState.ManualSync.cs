using System;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherCloudSaveState
{
    internal static Task<string> ManualPushAllAsync()
        => RunManualSyncAsync(CloudSyncCoordinator.ManualPushAllAsync);

    internal static Task<string> ManualPullAllAsync()
        => RunManualSyncAsync(CloudSyncCoordinator.ManualPullAllAsync);

    private static Task<string> RunManualSyncAsync(Func<string, string, Task<string>> sync)
        => RequireSavedCredentials().RunManualSyncAsync(sync);

    private static SavedSteamCredentials RequireSavedCredentials()
    {
        var credentials = _savedCredentials;
        if (!credentials.HasValue)
            throw new InvalidOperationException("No saved Steam credentials. Log in again before pulling cloud saves.");

        return credentials.Value;
    }
}
