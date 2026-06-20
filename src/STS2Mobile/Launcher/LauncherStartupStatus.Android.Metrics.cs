using System;
using Godot;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupStatus
{
    private static float CalculateAndroidScale(Vector2 viewportSize)
    {
        var shortEdge = Math.Max(1f, Math.Min(viewportSize.X, viewportSize.Y));
        return Math.Clamp(shortEdge / ReferenceShortEdge, AndroidMinimumScale, AndroidMaximumScale);
    }

    private static float CalculateAndroidPanelWidth(Vector2 viewportSize, float margin)
    {
        var safeWidth = Math.Max(1f, viewportSize.X - margin * 2f);
        return safeWidth * AndroidWidthRatio;
    }
}
