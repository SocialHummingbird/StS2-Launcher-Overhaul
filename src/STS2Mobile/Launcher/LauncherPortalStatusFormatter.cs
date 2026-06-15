using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal static class LauncherPortalStatusFormatter
{
    internal const bool PhaseLabelStatusSupported = true;
    internal const bool StructuredStatusChipSupported = true;
    internal const bool GuidedNextActionStatusSupported = true;
    internal const bool ErrorFirstGuidedStatusSupported = true;

    internal static string MessageFor(string status)
        => string.IsNullOrWhiteSpace(status) ? "Waiting for launcher state..." : status.Trim();

    internal static string ActionFor(string status)
    {
        status = MessageFor(status);

        if (ContainsFailure(status))
            return "FIX REQUIRED";

        if (ContainsAny(status, "Steam Guard", "code"))
            return "VERIFY CODE";

        if (ContainsAny(status, "Authenticating", "Connecting to Steam", "Verifying game ownership", "login", "sign in"))
            return "SIGN IN";

        if (ContainsAny(status, "game version", "branch", "metadata", "version list", "selected version"))
            return "CHOOSE VERSION";

        if (ContainsAny(status, "not ready", "not installed", "missing game files", "Download", "download", "Updating", "update", "game files", "cache", "cached"))
            return "INSTALL GAME";

        if (ContainsAny(status, "Cloud", "cloud", "Pull", "Push", "save", "backup"))
            return "SYNC SAVES";

        if (ContainsAny(status, "Launch", "launch", "ready", "Ready", "game launch"))
            return "START GAME";

        if (ContainsAny(status, "Diagnostics", "diagnostics", "console", "log copied", "error log", "Last error"))
            return "REVIEW DETAILS";

        return "NEXT STEP";
    }

    internal static string PhaseFor(string status)
    {
        status = MessageFor(status);

        if (ContainsFailure(status))
            return "ATTENTION";

        if (ContainsAny(status, "Steam Guard", "code", "Authenticating", "Connecting to Steam", "Verifying game ownership", "login"))
            return "STEAM AUTH";

        if (ContainsAny(status, "game version", "branch", "metadata", "version list", "selected version"))
            return "VERSION";

        if (ContainsAny(status, "not ready", "not installed", "missing game files", "Download", "download", "Updating", "update", "game files", "cache", "cached"))
            return "INSTALL";

        if (ContainsAny(status, "Cloud", "cloud", "Pull", "Push", "save", "backup"))
            return "CLOUD";

        if (ContainsAny(status, "Launch", "launch", "ready", "Ready", "game launch"))
            return "READY";

        if (ContainsAny(status, "Diagnostics", "diagnostics", "console", "log copied", "error log", "Last error"))
            return "DIAGNOSTICS";

        return "STATUS";
    }

    internal static Color ColorFor(string phase)
        => phase switch
        {
            "ATTENTION" => LauncherComponentTheme.OrangeHot,
            "STEAM AUTH" => LauncherComponentTheme.CyanAccent,
            "VERSION" => LauncherComponentTheme.CyanAccent,
            "INSTALL" => LauncherComponentTheme.OrangeAccent,
            "CLOUD" => LauncherComponentTheme.CyanAccent,
            "READY" => new Color(0.36f, 0.9f, 0.42f),
            "DIAGNOSTICS" => LauncherComponentTheme.TextSecondary,
            _ => LauncherComponentTheme.TextSecondary,
        };

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
}
