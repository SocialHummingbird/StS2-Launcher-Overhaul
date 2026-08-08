using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSessionCoordinator
{
    private const string FirstLaunchConnectionFailed =
        "Connection failed. Internet required for first launch.";
    private const string OfflineLaunchAllowed =
        "Connection timed out. Valid ownership marker found.";
    private const string SavedCredentialsFallback =
        "No connection - saved credentials will be used";
    private const int SteamConnectionTimeoutMs = 30_000;

    private void StartConnectionTimeout(int sessionAttemptId)
        => _ = RunConnectionTimeoutAsync(sessionAttemptId);

    private async Task RunConnectionTimeoutAsync(int sessionAttemptId)
    {
        await Task.Delay(SteamConnectionTimeoutMs);

        if (_model.ShouldIgnoreConnectionTimeout(sessionAttemptId))
            return;

        var outcome = CreateConnectionTimeoutOutcome();
        _runOnMainThread(() => outcome.Apply(_view, _launch));
    }

    private ConnectionTimeoutOutcome CreateConnectionTimeoutOutcome()
    {
        var readiness = EvaluateOfflineLaunchReadiness();
        return readiness?.Ready == true
            ? ConnectionTimeoutOutcome.OfflineLaunch(readiness)
            : ConnectionTimeoutOutcome.FirstLaunchFailure();
    }

    private LauncherLaunchReadiness EvaluateOfflineLaunchReadiness()
        => _model.HasOwnershipMarker()
            ? LauncherLaunchReadiness.EvaluateDownloadedState(
                _model.DataDir,
                LauncherPreferences.ReadGameBranch(),
                "connection timeout offline downloaded-state readiness"
            )
            : null;

    private readonly struct ConnectionTimeoutOutcome
    {
        private readonly string _status;
        private readonly string _logMessage;
        private readonly LaunchUpdateAction? _launchAction;
        private readonly LauncherLaunchReadiness _readiness;
        private readonly bool _showRetry;

        private ConnectionTimeoutOutcome(
            string status,
            string logMessage = null,
            LaunchUpdateAction? launchAction = null,
            LauncherLaunchReadiness readiness = null,
            bool showRetry = false
        )
        {
            _status = status;
            _logMessage = logMessage;
            _launchAction = launchAction;
            _readiness = readiness;
            _showRetry = showRetry;
        }

        internal static ConnectionTimeoutOutcome OfflineLaunch(LauncherLaunchReadiness readiness)
            => new(
                SavedCredentialsFallback,
                OfflineLaunchAllowed,
                LaunchUpdateAction.Hidden,
                readiness
            );

        internal static ConnectionTimeoutOutcome FirstLaunchFailure()
            => new(FirstLaunchConnectionFailed, showRetry: true);

        internal void Apply(LauncherView view, LauncherLaunchCoordinator launch)
        {
            if (_readiness != null && _launchAction.HasValue)
            {
                launch.ShowReadyToLaunch(
                    launch.SelectedVersionReadyStatus(_status, _readiness),
                    _launchAction.Value
                );
            }
            else
            {
                view.SetStatus(_status);
            }

            if (_logMessage != null)
                view.AppendLog(_logMessage);

            if (_launchAction.HasValue && _readiness == null)
                launch.ShowLaunchActions(_launchAction.Value);

            if (_showRetry)
                view.ShowRetry();
        }
    }
}
