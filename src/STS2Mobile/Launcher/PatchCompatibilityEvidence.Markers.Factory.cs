using System;

namespace STS2Mobile.Launcher;

internal sealed partial class PatchCompatibilityEvidence
{
    private static PatchCompatibilityEvidence MissingValidationMarker(
        string path,
        string branch,
        string source
    )
        => new(
            branch,
            source,
            path,
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

    private static PatchCompatibilityEvidence UnreadableValidationMarker(
        string path,
        string branch,
        string source,
        Exception ex
    )
        => new(
            branch,
            source,
            path,
            "unreadable",
            ex.GetType().Name,
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
            exists: true,
            readable: false,
            branchMatches: false,
            pckMatches: false,
            sourceAssemblyMatches: false
        );
}
