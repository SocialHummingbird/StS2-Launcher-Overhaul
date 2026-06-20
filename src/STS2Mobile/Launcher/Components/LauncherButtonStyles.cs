using System;
using Godot;

namespace STS2Mobile.Launcher.Components;

internal static class LauncherButtonStyles
{
    private const string FontColor = "font_color";
    private const string FontHoverColor = "font_hover_color";
    private const string FontPressedColor = "font_pressed_color";
    private const string FontDisabledColor = "font_disabled_color";
    private const string PopupHover = "hover";
    private const string PopupHorizontalSeparation = "h_separation";
    private const string PopupVerticalSeparation = "v_separation";
    private const string PopupItemStartPadding = "item_start_padding";
    private const string PopupItemEndPadding = "item_end_padding";
    private const int DropdownPopupHorizontalPadding = 14;
    private const int DropdownPopupHorizontalSeparation = 8;
    private const int DropdownPopupVerticalSeparation = 8;
    private const int CompactDropdownPopupHorizontalPadding = 20;
    private const int CompactDropdownPopupHorizontalSeparation = 12;
    private const int CompactDropdownPopupVerticalSeparation = 16;

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

    internal static void ApplyDropdownAction(
        OptionButton button,
        float scale,
        int fontSize,
        bool compact = false
    )
    {
        ApplySupportAction(button, scale);
        var scaledFontSize = LauncherComponentTheme.ScaleInt(scale, fontSize);
        button.AddThemeFontSizeOverride(LauncherComponentTheme.FontSize, scaledFontSize);

        var popup = button.GetPopup();
        popup.AddThemeFontSizeOverride(LauncherComponentTheme.FontSize, scaledFontSize);
        popup.AddThemeConstantOverride(
            PopupVerticalSeparation,
            LauncherComponentTheme.ScaleInt(
                scale,
                compact
                    ? CompactDropdownPopupVerticalSeparation
                    : DropdownPopupVerticalSeparation
            )
        );
        popup.AddThemeConstantOverride(
            PopupHorizontalSeparation,
            LauncherComponentTheme.ScaleInt(
                scale,
                compact
                    ? CompactDropdownPopupHorizontalSeparation
                    : DropdownPopupHorizontalSeparation
            )
        );
        popup.AddThemeConstantOverride(
            PopupItemStartPadding,
            LauncherComponentTheme.ScaleInt(
                scale,
                compact
                    ? CompactDropdownPopupHorizontalPadding
                    : DropdownPopupHorizontalPadding
            )
        );
        popup.AddThemeConstantOverride(
            PopupItemEndPadding,
            LauncherComponentTheme.ScaleInt(
                scale,
                compact
                    ? CompactDropdownPopupHorizontalPadding
                    : DropdownPopupHorizontalPadding
            )
        );
        popup.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            LauncherStyleBoxes.MakeFilled(
                LauncherComponentTheme.PanelBackground,
                LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.ButtonRadius)
            )
        );
        popup.AddThemeStyleboxOverride(
            PopupHover,
            LauncherStyleBoxes.MakeFilled(
                LauncherComponentTheme.ButtonHover,
                LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.ButtonRadius)
            )
        );
    }

    internal static void ApplyCloudPullAction(Button button, float scale)
        => Apply(
            button,
            scale,
            new Color(0.07f, 0.18f, 0.15f),
            new Color(0.24f, 0.7f, 0.36f),
            LauncherComponentTheme.TextPrimary,
            borderWidth: 2
        );

    internal static void ApplyDangerAction(Button button, float scale)
        => Apply(
            button,
            scale,
            new Color(0.22f, 0.07f, 0.07f),
            new Color(0.92f, 0.24f, 0.18f),
            LauncherComponentTheme.TextPrimary,
            borderWidth: 2
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
        button.ClipText = true;
        button.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;

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

        var disabled = filled
            ? LauncherStyleBoxes.MakeFilled(body.Darkened(0.25f), radius)
            : LauncherStyleBoxes.MakeOutline(border.Darkened(0.28f), radius, width);
        disabled.BorderColor = border.Darkened(0.3f);
        disabled.BorderWidthBottom = width;
        disabled.BorderWidthLeft = width;
        disabled.BorderWidthRight = width;
        disabled.BorderWidthTop = width;

        button.AddThemeStyleboxOverride(LauncherComponentTheme.StateNormal, normal);
        button.AddThemeStyleboxOverride(LauncherComponentTheme.StateHover, hover);
        button.AddThemeStyleboxOverride(LauncherComponentTheme.StatePressed, pressed);
        button.AddThemeStyleboxOverride(LauncherComponentTheme.StateDisabled, disabled);
        button.AddThemeColorOverride(FontColor, font);
        button.AddThemeColorOverride(FontHoverColor, font.Lightened(0.08f));
        button.AddThemeColorOverride(FontPressedColor, font.Darkened(0.08f));
        button.AddThemeColorOverride(FontDisabledColor, LauncherComponentTheme.TextMuted);
    }
}
