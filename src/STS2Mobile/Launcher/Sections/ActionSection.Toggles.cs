using Godot;
using System;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private void ConfigureLocalBackupToggle()
        => ConfigureToggle(
            _localBackupToggle,
            LocalBackupText,
            pressed =>
            {
                _localBackupEnabled = pressed;
                UpdateCloudOptionsToggleText();
                LocalBackupToggled?.Invoke(pressed);
                ApplyToggle(_localBackupToggle, pressed, LocalBackupText(pressed));
                RefreshCloudPushEligibility();
            }
        );

    private void ConfigureCloudSyncToggle()
        => ConfigureToggle(
            _cloudSyncToggle,
            CloudSyncText,
            pressed =>
            {
                _cloudSyncEnabled = pressed;
                UpdateCloudOptionsToggleText();
                CloudSyncToggled?.Invoke(pressed);
            }
        );

    private void ApplyLocalBackupToggle(bool value)
    {
        _localBackupEnabled = value;
        UpdateCloudOptionsToggleText();
        SetToggleChecked(_localBackupToggle, LocalBackupText, value);
    }

    private void ApplyCloudSyncToggle(bool value)
    {
        _cloudSyncEnabled = value;
        UpdateCloudOptionsToggleText();
        SetToggleChecked(_cloudSyncToggle, CloudSyncText, value);
    }

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
        SetCompactActionButtonText(button, text);
        var style = value ? _toggleOnStyle : _toggleOffStyle;
        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("hover", style);
        button.AddThemeStyleboxOverride("pressed", style);
        button.AddThemeStyleboxOverride("disabled", style);
    }

    private string LocalBackupText(bool value)
        => LocalBackupText(value, mirrorSaveCount: null);

    private string LocalBackupText(bool value, int? mirrorSaveCount)
    {
        var detail = LocalBackupDetail(value, mirrorSaveCount);
        return _compact
            ? CompactCloudOptionText("Save Backup", OnOff(value), detail)
            : $"Local Backup: {OnOff(value)} ({detail})";
    }

    private static string LocalBackupDetail(
        bool value,
        int? mirrorSaveCount
    )
    {
        if (!value)
            return "Local safety";
        if (!STS2Mobile.AppPaths.HasStoragePermission())
            return "Needs storage access";

        var count = mirrorSaveCount
            ?? LauncherBackupEvidence.CurrentMirrorSaveCount();
        return count == 1 ? "1 save stored" : $"{count} saves stored";
    }

    private string CloudSyncText(bool value)
        => _compact
            ? CompactCloudOptionText("Cloud Sync", OnOff(value), "Steam saves")
            : $"Game Cloud Sync: {OnOff(value)}";

    private static string CompactCloudOptionText(string label, string state, string detail)
        => $"{label} {state}\n{detail}";
}
