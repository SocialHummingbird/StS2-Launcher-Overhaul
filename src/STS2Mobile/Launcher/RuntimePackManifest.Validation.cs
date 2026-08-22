using System;
using System.IO;

namespace STS2Mobile.Launcher;

internal sealed partial class RuntimePackManifest
{
    private RuntimePackManifest WithStatus(string status)
        => new(
            Path,
            ExpectedBranch,
            ExpectedGameIdentity,
            PackId,
            SourceBranch,
            InstallGeneration,
            SourcePckSha256,
            SourceAssemblySha256,
            GameIdentityId,
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

    private static string RuntimePackStatus(RuntimePackManifest manifest)
    {
        if (!manifest.Exists)
            return "not installed";
        if (!manifest.Readable)
            return manifest.Status;
        if (manifest.SourceGameIdentity == null)
            return "missing or invalid source game identity";
        if (manifest.ExpectedGameIdentity == null)
            return "current game identity unavailable";
        if (!string.Equals(manifest.GameIdentityId, manifest.SourceGameIdentity.Id, StringComparison.OrdinalIgnoreCase))
            return "game identity ID mismatch";
        if (manifest.SourceGameIdentity != manifest.ExpectedGameIdentity)
            return "game identity mismatch";
        if (string.IsNullOrWhiteSpace(manifest.PackId))
            return "missing runtime pack ID";
        if (!string.Equals(manifest.PatchSetVersion, PatchCompatibilityValidator.PatchSetVersion, StringComparison.OrdinalIgnoreCase))
            return $"runtime pack patch-set mismatch: {manifest.PatchSetVersion}";
        if (!manifest.GeneratedFromCleanDirectory)
            return "runtime pack was not generated from a clean directory";
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
