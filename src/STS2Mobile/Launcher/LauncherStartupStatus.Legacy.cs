using System;
using Godot;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupStatus
{
    private static Label CreateLegacyLabel(Node parent, Vector2 viewportSize)
    {
        var margin = CalculateSafeMargin(viewportSize);
        var fontSize = CalculateFontSize(viewportSize);
        var label = new Label
        {
            Name = NodeName,
            ZIndex = ZIndex,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        label.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        label.OffsetLeft = margin;
        label.OffsetTop = margin;
        label.OffsetRight = -margin;
        label.OffsetBottom = margin + fontSize * 3.2f;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", new Color(0.55f, 0.85f, 1f));
        parent.AddChild(label);
        return label;
    }

    private static int CalculateFontSize(Vector2 viewportSize)
    {
        var shortEdge = Math.Min(viewportSize.X, viewportSize.Y);
        return (int)Math.Clamp(shortEdge / 42f, 18f, 28f);
    }
}
