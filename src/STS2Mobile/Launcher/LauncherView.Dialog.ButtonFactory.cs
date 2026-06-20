using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static void ApplyDialogButtonLayout(Button button, LauncherLayoutProfile profile)
    {
        if (!profile.Compact)
            return;

        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        button.CustomMinimumSize = new Vector2(
            DialogMessageWidth(profile),
            LauncherComponentTheme.ScaleInt(profile.Scale, LauncherComponentTheme.DialogButtonHeight)
        );
    }

    private static Button BuildDialogButton(
        string text,
        float scale,
        Action callback,
        Action<Button, float> applyStyle
    )
    {
        var button = new StyledButton(
            text,
            scale,
            LauncherComponentTheme.DialogButtonFontSize,
            LauncherComponentTheme.DialogButtonHeight
        );
        button.MouseFilter = Control.MouseFilterEnum.Stop;
        button.CustomMinimumSize = new Vector2(
            LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.DialogButtonWidth),
            button.CustomMinimumSize.Y
        );
        applyStyle(button, scale);
        button.Pressed += callback;
        return button;
    }
}
