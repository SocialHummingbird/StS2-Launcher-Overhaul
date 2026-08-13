using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal readonly struct LauncherViewSecondaryStatus
{
    internal LauncherViewSecondaryStatus(
        Control banner,
        StyledLabel severity,
        StyledLabel message,
        ColorRect accent
    )
    {
        Banner = banner;
        Severity = severity;
        Message = message;
        Accent = accent;
    }

    internal Control Banner { get; }
    internal StyledLabel Severity { get; }
    internal StyledLabel Message { get; }
    internal ColorRect Accent { get; }
}

internal sealed partial class LauncherView
{
    private static LauncherViewSecondaryStatus BuildSecondaryStatusBanner(
        LauncherLayoutProfile profile
    )
    {
        var scale = profile.Scale;
        var panel = new PanelContainer
        {
            Name = "SecondaryStatusBanner",
            Visible = false,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        var style = LauncherStyleBoxes.MakeFilled(
            new Color(0.025f, 0.04f, 0.05f, 0.86f),
            LauncherViewLayoutMetrics.ScaleInt(5, scale)
        );
        style.BorderColor = new Color(0.08f, 0.24f, 0.28f, 0.6f);
        style.SetBorderWidthAll(System.Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(8, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(8, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(6, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(6, scale);
        panel.AddThemeStyleboxOverride(LauncherComponentTheme.Panel, style);

        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(8, scale)
        );
        panel.AddChild(row);

        var accent = new ColorRect
        {
            CustomMinimumSize = new Vector2(LauncherViewLayoutMetrics.ScaleInt(3, scale), 0),
        };
        row.AddChild(accent);

        var severity = new StyledLabel(
            "Working",
            scale,
            fontSize: profile.Compact ? 12 : 11,
            align: HorizontalAlignment.Left
        )
        {
            Name = "SecondaryStatusSeverity",
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.AddChild(severity);

        var message = new StyledLabel(
            "Starting launcher...",
            scale,
            fontSize: profile.Compact ? 13 : 12,
            align: HorizontalAlignment.Left
        )
        {
            Name = "SecondaryStatusMessage",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.AddChild(message);
        return new LauncherViewSecondaryStatus(panel, severity, message, accent);
    }
}
