using System;
using System.Globalization;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBranchSwitchSafety
{
    internal static string MarkerUtc(string dataDir)
        => ReadMarkerValue(dataDir, UtcPrefix);

    internal static bool MarkerUtcParseable(string dataDir)
        => TryReadMarkerUtc(dataDir, out _);

    internal static string PreviousBranch(string dataDir)
        => ReadMarkerValue(dataDir, PreviousBranchPrefix);

    internal static string SelectedBranch(string dataDir)
        => ReadMarkerValue(dataDir, SelectedBranchPrefix);

    internal static string SelectedBranchSelectionKind(string dataDir)
        => ReadMarkerValue(dataDir, SelectedBranchSelectionKindPrefix);

    internal static string SelectorMode(string dataDir)
        => ReadMarkerValue(dataDir, SelectorModePrefix);

    internal static string SelectedVersion(string dataDir)
        => ReadMarkerValue(dataDir, SelectedVersionPrefix);

    internal static string SelectedVersionSlotKind(string dataDir)
        => ReadMarkerValue(dataDir, SelectedVersionSlotKindPrefix);

    internal static string SelectedVersionSlotDirectory(string dataDir)
        => ReadMarkerValue(dataDir, SelectedVersionSlotDirectoryPrefix);

    internal static string SelectedBranchNote(string dataDir)
        => ReadMarkerValue(dataDir, SelectedBranchNotePrefix);

    internal static bool LocalBackupForced(string dataDir)
        => ReadMarkerBool(dataDir, LocalBackupForcedPrefix);

    internal static bool ManualPushRequiresBackupStorage(string dataDir)
        => ReadMarkerBool(dataDir, ManualPushRequiresBackupStoragePrefix);

    internal static bool WarningAcknowledged(string dataDir)
        => HasValue(ReadMarkerValue(dataDir, WarningAcknowledgedPrefix));

    internal static bool NonPublicBranchWarningAcknowledged(string dataDir)
        => HasValue(ReadMarkerValue(dataDir, NonPublicBranchWarningAcknowledgedPrefix));

    private static string ReadMarkerValue(string dataDir, string prefix)
        => LauncherMarkerFile.ReadValue(MarkerPath(dataDir), prefix);

    private static bool HasValue(string value)
        => LauncherMarkerFile.HasConcreteValue(value);

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
