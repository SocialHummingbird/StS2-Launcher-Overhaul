using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private const int CompactBottomScrollSpacerHeight = 180;

    private static Control BuildCompactBottomScrollSpacer(float scale)
        => new Control
        {
            CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(CompactBottomScrollSpacerHeight, scale)
            ),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
}
