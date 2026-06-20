using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class PatchCompatibilityEvidence
{
    internal static PatchCompatibilityEvidence Missing(string branch, string markerPath, string source)
    {
        branch = SteamGameBranch.Normalize(branch);
        return new PatchCompatibilityEvidence(
            branch,
            source,
            markerPath,
            "missing",
            "validation evidence not found",
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            0,
            0,
            0,
            0,
            required: true,
            exists: false,
            readable: false,
            branchMatches: false,
            pckMatches: false,
            sourceAssemblyMatches: false
        );
    }
}
