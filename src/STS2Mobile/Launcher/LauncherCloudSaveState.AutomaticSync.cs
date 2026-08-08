using System;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherCloudSaveState
{
    internal static Task<AutomaticSyncResult> RecoverAutomaticSyncAsync(
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken
    )
        => RunAutomaticSyncAsync(
            (accountName, refreshToken, token) =>
                CloudSyncCoordinator.RecoverAutomaticSyncAsync(
                    accountName,
                    refreshToken,
                    progress,
                    token
                ),
            cancellationToken
        );

    internal static Task<AutomaticSyncResult> ReconcileAutomaticSyncAsync(
        SaveNamespace saveNamespace,
        string runtimeIdentity,
        string modSetFingerprint,
        AutomaticSyncSourceChoice? sourceChoice,
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken
    )
        => RunAutomaticSyncAsync(
            (accountName, refreshToken, token) =>
                CloudSyncCoordinator.ReconcileAutomaticSyncAsync(
                    accountName,
                    refreshToken,
                    saveNamespace,
                    runtimeIdentity,
                    modSetFingerprint,
                    sourceChoice,
                    progress,
                    token
                ),
            cancellationToken
        );

    internal static Task<AutomaticSyncResult> BeginAutomaticGameSessionAsync(
        SaveNamespace saveNamespace,
        string runtimeIdentity,
        string modSetFingerprint,
        CloudOperationProgressTracker progress,
        CancellationToken cancellationToken
    )
        => RunAutomaticSyncAsync(
            (accountName, refreshToken, token) =>
                CloudSyncCoordinator.BeginAutomaticGameSessionAsync(
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

    private static Task<AutomaticSyncResult> RunAutomaticSyncAsync(
        Func<
            string,
            string,
            CancellationToken,
            Task<AutomaticSyncResult>
        > sync,
        CancellationToken cancellationToken
    )
        => RequireSavedCredentials().RunAutomaticSyncAsync(
            sync,
            cancellationToken
        );
}
