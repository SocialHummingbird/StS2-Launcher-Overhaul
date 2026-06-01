using Godot;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private void ConfigureLocalBackupToggle()
    {
        _localBackupToggle.ToggleMode = true;
        ApplyLocalBackupToggle(false);
        _localBackupToggle.Toggled += pressed =>
        {
            ApplyLocalBackupToggle(pressed);
            LocalBackupToggled?.Invoke(pressed);
        };
    }

    private void ConfigureCloudSyncToggle()
    {
        _cloudSyncToggle.ToggleMode = true;
        ApplyCloudSyncToggle(false);
        _cloudSyncToggle.Toggled += pressed =>
        {
            ApplyCloudSyncToggle(pressed);
            CloudSyncToggled?.Invoke(pressed);
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
