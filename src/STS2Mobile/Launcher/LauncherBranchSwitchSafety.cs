using System;
using System.Globalization;
using System.IO;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class LauncherBranchSwitchSafety
{
    internal const string MarkerFileName = "last_game_branch_switch.txt";

    internal static string MarkerPath(string dataDir)
        => Path.Combine(dataDir, MarkerFileName);

    internal static bool HasMarker(string dataDir)
        => File.Exists(MarkerPath(dataDir));

    internal static string MarkerUtc(string dataDir)
        => ReadMarkerValue(dataDir, "UTC:");

    internal static bool MarkerUtcParseable(string dataDir)
        => TryReadMarkerUtc(dataDir, out _);

    internal static string PreviousBranch(string dataDir)
        => ReadMarkerValue(dataDir, "Previous branch:");

    internal static string SelectedBranch(string dataDir)
        => ReadMarkerValue(dataDir, "Selected branch:");

    internal static string SelectedBranchSelectionKind(string dataDir)
        => ReadMarkerValue(dataDir, "Selected branch selection kind:");

    internal static string SelectorMode(string dataDir)
        => ReadMarkerValue(dataDir, "Steam branch selector mode:");

    internal static string SelectedVersion(string dataDir)
        => ReadMarkerValue(dataDir, "Selected version:");

    internal static string SelectedVersionSlotKind(string dataDir)
        => ReadMarkerValue(dataDir, "Selected version slot kind:");

    internal static string SelectedVersionSlotDirectory(string dataDir)
        => ReadMarkerValue(dataDir, "Selected version slot directory:");

    internal static string SelectedBranchNote(string dataDir)
        => ReadMarkerValue(dataDir, "Selected branch note:");

    internal static bool LocalBackupForced(string dataDir)
        => ReadMarkerBool(dataDir, "Local backup forced on:");

    internal static bool ManualPushRequiresBackupStorage(string dataDir)
        => ReadMarkerBool(dataDir, "Manual Push requires backup storage:");

    internal static bool WarningAcknowledged(string dataDir)
        => HasValue(ReadMarkerValue(dataDir, "Warning acknowledged:"));

    internal static bool NonPublicBranchWarningAcknowledged(string dataDir)
        => HasValue(ReadMarkerValue(dataDir, "Non-public branch warning acknowledged:"));

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

    internal static void WriteMarker(string dataDir, string previousBranch, string selectedBranch)
    {
        try
        {
            var text =
                $"UTC: {DateTime.UtcNow:O}\n"
                + $"Previous branch: {SteamGameBranch.Normalize(previousBranch)}\n"
                + $"Selected branch: {SteamGameBranch.Normalize(selectedBranch)}\n"
                + $"Selected branch selection kind: {SteamGameBranch.SelectionKind(selectedBranch)}\n"
                + $"Steam branch selector mode: {SteamGameBranch.SelectorMode}\n"
                + $"Selected version: {SteamGameBranch.DisplayName(selectedBranch)}\n"
                + $"Selected version slot kind: {SteamGameInstallPaths.VersionSlotKind(selectedBranch)}\n"
                + $"Selected version slot directory: {SteamGameInstallPaths.VersionSlotDirectory(dataDir, selectedBranch)}\n"
                + $"Selected branch note: {SteamGameBranch.SelectorHelpText(selectedBranch)}\n"
                + "Local backup forced on: true\n"
                + "Manual Push requires backup storage: true\n"
                + "Warning acknowledged: branch switch can require another download and saves may not be compatible between Steam branches.\n"
                + "Non-public branch warning acknowledged: branch may be private or password-protected; beta password entry is not implemented.\n";
            File.WriteAllText(MarkerPath(dataDir), text);
            LauncherSaveOriginEvidence.WriteBranchSwitchPendingOrigin(dataDir, previousBranch, selectedBranch);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write branch switch safety marker: {ex.Message}");
        }
    }

    private static string ReadMarkerValue(string dataDir, string prefix)
    {
        try
        {
            var path = MarkerPath(dataDir);
            if (!File.Exists(path))
                return "<none>";

            foreach (var line in File.ReadLines(path))
            {
                if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return line.Substring(prefix.Length).Trim();
            }
        }
        catch
        {
            return "<read failed>";
        }

        return "<missing>";
    }

    private static bool HasValue(string value)
        => !string.IsNullOrWhiteSpace(value) && !value.StartsWith("<", StringComparison.Ordinal);

    private static bool ReadMarkerBool(string dataDir, string prefix)
        => string.Equals(
            ReadMarkerValue(dataDir, prefix),
            "true",
            StringComparison.OrdinalIgnoreCase
        );

    private static bool TryReadMarkerUtc(string dataDir, out DateTime utc)
        => DateTime.TryParse(
            MarkerUtc(dataDir),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal,
            out utc
        );
}
