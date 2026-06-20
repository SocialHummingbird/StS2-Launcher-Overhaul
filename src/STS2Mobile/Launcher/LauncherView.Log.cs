using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private const int CompactDiagnosticsLogFontSize = 15;
    private const int CompactDiagnosticsLogMarginHorizontal = 12;
    private const int CompactDiagnosticsLogMarginVertical = 10;

    internal void AppendLog(string msg) => AppendLogLine(Log, msg);

    internal void AppendColoredLog(string msg, Godot.Color color)
        => AppendColoredLogLine(Log, msg, color);

    private static RichTextLabel BuildLogView(LauncherLayoutProfile profile)
    {
        var scale = profile.Scale;
        var compact = profile.Compact;
        var log = new RichTextLabel
        {
            CustomMinimumSize = new Vector2(
                0,
                LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.LogHeight)
            ),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ScrollFollowing = true,
            BbcodeEnabled = true,
        };

        log.AddThemeFontSizeOverride(
            LauncherComponentTheme.NormalFontSize,
            LauncherComponentTheme.ScaleInt(
                scale,
                compact ? CompactDiagnosticsLogFontSize : LauncherComponentTheme.LogFontSize
            )
        );
        log.AddThemeColorOverride(LauncherComponentTheme.DefaultColor, LauncherComponentTheme.LogText);
        log.AddThemeStyleboxOverride(LauncherComponentTheme.StateNormal, BuildLogStyle(scale, compact));
        return log;
    }

    private static void AppendLogLine(RichTextLabel log, string msg)
        => log.AddText(msg + "\n");

    private static void AppendColoredLogLine(RichTextLabel log, string msg, Color color)
    {
        log.PushColor(color);
        log.AddText(msg + "\n");
        log.Pop();
    }

    private static StyleBoxFlat BuildLogStyle(float scale, bool compact)
    {
        var background = new StyleBoxFlat();
        background.BgColor = LauncherComponentTheme.LogBackground;
        background.SetCornerRadiusAll(
            LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.LogRadius)
        );
        var horizontalMargin = compact
            ? CompactDiagnosticsLogMarginHorizontal
            : LauncherComponentTheme.LogMarginHorizontal;
        var verticalMargin = compact
            ? CompactDiagnosticsLogMarginVertical
            : LauncherComponentTheme.LogMarginVertical;
        background.ContentMarginLeft = LauncherComponentTheme.ScaleInt(
            scale,
            horizontalMargin
        );
        background.ContentMarginRight = LauncherComponentTheme.ScaleInt(
            scale,
            horizontalMargin
        );
        background.ContentMarginTop = LauncherComponentTheme.ScaleInt(
            scale,
            verticalMargin
        );
        background.ContentMarginBottom = LauncherComponentTheme.ScaleInt(
            scale,
            verticalMargin
        );
        return background;
    }
}
