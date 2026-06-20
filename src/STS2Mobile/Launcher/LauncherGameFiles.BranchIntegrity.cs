using System.IO;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameFiles
{
    internal static string BranchIntegritySummary(string dataDir, string branch)
    {
        branch = SteamGameBranch.Normalize(branch);
        if (string.Equals(branch, SteamGameBranch.Public, System.StringComparison.OrdinalIgnoreCase))
            return null;

        var markerPath = SteamGameInstallPaths.BranchMarkerPath(dataDir, branch);
        if (!File.Exists(markerPath))
            return "Selected branch integrity evidence is unavailable because steam_branch.txt is missing.";

        var total = ReadMarkerInt(markerPath, BranchMarkerDepotManifestCountPrefix);
        var matchingPublic = ReadMarkerInt(markerPath, BranchMarkerDepotsMatchingPublicPrefix);
        var differingPublic = ReadMarkerInt(markerPath, BranchMarkerDepotsDifferingFromPublicPrefix);
        var inheritedPublic = ReadMarkerInt(markerPath, BranchMarkerDepotsInheritedFromPublicPrefix);
        var missingSelected = ReadMarkerInt(markerPath, BranchMarkerDepotsMissingSelectedManifestPrefix);
        var withoutPublicComparison = ReadMarkerInt(markerPath, BranchMarkerDepotsWithoutPublicComparisonPrefix);

        if (!total.HasValue)
            return "Selected branch integrity evidence is incomplete; depot manifest count is missing.";
        if (!BranchMarkerHasIntegrityProvenance(markerPath))
            return "Selected branch integrity evidence is incomplete; public-vs-selected depot comparison fields are missing. Redownload selected version to rebuild beta integrity evidence.";

        if (inheritedPublic.GetValueOrDefault() > 0 && differingPublic.GetValueOrDefault() > 0)
        {
            return $"Selected branch appears partial: {differingPublic.Value} depot(s) differ from public, {inheritedPublic.Value} depot(s) inherit public manifests.";
        }

        if (matchingPublic.GetValueOrDefault() > 0 && differingPublic.GetValueOrDefault() > 0)
        {
            return $"Selected branch appears partial: {differingPublic.Value} depot(s) differ from public, {matchingPublic.Value} depot(s) match public.";
        }

        if (inheritedPublic.GetValueOrDefault() > 0)
        {
            return $"Selected branch inherits public content for {inheritedPublic.Value} depot(s). If beta assets look public, compare file hashes before treating this as a launcher bug.";
        }

        if (missingSelected.GetValueOrDefault() > 0)
        {
            return $"Selected branch is missing explicit branch manifests for {missingSelected.Value} depot(s). See diagnostics for public-inheritance evidence.";
        }

        if (withoutPublicComparison.GetValueOrDefault() > 0)
        {
            return $"Selected branch downloaded {total.Value} depot(s), but {withoutPublicComparison.Value} depot(s) could not be compared with public.";
        }

        if (differingPublic.GetValueOrDefault() > 0)
            return $"Selected branch downloaded {differingPublic.Value} branch-specific depot(s).";

        if (matchingPublic.GetValueOrDefault() > 0)
            return $"Selected branch depot manifests all match public ({matchingPublic.Value} depot(s)).";

        return $"Selected branch downloaded {total.Value} depot manifest(s).";
    }
}
