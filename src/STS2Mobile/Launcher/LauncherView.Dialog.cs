using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private const float CompactDialogWidthRatio = 0.9f;
    private const float CompactDialogMaxMessageHeightRatio = 0.44f;
    private const int CompactDialogMessageMinWidth = 280;
    private const int CompactDialogMessageMinScrollHeight = 96;
    private const int CompactDialogMessageLineHeight = 24;
    private const int CompactDialogLongMessageWidthCharacters = 34;

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

    private static Control BuildDialogMessageArea(string message, LauncherLayoutProfile profile)
    {
        var label = BuildDialogMessage(message, profile);
        if (!profile.Compact || !ShouldScrollDialogMessage(message))
            return label;

        var scroll = new ScrollContainer
        {
            MouseFilter = Control.MouseFilterEnum.Pass,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(
                DialogMessageWidth(profile),
                DialogMessageScrollHeight(message, profile)
            ),
        };
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        scroll.AddChild(label);
        return scroll;
    }

    private static Label BuildDialogMessage(string message, LauncherLayoutProfile profile)
    {
        var scale = profile.Scale;
        var label = new StyledLabel(
            message,
            scale,
            fontSize: LauncherComponentTheme.DialogMessageFontSize
        );
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.CustomMinimumSize = new Vector2(
            DialogMessageWidth(profile),
            0
        );
        label.HorizontalAlignment = HorizontalAlignment.Center;
        return label;
    }

    private static bool ShouldScrollDialogMessage(string message)
        => !string.IsNullOrWhiteSpace(message)
            && (
                message.Length > CompactDialogLongMessageWidthCharacters * 3
                || message.Contains('\n', StringComparison.Ordinal)
            );

    private static int DialogMessageWidth(LauncherLayoutProfile profile)
    {
        var scaledDefault = LauncherComponentTheme.ScaleInt(
            profile.Scale,
            LauncherComponentTheme.DialogMessageWidth
        );
        if (!profile.Compact)
            return scaledDefault;

        var compactCap = Math.Max(CompactDialogMessageMinWidth, (int)(profile.ViewportSize.X * CompactDialogWidthRatio));
        return Math.Min(scaledDefault, compactCap);
    }

    private static int DialogMessageScrollHeight(string message, LauncherLayoutProfile profile)
    {
        var wrappedLines = Math.Max(
            3,
            (int)Math.Ceiling((message?.Length ?? 0) / (double)CompactDialogLongMessageWidthCharacters)
        );
        if (!string.IsNullOrEmpty(message))
            wrappedLines += Math.Max(0, message.Split('\n').Length - 1);

        var estimatedHeight = LauncherComponentTheme.ScaleInt(
            profile.Scale,
            Math.Max(CompactDialogMessageMinScrollHeight, wrappedLines * CompactDialogMessageLineHeight)
        );
        var maxHeight = Math.Max(
            LauncherComponentTheme.ScaleInt(profile.Scale, CompactDialogMessageMinScrollHeight),
            (int)(profile.ViewportSize.Y * CompactDialogMaxMessageHeightRatio)
        );
        return Math.Min(estimatedHeight, maxHeight);
    }

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
