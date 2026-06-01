using System;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherStartupRecoveryControlPanel
{
    private const int CanvasLayerIndex = 128;
    private const int ContainerSeparation = 10;
    private const int DetailFontSize = 18;
    private const string NodeName = "STS2MobileStartupRecovery";
    private const int TitleFontSize = 24;
    private const string ThemeFontColor = "font_color";
    private const string ThemeFontSize = "font_size";
    private const string ThemeSeparation = "separation";

    private static readonly Vector2 ButtonMinimumSize = new(420, 56);
    private static readonly Vector2 ContainerMinimumSize = new(820, 0);
    private static readonly Vector2 ContainerPosition = new(24, 72);
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

            var panel = new LauncherStartupRecoveryControlPanel();
            parent.AddChild(panel.Layer);
            return panel.Layer;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Startup recovery controls failed: {ex.Message}");
            return null;
        }
    }
}
