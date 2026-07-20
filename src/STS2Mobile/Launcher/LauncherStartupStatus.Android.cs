using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupStatus
{
    private const string MessageNodeName = "STS2MobileStartupStatusMessage";
    private const float ReferenceShortEdge = 720f;
    private const float AndroidMinimumScale = 1.06f;
    private const float AndroidMaximumScale = 1.38f;
    private const float AndroidWidthRatio = 0.94f;
    private const int AndroidPanelRadius = 8;
    private const int AndroidTitleFontSize = 12;
    private const int AndroidMessageFontSize = 18;
    private const int AndroidPanelHorizontalMargin = 14;
    private const int AndroidPanelVerticalMargin = 12;
    private const int AndroidPanelSeparation = 5;
    private const int AndroidPanelHeight = 98;

    private static Label CreateAndroidStatusCard(Node parent, Vector2 viewportSize)
    {
        var scale = CalculateAndroidScale(viewportSize);
        var margin = CalculateSafeMargin(viewportSize);
        var shell = new ColorRect
        {
            Name = NodeName,
            ZIndex = ZIndex,
            Color = LauncherComponentTheme.ScreenBackground,
            MouseFilter = Control.MouseFilterEnum.Stop,
            ProcessMode = Node.ProcessModeEnum.Always,
        };
        shell.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var safeArea = new MarginContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        safeArea.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        safeArea.OffsetLeft = margin;
        safeArea.OffsetTop = margin;
        safeArea.OffsetRight = -margin;
        safeArea.OffsetBottom = margin
            + LauncherComponentTheme.ScaleInt(scale, AndroidPanelHeight);
        shell.AddChild(safeArea);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(CalculateAndroidPanelWidth(viewportSize, margin), 0),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        panel.AddThemeStyleboxOverride(LauncherComponentTheme.Panel, BuildAndroidPanelStyle(scale));
        safeArea.AddChild(panel);

        var content = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        content.AddThemeConstantOverride(
            LauncherComponentTheme.ThemeSeparation,
            LauncherComponentTheme.ScaleInt(scale, AndroidPanelSeparation)
        );
        panel.AddChild(content);

        content.AddChild(CreateAndroidTitleLabel(scale));
        var message = CreateAndroidMessageLabel(scale);
        content.AddChild(message);

        parent.AddChild(shell);
        return message;
    }
}
