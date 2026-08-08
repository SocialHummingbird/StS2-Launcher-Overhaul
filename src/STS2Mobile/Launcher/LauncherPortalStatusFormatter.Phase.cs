namespace STS2Mobile.Launcher;

internal static partial class LauncherPortalStatusFormatter
{
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
}
