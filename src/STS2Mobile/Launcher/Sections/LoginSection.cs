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
    private readonly Button _nativeLoginButton;
    private readonly Button _loginButton;
    private readonly Timer _nativeCredentialPollTimer;
    private int _nativeCredentialPollsRemaining;

    internal LoginSection(float scale, bool compact = false)
    {
        LauncherSectionSetup.ConfigureHiddenSection(
            this,
            scale,
            "Steam Sign-in",
            "Use Steam once, then launch from cached encrypted session data when possible.",
            LauncherComponentTheme.OrangeAccent,
            compact
        );
        var useNativeAndroidCredentialPanel = OperatingSystem.IsAndroid();

        _usernameField = new StyledLineEdit(
            "Steam Username",
            scale,
            keyboardType: DisplayServer.VirtualKeyboardType.EmailAddress
        );
        _usernameField.Visible = !useNativeAndroidCredentialPanel;
        LauncherCredentialEntrySupport.ConfigureUsernameField(_usernameField);
        AddChild(_usernameField);

        _passwordField = new StyledLineEdit(
            "Password",
            scale,
            secret: true,
            keyboardType: DisplayServer.VirtualKeyboardType.Password
        );
        _passwordField.Visible = !useNativeAndroidCredentialPanel;
        LauncherCredentialEntrySupport.ConfigurePasswordField(_passwordField);
        AddChild(_passwordField);

        AddChild(CreateCredentialHelpLabel(scale, compact));

        _nativeLoginButton = new StyledButton(
            "SIGN IN WITH STEAM",
            scale,
            fontSize: LauncherSectionMetrics.SecondaryButtonFontSize,
            height: LauncherSectionMetrics.PrimaryButtonHeight
        );
        LauncherButtonStyles.ApplyPrimaryAction(_nativeLoginButton, scale);
        _nativeLoginButton.Visible = useNativeAndroidCredentialPanel;
        _nativeLoginButton.Disabled = !useNativeAndroidCredentialPanel;
        _nativeLoginButton.Pressed += OnNativeLoginPressed;
        AddChild(_nativeLoginButton);

        _loginButton = new StyledButton("SIGN IN", scale);
        LauncherButtonStyles.ApplyPrimaryAction(_loginButton, scale);
        _loginButton.Visible = !useNativeAndroidCredentialPanel;
        AddChild(_loginButton);

        _nativeCredentialPollTimer = new Timer
        {
            WaitTime = 0.25,
            OneShot = false,
            Autostart = false,
        };
        _nativeCredentialPollTimer.Timeout += PollNativeCredentialResult;
        AddChild(_nativeCredentialPollTimer);

        _usernameField.TextSubmitted += _ => _passwordField.GrabFocus();
        _passwordField.TextSubmitted += _ => OnLoginPressed();
        _loginButton.Pressed += OnLoginPressed;
    }

    internal void SetDisabled(bool disabled)
    {
        _loginButton.Disabled = disabled;
        _nativeLoginButton.Disabled = disabled || !OperatingSystem.IsAndroid();
        if (disabled)
            StopNativeCredentialPolling(hidePanel: true);
    }

    internal void ClearPassword()
    {
        _passwordField.Text = "";
        StopNativeCredentialPolling(hidePanel: true);
    }

    internal void SetFormVisible(bool visible, bool disabled)
    {
        Visible = visible;
        SetDisabled(disabled);
        if (visible && !disabled && OperatingSystem.IsAndroid())
            OpenNativeCredentialPanel();
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

    private void OnNativeLoginPressed()
    {
        if (!OperatingSystem.IsAndroid())
            return;

        OpenNativeCredentialPanel();
    }

    private void OpenNativeCredentialPanel()
    {
        try
        {
            _nativeLoginButton.Disabled = true;
            _nativeCredentialPollsRemaining = 720;
            AndroidGodotAppBridge.ShowSteamLoginCredentialPanel();
            _nativeCredentialPollTimer.Start();
        }
        catch (Exception ex)
        {
            _nativeLoginButton.Disabled = false;
            PatchHelper.Log($"[Launcher] Could not open native Steam login panel: {ex.Message}");
        }
    }

    private void PollNativeCredentialResult()
    {
        if (--_nativeCredentialPollsRemaining <= 0)
        {
            StopNativeCredentialPolling(hidePanel: false);
            return;
        }

        try
        {
            if (!AndroidGodotAppBridge.TryConsumeSteamLoginCredentialResult(out var username, out var password))
            {
                if (!AndroidGodotAppBridge.IsSteamLoginCredentialPanelVisible())
                    StopNativeCredentialPolling(hidePanel: false);

                return;
            }

            StopNativeCredentialPolling(hidePanel: true);
            _nativeLoginButton.Disabled = true;
            LoginRequested?.Invoke(username, password);
        }
        catch (Exception ex)
        {
            StopNativeCredentialPolling(hidePanel: true);
            _nativeLoginButton.Disabled = false;
            PatchHelper.Log($"[Launcher] Native Steam login panel result failed: {ex.Message}");
        }
    }

    private void StopNativeCredentialPolling(bool hidePanel)
    {
        _nativeCredentialPollTimer.Stop();
        _nativeLoginButton.Disabled = !OperatingSystem.IsAndroid();
        if (hidePanel && OperatingSystem.IsAndroid())
        {
            try
            {
                AndroidGodotAppBridge.HideSteamLoginCredentialPanel();
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Launcher] Could not hide native Steam login panel: {ex.Message}");
            }
        }
    }

    private static Label CreateCredentialHelpLabel(float scale, bool compact)
        => new()
        {
            Text = OperatingSystem.IsAndroid()
                ? (compact
                    ? "Android/Samsung/Google password suggestions may appear in the Steam sign-in panel. Passwords are not stored."
                    : "Use the integrated Steam login panel. Android/Samsung/Google password suggestions may appear there; StS2 Mobile does not store your Steam password.")
                : "Use the visible Steam fields above. StS2 Mobile does not store your Steam password.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Left,
            CustomMinimumSize = new Vector2(0, compact ? 26f * scale : 34f * scale),
        };
}
