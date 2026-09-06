using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private static Label CreateTitle(float scale)
        => CreateLabel(
            "Waiting for the game...",
            LauncherComponentTheme.ScaleInt(scale, TitleFontSize),
            TitleColor
        );

    private static Label CreateDetail(float scale, bool compact)
    {
        var detail = CreateLabel(
            compact
                ? "If the game does not appear, return to the launcher and try Safe Start. Create a support report if it repeats."
                : "If the game does not appear, return to the launcher and try Safe Start. Create a support report if it repeats. Repair files only when the launcher reports a preparation failure. Review diagnostics before sharing.",
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
