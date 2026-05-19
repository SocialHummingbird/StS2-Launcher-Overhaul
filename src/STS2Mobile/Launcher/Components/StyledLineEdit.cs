using Godot;

namespace STS2Mobile.Launcher.Components;

public class StyledLineEdit : LineEdit
{
    public StyledLineEdit(string placeholder, float scale, bool secret = false)
    {
        PlaceholderText = placeholder;
        Secret = secret;
        CustomMinimumSize = new Vector2(0, (int)(38 * scale));
        AddThemeFontSizeOverride("font_size", (int)(14 * scale));
        ContextMenuEnabled = true;
        ShortcutKeysEnabled = true;
        SelectAllOnFocus = true;
        FocusEntered += ShowAndroidKeyboard;
        GuiInput += OnGuiInput;
    }

    private void OnGuiInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { Pressed: true } or InputEventScreenTouch { Pressed: true })
            ShowAndroidKeyboard();
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
}
