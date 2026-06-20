using System;
using Godot;

namespace STS2Mobile.Launcher.Components;

internal sealed class StyledLineEdit : LineEdit
{
    private readonly DisplayServer.VirtualKeyboardType _keyboardType;

    internal StyledLineEdit(
        string placeholder,
        float scale,
        bool secret = false,
        DisplayServer.VirtualKeyboardType keyboardType = DisplayServer.VirtualKeyboardType.Default
    )
    {
        _keyboardType = keyboardType;
        PlaceholderText = placeholder;
        Secret = secret;
        CustomMinimumSize = new Vector2(
            0,
            LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.LineEditHeight)
        );
        AddThemeFontSizeOverride(
            LauncherComponentTheme.FontSize,
            LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.LineEditFontSize)
        );
        ContextMenuEnabled = true;
        ShortcutKeysEnabled = true;
        SelectAllOnFocus = true;
        ApplyTheme(scale);
        FocusEntered += ShowAndroidKeyboard;
        GuiInput += inputEvent =>
        {
            if (ShouldShowKeyboard(inputEvent))
                ShowAndroidKeyboard();
        };
    }

    private void ApplyTheme(float scale)
    {
        var radius = LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.LineEditRadius);
        var borderWidth = Math.Max(1, LauncherComponentTheme.ScaleInt(scale, 1));
        AddThemeStyleboxOverride(
            LauncherComponentTheme.StateNormal,
            BuildStyleBox(LauncherComponentTheme.LineEditNormalBorder, radius, borderWidth, scale)
        );
        AddThemeStyleboxOverride(
            "focus",
            BuildStyleBox(LauncherComponentTheme.LineEditFocusBorder, radius, borderWidth + 1, scale)
        );
        AddThemeStyleboxOverride(
            "read_only",
            BuildStyleBox(LauncherComponentTheme.LineEditNormalBorder.Darkened(0.2f), radius, borderWidth, scale)
        );
        AddThemeColorOverride("font_color", LauncherComponentTheme.TextPrimary);
        AddThemeColorOverride("font_placeholder_color", LauncherComponentTheme.TextMuted);
        AddThemeColorOverride("caret_color", LauncherComponentTheme.CyanAccent);
        AddThemeColorOverride("selection_color", new Color(0.04f, 0.84f, 0.95f, 0.28f));
    }

    private static StyleBoxFlat BuildStyleBox(Color border, int radius, int borderWidth, float scale)
    {
        var style = LauncherStyleBoxes.MakeFilled(LauncherComponentTheme.LineEditBackground, radius);
        style.BorderColor = border;
        style.SetBorderWidthAll(borderWidth);
        var horizontalPadding = LauncherComponentTheme.ScaleInt(
            scale,
            LauncherComponentTheme.LineEditHorizontalPadding
        );
        style.ContentMarginLeft = horizontalPadding;
        style.ContentMarginRight = horizontalPadding;
        style.ContentMarginTop = LauncherComponentTheme.ScaleInt(scale, 6);
        style.ContentMarginBottom = LauncherComponentTheme.ScaleInt(scale, 6);
        return style;
    }

    private void ShowAndroidKeyboard()
    {
        try
        {
            DisplayServer.VirtualKeyboardShow(
                Text,
                new Rect2(GlobalPosition, Size),
                _keyboardType,
                MaxLength,
                CaretColumn,
                CaretColumn
            );
        }
        catch
        {
            // Some desktop/editor backends do not expose a virtual keyboard.
        }
    }

    private static bool ShouldShowKeyboard(InputEvent inputEvent)
        => inputEvent
            is InputEventMouseButton { Pressed: true }
                or InputEventScreenTouch { Pressed: true };
}
