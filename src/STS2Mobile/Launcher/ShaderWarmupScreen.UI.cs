using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private const float ReferenceShortEdge = 720f;
    private const float MinimumScale = 0.85f;
    private const float AndroidMinimumScale = 1.06f;
    private const float MaximumScale = 1.6f;
    private const float MinimumPanelWidth = 420f;
    private const float AndroidMinimumPanelWidth = 320f;
    private const float MaximumPanelWidth = 980f;
    private const float PanelHeightRatio = 0.72f;
    private const float AndroidPanelWidthRatio = 0.94f;

    private void BuildUI(Vector2 vpSize)
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        var androidCompact = OperatingSystem.IsAndroid();
        var scale = CalculateAdaptiveScale(vpSize, androidCompact);

        var bg = new ScreenBackground();
        AddChild(bg);

        var panel = new StyledPanel(
            scale,
            widthRatio: CalculatePanelWidthRatio(vpSize, androidCompact),
            compact: androidCompact
        );
        panel.UpdateSizeFromViewport(
            CalculateWarmupPanelSize(vpSize, androidCompact),
            PanelHeightRatio
        );
        AddChild(panel);

        _statusLabel = new StyledLabel(Message.CompilingStatus, scale, fontSize: 20);
        _statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        panel.AddContent(_statusLabel);

        _progressBar = new StyledProgressBar(scale, androidCompact);
        _progressBar.MinValue = 0;
        _progressBar.MaxValue = 100;
        _progressBar.Value = 0;
        _progressBar.ShowPercentage = true;
        panel.AddContent(_progressBar);

        _detailLabel = new StyledLabel(Message.InitialDetail, scale, fontSize: 14);
        _detailLabel.Modulate = new Color(0.7f, 0.7f, 0.7f);
        _detailLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        panel.AddContent(_detailLabel);
    }

    private static float CalculateAdaptiveScale(Vector2 vpSize, bool androidCompact)
    {
        var shortEdge = Math.Max(1f, Math.Min(vpSize.X, vpSize.Y));
        return Math.Clamp(
            shortEdge / ReferenceShortEdge,
            androidCompact ? AndroidMinimumScale : MinimumScale,
            MaximumScale
        );
    }

    private static float CalculatePanelWidthRatio(Vector2 vpSize, bool androidCompact)
    {
        if (androidCompact)
            return AndroidPanelWidthRatio;

        var aspect = Math.Max(vpSize.X, vpSize.Y) / Math.Max(1f, Math.Min(vpSize.X, vpSize.Y));
        return aspect >= 2.0f ? 0.62f : 0.56f;
    }

    private static Vector2 CalculateWarmupPanelSize(Vector2 vpSize, bool androidCompact)
    {
        var safeMargin = CalculateSafeMargin(vpSize);
        var safeWidth = Math.Max(1f, vpSize.X - safeMargin * 2f);
        var widthRatio = CalculatePanelWidthRatio(vpSize, androidCompact);
        var minimumWidth = androidCompact ? AndroidMinimumPanelWidth : MinimumPanelWidth;
        var maximumWidth = MaximumPanelWidth / widthRatio;
        if (androidCompact)
            maximumWidth = Math.Min(safeWidth, maximumWidth);

        return new Vector2(
            Math.Clamp(
                safeWidth,
                Math.Min(minimumWidth, maximumWidth),
                maximumWidth
            ),
            Math.Max(1f, vpSize.Y - safeMargin * 2f)
        );
    }

    private static float CalculateSafeMargin(Vector2 vpSize)
    {
        var shortEdge = Math.Min(vpSize.X, vpSize.Y);
        return Math.Clamp(shortEdge * 0.04f, 16f, 48f);
    }
}
