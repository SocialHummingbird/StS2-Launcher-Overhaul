using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBranchSwitchSafety
{
    internal static bool HasRequiredEvidence(string dataDir)
    {
        if (!HasMarker(dataDir))
            return false;

        return MarkerUtcParseable(dataDir)
            && HasValue(PreviousBranch(dataDir))
            && HasValue(SelectedBranch(dataDir))
            && HasValue(SelectedBranchSelectionKind(dataDir))
            && HasValue(SelectorMode(dataDir))
            && HasValue(SelectedVersion(dataDir))
            && HasValue(SelectedVersionSlotKind(dataDir))
            && HasValue(SelectedVersionSlotDirectory(dataDir))
            && HasValue(SelectedBranchNote(dataDir))
            && LocalBackupForced(dataDir)
            && ManualPushRequiresBackupStorage(dataDir)
            && WarningAcknowledged(dataDir)
            && NonPublicBranchWarningAcknowledged(dataDir);
    }

    internal static bool HasRequiredEvidence(string dataDir, string selectedBranch)
        => HasRequiredEvidence(dataDir)
            && SelectedBranchMatches(dataDir, selectedBranch);

    internal static bool SelectedBranchMatches(string dataDir, string selectedBranch)
    {
        var markerBranch = SelectedBranch(dataDir);
        return HasValue(markerBranch)
            && string.Equals(
                SteamGameBranch.Normalize(markerBranch),
                SteamGameBranch.Normalize(selectedBranch),
                StringComparison.OrdinalIgnoreCase
            );
    }

    internal static bool ManualPushPrerequisitesSatisfied(string dataDir, string selectedBranch)
    {
        if (!HasMarker(dataDir))
            return false;

        return HasRequiredEvidence(dataDir, selectedBranch)
            && LauncherCloudSyncEvidence.HasManualPullAfterBranchSwitch(dataDir, selectedBranch)
            && LauncherLocalSaveEvidence.HasImportantSaveEvidence(dataDir)
            && LauncherSaveOriginEvidence.CurrentLocalSavesMatchSelectedRuntime(dataDir, selectedBranch)
            && STS2Mobile.AppPaths.HasStoragePermission();
    }
}
