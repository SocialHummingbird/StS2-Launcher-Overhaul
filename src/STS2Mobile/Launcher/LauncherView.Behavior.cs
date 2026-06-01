using System;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    internal void UpdateKeyboardOffset()
    {
        var kbHeight = DisplayServer.VirtualKeyboardGetHeight();
        if (kbHeight > 0)
        {
            var windowSize = DisplayServer.WindowGetSize();
            var vpSize = _parent.GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920, 1080);
            var scale = vpSize.Y / windowSize.Y;
            var offset = kbHeight * scale * 0.5f;
            _panel.Position = new Vector2(_panel.Position.X, _panelBaseY - offset);
            return;
        }

        _panel.Position = new Vector2(_panel.Position.X, _panelBaseY);
    }

    internal void ShowConfirmation(string message, Action onConfirmed)
    {
        _parent.AddChild(BuildConfirmationDialog(message, _scale, onConfirmed));
    }

    private void DismissKeyboard(InputEvent ev)
    {
        if (ev is InputEventMouseButton { Pressed: true } or InputEventScreenTouch { Pressed: true })
            _parent.GetViewport()?.GuiReleaseFocus();
    }
}
