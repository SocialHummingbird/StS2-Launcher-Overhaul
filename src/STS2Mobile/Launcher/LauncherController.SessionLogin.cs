using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void LoginPressed(string username, string password)
        => _ = LoginAsync(username, password);

    private void CodeSubmitPressed(string code)
    {
        _view.SetStatus("Verifying code...");
        _model.SubmitCode(code);
    }

    private async Task LoginAsync(string username, string password)
    {
        try
        {
            _view.ClearLoginPasswordAndDisable();
            await _model.LoginAsync(username, password);
        }
        catch (Exception ex)
        {
            LoginFormFailure.LoginHandler().Show(this, ex);
        }
    }

    private void ShowCodePrompt(bool wasIncorrect)
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
