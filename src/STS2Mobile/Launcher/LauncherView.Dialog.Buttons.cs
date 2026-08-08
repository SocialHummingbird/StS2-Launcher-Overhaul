using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static BoxContainer BuildDialogButtons(
        ColorRect dialog,
        LauncherLayoutProfile profile,
        Action onConfirmed,
        Action onCancelled,
        string confirmText,
        string cancelText
    )
    {
        var scale = profile.Scale;
        BoxContainer buttonRow = profile.Compact ? new VBoxContainer() : new HBoxContainer();
        buttonRow.MouseFilter = Control.MouseFilterEnum.Pass;
        buttonRow.AddThemeConstantOverride(
            LauncherComponentTheme.ThemeSeparation,
            LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.DialogButtonSeparation)
        );
        buttonRow.Alignment = profile.Compact
            ? BoxContainer.AlignmentMode.Begin
            : BoxContainer.AlignmentMode.Center;

        var dismissed = false;
        void Dismiss(Action callback)
        {
            if (dismissed)
                return;

            dismissed = true;
            dialog.QueueFree();
            callback?.Invoke();
        }

        var cancel = BuildDialogButton(
            DialogButtonText(cancelText, "Cancel"),
            scale,
            () => Dismiss(onCancelled),
            LauncherButtonStyles.ApplySupportAction
        );
        var ok = BuildDialogButton(
            DialogButtonText(confirmText, "OK"),
            scale,
            () => Dismiss(onConfirmed),
            LauncherButtonStyles.ApplyPrimaryAction
        );
        ApplyDialogButtonLayout(cancel, profile);
        ApplyDialogButtonLayout(ok, profile);

        buttonRow.AddChild(cancel);
        buttonRow.AddChild(ok);
        dialog.GuiInput += ev =>
        {
            if (!TryGetPressedPointerPosition(ev, out var position))
                return;

            if (ok.GetGlobalRect().HasPoint(position))
                Dismiss(onConfirmed);
            else if (cancel.GetGlobalRect().HasPoint(position))
                Dismiss(onCancelled);
        };

        return buttonRow;
    }

    private static string DialogButtonText(string text, string fallback)
        => string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
}
