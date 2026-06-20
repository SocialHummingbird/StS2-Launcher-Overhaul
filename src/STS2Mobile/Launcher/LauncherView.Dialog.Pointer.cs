using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static bool TryGetPressedPointerPosition(InputEvent ev, out Vector2 position)
    {
        switch (ev)
        {
            case InputEventScreenTouch { Pressed: true } touch:
                position = touch.Position;
                return true;
            case InputEventMouseButton { Pressed: true } mouse:
                position = mouse.Position;
                return true;
            default:
                position = Vector2.Zero;
                return false;
        }
    }
}
