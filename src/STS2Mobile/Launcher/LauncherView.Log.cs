using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static RichTextLabel BuildLogView(float scale)
    {
        var log = new RichTextLabel
        {
            CustomMinimumSize = new Vector2(
                0,
                LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.LogHeight)
            ),
            ScrollFollowing = true,
            BbcodeEnabled = true,
        };

        log.AddThemeFontSizeOverride(
            LauncherComponentTheme.NormalFontSize,
            LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.LogFontSize)
        );
        log.AddThemeColorOverride(LauncherComponentTheme.DefaultColor, LauncherComponentTheme.LogText);
        log.AddThemeStyleboxOverride(LauncherComponentTheme.StateNormal, BuildLogStyle(scale));
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

    private static StyleBoxFlat BuildLogStyle(float scale)
    {
        var background = new StyleBoxFlat();
        background.BgColor = LauncherComponentTheme.LogBackground;
        background.SetCornerRadiusAll(
            LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.LogRadius)
        );
        background.ContentMarginLeft = LauncherComponentTheme.ScaleInt(
            scale,
            LauncherComponentTheme.LogMarginHorizontal
        );
        background.ContentMarginRight = LauncherComponentTheme.ScaleInt(
            scale,
            LauncherComponentTheme.LogMarginHorizontal
        );
        background.ContentMarginTop = LauncherComponentTheme.ScaleInt(
            scale,
            LauncherComponentTheme.LogMarginVertical
        );
        background.ContentMarginBottom = LauncherComponentTheme.ScaleInt(
            scale,
            LauncherComponentTheme.LogMarginVertical
        );
        return background;
    }
}
