using Godot;
using System;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private void ConfigureLocalBackupToggle()
        => ConfigureToggle(
            _localBackupToggle,
            ApplyLocalBackupToggle,
            pressed => LocalBackupToggled?.Invoke(pressed)
        );

    private void ConfigureCloudSyncToggle()
        => ConfigureToggle(
            _cloudSyncToggle,
            ApplyCloudSyncToggle,
            pressed => CloudSyncToggled?.Invoke(pressed)
        );

    private static void ConfigureToggle(
        Button button,
        Action<bool> applyState,
        Action<bool> notifyChanged
    )
    {
        button.ToggleMode = true;
        applyState(false);
        button.Toggled += pressed =>
        {
            applyState(pressed);
            notifyChanged(pressed);
        };
    }

    private void ApplyLocalBackupToggle(bool value)
        => ApplyToggle(_localBackupToggle, value, LocalBackupText(value));

    private void ApplyCloudSyncToggle(bool value)
        => ApplyToggle(_cloudSyncToggle, value, CloudSyncText(value));

    private void ApplyToggle(Button button, bool value, string text)
    {
        button.Text = text;
        var style = value ? _toggleOnStyle : _toggleOffStyle;
        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("hover", style);
        button.AddThemeStyleboxOverride("pressed", style);
        button.AddThemeStyleboxOverride("disabled", style);
    }

    private static string LocalBackupText(bool value) => value ? "Local Backup: ON" : "Local Backup: OFF";

    private static string CloudSyncText(bool value) => value ? "Auto Sync: ON" : "Auto Sync: OFF";
}
