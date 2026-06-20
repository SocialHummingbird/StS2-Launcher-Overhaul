using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupStatus
{
    private static Label CreateAndroidTitleLabel(float scale)
    {
        var label = new Label
        {
            Text = "Starting Game",
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        label.AddThemeFontSizeOverride(
            LauncherComponentTheme.FontSize,
            LauncherComponentTheme.ScaleInt(scale, AndroidTitleFontSize)
        );
        label.AddThemeColorOverride("font_color", LauncherComponentTheme.OrangeHot);
        return label;
    }

    private static Label CreateAndroidMessageLabel(float scale)
    {
        var label = new Label
        {
            Name = MessageNodeName,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        label.AddThemeFontSizeOverride(
            LauncherComponentTheme.FontSize,
            LauncherComponentTheme.ScaleInt(scale, AndroidMessageFontSize)
        );
        label.AddThemeColorOverride("font_color", LauncherComponentTheme.TextPrimary);
        return label;
    }
}
