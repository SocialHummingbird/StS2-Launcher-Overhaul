using System;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Patches;
using SessionState = STS2Mobile.Launcher.LauncherModel.SessionState;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSessionCoordinator
{
    private const int LocalLoginPollDelayMs = 500;
    private static readonly TimeSpan LocalLoginPollTimeout = TimeSpan.FromSeconds(180);

    private readonly LauncherModel _model;
    private readonly LauncherView _view;
    private readonly LauncherLaunchCoordinator _launch;
    private readonly LauncherDownloadCoordinator _downloads;
    private readonly Action<Action> _runOnMainThread;
    private readonly Func<bool> _isUpdateCheckRunning;

    private bool _manualLoginInProgress;
    private int _localLoginHandoffStarted;

    internal LauncherSessionCoordinator(
        LauncherModel model,
        LauncherView view,
        LauncherLaunchCoordinator launch,
        LauncherDownloadCoordinator downloads,
        Action<Action> runOnMainThread,
        Func<bool> isUpdateCheckRunning
    )
    {
        _model = model;
        _view = view;
        _launch = launch;
        _downloads = downloads;
        _runOnMainThread = runOnMainThread;
        _isUpdateCheckRunning = isUpdateCheckRunning;
    }

    internal void StartSessionFlow()
    {
        var result = _model.StartSession();
        HandleSessionFlow(result);
    }

    internal void RetryPressed()
    {
        var result = _model.Retry();
        HandleSessionFlow(result);
    }

    internal void LocalBackupToggled(bool pressed)
        => LauncherPreferences.SaveLocalBackupEnabled(pressed);

    private void HandleSessionFlow(LauncherModel.FastPathResult result)
    {
        if (
            result != LauncherModel.FastPathResult.ReadyToLaunch
            && TryStartImmediateLocalLoginHandoff()
        )
            return;

        HandleFastPath(result);
        if (result != LauncherModel.FastPathResult.ReadyToLaunch)
            StartLocalLoginHandoff();
    }

    private void StartObservedLauncherTask(
        string taskName,
        Func<Task> taskFactory,
        Action<Exception> handleEscapedException
    )
    {
        try
        {
            _ = ObserveLauncherTaskAsync(
                taskName,
                taskFactory(),
                handleEscapedException
            );
        }
        catch (Exception ex)
        {
            HandleEscapedLauncherTaskException(
                taskName,
                handleEscapedException,
                ex
            );
        }
    }

    private async Task ObserveLauncherTaskAsync(
        string taskName,
        Task task,
        Action<Exception> handleEscapedException
    )
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            HandleEscapedLauncherTaskException(
                taskName,
                handleEscapedException,
                ex
            );
        }
    }

    private void HandleEscapedLauncherTaskException(
        string taskName,
        Action<Exception> handleEscapedException,
        Exception ex
    )
    {
        PatchHelper.Log(
            $"[Launcher] {taskName} failed outside its normal error boundary: {ex}"
        );
        _runOnMainThread(() => handleEscapedException(ex));
    }
}
