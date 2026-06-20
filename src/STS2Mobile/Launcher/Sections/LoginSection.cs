using System;
using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher.Sections;

internal sealed class LoginSection : VBoxContainer
{
    private const int CompactNativeLoginButtonHeight = LauncherSectionMetrics.CodeInputHeight;
    private const string CompactNativeLoginBodyName = "CompactNativeLoginBody";
    private const string CompactNativeLoginTitleName = "CompactNativeLoginTitle";
    private const string CompactNativeLoginDetailName = "CompactNativeLoginDetail";
    private const int CompactNativeLoginTitleFontSize = LauncherSectionMetrics.CompactDetailButtonFontSize;
    private const int CompactNativeLoginDetailFontSize = LauncherSectionMetrics.CompactDetailLabelFontSize;
    private const int CompactNativeLoginHorizontalMargin = 6;
    private const int CompactNativeLoginVerticalMargin = 4;

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

        var credentialHelpLabel = CreateCredentialHelpLabel(scale, compact);
        AddChild(credentialHelpLabel);

        var compactNativeLogin = compact && useNativeAndroidCredentialPanel;
        _nativeLoginButton = new StyledButton(
            compactNativeLogin ? CompactNativeLoginText() : "SIGN IN WITH STEAM",
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

    private static string CompactNativeLoginText()
        => "SIGN IN WITH STEAM\nAndroid login";

    private static void SetCompactNativeLoginButtonText(
        Button button,
        string text,
        float scale,
        bool compactNativeLogin
    )
    {
        if (!compactNativeLogin || !TrySplitCompactNativeLoginText(text, out var title, out var detail))
        {
            HideCompactNativeLoginButtonLabels(button);
            button.Text = text;
            return;
        }

        var labels = EnsureCompactNativeLoginButtonLabels(button, scale);
        button.Text = "";
        labels.Body.Visible = true;
        labels.Title.Text = title;
        labels.Detail.Text = detail;
    }

    private static bool TrySplitCompactNativeLoginText(
        string text,
        out string title,
        out string detail
    )
    {
        title = text ?? "";
        detail = "";
        var separator = title.IndexOf('\n');
        if (separator < 0)
            return false;

        detail = title[(separator + 1)..].Trim();
        title = title[..separator].Trim();
        return title.Length > 0 && detail.Length > 0;
    }

    private static void HideCompactNativeLoginButtonLabels(Button button)
    {
        var body = button.GetNodeOrNull<VBoxContainer>(new NodePath(CompactNativeLoginBodyName));
        if (body != null)
            body.Visible = false;
    }

    private static (VBoxContainer Body, StyledLabel Title, StyledLabel Detail) EnsureCompactNativeLoginButtonLabels(
        Button button,
        float scale
    )
    {
        var body = button.GetNodeOrNull<VBoxContainer>(new NodePath(CompactNativeLoginBodyName));
        if (body != null)
        {
            return (
                body,
                body.GetNode<StyledLabel>(new NodePath(CompactNativeLoginTitleName)),
                body.GetNode<StyledLabel>(new NodePath(CompactNativeLoginDetailName))
            );
        }

        body = new VBoxContainer
        {
            Name = CompactNativeLoginBodyName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        body.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        body.OffsetLeft = LauncherViewLayoutMetrics.ScaleInt(CompactNativeLoginHorizontalMargin, scale);
        body.OffsetRight = -LauncherViewLayoutMetrics.ScaleInt(CompactNativeLoginHorizontalMargin, scale);
        body.OffsetTop = LauncherViewLayoutMetrics.ScaleInt(CompactNativeLoginVerticalMargin, scale);
        body.OffsetBottom = -LauncherViewLayoutMetrics.ScaleInt(CompactNativeLoginVerticalMargin, scale);
        body.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, 0);

        var title = new StyledLabel(
            "",
            scale,
            fontSize: CompactNativeLoginTitleFontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = CompactNativeLoginTitleName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        title.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextPrimary
        );
        body.AddChild(title);

        var detail = new StyledLabel(
            "",
            scale,
            fontSize: CompactNativeLoginDetailFontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = CompactNativeLoginDetailName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        detail.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        body.AddChild(detail);

        button.AddChild(body);
        return (body, title, detail);
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
    {
        var compactAndroid = compact && OperatingSystem.IsAndroid();
        var label = new StyledLabel(
            CredentialHelpText(compact),
            scale,
            fontSize: LauncherSectionMetrics.ProgressFontSize,
            align: HorizontalAlignment.Left
        );
        label.AutowrapMode = compactAndroid
            ? TextServer.AutowrapMode.WordSmart
            : TextServer.AutowrapMode.WordSmart;
        label.ClipText = false;
        label.VerticalAlignment = VerticalAlignment.Center;
        label.CustomMinimumSize = new Vector2(
            0,
            compactAndroid
                ? LauncherViewLayoutMetrics.ScaleInt(LauncherSectionMetrics.CompactCredentialHelpHeight, scale)
                : (compact ? 30f * scale : 38f * scale)
        );
        label.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        return label;
    }

    private static string CredentialHelpText(bool compact)
    {
        if (!OperatingSystem.IsAndroid())
            return "Use the visible Steam fields above. StS2 Mobile does not store your Steam password.";

        return compact
            ? "Password manager can appear.\nSteam password is not stored."
            : "Use the integrated Steam login panel. Android/Samsung/Google password suggestions may appear there; StS2 Mobile does not store your Steam password.";
    }
}
