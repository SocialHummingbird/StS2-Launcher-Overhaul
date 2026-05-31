using Godot;

namespace STS2Mobile.Launcher.Components;

internal sealed class StyledProgressBar : ProgressBar
{
    internal StyledProgressBar(float scale)
    {
        CustomMinimumSize = new Vector2(
            0,
            LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.ProgressBarHeight)
        );
    }
}
