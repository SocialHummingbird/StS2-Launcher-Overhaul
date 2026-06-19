using System;
using System.Globalization;
using System.IO;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class LauncherSaveOriginEvidence
{
    internal const string MarkerFileName = "current_android_save_origin.txt";

    internal static string MarkerPath(string dataDir)
        => Path.Combine(dataDir, MarkerFileName);

    internal static bool MarkerPresent(string dataDir)
        => File.Exists(MarkerPath(dataDir));

    internal static string OriginAction(string dataDir)
        => ReadMarkerValue(dataDir, "Origin action:");

    internal static string OriginUtc(string dataDir)
    {
        var utc = ReadUtc(dataDir);
        return utc.HasValue
            ? utc.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
            : "<none>";
    }

    internal static bool OriginUtcParseable(string dataDir)
        => ReadUtc(dataDir).HasValue;

    internal static string SelectedBranch(string dataDir)
        => ReadMarkerValue(dataDir, "Selected branch:");

    internal static string SelectedVersion(string dataDir)
        => ReadMarkerValue(dataDir, "Selected version:");

    internal static string SelectedVersionSlotKind(string dataDir)
        => ReadMarkerValue(dataDir, "Selected version slot kind:");

    internal static string SelectedVersionSlotDirectory(string dataDir)
        => ReadMarkerValue(dataDir, "Selected version slot directory:");

    internal static string SelectedRuntimeSlotId(string dataDir)
        => ReadMarkerValue(dataDir, "Selected runtime slot ID:");

    internal static string SelectedPckSha256(string dataDir)
        => ReadMarkerValue(dataDir, "Selected PCK SHA256:");

    internal static string SelectedSourceAssemblySha256(string dataDir)
        => ReadMarkerValue(dataDir, "Selected source sts2.dll SHA256:");

    internal static string SelectedRuntimePlayable(string dataDir)
        => ReadMarkerValue(dataDir, "Selected runtime playable:");

    internal static string SelectedRuntimeReadinessProblem(string dataDir)
        => ReadMarkerValue(dataDir, "Selected runtime readiness problem:");

    internal static string ImportantLocalSaveEvidenceCount(string dataDir)
        => ReadMarkerValue(dataDir, "Important Android local save evidence count:");

    internal static bool MatchesSelectedBranch(string dataDir, string selectedBranch)
    {
        var markerBranch = SelectedBranch(dataDir);
        return HasValue(markerBranch)
            && string.Equals(
                SteamGameBranch.Normalize(markerBranch),
                SteamGameBranch.Normalize(selectedBranch),
                StringComparison.OrdinalIgnoreCase
            );
    }

    internal static bool CurrentLocalSavesMatchSelectedBranch(string dataDir, string selectedBranch)
        => MarkerPresent(dataDir)
            && OriginUtcParseable(dataDir)
            && MatchesSelectedBranch(dataDir, selectedBranch)
            && LauncherLocalSaveEvidence.HasImportantSaveEvidence(dataDir);

    internal static bool PckMatchesSelectedRuntime(string dataDir, string selectedBranch)
    {
        var markerHash = SelectedPckSha256(dataDir);
        if (!HasValue(markerHash))
            return false;

        var slot = GameRuntimeSlot.Inspect(dataDir, selectedBranch);
        if (HasValue(slot.PckSha256)
            && string.Equals(markerHash, slot.PckSha256, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return slot.RuntimePackUsable
            && slot.RuntimePack.SourcePckMatchesSelectedPck(markerHash, slot.PckPath);
    }

    internal static bool SourceAssemblyMatchesSelectedRuntime(string dataDir, string selectedBranch)
    {
        var markerHash = SelectedSourceAssemblySha256(dataDir);
        if (!HasValue(markerHash))
            return false;

        var slot = GameRuntimeSlot.Inspect(dataDir, selectedBranch);
        return HasValue(slot.SourceAssemblySha256)
            && string.Equals(markerHash, slot.SourceAssemblySha256, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool RuntimeSlotIdMatchesSelectedRuntime(string dataDir, string selectedBranch)
    {
        var markerSlotId = SelectedRuntimeSlotId(dataDir);
        if (!HasValue(markerSlotId))
            return false;

        var slot = GameRuntimeSlot.Inspect(dataDir, selectedBranch);
        return HasValue(slot.RuntimeSlotId)
            && string.Equals(markerSlotId, slot.RuntimeSlotId, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool SelectedRuntimeCurrentlyPlayable(string dataDir, string selectedBranch)
        => GameRuntimeSlot.Inspect(dataDir, selectedBranch).Playable;

    internal static bool CurrentLocalSavesMatchSelectedRuntime(string dataDir, string selectedBranch)
    {
        if (!CurrentLocalSavesMatchSelectedBranch(dataDir, selectedBranch))
            return false;

        var markerSlotId = SelectedRuntimeSlotId(dataDir);
        var markerSourceAssemblySha256 = SelectedSourceAssemblySha256(dataDir);
        if (!HasValue(markerSlotId) || !HasValue(markerSourceAssemblySha256))
            return false;

        var slot = GameRuntimeSlot.Inspect(dataDir, selectedBranch);
        return slot.Playable
            && HasValue(slot.RuntimeSlotId)
            && HasValue(slot.SourceAssemblySha256)
            && string.Equals(markerSlotId, slot.RuntimeSlotId, StringComparison.OrdinalIgnoreCase)
            && PckMatchesSelectedRuntime(dataDir, selectedBranch)
            && string.Equals(markerSourceAssemblySha256, slot.SourceAssemblySha256, StringComparison.OrdinalIgnoreCase);
    }

    internal static void WriteManualPullOrigin(string dataDir, string selectedBranch)
        => WriteOrigin(dataDir, selectedBranch, "manual cloud pull");

    internal static void WriteManualPushOrigin(string dataDir, string selectedBranch)
        => WriteOrigin(dataDir, selectedBranch, "manual cloud push completed from Android local saves");

    internal static void WriteBranchSwitchPendingOrigin(string dataDir, string previousBranch, string selectedBranch)
    {
        try
        {
            var text =
                $"UTC: {DateTime.UtcNow:O}\n"
                + "Origin action: branch switch pending Pull\n"
                + $"Previous branch: {SteamGameBranch.Normalize(previousBranch)}\n"
                + $"Selected branch: {SteamGameBranch.Normalize(selectedBranch)}\n"
                + $"Selected version: {SteamGameBranch.DisplayName(selectedBranch)}\n"
                + $"Selected version slot kind: {SteamGameInstallPaths.VersionSlotKind(selectedBranch)}\n"
                + $"Selected version slot directory: {SteamGameInstallPaths.VersionSlotDirectory(dataDir, selectedBranch)}\n"
                + "Current Android local saves verified for selected branch: false\n"
                + "Required next action: Pull from Cloud for the selected game version before Push.\n";
            File.WriteAllText(MarkerPath(dataDir), text);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write save-origin branch-switch marker: {ex.Message}");
        }
    }

    private static void WriteOrigin(string dataDir, string selectedBranch, string originAction)
    {
        try
        {
            selectedBranch = SteamGameBranch.Normalize(selectedBranch);
            var slot = GameRuntimeSlot.Inspect(dataDir, selectedBranch);
            var importantLocalSaveEvidencePresent = LauncherLocalSaveEvidence.HasImportantSaveEvidence(dataDir);
            var selectedBranchVerified = importantLocalSaveEvidencePresent;
            var selectedRuntimePlayable = slot.Playable;
            var selectedRuntimeVerified = selectedBranchVerified && selectedRuntimePlayable;
            var text =
                $"UTC: {DateTime.UtcNow:O}\n"
                + $"Origin action: {originAction}\n"
                + $"Selected branch: {selectedBranch}\n"
                + $"Selected branch selection kind: {SteamGameBranch.SelectionKind(selectedBranch)}\n"
                + $"Steam branch selector mode: {SteamGameBranch.SelectorMode}\n"
                + $"Selected version: {SteamGameBranch.DisplayName(selectedBranch)}\n"
                + $"Selected version slot kind: {SteamGameInstallPaths.VersionSlotKind(selectedBranch)}\n"
                + $"Selected version slot directory: {SteamGameInstallPaths.VersionSlotDirectory(dataDir, selectedBranch)}\n"
                + $"Selected runtime slot ID: {slot.RuntimeSlotId}\n"
                + $"Selected PCK SHA256: {slot.PckSha256}\n"
                + $"Selected source sts2.dll SHA256: {slot.SourceAssemblySha256}\n"
                + $"Selected runtime playable: {selectedRuntimePlayable.ToString().ToLowerInvariant()}\n"
                + $"Selected runtime readiness problem: {slot.ReadinessProblem() ?? string.Empty}\n"
                + $"Important Android local save evidence count: {LauncherLocalSaveEvidence.CountImportantSaveEvidence(dataDir)}\n"
                + $"Current Android local saves verified for selected branch: {selectedBranchVerified.ToString().ToLowerInvariant()}\n"
                + $"Current Android local saves verified for selected runtime: {selectedRuntimeVerified.ToString().ToLowerInvariant()}\n";
            File.WriteAllText(MarkerPath(dataDir), text);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write save-origin marker: {ex.Message}");
        }
    }

    private static DateTime? ReadUtc(string dataDir)
    {
        try
        {
            if (!File.Exists(MarkerPath(dataDir)))
                return null;

            foreach (var line in File.ReadLines(MarkerPath(dataDir)))
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
}
