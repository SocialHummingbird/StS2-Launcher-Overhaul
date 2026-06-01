using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher.Sections;

internal sealed class ActionSection : VBoxContainer
{
    internal event Action LaunchPressed;
    internal event Action RetryPressed;
    internal event Action<bool> LocalBackupToggled;
    internal event Action<bool> CloudSyncToggled;
    internal event Action CloudPushPressed;
    internal event Action CloudPullPressed;
    internal event Action CheckForUpdatesPressed;
    internal event Action RedownloadPressed;
    internal event Action DiagnosticsPressed;
    internal event Action ShowLastErrorPressed;
    internal event Action CopyRawLogPressed;
    internal event Action SafeLaunchPressed;

    private readonly Button _launchButton;
    private readonly Button _safeLaunchButton;
    private readonly Button _retryButton;
    private readonly Button _localBackupToggle;
    private readonly Button _cloudSyncToggle;
    private readonly Button _pushButton;
    private readonly Button _pullButton;
    private readonly Button _updateButton;
    private readonly Button _redownloadButton;
    private readonly Button _diagnosticsButton;
    private readonly Button _showLastErrorButton;
    private readonly Button _copyRawLogButton;
    private readonly HBoxContainer _pushPullRow;
    private readonly StyleBoxFlat _toggleOffStyle;
    private readonly StyleBoxFlat _toggleOnStyle;

    internal ActionSection(float scale)
    {
        var toggleRadius = (int)(4 * scale);
        var toggleBorderWidth = Math.Max(1, (int)(2 * scale));
        _toggleOffStyle = LauncherStyleBoxes.MakeOutline(
            new Color(0.7f, 0.25f, 0.25f),
            toggleRadius,
            toggleBorderWidth
        );
        _toggleOnStyle = LauncherStyleBoxes.MakeOutline(
            new Color(0.25f, 0.65f, 0.3f),
            toggleRadius,
            toggleBorderWidth
        );

        _retryButton = AddHiddenButton(
            "RETRY",
            scale,
            LauncherSectionMetrics.PrimaryButtonFontSize,
            LauncherSectionMetrics.PrimaryButtonHeight,
            () => RetryPressed?.Invoke()
        );

        _localBackupToggle = AddSecondaryHiddenButton("Local Backup: OFF", scale, null);
        _cloudSyncToggle = AddSecondaryHiddenButton("Auto Sync: OFF", scale, null);
        ConfigureToggle(
            _localBackupToggle,
            LocalBackupText,
            pressed => LocalBackupToggled?.Invoke(pressed)
        );
        ConfigureToggle(
            _cloudSyncToggle,
            CloudSyncText,
            pressed => CloudSyncToggled?.Invoke(pressed)
        );

        _pushPullRow = new HBoxContainer();
        _pushPullRow.Visible = false;
        _pushPullRow.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(LauncherSectionMetrics.PushPullRowSeparation, scale)
        );
        _pushButton = AddPushPullButton(
            _pushPullRow,
            "Push to Cloud",
            scale,
            () => CloudPushPressed?.Invoke()
        );
        _pullButton = AddPushPullButton(
            _pushPullRow,
            "Pull from Cloud",
            scale,
            () => CloudPullPressed?.Invoke()
        );
        AddChild(_pushPullRow);

