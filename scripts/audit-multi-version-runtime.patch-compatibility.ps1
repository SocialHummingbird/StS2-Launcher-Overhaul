function Add-MultiVersionRuntimePatchCompatibilityChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\PatchCompatibilityValidator.cs" `
        "defines static patch compatibility validation identity" `
        @(
            "internal static partial class PatchCompatibilityValidator",
            "PatchSetVersion",
            "ValidationMode",
            "ValidationSurfaceVersion",
            "static-critical-symbol-scan"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\PatchCompatibilityValidator.Symbols.cs" `
        "declares critical startup/save/model/platform symbols" `
        @(
            "RequiredCriticalSymbols",
            "SymbolCheck",
            "GameStartupWrapper",
            "SaveManager",
            "ModelDb",
            "PlatformUtil"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\PatchCompatibilityValidator.Scan.cs" `
        "scans selected source assembly for critical symbols" `
        @(
            "CheckSymbols",
            "File\.ReadAllBytes",
            "ContainsAscii",
            "RequiredCriticalSymbols",
            "source sts2\.dll could not be read"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\PatchCompatibilityValidator.Marker.cs" `
        "writes patch compatibility marker payload with symbol summaries" `
        @(
            "WriteMarker",
            "categorySummaries",
            "checkedSymbolCount",
            "presentSymbolCount",
            "ValidationSurfaceVersion",
            "PatchSetVersion"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\PatchCompatibilityValidator.Validation.cs" `
        "creates or deletes runtime packs after non-public selected-version validation" `
        @(
            "ValidateSelectedVersion",
            "RuntimePackWriter\.WriteValidatedRuntimePack",
            "RuntimePackWriter\.DeleteRuntimePack",
            "RuntimePackSlotIdMatches",
            "CheckSymbols",
            "WriteMarker",
            "BranchMarkerReady"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\PatchCompatibilityEvidence.cs" `
        "models patch validation evidence identity, hashes, symbol counts, and match state" `
        @(
            "GameDirectoryMarkerFileName",
            "internal sealed partial class PatchCompatibilityEvidence",
            "PatchCompatibilityEvidence\(",
            "ValidatedPckSha256",
            "ValidatedSourceAssemblySha256",
            "CheckedSymbolCount",
            "MissingSymbolCount",
            "PckMatches",
            "SourceAssemblyMatches"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\PatchCompatibilityEvidence.Status.cs" `
        "requires branch, PCK, and source assembly matches before patch evidence can pass" `
        @(
            "PassedStatus",
            "Passed",
            "StringComparison\.OrdinalIgnoreCase",
            "BranchMatches",
            "PckMatches",
            "SourceAssemblyMatches"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\PatchCompatibilityEvidence.Problem.cs" `
        "formats patch validation blockers for missing, unreadable, mismatched, and failed evidence" `
        @(
            "Problem",
            "Selected game version has no Android patch compatibility validation evidence",
            "unreadable Android patch validation evidence",
            "different Steam branch",
            "does not declare the validated PCK",
            "different PCK",
            "does not declare the validated game-code assembly",
            "different game-code assembly",
            "failed Android patch compatibility validation"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\PatchCompatibilityEvidence.Factory.cs" `
        "creates normalized missing patch validation evidence states" `
        @(
            "Missing",
            "SteamGameBranch\.Normalize",
            "validation evidence not found",
            "required: true",
            "exists: false",
            "readable: false",
            "branchMatches: false",
            "pckMatches: false",
            "sourceAssemblyMatches: false"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\PatchCompatibilityEvidence.Inspect.cs" `
        "selects patch validation evidence from runtime pack or game directory sources" `
        @(
            "patch_validation\.json",
            "PatchValidationPassed",
            'runtimePack\?\.Usable == true',
            "ValidationMode",
            "ValidationSurfaceVersion",
            "CheckedSymbolCount",
            "MissingSymbolCount",
            "runtime pack validation report",
            "selected game directory validation marker",
            "RuntimePackReportFileName",
            "ReadValidationMarker"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\PatchCompatibilityEvidence.Markers.cs" `
        "dispatches patch validation marker reads and unreadable/missing marker states" `
        @(
            "ReadValidationMarker",
            "MissingValidationMarker",
            "JsonDocument\.Parse",
            "ReadValidationDocument",
            "UnreadableValidationMarker"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\PatchCompatibilityEvidence.Markers.Document.cs" `
        "parses patch validation marker identity and symbol-count evidence" `
        @(
            "ReadValidationDocument",
            "patchValidationStatus",
            "sourcePckSha256",
            "sourceAssemblySha256",
            "checkedSymbolCount",
            "missingSymbolCount",
            "ValidationMode",
            "ValidationSurfaceVersion",
            "MatchesBranch",
            "MatchesDeclared"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\PatchCompatibilityEvidence.Markers.Factory.cs" `
        "creates missing and unreadable patch validation marker evidence states" `
        @(
            "MissingValidationMarker",
            "validation evidence not found",
            "exists: false",
            "readable: false",
            "UnreadableValidationMarker",
            "unreadable",
            "ex\.GetType\(\)\.Name",
            "exists: true"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\PatchCompatibilityEvidence.Markers.Match.cs" `
        "matches patch validation marker branch, PCK, and source assembly identity" `
        @(
            "MatchesBranch",
            "SteamGameBranch\.Normalize",
            "StringComparison\.OrdinalIgnoreCase",
            "MatchesDeclared",
            "actualValue\.StartsWith",
            "<",
            "string\.Equals\(declaredValue, actualValue"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\PatchCompatibilityEvidence.Markers.Json.cs" `
        "reads string and integer values from patch validation JSON markers" `
        @(
            "ReadString",
            "JsonValueKind\.String",
            "value\.GetString\(\)",
            "ReadInt",
            "JsonValueKind\.Number",
            "value\.TryGetInt32",
            "int\.TryParse"
        )
}
