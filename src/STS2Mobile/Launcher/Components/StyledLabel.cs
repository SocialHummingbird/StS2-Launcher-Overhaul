using Godot;

namespace STS2Mobile.Launcher.Components;

internal sealed class StyledLabel : Label
{
    internal StyledLabel(
        string text,
        float scale,
        int fontSize = 15,
        HorizontalAlignment align = HorizontalAlignment.Center
    )
    {
        Text = text;
        HorizontalAlignment = align;
        AddThemeFontSizeOverride(
            LauncherComponentTheme.FontSize,
            LauncherComponentTheme.ScaleInt(scale, fontSize)
        );
    }
}
