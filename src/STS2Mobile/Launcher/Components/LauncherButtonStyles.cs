using System;
using Godot;

namespace STS2Mobile.Launcher.Components;

internal static class LauncherButtonStyles
{
    private const string FontColor = "font_color";
    private const string FontHoverColor = "font_hover_color";
    private const string FontPressedColor = "font_pressed_color";
    private const string FontDisabledColor = "font_disabled_color";

    internal static void ApplyPrimaryAction(Button button, float scale)
        => Apply(
            button,
            scale,
            LauncherComponentTheme.OrangeAccent,
            LauncherComponentTheme.OrangeHot,
            LauncherComponentTheme.DarkInk,
            borderWidth: 2
        );

    internal static void ApplySafeAction(Button button, float scale)
        => Apply(
            button,
            scale,
            LauncherComponentTheme.CyanDim,
            LauncherComponentTheme.CyanAccent,
            LauncherComponentTheme.CyanAccent,
            borderWidth: 2,
            filled: false
        );

    internal static void ApplySupportAction(Button button, float scale)
        => Apply(
            button,
            scale,
            LauncherComponentTheme.ButtonNormal,
            LauncherComponentTheme.CyanDim,
            LauncherComponentTheme.TextPrimary,
            borderWidth: 1
        );

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
        var radius = LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.ButtonRadius);
        var width = Math.Max(1, LauncherComponentTheme.ScaleInt(scale, borderWidth));
        var normal = filled
            ? LauncherStyleBoxes.MakeFilled(body, radius)
            : LauncherStyleBoxes.MakeOutline(border, radius, width);
        normal.BorderColor = border;
        normal.BorderWidthBottom = width;
        normal.BorderWidthLeft = width;
        normal.BorderWidthRight = width;
        normal.BorderWidthTop = width;

        var hover = filled
            ? LauncherStyleBoxes.MakeFilled(body.Lightened(0.16f), radius)
            : LauncherStyleBoxes.MakeOutline(border.Lightened(0.1f), radius, width);
        hover.BorderColor = border.Lightened(0.15f);
        hover.BorderWidthBottom = width;
        hover.BorderWidthLeft = width;
        hover.BorderWidthRight = width;
        hover.BorderWidthTop = width;

        var pressed = filled
            ? LauncherStyleBoxes.MakeFilled(body.Darkened(0.18f), radius)
            : LauncherStyleBoxes.MakeOutline(border.Darkened(0.08f), radius, width);
        pressed.BorderColor = border.Darkened(0.08f);
        pressed.BorderWidthBottom = width;
        pressed.BorderWidthLeft = width;
        pressed.BorderWidthRight = width;
        pressed.BorderWidthTop = width;

        button.AddThemeStyleboxOverride(LauncherComponentTheme.StateNormal, normal);
        button.AddThemeStyleboxOverride(LauncherComponentTheme.StateHover, hover);
        button.AddThemeStyleboxOverride(LauncherComponentTheme.StatePressed, pressed);
        button.AddThemeColorOverride(FontColor, font);
        button.AddThemeColorOverride(FontHoverColor, font.Lightened(0.08f));
        button.AddThemeColorOverride(FontPressedColor, font.Darkened(0.08f));
        button.AddThemeColorOverride(FontDisabledColor, LauncherComponentTheme.TextMuted);
    }
}
