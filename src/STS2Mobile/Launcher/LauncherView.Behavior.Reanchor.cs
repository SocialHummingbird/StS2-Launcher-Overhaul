using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private void ReanchorCompactScrollTargetAfterViewportChange()
    {
        if (!_profile.Compact || !GodotObject.IsInstanceValid(PrimaryScroll))
            return;

        if (DisplayServer.VirtualKeyboardGetHeight() > 0)
        {
            var focusOwner = _parent.GetViewport()?.GuiGetFocusOwner();
            if (focusOwner != null && PrimaryScroll.IsAncestorOf(focusOwner))
                return;
        }

        var target = CompactViewportReanchorTarget();
        if (target == null)
            return;

        ScrollCompactPrimaryTo(target);
    }

    private Control CompactViewportReanchorTarget()
    {
        if (IsUsableCompactAnchor(_compactScrollAnchorTarget))
            return _compactScrollAnchorTarget;

        if (IsUsableCompactAnchor(_compactCurrentTaskTarget))
            return _compactCurrentTaskTarget;

        return IsUsableCompactAnchor(FirstRunGuide) ? FirstRunGuide : null;
    }

    private static bool IsUsableCompactAnchor(Control control)
        => GodotObject.IsInstanceValid(control)
            && control.IsInsideTree()
            && control.IsVisibleInTree();
}
