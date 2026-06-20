using System;
using Godot;

namespace STS2Mobile.Launcher.Components;

internal sealed class StyledPanel : CenterContainer
{
    private const int CompactPanelHorizontalMargin = 10;
    private const int CompactPanelTopMargin = 10;
    private const int CompactPanelBottomMargin = 12;
    private const float MaxWidth = 1400f;
    private const float MaxHeight = 2200f;

    private VBoxContainer Content { get; }

    internal StyledPanel(float scale, float widthRatio = 0.7f, bool compact = false)
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        var vpSize = new Vector2(1920, 1080); // fallback, overridden after AddChild
        var panelContainer = new PanelContainer();
        panelContainer.CustomMinimumSize = new Vector2(vpSize.X * widthRatio, 0);

        panelContainer.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildStyle(scale, compact)
        );
        AddChild(panelContainer);

        Content = new VBoxContainer();
        Content.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        Content.AddThemeConstantOverride("separation", (int)(10 * scale));
        panelContainer.AddChild(Content);

        // Defer viewport-based sizing until in tree
        _panelContainer = panelContainer;
        _widthRatio = widthRatio;
        _compact = compact;
    }

    private readonly PanelContainer _panelContainer;
    private readonly float _widthRatio;
    private readonly bool _compact;

    internal void AddContent(Control control)
        => Content.AddChild(control);

    internal void OnPanelGuiInput(Action<InputEvent> handler)
        => _panelContainer.GuiInput += input => handler(input);

    internal void UpdateSizeFromViewport(Vector2 vpSize)
        => _panelContainer.CustomMinimumSize = new Vector2(
            ConstrainWidth(vpSize),
            ConstrainHeight(vpSize, 0.85f)
        );

    internal void UpdateSizeFromViewport(Vector2 vpSize, float heightRatio)
        => _panelContainer.CustomMinimumSize = new Vector2(
            ConstrainWidth(vpSize),
            ConstrainHeight(vpSize, heightRatio)
        );

    private float ConstrainWidth(Vector2 vpSize)
        => _compact
            ? vpSize.X * _widthRatio
            : Math.Min(vpSize.X * _widthRatio, MaxWidth);

    private float ConstrainHeight(Vector2 vpSize, float heightRatio)
        => _compact
            ? vpSize.Y * heightRatio
            : Math.Min(vpSize.Y * heightRatio, MaxHeight);

    private static StyleBoxFlat BuildStyle(float scale)
        => BuildStyle(scale, compact: false);

    private static StyleBoxFlat BuildStyle(float scale, bool compact)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            LauncherComponentTheme.PanelBackground,
            LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.PanelRadius)
        );
        style.BorderColor = new Color(0.05f, 0.5f, 0.58f, 0.55f);
        style.SetBorderWidthAll(Math.Max(1, LauncherComponentTheme.ScaleInt(scale, 1)));
        var horizontalMargin = compact ? CompactPanelHorizontalMargin : LauncherComponentTheme.PanelHorizontalMargin;
        var topMargin = compact ? CompactPanelTopMargin : LauncherComponentTheme.PanelTopMargin;
        var bottomMargin = compact ? CompactPanelBottomMargin : LauncherComponentTheme.PanelBottomMargin;
        style.ContentMarginLeft = LauncherComponentTheme.ScaleInt(scale, horizontalMargin);
        style.ContentMarginRight = LauncherComponentTheme.ScaleInt(scale, horizontalMargin);
        style.ContentMarginTop = LauncherComponentTheme.ScaleInt(scale, topMargin);
        style.ContentMarginBottom = LauncherComponentTheme.ScaleInt(scale, bottomMargin);
        return style;
    }
}
