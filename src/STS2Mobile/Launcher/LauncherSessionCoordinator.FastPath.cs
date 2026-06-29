using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSessionCoordinator
{
    private void HandleFastPath(LauncherModel.FastPathResult result)
    {
        switch (result)
        {
            case LauncherModel.FastPathResult.ReadyToLaunch:
                _launch.ShowReadyToLaunch(
                    _launch.SelectedVersionReadyStatus(_model.WelcomeBackStatus()),
                    LaunchUpdateAction.Visible
                );
                break;

            case LauncherModel.FastPathResult.AutoConnect:
                StartAutoConnect();
                break;

            case LauncherModel.FastPathResult.ShowLogin:
                ShowLogin();
                break;
        }
    }

    private void StartAutoConnect()
        => StartObservedLauncherTask(
            "Saved Steam credential auto-connect",
            RunAutoConnectAsync,
            ex => LoginFormFailure.AutoConnect().Show(_view, ex)
        );

    private async Task RunAutoConnectAsync()
    {
        var connectTask = _model.ConnectAsync();
        _model.StartConnectionTimeout(StartConnectionTimeout);

        try
        {
            await connectTask;
        }
        catch (Exception ex)
        {
            LoginFormFailure.AutoConnect().Show(_view, ex);
        }
    }
}
