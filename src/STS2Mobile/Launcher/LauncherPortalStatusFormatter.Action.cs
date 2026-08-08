namespace STS2Mobile.Launcher;

internal static partial class LauncherPortalStatusFormatter
{
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
}
