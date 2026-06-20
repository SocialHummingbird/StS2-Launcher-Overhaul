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
}
