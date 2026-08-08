using System;
using System.Globalization;

namespace STS2Mobile.Launcher;

internal static partial class LauncherSaveOriginEvidence
{
    internal static string OriginAction(string dataDir)
        => ReadMarkerValue(dataDir, OriginActionPrefix);

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
        => ReadMarkerValue(dataDir, SelectedBranchPrefix);

    internal static string SelectedVersion(string dataDir)
        => ReadMarkerValue(dataDir, SelectedVersionPrefix);

    internal static string SelectedVersionSlotKind(string dataDir)
        => ReadMarkerValue(dataDir, SelectedVersionSlotKindPrefix);

    internal static string SelectedVersionSlotDirectory(string dataDir)
        => ReadMarkerValue(dataDir, SelectedVersionSlotDirectoryPrefix);

    internal static string SelectedRuntimeSlotId(string dataDir)
        => ReadMarkerValue(dataDir, SelectedRuntimeSlotIdPrefix);

    internal static string SelectedPckSha256(string dataDir)
        => ReadMarkerValue(dataDir, SelectedPckSha256Prefix);

    internal static string SelectedSourceAssemblySha256(string dataDir)
        => ReadMarkerValue(dataDir, SelectedSourceAssemblySha256Prefix);

    internal static string SelectedRuntimePlayable(string dataDir)
        => ReadMarkerValue(dataDir, SelectedRuntimePlayablePrefix);

    internal static string SelectedRuntimeReadinessProblem(string dataDir)
        => ReadMarkerValue(dataDir, SelectedRuntimeReadinessProblemPrefix);

    internal static string ImportantLocalSaveEvidenceCount(string dataDir)
        => ReadMarkerValue(dataDir, ImportantLocalSaveEvidenceCountPrefix);

    private static DateTime? ReadUtc(string dataDir)
        => LauncherMarkerFile.ReadUtc(MarkerPath(dataDir));

    private static string ReadMarkerValue(string dataDir, string prefix)
        => LauncherMarkerFile.ReadValue(MarkerPath(dataDir), prefix);

    private static bool HasValue(string value)
        => LauncherMarkerFile.HasConcreteValue(value);
}
