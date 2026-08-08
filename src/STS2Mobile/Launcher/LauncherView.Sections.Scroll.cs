using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private void ScrollCompactPrimaryTo(Control target)
    {
        if (!_profile.Compact || !GodotObject.IsInstanceValid(target))
            return;

        _compactScrollAnchorTarget = target;
        PrimaryScroll.ScrollVertical = 0;
    }
}
