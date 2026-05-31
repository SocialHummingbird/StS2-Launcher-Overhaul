using System;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static class LauncherStartupStatus
{
    private const int FontSize = 22;
    private const string NodeName = "STS2MobileStartupStatus";
    private const int ZIndex = 4096;
    private static readonly Vector2 Position = new(24, 24);

    internal static Label CreateLabel(Node parent)
    {
        try
        {
            var label = new Label
            {
                Name = NodeName,
                Position = Position,
                ZIndex = ZIndex,
            };
            label.AddThemeFontSizeOverride("font_size", FontSize);
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

}
