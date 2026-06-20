using System;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private const float CompactDiagnosticsLogViewportHeightRatio = 0.28f;
    private const int CompactDiagnosticsLogMinHeight = 220;
    private const int CompactDiagnosticsLogMaxHeight = 340;

    private static int DiagnosticsLogHeight(LauncherLayoutProfile profile)
    {
        if (!profile.Compact)
            return LauncherViewLayoutMetrics.ScaleInt(180, profile.Scale);

        var viewportHeight = (int)MathF.Round(
            profile.ViewportSize.Y * CompactDiagnosticsLogViewportHeightRatio,
            MidpointRounding.AwayFromZero
        );
        var minHeight = LauncherViewLayoutMetrics.ScaleInt(CompactDiagnosticsLogMinHeight, profile.Scale);
        var maxHeight = LauncherViewLayoutMetrics.ScaleInt(CompactDiagnosticsLogMaxHeight, profile.Scale);
        return Math.Clamp(viewportHeight, minHeight, maxHeight);
    }

    private void UpdateDiagnosticsLogViewport(Vector2 viewportSize)
    {
        if (!GodotObject.IsInstanceValid(Log))
            return;

        var profile = viewportSize.X > 0f && viewportSize.Y > 0f
            ? LauncherLayoutProfile.ForViewport(viewportSize)
            : _profile;
        Log.CustomMinimumSize = new Vector2(0, DiagnosticsLogHeight(profile));
        if (_profile.Compact && DiagnosticsDrawer.Visible)
            ScrollCompactPrimaryTo(DiagnosticsDrawer);
    }
}
