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
    internal const bool CompactShortStatusDetailsSupported = true;
    internal const bool CompactPlainLanguageStatusCopySupported = true;
    private const int CompactMessageMaxChars = 86;

    internal static string MessageFor(string status)
        => string.IsNullOrWhiteSpace(status) ? "Waiting for launcher state..." : status.Trim();

    internal static string CompactMessageFor(string status)
    {
        status = MessageFor(status);

        if (ContainsAny(status, "Game startup failed last time", "Previous game launch did not finish"))
            return "Last launch failed. Open details or try Safe Start.";

        if (ContainsAny(status, "Help report ready"))
            return "Help report ready to share.";

        if (ContainsAny(status, "Launcher log copied", "Raw error log copied"))
            return "Launcher log copied. Review first.";

        if (ContainsAny(status, "Last problem opened", "Last error"))
            return "Last problem details are open.";

        if (ContainsAny(status, "Enter your Steam credentials"))
            return "Sign in with Steam to continue.";

        if (ContainsAny(status, "Connecting to Steam"))
            return "Connecting to Steam...";

        if (ContainsAny(status, "Authenticating"))
            return "Signing in to Steam...";

        if (ContainsAny(status, "Verifying game ownership"))
            return "Checking game ownership...";

        if (ContainsAny(status, "Refreshing Steam game version list"))
            return "Refreshing game versions...";

        if (ContainsAny(status, "Could not refresh Steam game version list"))
            return "Could not refresh game versions.";

        if (ContainsAny(status, "Selected game version metadata cache cleared"))
            return "Rebuilding selected game version...";

        if (ContainsAny(status, "Download cancelled"))
            return "Download cancelled.";

        if (ContainsAny(status, "Pull from Cloud must complete"))
            return "Get Steam saves before uploading.";

        if (ContainsAny(status, "no Android local save files"))
            return "No Android saves found for this version.";

        if (ContainsAny(status, "local save origin is not verified"))
            return "Local saves are not verified for this version.";

        if (ContainsAny(status, "backup storage permission"))
            return "Allow backup storage before uploading.";

        if (ContainsAny(status, "Push blocked: branch switch detected"))
            return "Get Steam saves after switching versions.";

        if (ContainsAny(status, "Push blocked"))
            return "Upload blocked. Check save safety first.";

        if (ContainsAny(status, "runtime pack not usable", "requires a usable runtime pack"))
            return "Runtime files need repair. Redownload this version.";

        if (IsDownloadRequiredStatus(status))
            return "Download this game version to play.";

        if (ContainsAny(status, "missing game files", "not installed", "not ready"))
            return "Download selected version first.";

        if (ContainsAny(status, "Downloading", "download progress"))
            return "Downloading selected version...";

        if (IsReadyStatus(status))
            return "Ready to play this version.";

        if (ContainsAny(status, "Logged in as"))
            return "Signed in. Checking game files.";

        if (ContainsAny(status, "Welcome back"))
            return "Welcome back. Checking game files.";

        return ShortenCompactMessage(status);
    }

    internal static string ActionFor(string status)
    {
        status = MessageFor(status);

        if (ContainsFailure(status))
            return "Fix Required";

        if (ContainsAny(status, "Steam Guard", "code"))
            return "Verify Code";

        if (ContainsAny(status, "Authenticating", "Connecting to Steam", "Verifying game ownership", "login", "sign in", "credentials"))
            return "Sign in";

        if (IsDownloadRequiredStatus(status)
            || ContainsAny(status, "not ready", "not installed", "missing game files", "Download", "download", "Updating", "update", "game files", "cache", "cached"))
            return "Install Game";

        if (IsReadyStatus(status) || ContainsAny(status, "Launch", "launch", "ready", "Ready", "game launch"))
            return "Start Game";

        if (ContainsAny(status, "game version", "branch", "metadata", "version list", "selected version"))
            return "Choose Version";

        if (ContainsAny(status, "Cloud", "cloud", "Pull", "Push", "save", "backup"))
            return "Sync Saves";

        if (ContainsAny(status, "Diagnostics", "diagnostics", "Help Details", "Help & Reports", "Help report", "details opened", "console", "log copied", "launcher log", "error log", "Last error", "Last problem"))
            return "Review Details";

        return "Next Step";
    }

    internal static string PhaseFor(string status)
    {
        status = MessageFor(status);

        if (ContainsFailure(status))
            return "Attention";

        if (ContainsAny(status, "Steam Guard", "code", "Authenticating", "Connecting to Steam", "Verifying game ownership", "login", "credentials"))
            return "Steam";

        if (IsDownloadRequiredStatus(status)
            || ContainsAny(status, "not ready", "not installed", "missing game files", "Download", "download", "Updating", "update", "game files", "cache", "cached"))
            return "Install";

        if (IsReadyStatus(status) || ContainsAny(status, "Launch", "launch", "ready", "Ready", "game launch"))
            return "Ready";

        if (ContainsAny(status, "game version", "branch", "metadata", "version list", "selected version"))
            return "Version";

        if (ContainsAny(status, "Cloud", "cloud", "Pull", "Push", "save", "backup"))
            return "Cloud";

        if (ContainsAny(status, "Diagnostics", "diagnostics", "Help Details", "Help & Reports", "Help report", "details opened", "console", "log copied", "launcher log", "error log", "Last error", "Last problem"))
            return "Details";

        return "Status";
    }

    internal static Color ColorFor(string phase)
        => phase switch
        {
            "Attention" => LauncherComponentTheme.OrangeHot,
            "Steam" => LauncherComponentTheme.CyanAccent,
            "Version" => LauncherComponentTheme.CyanAccent,
            "Install" => LauncherComponentTheme.OrangeAccent,
            "Cloud" => LauncherComponentTheme.CyanAccent,
            "Ready" => new Color(0.36f, 0.9f, 0.42f),
            "Details" => LauncherComponentTheme.TextSecondary,
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

    private static bool IsDownloadRequiredStatus(string status)
        => ContainsAny(status, "Download selected game version")
            || (ContainsAny(status, "Selected game version")
                && ContainsAny(status, "not downloaded", "not ready", "missing game files"));

    private static bool IsReadyStatus(string status)
        => ContainsAny(status, "Selected game version:")
            && ContainsAny(status, "Runtime pairing is verified", "Active install slot");

    private static string ShortenCompactMessage(string status)
    {
        if (status.Length <= CompactMessageMaxChars)
            return status;

        var cut = status.LastIndexOf(' ', CompactMessageMaxChars);
        if (cut < CompactMessageMaxChars / 2)
            cut = CompactMessageMaxChars;

        return status.Substring(0, cut).TrimEnd(' ', '.', ';', ',') + "...";
    }
}
