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
            MouseFilter = Control.MouseFilterEnum.Stop,
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
        center.MouseFilter = Control.MouseFilterEnum.Pass;
        return center;
    }

    private static PanelContainer BuildDialogBox(float scale)
    {
        var dialogBox = new PanelContainer();
        dialogBox.MouseFilter = Control.MouseFilterEnum.Pass;
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
        vbox.MouseFilter = Control.MouseFilterEnum.Pass;
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
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
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
        buttonRow.MouseFilter = Control.MouseFilterEnum.Pass;
        buttonRow.AddThemeConstantOverride(
            LauncherComponentTheme.ThemeSeparation,
            LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.DialogButtonSeparation)
        );
        buttonRow.Alignment = BoxContainer.AlignmentMode.Center;

        var dismissed = false;
        void Dismiss(Action callback)
        {
            if (dismissed)
                return;

            dismissed = true;
            dialog.QueueFree();
            callback?.Invoke();
        }

        var cancel = BuildDialogButton("Cancel", scale, () => Dismiss(null));
        var ok = BuildDialogButton("OK", scale, () => Dismiss(onConfirmed));

        buttonRow.AddChild(cancel);
        buttonRow.AddChild(ok);
        dialog.GuiInput += ev =>
        {
            if (!TryGetPressedPointerPosition(ev, out var position))
                return;

            if (ok.GetGlobalRect().HasPoint(position))
                Dismiss(onConfirmed);
            else if (cancel.GetGlobalRect().HasPoint(position))
                Dismiss(null);
        };

        return buttonRow;
    }

    private static Button BuildDialogButton(
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
        button.MouseFilter = Control.MouseFilterEnum.Stop;
        button.CustomMinimumSize = new Vector2(
            LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.DialogButtonWidth),
            button.CustomMinimumSize.Y
        );
        button.Pressed += callback;
        return button;
    }

    private static bool TryGetPressedPointerPosition(InputEvent ev, out Vector2 position)
    {
        switch (ev)
        {
            case InputEventScreenTouch { Pressed: true } touch:
                position = touch.Position;
                return true;
            case InputEventMouseButton { Pressed: true } mouse:
                position = mouse.Position;
                return true;
            default:
                position = Vector2.Zero;
                return false;
        }
    }
}
