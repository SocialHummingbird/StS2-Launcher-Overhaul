using System;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherCloudSaveState
{
    internal static Task<ManualCloudSyncResult> ManualPushAllAsync()
        => ManualPushAllAsync(CancellationToken.None);

    internal static Task<ManualCloudSyncResult> ManualPushAllAsync(
        CancellationToken cancellationToken
    )
        => ManualPushAllAsync(
            new CloudOperationProgressTracker(CloudOperationKind.Push),
            cancellationToken
        );

    internal static Task<ManualCloudSyncResult> ManualPushAllAsync(
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken
    )
        => ManualPushAllAsync(
            CurrentSaveNamespace(),
            SteamGameBranch.StorageIdentity(
                LauncherPreferences.ReadGameBranch()
            ),
            CurrentModSetFingerprint(),
            progress,
            cancellationToken
        );

    internal static Task<ManualCloudSyncResult> ManualPushAllAsync(
        SaveNamespace saveNamespace,
        string runtimeIdentity,
        string modSetFingerprint,
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken
    )
        => RunManualSyncAsync(
            (accountName, refreshToken, token) =>
                CloudSyncCoordinator.ManualPushAllAsync(
                    accountName,
                    refreshToken,
                    saveNamespace,
                    runtimeIdentity,
                    modSetFingerprint,
                    progress,
                    token
                ),
            cancellationToken
        );

    internal static Task<ManualCloudSyncResult> ManualPullAllAsync()
        => ManualPullAllAsync(CancellationToken.None);

    internal static Task<ManualCloudSyncResult> ManualPullAllAsync(
        CancellationToken cancellationToken
    )
        => ManualPullAllAsync(
            new CloudOperationProgressTracker(CloudOperationKind.Pull),
            cancellationToken
        );

    internal static Task<ManualCloudSyncResult> ManualPullAllAsync(
        CloudOperationProgressTracker progress
    )
        => ManualPullAllAsync(progress, CancellationToken.None);

    internal static Task<ManualCloudSyncResult> ManualPullAllAsync(
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken
    )
        => ManualPullAllAsync(
            CurrentSaveNamespace(),
            SteamGameBranch.StorageIdentity(
                LauncherPreferences.ReadGameBranch()
            ),
            CurrentModSetFingerprint(),
            progress,
            cancellationToken
        );

    internal static Task<ManualCloudSyncResult> ManualPullAllAsync(
        SaveNamespace saveNamespace,
        string runtimeIdentity,
        string modSetFingerprint,
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken
    )
        => RunManualSyncAsync(
            (accountName, refreshToken, token) =>
                CloudSyncCoordinator.ManualPullAllAsync(
                    accountName,
                    refreshToken,
                    saveNamespace,
                    runtimeIdentity,
                    modSetFingerprint,
                    progress,
                    token
                ),
            cancellationToken
        );

    private static Task<ManualCloudSyncResult> RunManualSyncAsync(
        Func<
            string,
            string,
            CancellationToken,
            Task<ManualCloudSyncResult>
        > sync,
        CancellationToken cancellationToken
    )
        => RequireSavedCredentials().RunManualSyncAsync(
            sync,
            cancellationToken
        );

    private static SavedSteamCredentials RequireSavedCredentials()
    {
        var credentials = _savedCredentials;
        if (!credentials.HasValue)
            throw new InvalidOperationException("No saved Steam credentials. Log in again before transferring saves.");

        return credentials.Value;
    }

    private static SaveNamespace CurrentSaveNamespace()
        => LauncherModSelectionState.IsModdedMode
            ? SaveNamespace.Modded
            : SaveNamespace.Vanilla;

    private static string CurrentModSetFingerprint()
        => LauncherModSelectionState.EnabledModSetFingerprint() ?? "";
}
