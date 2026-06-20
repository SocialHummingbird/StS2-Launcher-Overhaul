using System;
using System.Globalization;
using System.IO;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherCloudSyncEvidence
{
    internal static string LastManualPushBlockedUtc(string dataDir)
    {
        var utc = ReadUtc(LastManualPushBlockedMarkerPath(dataDir));
        return utc.HasValue
            ? utc.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
            : "<none>";
    }

    internal static bool LastManualPushBlockedUtcParseable(string dataDir)
        => ReadUtc(LastManualPushBlockedMarkerPath(dataDir)).HasValue;

    internal static string LastManualPushBlockedSelectedBranch(string dataDir)
        => ReadSelectedBranch(LastManualPushBlockedMarkerPath(dataDir)) ?? "<none>";

    internal static string LastManualPushBlockedSelectedBranchSelectionKind(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), "Selected branch selection kind:") ?? "<none>";

    internal static string LastManualPushBlockedSelectorMode(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), "Steam branch selector mode:") ?? "<none>";

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

    internal static string LastManualPushBlockedRecordedImportantLocalSaveEvidenceCount(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), "Important Android local save evidence count:") ?? "<none>";

    internal static string LastManualPushBlockedRecordedBaselinePrerequisitesSatisfied(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), "Baseline manual Push prerequisites satisfied:") ?? "<none>";

    internal static string LastManualPushBlockedRecordedPrePushBackupEvidenceSatisfied(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), "Branch-switch pre-Push backup evidence satisfied:") ?? "<none>";

    internal static string LastManualPushBlockedReason(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), "Blocked reason:") ?? "<none>";

    internal static bool LastManualPushBlockedBeforeUpload(string dataDir)
        => HasCompletionFlag(LastManualPushBlockedMarkerPath(dataDir), "Manual Push blocked before upload:");

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
                + $"Selected branch selection kind: {SteamGameBranch.SelectionKind(selectedBranch)}\n"
                + $"Steam branch selector mode: {SteamGameBranch.SelectorMode}\n"
                + $"Selected version: {SteamGameBranch.DisplayName(selectedBranch)}\n"
                + $"Selected version slot kind: {SteamGameInstallPaths.VersionSlotKind(selectedBranch)}\n"
                + $"Selected version slot directory: {SteamGameInstallPaths.VersionSlotDirectory(dataDir, selectedBranch)}\n"
                + $"Selected branch note: {SteamGameBranch.SelectorHelpText(selectedBranch)}\n"
                + $"Branch-switch manual Push prerequisites satisfied: {LauncherBranchSwitchSafety.ManualPushPrerequisitesSatisfied(dataDir, selectedBranch).ToString().ToLowerInvariant()}\n"
                + $"Pre-Push local backup evidence count: {LauncherBackupEvidence.LocalPrePushBackupCount()}\n"
                + $"Pre-Push cloud backup evidence count: {LauncherBackupEvidence.CloudPrePushBackupCount()}\n"
                + $"Latest pre-Push local backup UTC: {LauncherBackupEvidence.LatestLocalPrePushBackupUtc()}\n"
                + $"Latest pre-Push cloud backup UTC: {LauncherBackupEvidence.LatestCloudPrePushBackupUtc()}\n"
                + $"Important Android local save evidence count: {LauncherLocalSaveEvidence.CountImportantSaveEvidence(dataDir)}\n"
                + $"Baseline manual Push prerequisites satisfied: {BaselineManualPushPrerequisitesSatisfied(dataDir, selectedBranch).ToString().ToLowerInvariant()}\n"
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
}