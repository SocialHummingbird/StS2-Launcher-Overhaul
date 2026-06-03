using Godot;
using System;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private void ConfigureLocalBackupToggle()
        => ConfigureToggle(
            _localBackupToggle,
            LocalBackupText,
            pressed => LocalBackupToggled?.Invoke(pressed)
        );

    private void ConfigureCloudSyncToggle()
        => ConfigureToggle(
            _cloudSyncToggle,
            CloudSyncText,
            pressed => CloudSyncToggled?.Invoke(pressed)
        );

    private void ApplyLocalBackupToggle(bool value)
        => SetToggleChecked(_localBackupToggle, LocalBackupText, value);

    private void ApplyCloudSyncToggle(bool value)
        => SetToggleChecked(_cloudSyncToggle, CloudSyncText, value);

    private void ConfigureToggle(
        Button button,
        Func<bool, string> text,
        Action<bool> notifyChanged
    )
    {
        button.ToggleMode = true;
        ApplyToggle(button, false, text(false));
        button.Toggled += pressed =>
        {
            ApplyToggle(button, pressed, text(pressed));
            notifyChanged(pressed);
        };
    }

    private void SetToggleChecked(
        Button button,
        Func<bool, string> text,
        bool value
    )
    {
        button.ButtonPressed = value;
        ApplyToggle(button, value, text(value));
    }

    private void ApplyToggle(Button button, bool value, string text)
    {
        button.Text = text;
        var style = value ? _toggleOnStyle : _toggleOffStyle;
        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("hover", style);
        button.AddThemeStyleboxOverride("pressed", style);
        button.AddThemeStyleboxOverride("disabled", style);
    }

    private static string LocalBackupText(bool value)
        => value ? "Local Backup: ON" : "Local Backup: OFF";

    private static string CloudSyncText(bool value)
        => value ? "Auto Sync: ON" : "Auto Sync: OFF";
}
