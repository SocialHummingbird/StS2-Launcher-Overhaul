function Add-SteamVersionSelectionWorkshopModChecks {
    Add-Check `
        "src\STS2Mobile\AppPaths.cs" `
        "keeps Workshop downloads and staged mods in app-private storage" `
        @(
            "WorkshopModsDirectoryName",
            "WorkshopDownloadsDir\(AppPrivateDataDir\)",
            "WorkshopStagedModsDir\(AppPrivateDataDir\)",
            "WorkshopManifestPath\(AppPrivateDataDir\)",
            "WorkshopClearMarkerPath\(AppPrivateDataDir\)",
            "EnsureWorkshopDirectories",
            "GetInternalFilesDirPath"
        )

    Add-Check `
        "src\STS2Mobile\Steam\SteamConnection.Workshop.cs" `
        "records Workshop subscription query attempts for device evidence" `
        @(
            "WorkshopSubscriptionQueries",
            "subscriptions",
            "subscribed",
            "LastWorkshopSubscriptionQueryType",
            "LastWorkshopSubscriptionQueryAttempts",
            "Steam GetUserFiles query="
        )

    Add-Check `
        "src\STS2Mobile\Steam\Workshop\SteamWorkshopDownloader.cs" `
        "guards Workshop downloads and zip extraction before staging" `
        @(
            "MaxWorkshopDownloadBytes",
            "DeclaredDownloadSize",
            "HttpCompletionOption\.ResponseHeadersRead",
            "CopyWithSizeLimitAsync",
            "DeleteFileQuietly\(tempFile\)",
            "ExtractZipSafely",
            "destination\.StartsWith\(root, StringComparison\.OrdinalIgnoreCase\)",
            "entry\.Length",
            "Workshop zip expands over",
            "source\.Kind",
            "source\.ExpectedSize",
            "Workshop item downloaded size mismatch",
            "CanReuseCachedDownload",
            "CachedDownloadStillPresent",
            "CachedDownloadHasFiles",
            "CleanupStaleDownloadArtifacts",
            "StaleDownloadFileNamePattern",
            "StaleDownloadDirectoryNamePattern",
            "RestoreOrDeleteDownloadBackup",
            "Restored stale Workshop download backup",
            "Workshop download temp file escapes downloads root",
            "Workshop download temp directory escapes downloads root",
            "Using cached Workshop download",
            "ReusedCachedDownload",
            "HContentFile",
            "depot-manifest",
            "no-download-source",
            "IsDependency",
            "RequiredByPublishedFileIds"
        )

    Add-Check `
        "src\STS2Mobile\Steam\Workshop\SteamWorkshopStager.cs" `
        "stages Workshop content only from the download root and removes inactive staged dirs" `
        @(
            "Path\.GetFullPath\(downloadsDirectory\)",
            "Path\.GetFullPath\(stagedDirectory\)",
            "IsPathInsideDirectory",
            "Workshop source directory escapes downloads root",
            "ClearStagedMods",
            "AppPrivateWorkshopClearMarkerPath",
            "Directory\.EnumerateFiles\(_stagedDirectory\)",
            "Workshop staged file escapes staged root",
            "DeleteFileQuietly",
            "removedStagedRootFileCount",
            "steamCloudPushPerformed=false",
            "ReplaceStagedDirectory",
            "Directory\.CreateDirectory\(targetDirectory\)",
            "RemoveInactiveStagedDirectories",
            "RemoveInactiveStagedRootFiles",
            "Removed inactive staged Workshop root file",
            "DirectoryContentSha256",
            "FileSha256",
            "File\.OpenRead",
            "ContentSha256",
            "SteamWorkshopSyncMetadata",
            "RequiredByPublishedFileIds"
        )

    Add-Check `
        "src\STS2Mobile\Steam\Workshop\SteamWorkshopSyncManifest.cs" `
        "persists subscription and dependency evidence in the Workshop manifest" `
        @(
            "SubscriptionQueryType",
            "SubscriptionQueryAttempts",
            "ClearedAtUtc",
            "ClearReason",
            "SubscribedItemCount",
            "DependencyItemCount",
            "MissingDependencyItemCount",
            "MissingDependencyIds",
            "TotalItemCount",
            "DownloadSourceKind",
            "ExpectedDownloadBytes",
            "HContentFile",
            "ReusedCachedDownload",
            "DownloadUrlPresent",
            "DownloadUrlHost",
            "IsDependency",
            "RequiredByPublishedFileIds"
        )

    Add-Check `
        "src\STS2Mobile\Steam\Workshop\SteamWorkshopSyncService.cs" `
        "resolves Workshop dependencies once and records broken dependency evidence" `
        @(
            "pendingIds",
            "MissingDependencyIds",
            "MissingDependencyItemCount",
            "child\.publishedfileid == detail\.publishedfileid",
            "DependencyParents",
            "previousItems",
            "DiscoveredDependencyItemCount"
        )

    Add-Check `
        "src\STS2Mobile\Patches\ModLoaderPatches.cs" `
        "loads launcher-selected Workshop and manual mod roots through the Android-safe scanner" `
        @(
            "Workshop sync stages into app-private storage",
            "AppPaths\.EnsureWorkshopDirectories",
            "LauncherModSelectionState\.KnownMods",
            "var selection = LauncherModSelectionState\.Load\(\)",
            "LauncherModSelectionState\.IsModdedModeFor\(selection\)",
            "var knownMods = LauncherModSelectionState\.KnownMods\(selection\)",
            "LauncherModSelectionState\.EnabledModCount\(knownMods\)",
            "LoadAndroidModRoots\(access, knownMods\)",
            "AndroidModRoots\(IReadOnlyList<LauncherKnownMod> knownMods\)",
            "access\.LoadModsInRoot\(root, dirAccess, knownMods\)",
            "IsSelectedForLaunch\(mod, knownMods\)",
            "LauncherModSelectionState\.IsPathEnabled\(typedMod\.path, knownMods\)",
            "knownMods: knownMods",
            "WriteModLaunchMarker\(",
            "selection: selection",
            "LauncherModSelectionState\.PushShouldBeLocked\(knownMods\)",
            "requiresWorkshopConsent: isWorkshop",
            "Selected root \{root\.Label\}",
            "FindAndroidManifestPath",
            "RebuildLoadedModsCache",
            "BuildActivationEvidence\(knownMods\)",
            "version = 2",
            "activationEvidence",
            "payloadReadyMods",
            "runtimePatchedMods",
            "partialCompatibilityMods",
            "compatibilitySubstituteMods",
            "inGameVerifiedMods"
        )

    Add-Check `
        "src\STS2Mobile\Patches\ModLoaderPatches.ActivationEvidence.cs" `
        "records truthful per-mod runtime activation and Android compatibility evidence" `
        @(
            "RuntimeModActivationSummary",
            "RuntimeLoadSucceeded",
            "PayloadReady",
            "HarmonyPatchTypeCount",
            "HarmonyTargetCount",
            "CompatibilityMode",
            "ActivationStatus",
            "InGameEffectVerified",
            "partial-android",
            "launcher-substitute",
            "runtime-patches-installed",
            "launcher-compatibility-substitute",
            "FindHarmonyTargetsForOwners",
            "RuntimePathIsWithinSelectedRoot",
            'normalizedSelectedRoot \+ "/"',
            "StringComparison\.OrdinalIgnoreCase"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncCoordinator.PushSafety.cs" `
        "blocks Steam Cloud Push when mods are selected for launch" `
        @(
            "CanPushWithWorkshopModSafety",
            "LauncherWorkshopModSafety\.ActiveSelectedModCount",
            "Manual Push blocked: \{selectedMods\} mod\(s\) are selected for launch",
            "mods are selected for launch",
            "protect unmodded cloud saves",
            "pushContext\.WriteBlockedMarker"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherWorkshopModSafety.cs" `
        "blocks Cloud Push from raw staged Workshop PCK files even when manifest state is stale" `
        @(
            "ActiveStagedModCount",
            "HasActiveStagedMods\(LauncherModSelectionDocument document\)",
            "LauncherModSelectionState\.PushShouldBeLocked\(document\)",
            "RawStagedPckCount",
            "Directory\.EnumerateFiles",
            "\*\.pck",
            "SearchOption\.AllDirectories",
            "Math\.Max\(manifestActiveCount, rawStagedPckCount\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportWorkshop.cs" `
        "reports Workshop sync manifest, staged hashes, and Cloud Push lock state" `
        @(
            "AppendWorkshopDiagnostics",
            "Workshop sync manifest path",
            "Workshop clear marker path",
            "Workshop cleared UTC",
            "Workshop subscription query type",
            "Workshop dependency item count",
            "Workshop missing dependency item count",
            "Workshop missing dependency ids",
            "Workshop active staged PCK mod count",
            "Workshop raw staged PCK file count",
            "Workshop modded-save Cloud Push locked",
            "LauncherModSelectionState\.KnownMods\(\)",
            "LauncherWorkshopModSafety\.HasActiveSelectedMods\(enabledMods\.Length\)",
            "ContentSha256",
            "PublishedFileId",
            "DownloadSourceKind",
            "ExpectedDownloadBytes",
            "HContentFile",
            "reusedCache",
            "downloadUrlPresent",
            "downloadUrlHost",
            "RequiredByPublishedFileIds",
            "StagedDirectory",
            "SourceDirectory"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Mods.cs" `
        "exposes a launcher Workshop sync action" `
        @(
            "BuildModsControls",
            "Sync Workshop Mods",
            "Clear Staged Mods",
            "WorkshopSyncPressed",
            "WorkshopClearPressed"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModSelectionState.cs" `
        "caches launcher known-mod snapshots and clears them when selector state changes" `
        @(
            "internal sealed class LauncherKnownModsSnapshot",
            "internal LauncherModSourceIdentity Identity \{ get; \}",
            "internal IReadOnlyList<LauncherKnownMod> Mods \{ get; \}",
            "KnownModsGate",
            "_knownModsCache",
            "KnownModsCacheEntry",
            "LauncherModSourceIdentity\.Create",
            "Identity\.Matches\(identity\)",
            "internal static IReadOnlyList<LauncherKnownMod> KnownMods\(\)",
            "internal static IReadOnlyList<LauncherKnownMod> KnownMods\(LauncherModSelectionDocument document\)",
            "PushShouldBeLocked\(LauncherModSelectionDocument document\)",
            "PushShouldBeLocked\(IReadOnlyList<LauncherKnownMod> knownMods\)",
            "EnabledModCount\(LauncherModSelectionDocument document\)",
            "EnabledModCount\(IReadOnlyList<LauncherKnownMod> knownMods\)",
            "IsPathEnabled\(string path, LauncherModSelectionDocument document\)",
            "IsPathEnabled\(string path, IReadOnlyList<LauncherKnownMod> knownMods\)",
            "internal static LauncherKnownModsSnapshot KnownModsSnapshot\(\)",
            "internal static LauncherKnownModsSnapshot KnownModsSnapshot\(LauncherModSelectionDocument document\)",
            "LauncherModSourceIdentity identity",
            "IsModdedModeFor\(LauncherModSelectionDocument document\)",
            "ClearKnownModsCache",
            "LauncherModLaunchReadinessCache\.Clear\(reason\)",
            "ClearKnownModsCache\(""mod selection changed""\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModLaunchReadiness.cs" `
        "reuses the known-mod source snapshot while preparing Start Game mod readiness" `
        @(
            "var document = LauncherModSelectionState\.Load\(\)",
            "LauncherModSelectionState\.IsModdedModeFor\(document\)",
            "var identity = LauncherModSourceIdentity\.Create\(\)",
            "LauncherModLaunchReadinessCache\.TryGet\(identity, phase, out var cached\)",
            "var snapshot = LauncherModSelectionState\.KnownModsSnapshot\(document, identity\)",
            "EvaluateFresh\(phase, snapshot\.Mods\)",
            "LauncherModLaunchReadinessCache\.Store\(snapshot\.Identity, readiness\)",
            "IReadOnlyList<LauncherKnownMod> knownMods"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Mods.cs" `
        "skips launcher mod source scans in vanilla mode and refreshes modded status from one selector snapshot" `
        @(
            "if \(!moddedMode\)",
            "RefreshModList\(System\.Array\.Empty<LauncherKnownMod>\(\)\)",
            "SetCompactWorkshopButtonText\(activeCount: 0\)",
            "var mods = LauncherModSelectionState\.KnownMods\(\)",
            "_readySummaryEnabledModCount = enabledCount",
            "RefreshModModeButtons\(moddedMode, enabledCount\)",
            "RefreshModList\(mods\)",
            "SetCompactWorkshopButtonText\(activeCount\)",
            "private void SetCompactWorkshopButtonText\(int activeCount\)",
            "IReadOnlyList<LauncherKnownMod> mods",
            "CompactSupportToolText\(""Play With Mods""",
            "\$""\{enabledCount\} enabled"""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.ReadySummary.cs" `
        "uses the launcher mod refresh snapshot for compact ready-summary mod count" `
        @(
            "var activeMods = _readySummaryEnabledModCount",
            "Mods \{activeMods\}",
            "Mods off"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Visibility.cs" `
        "refreshes launch controls before cloud summary repaint so compact ready text uses the latest mod snapshot" `
        @(
            "ShowLaunchButtons\(showUpdate\)",
            "SetCloudControlsVisible\(true\)",
            "_retryButton\.Visible = false"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.cs" `
        "disables Workshop support actions while Workshop operations run" `
        @(
            "SetWorkshopButtonsDisabled",
            "_workshopSyncButton\.Disabled = disabled",
            "_workshopClearButton\.Disabled = disabled"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Actions.cs" `
        "exposes Workshop busy-state control to the controller" `
        @(
            "SetWorkshopButtonsDisabled",
            "Actions\.SetWorkshopButtonsDisabled\(disabled\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherModel.Workshop.cs" `
        "routes Workshop sync and no-mods clear through guarded launcher operations" `
        @(
            "StartWorkshopSyncAsync",
            "ClearWorkshopMods",
            "DownloadRunGuard",
            "DepotConnectionAction\.WorkshopSync",
            "SteamWorkshopSyncService",
            "WorkshopSyncSummary",
            "Workshop mods need attention",
            "Workshop mods synced",
            "hasIssues",
            "RaiseWorkshopSyncCompleted\(summary\)",
            "MissingDependencyIds",
            "ClearKnownModsCache\(""workshop sync completed""\)",
            "ClearKnownModsCache\(""workshop staged mods cleared""\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherWorkshopCoordinator.cs" `
        "surfaces Workshop sync and clear result counts in launcher status" `
        @(
            "CompleteSync\(string summary\)",
            "var detail = string\.IsNullOrWhiteSpace\(summary\)",
            '_view\.SetStatus\(\$"\{detail\}',
            "SetWorkshopButtonsDisabled\(true\)",
            "SetWorkshopButtonsDisabled\(false\)",
            "Steam Cloud Push was not run",
            "Workshop mods cleared: removed \{removedCount\} staged entries",
            "Restart the game if it was already running"
        )

    Add-Check `
        "scripts\capture-workshop-mod-evidence.ps1" `
        "captures phase-labeled Workshop mod evidence without Steam Cloud Push" `
        @(
            "ValidateSet\(`"no-mods`", `"simple`", `"dependency`", `"broken`", `"public`", `"public-beta`", `"core-release`"\)",
            "readOnlySteamCloud",
            "Assert-DeviceUnlocked",
            "mScreenLocked",
            "mDreamingLockscreen",
            "StartGame",
            "startGameRequested",
            "input",
            "tap",
            "does not press Steam Cloud Push",
            "workshop_sync_manifest\.json",
            "last_workshop_mod_clear\.txt",
            "last_launcher_automation\.txt",
            "Download source/update provenance captured",
            "Raw signed Workshop download URL omitted",
            "workshop-derived-state\.json",
            "workshopCloudPushLocked",
            "Stale Workshop download temp artifacts absent",
            "workshop-hashes\.txt",
            "current_runtime_slot\.json",
            "current_runtime_cache\.txt",
            "last_runtime_patch_validation\.json",
            "runtime-hashes\.txt",
            "selected_runtime_pack_compatibility\.json",
            "selected_runtime_pack_patch_validation\.json",
            "Runtime selected PCK / active sts2.dll hashes captured",
            "staged-no-pck",
            "logcat-workshop-filtered\.txt",
            "Screenshot"
        )

    Add-Check `
        "scripts\review-workshop-mod-evidence.ps1" `
        "reviews required Workshop mod evidence phases" `
        @(
            "RequirePhase",
            "readOnlySteamCloud",
            "launchRequested",
            "Require-NoPattern",
            "NativeFallback",
            "FATAL EXCEPTION",
            "Require-WorkshopUsableSourceEvidence",
            "CAPTURE_FAILED\|run-as:\|Permission denied\|not debuggable",
            "has no stale Workshop download temp artifacts",
            "omits raw Workshop download URLs",
            "manifest is empty after clear",
            "no staged Workshop PCK remains after clear",
            "workshop-derived-state\.json",
            "workshopCloudPushLocked",
            "rawStagedPckCount",
            "manifestActivePckCount",
            "Require-WorkshopModLoaderScanEvidence",
            "Require-WorkshopRuntimeActivationEvidence",
            "activationEvidence",
            "RuntimeLoadSucceeded",
            "PayloadReady",
            "inGameVerifiedMods",
            "RequireCachedDownloadReuse",
            "Using cached Workshop download",
            "Scanning Workshop staged mods",
            "direct-url\|ugc-hcontent\|depot-manifest",
            "ManifestId",
            "DownloadUrlPresent",
            "DownloadUrlHost",
            "omits raw Workshop download URLs",
            "ExpectedDownloadBytes",
            "HContentFile",
            "ReusedCachedDownload",
            "steamCloudPushPerformed=false",
            "MissingDependencyItemCount",
            "staged-no-pck",
            "dependency manifest records discovered dependency count",
            "RequiredByPublishedFileIds",
            "runtime cache marker names selected PCK path",
            "Require-NonPublicRuntimeEvidence",
            "core-release",
            'game_versions\[/\\\\\]\$escapedPhase-',
            "runtime hashes include selected PCK",
            "runtime hashes include active sts2\.dll",
            "public runtime patch validation selected branch matches",
            "runtime patch validation selected branch matches",
            "public runtime validation selected PCK hash matches cache marker",
            "runtime validation selected PCK hash matches cache marker",
            "runtime validation active Android sts2\.dll hash matches cache marker",
            "runtime pack manifest ID matches runtime validation",
            "runtime pack source slot matches runtime validation slot",
            "runtime pack Android sts2\.dll hash matches runtime validation",
            "runtime pack validation report ID matches manifest",
            "public launch loaded public PCK path",
            "Workshop derived state locks Cloud Push",
            "Workshop derived state locks Cloud Push",
            "launch loaded selected non-public PCK path",
            "runtime patch validation passed",
            "selected runtime pack manifest is readable",
            "supportAssemblySha256",
            "selected runtime pack validation passed",
            "Publish cache active sts2\\.dll SHA256",
            "public-beta",
            "core-release",
            "RequireScreenshot"
        )

    Add-Check `
        "scripts\test-workshop-mod-evidence-reviewer.ps1" `
        "regression-tests Workshop evidence reviewer positive and negative cases" `
        @(
            "Invoke-ReviewShouldPass",
            "Invoke-ReviewShouldFail",
            "optional RequirePhase accepted",
            "optional cached download reuse accepted",
            "broken no-PCK phase accepted",
            "broken no-PCK cached download reuse accepted",
            "no-mods evidence with staged PCK",
            "capture failure marker in diagnostics",
            "staged simple mod without usable source provenance",
            "manifest containing raw Workshop download URL",
            "public-beta evidence without app launch request",
            "public-beta evidence containing NativeFallback/crash log",
            "core-release",
            "staged Workshop PCK without derived Cloud Push lock",
            "staged Workshop PCK without Workshop mod-loader scan log",
            "requested cached Workshop reuse without manifest/log evidence",
            "stale Workshop download temp artifact in evidence tree",
            "public-beta evidence with public selected PCK path",
            "public-beta evidence missing active sts2.dll hash",
            "public-beta runtime pack without support assembly hashes",
            "public-beta runtime pack validation failure",
            "public evidence with public-beta runtime validation branch",
            "public-beta evidence with public runtime validation branch",
            "public runtime validation selected PCK hash mismatch",
            "public-beta active Android sts2.dll hash mismatch",
            "public-beta runtime pack Android sts2.dll hash mismatch",
            "public-beta runtime pack ID mismatch",
            "public-beta runtime pack source slot mismatch",
            "public-beta runtime pack validation report ID mismatch",
            "public evidence with public-beta loaded PCK log",
            "public-beta evidence with public loaded PCK log",
            "current_runtime_slot\.json",
            "Selected PCK path:",
            "selected_runtime_pack_compatibility\.json",
            "Workshop mod evidence reviewer regression tests passed"
        )

    Add-Check `
        "docs\steam-version-selection-evidence-template.md" `
        "documents Workshop mod capture and review commands" `
        @(
            "capture-workshop-mod-evidence\.ps1 -Phase no-mods",
            "review-workshop-mod-evidence\.ps1 -EvidenceDir <no-mods-dir>",
            "capture-workshop-mod-evidence\.ps1 -Phase dependency",
            "capture-workshop-mod-evidence\.ps1 -Phase broken",
            "capture-workshop-mod-evidence\.ps1 -Phase public -Launch -StartGame",
            "capture-workshop-mod-evidence\.ps1 -Phase public-beta -Launch -StartGame",
            "capture-workshop-mod-evidence\.ps1 -Phase core-release -Launch -StartGame",
            "RequireCachedDownloadReuse",
            "staged-no-pck",
            "broken-no-pck-cached-reuse-dir",
            "Public/public-beta/core-release Workshop phase evidence includes",
            "Public-beta/core-release Workshop phase review rejects evidence",
            "Core-release branch launches",
            "selected runtime cache PCK path is under"
        )
}
