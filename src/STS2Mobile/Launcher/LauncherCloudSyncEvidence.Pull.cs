using System;
using System.Globalization;
using System.IO;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherCloudSyncEvidence
{
    internal static string LastManualPullUtc(string dataDir)
    {
        var utc = ReadUtc(LastManualPullMarkerPath(dataDir));
        return utc.HasValue
            ? utc.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
            : "<none>";
    }

    internal static bool LastManualPullUtcParseable(string dataDir)
        => ReadUtc(LastManualPullMarkerPath(dataDir)).HasValue;

    internal static string LastManualPullSelectedBranch(string dataDir)
        => ReadSelectedBranch(LastManualPullMarkerPath(dataDir)) ?? "<none>";

    internal static string LastManualPullSelectedBranchSelectionKind(string dataDir)
        => ReadMarkerValue(LastManualPullMarkerPath(dataDir), "Selected branch selection kind:") ?? "<none>";

    internal static string LastManualPullSelectorMode(string dataDir)
        => ReadMarkerValue(LastManualPullMarkerPath(dataDir), "Steam branch selector mode:") ?? "<none>";

    internal static string LastManualPullSelectedVersion(string dataDir)
        => ReadMarkerValue(LastManualPullMarkerPath(dataDir), "Selected version:") ?? "<none>";

    internal static string LastManualPullSelectedVersionSlotKind(string dataDir)
        => ReadMarkerValue(LastManualPullMarkerPath(dataDir), "Selected version slot kind:") ?? "<none>";

    internal static string LastManualPullSelectedVersionSlotDirectory(string dataDir)
        => ReadMarkerValue(LastManualPullMarkerPath(dataDir), "Selected version slot directory:") ?? "<none>";

    internal static bool LastManualPullCompletionRecorded(string dataDir)
        => LastManualPullBeforePushCompletionRecorded(dataDir)
            || HasCompletionFlag(LastManualPullMarkerPath(dataDir));

    internal static bool LastManualPullBeforePushCompletionRecorded(string dataDir)
        => HasCompletionFlag(LastManualPullMarkerPath(dataDir), "Manual Pull completed before Push:");

    internal static bool BaselineManualPushPrerequisitesSatisfied(string dataDir, string selectedBranch)
        => LastManualPullCompletionRecorded(dataDir)
            && LastManualPullMatchesSelectedBranch(dataDir, selectedBranch)
            && LauncherLocalSaveEvidence.HasImportantSaveEvidence(dataDir)
            && LauncherSaveOriginEvidence.CurrentLocalSavesMatchSelectedRuntime(dataDir, selectedBranch);

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
            LauncherSaveOriginEvidence.WriteManualPullOrigin(dataDir, selectedBranch);
            var text =
                $"UTC: {DateTime.UtcNow:O}\n"
                + $"Selected branch: {SteamGameBranch.Normalize(selectedBranch)}\n"
                + $"Selected branch selection kind: {SteamGameBranch.SelectionKind(selectedBranch)}\n"
                + $"Steam branch selector mode: {SteamGameBranch.SelectorMode}\n"
                + $"Selected version: {SteamGameBranch.DisplayName(selectedBranch)}\n"
                + $"Selected version slot kind: {SteamGameInstallPaths.VersionSlotKind(selectedBranch)}\n"
                + $"Selected version slot directory: {SteamGameInstallPaths.VersionSlotDirectory(dataDir, selectedBranch)}\n"
                + $"Selected branch note: {SteamGameBranch.SelectorHelpText(selectedBranch)}\n"
                + "Manual Pull completed before Push: true\n"
                + "Manual Pull completed before branch-switch Push: true\n";
            File.WriteAllText(LastManualPullMarkerPath(dataDir), text);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write manual Pull evidence marker: {ex.Message}");
        }
    }
}