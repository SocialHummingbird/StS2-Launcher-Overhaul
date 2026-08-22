using System;
using System.Threading.Tasks;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal readonly struct LauncherHandoffVisibility
{
    private LauncherHandoffVisibility(
        string activityLifecycle,
        bool activityForeground,
        bool windowFocused
    )
    {
        ActivityLifecycle = activityLifecycle;
        ActivityForeground = activityForeground;
        WindowFocused = windowFocused;
    }

    internal string ActivityLifecycle { get; }
    internal bool ActivityForeground { get; }
    internal bool WindowFocused { get; }
    internal bool SuspendsHandoffTimeout
        => IsLifecycle(
            "creating",
            "created",
            "started",
            "paused",
            "stopped"
        );

    internal static LauncherHandoffVisibility ParseAndroid(string value)
    {
        var parts = (value ?? string.Empty).Split('\n');
        var lifecycle = parts.Length > 0 ? parts[0].Trim() : "unknown";
        var focused = parts.Length > 1
            && bool.TryParse(parts[1].Trim(), out var parsedFocus)
            && parsedFocus;
        return new LauncherHandoffVisibility(
            lifecycle,
            string.Equals(lifecycle, "resumed", StringComparison.OrdinalIgnoreCase),
            focused
        );
    }

    internal static LauncherHandoffVisibility Capture()
    {
        if (OperatingSystem.IsAndroid())
        {
            return ParseAndroid(
                AndroidGodotAppBridge.GetHandoffVisibilityConfirmation()
            );
        }

        var focused = DisplayServer.WindowIsFocused();
        return new LauncherHandoffVisibility(
            focused ? "foreground" : "background",
            focused,
            focused
        );
    }

    private bool IsLifecycle(params string[] states)
    {
        foreach (var state in states)
        {
            if (string.Equals(ActivityLifecycle, state, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

internal static class LauncherHandoffVisibilityConfirmation
{
    internal static async Task<bool> WaitForGameVisibleAsync(
        LauncherHandoffStateOwner owner,
        string attemptId,
        Node gameNode,
        int timeoutMs
    )
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(gameNode);

        var tree = gameNode.GetTree();
        if (tree == null)
            return false;

        var deadline = LauncherMonotonicDeadline.Start(
            TimeSpan.FromMilliseconds(Math.Max(1, timeoutMs))
        );
        var lifecycle = new LauncherOperationLifecycle();
        var lifecycleMonitor = new LauncherOperationLifecycleMonitor(
            lifecycle,
            deadline
        );
        var bridgeFailureLogged = false;
        try
        {
            gameNode.AddChild(lifecycleMonitor);
            while (!deadline.IsExpired)
            {
                LauncherHandoffVisibility visibility;
                try
                {
                    visibility = LauncherHandoffVisibility.Capture();
                    lifecycleMonitor.ObserveApplicationActive(
                        !visibility.SuspendsHandoffTimeout
                    );
                    owner.ObserveVisibility(
                        attemptId,
                        visibility.ActivityForeground,
                        visibility.WindowFocused
                    );
                }
                catch (Exception ex)
                {
                    if (!bridgeFailureLogged)
                    {
                        PatchHelper.Log(
                            $"Waiting for Android activity recreation during handoff: {ex.Message}"
                        );
                        bridgeFailureLogged = true;
                    }
                }

                var state = owner.Capture();
                if (
                    state.State == LauncherHandoffState.GameVisible
                    && string.Equals(state.AttemptId, attemptId, StringComparison.Ordinal)
                )
                    return true;
                if (
                    state.State != LauncherHandoffState.HandoffPending
                    || !string.Equals(state.AttemptId, attemptId, StringComparison.Ordinal)
                )
                    return false;

                if (!await LauncherAsyncYield.ProcessFrameAsync(
                    tree,
                    deadline,
                    lifecycle
                ))
                    break;
            }
        }
        finally
        {
            lifecycle.Destroy();
            if (GodotObject.IsInstanceValid(lifecycleMonitor))
                lifecycleMonitor.QueueFree();
        }

        PatchHelper.Log(
            $"Game visibility confirmation timed out after {timeoutMs}ms for attempt {attemptId}"
        );
        return false;
    }
}
