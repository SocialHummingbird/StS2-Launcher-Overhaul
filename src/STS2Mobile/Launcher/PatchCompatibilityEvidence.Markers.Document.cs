using System;
using System.Text.Json;

namespace STS2Mobile.Launcher;

internal sealed partial class PatchCompatibilityEvidence
{
    private static PatchCompatibilityEvidence ReadValidationDocument(
        JsonElement root,
        string path,
        string branch,
        string selectedPckSha256,
        string selectedSourceAssemblySha256,
        string source
    )
    {
        var status = ReadString(root, "status", "result", "patchValidationStatus", "patch_validation_status");
        var validatedBranch = ReadString(root, "branch", "sourceBranch", "source_branch", "validatedBranch", "validated_branch");
        var validatedPckSha256 = ReadString(root, "pckSha256", "pck_sha256", "sourcePckSha256", "source_pck_sha256");
        var validatedSourceAssemblySha256 = ReadString(root, "sourceAssemblySha256", "source_assembly_sha256", "desktopAssemblySha256", "desktop_assembly_sha256");
        var patchSetVersion = ReadString(root, "patchSetVersion", "patch_set_version", "patchVersion", "patch_version");
        var validationMode = ReadString(root, "validationMode", "validation_mode");
        var validationSurfaceVersion = ReadString(root, "validationSurfaceVersion", "validation_surface_version");
        return new PatchCompatibilityEvidence(
            branch,
            source,
            path,
            string.IsNullOrWhiteSpace(status) ? "unknown" : status,
            ReadString(root, "detail", "message", "summary"),
            validatedBranch,
            validatedPckSha256,
            validatedSourceAssemblySha256,
            patchSetVersion,
            validationMode,
            validationSurfaceVersion,
            ReadInt(root, "requiredSymbolCount", "required_symbol_count"),
            ReadInt(root, "checkedSymbolCount", "checked_symbol_count"),
            ReadInt(root, "presentSymbolCount", "present_symbol_count"),
            ReadInt(root, "missingSymbolCount", "missing_symbol_count"),
            required: true,
            exists: true,
            readable: true,
            branchMatches: MatchesBranch(validatedBranch, branch),
            pckMatches: MatchesDeclared(validatedPckSha256, selectedPckSha256),
            sourceAssemblyMatches: MatchesDeclared(validatedSourceAssemblySha256, selectedSourceAssemblySha256)
        );
    }
}