        _updateButton = AddPrimaryHiddenButton(
            "CHECK FOR UPDATES",
            scale,
            () => CheckForUpdatesPressed?.Invoke()
        );
        _redownloadButton = AddSecondaryHiddenButton(
            "REDOWNLOAD GAME FILES",
            scale,
            () => RedownloadPressed?.Invoke()
        );
        _diagnosticsButton = AddSecondaryHiddenButton(
            "EXPORT DIAGNOSTICS",
            scale,
            () => DiagnosticsPressed?.Invoke()
        );
        _showLastErrorButton = AddSecondaryHiddenButton(
            "SHOW LAST ERROR",
            scale,
            () => ShowLastErrorPressed?.Invoke()
        );
        _copyRawLogButton = AddSecondaryHiddenButton(
            "COPY RAW ERROR LOG",
            scale,
            () => CopyRawLogPressed?.Invoke()
        );
        _safeLaunchButton = AddSecondaryHiddenButton(
            "SAFE LAUNCH",
            scale,
            () => SafeLaunchPressed?.Invoke()
        );
        _launchButton = AddPrimaryHiddenButton(
            "LAUNCH",
            scale,
            () => LaunchPressed?.Invoke()
        );
    }

    internal void SetLocalBackupChecked(bool value)
    {
        SetToggleChecked(_localBackupToggle, value, LocalBackupText);
    }

    internal void SetCloudSyncChecked(bool value)
    {
        SetToggleChecked(_cloudSyncToggle, value, CloudSyncText);
    }

    internal void ShowLaunch(string text, bool showCloudSync, bool showUpdate)
    {
        _launchButton.Text = text;
        _launchButton.Visible = true;
        SetCloudControlsVisible(showCloudSync);
        ShowLaunchButtons(showUpdate);
        _retryButton.Visible = false;
    }

    internal void ShowRetry()
    {
        _retryButton.Visible = true;
        SetCloudControlsVisible(false);
        ShowRetryButtons();
    }

    internal void HideAll()
    {
        _retryButton.Visible = false;
        SetCloudControlsVisible(false);
        HideSecondaryButtons();
    }

    internal void SetPushPullDisabled(bool disabled)
    {
        _pushButton.Disabled = disabled;
        _pullButton.Disabled = disabled;
    }

    internal void SetUpdateButtonText(string text) => _updateButton.Text = text;

    internal void SetUpdateButtonDisabled(bool disabled) => _updateButton.Disabled = disabled;

    private void SetCloudControlsVisible(bool visible)
    {
        _localBackupToggle.Visible = visible;
        _cloudSyncToggle.Visible = visible;
        _pushPullRow.Visible = visible;
    }

    private void ShowLaunchButtons(bool showUpdate)
    {
        ShowUpdateButton(showUpdate);
        _redownloadButton.Visible = true;
        SetSupportButtonsVisible(true);
        _safeLaunchButton.Visible = true;
    }

    private void ShowRetryButtons()
    {
        ShowUpdateButton(false);
        _redownloadButton.Visible = false;
        SetSupportButtonsVisible(true);
        _safeLaunchButton.Visible = false;
        _launchButton.Visible = false;
    }

    private void HideSecondaryButtons()
    {
        ShowUpdateButton(false);
        _redownloadButton.Visible = false;
        SetSupportButtonsVisible(false);
        _safeLaunchButton.Visible = false;
        _launchButton.Visible = false;
    }

    private void ShowUpdateButton(bool visible)
    {
        _updateButton.Visible = visible;
        _updateButton.Disabled = false;
        _updateButton.Text = "CHECK FOR UPDATES";
    }

    private void SetSupportButtonsVisible(bool visible)
    {
        _diagnosticsButton.Visible = visible;
        _showLastErrorButton.Visible = visible;
        _copyRawLogButton.Visible = visible;
    }

    private Button AddPrimaryHiddenButton(string text, float scale, Action pressed)
        => AddHiddenButton(
            text,
            scale,
            LauncherSectionMetrics.PrimaryButtonFontSize,
            LauncherSectionMetrics.PrimaryButtonHeight,
            pressed
        );

    private Button AddSecondaryHiddenButton(string text, float scale, Action pressed)
        => AddHiddenButton(
            text,
            scale,
            LauncherSectionMetrics.SecondaryButtonFontSize,
            LauncherSectionMetrics.SecondaryButtonHeight,
            pressed
        );

    private Button AddHiddenButton(
        string text,
        float scale,
        int fontSize,
        int height,
        Action pressed
    )
    {
        var button = new StyledButton(text, scale, fontSize: fontSize, height: height);
        button.Visible = false;
        if (pressed != null)
            button.Pressed += pressed;
        AddChild(button);
        return button;
    }

    private static Button AddPushPullButton(
        HBoxContainer row,
        string text,
        float scale,
        Action pressed
    )
    {
        var button = new StyledButton(
            text,
            scale,
            LauncherSectionMetrics.SecondaryButtonFontSize,
            LauncherSectionMetrics.SecondaryButtonHeight
        );
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        if (pressed != null)
            button.Pressed += pressed;
        row.AddChild(button);
        return button;
    }

    private void ConfigureToggle(Button button, Func<bool, string> text, Action<bool> toggled)
    {
        button.ToggleMode = true;
        ApplyToggle(button, false, text);
        button.Toggled += pressed =>
        {
            ApplyToggle(button, pressed, text);
            toggled?.Invoke(pressed);
        };
    }

    private void SetToggleChecked(Button button, bool value, Func<bool, string> text)
    {
        button.ButtonPressed = value;
        ApplyToggle(button, value, text);
    }

    private void ApplyToggle(Button button, bool value, Func<bool, string> text)
    {
        button.Text = text(value);
        var style = value ? _toggleOnStyle : _toggleOffStyle;
        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("hover", style);
        button.AddThemeStyleboxOverride("pressed", style);
        button.AddThemeStyleboxOverride("disabled", style);
    }

    private static string LocalBackupText(bool value) => value ? "Local Backup: ON" : "Local Backup: OFF";

    private static string CloudSyncText(bool value) => value ? "Auto Sync: ON" : "Auto Sync: OFF";
}
