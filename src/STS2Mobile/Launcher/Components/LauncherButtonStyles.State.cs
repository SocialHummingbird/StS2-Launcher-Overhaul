using System;
using Godot;

namespace STS2Mobile.Launcher.Components;

internal static partial class LauncherButtonStyles
{
    private const string FontColor = "font_color";
    private const string FontHoverColor = "font_hover_color";
    private const string FontPressedColor = "font_pressed_color";
    private const string FontDisabledColor = "font_disabled_color";

    private static void Apply(
        Button button,
        float scale,
        Color body,
        Color border,
        Color font,
        int borderWidth,
        bool filled = true
    )
    {
        button.ClipText = true;
        button.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;

        var radius = LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.ButtonRadius);
        var width = Math.Max(1, LauncherComponentTheme.ScaleInt(scale, borderWidth));
        var normal = BuildButtonStateStyle(body, border, radius, width, filled);
        var hover = BuildButtonStateStyle(
            filled ? body.Lightened(0.16f) : border.Lightened(0.1f),
            border.Lightened(0.15f),
            radius,
            width,
            filled
        );
        var pressed = BuildButtonStateStyle(
            filled ? body.Darkened(0.18f) : border.Darkened(0.08f),
            border.Darkened(0.08f),
            radius,
            width,
            filled
        );
        var disabled = BuildButtonStateStyle(
            filled ? body.Darkened(0.25f) : border.Darkened(0.28f),
            border.Darkened(0.3f),
            radius,
            width,
            filled
        );

        button.AddThemeStyleboxOverride(LauncherComponentTheme.StateNormal, normal);
        button.AddThemeStyleboxOverride(LauncherComponentTheme.StateHover, hover);
        button.AddThemeStyleboxOverride(LauncherComponentTheme.StatePressed, pressed);
        button.AddThemeStyleboxOverride(LauncherComponentTheme.StateDisabled, disabled);
        button.AddThemeColorOverride(FontColor, font);
        button.AddThemeColorOverride(FontHoverColor, font.Lightened(0.08f));
        button.AddThemeColorOverride(FontPressedColor, font.Darkened(0.08f));
        button.AddThemeColorOverride(FontDisabledColor, LauncherComponentTheme.TextMuted);
    }

    private static StyleBoxFlat BuildButtonStateStyle(
        Color body,
        Color border,
        int radius,
        int width,
        bool filled
    )
    {
        var style = filled
            ? LauncherStyleBoxes.MakeFilled(body, radius)
            : LauncherStyleBoxes.MakeOutline(body, radius, width);
        style.BorderColor = border;
        style.BorderWidthBottom = width;
        style.BorderWidthLeft = width;
        style.BorderWidthRight = width;
        style.BorderWidthTop = width;
        return style;
    }
}
