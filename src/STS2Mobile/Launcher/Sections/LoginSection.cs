using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class LoginSection : VBoxContainer
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
            compact,
            "Steam account"
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

        var credentialHelpLabel = CreateCredentialHelpLabel(scale, compact);
        AddChild(credentialHelpLabel);

        var compactNativeLogin = compact && useNativeAndroidCredentialPanel;
        _nativeLoginButton = new StyledButton(
            compactNativeLogin ? CompactNativeLoginText() : "Sign in with Steam",
            scale,
            fontSize: compactNativeLogin
                ? LauncherSectionMetrics.PrimaryButtonFontSize
                : LauncherSectionMetrics.SecondaryButtonFontSize,
            height: compactNativeLogin
                ? CompactNativeLoginButtonHeight
                : LauncherSectionMetrics.PrimaryButtonHeight
        );
        LauncherButtonStyles.ApplyPrimaryAction(_nativeLoginButton, scale);
        SetCompactNativeLoginButtonText(_nativeLoginButton, _nativeLoginButton.Text, scale, compactNativeLogin);
        _nativeLoginButton.Visible = useNativeAndroidCredentialPanel;
        _nativeLoginButton.Disabled = !useNativeAndroidCredentialPanel;
        _nativeLoginButton.Pressed += OnNativeLoginPressed;
        AddChild(_nativeLoginButton);
        if (compact && useNativeAndroidCredentialPanel)
            MoveChild(_nativeLoginButton, credentialHelpLabel.GetIndex());

        _loginButton = new StyledButton("Sign in", scale);
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
}
