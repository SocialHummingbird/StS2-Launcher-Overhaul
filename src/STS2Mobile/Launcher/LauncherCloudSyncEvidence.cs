using System;
using System.Globalization;
using System.IO;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class LauncherCloudSyncEvidence
{
    internal const string LastManualPullMarkerFileName = "last_manual_cloud_pull.txt";
    internal const string LastManualPushMarkerFileName = "last_manual_cloud_push.txt";
    internal const string LastManualPushBlockedMarkerFileName = "last_manual_cloud_push_blocked.txt";

    internal static string LastManualPullMarkerPath(string dataDir)
        => Path.Combine(dataDir, LastManualPullMarkerFileName);

    internal static string LastManualPushMarkerPath(string dataDir)
        => Path.Combine(dataDir, LastManualPushMarkerFileName);

    internal static string LastManualPushBlockedMarkerPath(string dataDir)
        => Path.Combine(dataDir, LastManualPushBlockedMarkerFileName);

    internal static string LastManualPullUtc(string dataDir)
    {
        var utc = ReadUtc(LastManualPullMarkerPath(dataDir));
        return utc.HasValue
            ? utc.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
            : "<none>";
    }

    internal static bool LastManualPullUtcParseable(string dataDir)
        => ReadUtc(LastManualPullMarkerPath(dataDir)).HasValue;

    internal static string LastManualPushUtc(string dataDir)
    {
        var utc = ReadUtc(LastManualPushMarkerPath(dataDir));
        return utc.HasValue
            ? utc.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
            : "<none>";
    }

    internal static bool LastManualPushUtcParseable(string dataDir)
        => ReadUtc(LastManualPushMarkerPath(dataDir)).HasValue;

    internal static string LatestManualPushEvidenceOutcome(string dataDir)
    {
        var pushUtc = ReadUtc(LastManualPushMarkerPath(dataDir));
        var blockedUtc = ReadUtc(LastManualPushBlockedMarkerPath(dataDir));

        if (!pushUtc.HasValue && !blockedUtc.HasValue)
            return "<none>";

        if (pushUtc.HasValue && (!blockedUtc.HasValue || pushUtc.Value >= blockedUtc.Value))
            return "completed";

        return "blocked-before-upload";
    }

    internal static string LatestManualPushEvidenceUtc(string dataDir)
    {
        var pushUtc = ReadUtc(LastManualPushMarkerPath(dataDir));
        var blockedUtc = ReadUtc(LastManualPushBlockedMarkerPath(dataDir));

        DateTime? latest = null;
        if (pushUtc.HasValue)
            latest = pushUtc.Value;

        if (blockedUtc.HasValue && (!latest.HasValue || blockedUtc.Value > latest.Value))
            latest = blockedUtc.Value;

        return latest.HasValue
            ? latest.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
            : "<none>";
    }

    internal static string LatestManualPushEvidenceSelectedBranch(string dataDir)
        => string.Equals(LatestManualPushEvidenceOutcome(dataDir), "blocked-before-upload", StringComparison.OrdinalIgnoreCase)
            ? LastManualPushBlockedSelectedBranch(dataDir)
            : LastManualPushSelectedBranch(dataDir);

    internal static string LatestManualPushEvidenceSelectedVersion(string dataDir)
        => string.Equals(LatestManualPushEvidenceOutcome(dataDir), "blocked-before-upload", StringComparison.OrdinalIgnoreCase)
            ? LastManualPushBlockedSelectedVersion(dataDir)
            : LastManualPushSelectedVersion(dataDir);

    internal static string LatestManualPushEvidenceSelectedVersionSlotKind(string dataDir)
        => string.Equals(LatestManualPushEvidenceOutcome(dataDir), "blocked-before-upload", StringComparison.OrdinalIgnoreCase)
            ? LastManualPushBlockedSelectedVersionSlotKind(dataDir)
            : LastManualPushSelectedVersionSlotKind(dataDir);

    internal static string LatestManualPushEvidenceSelectedVersionSlotDirectory(string dataDir)
        => string.Equals(LatestManualPushEvidenceOutcome(dataDir), "blocked-before-upload", StringComparison.OrdinalIgnoreCase)
            ? LastManualPushBlockedSelectedVersionSlotDirectory(dataDir)
            : LastManualPushSelectedVersionSlotDirectory(dataDir);

    internal static string LatestManualPushEvidenceReason(string dataDir)
    {
        var outcome = LatestManualPushEvidenceOutcome(dataDir);
        if (string.Equals(outcome, "blocked-before-upload", StringComparison.OrdinalIgnoreCase))
            return LastManualPushBlockedReason(dataDir);

        if (string.Equals(outcome, "completed", StringComparison.OrdinalIgnoreCase))
            return "Manual Push completed";

        return "<none>";
    }

    internal static string LastManualPushBlockedUtc(string dataDir)
    {
        var utc = ReadUtc(LastManualPushBlockedMarkerPath(dataDir));
        return utc.HasValue
            ? utc.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
            : "<none>";
    }

    internal static bool LastManualPushBlockedUtcParseable(string dataDir)
        => ReadUtc(LastManualPushBlockedMarkerPath(dataDir)).HasValue;

    internal static string LastManualPullSelectedBranch(string dataDir)
        => ReadSelectedBranch(LastManualPullMarkerPath(dataDir)) ?? "<none>";

    internal static string LastManualPullSelectedVersion(string dataDir)
        => ReadMarkerValue(LastManualPullMarkerPath(dataDir), "Selected version:") ?? "<none>";

    internal static string LastManualPullSelectedVersionSlotKind(string dataDir)
        => ReadMarkerValue(LastManualPullMarkerPath(dataDir), "Selected version slot kind:") ?? "<none>";

    internal static string LastManualPullSelectedVersionSlotDirectory(string dataDir)
        => ReadMarkerValue(LastManualPullMarkerPath(dataDir), "Selected version slot directory:") ?? "<none>";

    internal static string LastManualPushSelectedBranch(string dataDir)
        => ReadSelectedBranch(LastManualPushMarkerPath(dataDir)) ?? "<none>";

    internal static string LastManualPushSelectedVersion(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), "Selected version:") ?? "<none>";

    internal static string LastManualPushSelectedVersionSlotKind(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), "Selected version slot kind:") ?? "<none>";

    internal static string LastManualPushSelectedVersionSlotDirectory(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), "Selected version slot directory:") ?? "<none>";

    internal static string LastManualPushRecordedLocalBackupCount(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), "Pre-Push local backup evidence count:") ?? "<none>";

    internal static string LastManualPushRecordedCloudBackupCount(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), "Pre-Push cloud backup evidence count:") ?? "<none>";

    internal static string LastManualPushRecordedLatestLocalBackupUtc(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), "Latest pre-Push local backup UTC:") ?? "<none>";

    internal static string LastManualPushRecordedLatestCloudBackupUtc(string dataDir)
        => ReadMarkerValue(LastManualPushMarkerPath(dataDir), "Latest pre-Push cloud backup UTC:") ?? "<none>";

    internal static string LastManualPushBlockedSelectedBranch(string dataDir)
        => ReadSelectedBranch(LastManualPushBlockedMarkerPath(dataDir)) ?? "<none>";

    internal static string LastManualPushBlockedSelectedVersion(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), "Selected version:") ?? "<none>";

    internal static string LastManualPushBlockedSelectedVersionSlotKind(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), "Selected version slot kind:") ?? "<none>";

    internal static string LastManualPushBlockedSelectedVersionSlotDirectory(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), "Selected version slot directory:") ?? "<none>";

    internal static string LastManualPushBlockedRecordedPrerequisitesSatisfied(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), "Branch-switch manual Push prerequisites satisfied:") ?? "<none>";

    internal static string LastManualPushBlockedRecordedLocalBackupCount(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), "Pre-Push local backup evidence count:") ?? "<none>";

    internal static string LastManualPushBlockedRecordedCloudBackupCount(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), "Pre-Push cloud backup evidence count:") ?? "<none>";

    internal static string LastManualPushBlockedRecordedLatestLocalBackupUtc(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), "Latest pre-Push local backup UTC:") ?? "<none>";

    internal static string LastManualPushBlockedRecordedLatestCloudBackupUtc(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), "Latest pre-Push cloud backup UTC:") ?? "<none>";

    internal static string LastManualPushBlockedRecordedPrePushBackupEvidenceSatisfied(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), "Branch-switch pre-Push backup evidence satisfied:") ?? "<none>";

    internal static string LastManualPushBlockedReason(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), "Blocked reason:") ?? "<none>";

    internal static bool LastManualPullCompletionRecorded(string dataDir)
        => HasCompletionFlag(LastManualPullMarkerPath(dataDir));

    internal static bool LastManualPushCompletionRecorded(string dataDir)
        => HasCompletionFlag(LastManualPushMarkerPath(dataDir), "Manual Push completed after branch-switch safety gates:");

    internal static bool LastManualPushPrePushBackupEvidenceSatisfied(string dataDir)
        => HasCompletionFlag(LastManualPushMarkerPath(dataDir), "Branch-switch pre-Push backup evidence satisfied:");

    internal static bool LastManualPushBlockedBeforeUpload(string dataDir)
        => HasCompletionFlag(LastManualPushBlockedMarkerPath(dataDir), "Manual Push blocked before upload:");

    internal static bool LastManualPullIsAfterBranchSwitch(string dataDir)
    {
        var switchUtc = ReadUtc(LauncherBranchSwitchSafety.MarkerPath(dataDir));
        var pullUtc = ReadUtc(LastManualPullMarkerPath(dataDir));
        return switchUtc.HasValue && pullUtc.HasValue && pullUtc.Value >= switchUtc.Value;
    }

    internal static bool LastManualPullMatchesSelectedBranch(string dataDir, string selectedBranch)
    {
        var pullBranch = ReadSelectedBranch(LastManualPullMarkerPath(dataDir));
        return !string.IsNullOrWhiteSpace(pullBranch)
            && string.Equals(
                SteamGameBranch.Normalize(selectedBranch),
                SteamGameBranch.Normalize(pullBranch),
                StringComparison.OrdinalIgnoreCase
            );
    }

    internal static bool LastManualPushMatchesSelectedBranch(string dataDir, string selectedBranch)
    {
        var pushBranch = ReadSelectedBranch(LastManualPushMarkerPath(dataDir));
        return !string.IsNullOrWhiteSpace(pushBranch)
            && string.Equals(
                SteamGameBranch.Normalize(selectedBranch),
                SteamGameBranch.Normalize(pushBranch),
                StringComparison.OrdinalIgnoreCase
            );
    }

    internal static bool LastManualPushBlockedMatchesSelectedBranch(string dataDir, string selectedBranch)
    {
        var blockedBranch = ReadSelectedBranch(LastManualPushBlockedMarkerPath(dataDir));
        return !string.IsNullOrWhiteSpace(blockedBranch)
            && string.Equals(
                SteamGameBranch.Normalize(selectedBranch),
                SteamGameBranch.Normalize(blockedBranch),
                StringComparison.OrdinalIgnoreCase
            );
    }

    internal static bool LastManualPushIsAfterBranchSwitch(string dataDir)
    {
        var switchUtc = ReadUtc(LauncherBranchSwitchSafety.MarkerPath(dataDir));
        var pushUtc = ReadUtc(LastManualPushMarkerPath(dataDir));
        return switchUtc.HasValue && pushUtc.HasValue && pushUtc.Value >= switchUtc.Value;
    }

    internal static bool HasManualPushAfterBranchSwitch(string dataDir, string selectedBranch)
    {
        if (!string.Equals(LatestManualPushEvidenceOutcome(dataDir), "completed", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!LastManualPushIsAfterBranchSwitch(dataDir))
            return false;

        if (!LastManualPushCompletionRecorded(dataDir))
            return false;

        if (!LastManualPushMatchesSelectedBranch(dataDir, selectedBranch))
            return false;

        return LastManualPushPrePushBackupEvidenceSatisfied(dataDir);
    }

    internal static bool HasManualPullAfterBranchSwitch(string dataDir, string selectedBranch)
    {
        if (!LastManualPullIsAfterBranchSwitch(dataDir))
            return false;

        if (!LastManualPullCompletionRecorded(dataDir))
            return false;

        return LastManualPullMatchesSelectedBranch(dataDir, selectedBranch);
    }

    internal static void WriteManualPullMarker(string dataDir, string selectedBranch)
    {
        try
        {
            var text =
                $"UTC: {DateTime.UtcNow:O}\n"
                + $"Selected branch: {SteamGameBranch.Normalize(selectedBranch)}\n"
                + $"Selected version: {SteamGameBranch.DisplayName(selectedBranch)}\n"
                + $"Selected version slot kind: {SteamGameInstallPaths.VersionSlotKind(selectedBranch)}\n"
                + $"Selected version slot directory: {SteamGameInstallPaths.VersionSlotDirectory(dataDir, selectedBranch)}\n"
                + $"Selected branch note: {SteamGameBranch.SelectorHelpText(selectedBranch)}\n"
                + "Manual Pull completed before branch-switch Push: true\n";
            File.WriteAllText(LastManualPullMarkerPath(dataDir), text);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write manual Pull evidence marker: {ex.Message}");
        }
    }

    internal static void WriteManualPushMarker(string dataDir, string selectedBranch)
    {
        try
        {
            var text =
                $"UTC: {DateTime.UtcNow:O}\n"
                + $"Selected branch: {SteamGameBranch.Normalize(selectedBranch)}\n"
                + $"Selected version: {SteamGameBranch.DisplayName(selectedBranch)}\n"
                + $"Selected version slot kind: {SteamGameInstallPaths.VersionSlotKind(selectedBranch)}\n"
                + $"Selected version slot directory: {SteamGameInstallPaths.VersionSlotDirectory(dataDir, selectedBranch)}\n"
                + $"Selected branch note: {SteamGameBranch.SelectorHelpText(selectedBranch)}\n"
                + $"Pre-Push local backup evidence count: {LauncherBackupEvidence.LocalPrePushBackupCount()}\n"
                + $"Pre-Push cloud backup evidence count: {LauncherBackupEvidence.CloudPrePushBackupCount()}\n"
                + $"Latest pre-Push local backup UTC: {LauncherBackupEvidence.LatestLocalPrePushBackupUtc()}\n"
                + $"Latest pre-Push cloud backup UTC: {LauncherBackupEvidence.LatestCloudPrePushBackupUtc()}\n"
                + $"Branch-switch pre-Push backup evidence satisfied: {LauncherBackupEvidence.HasPrePushBackupEvidenceAfterBranchSwitch(dataDir).ToString().ToLowerInvariant()}\n"
                + "Manual Push completed after branch-switch safety gates: true\n";
            File.WriteAllText(LastManualPushMarkerPath(dataDir), text);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write manual Push evidence marker: {ex.Message}");
        }
    }

    internal static void WriteManualPushBlockedMarker(string dataDir, string selectedBranch, Exception ex)
        => WriteManualPushBlockedMarker(dataDir, selectedBranch, ex.Message);

    internal static void WriteManualPushBlockedMarker(string dataDir, string selectedBranch, string reason)
    {
        try
        {
            if (!reason.StartsWith("Manual Push blocked:", StringComparison.OrdinalIgnoreCase))
                return;

            var text =
                $"UTC: {DateTime.UtcNow:O}\n"
                + $"Selected branch: {SteamGameBranch.Normalize(selectedBranch)}\n"
                + $"Selected version: {SteamGameBranch.DisplayName(selectedBranch)}\n"
                + $"Selected version slot kind: {SteamGameInstallPaths.VersionSlotKind(selectedBranch)}\n"
                + $"Selected version slot directory: {SteamGameInstallPaths.VersionSlotDirectory(dataDir, selectedBranch)}\n"
                + $"Selected branch note: {SteamGameBranch.SelectorHelpText(selectedBranch)}\n"
                + $"Branch-switch manual Push prerequisites satisfied: {LauncherBranchSwitchSafety.ManualPushPrerequisitesSatisfied(dataDir, selectedBranch).ToString().ToLowerInvariant()}\n"
                + $"Pre-Push local backup evidence count: {LauncherBackupEvidence.LocalPrePushBackupCount()}\n"
                + $"Pre-Push cloud backup evidence count: {LauncherBackupEvidence.CloudPrePushBackupCount()}\n"
                + $"Latest pre-Push local backup UTC: {LauncherBackupEvidence.LatestLocalPrePushBackupUtc()}\n"
                + $"Latest pre-Push cloud backup UTC: {LauncherBackupEvidence.LatestCloudPrePushBackupUtc()}\n"
                + $"Branch-switch pre-Push backup evidence satisfied: {LauncherBackupEvidence.HasPrePushBackupEvidenceAfterBranchSwitch(dataDir).ToString().ToLowerInvariant()}\n"
                + $"Blocked reason: {SanitizeSingleLine(reason)}\n"
                + "Manual Push blocked before upload: true\n";
            File.WriteAllText(LastManualPushBlockedMarkerPath(dataDir), text);
        }
        catch (Exception markerEx)
        {
            PatchHelper.Log($"[Launcher] Failed to write manual Push blocked evidence marker: {markerEx.Message}");
        }
    }

    private static string SanitizeSingleLine(string value)
        => string.IsNullOrWhiteSpace(value)
            ? "<none>"
            : value.Replace('\r', ' ').Replace('\n', ' ').Trim();

    private static DateTime? ReadUtc(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            foreach (var line in File.ReadLines(path))
            {
                const string prefix = "UTC:";
                if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                var value = line.Substring(prefix.Length).Trim();
                return DateTime.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal,
                    out var utc
                )
                    ? utc.ToUniversalTime()
                    : null;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static string? ReadSelectedBranch(string path)
        => ReadMarkerValue(path, "Selected branch:");

    private static string? ReadMarkerValue(string path, string prefix)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            foreach (var line in File.ReadLines(path))
            {
                if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return line.Substring(prefix.Length).Trim();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static bool HasCompletionFlag(string path)
        => HasCompletionFlag(path, "Manual Pull completed before branch-switch Push:");

    private static bool HasCompletionFlag(string path, string prefix)
    {
        try
        {
            if (!File.Exists(path))
                return false;

            foreach (var line in File.ReadLines(path))
            {
                if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                return string.Equals(
                    line.Substring(prefix.Length).Trim(),
                    "true",
                    StringComparison.OrdinalIgnoreCase
                );
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}
