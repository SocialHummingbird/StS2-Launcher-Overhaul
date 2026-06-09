using System;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static class LauncherStartupStatus
{
    private const string NodeName = "STS2MobileStartupStatus";
    private const int ZIndex = 4096;

    internal static Label CreateLabel(Node parent)
    {
        try
        {
            var viewportSize = parent.GetViewport()?.GetVisibleRect().Size
                ?? new Vector2(1920, 1080);
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
        catch (Exception ex)
        {
            PatchHelper.Log($"Startup status label creation failed: {ex.Message}");
            return null;
        }
    }

    internal static void Set(Label label, string message)
    {
        PatchHelper.Log($"[Startup] {message}");
        if (label == null)
            return;

        try
        {
            label.Text = message;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Startup status label update failed: {ex.Message}");
        }
    }

    private static int CalculateFontSize(Vector2 viewportSize)
    {
        var shortEdge = Math.Min(viewportSize.X, viewportSize.Y);
        return (int)Math.Clamp(shortEdge / 42f, 18f, 28f);
    }

    private static float CalculateSafeMargin(Vector2 viewportSize)
    {
        var shortEdge = Math.Min(viewportSize.X, viewportSize.Y);
        return Math.Clamp(shortEdge * 0.035f, 16f, 48f);
    }

}
