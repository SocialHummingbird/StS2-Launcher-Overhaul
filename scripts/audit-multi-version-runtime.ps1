param(
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$failures = New-Object System.Collections.Generic.List[string]
$passes = 0

function Resolve-RepoPath([string]$RelativePath) {
    $normalized = $RelativePath -replace '[\\/]', [System.IO.Path]::DirectorySeparatorChar
    return Join-Path $root $normalized
}

function Read-RepoFile([string]$RelativePath) {
    $path = Resolve-RepoPath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing file: $RelativePath")
        return $null
    }

    return Get-Content -LiteralPath $path -Raw
}

function Add-Check([string]$RelativePath, [string]$Description, [string[]]$RequiredPatterns) {
    $content = Read-RepoFile $RelativePath
    if ($null -eq $content) {
        return
    }

    foreach ($pattern in $RequiredPatterns) {
        if ($content -notmatch $pattern) {
            $failures.Add("$RelativePath - $Description - missing pattern: $pattern")
            return
        }
    }

    $script:passes += 1
    if (-not $Quiet) {
        Write-Host "PASS $RelativePath - $Description"
    }
}

function Add-ForbiddenCheck([string]$RelativePath, [string]$Description, [string[]]$ForbiddenPatterns) {
    $content = Read-RepoFile $RelativePath
    if ($null -eq $content) {
        return
    }

    foreach ($pattern in $ForbiddenPatterns) {
        if ($content -match $pattern) {
            $failures.Add("$RelativePath - $Description - forbidden pattern present: $pattern")
            return
        }
    }

    $script:passes += 1
    if (-not $Quiet) {
        Write-Host "PASS $RelativePath - $Description"
    }
}

