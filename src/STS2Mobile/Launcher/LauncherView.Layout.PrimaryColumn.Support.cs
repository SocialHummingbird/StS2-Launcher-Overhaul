using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static Control BuildCompactBottomScrollSpacer(float scale)
        => new Control
        {
            CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(72, scale)
            ),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
}
