using System;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBranchAvailabilityStatus
{
    private static string SelectedStatus(
        string selectedBranch,
        string visibility,
        string manifestCount,
        string selectedBranchMarker
    )
    {
        selectedBranch = string.IsNullOrWhiteSpace(selectedBranch) ? "selected branch" : selectedBranch.Trim();
        manifestCount = string.IsNullOrWhiteSpace(manifestCount) ? "0" : manifestCount.Trim();

        if (!int.TryParse(manifestCount, out var windowsManifests))
            windowsManifests = 0;

        if (MarkerValuePasswordProtected(selectedBranchMarker))
            return $"{selectedBranch} is password-protected and cannot be downloaded until Steam beta password entry is supported";

        if (windowsManifests > 0)
            return $"{selectedBranch} has {windowsManifests} Windows depot manifest(s)";

        if (!string.IsNullOrWhiteSpace(visibility)
            && visibility.IndexOf("not listed", StringComparison.OrdinalIgnoreCase) >= 0)
            return $"{selectedBranch} is not listed for this Steam account and has no Windows manifest";

        return $"{selectedBranch} is visible but has no Windows manifest";
    }

    private static string VisibleBranchStatus(string markerValue)
    {
        if (string.IsNullOrWhiteSpace(markerValue))
            return null;

        var nameEnd = markerValue.IndexOf(" [", StringComparison.Ordinal);
        var name = nameEnd > 0 ? markerValue[..nameEnd] : markerValue;
        if (MarkerValuePasswordProtected(markerValue))
            return $"{name} (password-protected)";

        var downloadable = !markerValue.Contains(ZeroWindowsManifestsMarker, StringComparison.OrdinalIgnoreCase);
        return downloadable ? $"{name} (downloadable)" : $"{name} (no Windows manifest)";
    }

    private static string RemoveRawBranchAvailabilitySummary(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return message;

        var markerIndex = message.IndexOf(RawSelectedBranchVisibilitySummaryMarker, StringComparison.OrdinalIgnoreCase);
        return markerIndex < 0
            ? message.Trim()
            : message[..markerIndex].TrimEnd();
    }
}
