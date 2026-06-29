function Add-MultiVersionRuntimeSaveSafetyChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherSaveOriginEvidence.cs" `
        "defines selected-runtime save-origin marker identity" `
        @(
            "internal static partial class LauncherSaveOriginEvidence",
            "current_android_save_origin\.txt",
            "MarkerPath",
            "MarkerPresent"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherSaveOriginEvidence.Fields.cs" `
        "centralizes selected-runtime save-origin marker prefixes" `
        @(
            "OriginActionPrefix = ""Origin action:""",
            "SelectedBranchPrefix = ""Selected branch:""",
            "SelectedRuntimeSlotIdPrefix = ""Selected runtime slot ID:""",
            "SelectedPckSha256Prefix = ""Selected PCK SHA256:""",
            "SelectedSourceAssemblySha256Prefix = ""Selected source sts2\.dll SHA256:""",
            "SelectedRuntimePlayablePrefix = ""Selected runtime playable:""",
            "SelectedRuntimeReadinessProblemPrefix = ""Selected runtime readiness problem:""",
            "ImportantLocalSaveEvidenceCountPrefix = ""Important Android local save evidence count:""",
            "CurrentLocalSavesVerifiedForSelectedBranchPrefix = ""Current Android local saves verified for selected branch:""",
            "CurrentLocalSavesVerifiedForSelectedRuntimePrefix = ""Current Android local saves verified for selected runtime:""",
            "RequiredNextActionPrefix = ""Required next action:"""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherSaveOriginEvidence.Read.cs" `
        "reads selected-runtime save-origin marker fields" `
        @(
            "OriginUtcParseable",
            "ReadMarkerValue",
            "LauncherMarkerFile\.ReadValue",
            "LauncherMarkerFile\.ReadUtc",
            "LauncherMarkerFile\.HasConcreteValue",
            "SelectedRuntimeSlotId",
            "SelectedPckSha256Prefix",
            "SelectedSourceAssemblySha256Prefix",
            "SelectedRuntimePlayablePrefix",
            "SelectedRuntimeReadinessProblemPrefix",
            "ImportantLocalSaveEvidenceCountPrefix"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherSaveOriginEvidence.RuntimeMatch.cs" `
        "matches selected-runtime save origin to selected branch and runtime" `
        @(
            "CurrentLocalSavesMatchSelectedBranch",
            "CurrentLocalSavesMatchSelectedRuntime",
            "RuntimeSlotIdMatchesSelectedRuntime",
            "PckMatchesSelectedRuntime",
            "SourceAssemblyMatchesSelectedRuntime",
            "SelectedRuntimeCurrentlyPlayable",
            "slot\.Playable",
            "SourcePckMatchesSelectedPck"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherSaveOriginEvidence.Write.cs" `
        "writes selected-runtime save origin and invalidates saves on branch switch" `
        @(
            "WriteManualPullOrigin",
            "WriteManualPushOrigin",
            "WriteBranchSwitchPendingOrigin",
            "SelectedRuntimeSlotIdPrefix",
            "SelectedPckSha256Prefix",
            "SelectedSourceAssemblySha256Prefix",
            "SelectedRuntimePlayablePrefix",
            "SelectedRuntimeReadinessProblemPrefix",
            "CurrentLocalSavesVerifiedForSelectedBranchPrefix",
            "CurrentLocalSavesVerifiedForSelectedRuntimePrefix",
            "RequiredNextActionPrefix"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSaveState.cs" `
        "keeps cloud-sync state summary and launch-disable toggles centralized" `
        @(
            "StatusSummary",
            "HasSavedCredentials",
            "_cloudSyncEnabled",
            "_savedCredentials",
            "SetCloudSyncEnabled",
            "DisableCloudSyncForLaunch"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSaveState.Credentials.cs" `
        "captures persisted Steam credentials for cloud sync without app password storage" `
        @(
            "SavedSteamCredentials",
            "TryUseCredentials",
            "SaveCredentials\(string accountName, string refreshToken\)",
            "SavedSteamCredentials\.FromLogin",
            "_savedCredentials = null",
            "Saved Steam credentials available for cloud sync",
            "Saved Steam credentials unavailable for cloud sync",
            "ClearCredentials",
            "CloudSaveStoreFactory\.CreateCloudSaveStore"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSaveState.SaveManager.cs" `
        "uses cloud store only when credentials are available and falls back to Android local saves" `
        @(
            "TryCreateEnabledSaveManager",
            "TryGetSavedCredentialsForCloudSync",
            "TryCreateAndroidLocalSaveManager",
            "Cloud sync disabled - using Android local-only SaveManager when available",
            "No saved credentials - using Android local-only SaveManager when available",
            "CloudSaveStoreFactory\.CreateLocalOnlyCloudSaveStore",
            "Created Android local-only SaveManager"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSaveState.ManualSync.cs" `
        "requires saved credentials for manual cloud pull and push entry points" `
        @(
            "ManualPushAllAsync",
            "ManualPullAllAsync",
            "CloudSyncCoordinator\.ManualPushAllAsync",
            "CloudSyncCoordinator\.ManualPullAllAsync",
            "RequireSavedCredentials",
            "No saved Steam credentials\. Log in again before pulling cloud saves"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncCoordinator.PushSafety.Context.cs" `
        "shares selected branch context across Push safety gates" `
        @(
            "CloudPushSafetyContext",
            "LauncherPreferences\.ReadGameBranch\(\)",
            "SelectedBranch",
            "SelectedVersion",
            "WriteBlockedMarker"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncCoordinator.PushSafety.Baseline.cs" `
        "blocks baseline Push when save-origin evidence is missing or belongs to another selected runtime" `
        @(
            "CanPushWithBaselineEvidence",
            "LauncherSaveOriginEvidence\.CurrentLocalSavesMatchSelectedRuntime",
            "Manual Push blocked: Android local save origin evidence does not match the selected runtime"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncCoordinator.PushSafety.BranchSwitch.cs" `
        "blocks branch-switch Push when save-origin evidence belongs to another selected runtime" `
        @(
            "CanPushAfterBranchSwitch",
            "LauncherSaveOriginEvidence\.CurrentLocalSavesMatchSelectedRuntime",
            "Manual Push blocked: save-origin evidence is missing or belongs to a different selected runtime after branch switch"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Pull.cs" `
        "records save-origin evidence on Pull and includes it in baseline Push prerequisites" `
        @(
            "LauncherSaveOriginEvidence\.WriteManualPullOrigin",
            "BaselineManualPushPrerequisitesSatisfied",
            "LauncherSaveOriginEvidence\.CurrentLocalSavesMatchSelectedRuntime"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Push.Write.cs" `
        "records save-origin evidence on successful manual Push" `
        @(
            "LauncherSaveOriginEvidence\.WriteManualPushOrigin",
            "WriteManualPushMarker",
            "ManualPushCompletedAfterBranchSwitchSafetyGatesPrefix"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.Write.cs" `
        "marks selected-runtime save origin pending after branch switch" `
        @(
            "WriteMarker",
            "SteamGameBranch\.Normalize",
            "SteamGameInstallPaths\.VersionSlotDirectory",
            "LauncherSaveOriginEvidence\.WriteBranchSwitchPendingOrigin"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.Gates.cs" `
        "requires save-origin evidence before branch-switch Push" `
        @(
            "HasRequiredEvidence",
            "LauncherCloudSyncEvidence\.HasManualPullAfterBranchSwitch",
            "LauncherLocalSaveEvidence\.HasImportantSaveEvidence",
            "LauncherSaveOriginEvidence\.CurrentLocalSavesMatchSelectedRuntime",
            "AppPaths\.HasStoragePermission",
            "ManualPushPrerequisitesSatisfied"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.SavePathDiscovery.Fallback.cs" `
        "keeps Android fallback save path orchestration separate from profiles and enumeration" `
        @(
            "AddFallbackProfilePaths",
            "FallbackRootFiles",
            "FallbackProfiles\(\)",
            "AddEnumeratedSavePaths"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.SavePathDiscovery.FallbackProfiles.cs" `
        "keeps fallback profile templates and history selection isolated" `
        @(
            "FallbackHistoryDirectories",
            "FallbackProfileFiles",
            "FallbackProfilePrefixes",
            "HistoryFileSelection",
            "SelectFallbackRunHistoryFiles",
            "LimitRunHistory\(SelectRunHistoryFiles",
            "ProfileIds\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.SavePathDiscovery.Enumeration.cs" `
        "keeps bounded Android save path enumeration isolated and failure-tolerant" `
        @(
            "EnumeratedPathLimit",
            "EnumeratedDirectoryDepthLimit",
            "IgnoredEnumerationDirectories",
            "SafeGetFiles",
            "SafeGetDirectories",
            "IsDiscoveredSavePath",
            "ShouldSkipEnumeratedDirectory",
            "Save path enumeration failed, using fallback paths only"
        )
}
