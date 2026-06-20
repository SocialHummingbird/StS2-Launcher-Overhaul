using System;
using System.IO;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class PatchCompatibilityEvidence
{
    private const string RuntimePackReportFileName = "patch_validation.json";

    internal static PatchCompatibilityEvidence Inspect(
        string dataDir,
        string branch,
        string gameDirectory,
        string selectedPckSha256,
        string selectedSourceAssemblySha256,
        RuntimePackManifest runtimePack,
        bool runtimePackSlotIdMatches = true
    )
    {
        branch = SteamGameBranch.Normalize(branch);
        if (string.Equals(branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase))
        {
            return new PatchCompatibilityEvidence(
                branch,
                "legacy public APK baseline",
                string.Empty,
                PassedStatus,
                "public branch uses the APK-bundled patch compatibility baseline",
                branch,
                selectedPckSha256,
                selectedSourceAssemblySha256,
                runtimePack?.PatchSetVersion ?? string.Empty,
                "legacy-public-baseline",
                "legacy-public-baseline",
                0,
                0,
                0,
                0,
                required: false,
                exists: true,
                readable: true,
                branchMatches: true,
                pckMatches: true,
                sourceAssemblyMatches: true
            );
        }

        if (runtimePack?.Usable == true && runtimePack.PatchValidationPassed && runtimePackSlotIdMatches)
        {
            return new PatchCompatibilityEvidence(
                branch,
                "runtime pack manifest",
                runtimePack.Path,
                runtimePack.PatchValidationStatus,
                "runtime pack declares passed Android patch compatibility validation",
                runtimePack.SourceBranch,
                runtimePack.SourcePckSha256,
                runtimePack.SourceAssemblySha256,
                runtimePack.PatchSetVersion,
                runtimePack.ValidationMode,
                runtimePack.ValidationSurfaceVersion,
                runtimePack.CheckedSymbolCount,
                runtimePack.CheckedSymbolCount,
                runtimePack.PresentSymbolCount,
                runtimePack.MissingSymbolCount,
                required: true,
                exists: runtimePack.Exists,
                readable: runtimePack.Readable,
                branchMatches: runtimePack.BranchMatches,
                pckMatches: MatchesDeclared(runtimePack.SourcePckSha256, selectedPckSha256),
                sourceAssemblyMatches: MatchesDeclared(runtimePack.SourceAssemblySha256, selectedSourceAssemblySha256)
            );
        }

        var runtimePackReportPath = string.IsNullOrWhiteSpace(runtimePack?.DirectoryPath) || !runtimePackSlotIdMatches
            ? string.Empty
            : Path.Combine(runtimePack.DirectoryPath, RuntimePackReportFileName);
        var runtimePackReport = ReadValidationMarker(
            runtimePackReportPath,
            branch,
            selectedPckSha256,
            selectedSourceAssemblySha256,
            "runtime pack validation report"
        );
        if (runtimePackReport.Exists)
            return runtimePackReport;

        return ReadValidationMarker(
            Path.Combine(gameDirectory ?? string.Empty, GameDirectoryMarkerFileName),
            branch,
            selectedPckSha256,
            selectedSourceAssemblySha256,
            "selected game directory validation marker"
        );
    }
}
