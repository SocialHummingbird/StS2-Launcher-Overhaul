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

        private Button Button { get; }
        private Func<bool, string> Text { get; }
        private Action<bool> NotifyChanged { get; }

        internal static ToggleControl Create(
            Button button,
            Func<bool, string> text,
            Action<bool> notifyChanged
        )
            => new(button, text, notifyChanged);

        internal void Configure(ActionSection section)
        {
            var button = Button;
            var text = Text;
            var notifyChanged = NotifyChanged;

            Button.ToggleMode = true;
            section.ApplyToggle(button, false, text(false));
            Button.Toggled += pressed =>
            {
                section.ApplyToggle(button, pressed, text(pressed));
                notifyChanged(pressed);
            };
        }

        internal void SetChecked(ActionSection section, bool value)
        {
            Button.ButtonPressed = value;
            section.ApplyToggle(Button, value, Text(value));
        }
    }

    private void ConfigureLocalBackupToggle()
        => LocalBackupToggle().Configure(this);

    private void ConfigureCloudSyncToggle()
        => CloudSyncToggle().Configure(this);

    private ToggleControl LocalBackupToggle()
        => ToggleControl.Create(
            _localBackupToggle,
            LocalBackupText,
            NotifyLocalBackupToggled
        );

    private ToggleControl CloudSyncToggle()
        => ToggleControl.Create(
            _cloudSyncToggle,
            CloudSyncText,
            NotifyCloudSyncToggled
        );

    private void NotifyLocalBackupToggled(bool pressed)
        => LocalBackupToggled?.Invoke(pressed);

    private void NotifyCloudSyncToggled(bool pressed)
        => CloudSyncToggled?.Invoke(pressed);

    private void ApplyLocalBackupToggle(bool value)
        => LocalBackupToggle().SetChecked(this, value);

    private void ApplyCloudSyncToggle(bool value)
        => CloudSyncToggle().SetChecked(this, value);

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
