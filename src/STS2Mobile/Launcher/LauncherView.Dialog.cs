using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static ColorRect BuildConfirmationDialog(
        string message,
        LauncherLayoutProfile profile,
        Action onConfirmed,
        Action onCancelled = null,
        string confirmText = null,
        string cancelText = null
    )
    {
        var scale = profile.Scale;
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

        vbox.AddChild(BuildDialogMessageArea(message, profile));
        vbox.AddChild(BuildDialogButtons(
            dialog,
            profile,
            onConfirmed,
            onCancelled,
            confirmText,
            cancelText
        ));

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
}
