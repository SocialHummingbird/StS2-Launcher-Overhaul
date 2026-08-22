using System;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed class LauncherHandoffOverlay : ILauncherHandoffOverlay
{
    private LauncherHandoffOverlay(LauncherUI launcher)
    {
        Launcher = launcher;
    }

    internal LauncherUI Launcher { get; }
    private Label StartupStatus { get; set; }

    public bool IsAvailable => NodeAvailable(Launcher);

    public bool IsVisible
        => NodeVisible(Launcher) || LauncherStartupStatus.IsOwnedVisible(StartupStatus);

    internal static LauncherHandoffOverlay Show(Node parent, bool inGameMode)
    {
        var launcher = new LauncherUI();
        launcher.SetGameMode(inGameMode);
        parent.AddChild(launcher);
        return new LauncherHandoffOverlay(launcher);
    }

    internal Label ShowStartupStatus(Node parent)
    {
        if (StartupStatus != null)
            return StartupStatus;

        StartupStatus = LauncherStartupStatus.CreateOwned(parent);
        return StartupStatus;
    }

    public bool Dismiss()
    {
        var statusDismissed = LauncherStartupStatus.DismissOwned(StartupStatus);
        var launcherDismissed = DismissNode(Launcher, "launcher");
        return statusDismissed && launcherDismissed;
    }

    public bool ReturnToLauncher(string attemptId)
    {
        var statusDismissed = LauncherStartupStatus.DismissOwned(StartupStatus);
        StartupStatus = null;
        if (!RestoreNode(Launcher))
            return false;

        Launcher.RestoreAfterFailedHandoff(attemptId);
        return statusDismissed;
    }

    private static bool RestoreNode(Control node)
    {
        if (
            node == null
            || !GodotObject.IsInstanceValid(node)
            || node.IsQueuedForDeletion()
        )
            return false;

        try
        {
            node.ProcessMode = Node.ProcessModeEnum.Inherit;
            node.MouseFilter = Control.MouseFilterEnum.Stop;
            node.Visible = true;
            return true;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Handoff launcher restoration failed: {ex.Message}");
            return false;
        }
    }

    private static bool DismissNode(Control node, string label)
    {
        if (node == null || !GodotObject.IsInstanceValid(node))
            return true;

        try
        {
            node.MouseFilter = Control.MouseFilterEnum.Ignore;
            node.FocusMode = Control.FocusModeEnum.None;
            node.ProcessMode = Node.ProcessModeEnum.Disabled;
            node.Visible = false;
            node.QueueFree();
            return true;
        }
        catch (ObjectDisposedException)
        {
            return true;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Handoff {label} dismissal failed: {ex.Message}");
            return false;
        }
    }

    private static bool NodeVisible(CanvasItem node)
    {
        try
        {
            return node != null
                && GodotObject.IsInstanceValid(node)
                && node.Visible;
        }
        catch
        {
            return false;
        }
    }

    private static bool NodeAvailable(Node node)
    {
        try
        {
            return node != null
                && GodotObject.IsInstanceValid(node)
                && node.IsInsideTree()
                && !node.IsQueuedForDeletion();
        }
        catch
        {
            return false;
        }
    }
}
