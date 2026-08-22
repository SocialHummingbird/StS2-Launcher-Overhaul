using System.IO;

namespace STS2Mobile.Launcher;

internal sealed partial class PatchCompatibilityEvidence
{
    private const string RuntimePackReportFileName = "patch_validation.json";

    internal static PatchCompatibilityEvidence Inspect(
        GameIdentity gameIdentity,
        RuntimePackManifest runtimePack
    )
    {
        if (gameIdentity == null)
            return Missing(string.Empty, string.Empty, "current game identity");

        var reportPath = string.IsNullOrWhiteSpace(runtimePack?.DirectoryPath)
            ? string.Empty
            : Path.Combine(runtimePack.DirectoryPath, RuntimePackReportFileName);
        if (runtimePack == null || !runtimePack.Exists)
            return Missing(gameIdentity.Branch, reportPath, "runtime pack validation report");

        var declared = runtimePack.SourceGameIdentity;
        return new PatchCompatibilityEvidence(
            gameIdentity.Branch,
            "runtime pack validation report",
            reportPath,
            runtimePack.Usable ? runtimePack.PatchValidationStatus : runtimePack.Status,
            runtimePack.Status,
            gameIdentity,
            declared,
            runtimePack.PatchSetVersion,
            runtimePack.ValidationMode,
            runtimePack.ValidationSurfaceVersion,
            runtimePack.CheckedSymbolCount,
            runtimePack.CheckedSymbolCount,
            runtimePack.PresentSymbolCount,
            runtimePack.MissingSymbolCount,
            exists: runtimePack.Exists,
            readable: runtimePack.Readable
        );
    }
}
