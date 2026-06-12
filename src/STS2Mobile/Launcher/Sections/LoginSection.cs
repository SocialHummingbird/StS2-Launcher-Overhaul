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
    private readonly Button _autofillButton;
    private readonly Button _loginButton;
    private readonly Timer _autofillPollTimer;
    private int _autofillPollsRemaining;

    internal LoginSection(float scale)
    {
        LauncherSectionSetup.ConfigureHiddenSection(this, scale);

        _usernameField = new StyledLineEdit("Steam Username", scale);
        LauncherAutofillSupport.ConfigureUsernameField(_usernameField);
        AddChild(_usernameField);

        _passwordField = new StyledLineEdit("Password", scale, secret: true);
        LauncherAutofillSupport.ConfigurePasswordField(_passwordField);
        AddChild(_passwordField);

        _autofillButton = new StyledButton(
            "USE ANDROID AUTOFILL",
            scale,
            fontSize: LauncherSectionMetrics.SecondaryButtonFontSize,
            height: LauncherSectionMetrics.SecondaryButtonHeight
        );
        _autofillButton.Visible = OperatingSystem.IsAndroid();
        _autofillButton.Disabled = !OperatingSystem.IsAndroid();
        _autofillButton.Pressed += OnAutofillPressed;
        AddChild(_autofillButton);

        _loginButton = new StyledButton("LOGIN", scale);
        AddChild(_loginButton);

        _autofillPollTimer = new Timer
        {
            WaitTime = 0.5,
            OneShot = false,
            Autostart = false,
        };
        _autofillPollTimer.Timeout += PollAutofillResult;
        AddChild(_autofillPollTimer);

        _usernameField.TextSubmitted += _ => _passwordField.GrabFocus();
        _passwordField.TextSubmitted += _ => OnLoginPressed();
        _loginButton.Pressed += OnLoginPressed;
    }

    internal void SetDisabled(bool disabled)
    {
        _loginButton.Disabled = disabled;
        _autofillButton.Disabled = disabled || !OperatingSystem.IsAndroid();
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

    private void OnAutofillPressed()
    {
        if (!OperatingSystem.IsAndroid())
            return;

        try
        {
            _autofillButton.Disabled = true;
            _autofillPollsRemaining = 120;
            AndroidGodotAppBridge.ShowSteamLoginAutofillDialog();
            _autofillPollTimer.Start();
        }
        catch (Exception ex)
        {
            _autofillButton.Disabled = false;
            PatchHelper.Log($"[Launcher] Could not open Android Autofill login dialog: {ex.Message}");
        }
    }

    private void PollAutofillResult()
    {
        if (--_autofillPollsRemaining <= 0)
        {
            StopAutofillPolling();
            return;
        }

        try
        {
            if (!AndroidGodotAppBridge.TryConsumeSteamLoginAutofillResult(out var username, out var password))
                return;

            StopAutofillPolling();
            _usernameField.Text = username;
            _passwordField.Text = password;
            OnLoginPressed();
        }
        catch (Exception ex)
        {
            StopAutofillPolling();
            PatchHelper.Log($"[Launcher] Android Autofill login result failed: {ex.Message}");
        }
    }

    private void StopAutofillPolling()
    {
        _autofillPollTimer.Stop();
        _autofillButton.Disabled = !OperatingSystem.IsAndroid();
    }
}
