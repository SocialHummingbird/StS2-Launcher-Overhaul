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
        "creates or deletes runtime packs after every selected-version validation" `
        @(
            "ValidateSelectedVersion",
            "ValidateSelectedVersionSlot",
            "SelectedVersionSlotAlreadyValidated",
            "current runtime pack already passed validation",
            "slot\.RuntimePackUsable",
            "slot\.RuntimePack\?\.PatchValidationPassed == true",
            "slot\.PatchCompatibility\?\.Passed == true",
            "RuntimePackWriter\.WriteValidatedRuntimePack",
            "RuntimePackWriter\.DeleteRuntimePack",
            "GameRuntimeSlot\.Inspect\(dataDir, branch\)",
            "CheckSymbols",
            "WriteMarker",
            "runtimePackWritten",
            "runtime pack generation failed after patch compatibility validation",
            "failures\.Count > 0",
            "LauncherLaunchReadinessCache\.Clear",
            "patch compatibility validation updated runtime evidence",
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
            "failed Android patch compatibility validation",
            "\$""\{Status\}: \{Detail\}"""
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

    Add-PatchCompatibilityRuntimePackInvalidationOwnershipCheck
    Add-PublicRuntimePackValidationCoverageCheck
}

function Add-PublicRuntimePackValidationCoverageCheck {
    $validatorPath = "src\STS2Mobile\Launcher\PatchCompatibilityValidator.Validation.cs"
    $validator = Read-RepoFile $validatorPath
    $evidencePath = "src\STS2Mobile\Launcher\PatchCompatibilityEvidence.Inspect.cs"
    $evidence = Read-RepoFile $evidencePath
    if ($null -eq $validator -or $null -eq $evidence) {
        return
    }

    if ($validator -match "SteamGameBranch\.Public") {
        $script:StaticAuditFailures.Add(
            "$validatorPath - validates and publicizes every selected branch - public-specific bypass found"
        )
        return
    }

    $runtimePackEvidenceIndex = $evidence.IndexOf("runtimePack?.Usable == true", [StringComparison]::Ordinal)
    $gameDirectoryEvidenceIndex = $evidence.IndexOf("var gameDirectoryReport = ReadValidationMarker", [StringComparison]::Ordinal)
    $legacyPublicEvidenceIndex = $evidence.IndexOf("SteamGameBranch.Public", [StringComparison]::Ordinal)
    if (
        $runtimePackEvidenceIndex -lt 0 -or
        $gameDirectoryEvidenceIndex -lt 0 -or
        $legacyPublicEvidenceIndex -lt 0 -or
        $runtimePackEvidenceIndex -gt $legacyPublicEvidenceIndex -or
        $gameDirectoryEvidenceIndex -gt $legacyPublicEvidenceIndex
    ) {
        $script:StaticAuditFailures.Add(
            "$evidencePath - prefers runtime-pack and failed game-directory validation over legacy public fallback - evidence order is incorrect"
        )
        return
    }

    $script:StaticAuditPasses += 1
    if (-not $script:StaticAuditQuiet) {
        Write-Host "PASS $validatorPath - validates and publicizes every selected branch"
    }
}

function Add-PatchCompatibilityRuntimePackInvalidationOwnershipCheck {
    $validatorPath = "src\STS2Mobile\Launcher\PatchCompatibilityValidator.Validation.cs"
    $validator = Read-RepoFile $validatorPath
    if ($null -eq $validator) {
        return
    }

    $writeCallCount = ([regex]::Matches(
        $validator,
        "RuntimePackWriter\.WriteValidatedRuntimePack\("
    )).Count
    $deleteCallCount = ([regex]::Matches(
        $validator,
        "RuntimePackWriter\.DeleteRuntimePack\("
    )).Count
    $clearCallCount = ([regex]::Matches(
        $validator,
        "LauncherLaunchReadinessCache\.Clear\("
    )).Count

    if ($writeCallCount -ne 1 -or $deleteCallCount -ne 1 -or $clearCallCount -ne 1) {
        $script:StaticAuditFailures.Add(
            "$validatorPath - keeps runtime-pack readiness invalidation owned by selected-version validation - expected one write, one delete, and one cache clear; found write=$writeCallCount delete=$deleteCallCount clear=$clearCallCount"
        )
        return
    }

    $launcherFiles = Get-ChildItem `
        -LiteralPath (Resolve-RepoPath "src\STS2Mobile\Launcher") `
        -Filter "*.cs" `
        -File
    foreach ($file in $launcherFiles) {
        $relativePath = "src\STS2Mobile\Launcher\$($file.Name)"
        $content = Get-Content -LiteralPath $file.FullName -Raw
        if ($file.Name -eq "PatchCompatibilityValidator.Validation.cs") {
            continue
        }

        if ($content -match "RuntimePackWriter\.(WriteValidatedRuntimePack|DeleteRuntimePack)\(") {
            $script:StaticAuditFailures.Add(
                "$relativePath - keeps runtime-pack readiness invalidation owned by selected-version validation - direct RuntimePackWriter mutation call bypasses cache invalidation owner"
            )
            return
        }
    }

    $script:StaticAuditPasses += 1
    if (-not $script:StaticAuditQuiet) {
        Write-Host "PASS $validatorPath - keeps runtime-pack readiness invalidation owned by selected-version validation"
    }
}
