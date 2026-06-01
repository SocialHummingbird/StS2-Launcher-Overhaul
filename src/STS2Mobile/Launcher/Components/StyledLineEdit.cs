using Godot;

namespace STS2Mobile.Launcher.Components;

internal sealed class StyledLineEdit : LineEdit
{
    internal StyledLineEdit(string placeholder, float scale, bool secret = false)
    {
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
        FocusEntered += ShowAndroidKeyboard;
        GuiInput += inputEvent =>
        {
            if (ShouldShowKeyboard(inputEvent))
                ShowAndroidKeyboard();
        };
    }

    private void ShowAndroidKeyboard()
    {
        try
        {
            DisplayServer.VirtualKeyboardShow(
                Text,
                new Rect2(GlobalPosition, Size),
                DisplayServer.VirtualKeyboardType.Default,
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
