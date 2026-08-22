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
            null,
            null,
            string.Empty,
            string.Empty,
            string.Empty,
            0,
            0,
            0,
            0,
            exists: false,
            readable: false
        );
    }
}
