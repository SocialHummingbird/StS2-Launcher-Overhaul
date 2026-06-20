using System;
using Godot;

namespace STS2Mobile.Launcher;

internal readonly struct LauncherLayoutProfile
{
    private const float ReferenceShortEdge = 900f;
    private const float CompactScaleFloor = 0.94f;
    private const float AndroidCompactTouchScaleFloor = 1.06f;
    private const float WideScaleFloor = 0.82f;
    private const float CompactStackedActionRowsWidth = 560f;

    private LauncherLayoutProfile(
        Vector2 viewportSize,
        float scale,
        float panelWidthRatio,
        float panelHeightRatio,
        int contentMaxWidth,
        bool compact,
        bool compactStackedActionRows
    )
    {
        ViewportSize = viewportSize;
        Scale = scale;
        PanelWidthRatio = panelWidthRatio;
        PanelHeightRatio = panelHeightRatio;
        ContentMaxWidth = contentMaxWidth;
        Compact = compact;
        CompactStackedActionRows = compactStackedActionRows;
    }

    internal Vector2 ViewportSize { get; }
    internal float Scale { get; }
    internal float PanelWidthRatio { get; }
    internal float PanelHeightRatio { get; }
    internal int ContentMaxWidth { get; }
    internal bool Compact { get; }
    internal bool CompactStackedActionRows { get; }

    internal static LauncherLayoutProfile ForViewport(Vector2 viewportSize)
    {
        var safeViewport = viewportSize == Vector2.Zero ? new Vector2(1920, 1080) : viewportSize;
        var shortEdge = Math.Max(1f, Math.Min(safeViewport.X, safeViewport.Y));
        var longEdge = Math.Max(safeViewport.X, safeViewport.Y);
        var aspect = longEdge / shortEdge;
        var mobileShell = OperatingSystem.IsAndroid();
        var compact = mobileShell || shortEdge <= 1150f || aspect >= 1.55f;
        var scaleCeiling = compact ? 1.34f : 1.22f;
        var scaleFloor = compact
            ? (mobileShell ? AndroidCompactTouchScaleFloor : CompactScaleFloor)
            : WideScaleFloor;
        var scale = Math.Clamp(shortEdge / ReferenceShortEdge, scaleFloor, scaleCeiling);
        var panelWidth = compact ? 1.0f : 0.78f;
        var panelHeight = compact ? 1.0f : 0.88f;
        var contentMaxWidth = compact
            ? Math.Max(1, (int)Math.Min(safeViewport.X * 0.96f, 1600f))
            : Math.Max(860, (int)Math.Min(safeViewport.X * 0.84f, 1180f));
        var compactStackedActionRows = compact
            && contentMaxWidth < MathF.Round(CompactStackedActionRowsWidth * scale);

        return new LauncherLayoutProfile(
            safeViewport,
            scale,
            panelWidth,
            panelHeight,
            contentMaxWidth,
            compact,
            compactStackedActionRows
        );
    }

    public override string ToString()
        => $"Viewport={ViewportSize} Scale={Scale:0.00} Compact={Compact} CompactStackedActionRows={CompactStackedActionRows}";
}
