using Godot;

namespace STS2Mobile.Launcher.Components;

internal sealed class StyledButton : Button
{
    internal StyledButton(
        string text,
        float scale,
        int fontSize = LauncherComponentTheme.ButtonDefaultFontSize,
        int height = LauncherComponentTheme.ButtonDefaultHeight
    )
    {
        Text = text;
        MouseDefaultCursorShape = CursorShape.PointingHand;
        FocusMode = FocusModeEnum.All;
        AccessibilityName = text;
        ClipText = true;
        TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        CustomMinimumSize = new Vector2(0, LauncherComponentTheme.ScaleInt(scale, height));
        AddThemeFontSizeOverride(
            LauncherComponentTheme.FontSize,
            LauncherComponentTheme.ScaleInt(scale, fontSize)
        );
        ApplyTheme(scale);
    }

    private void ApplyTheme(float scale)
    {
        var radius = LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.ButtonRadius);
        var focus = LauncherStyleBoxes.MakeOutline(LauncherComponentTheme.OrangeHot, radius, 2);
        AddThemeStyleboxOverride("focus", focus);
        AddThemeStyleboxOverride(
            LauncherComponentTheme.StateNormal,
            LauncherStyleBoxes.MakeFilled(LauncherComponentTheme.ButtonNormal, radius)
        );
        AddThemeStyleboxOverride(
            LauncherComponentTheme.StateHover,
            LauncherStyleBoxes.MakeFilled(LauncherComponentTheme.ButtonHover, radius)
        );
        AddThemeStyleboxOverride(
            LauncherComponentTheme.StatePressed,
            LauncherStyleBoxes.MakeFilled(LauncherComponentTheme.ButtonPressed, radius)
        );
        AddThemeStyleboxOverride(
            LauncherComponentTheme.StateDisabled,
            LauncherStyleBoxes.MakeFilled(LauncherComponentTheme.ButtonDisabled, radius)
        );
        AddThemeColorOverride("font_color", LauncherComponentTheme.TextPrimary);
        AddThemeColorOverride("font_hover_color", LauncherComponentTheme.TextPrimary);
        AddThemeColorOverride("font_pressed_color", LauncherComponentTheme.CyanAccent);
        AddThemeColorOverride("font_disabled_color", LauncherComponentTheme.TextMuted);
    }
}
