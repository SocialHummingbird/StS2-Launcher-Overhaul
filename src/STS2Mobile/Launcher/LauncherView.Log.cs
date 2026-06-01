using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private sealed class LogView : RichTextLabel
    {
        private LogView(float scale)
        {
            CustomMinimumSize = new Vector2(
                0,
                LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.LogHeight)
            );
            ScrollFollowing = true;
            BbcodeEnabled = true;
            AddThemeFontSizeOverride(
                LauncherComponentTheme.NormalFontSize,
                LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.LogFontSize)
            );
            AddThemeColorOverride(LauncherComponentTheme.DefaultColor, LauncherComponentTheme.LogText);
            AddThemeStyleboxOverride(LauncherComponentTheme.StateNormal, BuildStyle(scale));
        }

        private void AppendLog(string msg) => AddText(msg + "\n");

        private void AppendColoredLog(string msg, Color color)
        {
            PushColor(color);
            AddText(msg + "\n");
            Pop();
        }

        private static StyleBoxFlat BuildStyle(float scale)
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
}
