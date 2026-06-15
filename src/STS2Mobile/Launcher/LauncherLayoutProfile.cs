using System;
using Godot;

namespace STS2Mobile.Launcher;

internal readonly struct LauncherLayoutProfile
{
    private const float ReferenceShortEdge = 900f;

    private LauncherLayoutProfile(
        Vector2 viewportSize,
        float scale,
        float panelWidthRatio,
        float panelHeightRatio,
        int contentMaxWidth,
        bool compact
    )
    {
        ViewportSize = viewportSize;
        Scale = scale;
        PanelWidthRatio = panelWidthRatio;
        PanelHeightRatio = panelHeightRatio;
        ContentMaxWidth = contentMaxWidth;
        Compact = compact;
    }

    internal Vector2 ViewportSize { get; }
    internal float Scale { get; }
    internal float PanelWidthRatio { get; }
    internal float PanelHeightRatio { get; }
    internal int ContentMaxWidth { get; }
    internal bool Compact { get; }

    internal static LauncherLayoutProfile ForViewport(Vector2 viewportSize)
    {
        var safeViewport = viewportSize == Vector2.Zero ? new Vector2(1920, 1080) : viewportSize;
        var shortEdge = Math.Max(1f, Math.Min(safeViewport.X, safeViewport.Y));
        var longEdge = Math.Max(safeViewport.X, safeViewport.Y);
        var aspect = longEdge / shortEdge;
        var compact = shortEdge <= 1150f || aspect >= 1.55f;
        var scaleCeiling = compact ? 1.08f : 1.22f;
        var scale = Math.Clamp(shortEdge / ReferenceShortEdge, 0.82f, scaleCeiling);
        var panelWidth = compact ? 0.98f : 0.78f;
        var panelHeight = compact ? 0.98f : 0.88f;
        var contentMaxWidth = compact
            ? Math.Max(1, (int)Math.Min(safeViewport.X * 0.94f, 1120f))
            : Math.Max(860, (int)Math.Min(safeViewport.X * 0.84f, 1180f));

        return new LauncherLayoutProfile(
            safeViewport,
            scale,
            panelWidth,
            panelHeight,
            contentMaxWidth,
            compact
        );
    }

    public override string ToString()
        => $"Viewport={ViewportSize} Scale={Scale:0.00} Compact={Compact}";
}
