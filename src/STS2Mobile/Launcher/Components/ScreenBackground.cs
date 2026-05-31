using Godot;

namespace STS2Mobile.Launcher.Components;

internal sealed class ScreenBackground : ColorRect
{
    internal ScreenBackground()
    {
        Color = LauncherComponentTheme.ScreenBackground;
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
    }
}
