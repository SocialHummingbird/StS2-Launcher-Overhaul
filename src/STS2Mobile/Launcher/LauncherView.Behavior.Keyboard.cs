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
            var scale = windowSize.Y > 0 ? vpSize.Y / windowSize.Y : 1f;
            var maxOffset = Math.Max(0f, vpSize.Y * 0.42f);
            _keyboardOffset = Math.Min(kbHeight * scale * 0.85f, maxOffset);
            _panel.Position = new Vector2(_panel.Position.X, _panelBaseY - _keyboardOffset);
            ScrollFocusedInputAboveKeyboard();
            return;
        }

        _keyboardOffset = 0f;
        _keyboardFocusScrollTarget = null;
        _keyboardFocusScrollOffset = -1f;
        _panel.Position = new Vector2(_panel.Position.X, _panelBaseY);
    }

    private void ScrollFocusedInputAboveKeyboard()
    {
        var focusOwner = _parent.GetViewport()?.GuiGetFocusOwner();
        if (focusOwner == null || !PrimaryScroll.IsAncestorOf(focusOwner))
            return;

        if (focusOwner == _keyboardFocusScrollTarget
            && Math.Abs(_keyboardFocusScrollOffset - _keyboardOffset) < 1f)
            return;

        _keyboardFocusScrollTarget = focusOwner;
        _keyboardFocusScrollOffset = _keyboardOffset;
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(PrimaryScroll)
                || !GodotObject.IsInstanceValid(focusOwner)
                || !PrimaryScroll.IsInsideTree()
                || !focusOwner.IsInsideTree())
            {
                return;
            }

            PrimaryScroll.EnsureControlVisible(focusOwner);
        }).CallDeferred();
    }

    private void DismissKeyboard(InputEvent ev)
    {
        if (ev is InputEventMouseButton { Pressed: true } or InputEventScreenTouch { Pressed: true })
            _parent.GetViewport()?.GuiReleaseFocus();
    }
}
