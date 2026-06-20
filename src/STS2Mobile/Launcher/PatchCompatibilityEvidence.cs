namespace STS2Mobile.Launcher;

internal sealed partial class PatchCompatibilityEvidence
{
    internal const string GameDirectoryMarkerFileName = ".android_patch_validation.json";

    private PatchCompatibilityEvidence(
        string branch,
        string source,
        string markerPath,
        string status,
        string detail,
        string validatedBranch,
        string validatedPckSha256,
        string validatedSourceAssemblySha256,
        string patchSetVersion,
        string validationMode,
        string validationSurfaceVersion,
        int requiredSymbolCount,
        int checkedSymbolCount,
        int presentSymbolCount,
        int missingSymbolCount,
        bool required,
        bool exists,
        bool readable,
        bool branchMatches,
        bool pckMatches,
        bool sourceAssemblyMatches
    )
    {
        Branch = branch;
        Source = source;
        MarkerPath = markerPath;
        Status = status;
        Detail = detail;
        ValidatedBranch = validatedBranch;
        ValidatedPckSha256 = validatedPckSha256;
        ValidatedSourceAssemblySha256 = validatedSourceAssemblySha256;
        PatchSetVersion = patchSetVersion;
        ValidationMode = validationMode;
        ValidationSurfaceVersion = validationSurfaceVersion;
        RequiredSymbolCount = requiredSymbolCount;
        CheckedSymbolCount = checkedSymbolCount;
        PresentSymbolCount = presentSymbolCount;
        MissingSymbolCount = missingSymbolCount;
        Required = required;
        Exists = exists;
        Readable = readable;
        BranchMatches = branchMatches;
        PckMatches = pckMatches;
        SourceAssemblyMatches = sourceAssemblyMatches;
    }

    internal string Branch { get; }
    internal string Source { get; }
    internal string MarkerPath { get; }
    internal string Status { get; }
    internal string Detail { get; }
    internal string ValidatedBranch { get; }
    internal string ValidatedPckSha256 { get; }
    internal string ValidatedSourceAssemblySha256 { get; }
    internal string PatchSetVersion { get; }
    internal string ValidationMode { get; }
    internal string ValidationSurfaceVersion { get; }
    internal int RequiredSymbolCount { get; }
    internal int CheckedSymbolCount { get; }
    internal int PresentSymbolCount { get; }
    internal int MissingSymbolCount { get; }
    internal bool Required { get; }
    internal bool Exists { get; }
    internal bool Readable { get; }
    internal bool BranchMatches { get; }
    internal bool PckMatches { get; }
    internal bool SourceAssemblyMatches { get; }
}
