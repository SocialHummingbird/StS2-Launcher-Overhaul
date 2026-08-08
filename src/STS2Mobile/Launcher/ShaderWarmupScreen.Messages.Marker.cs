using System;

namespace STS2Mobile.Launcher;

internal sealed partial class ShaderWarmupScreen
{
    private static partial class Message
    {
        internal static string MarkerCheckFailed(Exception ex)
            => $"[ShaderWarmup] NeedsWarmup check failed: {ex.Message}";

        internal static string MarkerMissing()
            => "[ShaderWarmup] NeedsWarmup=true (no marker file)";

        internal static string MarkerMatches(string content)
            => $"[ShaderWarmup] NeedsWarmup=false (marker v{content} matches)";

        internal static string MarkerMismatch(string content, int expectedVersion)
            => $"[ShaderWarmup] NeedsWarmup=true (marker v{content} != v{expectedVersion})";

        internal static string MarkerWriteFailed(Exception ex)
            => $"[ShaderWarmup] Failed to write version marker: {ex.Message}";
    }
}
