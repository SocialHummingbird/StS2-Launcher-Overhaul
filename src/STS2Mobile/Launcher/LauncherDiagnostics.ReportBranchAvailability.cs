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
        sb.AppendLine($"Steam branch availability UTC: {ReadBranchAvailabilityMarkerValue(dataDir, "UTC:")}");
        sb.AppendLine($"Steam branch availability selected branch: {ReadBranchAvailabilityMarkerValue(dataDir, "Selected branch:")}");
        sb.AppendLine($"Steam branch availability matches current selected branch: {BoolText(BranchAvailabilityMarkerMatchesSelectedBranch(dataDir))}");
        sb.AppendLine($"Steam branch availability selected branch visibility: {ReadBranchAvailabilityMarkerValue(dataDir, "Selected branch visibility:")}");
        sb.AppendLine($"Steam branch availability selected branch Windows depot manifests: {ReadBranchAvailabilityMarkerValue(dataDir, "Windows depot manifests for selected branch:")}");
        sb.AppendLine($"Steam branch availability selected branch downloadable: {BranchAvailabilitySelectedBranchDownloadable(dataDir)}");
        sb.AppendLine($"Steam branch availability selected branch problem: {BranchAvailabilitySelectedBranchProblem(dataDir)}");
        sb.AppendLine($"Steam branch availability visible branch count: {ReadBranchAvailabilityMarkerValue(dataDir, "Visible branch count:")}");
        sb.AppendLine($"Steam branch availability visible branches: {ReadBranchAvailabilityMarkerValues(dataDir, "Visible branch:")}");
    }
}
