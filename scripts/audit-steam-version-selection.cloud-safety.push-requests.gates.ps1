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
        "captures only local-save presence for Push eligibility" `
        @(
            "CloudPushSafetyContext",
            "LauncherPreferences\.ReadGameBranch\(\)",
            "SelectedBranch",
            "CaptureEligibilityState",
            "CloudPushEligibilityState",
            "CloudSyncCoordinator\.HasTransferableLocalSaveContent",
            "LauncherModSelectionState\.IsModdedMode",
            "SaveNamespace\.Modded",
            "SaveNamespace\.Vanilla"
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
        "blocks Push only when transferable local saves are absent" `
        @(
            "CloudPushEligibilityPolicy",
            "CloudPushEligibilityResult Evaluate",
            "HasImportantLocalSaveEvidence",
            "ImportantLocalSavesMissing",
            "VerifyAndroidLocalSaves",
            "No transferable Android local save files were found",
            "Open the game and verify that Android local saves exist"
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
        "renders local-save eligibility and remediation as clear Upload guidance" `
        @(
            "CloudPushEligibilityPresentation",
            "Upload available\.",
            "Android local saves are present",
            "Local saves found",
            "Upload unavailable:",
            "Why upload is unavailable:",
            "result\.BlockingReasons",
            "How to unlock upload:",
            "result\.RequiredNextActions",
            "ReviewButtonDetail",
            "check.*to fix"
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
        "keeps eligibility fact capture local-only, read-only, and UI-neutral" `
        @(
            "_view",
            "SetStatus",
            "AppendLog",
            "LauncherWorkshopModSafety",
            "LauncherCloudSyncEvidence",
            "LauncherSaveOriginEvidence",
            "LauncherBranchSwitchSafety",
            "ReadLocalBackupEnabled",
            "HasStoragePermission",
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
        "runs real marker and local-save eligibility regressions" `
        @(
            "LauncherMarkerFile\.Read\.cs",
            "CloudPushEligibilityPolicy\.cs",
            "LauncherCloudSyncEvidence\.Pull\.cs",
            "LauncherCloudSafetyEvidenceTest\.cs",
            "TreatWarningsAsErrors>true",
            "dotnet\.Source run"
        )

    Add-Check `
        "scripts\tests\LauncherCloudSafetyEvidenceTest.cs" `
        "proves Pull diagnostics remain observable while live transfer state gates Upload" `
        @(
            "SuccessfulPullRecordsEvidence",
            "FailedPullRecordsDiagnosticOutcome",
            "SaveOriginFailureRecordsEvidenceFailure",
            "BranchEvidenceTracksIdentityAndOrdering",
            "LocalSavePresenceControlsEligibility",
            "ImportantLocalSavesMissing",
            "VerifyAndroidLocalSaves",
            "tests passed 5/5"
        )
}
