using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherLaunchCoordinator
{
    private readonly LauncherModel _model;
    private readonly LauncherView _view;
    private readonly LauncherCloudSyncCoordinator _cloud;
    private readonly Action<Action> _runOnMainThread;
    private readonly LauncherDiagnosticsCoordinator _diagnostics;
    private readonly Action<LocalBackupRefreshResult>
        _localBackupRecoveryCompleted;
    private bool _launchInProgress;
    private LaunchAttemptContext _activeLaunchAttempt;

    internal LauncherLaunchCoordinator(
        LauncherModel model,
        LauncherView view,
        LauncherCloudSyncCoordinator cloud,
        Action<Action> runOnMainThread,
        LauncherDiagnosticsCoordinator diagnostics,
        Action<LocalBackupRefreshResult>
            localBackupRecoveryCompleted
    )
    {
        _model = model;
        _view = view;
        _cloud = cloud ?? throw new ArgumentNullException(nameof(cloud));
        _runOnMainThread = runOnMainThread
            ?? throw new ArgumentNullException(nameof(runOnMainThread));
        _diagnostics = diagnostics;
        _localBackupRecoveryCompleted =
            localBackupRecoveryCompleted
            ?? throw new ArgumentNullException(
                nameof(localBackupRecoveryCompleted)
            );
    }

    internal void LaunchPressed()
        => StartGame(LauncherStartGamePlan.Normal());

    internal void SafeLaunchPressed()
        => StartGame(LauncherStartGamePlan.Safe());

    internal void AutoLaunchRequested(bool safeLaunch)
        => StartGame(
            safeLaunch
                ? LauncherStartGamePlan.AutoSafe()
                : LauncherStartGamePlan.AutoNormal()
        );

    internal void AutomationLaunchRequested(bool safeLaunch)
        => StartGame(
            safeLaunch
                ? LauncherStartGamePlan.AutomationSafe()
                : LauncherStartGamePlan.AutomationNormal()
        );
}
