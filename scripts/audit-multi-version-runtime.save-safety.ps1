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
        "keeps persisted Steam credentials confined to foreground launcher transfers" `
        @(
            "SavedSteamCredentials",
            "TryUseCredentials",
            "SaveCredentials\(string accountName, string refreshToken\)",
            "SavedSteamCredentials\.FromLogin",
            "RunManualSyncAsync",
            "RunAutomaticSyncAsync",
            "_savedCredentials = null",
            "Saved Steam credentials available for cloud sync",
            "Saved Steam credentials unavailable for cloud sync",
            "ClearCredentials"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherCloudSaveState.Credentials.cs" `
        "prevents saved Steam credentials from constructing a gameplay save store" `
        @(
            "new\s+SaveManager",
            "CloudSaveStoreFactory",
            "SteamKit2CloudSaveStore"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSaveState.SaveManager.cs" `
        "constructs one fail-closed Android gameplay SaveManager from local storage" `
        @(
            "CreateAndroidGameplaySaveManager",
            "OperatingSystem\.IsAndroid",
            "PlatformNotSupportedException",
            "new SaveManager\(CloudSaveStoreFactory\.CreateLocalStore\(\)\)",
            "Created Android gameplay SaveManager with local storage only"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherCloudSaveState.SaveManager.cs" `
        "keeps gameplay SaveManager construction independent of Steam transfers" `
        @(
            "SavedSteamCredentials",
            "TryGetSavedCredentials",
            "CreateTransferCloudSaveStore",
            "SteamKit2CloudSaveStore",
            "DisposeActive"
        )

    Add-Check `
        "src\STS2Mobile\Steam\AndroidLocalSaveStore.FileIo.cs" `
        "commits synchronous and cancellable Android gameplay writes atomically" `
        @(
            "WriteBytesFile",
            "WriteBytesFileAsync",
            "CancellableAtomicFile\.WriteAllBytesAsync",
            "CancellationToken\.None",
            "overwrite: true"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Steam\AndroidLocalSaveStore.FileIo.cs" `
        "keeps Android gameplay writes free of Steam upload and queue behavior" `
        @(
            "SteamKit2CloudSaveStore",
            "CreateTransferCloudSaveStore",
            "CloudWriteQueue",
            "EnqueueUpload",
            "FlushActive"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSaveStoreFactory.cs" `
        "separates the local gameplay store from the launcher-only transfer store" `
        @(
            "CreateTransferCloudSaveStore",
            "CreateLocalStore",
            "new AndroidLocalSaveStore",
            "SteamKit2CloudSaveStore\.GetOrCreate"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Steam\CloudSaveStoreFactory.cs" `
        "removes ambiguous and disabled gameplay cloud-store factory paths" `
        @(
            "CreateCloudSaveStore",
            "CreateLocalOnlyCloudSaveStore",
            "DisabledCloudSaveStore"
        )

    Add-Check `
        "src\STS2Mobile\Patches\LauncherPatches.cs" `
        "replaces Android default SaveManager construction with the local-only manager" `
        @(
            "ApplySaveManagerPatches",
            "SaveManager",
            "ConstructDefault",
            "CreateAndroidGameplaySaveManager",
            "return false"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Patches\LauncherPatches.cs" `
        "prevents gameplay patches from starting or draining Steam operations" `
        @(
            "SyncCloudToLocal",
            "AutoSync",
            "Flush",
            "SteamKit2CloudSaveStore",
            "DisposeActive",
            "CloudSaveStore"
        )

    Add-Check `
        "src\STS2Mobile\Patches\AppLifecyclePatches.cs" `
        "lets Quit finish its local saves before restarting the launcher" `
        @(
            "Let NGame\.Quit perform its final local saves",
            '"Quit"',
            "postfix: PatchHelper\.Method\(typeof\(AppLifecyclePatches\), nameof\(QuitPostfix\)\)",
            "NGame\.Quit completed final local saves; restarting launcher",
            "AndroidGodotAppBridge\.RestartApp"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Patches\AppLifecyclePatches.cs" `
        "keeps temporary backgrounding and Quit free of save or Steam workarounds" `
        @(
            "QuitPrefix",
            "FlushCloud",
            "FlushActive",
            "CloudSyncCoordinator",
            "SteamKit2CloudSaveStore",
            "SaveManager"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.Plan.cs" `
        "closes the launcher manual-sync Steam store in one explicit finally path" `
        @(
            "RunManualSyncAsync",
            "finally",
            "SteamKit2CloudSaveStore\.DisposeActive",
            "Closing launcher manual-sync store"
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
        "captures selected-runtime save-origin evidence for Push eligibility" `
        @(
            "CloudPushSafetyContext",
            "LauncherPreferences\.ReadGameBranch\(\)",
            "SelectedBranch",
            "SelectedVersion",
            "CaptureEligibilityState",
            "LauncherSaveOriginEvidence\.CurrentLocalSavesMatchSelectedRuntime"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\CloudPushEligibilityState.cs" `
        "models local-save and selected-runtime evidence independently" `
        @(
            "HasImportantLocalSaveEvidence",
            "LocalSaveOriginMatchesSelectedRuntime",
            "HasBranchSwitchMarker",
            "BranchSwitchEvidenceValid"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\CloudPushEligibilityPolicy.cs" `
        "blocks Push whenever selected-runtime save-origin evidence is not verified" `
        @(
            "LocalSaveOriginMatchesSelectedRuntime",
            "LocalSaveOriginNotVerified",
            "Complete Pull from Steam Cloud against the installed"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Pull.cs" `
        "records Pull outcome and save-origin diagnostic evidence without a Push prerequisite" `
        @(
            "LauncherSaveOriginEvidence\.TryWriteManualPullOrigin",
            "WriteManualPullIncompleteMarker",
            "LastManualPullOutcome",
            "ManualPullOutcomePrefix"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Push.Write.cs" `
        "records save-origin evidence on successful manual Push" `
        @(
            "LauncherSaveOriginEvidence\.WriteManualPushOrigin",
            "WriteManualPushMarker"
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
        "validates branch-switch marker identity independently of Push" `
        @(
            "HasRequiredEvidence",
            "SelectedBranchMatches",
            "SteamGameBranch\.Normalize"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.Gates.cs" `
        "does not turn branch-switch history into a Push prerequisite" `
        @(
            "ManualPushPrerequisitesSatisfied",
            "HasManualPullAfterBranchSwitch",
            "CurrentLocalSavesMatchSelectedRuntime",
            "AppPaths\.HasStoragePermission"
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
