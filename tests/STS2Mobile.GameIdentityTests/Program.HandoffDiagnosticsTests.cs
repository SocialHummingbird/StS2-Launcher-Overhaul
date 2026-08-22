using System;
using System.Diagnostics;
using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void HandoffAttemptIdsAreStableAndStructured()
    {
        const string attemptId = "0123456789abcdef0123456789abcdef";
        var context = new LaunchAttemptContext(
            attemptId,
            Stopwatch.StartNew(),
            "public-beta",
            pendingReadiness: null
        );
        Equal(attemptId, context.AttemptId, "An accepted launch-attempt ID must be preserved.");

        var generated = LaunchAttemptContext.CreateAttemptId();
        Equal(32, generated.Length, "Generated launch-attempt IDs must use compact GUID form.");
        True(Guid.TryParseExact(generated, "N", out _), "Generated launch-attempt IDs must be valid GUIDs.");

        var line = LauncherHandoffEvent.Format(
            attemptId,
            LauncherHandoffEvent.MainMenuReady,
            timestampUtcMs: 123456789L,
            thread: "managed#7",
            activityLifecycle: "resumed",
            windowFocus: "true",
            overlayVisible: "true",
            godotReadiness: "main_menu_ready"
        );
        Contains(line, $"attemptId={attemptId}", "Structured handoff logs must carry the exact attempt ID.");
        Contains(line, "event=main_menu_ready", "Structured handoff logs must carry the event name.");
        Contains(line, "timestampUtcMs=123456789", "Structured handoff logs must carry the timestamp.");
        Contains(line, "thread=managed#7", "Structured handoff logs must carry the thread.");
        Contains(line, "activityLifecycle=resumed", "Structured handoff logs must carry lifecycle state.");
        Contains(line, "windowFocus=true", "Structured handoff logs must carry focus state.");
        Contains(line, "overlayVisible=true", "Structured handoff logs must carry overlay state.");
        Contains(line, "godotReady=main_menu_ready", "Structured handoff logs must carry Godot readiness.");
    }
}
