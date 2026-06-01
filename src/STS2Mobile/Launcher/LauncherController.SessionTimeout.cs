using System.Threading.Tasks;
using SessionState = STS2Mobile.Launcher.LauncherModel.SessionState;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private const string FirstLaunchConnectionFailed =
        "Connection failed. Internet required for first launch.";
    private const string OfflineLaunchAllowed =
        "Connection timed out. Valid ownership marker found.";
    private const string SavedCredentialsFallback =
        "No connection - saved credentials will be used";
    private const int SteamConnectionTimeoutMs = 10_000;

    private void StartConnectionTimeout()
        => _ = RunConnectionTimeoutAsync();

    private async Task RunConnectionTimeoutAsync()
    {
        await Task.Delay(SteamConnectionTimeoutMs);

        if (ShouldIgnoreConnectionTimeout())
            return;

        if (CanUseOfflineLaunch())
        {
            _runOnMainThread(ShowOfflineLaunch);
            return;
        }

        _runOnMainThread(ShowFirstLaunchFailure);
    }

    private bool ShouldIgnoreConnectionTimeout()
    {
        if (_model.ConnectionResolved)
            return true;

        return _model.CurrentSessionState
            is not (
                SessionState.Connecting
                or SessionState.Authenticating
                or SessionState.VerifyingOwnership
            );
    }

    private bool CanUseOfflineLaunch()
        => _model.HasOwnershipMarker() && LauncherGameFiles.Ready();

    private void ShowOfflineLaunch()
    {
        _view.SetStatus(SavedCredentialsFallback);
        _view.AppendLog(OfflineLaunchAllowed);
        ShowLaunchActions(showUpdate: false);
    }

    private void ShowFirstLaunchFailure()
    {
        _view.SetStatus(FirstLaunchConnectionFailed);
        _view.Actions.ShowRetry();
    }
}
