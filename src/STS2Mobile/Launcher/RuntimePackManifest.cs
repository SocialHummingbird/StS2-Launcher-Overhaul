using System;
using System.Collections.Generic;
using System.IO;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class RuntimePackManifest
{
    internal const string AndroidAssemblyFileName = "sts2.dll";
    private const string AndroidPckPatchMarkerFileName = ".android_pck_patch_v29";

    private RuntimePackManifest(
        string path,
        string expectedBranch,
        string packId,
        string sourceRuntimeSlotId,
        string sourceBranch,
        string sourcePckSha256,
        string sourceAssemblySha256,
        string androidAssemblySha256,
        string patchSetVersion,
        string patchValidationStatus,
        string patchValidationReport,
        string validationMode,
        string validationSurfaceVersion,
        string[] supportAssemblies,
        IReadOnlyDictionary<string, string> supportAssemblySha256,
        bool supportAssembliesDeclared,
        bool supportAssemblySha256Declared,
        int checkedSymbolCount,
        int presentSymbolCount,
        int missingSymbolCount,
        string minimumLauncherVersion,
        bool generatedFromCleanDirectory,
        string status,
        bool exists,
        bool readable,
        bool androidAssemblyExists,
        string androidAssemblyPath,
        string actualAndroidAssemblySha256
    )
    {
        Path = path;
        DirectoryPath = System.IO.Path.GetDirectoryName(path) ?? string.Empty;
        ExpectedBranch = expectedBranch;
        PackId = packId;
        SourceRuntimeSlotId = sourceRuntimeSlotId;
        SourceBranch = sourceBranch;
        SourcePckSha256 = sourcePckSha256;
        SourceAssemblySha256 = sourceAssemblySha256;
        AndroidAssemblySha256 = androidAssemblySha256;
        PatchSetVersion = patchSetVersion;
        PatchValidationStatus = patchValidationStatus;
        PatchValidationReport = patchValidationReport;
        ValidationMode = validationMode;
        ValidationSurfaceVersion = validationSurfaceVersion;
        SupportAssemblies = supportAssemblies ?? Array.Empty<string>();
        SupportAssemblySha256 = supportAssemblySha256 ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        SupportAssembliesDeclared = supportAssembliesDeclared;
        SupportAssemblySha256Declared = supportAssemblySha256Declared;
        CheckedSymbolCount = checkedSymbolCount;
        PresentSymbolCount = presentSymbolCount;
        MissingSymbolCount = missingSymbolCount;
        MinimumLauncherVersion = minimumLauncherVersion;
        GeneratedFromCleanDirectory = generatedFromCleanDirectory;
        Status = status;
        Exists = exists;
        Readable = readable;
        AndroidAssemblyExists = androidAssemblyExists;
        AndroidAssemblyPath = androidAssemblyPath;
        ActualAndroidAssemblySha256 = actualAndroidAssemblySha256;
    }

    internal string Path { get; }
    internal string DirectoryPath { get; }
    internal string ExpectedBranch { get; }
    internal string PackId { get; }
    internal string SourceRuntimeSlotId { get; }
    internal string SourceBranch { get; }
    internal string SourcePckSha256 { get; }
    internal string SourceAssemblySha256 { get; }
    internal string AndroidAssemblySha256 { get; }
    internal string PatchSetVersion { get; }
    internal string PatchValidationStatus { get; }
    internal string PatchValidationReport { get; }
    internal string ValidationMode { get; }
    internal string ValidationSurfaceVersion { get; }
    internal string[] SupportAssemblies { get; }
    internal IReadOnlyDictionary<string, string> SupportAssemblySha256 { get; }
    internal bool SupportAssembliesDeclared { get; }
    internal bool SupportAssemblySha256Declared { get; }
    internal int CheckedSymbolCount { get; }
    internal int PresentSymbolCount { get; }
    internal int MissingSymbolCount { get; }
    internal string MinimumLauncherVersion { get; }
    internal bool GeneratedFromCleanDirectory { get; }
    internal string Status { get; }
    internal bool Exists { get; }
    internal bool Readable { get; }
    internal bool AndroidAssemblyExists { get; }
    internal string AndroidAssemblyPath { get; }
    internal string ActualAndroidAssemblySha256 { get; }

    internal bool BranchMatches =>
        !string.IsNullOrWhiteSpace(SourceBranch)
        && string.Equals(
            SteamGameBranch.Normalize(SourceBranch),
            SteamGameBranch.Normalize(ExpectedBranch),
            StringComparison.OrdinalIgnoreCase
        );

    internal bool AndroidAssemblyHashMatches =>
        !string.IsNullOrWhiteSpace(AndroidAssemblySha256)
        && !string.IsNullOrWhiteSpace(ActualAndroidAssemblySha256)
        && !ActualAndroidAssemblySha256.StartsWith("<", StringComparison.Ordinal)
        && string.Equals(AndroidAssemblySha256, ActualAndroidAssemblySha256, StringComparison.OrdinalIgnoreCase);

    internal bool Usable =>
        string.Equals(Status, "usable", StringComparison.OrdinalIgnoreCase);

    internal bool PatchValidationPassed =>
        string.Equals(PatchValidationStatus, "passed", StringComparison.OrdinalIgnoreCase);

    internal bool SourcePckMatchesSelectedPck(string selectedPckSha256, string selectedPckPath)
        => SourcePckMatches(SourcePckSha256, selectedPckSha256, selectedPckPath);
}
