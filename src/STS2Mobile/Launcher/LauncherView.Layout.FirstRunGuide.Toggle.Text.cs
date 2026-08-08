using Godot;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static void SetCompactSafeFlowToggleText(
        Button toggle,
        float scale,
        string title,
        string detail
    )
        => CompactButtonDetailLabels.Apply(
            toggle,
            $"{title}\n{detail}",
            scale,
            enabled: true,
            CompactSafeFlowToggleLabels
        );
}
