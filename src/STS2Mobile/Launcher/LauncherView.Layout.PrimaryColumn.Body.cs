using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static LauncherViewPrimaryBody BuildPrimaryColumnBody(
        LauncherLayoutProfile profile,
        VBoxContainer root
    )
    {
        var scale = profile.Scale;
        var leftScroll = new ScrollContainer();
        leftScroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        leftScroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        leftScroll.FollowFocus = true;
        root.AddChild(leftScroll);

        var leftFrame = new MarginContainer();
        leftFrame.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        leftFrame.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        leftScroll.AddChild(leftFrame);

        var left = new VBoxContainer();
        left.SizeFlagsHorizontal = profile.Compact
            ? Control.SizeFlags.ExpandFill
            : Control.SizeFlags.ShrinkCenter;
        left.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        left.CustomMinimumSize = new Vector2(
            profile.Compact ? 0 : profile.ContentMaxWidth,
            0
        );
        left.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(
                profile.Compact
                    ? LauncherViewLayoutMetrics.CompactPrimaryColumnSeparation
                    : LauncherViewLayoutMetrics.PrimaryColumnSeparation,
                scale
            )
        );
        leftFrame.AddChild(left);

        return new LauncherViewPrimaryBody(leftScroll, left);
    }
}
