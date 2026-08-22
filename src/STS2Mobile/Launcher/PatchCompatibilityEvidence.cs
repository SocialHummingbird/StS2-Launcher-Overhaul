namespace STS2Mobile.Launcher;

internal sealed partial class PatchCompatibilityEvidence
{
    private PatchCompatibilityEvidence(
        string branch,
        string source,
        string markerPath,
        string status,
        string detail,
        GameIdentity expectedGameIdentity,
        GameIdentity validatedGameIdentity,
        string patchSetVersion,
        string validationMode,
        string validationSurfaceVersion,
        int requiredSymbolCount,
        int checkedSymbolCount,
        int presentSymbolCount,
        int missingSymbolCount,
        bool exists,
        bool readable
    )
    {
        Branch = branch;
        Source = source;
        MarkerPath = markerPath;
        Status = status;
        Detail = detail;
        ExpectedGameIdentity = expectedGameIdentity;
        ValidatedGameIdentity = validatedGameIdentity;
        PatchSetVersion = patchSetVersion;
        ValidationMode = validationMode;
        ValidationSurfaceVersion = validationSurfaceVersion;
        RequiredSymbolCount = requiredSymbolCount;
        CheckedSymbolCount = checkedSymbolCount;
        PresentSymbolCount = presentSymbolCount;
        MissingSymbolCount = missingSymbolCount;
        Exists = exists;
        Readable = readable;
    }

    internal string Branch { get; }
    internal string Source { get; }
    internal string MarkerPath { get; }
    internal string Status { get; }
    internal string Detail { get; }
    internal GameIdentity ExpectedGameIdentity { get; }
    internal GameIdentity ValidatedGameIdentity { get; }
    internal string PatchSetVersion { get; }
    internal string ValidationMode { get; }
    internal string ValidationSurfaceVersion { get; }
    internal int RequiredSymbolCount { get; }
    internal int CheckedSymbolCount { get; }
    internal int PresentSymbolCount { get; }
    internal int MissingSymbolCount { get; }
    internal bool Exists { get; }
    internal bool Readable { get; }
    internal bool GameIdentityMatches =>
        ExpectedGameIdentity != null && ExpectedGameIdentity == ValidatedGameIdentity;
}
