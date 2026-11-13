using Godot;
using System;

namespace STS2Mobile.Launcher.Sections;
internal sealed partial class DownloadSection
{
    private void MoveCompactProgressControlsNearPrimaryAction()
    {
        if (!_compact)
            return;
        MoveChild(_progressLabel, _downloadButton.GetIndex() + 1);
        MoveChild(_progressBar, _progressLabel.GetIndex() + 1);
        MoveChild(_pauseButton, _progressBar.GetIndex() + 1);
    }

    private static readonly CompactButtonDetailLabelSpec CompactDownloadActionLabels = new(CompactDownloadActionBodyName, CompactDownloadActionTitleName, CompactDownloadActionDetailName, CompactDownloadActionTitleFontSize, CompactDownloadActionDetailFontSize, CompactDownloadActionHorizontalMargin, CompactDownloadActionVerticalMargin);
    private void SetCompactDownloadButtonText(Button button, string text) => CompactButtonDetailLabels.Apply(button, text, _scale, _compact, CompactDownloadActionLabels);
    private static string CompactDownloadButtonText(string text, bool compact)
    {
        if (!compact)
            return text;
        var (title, detail) = CompactDownloadButtonTitleDetail(text);
        return $"{title}\n{detail}";
    }

    private static string CompactDownloadProgressButtonText(string text) => string.Equals(text, LocalPckRepairOperation.ProgressMessage, StringComparison.Ordinal) ? "Updating...\nLocal game files" : "Downloading...\nSteam files";
    private static string CompactDownloadProgressText(string text)
    {
        if (string.Equals(text, LocalPckRepairOperation.ProgressMessage, StringComparison.Ordinal))
        {
            return text;
        }

        var payload = CompactPayloadProgressText(NormalizeCompactProgressText(text));
        if (payload != null)
            return payload;

        var detail = CompactDownloadProgressDetail(text);
        return detail.Length == 0 ? "Downloading selected version" : $"Downloading selected version\n{detail}";
    }

    private static string CompactPayloadProgressText(string text)
    {
        const string reusedMarker = " · Reused ";
        const string resumedMarker = ", resumed ";
        var reused = text.IndexOf(reusedMarker, StringComparison.Ordinal);
        var resumed = text.IndexOf(resumedMarker, StringComparison.Ordinal);
        if (!text.StartsWith("Download ", StringComparison.Ordinal) || reused < 0 || resumed <= reused)
            return null;

        var payload = text[..reused];
        var percentage = payload.LastIndexOf(" (", StringComparison.Ordinal);
        if (percentage >= 0)
            payload = payload[..percentage];
        var reusedSize = text[(reused + reusedMarker.Length)..resumed];
        var resumedSize = text[(resumed + resumedMarker.Length)..];
        // The progress bar carries the percentage; these two lines retain every byte metric.
        return $"{payload}\nReused {reusedSize} · Resumed {resumedSize}";
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

    private static string NormalizeCompactProgressText(string text) => string.IsNullOrWhiteSpace(text) ? "" : string.Join(" ", text.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries));
    private static (string Title, string Detail) CompactDownloadButtonTitleDetail(string text)
    {
        var normalized = (text ?? "").Trim();
        if (normalized.Length == 0 || string.Equals(normalized, DefaultDownloadButtonText, StringComparison.OrdinalIgnoreCase) || string.Equals(normalized, "DOWNLOAD SELECTED VERSION", StringComparison.OrdinalIgnoreCase) || string.Equals(normalized, "DOWNLOAD VERSION", StringComparison.OrdinalIgnoreCase))
            return ("Download Version", "Local files only");
        if (string.Equals(normalized, "REDOWNLOAD SELECTED VERSION", StringComparison.OrdinalIgnoreCase) || string.Equals(normalized, "REDOWNLOAD VERSION", StringComparison.OrdinalIgnoreCase) || string.Equals(normalized, "REPAIR SELECTED BRANCH", StringComparison.OrdinalIgnoreCase))
            return ("Redownload Version", "Selected files only");
        if (string.Equals(normalized, "RETRY DOWNLOAD", StringComparison.OrdinalIgnoreCase))
            return ("Retry Download", "Local files only");
        if (string.Equals(normalized, "RESUME DOWNLOAD", StringComparison.OrdinalIgnoreCase))
            return ("Resume Download", "Keep downloaded progress");
        if (string.Equals(normalized, "DOWNLOADING...", StringComparison.OrdinalIgnoreCase))
            return ("Downloading...", "Steam files");
        return (normalized, "Local files only");
    }
}
