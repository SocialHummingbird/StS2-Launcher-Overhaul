using System;
using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher.Sections;

internal sealed class LoginSection : VBoxContainer
{
    internal event Action<string, string> LoginRequested;

    private readonly LineEdit _usernameField;
    private readonly LineEdit _passwordField;
    private readonly Button _loginButton;

    internal LoginSection(float scale)
    {
        LauncherSectionSetup.ConfigureHiddenSection(this, scale);

        _usernameField = new StyledLineEdit("Steam Username", scale);
        AddChild(_usernameField);

        _passwordField = new StyledLineEdit("Password", scale, secret: true);
        AddChild(_passwordField);

        _loginButton = new StyledButton("LOGIN", scale);
        AddChild(_loginButton);

        _usernameField.TextSubmitted += _ => _passwordField.GrabFocus();
        _passwordField.TextSubmitted += _ => OnLoginPressed();
        _loginButton.Pressed += OnLoginPressed;
    }

    internal void SetDisabled(bool disabled)
    {
        _loginButton.Disabled = disabled;
    }

    internal void ClearPassword()
    {
        _passwordField.Text = "";
    }

    private void OnLoginPressed()
    {
        var username = _usernameField.Text.Trim();
        var password = _passwordField.Text;
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            return;

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
