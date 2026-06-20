using System;
using System.IO;

namespace STS2Mobile.Launcher;

internal sealed partial class RuntimePackManifest
{
    private RuntimePackManifest WithStatus(string status)
        => new(
            Path,
            ExpectedBranch,
            PackId,
            SourceRuntimeSlotId,
            SourceBranch,
            SourcePckSha256,
            SourceAssemblySha256,
            AndroidAssemblySha256,
            PatchSetVersion,
            PatchValidationStatus,
            PatchValidationReport,
            ValidationMode,
            ValidationSurfaceVersion,
            SupportAssemblies,
            SupportAssemblySha256,
            SupportAssembliesDeclared,
            SupportAssemblySha256Declared,
            CheckedSymbolCount,
            PresentSymbolCount,
            MissingSymbolCount,
            MinimumLauncherVersion,
            GeneratedFromCleanDirectory,
            status,
            Exists,
            Readable,
            AndroidAssemblyExists,
            AndroidAssemblyPath,
            ActualAndroidAssemblySha256
        );

    private static string RuntimePackStatus(RuntimePackManifest manifest, string selectedPckSha256, string selectedSourceAssemblySha256, string selectedPckPath)
    {
        if (!manifest.Exists)
            return "not installed";
        if (!manifest.Readable)
            return manifest.Status;
        if (!manifest.BranchMatches)
            return "branch mismatch";
        if (string.IsNullOrWhiteSpace(manifest.PackId))
            return "missing runtime pack ID";
        if (!manifest.GeneratedFromCleanDirectory)
            return "runtime pack was not generated from a clean directory";
        if (string.IsNullOrWhiteSpace(manifest.SourceRuntimeSlotId))
            return "missing source runtime slot ID";
        if (!manifest.AndroidAssemblyExists)
            return "missing Android sts2.dll";
        if (string.IsNullOrWhiteSpace(manifest.AndroidAssemblySha256))
            return "missing Android assembly hash";
        if (!manifest.AndroidAssemblyHashMatches)
            return "Android sts2.dll hash mismatch";
        if (!manifest.SupportAssembliesDeclared)
            return "missing runtime pack support assembly declaration";
        if (!manifest.SupportAssemblySha256Declared)
            return "missing runtime pack support assembly hashes";
        var supportAssemblyProblem = RuntimePackSupportAssemblyProblem(manifest);
        if (!string.IsNullOrWhiteSpace(supportAssemblyProblem))
            return supportAssemblyProblem;
        if (string.IsNullOrWhiteSpace(manifest.SourcePckSha256))
            return "missing source PCK hash";
        if (!SourcePckMatches(manifest.SourcePckSha256, selectedPckSha256, selectedPckPath))
            return "PCK hash mismatch";
        if (string.IsNullOrWhiteSpace(manifest.SourceAssemblySha256))
            return "missing source assembly hash";
        if (!MatchesDeclared(manifest.SourceAssemblySha256, selectedSourceAssemblySha256))
            return "source assembly hash mismatch";
        if (string.IsNullOrWhiteSpace(manifest.PatchValidationStatus))
            return "missing patch validation status";
        if (!manifest.PatchValidationPassed)
            return $"patch validation not passed: {manifest.PatchValidationStatus}";
        if (string.IsNullOrWhiteSpace(manifest.PatchValidationReport))
            return "missing patch validation report";
        var reportPath = System.IO.Path.Combine(manifest.DirectoryPath, manifest.PatchValidationReport);
        if (!File.Exists(reportPath))
            return "missing patch validation report file";
        if (!PatchValidationReportMatches(reportPath, manifest))
            return "patch validation report mismatch";
        return "usable";
    }
}
