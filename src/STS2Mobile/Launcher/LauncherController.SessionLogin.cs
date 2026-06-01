using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

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
            _view.Login.SetDisabled(true);
            _view.Login.ClearPassword();
            await _model.LoginAsync(username, password);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Login handler failed: {ex.Message}");
            ShowLoginForm($"Login failed: {ex.Message}");
        }
    }

    private void ShowCodePrompt(bool wasIncorrect)
    {
        SetLoginFormVisible(false, disabled: true);
        _view.Code.Show(wasIncorrect);
    }

    private void ShowLogin()
        => ShowLoginForm("Enter your Steam credentials");

    private void ShowLoginForm(string status)
    {
        _view.SetStatus(status);
        SetLoginFormVisible(true, disabled: false);
    }

    private void SetLoginFormVisible(bool visible, bool disabled)
    {
        _view.Login.Visible = visible;
        _view.Login.SetDisabled(disabled);
    }
}
