using System;
using System.Globalization;

namespace STS2Mobile.Launcher;

internal static partial class LauncherCloudSyncEvidence
{
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
        => LatestManualPushEvidenceBlocked(dataDir)
            ? LastManualPushBlockedSelectedBranch(dataDir)
            : LastManualPushSelectedBranch(dataDir);

    internal static string LatestManualPushEvidenceSelectedBranchSelectionKind(string dataDir)
        => LatestManualPushEvidenceBlocked(dataDir)
            ? LastManualPushBlockedSelectedBranchSelectionKind(dataDir)
            : LastManualPushSelectedBranchSelectionKind(dataDir);

    internal static string LatestManualPushEvidenceSelectorMode(string dataDir)
        => LatestManualPushEvidenceBlocked(dataDir)
            ? LastManualPushBlockedSelectorMode(dataDir)
            : LastManualPushSelectorMode(dataDir);

    internal static string LatestManualPushEvidenceSelectedVersion(string dataDir)
        => LatestManualPushEvidenceBlocked(dataDir)
            ? LastManualPushBlockedSelectedVersion(dataDir)
            : LastManualPushSelectedVersion(dataDir);

    internal static string LatestManualPushEvidenceSelectedVersionSlotKind(string dataDir)
        => LatestManualPushEvidenceBlocked(dataDir)
            ? LastManualPushBlockedSelectedVersionSlotKind(dataDir)
            : LastManualPushSelectedVersionSlotKind(dataDir);

    internal static string LatestManualPushEvidenceSelectedVersionSlotDirectory(string dataDir)
        => LatestManualPushEvidenceBlocked(dataDir)
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

    private static bool LatestManualPushEvidenceBlocked(string dataDir)
        => string.Equals(
            LatestManualPushEvidenceOutcome(dataDir),
            "blocked-before-upload",
            StringComparison.OrdinalIgnoreCase
        );
}
