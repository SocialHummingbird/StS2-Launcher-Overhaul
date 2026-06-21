function Add-MultiVersionRuntimeEvidenceToolingChecks {
    Add-Check `
        "scripts\capture-multi-version-runtime-evidence.ps1" `
        "captures read-only multi-version runtime evidence for future branch updates" `
        @(
            "android-shell-utils\.ps1",
            "evidence-marker-utils\.ps1",
            "evidence-report-utils\.ps1",
            "capture-multi-version-runtime-evidence\.helpers\.ps1",
            "multi-version-runtime",
            "runtime-marker-files\.txt",
            "runtime-marker-contents\.txt",
            "runtime-hashes\.txt",
            "current_runtime_slot\.json",
            "Installed runtime slot evidence",
            "Runtime slot marker matches selected files",
            "RunLabel",
            "safeRunLabel",
            "Resolve-AndroidAdbPath",
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
            "last_game_version_cache_cleanup\.txt",
            "last_game_version_redownload\.txt",
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
            "Compare-InstalledRuntimeEvidence",
            "Compare-SaveOriginEvidence",
            "Compare-RuntimePackEvidence",
            "Test-RuntimeCacheMatchesValidation",
            "Test-EvidenceTrue",
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

    Add-Check `
        "scripts\capture-multi-version-runtime-evidence.helpers.ps1" `
        "keeps multi-version runtime evidence capture helpers isolated from report flow" `
        @(
            "Invoke-AdbText",
            "Invoke-RunAsText",
            "Save-Text",
            "Save-AdbText",
            "Save-RunAsText",
            "Read-JsonFile",
            "Read-DeviceSha256",
            "Read-DeviceRuntimePackDllNames",
            "Get-ObjectPropertyMap",
            "ConvertTo-EvidenceString",
            "Test-EvidenceTrue",
            "Compare-InstalledRuntimeEvidence",
            "Test-RuntimeCacheMatchesValidation",
            "Compare-SaveOriginEvidence",
            "Compare-RuntimePackEvidence",
            "Test-RuntimePackClosedDllSet",
            "ConvertTo-AndroidShellSingleQuoted",
            "ConvertTo-AndroidShellPathSingleQuoted",
            "PckMatchesRuntimePackSource",
            "ReportMatches",
            "supportAssemblySha256",
            "closed",
            "mismatch"
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

    Add-ForbiddenCheck `
        "scripts\capture-multi-version-runtime-evidence.helpers.ps1" `
        "keeps runtime evidence capture helpers read-only" `
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
}
