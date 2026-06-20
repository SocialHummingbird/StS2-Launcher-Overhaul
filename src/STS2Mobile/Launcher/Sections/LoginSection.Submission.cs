using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class LoginSection
{
    private void OnLoginPressed()
    {
        var username = _usernameField.Text.Trim();
        var password = _passwordField.Text;
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            return;

        _passwordField.Text = "";

        try
        {
            LoginRequested?.Invoke(username, password);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"[Launcher] Login request handler failed before authentication: {ex}"
            );
        }
    }
}
