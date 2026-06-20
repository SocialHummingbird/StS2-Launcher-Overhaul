using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherCloudSyncEvidence
{
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
}
