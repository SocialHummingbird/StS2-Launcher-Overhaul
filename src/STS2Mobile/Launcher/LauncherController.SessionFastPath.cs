using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void HandleFastPath(LauncherModel.FastPathResult result)
    {
        switch (result)
        {
            case LauncherModel.FastPathResult.ReadyToLaunch:
                ShowReadyToLaunch(
                    _model.WelcomeBackStatus(),
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
        => _ = RunAutoConnectAsync();

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
            LoginFormFailure.AutoConnect().Show(this, ex);
        }
    }
}
