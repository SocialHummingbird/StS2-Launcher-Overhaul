using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static class LauncherHandoffDiagnostics
{
    internal static void Record(
        string eventName,
        bool? overlayVisible,
        string godotReadiness,
        string attemptId
    )
    {
        var resolvedAttemptId = LauncherHandoffEvent.NormalizeAttemptId(attemptId);
        var overlay = BooleanState(overlayVisible);
        if (OperatingSystem.IsAndroid())
        {
            try
            {
                if (AndroidGodotAppBridge.RecordHandoffEvent(
                    eventName,
                    resolvedAttemptId,
                    overlay,
                    godotReadiness
                ))
                    return;
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"Handoff native state capture failed: {ex.Message}");
            }
        }

        PatchHelper.Log(
            LauncherHandoffEvent.Format(
                resolvedAttemptId,
                eventName,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                $"managed#{Environment.CurrentManagedThreadId}",
                OperatingSystem.IsAndroid() ? "unknown" : "not_android",
                "unknown",
                overlay,
                godotReadiness
            )
        );
    }

    private static string BooleanState(bool? value)
        => value.HasValue ? (value.Value ? "true" : "false") : "unknown";
}
