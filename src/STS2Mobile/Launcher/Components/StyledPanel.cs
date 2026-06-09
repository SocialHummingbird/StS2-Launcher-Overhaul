using System;
using Godot;

namespace STS2Mobile.Launcher.Components;

internal sealed class StyledPanel : CenterContainer
{
    private const float MaxWidth = 1400f;
    private const float MaxHeight = 800f;

    private VBoxContainer Content { get; }

    internal StyledPanel(float scale, float widthRatio = 0.7f)
    {
        SetAnchorsPreset(LayoutPreset.FullRect);

        var vpSize = new Vector2(1920, 1080); // fallback, overridden after AddChild
        var panelContainer = new PanelContainer();
        panelContainer.CustomMinimumSize = new Vector2(vpSize.X * widthRatio, 0);

        panelContainer.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildStyle(scale)
        );
        AddChild(panelContainer);

        Content = new VBoxContainer();
        Content.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        Content.AddThemeConstantOverride("separation", (int)(10 * scale));
        panelContainer.AddChild(Content);

        // Defer viewport-based sizing until in tree
        _panelContainer = panelContainer;
        _widthRatio = widthRatio;
    }

    private readonly PanelContainer _panelContainer;
    private readonly float _widthRatio;

    internal void AddContent(Control control)
        => Content.AddChild(control);

    internal void OnPanelGuiInput(Action<InputEvent> handler)
        => _panelContainer.GuiInput += input => handler(input);

    internal void UpdateSizeFromViewport(Vector2 vpSize)
        => _panelContainer.CustomMinimumSize = new Vector2(
            Math.Min(vpSize.X * _widthRatio, MaxWidth),
            Math.Min(vpSize.Y * 0.85f, MaxHeight)
        );

    internal void UpdateSizeFromViewport(Vector2 vpSize, float heightRatio)
        => _panelContainer.CustomMinimumSize = new Vector2(
            Math.Min(vpSize.X * _widthRatio, MaxWidth),
            Math.Min(vpSize.Y * heightRatio, MaxHeight)
        );

    private static StyleBoxFlat BuildStyle(float scale)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            LauncherComponentTheme.PanelBackground,
            LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.PanelRadius)
        );
        style.ContentMarginLeft = LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.PanelHorizontalMargin);
        style.ContentMarginRight = LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.PanelHorizontalMargin);
        style.ContentMarginTop = LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.PanelTopMargin);
        style.ContentMarginBottom = LauncherComponentTheme.ScaleInt(scale, LauncherComponentTheme.PanelBottomMargin);
        return style;
    }
}
