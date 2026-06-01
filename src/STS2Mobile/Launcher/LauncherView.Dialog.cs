using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private sealed class StyledDialog : ColorRect
    {
        internal StyledDialog(string message, float scale, Action onConfirmed)
        {
            SetAnchorsPreset(Control.LayoutPreset.FullRect);
            Color = LauncherComponentTheme.DialogOverlay;

            var center = BuildCenter();
            var dialogBox = BuildDialogBox(scale);
            var vbox = BuildContentBox(scale);
            dialogBox.AddChild(vbox);

            vbox.AddChild(BuildMessage(message, scale));
            vbox.AddChild(BuildButtons(scale, onConfirmed));

            center.AddChild(dialogBox);
            AddChild(center);
        }

        private static CenterContainer BuildCenter()
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

        private static VBoxContainer BuildContentBox(float scale)
        {
            var vbox = new VBoxContainer();
            vbox.AddThemeConstantOverride(
                LauncherComponentTheme.ThemeSeparation,
                LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.DialogContentSeparation)
            );
            return vbox;
        }

        private static Label BuildMessage(string message, float scale)
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

        private HBoxContainer BuildButtons(float scale, Action onConfirmed)
        {
            var buttonRow = new HBoxContainer();
            buttonRow.AddThemeConstantOverride(
                LauncherComponentTheme.ThemeSeparation,
                LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.DialogButtonSeparation)
            );
            buttonRow.Alignment = BoxContainer.AlignmentMode.Center;

            buttonRow.AddChild(BuildButton("Cancel", scale, null));
            buttonRow.AddChild(BuildButton("OK", scale, onConfirmed));

            return buttonRow;
        }

        private Button BuildButton(string text, float scale, Action callback)
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
                QueueFree();
                callback?.Invoke();
            };
            return button;
        }
    }
}
