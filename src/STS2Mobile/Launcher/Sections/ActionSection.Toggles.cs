using Godot;
using System;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private readonly struct ToggleControl
    {
        private ToggleControl(
            Button button,
            Func<bool, string> text,
            Action<bool> notifyChanged
        )
        {
            Button = button;
            Text = text;
            NotifyChanged = notifyChanged;
        }

        internal Button Button { get; }
        internal Func<bool, string> Text { get; }
        internal Action<bool> NotifyChanged { get; }

        internal static ToggleControl Create(
            Button button,
            Func<bool, string> text,
            Action<bool> notifyChanged
        )
            => new(button, text, notifyChanged);
    }

    private void ConfigureLocalBackupToggle()
        => ConfigureToggle(LocalBackupToggle());

    private void ConfigureCloudSyncToggle()
        => ConfigureToggle(CloudSyncToggle());

    private ToggleControl LocalBackupToggle()
        => ToggleControl.Create(
            _localBackupToggle,
            LocalBackupText,
            pressed => LocalBackupToggled?.Invoke(pressed)
        );

    private ToggleControl CloudSyncToggle()
        => ToggleControl.Create(
            _cloudSyncToggle,
            CloudSyncText,
            pressed => CloudSyncToggled?.Invoke(pressed)
        );

    private void ConfigureToggle(ToggleControl toggle)
    {
        toggle.Button.ToggleMode = true;
        ApplyToggle(toggle.Button, false, toggle.Text(false));
        toggle.Button.Toggled += pressed =>
        {
            ApplyToggle(toggle.Button, pressed, toggle.Text(pressed));
            toggle.NotifyChanged(pressed);
        };
    }

    private void ApplyLocalBackupToggle(bool value)
        => SetToggleChecked(LocalBackupToggle(), value);

    private void ApplyCloudSyncToggle(bool value)
        => SetToggleChecked(CloudSyncToggle(), value);

    private void SetToggleChecked(ToggleControl toggle, bool value)
    {
        toggle.Button.ButtonPressed = value;
        ApplyToggle(toggle.Button, value, toggle.Text(value));
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

    private static string LocalBackupText(bool value) => value ? "Local Backup: ON" : "Local Backup: OFF";

    private static string CloudSyncText(bool value) => value ? "Auto Sync: ON" : "Auto Sync: OFF";
}
