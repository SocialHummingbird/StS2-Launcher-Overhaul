using System.IO;
using System.Text;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendBranchAvailability(StringBuilder sb, string dataDir)
    {
        var markerPath = SteamGameInstallPaths.BranchAvailabilityMarkerPath(dataDir);
        sb.AppendLine($"Steam branch availability marker filename: {SteamGameInstallPaths.BranchAvailabilityMarkerFileName}");
        sb.AppendLine($"Steam branch availability marker path: {markerPath}");
        sb.AppendLine($"Steam branch availability marker present: {BoolText(File.Exists(markerPath))}");
        sb.AppendLine($"Steam branch availability UTC: {ReadBranchAvailabilityMarkerValue(dataDir, SteamBranchAvailabilityMarkerFields.Utc)}");
        sb.AppendLine($"Steam branch availability selected branch: {ReadBranchAvailabilityMarkerValue(dataDir, SteamBranchAvailabilityMarkerFields.SelectedBranch)}");
        sb.AppendLine($"Steam branch availability matches current selected branch: {BoolText(BranchAvailabilityMarkerMatchesSelectedBranch(dataDir))}");
        sb.AppendLine($"Steam branch availability selected branch visibility: {ReadBranchAvailabilityMarkerValue(dataDir, SteamBranchAvailabilityMarkerFields.SelectedBranchVisibility)}");
        sb.AppendLine($"Steam branch availability selected branch Windows depot manifests: {ReadBranchAvailabilityMarkerValue(dataDir, SteamBranchAvailabilityMarkerFields.SelectedBranchWindowsDepotManifests)}");
        sb.AppendLine($"Steam branch availability selected branch downloadable: {BranchAvailabilitySelectedBranchDownloadable(dataDir)}");
        sb.AppendLine($"Steam branch availability selected branch problem: {BranchAvailabilitySelectedBranchProblem(dataDir)}");
        sb.AppendLine($"Steam branch availability visible branch count: {ReadBranchAvailabilityMarkerValue(dataDir, SteamBranchAvailabilityMarkerFields.VisibleBranchCount)}");
        sb.AppendLine($"Steam branch availability visible branches: {ReadBranchAvailabilityMarkerValues(dataDir, SteamBranchAvailabilityMarkerFields.VisibleBranch)}");
    }
}
