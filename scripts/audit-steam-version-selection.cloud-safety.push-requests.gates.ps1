function Add-SteamVersionSelectionCloudSafetyPushGateChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncCoordinator.PushSafety.cs" `
        "routes manual Push entry points through structured eligibility" `
        @(
            "CloudPushPressed",
            "RejectWhenOperationActive",
            "CloudPushSafetyContext\.Create",
            "EvaluateCloudPushEligibility",
            "EnsureCloudPushStillEligible",
            "CloudPushEligibilityResult",
            "CloudPushEligibilityPolicy\.Evaluate",
            "CaptureEligibilityState",
            "\.IsEligible",
            "ManualCloudSyncRequest\.Push",
            "SteamGameBranch\.Normalize\(expectedBranch\)",
            "SteamGameBranch\.Normalize\(currentBranch\)",
            "eligibility\.BlockingReasons",
            "blocked before any Steam Cloud write",
            "state changed after confirmation"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncCoordinator.PushSafety.Context.cs" `
        "captures every read-only fact required for Push eligibility" `
        @(
            "CloudPushSafetyContext",
            "LauncherPreferences\.ReadGameBranch\(\)",
            "SteamGameBranch\.DisplayName",
            "SelectedBranch",
            "SelectedVersion",
            "CaptureEligibilityState",
            "CloudPushEligibilityState",
            "LauncherWorkshopModSafety\.ActiveSelectedModCount",
            "LastManualPullCompletionRecorded",
            "LastManualPullMatchesSelectedBranch",
            "LauncherLocalSaveEvidence\.HasImportantSaveEvidence",
            "LauncherSaveOriginEvidence\.CurrentLocalSavesMatchSelectedRuntime",
            "LauncherBranchSwitchSafety\.HasMarker",
            "LauncherBranchSwitchSafety\.HasRequiredEvidence",
            "LauncherCloudSyncEvidence\.HasManualPullAfterBranchSwitch",
            "LauncherPreferences\.ReadLocalBackupEnabled",
            "AppPaths\.HasStoragePermission"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\CloudPushEligibilityResult.cs" `
        "models blockers and deduplicated required actions as immutable structured data" `
        @(
            "CloudPushEligibilityBlockCode",
            "CloudPushRequiredActionCode",
            "CloudPushEligibilityBlock",
            "CloudPushRequiredAction",
            "CloudPushEligibilityResult",
            "BlockingReasons",
            "RequiredNextActions",
            "ReadOnlyCollection",
            "HashSet<CloudPushRequiredActionCode>",
            "IsEligible"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\CloudPushEligibilityPolicy.cs" `
        "collects every applicable Push blocker without short-circuiting" `
        @(
            "CloudPushEligibilityPolicy",
            "CloudPushEligibilityResult Evaluate",
            "ModsSelected",
            "ManualPullNotCompleted",
            "ManualPullVersionMismatch",
            "ImportantLocalSavesMissing",
            "LocalSaveOriginNotVerified",
            "BranchSwitchEvidenceInvalid",
            "ManualPullAfterBranchSwitchMissing",
            "LocalBackupDisabledAfterBranchSwitch",
            "BackupStoragePermissionMissing",
            "AddBranchSwitchBlocks",
            "HasBranchSwitchMarker"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Events.cs" `
        "propagates structured Push eligibility through launcher event wiring" `
        @(
            "Func<CloudPushEligibilityResult> cloudPushArmRequested",
            "CloudPushArmRequested \+= cloudPushArmRequested"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.cs" `
        "exposes a structured Push eligibility request event" `
        @(
            "event Func<CloudPushEligibilityResult> CloudPushArmRequested"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.CloudPush.cs" `
        "consumes eligibility without reducing the gate contract to a boolean" `
        @(
            "var eligibility = CloudPushArmRequested\?\.Invoke\(\)",
            "eligibility == null \|\| !eligibility\.IsEligible",
            "ReadAndApplyCloudPushEligibility"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\CloudPushEligibilityPresentation.cs" `
        "renders every blocker and unique next action as clear Upload guidance" `
        @(
            "CloudPushEligibilityPresentation",
            "Upload unavailable:",
            "Why upload is unavailable:",
            "result\.BlockingReasons",
            "How to unlock upload:",
            "result\.RequiredNextActions",
            "ReviewButtonDetail",
            "checks passed"
        )

    Add-Check `
        "scripts\test-launcher-cloud-upload-workflow.ps1" `
        "runs focused Upload workflow presentation tests" `
        @(
            "CloudPushEligibilityPresentation\.cs",
            "LauncherCloudUploadWorkflowTest\.cs",
            "TreatWarningsAsErrors>true",
            "dotnet\.Source run"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\CloudPushEligibilityPolicy.cs" `
        "keeps eligibility policy independent of UI, storage, and launcher services" `
        @(
            "_view",
            "LauncherPreferences",
            "LauncherCloudSyncEvidence",
            "LauncherLocalSaveEvidence",
            "LauncherSaveOriginEvidence",
            "LauncherBranchSwitchSafety",
            "AppPaths",
            "SetStatus",
            "AppendLog"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherCloudSyncCoordinator.PushSafety.Context.cs" `
        "keeps eligibility fact capture read-only and UI-neutral" `
        @(
            "_view",
            "SetStatus",
            "AppendLog",
            "SaveLocalBackupEnabled",
            "RequestStoragePermission",
            "EnsureExternalDirectories",
            "WriteManualPushBlockedMarker"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherCloudSyncCoordinator.PushSafety.cs" `
        "keeps coordinator eligibility decisions UI-neutral" `
        @(
            "_view",
            "SetStatus",
            "AppendLog",
            "SetActionPreferences"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherView.Events.cs" `
        "prevents boolean Push eligibility from returning at the view boundary" `
        @(
            "Func<bool> cloudPushArmRequested"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\Sections\ActionSection.cs" `
        "prevents boolean Push eligibility from returning at the action boundary" `
        @(
            "event Func<bool> CloudPushArmRequested"
        )

    Add-Check `
        "scripts\test-launcher-cloud-safety-evidence.ps1" `
        "runs real marker and backup-policy cloud safety regressions" `
        @(
            "LauncherMarkerFile\.Read\.cs",
            "CloudPushEligibilityPolicy\.cs",
            "ManualPushBackupSafetyPolicy\.cs",
            "LauncherCloudSyncEvidence\.Pull\.cs",
            "LauncherCloudSafetyEvidenceTest\.cs",
            "TreatWarningsAsErrors>true",
            "dotnet\.Source run"
        )

    Add-Check `
        "scripts\tests\LauncherCloudSafetyEvidenceTest.cs" `
        "proves incomplete Pulls and missing backup evidence cannot unlock Upload" `
        @(
            "PullStartInvalidatesPriorSuccess",
            "PartialPullRemainsIneligible",
            "SaveOriginFailureRemainsIneligible",
            "BranchIdentityAndOrderingRemainEnforced",
            "PrePushBackupEvidenceFailsClosed",
            "ManualPullNotCompleted",
            "LocalSaveOriginNotVerified",
            "tests passed 6/6"
        )
}
