using System;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupStatus
{
    private const string NodeName = "STS2MobileStartupStatus";
    private const int ZIndex = 4096;

    internal static Label CreateLabel(Node parent)
    {
        try
        {
            var viewportSize = parent.GetViewport()?.GetVisibleRect().Size
                ?? new Vector2(1920, 1080);
            if (OperatingSystem.IsAndroid())
                return CreateAndroidStatusCard(parent, viewportSize);

            return CreateLegacyLabel(parent, viewportSize);
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

    internal static bool QueueFree(Label label)
    {
        var target = FindStatusRoot(label);
        if (target == null)
            return true;

        try
        {
            target.QueueFree();
            return true;
        }
        catch (ObjectDisposedException)
        {
            PatchHelper.Log("Startup status already disposed");
            return true;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Startup status cleanup failed: {ex.Message}");
            return false;
        }
    }

    private static Node FindStatusRoot(Label label)
    {
        if (label == null)
            return null;

        for (Node current = label; current != null; current = current.GetParent())
        {
            if (current.Name == NodeName)
                return current;
        }

        return label;
    }

    private static float CalculateSafeMargin(Vector2 viewportSize)
    {
        var shortEdge = Math.Min(viewportSize.X, viewportSize.Y);
        return Math.Clamp(shortEdge * 0.035f, 16f, 48f);
    }
}
