using System;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupStatus
{
    private const string NodeName = "STS2MobileStartupStatus";
    private const int ZIndex = StartupPresentationLayerPolicy.StartupStatusZIndex;

    internal static Label CreateOwned(Node parent)
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

    internal static bool DismissOwned(Label label)
    {
        var target = FindStatusRoot(label);
        if (target == null)
            return true;

        try
        {
            DisableOwnedNode(target);
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

    internal static bool IsOwnedVisible(Label label)
    {
        try
        {
            var target = FindStatusRoot(label);
            if (target == null || !GodotObject.IsInstanceValid(target))
                return false;

            return target switch
            {
                CanvasItem canvasItem => canvasItem.Visible,
                CanvasLayer canvasLayer => canvasLayer.Visible,
                _ => target.IsInsideTree(),
            };
        }
        catch
        {
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

    private static void DisableOwnedNode(Node node)
    {
        if (node is Control control)
        {
            control.MouseFilter = Control.MouseFilterEnum.Ignore;
            control.FocusMode = Control.FocusModeEnum.None;
            if (control.HasFocus())
                control.ReleaseFocus();
        }

        node.ProcessMode = Node.ProcessModeEnum.Disabled;
        if (node is CanvasItem canvasItem)
            canvasItem.Visible = false;
        else if (node is CanvasLayer canvasLayer)
            canvasLayer.Visible = false;

        foreach (var child in node.GetChildren())
            DisableOwnedNode(child);
    }

    private static float CalculateSafeMargin(Vector2 viewportSize)
    {
        var shortEdge = Math.Min(viewportSize.X, viewportSize.Y);
        return Math.Clamp(shortEdge * 0.035f, 16f, 48f);
    }
}
