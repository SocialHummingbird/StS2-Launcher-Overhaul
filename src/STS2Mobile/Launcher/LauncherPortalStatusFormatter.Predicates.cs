using System;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPortalStatusFormatter
{
    private static bool ContainsAny(string text, params string[] needles)
    {
        foreach (var needle in needles)
        {
            if (text.Contains(needle, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool ContainsFailure(string text)
        => ContainsAny(text, "failed", "Failed", "blocked", "Blocked", "Could not", "error", "Error", "crash", "Crash");

    private static bool IsDownloadRequiredStatus(string status)
        => ContainsAny(status, "Download selected game version")
            || (ContainsAny(status, "Selected game version")
                && ContainsAny(status, "not downloaded", "not ready", "missing game files"));

    private static bool IsReadyStatus(string status)
        => ContainsAny(status, "Selected game version:")
            && ContainsAny(status, "Runtime pairing is verified", "Active install slot");
}
