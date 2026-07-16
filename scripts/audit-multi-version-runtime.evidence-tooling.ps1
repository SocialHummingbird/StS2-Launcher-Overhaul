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
            "last_launch_attempt\.txt",
            "Start Game launch-attempt marker",
            "Launch attempt matches runtime validation",
            "Prepared readiness used:",
            "Launch attempt successful handoff phase:",
            "Launch attempt UTC:",
            "Launch attempt ID:",
            "Launch attempt action:",
            "Launch attempt source:",
            "Readiness cache status:",
            "Launch attempt elapsed ms:",
            "Launch readiness elapsed ms:",
            "Mod readiness elapsed ms:",
            "Mod readiness cache status:",
            "Mod play mode:",
            "Mod enabled count:",
            "Modded save cloud push locked:",
            "Test-LaunchAttemptId",
            "Test-LaunchReadinessCacheStatus",
            "Test-ModReadinessCacheStatus",
            "Test-LaunchTimingValue",
            "Test-ModPlayMode",
            "Test-BoolText",
            "Test-NonNegativeIntText",
            "launchAttemptProofReady",
            "launchAttemptIdValid",
            "Launch attempt timing measured:",
            "Launch attempt mod readiness cache status:",
            "Launch attempt mod play mode:",
            "Launch attempt enabled mod count:",
            "Launch attempt modded-save Cloud Push locked:",
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
        "scripts\capture-launch-attempt-runtime-evidence.ps1" `
        "captures and immediately reviews Start Game launch-attempt runtime evidence without mutating device or cloud state" `
        @(
            "capture-multi-version-runtime-evidence\.ps1",
            "review-multi-version-runtime-evidence\.ps1",
            "Resolve-AndroidAdbPath",
            "Resolve-AndroidTargetDevice",
            "Resolve-AndroidInstalledLauncherPackageName",
            "WaitForDeviceSeconds",
            "Choose only one of -RequirePublic, -RequirePublicBeta, or -RequireBranchSwitch",
            "Write-Status",
            "Quiet",
            "RunLabel",
            "RequireLaunchAttempt",
            "MaxLaunchAttemptAgeMinutes",
            "Multi-version runtime evidence captured:",
            "Launch-attempt runtime evidence captured and reviewed",
            "IncludeRawLogcat"
        )

    Add-ForbiddenCheck `
        "scripts\capture-launch-attempt-runtime-evidence.ps1" `
        "keeps launch-attempt evidence wrapper read-only" `
        @(
            "adb[^\r\n]+install",
            "pm\s+clear",
            "am\s+start",
            "input\s+tap",
            "ManualPush",
            "WriteManualPush"
        )

    Add-Check `
        "scripts\evidence-launch-attempt-phases.ps1" `
        "centralizes launch-attempt phase proof classification for capture and review" `
        @(
            "LaunchAttemptSuccessfulHandoffPhases",
            "LaunchAttemptRejectedProofPhases",
            "Test-SuccessfulLaunchAttemptPhase",
            "Get-LaunchAttemptSuccessfulPhaseRegex",
            "Get-LaunchAttemptRejectedProofPhaseRegex",
            "restart requested",
            "safe android restart requested",
            "in-process signalled",
            "setup failed",
            "checking",
            "blocked",
            "blocked in model",
            "readiness failed",
            "in-process signal failed",
            "launch handoff not requested",
            "restart requested without ready files"
        )

    Add-Check `
        "scripts\review-multi-version-runtime-evidence.ps1" `
        "reviews collected multi-version runtime evidence without mutating device or cloud state" `
        @(
            "RequirePublicBeta",
            "RequireBranchSwitch",
            "RequireSaveSafety",
            "RequireLaunchAttempt",
            "RequireResolvedClassification",
            "MaxLaunchAttemptAgeMinutes",
            "Require-NoPattern",
            "Require-LaunchAttemptFreshness",
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
            "diagnostics/last_launch_attempt\.txt",
            "evidence-launch-attempt-phases\.ps1",
            "Get-LaunchAttemptSuccessfulPhaseRegex",
            "Get-LaunchAttemptRejectedProofPhaseRegex",
            "summarizes launch-attempt timestamp",
            "records launch-attempt timestamp",
            "summarizes launch-attempt ID",
            "records launch-attempt ID",
            "summarizes launch-attempt action",
            "summarizes launch-attempt source",
            "records launch action",
            "records launch source",
            "launch-attempt marker is fresh enough",
            "launch-attempt marker older than freshness window",
            "launch-attempt marker is captured",
            "does not accept NativeFallback or Android fatal crash logs as launch-attempt proof",
            "records ready launch state",
            "uses prepared readiness",
            "records successful launch handoff phase",
            "does not treat failed launch handoff as success",
            "records launch attempt timing",
            "records measured launch attempt timing",
            "records measured launch readiness timing",
            "records measured mod readiness timing",
            "records concrete launch readiness cache status",
            "records selected game directory",
            "records selected PCK path",
            "records selected source assembly path",
            "records active Android assembly path",
            "records runtime-pack directory",
            "records runtime-pack manifest path",
            "records mod readiness phase",
            "records concrete mod readiness cache status",
            "records concrete mod play mode",
            "records numeric installed mod count",
            "records numeric enabled mod count",
            "records numeric unsupported mod count",
            "records modded-save Cloud Push lock state",
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
            "test-multi-version-runtime-evidence-reviewer\.ps1",
            "review-multi-version-runtime-evidence\.ps1",
            "PublicEvidenceDirs",
            "PublicBetaEvidenceDirs",
            "BranchSwitchEvidenceDirs",
            "RequirePublic",
            "RequirePublicBeta",
            "RequireBranchSwitch",
            "RequireSaveSafety",
            "RequireLaunchAttempt",
            "RequireResolvedClassification",
            "MaxLaunchAttemptAgeMinutes",
            "quietArguments",
            "reviewerTestArguments",
            '\*> \$null',
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
            "scripts\test-multi-version-runtime-evidence-reviewer.ps1" `
        "regression-tests multi-version runtime evidence reviewer launch-attempt gates" `
        @(
            "review-multi-version-runtime-evidence\.ps1",
            "evidence-launch-attempt-phases\.ps1",
            "LauncherLaunchAttemptPhases\.cs",
            "Assert-LaunchAttemptPhaseContractMatchesCSharp",
            "Assert-LaunchAttemptCacheStatusContractMatchesCSharp",
            "LauncherLaunchReadinessCacheStatus\.cs",
            "LauncherModLaunchReadinessCacheStatus\.cs",
            "Get-ReviewerStatusValues",
            "Assert-SameStringSet",
            "Launch-attempt readiness cache status reviewer contract",
            "Launch-attempt mod readiness cache status reviewer contract",
            "Normalize-LaunchAttemptPhase",
            "LaunchAttemptSuccessfulHandoffPhases",
            "LaunchAttemptRejectedProofPhases",
            "C# launch-attempt phases missing reviewer classification",
            "Reviewer launch-attempt phases missing C# constants",
            "launch-attempt cache status contracts match C# constants",
            "both success and rejected proof",
            "New-MultiVersionRuntimeEvidenceBundle",
            "RequireLaunchAttempt",
            "Quiet",
            "Write-TestPass",
            "last_launch_attempt\.txt",
            "UTC:",
            "Attempt ID:",
            "Action:",
            "Source:",
            "LaunchAttemptUnmeasuredTiming",
            "LaunchAttemptStale",
            "LaunchAttemptFallbackLog",
            "MaxLaunchAttemptAgeMinutes",
            "Prepared readiness used:",
            "Files ready:",
            "Mod readiness cache status:",
            "Mod play mode:",
            "Mod enabled count:",
            "Modded save cloud push locked:",
            "Start Game launch-attempt marker",
            "Launch attempt matches runtime validation",
            "old artifact without launch-attempt accepted when not required",
            "launch-attempt marker older than freshness window",
            "launch-attempt marker without prepared readiness",
            "launch-attempt marker without measured readiness timings",
            "launch-attempt marker that only reached pre-handoff ready phase",
            "launch-attempt marker that only reached pre-readiness checking phase",
            "launch-attempt marker that only reached mod readiness checking phase",
            "launch-attempt marker that failed before selected-version readiness",
            "launch-attempt marker that failed selected-version readiness",
            "launch-attempt marker with failed in-process signal",
            "launch-attempt marker where model returned without requesting handoff",
            "launch-attempt marker with NativeFallbackActivity log evidence",
            "launch-attempt report not matched to runtime validation"
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
            "last_launch_attempt\.txt",
            "current_android_save_origin\.txt",
            "capture-multi-version-runtime-evidence\.ps1",
            "review-multi-version-runtime-evidence\.ps1",
            "run-multi-version-runtime-release-gates\.ps1",
            "RequireResolvedClassification",
            "RequireLaunchAttempt",
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
            "RequireLaunchAttempt",
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
