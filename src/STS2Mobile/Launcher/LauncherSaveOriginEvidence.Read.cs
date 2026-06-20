using System;
using System.Globalization;

namespace STS2Mobile.Launcher;

internal static partial class LauncherSaveOriginEvidence
{
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

    private static DateTime? ReadUtc(string dataDir)
        => LauncherMarkerFile.ReadUtc(MarkerPath(dataDir));

    private static string ReadMarkerValue(string dataDir, string prefix)
        => LauncherMarkerFile.ReadValue(MarkerPath(dataDir), prefix);

    private static bool HasValue(string value)
        => LauncherMarkerFile.HasConcreteValue(value);
}
