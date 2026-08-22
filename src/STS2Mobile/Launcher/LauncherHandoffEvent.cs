using System;

namespace STS2Mobile.Launcher;

internal static class LauncherHandoffEvent
{
    internal const string LaunchRequested = "launch_requested";
    internal const string NativeBootstrapAccepted = "native_bootstrap_accepted";
    internal const string GodotSurfaceCreated = "godot_surface_created";
    internal const string MainMenuReady = "main_menu_ready";
    internal const string WindowFocusGained = "window_focus_gained";
    internal const string WindowFocusLost = "window_focus_lost";
    internal const string OverlayShown = "overlay_shown";
    internal const string OverlayHidden = "overlay_hidden";
    internal const string HandoffCompleted = "handoff_completed";
    internal const string HandoffFailed = "handoff_failed";

    internal static string Format(
        string attemptId,
        string eventName,
        long timestampUtcMs,
        string thread,
        string activityLifecycle,
        string windowFocus,
        string overlayVisible,
        string godotReadiness
    )
        => "[Handoff] "
            + $"attemptId={Token(attemptId)} "
            + $"event={Token(eventName)} "
            + $"timestampUtcMs={timestampUtcMs} "
            + $"thread={Token(thread)} "
            + $"activityLifecycle={Token(activityLifecycle)} "
            + $"windowFocus={Token(windowFocus)} "
            + $"overlayVisible={Token(overlayVisible)} "
            + $"godotReady={Token(godotReadiness)}";

    internal static string NormalizeAttemptId(string attemptId)
        => string.IsNullOrWhiteSpace(attemptId) ? "unknown" : Token(attemptId);

    private static string Token(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "unknown";

        var token = value.Trim();
        foreach (var character in new[] { ' ', '\t', '\r', '\n', '=' })
            token = token.Replace(character, '_');
        return token;
    }
}
