using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static ColorRect BuildConfirmationDialog(string message, float scale, Action onConfirmed)
    {
        var dialog = new ColorRect
        {
            Color = LauncherComponentTheme.DialogOverlay,
        };
        dialog.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var center = BuildDialogCenter();
        var dialogBox = BuildDialogBox(scale);
        var vbox = BuildDialogContentBox(scale);
        dialogBox.AddChild(vbox);

        vbox.AddChild(BuildDialogMessage(message, scale));
        vbox.AddChild(BuildDialogButtons(dialog, scale, onConfirmed));

        center.AddChild(dialogBox);
        dialog.AddChild(center);
        return dialog;
    }

    private static CenterContainer BuildDialogCenter()
    {
        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        return center;
    }

    private static PanelContainer BuildDialogBox(float scale)
    {
        var dialogBox = new PanelContainer();
        var boxStyle = new StyleBoxFlat();
        boxStyle.BgColor = LauncherComponentTheme.DialogPanelBackground;
        boxStyle.SetCornerRadiusAll(
            LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.DialogPanelRadius)
        );
        boxStyle.SetContentMarginAll(
            LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.DialogPanelMargin)
        );
        dialogBox.AddThemeStyleboxOverride(LauncherComponentTheme.Panel, boxStyle);
        return dialogBox;
    }

    private static VBoxContainer BuildDialogContentBox(float scale)
    {
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride(
            LauncherComponentTheme.ThemeSeparation,
            LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.DialogContentSeparation)
        );
        return vbox;
    }

    private static Label BuildDialogMessage(string message, float scale)
    {
        var label = new StyledLabel(
            message,
            scale,
            fontSize: LauncherComponentTheme.DialogMessageFontSize
        );
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.CustomMinimumSize = new Vector2(
            LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.DialogMessageWidth),
            0
        );
        label.HorizontalAlignment = HorizontalAlignment.Center;
        return label;
    }

    private static HBoxContainer BuildDialogButtons(
        ColorRect dialog,
        float scale,
        Action onConfirmed
    )
    {
        var buttonRow = new HBoxContainer();
        buttonRow.AddThemeConstantOverride(
            LauncherComponentTheme.ThemeSeparation,
            LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.DialogButtonSeparation)
        );
        buttonRow.Alignment = BoxContainer.AlignmentMode.Center;

        buttonRow.AddChild(BuildDialogButton(dialog, "Cancel", scale, null));
        buttonRow.AddChild(BuildDialogButton(dialog, "OK", scale, onConfirmed));

        return buttonRow;
    }

    private static Button BuildDialogButton(
        ColorRect dialog,
        string text,
        float scale,
        Action callback
    )
    {
        var button = new StyledButton(
            text,
            scale,
            LauncherComponentTheme.DialogButtonFontSize,
            LauncherComponentTheme.DialogButtonHeight
        );
        button.CustomMinimumSize = new Vector2(
            LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.DialogButtonWidth),
            button.CustomMinimumSize.Y
        );
        button.Pressed += () =>
        {
            dialog.QueueFree();
            callback?.Invoke();
        };
        return button;
    }
}
