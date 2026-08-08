using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private static Label CreateTitle(float scale)
        => CreateLabel(
            "Game is starting...",
            LauncherComponentTheme.ScaleInt(scale, TitleFontSize),
            TitleColor
        );

    private static Label CreateDetail(float scale, bool compact)
    {
        var detail = CreateLabel(
            compact
                ? "If startup stalls, restart the app, try Safe Start, or create a help report. Review logs before sharing."
                : "If this screen does not change, create a help report, copy the launcher log for local review, or restart with safe launch. Logs can contain identifying data; review/redact before sharing. These controls hide automatically after a successful startup.",
            LauncherComponentTheme.ScaleInt(scale, DetailFontSize),
            DetailColor
        );
        detail.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        return detail;
    }

    private static Label CreateLabel(string text, int fontSize, Color color)
    {
        var label = new Label
        {
            Text = text,
        };
        label.AddThemeFontSizeOverride(ThemeFontSize, fontSize);
        label.AddThemeColorOverride(ThemeFontColor, color);
        return label;
    }
}
