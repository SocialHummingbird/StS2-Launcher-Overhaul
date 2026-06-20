using System;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private const int CompactScrollAnchorTopPadding = 14;

    private void ScrollCompactPrimaryTo(Control target)
    {
        if (!_profile.Compact || !GodotObject.IsInstanceValid(target))
            return;

        _compactScrollAnchorTarget = target;
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(PrimaryScroll)
                || !GodotObject.IsInstanceValid(target)
                || !PrimaryScroll.IsInsideTree()
                || !target.IsInsideTree()
                || !target.IsVisibleInTree())
            {
                return;
            }

            PrimaryScroll.EnsureControlVisible(target);
            Callable.From(() => ApplyCompactScrollAnchorPadding(target)).CallDeferred();
        }).CallDeferred();
    }

    private void ApplyCompactScrollAnchorPadding(Control target)
    {
        if (!_profile.Compact || !PrimaryScroll.IsInsideTree() || !target.IsInsideTree() || !target.IsVisibleInTree())
            return;

        var scrollTop = PrimaryScroll.GetGlobalRect().Position.Y;
        var targetTop = target.GetGlobalRect().Position.Y;
        var anchoredScroll = PrimaryScroll.ScrollVertical
            + targetTop
            - scrollTop
            - LauncherViewLayoutMetrics.ScaleInt(CompactScrollAnchorTopPadding, _scale);
        PrimaryScroll.ScrollVertical = Math.Max(0, (int)MathF.Round(anchoredScroll));
    }
}
