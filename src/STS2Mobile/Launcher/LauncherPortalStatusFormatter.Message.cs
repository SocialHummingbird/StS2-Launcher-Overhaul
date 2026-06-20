namespace STS2Mobile.Launcher;

internal static partial class LauncherPortalStatusFormatter
{
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
