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
        LauncherLayoutMode mode,
        bool touchOptimized,
        bool compact,
        bool compactStackedActionRows
    )
    {
        ViewportSize = viewportSize;
        Scale = scale;
        PanelWidthRatio = panelWidthRatio;
        PanelHeightRatio = panelHeightRatio;
        ContentMaxWidth = contentMaxWidth;
        Mode = mode;
        TouchOptimized = touchOptimized;
        Compact = compact;
        CompactStackedActionRows = compactStackedActionRows;
    }

    internal Vector2 ViewportSize { get; }
    internal float Scale { get; }
    internal float PanelWidthRatio { get; }
    internal float PanelHeightRatio { get; }
    internal int ContentMaxWidth { get; }
    internal LauncherLayoutMode Mode { get; }
    internal bool TouchOptimized { get; }
    internal bool Compact { get; }
    internal bool CompactStackedActionRows { get; }

    internal static LauncherLayoutProfile ForViewport(Vector2 viewportSize)
        => ForViewport(viewportSize, OperatingSystem.IsAndroid());

    internal static LauncherLayoutProfile ForViewport(
        Vector2 viewportSize,
        bool touchOptimized
    )
    {
        var safeViewport = viewportSize == Vector2.Zero ? new Vector2(1920, 1080) : viewportSize;
        var shortEdge = Math.Max(1f, Math.Min(safeViewport.X, safeViewport.Y));
        var longEdge = Math.Max(safeViewport.X, safeViewport.Y);
        var aspect = longEdge / shortEdge;
        var mode = ResolveMode(safeViewport, shortEdge, aspect, touchOptimized);
        var compact = mode != LauncherLayoutMode.Wide;
        var scaleCeiling = compact ? 1.34f : 1.22f;
        var scaleFloor = compact
            ? (touchOptimized ? AndroidCompactTouchScaleFloor : CompactScaleFloor)
            : touchOptimized
                ? CompactScaleFloor
                : WideScaleFloor;
        var viewportScale = Math.Clamp(shortEdge / ReferenceShortEdge, scaleFloor, scaleCeiling);
        var scale = OperatingSystem.IsAndroid() && touchOptimized
            ? ResolveAndroidScale(viewportScale)
            : viewportScale;
        var panelWidth = compact ? 1.0f : touchOptimized ? 0.96f : 0.78f;
        var panelHeight = compact ? 1.0f : touchOptimized ? 0.96f : 0.88f;
        var availablePanelWidth = safeViewport.X * panelWidth * 0.92f;
        var contentMaxWidth = compact
            ? Math.Max(1, (int)Math.Min(safeViewport.X * 0.96f, 1600f))
            : touchOptimized
                ? Math.Max(860, (int)Math.Min(availablePanelWidth, 1280f))
                : Math.Max(720, (int)Math.Min(availablePanelWidth, 1120f));
        var compactStackedActionRows = compact
            && contentMaxWidth < MathF.Round(CompactStackedActionRowsWidth * scale);

        return new LauncherLayoutProfile(
            safeViewport,
            scale,
            panelWidth,
            panelHeight,
            contentMaxWidth,
            mode,
            touchOptimized,
            compact,
            compactStackedActionRows
        );
    }

    private static float ResolveAndroidScale(float viewportScale)
    {
        var dpi = DisplayServer.ScreenGetDpi();
        if (dpi <= 0)
            return viewportScale;

        return Math.Clamp(Math.Max(viewportScale, dpi / 160f), viewportScale, 3f);
    }

    private static LauncherLayoutMode ResolveMode(
        Vector2 viewport,
        float shortEdge,
        float aspect,
        bool touchOptimized
    )
    {
        if (!touchOptimized)
            return viewport.X < 900f
                ? LauncherLayoutMode.PhonePortrait
                : LauncherLayoutMode.Wide;

        var foldableOrTablet = shortEdge >= 1280f && aspect <= 1.7f;
        if (foldableOrTablet)
            return LauncherLayoutMode.Wide;

        return viewport.Y >= viewport.X
            ? LauncherLayoutMode.PhonePortrait
            : LauncherLayoutMode.PhoneLandscape;
    }

    public override string ToString()
        => $"Viewport={ViewportSize} Scale={Scale:0.00} Mode={Mode} Touch={TouchOptimized} CompactStackedActionRows={CompactStackedActionRows}";
}
