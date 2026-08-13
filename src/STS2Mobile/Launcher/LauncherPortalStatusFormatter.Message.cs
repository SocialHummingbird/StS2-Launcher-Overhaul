namespace STS2Mobile.Launcher;

internal static partial class LauncherPortalStatusFormatter
{
    private const int CompactMessageMaxChars = 86;

    internal static string MessageFor(string status)
        => string.IsNullOrWhiteSpace(status) ? "Waiting for launcher state..." : status.Trim();

    internal static string CompactMessageFor(string status)
    {
        status = MessageFor(status);
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