Add-Check `
    "src\STS2Mobile\Launcher\GameRuntimeSlot.cs" `
    "defines a branch runtime slot with PCK/runtime/patch identity and playability gate" `
    @(
        "internal sealed class GameRuntimeSlot",
        "RuntimePackManifest",
        "PatchCompatibilityEvidence",
        "RuntimeSlotMetadata",
        "PckSha256",
        "SourceAssemblySha256",
        "ActiveAndroidAssemblySha256",
        "RuntimeSlotId",
        "RuntimeSlotIdentity",
        "RuntimePackSlotIdMatches",
        "RuntimePackUsabilityStatus",
        "BranchMatchedAndroidRuntimePrepared",
        "BranchRuntimeAvailable",
        "no-usable-runtime",
        "non-public versions require a usable runtime pack",
        "UsesLegacyPackagedPublicRuntime",
        "RequiresRuntimePackOrPreparedCache",
        "BuildRuntimeSlotId",
        "RuntimeCompatible",
        "PatchCompatible",
        "Playable",
        "RuntimePackUsable",
        "missing source runtime slot ID",
        "ReadinessProblem",
        "SteamGameInstallPaths\.GameDirectory",
        "RuntimePackManifestPath",
        "RuntimePackDirectoryPath"
    )

Add-Check `
    "src\STS2Mobile\Launcher\RuntimeSlotMetadata.cs" `
    "reads release-info and depot manifest provenance into runtime-slot identity" `
    @(
        "ReadReleaseInfo",
        "ReleaseVersion",
        "ReleaseCommit",
        "ReleaseBuildId",
        "DepotManifestCount",
        "DepotManifestFingerprint",
        "Depot manifests differing from public count:",
        "Depot manifest:",
        "IdentitySummary"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherGameFiles.cs" `
    "keeps selected-file readiness lightweight while surfacing runtime playability through readiness problems" `
    @(
        "IsValidPck\(PckPath\(dataDir, branch\)\)",
        "BranchMarkerReady\(dataDir, branch\)",
        "SourceAssemblyExists\(GameDirectoryPath\(dataDir, branch\)\)",
        "GameRuntimeSlot\.Inspect\(dataDir, branch\)\.ReadinessProblem\(\)",
        "ReadinessProblem\(string dataDir, string branch\)",
        "DeleteDirectory\(runtimePackDirectory\)",
        "LauncherRuntimeSlotEvidence\.Clear\(dataDir\)",
        "LauncherRuntimeCacheEvidence\.Clear\(dataDir\)",
        "LauncherRuntimePatchValidationEvidence\.Clear\(dataDir\)",
        "Removed runtime pack count",
        "Runtime packs directory present",
        "Selected runtime pack present before cleanup",
        "Preserved selected runtime pack",
        "CacheCleanupMarkerSelectedRuntimePackPreservedWhereApplicable",
        "RuntimePackDirectoryPathForStateDirectory",
        "DeleteInactiveRuntimePacks",
        "Removed orphan runtime pack"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.Session.Actions.cs" `
    "shows selected-runtime readiness blockers instead of generic download-required status after login" `
    @(
        "LauncherGameFiles\.ReadinessProblem\(_model\.DataDir, branch\)",
        "SelectedVersionDownloadRequiredStatus"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.Startup.cs" `
    "blocks normal and safe launch actions when selected runtime is not playable" `
    @(
        "LaunchPressed",
        "SafeLaunchPressed",
        "LauncherGameFiles\.Ready\(_model\.DataDir, branch\)",
        "LauncherGameFiles\.ReadinessProblem\(_model\.DataDir, branch\)",
        "Selected game version is not ready to safe launch"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherModel.Launch.cs" `
    "prevents in-process or restart launch paths from bypassing selected-runtime readiness" `
    @(
        "SelectedGameVersionReadyForLaunch",
        "LauncherGameFiles\.Ready\(_dataDir\)",
        "LauncherGameFiles\.ReadinessProblem\(_dataDir, branch\)",
        "Launch blocked"
    )

Add-Check `
    "src\STS2Mobile\Launcher\RuntimePackManifest.cs" `
    "parses runtime-pack compatibility metadata and hash evidence" `
    @(
        "AndroidAssemblySha256",
        "SourceRuntimeSlotId",
        "PatchValidationStatus",
        "PatchValidationReport",
        "ValidationMode",
        "ValidationSurfaceVersion",
        "GeneratedFromCleanDirectory",
        "generatedFromCleanDirectory",
        "runtime pack was not generated from a clean directory",
        "SupportAssembliesDeclared",
        "SupportAssemblySha256Declared",
        "missing runtime pack support assembly declaration",
        "missing runtime pack support assembly hashes",
        "runtime pack contains undeclared DLL",
        "runtime pack support assembly hash missing",
        "CheckedSymbolCount",
        "PresentSymbolCount",
        "MissingSymbolCount",
        "PatchValidationPassed",
        "BranchMatches",
        "AndroidAssemblyHashMatches",
        "missing Android assembly hash",
        "missing patch validation status",
        "patch validation not passed",
        "missing patch validation report",
        "missing patch validation report file",
        "PatchValidationReportMatches",
        "StringPropertyMatches",
        "BoolPropertyMatches",
        "patch validation report mismatch",
        "RuntimePackStatus",
        "sourcePckSha256",
        "sourceAssemblySha256",
        "missing runtime pack ID",
        "missing source runtime slot ID",
        "missing source PCK hash",
        "missing source assembly hash",
        "MatchesDeclared"
    )

Add-Check `
    "src\STS2Mobile\Launcher\RuntimePackWriter.cs" `
    "generates a branch-local runtime pack after selected-version validation passes" `
    @(
        "WriteValidatedRuntimePack",
        "Directory\.Delete\(packDirectory, recursive: true\)",
        "DeleteRuntimePack",
        "compatibility\.json",
        "patch_validation\.json",
        "sts2\.dll",
        "sourceRuntimeSlotId",
        "sourceRuntimeSlotIdentity",
        "sourceBranch",
        "releaseVersion",
        "releaseCommit",
        "releaseBuildId",
        "depotManifestFingerprint",
        "sourcePckSha256",
        "sourceAssemblySha256",
        "androidAssemblySha256",
        "patchValidationStatus\s*=\s*""passed""",
        "patchSetVersion",
        "validationSurfaceVersion",
        "checkedSymbolCount",
        "categorySummaries",
        "generatedFromCleanDirectory",
        "RuntimeSupportAssemblyFileNames",
        "CopyRuntimeSupportAssemblies",
        "supportAssemblies",
        "supportAssemblySha256"
    )

Add-Check `
    "src\STS2Mobile\Launcher\PatchCompatibilityValidator.cs" `
    "writes static patch compatibility evidence and creates runtime packs for non-public versions" `
    @(
        "ValidateSelectedVersion",
        "RequiredCriticalSymbols",
        "ValidationSurfaceVersion",
        "SymbolCheck",
        "categorySummaries",
        "checkedSymbolCount",
        "presentSymbolCount",
        "GameStartupWrapper",
        "SaveManager",
        "ModelDb",
        "PlatformUtil",
        "WriteMarker",
        "RuntimePackWriter\.WriteValidatedRuntimePack",
        "RuntimePackWriter\.DeleteRuntimePack",
        "RuntimePackSlotIdMatches",
        "static-critical-symbol-scan"
    )

Add-Check `
    "src\STS2Mobile\Launcher\PatchCompatibilityEvidence.cs" `
    "blocks non-public playability unless patch validation evidence matches selected runtime identity" `
    @(
        "GameDirectoryMarkerFileName",
        "patch_validation\.json",
        "PatchValidationPassed",
        'runtimePack\?\.Usable == true',
        "ValidationMode",
        "ValidationSurfaceVersion",
        "CheckedSymbolCount",
        "MissingSymbolCount",
        "PckMatches",
        "SourceAssemblyMatches",
        "MatchesDeclared",
        "does not declare the validated PCK",
        "does not declare the validated game-code assembly",
        "Problem",
        "Selected game version has no Android patch compatibility validation evidence"
    )

Add-Check `
    "android\src\com\game\sts2launcher\GodotApp.java" `
    "uses branch/runtime-pack-aware assembly cache identity and records prepared runtime-cache evidence" `
    @(
        "KEY_ASSEMBLY_CACHE_RUNTIME_ID",
        "CURRENT_RUNTIME_CACHE_MARKER",
        "BRANCH_GAME_CODE_ASSEMBLIES",
        "RUNTIME_PACKS_DIRECTORY",
        "RUNTIME_PACK_COMPATIBILITY_MANIFEST",
        "RUNTIME_PACK_PATCH_VALIDATION_REPORT",
        "CURRENT_RUNTIME_SLOT_MARKER",
        "findRuntimePackDir",
        "isRuntimePackManifestUsable",
        "runtimePackSupportAssembliesUsable",
        "isRuntimeSlotEvidenceReadyForLaunch",
        "Blocking selected game startup because runtime slot evidence is missing",
        "Blocking selected game startup because runtime slot evidence is not playable",
        "Runtime slot evidence ready for startup",
        "pckMatches",
        "sourceAssemblyMatches",
        "packId",
        "sourceRuntimeSlotId",
        "sourceBranch",
        "sourcePckSha256",
        "sourceAssemblySha256",
        "androidAssemblySha256",
        "generatedFromCleanDirectory",
        "patchValidationStatus",
        "Runtime pack was not generated from a clean directory",
        "Runtime pack support assembly hash set does not match declared support assemblies",
        "Runtime pack contains undeclared DLL",
        "Runtime pack patch validation report did not pass",
        "Runtime pack patch validation report does not match compatibility manifest",
        "Runtime pack branch mismatch",
        "Runtime pack selected PCK hash mismatch",
        "Runtime pack selected source assembly hash mismatch",
        "Runtime pack cannot be matched because selected PCK is missing",
        "Runtime pack cannot be matched because selected source sts2\.dll is missing",
        "Selected non-public branch requires a usable runtime pack",
        "Skipping selected-game branch code assembly without usable runtime pack",
        "no-usable-runtime",
        "Selected branch requires runtime pack:",
        "Game assembly cache is not current because selected non-public branch has no usable runtime pack",
        "runtimePackGameAssembly",
        "runtimeSource=",
        "runtimePackIdentity",
        "runtimePackDeclaredAssemblyNames",
        "manifest-declared runtime-pack assemblies",
        "appendRuntimePackFileIdentity",
        "compatibility\.json",
        "patch_validation\.json",
        "currentRuntimeCacheId",
        "writeRuntimeCacheMarker",
        "Runtime ID:",
        "Publish cache active sts2\.dll SHA256:",
        "Assembly cache runtime changed",
        "Copied .* runtime-pack assembly files",
        "shouldCopyGameAssemblyFile"
    )

Add-Check `
    "android\src\com\game\sts2launcher\NativeFallbackActivity.java" `
    "surfaces runtime-slot evidence in native fallback diagnostics" `
    @(
        "CURRENT_RUNTIME_SLOT_MARKER",
        "appendRuntimeSlotState",
        "Runtime slot evidence exists",
        "Runtime slot files ready",
        "Runtime slot playable",
        "Runtime slot runtime compatible",
        "Runtime slot patch compatible",
        "Runtime slot PCK hash matches selected file",
        "Runtime slot source sts2\.dll hash matches selected file",
        "Runtime slot readiness problem",
        "Runtime slot runtime pack usability",
        "Runtime slot patch compatibility status"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherRuntimeSlotEvidence.cs" `
    "records persistent selected runtime-slot evidence after download validation" `
    @(
        "current_runtime_slot\.json",
        "runtimeSlotId",
        "BranchMatchesSelectedRuntime",
        "RuntimeSlotIdMatchesSelectedRuntime",
        "PckMatchesSelectedRuntime",
        "SourceAssemblyMatchesSelectedRuntime",
        "Clear",
        "requiresUsableRuntimePack",
        "filesReady",
        "readinessProblem",
        "runtimePackUsabilityStatus",
        "patchCompatibilityStatus"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherRuntimeCacheEvidence.cs" `
    "reads native prepared-runtime cache marker and compares it to the selected runtime slot" `
    @(
        "current_runtime_cache\.txt",
        "RuntimeId",
        "RuntimeSource",
        "SelectedBranchRequiresRuntimePack",
        "SelectedPckSha256",
        "SelectedSourceAssemblySha256",
        "PublishCacheActiveAssemblySha256",
        "PckMatchesSelectedRuntime",
        "SourceAssemblyMatchesSelectedRuntime",
        "PublishCacheMatchesSelectedRuntime",
        "RequiresRuntimePackOrPreparedCache",
        "CachePreparedForSelectedRuntime",
        "Clear"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherRuntimePatchValidationEvidence.cs" `
    "records actual runtime Harmony patch results for the selected runtime slot" `
    @(
        "last_runtime_patch_validation\.json",
        "StartupPatchOrchestrator\.StartupPatchResult",
        "critical_failed",
        "passed_with_noncritical_failures",
        "selectedPckSha256",
        "selectedSourceAssemblySha256",
        "activeAndroidAssemblySha256",
        "runtimePackId",
        "runtimeSlotId",
        "failureMessages",
        "Clear"
    )

Add-Check `
    "src\STS2Mobile\ModEntry.cs" `
    "writes runtime patch validation evidence immediately after startup patch orchestration" `
    @(
        "StartupPatchOrchestrator\.Apply\(harmony\)",
        "LauncherRuntimePatchValidationEvidence\.Write\(OS\.GetDataDir\(\), patchResult\)",
        "Critical startup patches failed"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherSaveOriginEvidence.cs" `
    "tracks selected-runtime save origin and invalidates saves on branch switch" `
    @(
        "current_android_save_origin\.txt",
        "WriteManualPullOrigin",
        "WriteBranchSwitchPendingOrigin",
        "CurrentLocalSavesMatchSelectedBranch",
        "CurrentLocalSavesMatchSelectedRuntime",
        "RuntimeSlotIdMatchesSelectedRuntime",
        "PckMatchesSelectedRuntime",
        "SourceAssemblyMatchesSelectedRuntime",
        "SelectedRuntimeCurrentlyPlayable",
        "slot\.Playable",
        "Selected runtime slot ID",
        "Selected PCK SHA256",
        "Selected source sts2\.dll SHA256",
        "Selected runtime playable",
        "Selected runtime readiness problem",
        "Current Android local saves verified for selected branch: false",
        "Current Android local saves verified for selected runtime"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.CloudSync.cs" `
    "blocks Push when save-origin evidence is missing or belongs to another selected runtime" `
    @(
        "CanPushWithBaselineEvidence",
        "CanPushAfterBranchSwitch",
        "LauncherSaveOriginEvidence\.CurrentLocalSavesMatchSelectedRuntime",
        "Manual Push blocked: Android local save origin evidence does not match the selected runtime",
        "Manual Push blocked: save-origin evidence is missing or belongs to a different selected runtime after branch switch"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.cs" `
    "records save-origin evidence on Pull/Push and includes it in baseline Push prerequisites" `
    @(
        "LauncherSaveOriginEvidence\.WriteManualPullOrigin",
        "LauncherSaveOriginEvidence\.WriteManualPushOrigin",
        "BaselineManualPushPrerequisitesSatisfied",
        "LauncherSaveOriginEvidence\.CurrentLocalSavesMatchSelectedRuntime"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.cs" `
    "marks save origin pending after branch switch and requires save-origin evidence before branch-switch Push" `
    @(
        "LauncherSaveOriginEvidence\.WriteBranchSwitchPendingOrigin",
        "LauncherSaveOriginEvidence\.CurrentLocalSavesMatchSelectedRuntime",
        "ManualPushPrerequisitesSatisfied"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.Reports.cs" `
        "surfaces runtime slot, runtime pack, patch validation, runtime patch, and save-origin evidence" `
    @(
        "Runtime slot evidence marker present",
        "Runtime slot evidence selected branch matches current runtime",
        "Runtime slot evidence runtime slot ID matches current runtime",
        "Runtime slot evidence selected PCK matches current runtime",
        "Runtime slot evidence selected source sts2\.dll matches current runtime",
        "Selected runtime requires usable runtime pack",
        "Runtime cache marker selected branch requires runtime pack",
        "Selected runtime branch-matched Android runtime prepared",
        "Selected runtime pack status",
        "Selected runtime pack usability status",
        "Selected runtime release version",
        "Selected runtime depot manifest fingerprint",
        "Selected runtime identity summary",
        "Selected runtime slot ID",
        "Selected runtime pack source runtime slot ID",
        "Selected runtime pack source runtime slot ID matches selected runtime",
        "Selected runtime pack generated from clean directory",
        "Selected runtime pack support assemblies declared",
        "Selected runtime pack support assembly hashes declared",
        "Selected patch compatibility status",
        "Selected patch compatibility validation surface version",
        "Selected patch compatibility checked symbol count",
        "Runtime patch validation status",
        "Runtime cache marker present",
        "Runtime cache prepared for selected runtime",
        "Android save-origin marker present",
        "Android save-origin selected runtime slot ID matches current runtime",
        "Android save-origin current selected runtime is playable",
        "Android local saves verified for selected branch",
        "Android local saves verified for selected runtime",
        "Android save-origin selected PCK matches current runtime",
        "Android save-origin selected source sts2\.dll matches current runtime",
        "Selected runtime playable"
    )

Add-Check `
    "scripts\capture-multi-version-runtime-evidence.ps1" `
    "captures read-only multi-version runtime evidence for future branch updates" `
    @(
        "multi-version-runtime",
        "runtime-marker-files\.txt",
        "runtime-marker-contents\.txt",
        "runtime-hashes\.txt",
        "current_runtime_slot\.json",
        "Installed runtime slot evidence",
        "Runtime slot marker matches selected files",
        "Read-DeviceSha256",
        "Read-DeviceRuntimePackDllNames",
        "Test-RuntimePackClosedDllSet",
        "RunLabel",
        "safeRunLabel",
        'multi-version-runtime-\$safeRunLabel-\$timestamp',
        "run-metadata\.json",
        "artifactFolderName",
        "Run label:",
        "Collector boundary: This collector is read-only and does not mutate Steam Cloud or app data",
        "closedDllSet",
        "runtimeSlotPckActualSha256",
        "runtimeSlotSourceAssemblyActualSha256",
        "stale-or-unreadable",
        "Installed slot matches runtime patch validation",
        "installedPck",
        "validatedPck",
        "installedSourceAssembly",
        "validatedSourceAssembly",
        "validation-report\.md",
        "Multi-version runtime validation report",
        "Mixed/split asset hypothesis matrix",
        "Status values: confirmed, ruled out, likely, unknown, needs device-only validation",
        "Steam branch partial/shared content",
        "Stale/incomplete downloader cache",
        "Wrong launch path",
        "Shared assembly/runtime cache",
        "In-process branch switch reuse",
        "Android PCK patch side effect",
        "Godot import/resource mismatch",
        "Save/config asset reference mismatch",
        "Prepared cache matches runtime",
        "Canonical slot bound to native cache identity",
        "Selected runtime pack manifest",
        "runtimePackValidation",
        "packClean",
        "packReportMatches",
        "runtimePackReportMatches",
        "runtimePackClosedDllSet",
        "generatedFromCleanDirectory",
        "blocked-no-usable-runtime",
        "missing-or-rejected",
        "requiresPack",
        "this should be treated as invalid",
        "selected_runtime_pack_compatibility\.json",
        "selected_runtime_pack_patch_validation\.json",
        "Steam Cloud Push save-origin safety",
        "save-origin runtime slot ID",
        "Selected runtime playable:",
        "Current Android local saves verified for selected runtime:",
        "runtimePlayable",
        "savesVerified",
        "last_runtime_patch_validation\.json",
        "current_runtime_slot\.json",
        "current_android_save_origin\.txt",
        "logcat-runtime-filtered\.txt",
        "This collector is read-only and does not mutate Steam Cloud or app data"
    )

Add-ForbiddenCheck `
    "scripts\capture-multi-version-runtime-evidence.ps1" `
    "keeps runtime evidence collection read-only" `
    @(
        "rm\s+-rf",
        "adb[^\r\n]+push",
        "adb[^\r\n]+install",
        "ManualPush",
        "WriteManualPush"
    )

Add-Check `
    "scripts\review-multi-version-runtime-evidence.ps1" `
    "reviews collected multi-version runtime evidence without mutating device or cloud state" `
    @(
        "RequirePublicBeta",
        "RequireBranchSwitch",
        "RequireSaveSafety",
        "RequireResolvedClassification",
        "Require-NoPattern",
        "run-metadata\.json",
        "metadata identifies collector",
        "metadata records read-only collector boundary",
        "metadata label",
        "last_game_branch_switch",
        "has branch-switch marker file evidence",
        "has branch-switch marker content evidence",
        "Run label:\\s\*public",
        "Run label:\\s\*public-beta",
        "Run label:\\s\*branch-switch",
        "validation-report\.md",
        "Mixed/split asset hypothesis matrix",
        "diagnostics/current_runtime_slot\.json",
        "diagnostics/current_runtime_cache\.txt",
        "diagnostics/selected_runtime_pack_compatibility\.json",
        "diagnostics/selected_runtime_pack_patch_validation\.json",
        "logs/logcat-runtime-filtered\.txt",
        "runtime pack validation report passed",
        "Steam Cloud Push save-origin safety",
        "does not carry unknown classifications into release signoff",
        "public-beta slot is selected"
    )

Add-ForbiddenCheck `
    "scripts\review-multi-version-runtime-evidence.ps1" `
    "keeps evidence review local and read-only" `
    @(
        "adb",
        "run-as",
        "rm\s+-rf",
        "Remove-Item",
        "Set-Content",
        "New-Item",
        "ManualPush",
        "WriteManualPush"
    )

Add-Check `
    "scripts\run-multi-version-runtime-release-gates.ps1" `
    "runs static multi-version release gates and optional local evidence review" `
    @(
        "audit-multi-version-runtime\.ps1",
        "audit-steam-version-selection\.ps1",
        "audit-steam-branch-guidance-parity\.ps1",
        "review-multi-version-runtime-evidence\.ps1",
        "PublicEvidenceDirs",
        "PublicBetaEvidenceDirs",
        "BranchSwitchEvidenceDirs",
        "RequirePublic",
        "RequirePublicBeta",
        "RequireBranchSwitch",
        "RequireSaveSafety",
        "RequireResolvedClassification",
        "Invoke-EvidenceReview",
        "EvidenceDirs"
    )

Add-ForbiddenCheck `
    "scripts\run-multi-version-runtime-release-gates.ps1" `
    "keeps release gate runner off device and cloud mutation paths" `
    @(
        "adb",
        "run-as",
        "rm\s+-rf",
        "Remove-Item",
        "Set-Content",
        "New-Item",
        "ManualPush",
        "WriteManualPush",
        "capture-multi-version-runtime-evidence\.ps1"
    )

Add-Check `
    "docs\multi-version-runtime-architecture.md" `
    "documents the implemented runtime-slot, pack, patch-validation, save-origin, and evidence workflow" `
    @(
        "GameRuntimeSlot",
        "runtime slot ID",
        "files/runtime_packs/<branch>/sts2\.dll",
        "compatibility\.json",
        "patch_validation\.json",
        "last_runtime_patch_validation\.json",
        "current_android_save_origin\.txt",
        "capture-multi-version-runtime-evidence\.ps1",
        "review-multi-version-runtime-evidence\.ps1",
        "run-multi-version-runtime-release-gates\.ps1",
        "RequireResolvedClassification",
        "validation-report\.md",
        "read-only"
    )

Add-Check `
    "docs\multi-version-runtime-release-gates.md" `
    "documents release signoff gates for public/beta runtime coexistence" `
    @(
        "Multi-version runtime release gates",
        "audit-multi-version-runtime\.ps1",
        "audit-steam-version-selection\.ps1",
        "audit-steam-branch-guidance-parity\.ps1",
        "ARM64 physical Android device",
        "branch switch public -> public-beta -> public -> public-beta",
        "capture-multi-version-runtime-evidence\.ps1",
        "review-multi-version-runtime-evidence\.ps1",
        "run-multi-version-runtime-release-gates\.ps1",
        "RunLabel public",
        "RunLabel public-beta",
        "RunLabel branch-switch",
        "RequirePublic",
        "RequireBranchSwitch",
        "RequireResolvedClassification",
        "current_runtime_slot\.json",
        "runtime pack closed DLL set passes",
        "active publish-cache.*sts2\.dll.*hash matches",
        "Steam Cloud Push must not be used during branch-runtime investigation",
        "Steam branch partial/shared content",
        "save/config asset reference mismatch",
        "public-beta can launch without a usable runtime pack",
        "branch switch reuses the previous branch runtime cache",
        "full runtime Harmony validation is still post-startup"
    )

if ($failures.Count -gt 0) {
    Write-Host ""
    Write-Host "Multi-version runtime audit failed:"
    foreach ($failure in $failures) {
        Write-Host "FAIL $failure"
    }
    throw "Multi-version runtime audit failed with $($failures.Count) failure(s)."
}

Write-Host "Multi-version runtime audit passed ($passes checks)."
