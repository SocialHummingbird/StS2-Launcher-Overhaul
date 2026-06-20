using System;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private const int CanvasLayerIndex = 128;
    private const int ContainerSeparation = 12;
    private const int DetailFontSize = 19;
    private const string NodeName = "STS2MobileStartupRecovery";
    private const int TitleFontSize = 26;
    private const string ThemeFontColor = "font_color";
    private const string ThemeFontSize = "font_size";
    private const string ThemeSeparation = "separation";

    private const float ReferenceShortEdge = 1080f;
    private const float ContainerMaxWidth = 820f;
    private const float ContainerMargin = 24f;
    private const float ContainerTop = 72f;
    private const float ButtonMaxWidth = 520f;
    private const float ButtonHeight = 64f;

    private static readonly Color DetailColor = new(0.9f, 0.9f, 0.9f);
    private static readonly Color TitleColor = new(0.55f, 0.85f, 1f);

    private CanvasLayer Layer { get; }

    private readonly Label _detail;

    internal static CanvasLayer Show(Node parent)
    {
        try
        {
            if (parent.HasNode(NodeName))
                return parent.GetNode<CanvasLayer>(NodeName);

            var panel = new LauncherStartupRecoveryControlPanel(VisibleViewportSize(parent));
            parent.AddChild(panel.Layer);
            return panel.Layer;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Startup recovery controls failed: {ex.Message}");
            return null;
        }
    }

    private static Vector2 VisibleViewportSize(Node parent)
        => parent.GetViewport()?.GetVisibleRect().Size ?? new Vector2(1080, 1920);

    private static float LayoutScale(Vector2 viewportSize)
    {
        var shortEdge = Math.Max(1f, Math.Min(viewportSize.X, viewportSize.Y));
        return Math.Clamp(shortEdge / ReferenceShortEdge, 0.94f, 1.34f);
    }
}
