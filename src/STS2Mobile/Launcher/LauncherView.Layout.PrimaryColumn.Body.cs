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
        var leftScroll = new ScrollContainer
        {
            Name = "LauncherPrimaryScroll",
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        leftScroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        leftScroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        leftScroll.FollowFocus = true;
        root.AddChild(leftScroll);

        var leftFrame = new MarginContainer();
        leftFrame.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        leftFrame.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        leftScroll.AddChild(leftFrame);

        var left = new VBoxContainer();
        left.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        left.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
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
