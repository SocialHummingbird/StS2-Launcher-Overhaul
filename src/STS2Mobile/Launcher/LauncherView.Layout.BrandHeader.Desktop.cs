using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static Control BuildDesktopBrandCopy(float scale)
    {
        var copy = new VBoxContainer();
        copy.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        copy.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(0, scale)
        );

        var title = new StyledLabel("StS2 Launcher", scale, fontSize: 26);
        title.HorizontalAlignment = HorizontalAlignment.Left;
        title.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextPrimary
        );
        copy.AddChild(title);

        var subtitle = new StyledLabel(
            "Sign in. Download. Play.",
            scale,
            fontSize: 11
        );
        subtitle.HorizontalAlignment = HorizontalAlignment.Left;
        subtitle.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.CyanAccent
        );
        copy.AddChild(subtitle);
        return copy;
    }
}
