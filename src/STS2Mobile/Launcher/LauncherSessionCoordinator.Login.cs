using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSessionCoordinator
{
    internal void LoginPressed(string username, string password)
    {
        if (_manualLoginInProgress)
            return;

        LauncherLaunchMarkers.RecordPhase("steam login requested", "Manual login button pressed");
        _manualLoginInProgress = true;
        StartObservedLauncherTask(
            "Manual Steam login",
            () => LoginAsync(username, password),
            ex =>
            {
                _manualLoginInProgress = false;
                LoginFormFailure.LoginHandler().Show(_view, ex);
            }
        );
    }

    internal void CodeSubmitPressed(string code)
    {
        _view.SetStatus("Verifying code...");
        _model.SubmitCode(code);
    }

    private async Task LoginAsync(string username, string password)
    {
        try
        {
            LauncherLaunchMarkers.RecordPhase("steam login running", "Manual Steam authentication started");
            _view.ClearLoginPasswordAndDisable();
            await _model.LoginAsync(username, password);
            LauncherLaunchMarkers.RecordPhase("steam login completed");
        }
        catch (Exception ex)
        {
            LauncherLaunchMarkers.RecordPhase("steam login failed", ex.GetBaseException().Message);
            LoginFormFailure.LoginHandler().Show(_view, ex);
        }
        finally
        {
            _manualLoginInProgress = false;
        }
    }

    internal void ShowCodePrompt(bool wasIncorrect)
    {
        SetLoginFormVisible(false, disabled: true);
        _view.ShowCodePrompt(wasIncorrect);
    }

    private void ShowLogin()
        => ShowLoginForm("Enter your Steam credentials");

    private void ShowLoginForm(string status)
    {
        _view.SetStatus(status);
        SetLoginFormVisible(true, disabled: false);
    }

    private void SetLoginFormVisible(bool visible, bool disabled)
        => _view.SetLoginFormVisible(visible, disabled);
}
