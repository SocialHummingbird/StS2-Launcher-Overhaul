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
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), SelectedBranchSelectionKindPrefix) ?? "<none>";

    internal static string LastManualPushBlockedSelectorMode(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), SelectorModePrefix) ?? "<none>";

    internal static string LastManualPushBlockedSelectedVersion(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), SelectedVersionPrefix) ?? "<none>";

    internal static string LastManualPushBlockedSelectedVersionSlotKind(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), SelectedVersionSlotKindPrefix) ?? "<none>";

    internal static string LastManualPushBlockedSelectedVersionSlotDirectory(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), SelectedVersionSlotDirectoryPrefix) ?? "<none>";

    internal static string LastManualPushBlockedRecordedPrerequisitesSatisfied(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), BranchSwitchManualPushPrerequisitesSatisfiedPrefix) ?? "<none>";

    internal static string LastManualPushBlockedRecordedLocalBackupCount(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), PrePushLocalBackupEvidenceCountPrefix) ?? "<none>";

    internal static string LastManualPushBlockedRecordedCloudBackupCount(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), PrePushCloudBackupEvidenceCountPrefix) ?? "<none>";

    internal static string LastManualPushBlockedRecordedLatestLocalBackupUtc(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), LatestPrePushLocalBackupUtcPrefix) ?? "<none>";

    internal static string LastManualPushBlockedRecordedLatestCloudBackupUtc(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), LatestPrePushCloudBackupUtcPrefix) ?? "<none>";

    internal static string LastManualPushBlockedRecordedImportantLocalSaveEvidenceCount(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), ImportantLocalSaveEvidenceCountPrefix) ?? "<none>";

    internal static string LastManualPushBlockedRecordedBaselinePrerequisitesSatisfied(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), BaselineManualPushPrerequisitesSatisfiedPrefix) ?? "<none>";

    internal static string LastManualPushBlockedRecordedPrePushBackupEvidenceSatisfied(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), BranchSwitchPrePushBackupEvidenceSatisfiedPrefix) ?? "<none>";

    internal static string LastManualPushBlockedReason(string dataDir)
        => ReadMarkerValue(LastManualPushBlockedMarkerPath(dataDir), BlockedReasonPrefix) ?? "<none>";

    internal static bool LastManualPushBlockedBeforeUpload(string dataDir)
        => HasCompletionFlag(LastManualPushBlockedMarkerPath(dataDir), ManualPushBlockedBeforeUploadPrefix);

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
            if (!reason.StartsWith(ManualPushBlockedReasonPrefix, StringComparison.OrdinalIgnoreCase))
                return;

            var text =
                $"{UtcPrefix} {DateTime.UtcNow:O}\n"
                + $"{SelectedBranchPrefix} {SteamGameBranch.Normalize(selectedBranch)}\n"
                + $"{SelectedBranchSelectionKindPrefix} {SteamGameBranch.SelectionKind(selectedBranch)}\n"
                + $"{SelectorModePrefix} {SteamGameBranch.SelectorMode}\n"
                + $"{SelectedVersionPrefix} {SteamGameBranch.DisplayName(selectedBranch)}\n"
                + $"{SelectedVersionSlotKindPrefix} {SteamGameInstallPaths.VersionSlotKind(selectedBranch)}\n"
                + $"{SelectedVersionSlotDirectoryPrefix} {SteamGameInstallPaths.VersionSlotDirectory(dataDir, selectedBranch)}\n"
                + $"{SelectedBranchNotePrefix} {SteamGameBranch.SelectorHelpText(selectedBranch)}\n"
                + $"{BranchSwitchManualPushPrerequisitesSatisfiedPrefix} {LauncherBranchSwitchSafety.ManualPushPrerequisitesSatisfied(dataDir, selectedBranch).ToString().ToLowerInvariant()}\n"
                + $"{PrePushLocalBackupEvidenceCountPrefix} {LauncherBackupEvidence.LocalPrePushBackupCount()}\n"
                + $"{PrePushCloudBackupEvidenceCountPrefix} {LauncherBackupEvidence.CloudPrePushBackupCount()}\n"
                + $"{LatestPrePushLocalBackupUtcPrefix} {LauncherBackupEvidence.LatestLocalPrePushBackupUtc()}\n"
                + $"{LatestPrePushCloudBackupUtcPrefix} {LauncherBackupEvidence.LatestCloudPrePushBackupUtc()}\n"
                + $"{ImportantLocalSaveEvidenceCountPrefix} {LauncherLocalSaveEvidence.CountImportantSaveEvidence(dataDir)}\n"
                + $"{BaselineManualPushPrerequisitesSatisfiedPrefix} {BaselineManualPushPrerequisitesSatisfied(dataDir, selectedBranch).ToString().ToLowerInvariant()}\n"
                + $"{BranchSwitchPrePushBackupEvidenceSatisfiedPrefix} {LauncherBackupEvidence.HasPrePushBackupEvidenceAfterBranchSwitch(dataDir).ToString().ToLowerInvariant()}\n"
                + $"{BlockedReasonPrefix} {SanitizeSingleLine(reason)}\n"
                + $"{ManualPushBlockedBeforeUploadPrefix} true\n";
            File.WriteAllText(LastManualPushBlockedMarkerPath(dataDir), text);
        }
        catch (Exception markerEx)
        {
            PatchHelper.Log($"[Launcher] Failed to write manual Push blocked evidence marker: {markerEx.Message}");
        }
    }
}
