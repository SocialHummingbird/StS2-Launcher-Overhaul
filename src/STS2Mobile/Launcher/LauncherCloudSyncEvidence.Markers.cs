using System;

namespace STS2Mobile.Launcher;

internal static partial class LauncherCloudSyncEvidence
{
    private static string SanitizeSingleLine(string value)
        => string.IsNullOrWhiteSpace(value)
            ? "<none>"
            : value.Replace('\r', ' ').Replace('\n', ' ').Trim();

    private static DateTime? ReadUtc(string path)
        => LauncherMarkerFile.ReadUtc(path);

    private static string? ReadSelectedBranch(string path)
        => ReadMarkerValue(path, SelectedBranchPrefix);

    private static string? ReadMarkerValue(string path, string prefix)
        => LauncherMarkerFile.ReadOptionalValue(path, prefix);

    private static bool HasCompletionFlag(string path)
        => HasCompletionFlag(path, ManualPullCompletedBeforeBranchSwitchPushPrefix);

    private static bool HasCompletionFlag(string path, string prefix)
        => LauncherMarkerFile.ReadBoolFlag(path, prefix);
}
