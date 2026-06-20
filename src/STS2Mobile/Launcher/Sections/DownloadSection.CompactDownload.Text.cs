using System;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class DownloadSection
{
    private static string CompactDownloadButtonText(string text, bool compact)
    {
        if (!compact)
            return text;

        var (title, detail) = CompactDownloadButtonTitleDetail(text);
        return $"{title}\n{detail}";
    }

    private static string CompactDownloadProgressButtonText()
        => "Downloading...\nSteam files";

    private static string CompactDownloadProgressText(string text)
    {
        var detail = CompactDownloadProgressDetail(text);
        return detail.Length == 0
            ? "Downloading selected version"
            : $"Downloading selected version\n{detail}";
    }

    private static string CompactDownloadProgressDetail(string text)
    {
        var normalized = NormalizeCompactProgressText(text);
        if (normalized.Length == 0)
            return "Waiting for Steam";

        if (normalized.Length <= CompactDownloadProgressDetailLimit)
            return normalized;

        return normalized[..Math.Max(0, CompactDownloadProgressDetailLimit - 3)].TrimEnd() + "...";
    }

    private static string NormalizeCompactProgressText(string text)
        => string.IsNullOrWhiteSpace(text)
            ? ""
            : string.Join(" ", text.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries));

    private static (string Title, string Detail) CompactDownloadButtonTitleDetail(string text)
    {
        var normalized = (text ?? "").Trim();
        if (normalized.Length == 0
            || string.Equals(normalized, DefaultDownloadButtonText, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "DOWNLOAD SELECTED VERSION", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "DOWNLOAD VERSION", StringComparison.OrdinalIgnoreCase))
            return ("Download Version", "Local files only");

        if (string.Equals(normalized, "REDOWNLOAD SELECTED VERSION", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "REDOWNLOAD VERSION", StringComparison.OrdinalIgnoreCase))
            return ("Redownload Version", "Rebuild local files");

        if (string.Equals(normalized, "RETRY DOWNLOAD", StringComparison.OrdinalIgnoreCase))
            return ("Retry Download", "Local files only");

        if (string.Equals(normalized, "DOWNLOADING...", StringComparison.OrdinalIgnoreCase))
            return ("Downloading...", "Steam files");

        return (normalized, "Local files only");
    }
}
