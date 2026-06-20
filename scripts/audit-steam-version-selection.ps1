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
    "src\STS2Mobile\Steam\SteamGameBranch.cs" `
    "declares current selector capabilities and unsupported beta features" `
    @(
        "Public\s*=\s*""public""",
        "Beta\s*=\s*""beta""",
        "SelectorMode\s*=\s*""Steam branch dropdown""",
        "SelectionKind",
        "SelectorHelpText",
        "SelectorInstallSlotHelpText",
        "Active install slot",
        "Choose a game version from the dropdown",
        "Private/password-protected branches may be inaccessible",
        "Failed downloads do not change Steam Cloud saves",
        "BetaPasswordEntrySupported\s*=\s*false",
        "BranchDiscoverySupported\s*=\s*true",
        "Account-visible branch options refresh after Steam app-info is available",
        "StorageIdentity",
        "StableBranchHash",
        "safePrefix",
        "TrimEnd",
        "StableBranchHash\(storageBranch\)"
    )

Add-Check `
    "src\STS2Mobile\Steam\SteamConnectionConfigurationFactory.cs" `
    "sanitizes SteamKit debug logs before writing launcher diagnostics" `
    @(
        "SanitizeSteamKitDebugMessage",
        "SteamKitDebugLogsSanitized\s*=\s*true",
        "SteamKitDebugLogsOptInEnabled",
        "STS2_STEAMKIT_DEBUG_LOGS",
        "disabled by default",
        "SensitiveJsonValueRegex",
        "SensitiveKeyValueRegex",
        "BearerTokenRegex",
        "<redacted>",
        "PatchHelper\.Log",
        "SteamKit"
    )

Add-Check `
    "src\STS2Mobile\Steam\DepotDownloader.UpdateCheck.cs" `
    "refreshes Steam branch catalog without starting a download" `
    @(
        "RefreshBranchCatalogAsync",
        "Refreshing Steam branch catalog",
        "GetMainAppDepotsAsync",
        "Steam branch catalog refreshed"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.BranchCatalog.cs" `
    "wires non-mutating refresh game versions action" `
    @(
        "RunBranchCatalogRefresh",
        "RefreshBranchCatalogAsync",
        "This does not download or modify game files",
        "CompleteBranchCatalogRefresh",
        "FailBranchCatalogRefresh",
        "SetRefreshGameVersionsBusy",
        "SelectedOptionStatus",
        "SelectedOptionDownloadProblem",
        "Selected version:",
        "RefreshGameBranchOptions"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchCatalog.cs" `
    "reads account-visible Steam branch catalog from app-info marker evidence" `
    @(
        "BranchAvailabilityMarkerPath",
        "Visible branch:",
        "ReadVisibleBranches",
        "BranchOption",
        "DropdownOptions",
        "DropdownOptionMetadata",
        "SelectedOptionStatus",
        "SelectedOptionCompactStatus",
        "SelectedOptionDownloadProblem",
        "DropdownLabelWithMetadata",
        "StatusText",
        "windowsManifestDepots",
        "passwordRequired",
        "buildId",
        "\(password\)",
        "\(unavailable\)",
        "\(ready\)",
        "Password branch blocked",
        "Ready in Steam catalog",
        "Refresh before download",
        "Steam app-info visible branch catalog",
        "GroupBy",
        "saved selection",
        "not listed in the latest Steam app-info catalog",
        "private, inaccessible, password-protected, or unavailable",
        "Download blocked: Steam marks this branch as password-protected",
        "password gate still blocks this launcher from downloading it",
        "Download blocked: this branch is visible to this account, but no Windows depot manifest was exposed",
        "Download blocked: this branch was not listed in Steam branch metadata",
        "Download blocked: selected saved branch was not listed",
        "Download blocked: selected branch is password-protected",
        "Download blocked: selected branch has no Windows depot manifest",
        "Steam app-info metadata has not been captured",
        "SteamGameBranch\.Public"
    )

Add-ForbiddenCheck `
    "src\STS2Mobile\Launcher\LauncherBranchCatalog.cs" `
    "does not inject hardcoded beta as a normal dropdown fallback" `
    @(
        "SteamGameBranch\.Beta,\s*source:\s*""fallback""",
        "AddIfMissing\(options,\s*new BranchOption\(SteamGameBranch\.Beta"
    )

Add-Check `
    "src\STS2Mobile\Steam\SteamGameInstallPaths.cs" `
    "keeps public and non-public install/download state separated" `
    @(
        "game_versions",
        "download_state",
        "steam_branch\.txt",
        "last_steam_branch_availability\.txt",
        "BranchAvailabilityMarkerPath",
        "VersionSlotDirectory",
        "VersionSlotKind",
        "public legacy",
        "side-by-side branch cache",
        "BranchMarkerPath"
    )

Add-Check `
    "src\STS2Mobile\ModEntry.cs" `
    "keeps managed startup branch cache routing aligned with downloader paths" `
    @(
        "GameVersionsDirectoryName",
        "StateDirectoryName",
        "StorageIdentity",
        "StableBranchHash",
        "safePrefix",
        "TrimEnd",
        "StableBranchHash\(branch\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPreferences.cs" `
    "persists selected Steam game branch" `
    @(
        "game_branch",
        "SteamGameBranch\.Normalize"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.cs" `
    "shows selector limitations before downloading a selected version" `
    @(
        "_branchHelpLabel",
        "REFRESH GAME VERSIONS",
        "RefreshGameVersionsRequested",
        "SelectedOptionStatus",
        "SelectedOptionCompactStatus",
        "UpdateBranchHelpText",
        "OptionButton",
        "_branchDropdown",
        "LauncherBranchCatalog\.DropdownOptions",
        "SteamGameBranch\.SelectorInstallSlotHelpText",
        "AutowrapMode",
        "MouseFilterEnum\.Ignore",
        "ItemSelected"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.cs" `
    "updates selector help text in ready/action state" `
    @(
        "_branchHelpLabel",
        "RefreshGameVersionsPressed",
        "SetRefreshVersionsButtonDisabled",
        "SelectedOptionStatus",
        "SelectedOptionCompactStatus",
        "UpdateBranchHelpText",
        "SteamGameBranch\.SelectorInstallSlotHelpText",
        "Version/download actions affect local game files only",
        "Steam Cloud saves move only through Pull/Push"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.cs" `
    "creates dropdown branch selector and wrapped selector help text in ready/action state" `
    @(
        "OptionButton",
        "ItemSelected",
        "REFRESH GAME VERSIONS",
        "PopulateBranchDropdown",
        "_branchHelpLabel",
        "AutowrapMode",
        "MouseFilterEnum\.Ignore",
        "HorizontalAlignment\.Left"
    )

Add-Check `
    "src\STS2Mobile\Steam\DepotDownloader.cs" `
    "uses branch-aware depot metadata and writes branch provenance" `
    @(
        "_branch",
        "WriteBranchMarker",
        "DepotManifestReference",
        "Depot manifest",
        "Install slot kind",
        "Install slot directory",
        "SteamGameInstallPaths\.VersionSlotKind",
        "SteamGameInstallPaths\.VersionSlotDirectory",
        "SteamGameInstallPaths\.BranchMarkerPath"
    )

Add-Check `
    "src\STS2Mobile\Steam\DepotDownloader.cs" `
    "writes beta branch integrity and public-inheritance marker evidence" `
    @(
        "Depot manifests matching public count",
        "Depot manifests differing from public count",
        "Depot manifests without public comparison count",
        "Depot manifests inherited from public count",
        "Depot manifests missing selected branch manifest count",
        "selectedBranchManifest=",
        "publicManifest=",
        "manifestSource=",
        "manifestRequestBranch=",
        "selectedMatchesPublic=",
        "effectiveMatchesPublic="
    )

Add-Check `
    "src\STS2Mobile\Steam\DepotDownloader.Depots.cs" `
    "uses explicit public inheritance for selected branches with missing depot manifests" `
    @(
        "public-inherited",
        "manifestRequestBranch = SteamGameBranch\.Public",
        "has no explicit branch manifest; inheriting public manifest",
        "source=\{manifestSource\}",
        "requestBranch='\{manifestRequestBranch\}'"
    )

Add-Check `
    "src\STS2Mobile\Steam\DepotDownloader.DepotDownload.cs" `
    "requests inherited public manifests against the branch that owns the manifest" `
    @(
        "ManifestRequestBranch",
        "source=\{depot\.ManifestSource\}",
        "requestBranch='\{depot\.ManifestRequestBranch\}'",
        "GetManifestRequestCodeAsync",
        "depot\.ManifestRequestBranch"
    )

Add-Check `
    "src\STS2Mobile\Steam\DepotDownloader.DepotManifestReference.cs" `
    "tracks selected, public, effective, and request-branch manifest provenance" `
    @(
        "SelectedBranchManifestId",
        "PublicManifestId",
        "ManifestSource",
        "ManifestRequestBranch",
        "HasSelectedBranchManifest",
        "EffectiveMatchesPublicManifest",
        "SelectedBranchManifestMatchesPublic",
        "InheritedFromPublic"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.Reports.cs" `
    "surfaces partial Steam branch and inherited-public depot evidence" `
    @(
        "Selected game branch marker depots matching public",
        "Selected game branch marker depots differing from public",
        "Selected game branch marker depots without public comparison",
        "Selected game branch marker depots inherited from public",
        "Selected game branch marker depots missing selected branch manifest",
        "Selected game branch marker partial Steam branch evidence",
        "Selected game branch marker depot manifest rows",
        "branchMarkerDepotsMatchingPublic",
        "branchMarkerDepotsDifferingFromPublic",
        "branchMarkerDepotsInheritedFromPublic",
        "branchMarkerDepotsMissingSelectedManifest",
        "BranchMarkerPartialSteamBranchEvidence",
        "ReadBranchMarkerValues",
        "selected branch inherits public depot manifests"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchAvailabilityStatus.cs" `
    "surfaces compact Steam branch availability diagnoses in launcher failure status" `
    @(
        "BranchAvailabilityMarkerPath",
        "CompactFailureMessage",
        "Clear",
        "MarkerBranchMatchesCurrentSelection",
        "LauncherPreferences\.ReadGameBranch",
        "Failed to clear Steam branch availability marker",
        "Branch availability:",
        "Selected branch visibility:",
        "Windows depot manifests for selected branch:",
        "Visible branch:",
        "MaxVisibleBranchesInStatus",
        "MarkerValuePasswordProtected",
        "passwordRequired=true",
        "password-protected",
        "Steam beta password entry is supported",
        "RemoveRawBranchAvailabilitySummary"
    )

Add-Check `
    "src\STS2Mobile\Steam\DepotDownloader.BranchAvailability.cs" `
    "surfaces account-visible Steam branch availability from app info" `
    @(
        "BranchAvailabilityReport",
        "WriteMarker",
        "BranchAvailabilityMarkerPath",
        "MaxBranchAvailabilityMarkerBranches",
        "MaxBranchAvailabilityMarkerValueLength",
        "SafeMarkerValue",
        "Visible branch overflow count",
        "Visible Steam branches",
        "Selected branch visibility",
        "Windows depot manifests for selected branch",
        "visible branches",
        "passwordRequired",
        "DownloadabilityText",
        "password-protected",
        "!PasswordRequired\.Equals\(""true""",
        "windowsManifestDepots",
        "metadataVisible",
        "DepotIsWindowsCompatible",
        "pwdrequired",
        "password_required"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherGameFiles.cs" `
    "blocks ambiguous non-public caches without marker provenance" `
    @(
        "BranchMarkerReady",
        "BranchIntegritySummary",
        "Selected branch appears partial",
        "inherits public content",
        "Selected branch depot manifests all match public",
        "BranchMarkerHasInstallSlotProvenance",
        "ReadinessProblem",
        "HasBranchMetadataProblem",
        "DeleteInactiveVersionCaches",
        "RedownloadMarkerFileName",
        "RedownloadMarkerUtcParseable",
        "RedownloadMarkerVersionSlotKind",
        "RedownloadMarkerVersionSlotDirectory",
        "last_game_version_redownload\.txt",
        "Selected version slot kind",
        "Selected version slot directory",
        "Deleted game directory",
        "Game directory existed before delete",
        "Game directory exists after delete",
        "Deleted download state directory",
        "Download state directory existed before delete",
        "Download state directory exists after delete",
        "RedownloadMarkerSelectedDirectoriesCleared",
        "CacheCleanupMarkerFileName",
        "CacheCleanupMarkerPath",
        "CacheCleanupMarkerUtc",
        "CacheCleanupMarkerUtcParseable",
        "CacheCleanupMarkerSelectedBranch",
        "CacheCleanupMarkerSelectedVersion",
        "CacheCleanupMarkerVersionSlotKind",
        "CacheCleanupMarkerVersionSlotDirectory",
        "CacheCleanupMarkerGameVersionsDirectoryPresent",
        "CacheCleanupMarkerRuntimePacksDirectoryPresent",
        "CacheCleanupMarkerSelectedRuntimePackDirectory",
        "CacheCleanupMarkerSelectedRuntimePackPresentBeforeCleanup",
        "CacheCleanupMarkerRemovedCount",
        "CacheCleanupMarkerRemovedRuntimePackCount",
        "CacheCleanupMarkerSelectedCachePreservedWhereApplicable",
        "CacheCleanupMarkerSelectedRuntimePackPreservedWhereApplicable",
        "last_game_version_cache_cleanup\.txt",
        "Preserved selected cache",
        "Preserved selected runtime pack",
        "Removed count",
        "Removed runtime pack count",
        "Removing inactive game version cache",
        "Preserving selected game version cache",
        "selected branch",
        "Depot manifest",
        "Install slot kind:",
        "Install slot directory:"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.Downloads.cs" `
    "reports selected-version preservation when clearing inactive caches" `
    @(
        "SelectedOptionDownloadProblem",
        "blocked",
        "BlockedRedownloadConfirmationMessage",
        "ApplyRedownloadBlockedByBranchProblem",
        "replacement download remains blocked",
        "ClearCachedVersionsPressed",
        "ClearCachedVersions",
        "DeleteInactiveVersionCaches",
        "Selected version preserved",
        "runtime pack cache\(s\)",
        "SteamGameBranch\.DisplayName"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.Downloads.cs" `
    "logs selected-branch integrity summary after non-public downloads" `
    @(
        "BranchIntegritySummary",
        "integritySummary",
        "AppendLog"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Dialog.cs" `
    "accepts contextual confirmation button labels" `
    @(
        "confirmText",
        "cancelText",
        "DialogButtonText",
        "BuildDialogButtons"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Dialog.cs" `
    "keeps compact confirmation warnings scroll-safe with reachable buttons" `
    @(
        "CompactDialogWidthRatio = 0\.9f",
        "CompactDialogMaxMessageHeightRatio = 0\.44f",
        "CompactDialogMessageMinScrollHeight = 96",
        "BuildDialogMessageArea",
        "ShouldScrollDialogMessage",
        "DialogMessageScrollHeight",
        "new ScrollContainer",
        "DialogMessageWidth\(profile\)",
        "profile\.ViewportSize\.Y \* CompactDialogMaxMessageHeightRatio",
        "button\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Behavior.cs" `
    "exposes contextual confirmation button label overloads" `
    @(
        "confirmText",
        "cancelText",
        "BuildConfirmationDialog"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Behavior.cs" `
    "sizes confirmation dialogs from the current visible viewport" `
    @(
        "CurrentConfirmationProfile",
        "GetVisibleRect\(\)\.Size",
        "LauncherLayoutProfile\.ForViewport\(viewportSize\)",
        "BuildConfirmationDialog\(message,\s*CurrentConfirmationProfile\(\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.Downloads.cs" `
    "labels redownload and cache confirmations with explicit compact actions" `
    @(
        "REDOWNLOAD VERSION",
        "KEEP FILES",
        "DELETE CACHE",
        "CLEAR CACHE",
        "KEEP CACHE"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.Startup.cs" `
    "labels branch-switch confirmation with explicit compact actions" `
    @(
        "SWITCH VERSION",
        "KEEP CURRENT"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.CloudSync.cs" `
    "labels final cloud confirmation with explicit compact actions" `
    @(
        "ConfirmText",
        "CancelText",
        "PUSH TO CLOUD",
        "CANCEL PUSH"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.UpdateChecks.cs" `
    "blocks selected-version update checks for known unavailable branches while preserving app update checks" `
    @(
        "CheckForAppUpdatesAsync",
        "SelectedOptionDownloadProblem",
        "Update check blocked:",
        "CHECK BLOCKED",
        "Update check blocked for selected game version",
        "LauncherBranchCatalog\.ReadVisibleBranches",
        "_model\.CheckForUpdatesAsync",
        "await appUpdateTask"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherGameVersionCache.cs" `
    "enumerates side-by-side cached non-public versions" `
    @(
        "CachedVersion",
        "LauncherStorageNames\.GameVersionsDirectory",
        "Selected"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.cs" `
    "records branch-switch safety posture for later Push gating" `
    @(
        "last_game_branch_switch\.txt",
        "internal const string MarkerFileName",
        "MarkerUtc",
        "MarkerUtcParseable",
        "PreviousBranch",
        "SelectedBranch",
        "SelectedBranchSelectionKind",
        "SelectorMode",
        "SelectedVersion",
        "SelectedVersionSlotKind",
        "SelectedVersionSlotDirectory",
        "SelectedBranchNote",
        "LocalBackupForced",
        "ManualPushRequiresBackupStorage",
        "WarningAcknowledged",
        "NonPublicBranchWarningAcknowledged",
        "HasRequiredEvidence",
        "SelectedBranchMatches",
        "ManualPushPrerequisitesSatisfied",
        "Local backup forced on:",
        "Manual Push requires backup storage:",
        "Warning acknowledged:",
        "Selected branch selection kind:",
        "Steam branch selector mode:",
        "Selected branch note",
        "Selected version:",
        "Selected version slot kind:",
        "Selected version slot directory:",
        "SelectorHelpText",
        "Local backup forced on",
        "Manual Push requires backup storage"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.CloudSync.cs" `
    "guards manual Push after branch switches until backup storage is available" `
    @(
        "CloudPushPressed",
        "CanPushWithBaselineEvidence",
        "BranchSwitchSafety",
        "Selected version slot:",
        "Pull-after-switch for",
        "Android local save evidence",
        "Pull from Cloud must complete for the selected game version before Push",
        "selected game version \{selectedVersion\}",
        "no Android local save evidence exists before Push",
        "LastManualPullCompletionRecorded",
        "LastManualPullMatchesSelectedBranch",
        "LauncherBranchSwitchSafety\.HasRequiredEvidence",
        "branch switch marker is missing required safety evidence",
        "does not match the selected game version",
        "no current Pull-after-switch evidence exists",
        "no Android local save evidence exists",
        "backup storage permission is unavailable",
        "LauncherCloudSyncEvidence\.HasManualPullAfterBranchSwitch",
        "ManualCloudSyncRequest\.Pull\(",
        "LauncherPreferences\.ReadGameBranch\(\)",
        "Pull from Cloud must complete after this game-version switch before Push",
        "HasManualPullAfterBranchSwitch",
        "WriteManualPushMarker",
        "WriteManualPushBlockedMarker",
        "A game version switch was recorded",
        "cross-version/destructive",
        "LauncherLocalSaveEvidence\.HasImportantSaveEvidence",
        "no Android local save files were found",
        "local pre-Push backup",
        "cloud pre-Push backup",
        "backup storage",
        "Push"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.cs" `
    "records successful manual Pull evidence for branch-switch Push safety" `
    @(
        "last_manual_cloud_pull\.txt",
        "LastManualPushMarkerFileName",
        "last_manual_cloud_push_blocked\.txt",
        "HasManualPullAfterBranchSwitch",
        "LastManualPullUtc",
        "LastManualPullUtcParseable",
        "LastManualPullSelectedBranch",
        "LastManualPullSelectedBranchSelectionKind",
        "LastManualPullSelectorMode",
        "LastManualPullSelectedVersion",
        "LastManualPullSelectedVersionSlotKind",
        "LastManualPullSelectedVersionSlotDirectory",
        "LastManualPullCompletionRecorded",
        "LastManualPullBeforePushCompletionRecorded",
        "BaselineManualPushPrerequisitesSatisfied",
        "Manual Pull completed before Push",
        "LastManualPullIsAfterBranchSwitch",
        "LastManualPullMatchesSelectedBranch",
        "LastManualPushUtc",
        "LastManualPushUtcParseable",
        "LatestManualPushEvidenceOutcome",
        "LatestManualPushEvidenceUtc",
        "LatestManualPushEvidenceSelectedBranch",
        "LatestManualPushEvidenceSelectedBranchSelectionKind",
        "LatestManualPushEvidenceSelectorMode",
        "LatestManualPushEvidenceSelectedVersion",
        "LatestManualPushEvidenceSelectedVersionSlotKind",
        "LatestManualPushEvidenceSelectedVersionSlotDirectory",
        "LatestManualPushEvidenceReason",
        "blocked-before-upload",
        "LastManualPushSelectedBranch",
        "LastManualPushSelectedBranchSelectionKind",
        "LastManualPushSelectorMode",
        "LastManualPushSelectedVersion",
        "LastManualPushSelectedVersionSlotKind",
        "LastManualPushSelectedVersionSlotDirectory",
        "LastManualPushRecordedLocalBackupCount",
        "LastManualPushRecordedCloudBackupCount",
        "LastManualPushRecordedLatestLocalBackupUtc",
        "LastManualPushRecordedLatestCloudBackupUtc",
        "LastManualPushRecordedImportantLocalSaveEvidenceCount",
        "LastManualPushRecordedBaselinePrerequisitesSatisfied",
        "LastManualPushCompletionRecorded",
        "LastManualPushIsAfterBranchSwitch",
        "LastManualPushMatchesSelectedBranch",
        "LastManualPushPrePushBackupEvidenceSatisfied",
        "HasManualPushAfterBranchSwitch",
        'LatestManualPushEvidenceOutcome\(dataDir\), "completed"',
        "LastManualPushBlockedUtc",
        "LastManualPushBlockedUtcParseable",
        "LastManualPushBlockedSelectedBranch",
        "LastManualPushBlockedSelectedBranchSelectionKind",
        "LastManualPushBlockedSelectorMode",
        "LastManualPushBlockedSelectedVersion",
        "LastManualPushBlockedSelectedVersionSlotKind",
        "LastManualPushBlockedSelectedVersionSlotDirectory",
        "LastManualPushBlockedRecordedPrerequisitesSatisfied",
        "LastManualPushBlockedRecordedLocalBackupCount",
        "LastManualPushBlockedRecordedCloudBackupCount",
        "LastManualPushBlockedRecordedLatestLocalBackupUtc",
        "LastManualPushBlockedRecordedLatestCloudBackupUtc",
        "LastManualPushBlockedRecordedImportantLocalSaveEvidenceCount",
        "LastManualPushBlockedRecordedBaselinePrerequisitesSatisfied",
        "LastManualPushBlockedRecordedPrePushBackupEvidenceSatisfied",
        "LastManualPushBlockedReason",
        "LastManualPushBlockedBeforeUpload",
        "LastManualPushBlockedMatchesSelectedBranch",
        "HasCompletionFlag",
        "WriteManualPullMarker",
        "WriteManualPushMarker",
        "WriteManualPushBlockedMarker",
        "WriteManualPushBlockedMarker\(string dataDir, string selectedBranch, string reason\)",
        "Manual Push blocked before upload",
        'StartsWith\("Manual Push blocked:',
        "Pre-Push local backup evidence count:",
        "Pre-Push cloud backup evidence count:",
        "Latest pre-Push local backup UTC:",
        "Latest pre-Push cloud backup UTC:",
        "Important Android local save evidence count:",
        "Baseline manual Push prerequisites satisfied:",
        'string\? ReadSelectedBranch',
        "return false",
        "Manual Pull completed before branch-switch Push",
        "Manual Pull completed before Push",
        "Manual Push completed after branch-switch safety gates",
        "Selected version:",
        "Selected branch note"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherLocalSaveEvidence.cs" `
    "detects local save evidence before branch-switch Push" `
    @(
        "HasImportantSaveEvidence",
        "CountImportantSaveEvidence",
        "\.save",
        "\.run",
        "prefs",
        "IgnoredDirectoryNames",
        "MaxDirectoriesToInspect",
        "SafeFiles",
        "SafeDirectories"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBackupEvidence.cs" `
    "reports local/cloud pre-Push backup evidence after branch switching" `
    @(
        "local-pre-push",
        "cloud-pre-push",
        "MaxBackupFilesToInspect",
        "LocalPrePushBackupCount",
        "CloudPrePushBackupCount",
        "LatestLocalPrePushBackupUtc",
        "LatestCloudPrePushBackupUtc",
        "HasLocalPrePushBackupAfterBranchSwitch",
        "HasCloudPrePushBackupAfterBranchSwitch",
        "HasPrePushBackupEvidenceAfterBranchSwitch",
        "LauncherBranchSwitchSafety\.MarkerUtc"
    )

Add-Check `
    "src\STS2Mobile\Steam\CloudSyncCoordinator.SaveBackups.Manual.cs" `
    "fails manual Push before upload when required backup evidence is missing" `
    @(
        "EnforceManualPushBackupEvidence",
        "Manual Push blocked: local backup is enabled but backup storage permission is unavailable",
        "Manual Push blocked: local pre-Push backup evidence is incomplete",
        "Manual Push blocked: cloud pre-Push backup evidence is incomplete",
        "CloudImportantSaveCount",
        "importantPaths.Count",
        "localBackups < importantPaths.Count",
        "cloudBackups < cloudImportantSaveCount",
        "AppPaths\.HasStoragePermission"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.Startup.cs" `
    "uses centralized selector guidance in branch-switch confirmation" `
    @(
        "BranchSwitchConfirmationMessage",
        "SteamGameBranch\.SelectorInstallSlotHelpText",
        "LauncherBranchCatalog\.ReadVisibleBranches",
        "SelectedOptionStatus",
        "SelectedOptionDownloadProblem",
        "AppendLog\(STS2Mobile\.Steam\.SteamGameBranch\.SelectorInstallSlotHelpText",
        "SelectedVersionReadyStatus",
        "SelectedVersionDownloadRequiredStatus",
        "SteamGameInstallPaths\.VersionSlotKind",
        "Active install slot",
        "Steam Cloud Push will require backup storage permission"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherCredentialEntrySupport.cs" `
    "declares native Android credential panel support without app-owned password storage" `
    @(
        "AppStoresSteamPassword\s*=\s*false",
        "NativeCredentialHandoffPopupSupported\s*=\s*false",
        "NativeIntegratedCredentialPanelSupported\s*=\s*true",
        "NativeCredentialFieldsAutofillHintsConfigured\s*=\s*true",
        "SteamCredentialWebDomainConfigured\s*=\s*true",
        "NativeCredentialPanelInlineStatusConfigured\s*=\s*true",
        "NativeCredentialPanelKeyboardSafeLayoutConfigured\s*=\s*true",
        "NativeCredentialPanelImeInsetScrollSupported\s*=\s*true",
        "NativeCredentialPanelTouchTargetLayoutConfigured\s*=\s*true",
        "NativeCredentialPanelLargeFieldTargetsSupported\s*=\s*true",
        "NativeCredentialPanelRequestsBothAutofillFields\s*=\s*true",
        "NativeCredentialPanelFocusAutofillRequestsSupported\s*=\s*true",
        "NativeCredentialPanelTaskLedButtonsSupported\s*=\s*true",
        "NativeCredentialPanelResponsiveActionRowsSupported\s*=\s*true",
        "NativeCredentialPanelOrientationReflowSupported\s*=\s*true",
        "NativeCredentialPanelShortHeightCopySupported\s*=\s*true",
        "NativeCredentialPanelShortHeightReflowSupported\s*=\s*true",
        "NativeCredentialPanelImeHeightReflowSupported\s*=\s*true",
        "NativeCredentialPanelPasswordVisibilityToggleSupported\s*=\s*true",
        "NativeCredentialPanelPasswordFocusButtonSupported\s*=\s*true",
        "NativeCredentialPanelBackDismissSupported\s*=\s*true",
        "NativeCredentialPanelDismissRetrySupported\s*=\s*true",
        "NativeCredentialPanelDismissHidesKeyboardSupported\s*=\s*true",
        "NativeCredentialPanelSuppressesPreAuthSavePrompt\s*=\s*true",
        "SteamGuardOneShotCodeGuidanceSupported\s*=\s*true",
        "SteamGuardAlphanumericKeyboardSupported\s*=\s*true",
        "FailedLoginRetryGuidanceSupported\s*=\s*true",
        "ContextSpecificLoginRecoveryGuidanceSupported\s*=\s*true",
        "GodotFieldCredentialMetadataConfigured\s*=\s*true",
        "AndroidKeyboardCredentialHintsConfigured\s*=\s*true",
        "GodotFieldsAreNativeAndroidAutofillTargets\s*=\s*false",
        "PasswordManagerSuggestionsDeviceValidated\s*=\s*false",
        "NativeCredentialHandoffResultTtlSeconds",
        "NativeCredentialHandoffResultTtlSeconds\s*=\s*60",
        "Integrated native Android credential panel",
        "must not store or inject Steam passwords",
        "real username/password EditText fields",
        "Steam web-domain metadata",
        "inline status/error guidance",
        "explicit large styled credential-field touch targets",
        "keyboard-safe scrollable top-weighted layout with IME inset padding and focus scrolling",
        "branded task-led controls that stay full-width on portrait phones and switch to responsive credential/action rows on wide landscape Android viewports",
        "short-height landscape copy compression so credential fields and primary actions stay higher on phone screens",
        "short-height copy reflow when the landscape height class changes",
        "IME-visible height reflow when the keyboard reduces usable landscape height",
        "orientation and screen-size changes rebuild the native credential panel when the width class changes while clearing stale view fields",
        "manual password visibility toggle that resets to hidden",
        "one-shot Steam Guard code guidance with alphanumeric keyboard entry and uppercase/separator normalization",
        "context-specific failed-login and connection-recovery guidance",
        "keyboard/focus cleanup on native panel dismiss",
        "old user-facing native credential popup is disabled",
        "provider behavior is device/provider dependent",
        "Native username/password fields",
        "cleared after submit/cancel/expiry",
        "credential_hint",
        "credential_storage_owner",
        "android_credential_provider"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\LoginSection.cs" `
    "clears captured Steam password from Godot login UI before authentication handoff" `
    @(
        "var password = _passwordField\.Text",
        "_passwordField\.Text = """"",
        "LoginRequested\?\.Invoke\(username, password\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\LoginSection.cs" `
    "uses integrated native Steam login panel instead of a separate credential popup on Android" `
    @(
        "ConfigureUsernameField",
        "ConfigurePasswordField",
        "VirtualKeyboardType\.EmailAddress",
        "VirtualKeyboardType\.Password",
        "SIGN IN WITH STEAM",
        "credentialHelpLabel",
        "MoveChild\(_nativeLoginButton, credentialHelpLabel\.GetIndex\(\)\)",
        "ShowSteamLoginCredentialPanel",
        "TryConsumeSteamLoginCredentialResult",
        "IsSteamLoginCredentialPanelVisible",
        "StopNativeCredentialPolling\(hidePanel: false\)",
        "integrated Steam login panel",
        "does not store your Steam password",
        "ClearPassword",
        "LoginRequested"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\LoginSection.cs" `
    "promotes compact Android Steam sign-in to a large primary CTA with a readable two-line helper" `
    @(
        "CompactNativeLoginButtonHeight = LauncherSectionMetrics\.CodeInputHeight",
        "CompactNativeLoginText",
        "SetCompactNativeLoginButtonText",
        "SIGN IN WITH STEAM",
        "Android login",
        "LauncherSectionMetrics\.CompactCredentialHelpHeight",
        "compactNativeLogin",
        "LauncherSectionMetrics\.PrimaryButtonFontSize",
        "TextServer\.AutowrapMode\.WordSmart",
        "ClipText = false",
        "VerticalAlignment\.Center",
        "ScaleInt\(LauncherSectionMetrics\.CompactCredentialHelpHeight, scale\)",
        "Password manager can appear\.",
        "Steam password is not stored\."
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\LauncherSectionSetup.cs" `
    "frames launcher states with explicit titled portal sections and readable compact task cues" `
    @(
        "ConfigureHiddenSection",
        "BuildSectionHeader",
        "BuildCompactSectionHeader",
        "bool compact",
        "!compact",
        "title",
        "subtitle",
        "accent",
        "CompactSectionHeaderMinHeight = 42",
        "CompactSectionHeaderCueFontSize = 12",
        "CompactSectionHeaderTitleFontSize = 14",
        "CompactSectionHeaderTitleMinWidth = 106",
        "CompactSectionHeaderAccentWidth = 3",
        "LauncherSectionMetrics\.CompactSectionSeparation",
        "new HBoxContainer",
        "new VBoxContainer",
        "BuildCompactSectionHeader\(title, subtitle, scale, accent\)",
        "BuildHeaderStyle\(scale, compact\)",
        "compact \? 2 : 4",
        "compact \? 2 : 3",
        "compact \? 15 : 13",
        "compact \? 6 : 8",
        "compact \? 4 : 8",
        "compact \? 4 : 9",
        "StyledLabel",
        "TextServer\.AutowrapMode\.WordSmart",
        "cueLabel\.AutowrapMode = TextServer\.AutowrapMode\.Off",
        "cueLabel\.ClipText = true",
        "cueLabel\.TooltipText = subtitle",
        "titleLabel\.SizeFlagsHorizontal = Control\.SizeFlags\.ShrinkBegin",
        "TextServer\.OverrunBehavior\.TrimEllipsis",
        "SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill",
        "TextSecondary",
        "SetBorderWidthAll"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.cs" `
    "wraps launcher status in a readable portal status capsule" `
    @(
        "BuildStatusCapsule",
        "BuildStatusStyle",
        "BuildFirstRunGuide",
        "BuildCollapsedFirstRunGuide",
        "BuildFirstRunGuidePanel",
        "CompactSafeFlowGuideText",
        "CompactSafeFlowGuideTextHeight",
        "BuildFirstRunGuideStyle\(scale, compact\)",
        "Setup: sign in -> download version -> Pull saves",
        "Push stays locked until verified",
        "SetCompactSafeFlowToggleText",
        "EnsureCompactSafeFlowToggleLabels",
        "CompactSafeFlowToggleBodyName",
        "CompactSafeFlowToggleTitleName",
        "CompactSafeFlowToggleDetailName",
        "CompactSafeFlowToggleDetailFontSize = LauncherSectionMetrics\.CompactDetailLabelFontSize",
        'toggle\.Text = ""',
        "body\.SetAnchorsPreset\(Control\.LayoutPreset\.FullRect\)",
        "TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
        "guidance\.CustomMinimumSize = new Vector2",
        "LauncherComponentTheme\.TextSecondary",
        "`"SAFE FLOW`"",
        "`"Pull then play`"",
        "`"HIDE SAFE FLOW`"",
        "`"Pull-first guard`"",
        "LauncherSectionMetrics\.CompactDrawerToggleHeight",
        "LauncherSectionMetrics\.CompactDetailButtonFontSize",
        "profile\.Compact",
        "CompactPrimaryColumnSeparation",
        "Setup: sign in -> download version -> Pull saves",
        "Safe first-run flow",
        "Push stays hidden until local saves are verified",
        "Initializing\.\.\.",
        "Status",
        "CyanAccent",
        "TextSecondary",
        "SetBorderWidthAll"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.LogColumn.cs" `
    "keeps diagnostics hidden behind a clearly labeled console drawer" `
    @(
        "SHOW DIAGNOSTICS CONSOLE",
        "HIDE DIAGNOSTICS CONSOLE",
        "Diagnostics Console",
        "Hidden by default",
        "Export sanitized diagnostics",
        "drawer\.Visible = false",
        "DiagnosticsToggleText",
        "SetDiagnosticsToggleText",
        "EnsureCompactDiagnosticsToggleLabels",
        "CompactDiagnosticsToggleBodyName",
        "CompactDiagnosticsToggleTitleName",
        "CompactDiagnosticsToggleDetailName",
        "CompactDiagnosticsToggleDetailFontSize = LauncherSectionMetrics\.CompactDetailLabelFontSize",
        'toggle\.Text = ""',
        "body\.SetAnchorsPreset\(Control\.LayoutPreset\.FullRect\)",
        "TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
        "LauncherComponentTheme\.TextSecondary",
        "LauncherSectionMetrics\.CompactDetailButtonFontSize",
        "LauncherSectionMetrics\.CompactDrawerToggleHeight",
        "`"Log \+ export context`"",
        "`"Keep log closed`"",
        "return \(log, drawer, toggle\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.cs" `
    "hosts compact diagnostics inside the primary scroll body instead of fixed root chrome" `
    @(
        "VBoxContainer CompactDiagnosticsHost",
        "compactDiagnosticsHost = new VBoxContainer",
        "compactDiagnosticsHost\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill",
        "left\.AddChild\(compactDiagnosticsHost\)",
        "compactDiagnosticsHost"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.LogColumn.cs" `
    "bounds the compact diagnostics log viewport from the current launcher profile" `
    @(
        "CompactDiagnosticsLogViewportHeightRatio = 0\.28f",
        "CompactDiagnosticsLogMinHeight = 220",
        "CompactDiagnosticsLogMaxHeight = 340",
        "DiagnosticsLogHeight\(LauncherLayoutProfile profile\)",
        "profile\.ViewportSize\.Y \* CompactDiagnosticsLogViewportHeightRatio",
        "Math\.Clamp\(viewportHeight, minHeight, maxHeight\)",
        "BuildLogView\(profile\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Behavior.cs" `
    "refreshes diagnostics log viewport height after Android viewport changes" `
    @(
        "UpdateViewportSize\(Vector2 viewportSize\)",
        "_panel\.UpdateSizeFromViewport\(viewportSize, _profile\.PanelHeightRatio\)",
        "UpdateDiagnosticsLogViewport\(viewportSize\)",
        "UpdateKeyboardOffset\(\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.LogColumn.cs" `
    "keeps open compact diagnostics readable after viewport resize" `
    @(
        "UpdateDiagnosticsLogViewport\(Vector2 viewportSize\)",
        "LauncherLayoutProfile\.ForViewport\(viewportSize\)",
        "Log\.CustomMinimumSize = new Vector2\(0, DiagnosticsLogHeight\(profile\)\)",
        "_profile\.Compact && DiagnosticsDrawer\.Visible",
        "ScrollCompactPrimaryTo\(DiagnosticsDrawer\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Log.cs" `
    "renders compact diagnostics logs with readable Android text and padding" `
    @(
        "CompactDiagnosticsLogFontSize = 15",
        "CompactDiagnosticsLogMarginHorizontal = 12",
        "CompactDiagnosticsLogMarginVertical = 10",
        "BuildLogView\(LauncherLayoutProfile profile\)",
        "var compact = profile\.Compact",
        "compact \? CompactDiagnosticsLogFontSize : LauncherComponentTheme\.LogFontSize",
        "BuildLogStyle\(scale, compact\)",
        "compact\s*\?\s*CompactDiagnosticsLogMarginHorizontal",
        "compact\s*\?\s*CompactDiagnosticsLogMarginVertical",
        "SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.cs" `
    "can reveal the hidden diagnostics console after explicit diagnostics actions" `
    @(
        "DiagnosticsDrawer",
        "DiagnosticsToggle",
        "ShowDiagnosticsConsole",
        "var diagnosticsRoot = profile\.Compact",
        "primary\.CompactDiagnosticsHost",
        "DiagnosticsDrawer\.Visible = true",
        "SetDiagnosticsToggleText\(DiagnosticsToggle, _profile, visible: true\)",
        "ScrollCompactPrimaryTo\(DiagnosticsDrawer\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.Diagnostics.cs" `
    "opens diagnostics console when error summary or raw-log actions write output" `
    @(
        "ShowDiagnosticsSummary",
        "CopyRawLogToClipboard",
        "view\.ShowDiagnosticsConsole\(\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.Diagnostics.Export.cs" `
    "opens diagnostics console after manual diagnostics export writes output" `
    @(
        "ShowDiagnosticsExportResult",
        "_view\.ShowDiagnosticsConsole\(\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.cs" `
    "presents launcher as a polished Steam/version/cloud portal instead of a generic debug shell" `
    @(
        "StS2 Mobile",
        "STEAM \| CLOUD \| PLAY",
        "STEAM LOGIN\s*\|\s*VERSION SLOTS\s*\|\s*CLOUD SAVES",
        "CompactBrandTitleFontSize = 18",
        "CompactBrandSubtitleFontSize = 12",
        "CompactBrandMarkHeight = 26",
        "CompactBrandRowSeparation = 6",
        "CompactBrandHeaderSeparation = 2",
        "BuildCompactBrandHeader",
        "BuildBrandMark",
        "profile\.Compact",
        "if \(profile\.Compact\)",
        "return BuildCompactBrandHeader\(profile\)",
        "fontSize: CompactBrandTitleFontSize",
        "fontSize: CompactBrandSubtitleFontSize",
        "title\.ClipText = true",
        "subtitle\.ClipText = true",
        "CompactRootColumnSeparation",
        "compact \? CompactBrandMarkHeight : 50",
        "compact \? 12 : 16",
        "OrangeAccent",
        "CyanAccent"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherLayoutProfile.cs" `
    "uses the full available Android viewport for compact launcher layouts" `
    @(
        "mobileShell = OperatingSystem\.IsAndroid\(\)",
        "compact = mobileShell",
        "AndroidCompactTouchScaleFloor = 1\.06f",
        "mobileShell \? AndroidCompactTouchScaleFloor : CompactScaleFloor",
        "CompactStackedActionRowsWidth = 560f",
        "CompactStackedActionRows",
        "contentMaxWidth < MathF\.Round\(CompactStackedActionRowsWidth \* scale\)",
        "panelWidth = compact \? 1\.0f",
        "panelHeight = compact \? 1\.0f",
        "Math\.Min\(safeViewport\.X \* 0\.96f, 1600f\)",
        "Math\.Min\(safeViewport\.X \* 0\.84f, 1180f\)",
        "CompactStackedActionRows=\{CompactStackedActionRows\}"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Components\StyledPanel.cs" `
    "reduces compact-mode panel padding and avoids a short fixed-height phone panel" `
    @(
        "MaxHeight = 2200f",
        "CompactPanelHorizontalMargin = 10",
        "CompactPanelTopMargin = 10",
        "CompactPanelBottomMargin = 12",
        "compact \? CompactPanelHorizontalMargin : LauncherComponentTheme\.PanelHorizontalMargin",
        "compact \? CompactPanelTopMargin : LauncherComponentTheme\.PanelTopMargin",
        "compact \? CompactPanelBottomMargin : LauncherComponentTheme\.PanelBottomMargin",
        "_compact\s*\?\s*vpSize\.Y \* heightRatio",
        "BuildStyle\(scale, compact\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.cs" `
    "adds compact-only bottom scroll breathing room for phone gesture areas" `
    @(
        "profile\.Compact",
        "BuildCompactBottomScrollSpacer",
        "LauncherViewLayoutMetrics\.ScaleInt\(72, scale\)",
        "MouseFilterEnum\.Ignore"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.cs" `
    "uses a low-profile compact status card with responsive headline and narrow stacked fallback" `
    @(
        "BuildCompactStatusCapsule",
        "CompactStatusBodySeparation = 5",
        "CompactStatusAccentHeight = 3",
        "CompactStatusHeadlineSeparation = 3",
        "CompactStatusHeadlineInlineSeparation = 6",
        "CompactStatusPhaseInlineWidth = 112",
        "CompactStatusPhaseHorizontalMargin = 7",
        "CompactStatusPhaseVerticalMargin = 3",
        "CompactStatusActionMinHeight = 24",
        "profile\.CompactStackedActionRows",
        "ApplyCompactStatusHeadlineLayout",
        "GridContainer CompactHeadline",
        "PanelContainer CompactPhasePanel",
        "LauncherViewLayoutMetrics\.ScaleInt\(CompactStatusBodySeparation, scale\)",
        "LauncherViewLayoutMetrics\.ScaleInt\(CompactStatusAccentHeight, scale\)",
        "var headline = new GridContainer",
        "headline\.Columns = stacked \? 1 : 2",
        "LauncherViewLayoutMetrics\.ScaleInt\(\s*stacked\s*\?\s*CompactStatusHeadlineSeparation\s*:\s*CompactStatusHeadlineInlineSeparation",
        "LauncherViewLayoutMetrics\.ScaleInt\(stacked \? 0 : CompactStatusPhaseInlineWidth, scale\)",
        "LauncherViewLayoutMetrics\.ScaleInt\(CompactStatusActionMinHeight, scale\)",
        "var stacked = profile\.CompactStackedActionRows",
        "phasePanel\.SizeFlagsHorizontal = stacked",
        "Control\.SizeFlags\.ShrinkBegin",
        "headline\.AddChild\(phasePanel\)",
        "BuildStatusPhaseStyle\(scale, compact: true\)",
        "statusActionLabel\.VerticalAlignment = VerticalAlignment\.Center",
        "statusActionLabel\.AutowrapMode = TextServer\.AutowrapMode\.WordSmart",
        "statusActionLabel\.ClipText = false",
        "statusActionLabel\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill",
        "statusActionLabel\.CustomMinimumSize = new Vector2",
        "headline\.AddChild\(statusActionLabel\)",
        "body\.AddChild\(headline\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Behavior.cs" `
    "reflows the compact status headline after viewport changes" `
    @(
        "UpdateCompactStatusHeadline\(viewportSize\)",
        "private void UpdateCompactStatusHeadline\(Vector2 viewportSize\)",
        "_compactStatusHeadline",
        "_compactStatusPhasePanel",
        "LauncherLayoutProfile\.ForViewport\(viewportSize\)",
        "ApplyCompactStatusHeadlineLayout"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.cs" `
    "keeps normal compact status details to a stable one-line row" `
    @(
        "CompactStatusDetailHeight = 28",
        "LauncherViewLayoutMetrics\.ScaleInt\(CompactStatusDetailHeight, scale\)",
        "statusLabel\.AutowrapMode = TextServer\.AutowrapMode\.Off",
        "statusLabel\.ClipText = true",
        "statusLabel\.TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.cs" `
    "lets compact attention statuses expand while normal status details stay stable" `
    @(
        "_statusLabel\.TooltipText = message",
        "string\.Equals\(phase, `"ATTENTION`", StringComparison\.Ordinal\)",
        "TextServer\.AutowrapMode\.WordSmart",
        "TextServer\.AutowrapMode\.Off",
        "_statusLabel\.ClipText = !attention"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.cs" `
    "adds a touch-safe responsive compact workflow step strip so phone users can see and tap the current launcher step without hover tooltips" `
    @(
        "BuildCompactWorkflowStrip",
        "CompactWorkflowStepHeight = LauncherSectionMetrics\.CompactDrawerToggleHeight",
        "CompactWorkflowStepDenseHeight = LauncherSectionMetrics\.CompactDrawerToggleHeight",
        "CompactWorkflowStepLabelFontSize = 13",
        "CompactWorkflowStepNumberFontSize = LauncherSectionMetrics\.CompactDetailLabelFontSize",
        "CompactWorkflowStepNumberMinWidth = 16",
        "CompactWorkflowStepAccentHeight = 2",
        "CompactWorkflowStepSeparation = 1",
        "CompactWorkflowStepCellGap = 2",
        "CompactWorkflowStepNumberGap = 2",
        "CompactWorkflowStepHorizontalMargin = 3",
        "CompactWorkflowStepVerticalMargin = 2",
        "GridContainer",
        "bool denseNarrowWorkflow",
        "Columns = CompactWorkflowStepNames\.Length",
        "var stepHeight = denseNarrowWorkflow",
        "\? CompactWorkflowStepDenseHeight",
        ": CompactWorkflowStepHeight",
        "Button\[\] StepButtons",
        "BuildCompactWorkflowStepButton\(i, scale, stepHeight\)",
        "BuildCompactWorkflowStepButton\(int index, float scale, int height\)",
        "ApplyWorkflowStepButtonStyle",
        "CompactWorkflowStepTooltips",
        "CompactWorkflowStepNumbers",
        "MouseDefaultCursorShape = Control\.CursorShape\.PointingHand",
        "Go to \{CompactWorkflowStepTooltips\[index\]\}",
        "StyledLabel\[\] StepNumberLabels",
        "StyledLabel\[\] StepLabels",
        "ColorRect\[\] StepAccents",
        "SetAnchorsPreset\(Control\.LayoutPreset\.FullRect\)",
        "new HBoxContainer",
        "OffsetLeft",
        "OffsetRight",
        "numberLabels\[i\] = numberLabel",
        "label\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill",
        "label\.TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
        "LauncherViewLayoutMetrics\.ScaleInt\(height, scale\)",
        "fontSize: CompactWorkflowStepNumberFontSize",
        "fontSize: CompactWorkflowStepLabelFontSize",
        "LauncherViewLayoutMetrics\.ScaleInt\(CompactWorkflowStepAccentHeight, scale\)",
        "BuildWorkflowStepStyle"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.cs" `
    "makes compact workflow steps tappable direct navigation controls" `
    @(
        "_workflowStepButtons",
        "WireCompactWorkflowStepNavigation",
        "ScrollCompactWorkflowStep",
        "Pressed \+= \(\) => ScrollCompactWorkflowStep\(capturedStep\)",
        "CompactWorkflowStep\.SignIn => Login\.Visible",
        "CompactWorkflowStep\.Code => Code\.Visible",
        "CompactWorkflowStep\.Files => Download\.Visible",
        "CompactWorkflowStep\.Play => _compactCurrentTaskTarget",
        "ScrollCompactPrimaryTo\(target\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.cs" `
    "anchors the compact workflow step strip outside the scrolling body so progress remains visible" `
    @(
        "var workflowStrip = BuildCompactWorkflowStrip\(scale, profile\.Compact, profile\.CompactStackedActionRows\)",
        "BuildCompactStickyTaskHeader\(profile, compactCurrentTaskButton, workflowStrip\.Strip\)",
        "root\.AddChild\(leftScroll\)",
        "if \(!profile\.Compact\)",
        "left\.AddChild\(workflowStrip\.Strip\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.cs" `
    "adds a low-profile compact current-task jump button so phone users can reach the active section quickly" `
    @(
        "BuildCompactCurrentTaskButton",
        "SetCompactCurrentTaskButtonText",
        "EnsureCompactCurrentTaskButtonLabels",
        "CompactCurrentTaskButtonBodyName",
        "CompactCurrentTaskButtonTitleName",
        "CompactCurrentTaskButtonDetailName",
        "CompactCurrentTaskButtonDetailFontSize = LauncherSectionMetrics\.CompactDetailLabelFontSize",
        'button\.Text = ""',
        "body\.SetAnchorsPreset\(Control\.LayoutPreset\.FullRect\)",
        "TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
        "LauncherComponentTheme\.TextSecondary",
        'SetCompactCurrentTaskButtonText\(button, scale, "GO TO SETUP", "Setup guide"\)',
        "LauncherSectionMetrics\.CompactDetailButtonHeight",
        "LauncherSectionMetrics\.CompactDetailButtonFontSize",
        "LauncherButtonStyles\.ApplyPrimaryAction",
        "compactCurrentTaskButton"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.cs" `
    "anchors the compact current-task jump button outside the scrolling body so it remains reachable" `
    @(
        "var compactCurrentTaskButton = BuildCompactCurrentTaskButton\(scale, profile\.Compact\)",
        "if \(profile\.Compact\)",
        "BuildCompactStickyTaskHeader\(profile, compactCurrentTaskButton, workflowStrip\.Strip\)",
        "root\.AddChild\(leftScroll\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.cs" `
    "builds the compact current-task button and workflow strip as one viewport-reflowable sticky header grid" `
    @(
        "CompactStickyTaskHeaderGridName",
        "GridContainer CompactStickyTaskHeader",
        "Control CompactWorkflowStrip",
        "var stickyHeader = BuildCompactStickyTaskHeader\(profile, compactCurrentTaskButton, workflowStrip\.Strip\)",
        "compactStickyTaskHeader = stickyHeader\.Header",
        "root\.AddChild\(stickyHeader\.Toolbar\)",
        "BuildCompactStickyTaskHeader",
        "new GridContainer",
        "Name = CompactStickyTaskHeaderGridName",
        "ApplyCompactStickyTaskHeaderLayout",
        "header\.Columns = stacked \? 1 : 2",
        "CompactStickyTaskHeaderInlineGap = 6",
        "CompactStickyTaskButtonMinWidth = 176",
        "CompactInlineCurrentTaskHeight = LauncherSectionMetrics\.CompactDrawerToggleHeight",
        "compactCurrentTaskButton\.SizeFlagsHorizontal = Control\.SizeFlags\.ShrinkBegin",
        "LauncherViewLayoutMetrics\.ScaleInt\(CompactStickyTaskButtonMinWidth, scale\)",
        "LauncherViewLayoutMetrics\.ScaleInt\(CompactInlineCurrentTaskHeight, scale\)",
        "workflowStrip\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill",
        "header\.AddChild\(compactCurrentTaskButton\)",
        "header\.AddChild\(workflowStrip\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.cs" `
    "wraps the compact sticky task header in a low-profile toolbar shell" `
    @(
        "WrapCompactStickyTaskHeader",
        "BuildCompactStickyTaskHeaderStyle",
        "CompactStickyTaskToolbarRadius = 7",
        "CompactStickyTaskToolbarHorizontalMargin = 5",
        "CompactStickyTaskToolbarVerticalMargin = 4",
        "new PanelContainer",
        "BuildCompactStickyTaskHeaderStyle\(scale\)",
        "LauncherViewLayoutMetrics\.ScaleInt\(CompactStickyTaskToolbarHorizontalMargin, scale\)",
        "LauncherViewLayoutMetrics\.ScaleInt\(CompactStickyTaskToolbarVerticalMargin, scale\)",
        "return \(WrapCompactStickyTaskHeader\(scale, header\), header\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.cs" `
    "stacks the compact sticky task header on narrow compact viewports so task and workflow controls stay readable" `
    @(
        "CompactStickyTaskHeaderStackGap = 3",
        "CompactStickyTaskHeaderStackWidth = 560",
        "ShouldStackCompactStickyTaskHeader",
        "profile\.ContentMaxWidth < LauncherViewLayoutMetrics\.ScaleInt",
        "ApplyCompactStickyTaskHeaderLayout",
        "header\.Columns = stacked \? 1 : 2",
        "stacked \? CompactStickyTaskHeaderStackGap : CompactStickyTaskHeaderInlineGap",
        "CompactStackedCurrentTaskHeight = CompactWorkflowStepDenseHeight",
        "stacked \? CompactStickyTaskHeaderStackGap : CompactStickyTaskHeaderInlineGap",
        "compactCurrentTaskButton\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill",
        "LauncherViewLayoutMetrics\.ScaleInt\(CompactStackedCurrentTaskHeight, scale\)",
        "workflowStrip\.SizeFlagsVertical = Control\.SizeFlags\.ShrinkBegin"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Behavior.cs" `
    "reflows the compact sticky task header after Android viewport changes" `
    @(
        "UpdateCompactStickyTaskHeader\(viewportSize\)",
        "UpdateCompactStickyTaskHeader\(Vector2 viewportSize\)",
        "LauncherLayoutProfile\.ForViewport\(viewportSize\)",
        "ApplyCompactStickyTaskHeaderLayout",
        "_compactStickyTaskHeader",
        "_compactWorkflowStrip"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.cs" `
    "tracks compact workflow step labels and updates active/completed step colors" `
    @(
        "CompactWorkflowStepNames",
        "CompactWorkflowStepNumbers",
        "CompactWorkflowStep",
        "SetCompactWorkflowStep",
        '"SIGN IN"',
        '"GUARD"',
        '"FILES"',
        '"PLAY"',
        '"1"',
        '"2"',
        '"3"',
        '"4"',
        "CompactWorkflowStepTooltips",
        "_workflowStepNumberLabels",
        "Sign in",
        "Steam Guard",
        "Files",
        "Play",
        "_workflowStepNumberLabels\[i\]\.AddThemeColorOverride",
        "LauncherComponentTheme\.OrangeHot",
        "LauncherComponentTheme\.CyanAccent",
        "LauncherComponentTheme\.TextMuted"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.cs" `
    "tracks the compact current-task jump target without invoking launcher actions directly" `
    @(
        "_compactCurrentTaskButton",
        "_compactCurrentTaskTarget",
        "SetCompactCurrentTask",
        "SetCompactCurrentTaskButtonText",
        "string detail",
        "SetCompactCurrentTaskButtonText\(_compactCurrentTaskButton, _scale, text, detail\)",
        "ScrollCompactPrimaryTo\(_compactCurrentTaskTarget\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.cs" `
    "updates the compact workflow step strip from existing section state transitions" `
    @(
        "SetCompactWorkflowStep\(CompactWorkflowStep\.SignIn\)",
        "SetCompactWorkflowStep\(CompactWorkflowStep\.Code\)",
        "SetCompactWorkflowStep\(CompactWorkflowStep\.Files\)",
        "SetCompactWorkflowStep\(CompactWorkflowStep\.Play\)",
        "ShowDownloadProgress",
        "SetDownloadProgress",
        "ShowLaunchActions",
        "ShowRetry"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.cs" `
    "updates the compact current-task jump button from existing section state transitions" `
    @(
        'SetCompactCurrentTask\("GO TO LOGIN", Login, "Steam login"\)',
        'SetCompactCurrentTask\("GO TO GUARD", Code, "Verification code"\)',
        'SetCompactCurrentTask\("GO TO FILES", Download, "Game files"\)',
        'SetCompactCurrentTask\("GO TO RETRY", Actions\.RetryScrollTarget, "Recovery action"\)',
        'SetCompactCurrentTask\("GO TO PLAY", Actions\.ReadyScrollTarget, "Play and saves"\)'
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.cs" `
    "labels compact current-task jumps as navigation rather than direct launcher actions" `
    @(
        '"GO TO LOGIN"',
        '"GO TO GUARD"',
        '"GO TO FILES"',
        '"GO TO RETRY"',
        '"GO TO PLAY"'
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.cs" `
    "promotes compact retry recovery to a primary structured action" `
    @(
        'compact \? CompactRetryButtonText\(\) : "RETRY"',
        "LauncherButtonStyles\.ApplyPrimaryAction\(_retryButton, scale\)",
        "SetCompactActionButtonText\(_retryButton, _retryButton\.Text\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.cs" `
    "labels compact retry recovery as TRY AGAIN with restart-task detail" `
    @(
        "CompactRetryButtonText",
        'CompactPlaySyncDrawerText\("TRY AGAIN", "Restart task"\)'
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.cs" `
    "labels compact launch CTA with selected-version detail" `
    @(
        "SetCompactActionButtonText\(_launchButton",
        "CompactLaunchButtonText\(text\)",
        "CompactLaunchButtonText",
        "START GAME",
        "Selected version"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.cs" `
    "suppresses compact first-run safe-flow guidance during active task states while preserving setup guidance" `
    @(
        "SetFirstRunGuideVisible\(true\)",
        "SetFirstRunGuideVisible\(false\)",
        "SetLoginFormVisible\(bool visible, bool disabled\)[\s\S]*SetFirstRunGuideVisible\(false\)[\s\S]*HideCompactCompletedAuthSections",
        "ShowCodePrompt\(bool wasIncorrect\)[\s\S]*SetFirstRunGuideVisible\(false\)",
        "ShowDownloadAction\(string buttonText\)[\s\S]*SetFirstRunGuideVisible\(false\)",
        "ShowLaunchActions",
        "ShowRetry",
        "ShowDownloadAction",
        "SetLoginFormVisible",
        "FirstRunGuide\.Visible = !_profile\.Compact \|\| visible"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\CodeSection.cs" `
    "uses larger compact Steam Guard code entry and verification controls" `
    @(
        "bool compactStackedActionRows = false",
        "_compactStackedActionRows = compact && compactStackedActionRows",
        "compact \? `"ABC123`" : `"Steam Guard code`"",
        "CodeInputHeight",
        "CodeInputFontSize",
        "CodeSubmitFontSize",
        "CompactCodePromptHeight",
        "CompactCodeHelpHeight",
        "CompactCodeSubmitText",
        "SetCompactCodeSubmitButtonText",
        "One-shot submit; code is not stored",
        "_codeLabel\.CustomMinimumSize = new Vector2",
        "_codeHelpLabel\.CustomMinimumSize = new Vector2",
        "_codeHelpLabel\.TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
        "inputHeight",
        "inputFontSize",
        "height: inputHeight",
        "VERIFY CODE",
        "Submit once",
        "CompactCodeActionRowSeparation = 6",
        "BuildCompactCodeActionRow",
        "GridContainer _compactCodeActionRow",
        "new GridContainer",
        "ApplyCompactCodeActionRowLayout",
        "row\.Columns = compactStackedActionRows \? 1 : 2",
        "codeActionParent\.AddChild\(_codeField\)",
        "codeActionParent\.AddChild\(submitButton\)",
        "_codeField\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill",
        "submitButton\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill",
        "MoveChild\(_codeHelpLabel, compactCodeActionRow\.GetIndex\(\) \+ 1\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\CodeSection.cs" `
    "reflows compact Steam Guard code controls after viewport changes" `
    @(
        "UpdateViewportProfile\(LauncherLayoutProfile profile\)",
        "GodotObject\.IsInstanceValid\(_compactCodeActionRow\)",
        "_compactStackedActionRows = profile\.Compact && profile\.CompactStackedActionRows",
        "ApplyCompactCodeActionRowLayout\(_compactCodeActionRow, profile\.Scale, _compactStackedActionRows\)",
        "row\.Columns = compactStackedActionRows \? 1 : 2",
        "LauncherViewLayoutMetrics\.ScaleInt\(CompactCodeActionRowSeparation, scale\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Behavior.cs" `
    "updates compact section responsive rows after viewport changes" `
    @(
        "UpdateCompactSectionResponsiveRows\(viewportSize\)",
        "private void UpdateCompactSectionResponsiveRows\(Vector2 viewportSize\)",
        "LauncherLayoutProfile\.ForViewport\(viewportSize\)",
        "Code\.UpdateViewportProfile\(profile\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\CodeSection.cs" `
    "keeps compact Steam Guard retry guidance short and readable" `
    @(
        "CompactIncorrectPrompt",
        "Code rejected",
        "CompactIncorrectHelp",
        "Use newest Steam Guard code",
        "Old codes can expire; spaces removed",
        "CodePromptText\(_compact, wasIncorrect\)",
        "CodeHelpText\(_compact, wasIncorrect\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.cs" `
    "passes compact width class into the Steam Guard code section for responsive verification controls" `
    @(
        "new CodeSection\(scale, profile\.Compact, profile\.CompactStackedActionRows\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\LauncherSectionMetrics.cs" `
    "defines compact Steam Guard touch target metrics" `
    @(
        "CodeInputFontSize\s*=\s*22",
        "CodeInputHeight\s*=\s*76",
        "CodeSubmitFontSize\s*=\s*19",
        "CompactDetailLabelFontSize\s*=\s*12",
        "CompactDrawerToggleHeight\s*=\s*54"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.cs" `
    "suppresses compact install section after launch-ready while restoring it for downloads" `
    @(
        "SetCompactReadyInstallSectionVisible",
        "ShowDownloadAction",
        "SetCompactReadyInstallSectionVisible\(true\)",
        "ShowLaunchActions",
        "SetCompactReadyInstallSectionVisible\(false\)",
        "!_profile\.Compact",
        "Download\.Visible = visible"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.cs" `
    "suppresses completed compact auth sections after advancing past sign-in or Steam Guard" `
    @(
        "HideCompactCompletedAuthSections",
        "ShowCodePrompt",
        "HideCompactCompletedAuthSections\(showCode: true\)",
        "ShowDownloadAction",
        "ShowRetry",
        "ShowLaunchActions",
        "HideCompactCompletedAuthSections\(showCode: false\)",
        "Login\.SetFormVisible\(false, disabled: true\)",
        "Code\.Visible = showCode"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.cs" `
    "scrolls compact state transitions to the active section" `
    @(
        "CompactScrollAnchorTopPadding = 14",
        "ScrollCompactPrimaryTo",
        "ApplyCompactScrollAnchorPadding",
        "!_profile\.Compact",
        "Callable\.From",
        "PrimaryScroll\.EnsureControlVisible\(target\)",
        "PrimaryScroll\.ScrollVertical",
        "LauncherViewLayoutMetrics\.ScaleInt\(CompactScrollAnchorTopPadding, _scale\)",
        "ScrollCompactPrimaryTo\(Login\)",
        "ScrollCompactPrimaryTo\(Code\)",
        "ScrollCompactPrimaryTo\(Download\)",
        "ScrollCompactPrimaryTo\(Actions\.RetryScrollTarget\)",
        "ScrollCompactPrimaryTo\(Actions\.ReadyScrollTarget\)",
        "ScrollCompactPrimaryTo\(FirstRunGuide\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.cs" `
    "remembers the latest compact scroll anchor for viewport-change re-anchoring" `
    @(
        "ScrollCompactPrimaryTo\(Control target\)",
        "!GodotObject\.IsInstanceValid\(target\)",
        "_compactScrollAnchorTarget = target",
        "PrimaryScroll\.EnsureControlVisible\(target\)",
        "ApplyCompactScrollAnchorPadding\(target\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.cs" `
    "tracks compact scroll anchor state separately from the current task target" `
    @(
        "_compactScrollAnchorTarget",
        "_compactScrollAnchorTarget = FirstRunGuide",
        "_compactCurrentTaskTarget = FirstRunGuide",
        "_compactCurrentTaskTarget = target",
        "_compactScrollAnchorTarget = target"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Behavior.cs" `
    "re-anchors compact task scroll position after Android viewport changes without fighting focused keyboard input" `
    @(
        "ReanchorCompactScrollTargetAfterViewportChange\(\)",
        "DisplayServer\.VirtualKeyboardGetHeight\(\) > 0",
        "GuiGetFocusOwner",
        "PrimaryScroll\.IsAncestorOf\(focusOwner\)",
        "CompactViewportReanchorTarget",
        "IsUsableCompactAnchor",
        "ScrollCompactPrimaryTo\(target\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.cs" `
    "anchors compact ready/retry scrolling to the actual primary controls" `
    @(
        "ReadyScrollTarget",
        "_compact \? _cloudGroup : _launchButton",
        "RetryScrollTarget",
        "_retryButton"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.cs" `
    "states that version downloads affect local files and do not mutate Steam Cloud saves" `
    @(
        "SHOW VERSION DETAILS",
        "HIDE VERSION DETAILS",
        "CompactDownloadButtonText",
        "CompactDownloadButtonTitleDetail",
        "SetCompactDownloadButtonText",
        "EnsureCompactDownloadButtonLabels",
        "CompactDownloadActionBodyName",
        "CompactDownloadActionTitleName",
        "CompactDownloadActionDetailName",
        "CompactDownloadActionHeight = LauncherSectionMetrics\.CodeInputHeight",
        "REFRESH VERSIONS",
        "DOWNLOAD",
        "ToggleBranchDetails",
        "_branchDetailsExpanded",
        "Download/update changes local files for the selected game version only",
        "does not change Steam Cloud saves",
        "SelectedOptionStatus",
        "SelectedOptionCompactStatus",
        "SelectorInstallSlotHelpText",
        "CompactInstallVersionHelpText",
        "CompactVersionHelpHeight",
        "CompactVersionHelpFontSize",
        "Install target:",
        "local files only",
        "_branchHelpLabel\.ClipText = compact",
        "_branchHelpLabel\.TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
        "_branchHelpLabel\.CustomMinimumSize = new Vector2",
        "_compactSelectedVersionLabel",
        "Selected version:",
        "Install slot:",
        "Downloads do not change Steam Cloud saves",
        "_compactSelectedVersionPanel",
        "_compactVersionControlsRow",
        "BuildCompactVersionControlsRow",
        "compactStackedActionRows \? new VBoxContainer\(\) : new HBoxContainer\(\)",
        "_compactVersionControlsRow\.AddChild\(_branchDropdown\)",
        "_compactVersionControlsRow\.AddChild\(_refreshBranchesButton\)",
        "_compactVersionControlsRow\.Visible = controlsVisible",
        "BuildSelectedVersionSummaryStyle",
        "CompactVersionSummaryFontSize",
        "CompactVersionSummaryHeight",
        "CompactStackedVersionSummaryHeight",
        "CompactVersionSummaryHorizontalMargin",
        "CompactVersionSummaryVerticalMargin",
        "CompactSelectedVersionBranchLimit = 18",
        "CompactSelectedVersionStackedBranchLimit = 28",
        "_compactStackedActionRows = compact && compactStackedActionRows",
        "compactStackedActionRows = false",
        "AutowrapMode\.Off",
        "TextServer\.AutowrapMode\.WordSmart",
        "TextServer\.OverrunBehavior\.TrimEllipsis",
        "ClipText = compact && !_compactStackedActionRows",
        "CompactStackedVersionSummaryHeight",
        "CompactSelectedVersionStackedBranchLimit",
        "CompactSelectedVersionHeadline",
        "SetCompactVersionActionButtonText",
        "EnsureCompactVersionActionButtonLabels",
        "CompactVersionActionBodyName",
        "CompactVersionActionTitleName",
        "CompactVersionActionDetailName",
        "CompactVersionActionDetailFontSize = LauncherSectionMetrics\.CompactDetailLabelFontSize",
        'button\.Text = ""',
        "body\.SetAnchorsPreset\(Control\.LayoutPreset\.FullRect\)",
        "TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
        "LauncherComponentTheme\.TextSecondary",
        "`"CHANGE VERSION`"",
        "`"REFRESH VERSIONS`"",
        "`"DOWNLOAD VERSION`"",
        "`"REDOWNLOAD VERSION`"",
        "`"RETRY DOWNLOAD`"",
        "`"DOWNLOADING\.\.\.`"",
        "Local files only",
        "Rebuild local files",
        "Steam files",
        "Update branch list",
        "Keep selection",
        "CompactDrawerToggleHeight",
        "CompactDetailButtonFontSize",
        "Cloud unchanged",
        "local files only",
        "CompactInstallSlotKind",
        "public slot",
        "branch slot",
        "branchChanged",
        "_branchDetailsExpanded = false",
        "CollapseCompactBranchDetailsAfterSelection",
        "ApplyBranchControlVisibility\(\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.cs" `
    "passes compact width class into the Game Install section for responsive selected-version summaries" `
    @(
        "new DownloadSection\(scale, profile\.Compact, profile\.CompactStackedActionRows\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.cs" `
    "keeps compact version drawer toggle before expanded install-version details" `
    @(
        "_branchDetailsToggle = new StyledButton",
        "CompactDrawerToggleHeight",
        "SetCompactVersionActionButtonText",
        "EnsureCompactVersionActionButtonLabels",
        "CompactVersionActionBodyName",
        "CompactVersionActionTitleName",
        "CompactVersionActionDetailName",
        "CompactVersionActionDetailFontSize = LauncherSectionMetrics\.CompactDetailLabelFontSize",
        'button\.Text = ""',
        "body\.SetAnchorsPreset\(Control\.LayoutPreset\.FullRect\)",
        "TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
        "LauncherComponentTheme\.TextSecondary",
        "`"CHANGE VERSION`"",
        "`"REFRESH VERSIONS`"",
        "Local files only",
        "Update branch list",
        "AddChild\(_branchDetailsToggle\)",
        "_branchDropdown = new OptionButton",
        "(?s)_branchDetailsToggle = new StyledButton.*AddChild\(_branchDetailsToggle\).*_branchDropdown = new OptionButton"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.cs" `
    "uses primary compact touch-target sizing for the install-version dropdown" `
    @(
        "compact \? LauncherSectionMetrics\.PrimaryButtonHeight : LauncherSectionMetrics\.SecondaryButtonHeight",
        "compact \? LauncherSectionMetrics\.PrimaryButtonFontSize : LauncherSectionMetrics\.SecondaryButtonFontSize",
        "ApplyDropdownAction",
        "(?s)ApplyDropdownAction\(\s*_branchDropdown,\s*scale,.*?,\s*compact\s*\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.cs" `
    "puts the compact install primary action before optional version details" `
    @(
        "MoveCompactPrimaryInstallControlsBeforeVersionDetails",
        "MoveChild\(_compactSelectedVersionPanel, _branchDetailsToggle\.GetIndex\(\)\)",
        "MoveChild\(_downloadButton, _branchDetailsToggle\.GetIndex\(\)\)",
        "DOWNLOAD VERSION",
        "CHANGE VERSION"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.cs" `
    "renders compact install primary actions as structured title/detail labels" `
    @(
        "CompactDownloadActionHeight = LauncherSectionMetrics\.CodeInputHeight",
        "CompactDownloadActionBodyName",
        "CompactDownloadActionTitleName",
        "CompactDownloadActionDetailName",
        "CompactDownloadActionTitleFontSize = LauncherSectionMetrics\.PrimaryButtonFontSize",
        "CompactDownloadActionDetailFontSize = LauncherSectionMetrics\.CompactDetailLabelFontSize",
        "CompactDownloadButtonTitleDetail",
        "CompactDownloadButtonText",
        "SetCompactDownloadButtonText",
        "TrySplitCompactDownloadButtonText",
        "HideCompactDownloadButtonLabels",
        "EnsureCompactDownloadButtonLabels",
        'button\.Text = ""',
        "body\.SetAnchorsPreset\(Control\.LayoutPreset\.FullRect\)",
        "TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
        "LauncherComponentTheme\.TextSecondary",
        "`"DOWNLOAD SELECTED VERSION`"",
        "`"DOWNLOAD VERSION`"",
        "`"Local files only`"",
        "`"REDOWNLOAD SELECTED VERSION`"",
        "`"REDOWNLOAD VERSION`"",
        "`"Rebuild local files`"",
        "`"RETRY DOWNLOAD`"",
        "`"DOWNLOADING\.\.\.`"",
        "`"Steam files`""
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.cs" `
    "promotes compact download progress directly under the active primary action" `
    @(
        "MoveCompactProgressControlsNearPrimaryAction",
        "MoveChild\(_progressLabel, _downloadButton\.GetIndex\(\) \+ 1\)",
        "MoveChild\(_progressBar, _progressLabel\.GetIndex\(\) \+ 1\)",
        "new StyledProgressBar\(scale, compact\)",
        "CompactDownloadProgressButtonText",
        "CompactDownloadProgressText",
        "CompactDownloadProgressDetail",
        "NormalizeCompactProgressText",
        "CompactDownloadProgressLabelHeight = 50",
        "CompactDownloadProgressDetailLimit = 54",
        "_progressLabel\.AutowrapMode = TextServer\.AutowrapMode\.WordSmart",
        "_progressLabel\.ClipText = compact",
        "_progressLabel\.TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
        "_progressLabel\.CustomMinimumSize = new Vector2",
        "_progressLabel\.Text = _compact \? CompactDownloadProgressText\(text\) : text",
        "Downloading selected version",
        "SetCompactDownloadButtonText\(_downloadButton, CompactDownloadProgressButtonText\(\)\)",
        "compact\s*\?\s*LauncherSectionMetrics\.SecondaryButtonFontSize",
        "compact\s*\?\s*LauncherComponentTheme\.CyanAccent"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Components\LauncherComponentTheme.cs" `
    "defines compact readable download progress bar metrics and colors" `
    @(
        "ProgressBarHeight = 24",
        "CompactProgressBarHeight = 34",
        "ProgressBarFontSize = 12",
        "CompactProgressBarFontSize = 14",
        "ProgressBarRadius = 6",
        "ProgressBackground",
        "ProgressFill",
        "ProgressFillCompact"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Components\LauncherComponentTheme.cs" `
    "rounds shared component scaling instead of flooring compact Android metrics" `
    @(
        "using System;",
        "MathF\.Round\(value \* scale, MidpointRounding\.AwayFromZero\)",
        "Math\.Max\(0,"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherViewLayoutMetrics.cs" `
    "rounds shared layout scaling instead of flooring compact Android metrics" `
    @(
        "using System;",
        "MathF\.Round\(value \* scale, MidpointRounding\.AwayFromZero\)",
        "Math\.Max\(0,"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Components\StyledProgressBar.cs" `
    "uses a taller styled compact percentage progress bar" `
    @(
        "internal StyledProgressBar\(float scale, bool compact = false\)",
        "compact\s*\?\s*LauncherComponentTheme\.CompactProgressBarHeight",
        "ShowPercentage = true",
        "CompactProgressBarFontSize",
        "BackgroundStyle",
        "FillStyle",
        "ProgressFillCompact",
        "BuildProgressStyle"
    )

Add-Check `
    "src\STS2Mobile\Launcher\ShaderWarmupScreen.UI.cs" `
    "uses Android-readable compact sizing and styled progress for shader warmup" `
    @(
        "OperatingSystem\.IsAndroid\(\)",
        "AndroidMinimumScale = 1\.06f",
        "AndroidMinimumPanelWidth = 320f",
        "AndroidPanelWidthRatio = 0\.94f",
        "CalculateAdaptiveScale\(vpSize, androidCompact\)",
        "widthRatio: CalculatePanelWidthRatio\(vpSize, androidCompact\)",
        "compact: androidCompact",
        "CalculateWarmupPanelSize\(vpSize, androidCompact\)",
        "new StyledProgressBar\(scale, androidCompact\)",
        "androidCompact \? AndroidMinimumScale : MinimumScale",
        "return AndroidPanelWidthRatio"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherStartupStatus.cs" `
    "uses an Android-readable framed startup status card after launcher close" `
    @(
        "OperatingSystem\.IsAndroid\(\)",
        "CreateAndroidStatusCard\(parent, viewportSize\)",
        "CreateLegacyLabel\(parent, viewportSize\)",
        "AndroidMinimumScale = 1\.06f",
        "AndroidWidthRatio = 0\.94f",
        "AndroidMessageFontSize = 18",
        "AndroidPanelHeight = 98",
        "PanelContainer",
        "BuildAndroidPanelStyle",
        "STARTING GAME",
        "LauncherComponentTheme\.TextPrimary",
        "CalculateAndroidPanelWidth",
        "MouseFilterEnum\.Ignore"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherStartupStatus.cs" `
    "cleans up the full Android startup status card after observed game startup" `
    @(
        "internal static bool QueueFree\(Label label\)",
        "FindStatusRoot\(label\)",
        "for \(Node current = label; current != null; current = current\.GetParent\(\)\)",
        "current\.Name == NodeName",
        "target\.QueueFree\(\)",
        "Startup status cleanup failed"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherGameStartupRecovery.State.cs" `
    "uses startup-status root cleanup instead of freeing only the message label" `
    @(
        "LauncherStartupStatus\.QueueFree\(StartupStatus\)",
        "Post-startup recovery UI cleanup finished after game startup was observed",
        "statusCleared"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.cs" `
    "keeps compact version drawer toggle before expanded ready-state version details" `
    @(
        "_branchDetailsToggle = new StyledButton",
        "CompactDrawerToggleHeight",
        "CompactDetailButtonFontSize",
        "CompactPlaySyncDrawerText",
        "_cloudSafetyToggle = new StyledButton",
        "_cloudOptionsToggle = new StyledButton",
        "_supportToggle = AddHiddenButton",
        "SupportToggleText\(\)",
        "AddChild\(_branchDetailsToggle\)",
        "_branchDropdown = new OptionButton",
        "branchChanged",
        "CollapseCompactBranchDetailsAfterSelection",
        "_branchDetailsExpanded = false",
        "(?s)_branchDetailsToggle = new StyledButton.*AddChild\(_branchDetailsToggle\).*_branchDropdown = new OptionButton"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.cs" `
    "uses primary compact touch-target sizing for the ready-state version dropdown" `
    @(
        "compact \? LauncherSectionMetrics\.PrimaryButtonHeight : LauncherSectionMetrics\.SecondaryButtonHeight",
        "compact \? LauncherSectionMetrics\.PrimaryButtonFontSize : LauncherSectionMetrics\.SecondaryButtonFontSize",
        "ApplyDropdownAction",
        "(?s)ApplyDropdownAction\(\s*_branchDropdown,\s*scale,.*?,\s*compact\s*\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Components\LauncherButtonStyles.cs" `
    "uses touch-safe compact dropdown popup row spacing and padding" `
    @(
        "ApplyDropdownAction",
        "bool compact = false",
        "PopupVerticalSeparation",
        "PopupHorizontalSeparation",
        "PopupItemStartPadding",
        "PopupItemEndPadding",
        "PopupHover",
        "CompactDropdownPopupVerticalSeparation = 16",
        "CompactDropdownPopupHorizontalSeparation = 12",
        "CompactDropdownPopupHorizontalPadding = 20",
        "compact\s*\?\s*CompactDropdownPopupVerticalSeparation",
        "compact\s*\?\s*CompactDropdownPopupHorizontalSeparation",
        "compact\s*\?\s*CompactDropdownPopupHorizontalPadding",
        "LauncherComponentTheme\.ButtonHover"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.cs" `
    "configures compact ready-version summary as a readable responsive summary card" `
    @(
        "BuildReadyVersionSummaryStyle\(scale, compact\)",
        "CompactVersionSummaryFontSize",
        "VerticalAlignment\.Center",
        "CompactReadyVersionHelpHeight",
        "CompactReadyVersionHelpFontSize",
        "_branchHelpLabel\.ClipText = compact",
        "_branchHelpLabel\.TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
        "_branchHelpLabel\.CustomMinimumSize = new Vector2",
        "_compactStackedActionRows\s*\?\s*TextServer\.AutowrapMode\.WordSmart",
        "_readyVersionSummaryLabel\.ClipText = compact && !_compactStackedActionRows",
        "TextServer\.OverrunBehavior\.TrimEllipsis",
        "CompactStackedVersionSummaryHeight",
        "CompactVersionSummaryHeight"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.cs" `
    "uses a responsive compact ready-version headline with Pull-first and Push-locked state" `
    @(
        "CompactReadySummaryBranchLimit = 14",
        "CompactReadyStackedSummaryBranchLimit = 28",
        "CompactVersionSummaryRadius",
        "CompactVersionSummaryHorizontalMargin",
        "CompactVersionSummaryVerticalMargin",
        "CompactReadyVersionSummary\(\)",
        "CompactReadyVersionHelpText\(\)",
        "SelectedOptionCompactStatus",
        "Launch target:",
        "cloud via Pull/Push",
        "_compactStackedActionRows",
        "Ready:",
        "Pull first \| Push locked",
        "no auto cloud upload",
        "BuildReadyVersionSummaryStyle\(float scale, bool compact\)",
        "compact \? LauncherSectionMetrics\.CompactVersionSummaryRadius : 8",
        "compact \? LauncherSectionMetrics\.CompactVersionSummaryHorizontalMargin : 12",
        "compact \? LauncherSectionMetrics\.CompactVersionSummaryVerticalMargin : 9"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.cs" `
    "uses compact Play and Sync drawer detail labels for version, cloud-safety, cloud-options, and tools" `
    @(
        "CompactPlaySyncDrawerText",
        "Launch \+ cloud target",
        "Pull-first guard",
        "Push stays locked",
        "CompactCloudSafetyDetailText",
        "Cloud target:",
        "PULL downloads to Android\. PUSH can overwrite Steam\.",
        "Backup \+ sync",
        "Keep active"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.cs" `
    "packs compact recovery and support tools into a responsive grid that becomes full-width on narrow compact viewports" `
    @(
        "compactStackedActionRows = false",
        "_compactStackedActionRows = compact && compactStackedActionRows",
        "_supportToolsGrid = BuildCompactSupportToolsGrid\(scale, compact, _compactStackedActionRows\)",
        "if \(compact\)",
        "_supportGroup\.AddChild\(_supportToolsGrid\)",
        "supportToolsParent = compact",
        "GridContainer",
        "Columns = compactStackedActionRows \? 1 : 2",
        "AddCompactSupportToolButton",
        "`"UPDATES`"",
        "`"VERSIONS`"",
        "`"REDOWNLOAD`"",
        "`"CLEAR CACHE`"",
        "`"DIAGNOSTICS`"",
        "`"LAST ERROR`"",
        "`"COPY LOG`"",
        "`"Check files`"",
        "`"Refresh list`"",
        "`"Rebuild slot`"",
        "`"Old versions`"",
        "`"Export report`"",
        "`"Open details`"",
        "`"Review first`""
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Buttons.cs" `
    "uses readable fill-width buttons inside the compact support grid" `
    @(
        "AddCompactSupportToolButton",
        "SetCompactActionButtonText",
        "EnsureCompactActionButtonLabels",
        "CompactActionButtonBodyName",
        "CompactActionButtonTitleName",
        "CompactActionButtonDetailName",
        "CompactActionButtonDetailFontSize = LauncherSectionMetrics\.CompactDetailLabelFontSize",
        "TrySplitCompactActionButtonText",
        'button\.Text = ""',
        "body\.SetAnchorsPreset\(Control\.LayoutPreset\.FullRect\)",
        "TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
        "LauncherComponentTheme\.TextSecondary",
        "CompactSupportToolHeight",
        "CompactSupportToolFontSize",
        "CompactSupportToolText",
        "SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.cs" `
    "keeps dynamic compact Play and Sync drawer labels synced with structured title/detail button labels" `
    @(
        "private readonly float _scale",
        "SetCompactActionButtonText\(_branchDetailsToggle",
        "SetCompactActionButtonText\(_cloudSafetyToggle",
        "SetCompactActionButtonText\(_updateButton, text\)",
        "SetCompactActionButtonText\(_cloudOptionsToggle"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Toggles.cs" `
    "keeps compact Backup and Sync option toggles synced with structured title/detail button labels" `
    @(
        "SetCompactActionButtonText\(button, text\)",
        "CompactCloudOptionText",
        "Local saves",
        "Game cloud"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.cs" `
    "uses explicit compact cloud direction labels while keeping Push locked" `
    @(
        "CompactCloudPullText\(\)",
        "CompactCloudPushToggleText\(expanded: false\)",
        "CompactCloudPushDangerText\(\)",
        "CompactCloudPushConfirmText\(\)",
        "SetCompactActionButtonText\(_pullButton, _pullButton\.Text\)",
        "SetCompactActionButtonText\(_pushButton, _pushButton\.Text\)",
        "SetCompactActionButtonText\(_confirmPushButton, _confirmPushButton\.Text\)",
        "CompactCloudPushWarningText\(\)",
        "CompactCloudPushWarningFontSize",
        "CompactCloudPushWarningHeight",
        "_pushConfirmationLabel\.ClipText = compact",
        "_pushConfirmationLabel\.TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
        "_pushConfirmationLabel\.CustomMinimumSize = new Vector2"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.cs" `
    "defines compact Pull label as an explicit title/detail Android download action" `
    @(
        "CompactCloudPullText",
        "PULL TO ANDROID",
        "Download saves"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.cs" `
    "defines compact dangerous Push labels as explicit title/detail actions" `
    @(
        "CompactCloudPushDangerText",
        "PUSH TO STEAM",
        "Upload Android",
        "CompactCloudPushConfirmText",
        "CONFIRM OVERWRITE",
        "Final upload",
        "CompactCloudPushWarningText",
        "STEAM CLOUD OVERWRITE",
        "Confirm only after Pull/local saves are verified"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.cs" `
    "keeps compact Push relock label direction-aware and structured after reset" `
    @(
        "CompactCloudPushToggleText",
        "STEAM PUSH LOCKED",
        "Open overwrite",
        "HIDE PUSH",
        "Close overwrite",
        "SetCompactActionButtonText\(_cloudPushToggle, _compact"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.cs" `
    "packs compact Pull and locked Steam Push into responsive action rows while preserving Pull-first order" `
    @(
        "BuildCompactCloudPrimaryActionsRow",
        "compactStackedActionRows",
        "new VBoxContainer\(\) : new HBoxContainer\(\)",
        "CompactCloudPrimaryActionSeparation",
        "cloudPrimaryActionsParent = compact",
        "BuildCompactCloudPrimaryActionsRow\(_pushPullRow, scale, _compactStackedActionRows\)",
        "CompactCloudPullText\(\)",
        "CompactCloudPushToggleText\(expanded: false\)",
        "CompactCloudPushDangerText\(\)",
        "parent\.AddChild\(row\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPortalStatusFormatter.cs" `
    "formats launcher status text with clear user-facing phase labels" `
    @(
        "PhaseLabelStatusSupported\s*=\s*true",
        "StructuredStatusChipSupported\s*=\s*true",
        "STEAM AUTH",
        "VERSION",
        "INSTALL",
        "CLOUD",
        "READY",
        "DIAGNOSTICS",
        "ATTENTION",
        "Could not",
        "Waiting for launcher state",
        "MessageFor",
        "ColorFor",
        "PhaseFor"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPortalUxSupport.cs" `
    "declares secret-safe diagnostic metadata for portal UX hardening" `
    @(
        "StatusLedPortalSupported\s*=\s*true",
        "PhaseLabelStatusSupported",
        "StructuredStatusChipSupported",
        "GuidedNextActionStatusSupported",
        "ErrorFirstGuidedStatusSupported",
        "TitledStateSectionsSupported\s*=\s*true",
        "SafeFirstRunGuidanceSupported\s*=\s*true",
        "CompactSafeFlowCollapsibleSupported\s*=\s*true",
        "CompactSafeFlowToggleDetailLabelsSupported\s*=\s*true",
        "CompactStructuredSafeFlowToggleLabelsSupported\s*=\s*true",
        "CompactSafeFlowBoundedGuideSupported\s*=\s*true",
        "MobileFirstCompactLayoutSupported\s*=\s*true",
        "CompactDensePanelPaddingSupported\s*=\s*true",
        "CompactDenseVerticalRhythmSupported\s*=\s*true",
        "RoundedScaledLauncherMetricsSupported\s*=\s*true",
        "AndroidCompactTouchScaleFloorSupported\s*=\s*true",
        "CompactDynamicContentWidthSupported\s*=\s*true",
        "TabletWideContentLayoutSupported\s*=\s*true",
        "PortalTopAnchoredContentSupported\s*=\s*true",
        "CompactVerticalStatusHeroSupported\s*=\s*true",
        "CompactStackedStatusHeaderSupported\s*=\s*true",
        "CompactLowProfileStatusCardSupported\s*=\s*true",
        "CompactStatusHeadlineRowSupported\s*=\s*true",
        "CompactStackedStatusHeadlineSupported\s*=\s*true",
        "ViewportAwareCompactStatusHeadlineReflowSupported\s*=\s*true",
        "CompactStableStatusDetailRowSupported\s*=\s*true",
        "CompactWorkflowStepStripSupported\s*=\s*true",
        "CompactTwoColumnWorkflowStripSupported\s*=\s*false",
        "CompactSingleRowNumberedWorkflowStripSupported\s*=\s*true",
        "CompactNarrowWorkflowSingleRowSupported\s*=\s*true",
        "CompactVisibleWorkflowStepLabelsSupported\s*=\s*true",
        "CompactWorkflowStepNumberBadgesSupported\s*=\s*true",
        "CompactReadableWorkflowStepNumberBadgesSupported\s*=\s*true",
        "CompactStickyWorkflowStepStripSupported\s*=\s*true",
        "CompactLowProfileWorkflowStepStripSupported\s*=\s*true",
        "CompactLowProfileTwoColumnWorkflowStepStripSupported\s*=\s*false",
        "CompactWorkflowStepDirectNavigationSupported\s*=\s*true",
        "CompactCurrentTaskJumpSupported\s*=\s*true",
        "CompactStickyCurrentTaskBarSupported\s*=\s*true",
        "CompactLowProfileCurrentTaskBarSupported\s*=\s*true",
        "CompactDenseInlineCurrentTaskBarSupported\s*=\s*true",
        "CompactCurrentTaskSharedTouchHeightSupported\s*=\s*true",
        "CompactLowProfileStackedCurrentTaskBarSupported\s*=\s*true",
        "CompactCurrentTaskContextLabelsSupported\s*=\s*true",
        "CompactStructuredCurrentTaskLabelsSupported\s*=\s*true",
        "CompactCurrentTaskShortTitleLabelsSupported\s*=\s*true",
        "CompactTouchSafeStickyHeaderControlsSupported\s*=\s*true",
        "CompactGroupedStickyTaskHeaderSupported\s*=\s*true",
        "CompactStickyTaskToolbarShellSupported\s*=\s*true",
        "CompactInlineStickyTaskHeaderSupported\s*=\s*true",
        "CompactResponsiveStickyTaskHeaderSupported\s*=\s*true",
        "ViewportAwareStickyTaskHeaderReflowSupported\s*=\s*true",
        "ViewportAwareCompactTaskReanchorSupported\s*=\s*true",
        "CompactDenseStickyTaskHeaderSupported\s*=\s*true",
        "CompactTaskJumpNavigationLabelsSupported\s*=\s*true",
        "CompactReadableDetailLabelFontSupported\s*=\s*true",
        "CompactTouchSafeConfirmationDialogsSupported\s*=\s*true",
        "CompactScrollSafeConfirmationDialogsSupported\s*=\s*true",
        "CompactContextualConfirmationLabelsSupported\s*=\s*true",
        "ViewportAwareConfirmationDialogsSupported\s*=\s*true",
        "CompactSteamGuardLargeInputSupported\s*=\s*true",
        "CompactSteamGuardActionFirstSupported\s*=\s*true",
        "CompactSteamGuardInlineActionRowSupported\s*=\s*true",
        "CompactResponsiveSteamGuardActionLayoutSupported\s*=\s*true",
        "ViewportAwareCompactSteamGuardActionRowReflowSupported\s*=\s*true",
        "CompactSteamGuardSubmitDetailLabelSupported\s*=\s*true",
        "CompactSteamGuardRetryGuidanceSupported\s*=\s*true",
        "CompactSteamGuardBoundedHelperSupported\s*=\s*true",
        "CompactPrimaryRetryActionSupported\s*=\s*true",
        "CompactStructuredRetryActionLabelsSupported\s*=\s*true",
        "CompactPrimaryLoginActionFirstSupported\s*=\s*true",
        "CompactAndroidLoginPrimaryCtaSupported\s*=\s*true",
        "CompactAndroidLoginDetailLabelSupported\s*=\s*true",
        "CompactAndroidLoginHelperDetailLabelSupported\s*=\s*true",
        "CompactCompletedAuthSectionSuppressionSupported\s*=\s*true",
        "CompactLowProfileSafeFlowToggleSupported\s*=\s*true",
        "TouchFirstActionTargetsSupported\s*=\s*true",
        "PrimaryActionWordingSupported\s*=\s*true",
        "ConsistentStartGameCtaSupported\s*=\s*true",
        "CompactLaunchDetailLabelSupported\s*=\s*true",
        "BrandedAtmosphericBackgroundSupported\s*=\s*true",
        "BrandedBackgroundExplicitRgbaSupported\s*=\s*true",
        "HighContrastRoundedActionsSupported\s*=\s*true",
        "CompactHeaderChromeReductionSupported\s*=\s*true",
        "CompactCondensedBrandHeaderSupported\s*=\s*true",
        "CompactSingleLineBrandHeaderSupported\s*=\s*true",
        "CompactReadableBrandSubtitleSupported\s*=\s*true",
        "CompactSectionHeaderSubtitleSuppressionSupported\s*=\s*true",
        "CompactLowProfileSectionHeadersSupported\s*=\s*true",
        "CompactSingleRowSectionHeadersSupported\s*=\s*true",
        "CompactSectionHeaderTaskCueSupported\s*=\s*true",
        "CompactReadableSectionHeaderCuesSupported\s*=\s*true",
        "AndroidReadableWarmupScreenSupported\s*=\s*true",
        "AndroidReadableStartupStatusCardSupported\s*=\s*true",
        "CompactInstallPrimaryActionFirstSupported\s*=\s*true",
        "CompactInstallPrimaryDetailLabelSupported\s*=\s*true",
        "CompactDownloadProgressHeroSupported\s*=\s*true",
        "CompactDownloadProgressStatusLabelSupported\s*=\s*true",
        "CompactReadableDownloadProgressBarSupported\s*=\s*true",
        "CompactInlineInstallVersionControlsSupported\s*=\s*true",
        "CompactVersionDetailsCollapsibleSupported\s*=\s*true",
        "CompactVersionDrawerDetailLabelsSupported\s*=\s*true",
        "CompactVersionDrawerBoundedHelpLabelSupported\s*=\s*true",
        "CompactSelectedVersionSummarySupported\s*=\s*true",
        "CompactSelectedVersionHeadlineSupported\s*=\s*true",
        "CompactResponsiveSelectedVersionSummarySupported\s*=\s*true",
        "CompactStructuredInstallVersionActionLabelsSupported\s*=\s*true",
        "CompactReadyVersionSummarySupported\s*=\s*true",
        "CompactReadyVersionSummaryPanelSupported\s*=\s*true",
        "CompactReadyVersionSummaryHeadlineSupported\s*=\s*true",
        "CompactResponsiveReadyVersionSummarySupported\s*=\s*true",
        "CompactPlaySyncDrawerDetailLabelsSupported\s*=\s*true",
        "CompactStructuredPlaySyncActionLabelsSupported\s*=\s*true",
        "CompactReadyStateInstallSectionSuppressionSupported\s*=\s*true",
        "CompactTouchSafeVersionDropdownSupported\s*=\s*true",
        "CompactTouchSafeDropdownPopupSupported\s*=\s*true",
        "CompactCloudSafetyCollapsibleSupported\s*=\s*true",
        "CompactCloudSafetyCueBeforeActionsSupported\s*=\s*true",
        "CompactCloudSafetyDetailLabelSupported\s*=\s*true",
        "CompactCloudOptionsCollapsibleSupported\s*=\s*true",
        "PrimaryCloudActionsBeforeCloudOptionsSupported\s*=\s*true",
        "SaferPullBeforePushOrderingSupported\s*=\s*true",
        "CompactCloudDirectionLabelsSupported\s*=\s*true",
        "CompactCloudPrimaryActionsRowSupported\s*=\s*true",
        "CompactCloudPullDetailLabelSupported\s*=\s*true",
        "CompactCloudPushLockDetailLabelsSupported\s*=\s*true",
        "CompactCloudPushDangerDetailLabelsSupported\s*=\s*true",
        "CompactCloudPushWarningDetailLabelSupported\s*=\s*true",
        "CompactResponsiveActionRowsSupported\s*=\s*true",
        "ManualPushArmedOverwriteWarningSupported\s*=\s*true",
        "CompactButtonLabelsSupported\s*=\s*true",
        "CompactCloudOptionLabelsSupported\s*=\s*true",
        "CompactCloudOptionDetailLabelsSupported\s*=\s*true",
        "CompactCloudOptionsRowSupported\s*=\s*true",
        "CompactDrawerStateResetSupported\s*=\s*true",
        "CompactDrawerToggleFirstSupported\s*=\s*true",
        "CompactLowProfileDrawerTogglesSupported\s*=\s*true",
        "CompactDenseDrawerToggleHeightSupported\s*=\s*true",
        "CompactTouchSafeDrawerToggleSizingSupported\s*=\s*true",
        "CompactSupportToolsGridSupported\s*=\s*true",
        "CompactSupportToolDetailLabelsSupported\s*=\s*true",
        "CompactRawLogReviewLabelSupported\s*=\s*true",
        "CompactDrawerSelectionCollapseSupported\s*=\s*true",
        "CompactActiveSectionScrollSupported\s*=\s*true",
        "CompactPrimaryActionScrollAnchorsSupported\s*=\s*true",
        "CompactPaddedScrollAnchorsSupported\s*=\s*true",
        "CompactBottomScrollBreathingRoomSupported\s*=\s*true",
        "CompactLowProfileAttributionSupported\s*=\s*true",
        "ViewportAwareKeyboardOffsetSupported\s*=\s*true",
        "KeyboardFocusedInputScrollSupported\s*=\s*true",
        "CompactReadyStateSafeFlowSuppressionSupported\s*=\s*true",
        "CompactActiveTaskSafeFlowSuppressionSupported\s*=\s*true",
        "NativeFallbackRecoveryActionsStyledSupported\s*=\s*true",
        "NativeFallbackDiagnosticsCollapsedSupported\s*=\s*true",
        "NativeFallbackResponsiveRecoveryRowsSupported\s*=\s*true",
        "StartupRecoveryScrollSafeControlsSupported\s*=\s*true",
        "VersionInstallCloudSeparationGuidanceSupported\s*=\s*true",
        "DiagnosticsConsoleHiddenByDefault\s*=\s*true",
        "DiagnosticsConsoleAutoOpensForDiagnosticsActionsSupported\s*=\s*true",
        "CompactLowProfileDiagnosticsToggleSupported\s*=\s*true",
        "CompactDiagnosticsToggleDetailLabelsSupported\s*=\s*true",
        "CompactStructuredDiagnosticsToggleLabelsSupported\s*=\s*true",
        "CompactDiagnosticsScrollHostedSupported\s*=\s*true",
        "StartupFallbackRawBannerSuppressed\s*=\s*true",
        "PortalUxDeviceValidated\s*=\s*false",
        "Status-led launcher portal",
        "Steam sign-in",
        "Steam Guard",
        "game install",
        "play/sync",
        "low-profile status card",
        "stacked phase and next-action labels",
        "stable one-line non-attention detail row",
        "compact touch-safe responsive numbered workflow step strip with visible short step labels and separate readable number badges",
        "stays one dense row even on narrow compact viewports",
        "tappable compact workflow steps that directly navigate to visible or fallback task sections",
        "dense inline current-task jump button that uses the shared compact touch-safe control height in a low-profile toolbar shell around the touch-safe sticky compact task header",
        "reflows between inline and stacked layouts after Android rotation or keyboard viewport changes",
        "compact current-task jump button",
        "touch-safe sticky compact task bar",
        "structured two-line GO TO title/detail navigation labels",
        "contextual task detail labels",
        "compact touch-safe confirmation dialogs with wider scroll-safe warning text, contextual confirm/cancel labels, and current-viewport sizing after rotation or keyboard viewport changes",
        "compact Android sign-in primary action before helper copy",
        "compact Android sign-in CTA promoted to a large primary action with SIGN IN WITH STEAM / Android login detail labels before the readable two-line password-manager safety helper",
        "larger compact Steam Guard code entry and VERIFY CODE controls before helper copy, inline where width allows and stacked full-width on narrow compact viewports",
        "compact Steam Guard submit detail labels that keep VERIFY CODE / Submit once action-first",
        "compact Steam Guard bounded two-line helper labels that keep one-shot/no-storage and newest-code retry guidance below the action row",
        "compact failure recovery promoted to a primary TRY AGAIN / Restart task action while cloud controls remain hidden",
        "compact completed-auth section suppression",
        "consistent START GAME primary CTA with compact START GAME / Selected version detail labels",
        "Android compact touch-scale floor for small-device readability",
        "Android-readable shader warmup/loading screen",
        "Android-readable startup status card",
        "verbose native fallback diagnostics collapsed until requested",
        "responsive recovery rows on narrow landscape screens",
        "bounded compact setup safe-flow panel",
        "collapsible compact safe-flow guidance with structured Pull-then-play title/detail labels",
        "compact active-task safe-flow suppression after setup",
        "compact install step ordering with selected-version summary and a large DOWNLOAD VERSION / Local files only primary action before optional version details",
        "compact install primary action detail labels for download, redownload, retry, and disabled downloading states",
        "compact install-version drawer controls with structured title/detail labels that keep the version dropdown and refresh action inline where width allows and stack on narrow compact viewports",
        "collapsible compact version details with structured local-files-only drawer labels and bounded two-line Install target / Launch target helper labels",
        "compact download progress promoted directly under the disabled DOWNLOADING primary action with a stable two-line Downloading selected version status label",
        "compact readable selected-version and ready-version summary cards",
        "compact responsive selected-version summary that stays one-line where space allows and becomes a two-line local-files-only cue on narrow compact viewports",
        "compact ready-version summary",
        "compact ready-version summary panel",
        "compact responsive ready-version summary that stays one-line where space allows and becomes a two-line Pull-first, Push-locked cloud-safety cue on narrow compact viewports",
        "compact Play/Sync drawer detail labels rendered as structured title/detail action labels",
        "compact support tool detail labels including COPY LOG / Review first for raw-log safety",
        "compact responsive action rows that keep Pull/Push, Backup/Sync, and support tools side-by-side where space allows and stack them into full-width controls on narrow compact viewports",
        "compact ready-state install-section suppression",
        "touch-safe compact version dropdowns with larger opened popup row spacing/padding",
        "collapsible compact cloud-safety guidance shown before Pull/Push actions with a stable two-line Cloud target / PULL downloads to Android / PUSH can overwrite Steam detail label",
        "compact cloud action labels that name Android as the Pull destination and Steam as the Push destination",
        "compact Pull detail labels that keep PULL TO ANDROID / Download saves direction-explicit",
        "compact locked Push toggle labels that stay structured as STEAM PUSH LOCKED / Open overwrite and HIDE PUSH / Close overwrite",
        "compact dangerous Push detail labels that keep PUSH TO STEAM / Upload Android and CONFIRM OVERWRITE / Final upload direction-explicit after unlock",
        "compact armed Push overwrite warning rendered as a stable STEAM CLOUD OVERWRITE / Confirm only after Pull/local saves are verified label",
        "compact drawer state reset",
        "compact touch-safe dense drawer toggles before expanded drawer details",
        "compact drawer collapse after version selection",
        "compact state-transition active-section scroll",
        "compact primary-action scroll anchors",
        "padded compact scroll anchors that keep jumped-to task sections below the sticky header",
        "viewport-aware compact task re-anchoring after rotation or keyboard viewport changes",
        "rounded shared metric scaling",
        "compact bottom scroll breathing room",
        "compact low-profile attribution footer",
        "compact scroll-hosted diagnostics with structured touch-safe drawer title/detail labels, a readable bounded compact diagnostics log viewport, and viewport-aware diagnostics log resizing",
        "compact dense vertical rhythm with single-row section headers and readable clipped task cues",
        "mobile-first compact panel sizing with dense compact shell padding",
        "viewport-aware keyboard offset refresh",
        "keyboard-focused input scrolling so managed Steam Guard and fallback credential fields stay reachable above the soft keyboard",
        "compact ready-state safe-flow suppression",
        "scroll-safe startup recovery controls",
        "hidden diagnostics drawer with automatic opening for explicit diagnostics actions",
        "styled native fallback recovery actions",
        "ARM64 visual validation"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.Reports.cs" `
    "exports launcher UI hardening support flags in diagnostics" `
    @(
        "compact cloud option labels supported",
        "CompactCloudOptionLabelsSupported",
        "compact cloud option detail labels supported",
        "CompactCloudOptionDetailLabelsSupported",
        "compact cloud options row supported",
        "CompactCloudOptionsRowSupported",
        "compact stacked status header supported",
        "CompactStackedStatusHeaderSupported",
        "compact low-profile status card supported",
        "CompactLowProfileStatusCardSupported",
        "compact status headline row supported",
        "CompactStatusHeadlineRowSupported",
        "compact stacked status headline supported",
        "CompactStackedStatusHeadlineSupported",
        "viewport-aware compact status headline reflow supported",
        "ViewportAwareCompactStatusHeadlineReflowSupported",
        "compact stable status detail row supported",
        "CompactStableStatusDetailRowSupported",
        "compact workflow step strip supported",
        "CompactWorkflowStepStripSupported",
        "compact two-column workflow step strip supported",
        "CompactTwoColumnWorkflowStripSupported",
        "compact single-row numbered workflow step strip supported",
        "CompactSingleRowNumberedWorkflowStripSupported",
        "compact narrow workflow single-row supported",
        "CompactNarrowWorkflowSingleRowSupported",
        "compact visible workflow step labels supported",
        "CompactVisibleWorkflowStepLabelsSupported",
        "compact sticky workflow step strip supported",
        "CompactStickyWorkflowStepStripSupported",
        "compact low-profile workflow step strip supported",
        "CompactLowProfileWorkflowStepStripSupported",
        "compact workflow step direct navigation supported",
        "CompactWorkflowStepDirectNavigationSupported",
        "compact workflow step number badges supported",
        "CompactWorkflowStepNumberBadgesSupported",
        "compact readable workflow step number badges supported",
        "CompactReadableWorkflowStepNumberBadgesSupported",
        "compact current-task jump supported",
        "CompactCurrentTaskJumpSupported",
        "compact sticky current-task bar supported",
        "CompactStickyCurrentTaskBarSupported",
        "compact low-profile current-task bar supported",
        "CompactLowProfileCurrentTaskBarSupported",
        "compact dense inline current-task bar supported",
        "CompactDenseInlineCurrentTaskBarSupported",
        "compact current-task shared touch height supported",
        "CompactCurrentTaskSharedTouchHeightSupported",
        "compact low-profile stacked current-task bar supported",
        "CompactLowProfileStackedCurrentTaskBarSupported",
        "compact current-task context labels supported",
        "CompactCurrentTaskContextLabelsSupported",
        "compact structured current-task labels supported",
        "CompactStructuredCurrentTaskLabelsSupported",
        "compact current-task short title labels supported",
        "CompactCurrentTaskShortTitleLabelsSupported",
        "compact touch-safe sticky header controls supported",
        "CompactTouchSafeStickyHeaderControlsSupported",
        "compact grouped sticky task header supported",
        "CompactGroupedStickyTaskHeaderSupported",
        "compact sticky task toolbar shell supported",
        "CompactStickyTaskToolbarShellSupported",
        "compact responsive sticky task header supported",
        "CompactResponsiveStickyTaskHeaderSupported",
        "viewport-aware sticky task header reflow supported",
        "ViewportAwareStickyTaskHeaderReflowSupported",
        "viewport-aware compact task re-anchor supported",
        "ViewportAwareCompactTaskReanchorSupported",
        "compact dense sticky task header supported",
        "CompactDenseStickyTaskHeaderSupported",
        "compact task-jump navigation labels supported",
        "CompactTaskJumpNavigationLabelsSupported",
        "compact readable detail label font supported",
        "CompactReadableDetailLabelFontSupported",
        "compact touch-safe confirmation dialogs supported",
        "CompactTouchSafeConfirmationDialogsSupported",
        "compact scroll-safe confirmation dialogs supported",
        "CompactScrollSafeConfirmationDialogsSupported",
        "compact contextual confirmation labels supported",
        "CompactContextualConfirmationLabelsSupported",
        "viewport-aware confirmation dialogs supported",
        "ViewportAwareConfirmationDialogsSupported",
        "compact Steam Guard large input supported",
        "CompactSteamGuardLargeInputSupported",
        "compact Steam Guard action-first layout supported",
        "CompactSteamGuardActionFirstSupported",
        "compact Steam Guard inline action row supported",
        "CompactSteamGuardInlineActionRowSupported",
        "compact responsive Steam Guard action layout supported",
        "CompactResponsiveSteamGuardActionLayoutSupported",
        "viewport-aware compact Steam Guard action row reflow supported",
        "ViewportAwareCompactSteamGuardActionRowReflowSupported",
        "compact Steam Guard submit detail label supported",
        "CompactSteamGuardSubmitDetailLabelSupported",
        "compact Steam Guard retry guidance supported",
        "CompactSteamGuardRetryGuidanceSupported",
        "compact Steam Guard bounded helper supported",
        "CompactSteamGuardBoundedHelperSupported",
        "compact primary retry action supported",
        "CompactPrimaryRetryActionSupported",
        "compact structured retry action labels supported",
        "CompactStructuredRetryActionLabelsSupported",
        "compact primary login action first supported",
        "CompactPrimaryLoginActionFirstSupported",
        "compact Android login primary CTA supported",
        "CompactAndroidLoginPrimaryCtaSupported",
        "compact Android login detail label supported",
        "CompactAndroidLoginDetailLabelSupported",
        "compact Android login helper detail label supported",
        "CompactAndroidLoginHelperDetailLabelSupported",
        "compact completed-auth section suppression supported",
        "CompactCompletedAuthSectionSuppressionSupported",
        "compact dense vertical rhythm supported",
        "CompactDenseVerticalRhythmSupported",
        "Android-readable warmup screen supported",
        "AndroidReadableWarmupScreenSupported",
        "Android-readable startup status card supported",
        "AndroidReadableStartupStatusCardSupported",
        "compact low-profile safe-flow toggle supported",
        "CompactLowProfileSafeFlowToggleSupported",
        "compact safe-flow toggle detail labels supported",
        "CompactSafeFlowToggleDetailLabelsSupported",
        "compact structured safe-flow toggle labels supported",
        "CompactStructuredSafeFlowToggleLabelsSupported",
        "compact safe-flow bounded guide supported",
        "CompactSafeFlowBoundedGuideSupported",
        "compact single-row section headers supported",
        "CompactSingleRowSectionHeadersSupported",
        "compact section-header task cues supported",
        "CompactSectionHeaderTaskCueSupported",
        "compact install primary action first supported",
        "CompactInstallPrimaryActionFirstSupported",
        "compact install primary detail label supported",
        "CompactInstallPrimaryDetailLabelSupported",
        "compact download progress hero supported",
        "CompactDownloadProgressHeroSupported",
        "compact download progress status label supported",
        "CompactDownloadProgressStatusLabelSupported",
        "compact readable download progress bar supported",
        "CompactReadableDownloadProgressBarSupported",
        "compact inline install-version controls supported",
        "CompactInlineInstallVersionControlsSupported",
        "compact selected-version summary supported",
        "CompactSelectedVersionSummarySupported",
        "compact version summary cards supported",
        "CompactVersionSummaryCardsSupported",
        "compact selected-version headline supported",
        "CompactSelectedVersionHeadlineSupported",
        "compact responsive selected-version summary supported",
        "CompactResponsiveSelectedVersionSummarySupported",
        "compact structured install-version action labels supported",
        "CompactStructuredInstallVersionActionLabelsSupported",
        "compact ready-version summary supported",
        "CompactReadyVersionSummarySupported",
        "compact ready-version summary panel supported",
        "CompactReadyVersionSummaryPanelSupported",
        "compact ready-version headline supported",
        "CompactReadyVersionSummaryHeadlineSupported",
        "compact responsive ready-version summary supported",
        "CompactResponsiveReadyVersionSummarySupported",
        "compact ready-state install-section suppression supported",
        "CompactReadyStateInstallSectionSuppressionSupported",
        "compact touch-safe version dropdown supported",
        "CompactTouchSafeVersionDropdownSupported",
        "compact touch-safe dropdown popup supported",
        "CompactTouchSafeDropdownPopupSupported",
        "compact cloud-safety cue before actions supported",
        "CompactCloudSafetyCueBeforeActionsSupported",
        "compact cloud-safety detail label supported",
        "CompactCloudSafetyDetailLabelSupported",
        "compact cloud direction labels supported",
        "CompactCloudDirectionLabelsSupported",
        "compact cloud primary actions row supported",
        "CompactCloudPrimaryActionsRowSupported",
        "compact Pull detail label supported",
        "CompactCloudPullDetailLabelSupported",
        "compact locked Push detail labels supported",
        "CompactCloudPushLockDetailLabelsSupported",
        "compact dangerous Push detail labels supported",
        "CompactCloudPushDangerDetailLabelsSupported",
        "compact armed Push warning detail label supported",
        "CompactCloudPushWarningDetailLabelSupported",
        "compact responsive action rows supported",
        "CompactResponsiveActionRowsSupported",
        "compact drawer state reset supported",
        "CompactDrawerStateResetSupported",
        "compact drawer toggle-first ordering supported",
        "CompactDrawerToggleFirstSupported",
        "compact dense drawer toggle height supported",
        "CompactDenseDrawerToggleHeightSupported",
        "compact touch-safe drawer toggle sizing supported",
        "CompactTouchSafeDrawerToggleSizingSupported",
        "compact support tools grid supported",
        "CompactSupportToolsGridSupported",
        "compact raw-log review label supported",
        "CompactRawLogReviewLabelSupported",
        "compact drawer selection collapse supported",
        "CompactDrawerSelectionCollapseSupported",
        "compact active-section scroll supported",
        "CompactActiveSectionScrollSupported",
        "compact primary-action scroll anchors supported",
        "CompactPrimaryActionScrollAnchorsSupported",
        "compact padded scroll anchors supported",
        "CompactPaddedScrollAnchorsSupported",
        "compact bottom scroll breathing room supported",
        "CompactBottomScrollBreathingRoomSupported",
        "compact dense panel padding supported",
        "CompactDensePanelPaddingSupported",
        "compact dense vertical rhythm supported",
        "CompactDenseVerticalRhythmSupported",
        "rounded scaled metrics supported",
        "RoundedScaledLauncherMetricsSupported",
        "compact low-profile attribution footer supported",
        "CompactLowProfileAttributionSupported",
        "viewport-aware keyboard offset supported",
        "ViewportAwareKeyboardOffsetSupported",
        "keyboard-focused input scroll supported",
        "KeyboardFocusedInputScrollSupported",
        "compact ready-state safe-flow suppression supported",
        "CompactReadyStateSafeFlowSuppressionSupported",
        "compact active-task safe-flow suppression supported",
        "CompactActiveTaskSafeFlowSuppressionSupported",
        "native fallback recovery actions styled",
        "NativeFallbackRecoveryActionsStyledSupported",
        "native fallback diagnostics collapsed by default",
        "NativeFallbackDiagnosticsCollapsedSupported",
        "native fallback responsive recovery rows supported",
        "NativeFallbackResponsiveRecoveryRowsSupported",
        "startup recovery scroll-safe controls supported",
        "StartupRecoveryScrollSafeControlsSupported",
        "diagnostics console auto-opens for diagnostics actions",
        "DiagnosticsConsoleAutoOpensForDiagnosticsActionsSupported",
        "compact diagnostics toggle detail labels supported",
        "CompactDiagnosticsToggleDetailLabelsSupported",
        "compact structured diagnostics toggle labels supported",
        "CompactStructuredDiagnosticsToggleLabelsSupported",
        "compact readable diagnostics log supported",
        "CompactReadableDiagnosticsLogSupported",
        "compact bounded diagnostics log viewport supported",
        "CompactBoundedDiagnosticsLogViewportSupported",
        "viewport-aware diagnostics log resize supported",
        "ViewportAwareDiagnosticsLogResizeSupported"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.FmodAttribution.cs" `
    "keeps compact FMOD attribution low-profile without expanding the phone layout" `
    @(
        "BuildFmodAttributionSection\(float scale, bool compact\)",
        "Control\.SizeFlags\.ShrinkBegin",
        "Control\.SizeFlags\.ExpandFill",
        "if \(!compact\)",
        "CompactFmodCreditFontSize",
        "AutowrapMode"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Behavior.cs" `
    "refreshes keyboard offset anchor after viewport size changes" `
    @(
        "_panelBaseY = _panel\.Position\.Y \+ _keyboardOffset",
        "_panel\.UpdateSizeFromViewport",
        "UpdateKeyboardOffset\(\)",
        "_keyboardOffset = Math\.Min",
        "_panelBaseY - _keyboardOffset",
        "_keyboardOffset = 0f"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Behavior.cs" `
    "scrolls focused managed inputs above the Android keyboard" `
    @(
        "ScrollFocusedInputAboveKeyboard",
        "GuiGetFocusOwner",
        "PrimaryScroll\.IsAncestorOf\(focusOwner\)",
        "PrimaryScroll\.EnsureControlVisible\(focusOwner\)",
        "GodotObject\.IsInstanceValid\(focusOwner\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.cs" `
    "tracks mutable keyboard offset state for viewport-aware Android keyboard avoidance" `
    @(
        "private float _panelBaseY",
        "private float _keyboardOffset",
        "private Control _keyboardFocusScrollTarget",
        "private float _keyboardFocusScrollOffset"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Components\StyledPanel.cs" `
    "uses a framed launcher shell rather than a flat unbounded panel" `
    @(
        "PanelBackground",
        "BorderColor",
        "SetBorderWidthAll",
        "PanelRadius"
    )

Add-Check `
    "src\STS2Mobile\ModEntry.cs" `
    "suppresses raw startup fallback banner behind the launcher portal" `
    @(
        "Startup fallback raw banner suppressed",
        "launcher diagnostics retain the startup failure detail"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.cs" `
    "reveals the framed play/sync action section when launch or retry actions are available" `
    @(
        "internal void ShowLaunch",
        "internal void ShowRetry",
        "internal void HideAll",
        "Visible = true",
        "Visible = false",
        "SetCloudControlsVisible",
        "ShowLaunchButtons",
        "ShowRetryButtons",
        "HideSecondaryButtons"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.cs" `
    "uses explicit Steam Cloud direction and overwrite-risk wording in the portal" `
    @(
        "_pushPullRow",
        "_pushButton",
        "_confirmPushButton",
        "_pushConfirmationLabel",
        "PUSH LOCKED",
        "CompactCloudPushDangerText\(\)",
        "CompactCloudPushConfirmText\(\)",
        "CompactCloudPushWarningText\(\)",
        "Pull Saves from Steam Cloud",
        "CloudPushArmRequested",
        "CloudPushArmRequested\?\.Invoke\(\) == false",
        "ArmCloudPush",
        "ConfirmCloudPush",
        "ResetCloudPushArm",
        "can overwrite remote Steam Cloud saves"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.cs" `
    "uses explicit Steam Cloud direction wording in ready/action state" `
    @(
        "_readyVersionSummaryLabel",
        "_readyVersionSummaryPanel",
        "BuildReadyVersionSummaryStyle",
        "Ready version:",
        "START GAME and Pull/Push use this version",
        "Push stays locked until explicitly opened",
        "SteamGameInstallPaths\.VersionSlotKind",
        "Pull copies Steam Cloud saves to Android",
        "Push copies Android saves to Steam Cloud",
        "can overwrite remote saves",
        "Version/download actions affect local game files only",
        "Steam Cloud saves move only through Pull/Push",
        "_cloudOptionsExpanded = false",
        "_branchDetailsExpanded = false",
        "_cloudPushExpanded = false"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Toggles.cs" `
    "uses compact cloud option labels that fit phone touch targets" `
    @(
        "_compact",
        "CompactCloudOptionText",
        "BACKUP",
        "Local saves",
        "SYNC",
        "Game cloud",
        "Local Backup: \{OnOff\(value\)\}",
        "Game Cloud Sync: \{OnOff\(value\)\}"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.cs" `
    "packs compact Backup and Sync cloud options into responsive action rows" `
    @(
        "BuildCompactCloudOptionsRow",
        "compactStackedActionRows",
        "CompactCloudOptionToggleSeparation",
        "_compactCloudOptionsRow = BuildCompactCloudOptionsRow\(_cloudGroup, scale, _compactStackedActionRows\)",
        "AddCompactSupportToolButton\(cloudOptionsParent, ""BACKUP OFF""",
        "AddCompactSupportToolButton\(cloudOptionsParent, ""SYNC OFF""",
        "Visible = false",
        "new VBoxContainer\(\) : new HBoxContainer\(\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.cs" `
    "shows cloud-save safety guidance beside Pull/Push actions" `
    @(
        "_cloudSafetyLabel",
        "_cloudGroup.AddChild\(_pushPullRow\)",
        "OrangeHot",
        "Pull Saves from Steam Cloud",
        "PUSH LOCKED",
        "CompactCloudPushConfirmText\(\)",
        "BACKUP OFF",
        "SYNC OFF",
        "SHOW CLOUD OPTIONS"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.cs" `
    "moves compact cloud-safety cue before Pull/Push controls" `
    @(
        "MoveCompactCloudSafetyCueBeforeCloudActions",
        "_cloudGroup\.MoveChild\(_cloudSafetyLabel, _pushPullRow\.GetIndex\(\)\)",
        "_cloudGroup\.MoveChild\(_cloudSafetyToggle, _cloudSafetyLabel\.GetIndex\(\)\)",
        "CompactCloudSafetySummary\(\)",
        "CompactCloudSafetyDetailHeight",
        "CompactCloudSafetyDetailFontSize",
        "_cloudSafetyLabel\.ClipText = compact",
        "_cloudSafetyLabel\.TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
        "_cloudSafetyLabel\.CustomMinimumSize = new Vector2",
        "_cloudGroup.AddChild\(_pushPullRow\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\CodeSection.cs" `
    "uses alphanumeric keyboard and normalization for Steam Guard code entry" `
    @(
        "VirtualKeyboardType\.Default",
        "Enter Steam Guard code",
        "TextChanged \+= NormalizeCodeText",
        "NormalizeCode\(string text\)",
        "char\.IsLetterOrDigit",
        "char\.ToUpperInvariant",
        "CodeSubmitted"
    )

Add-Check `
    "android\src\com\game\sts2launcher\GodotApp.java" `
    "provides integrated native Steam credential panel with Android credential-provider hints" `
    @(
        "showSteamLoginCredentialPanel",
        "hideSteamLoginCredentialPanel",
        "consumeSteamLoginCredentialResult",
        "SteamLoginCredentialEditText",
        "AUTOFILL_HINT_USERNAME",
        "AUTOFILL_HINT_PASSWORD",
        "setContentDescription",
        "setSaveEnabled\(false\)",
        "requestSteamLoginCredentialAutofill",
        "requestSteamLoginCredentialAutofillField",
        "setOnFocusChangeListener",
        "requestSteamLoginCredentialAutofillField\(steamLoginCredentialUsernameField\)",
        "requestSteamLoginCredentialAutofillField\(steamLoginCredentialPasswordField\)",
        "autofillManager\.requestAutofill\(field\)",
        "SIGN IN WITH STEAM",
        "NEXT: PASSWORD",
        "focusSteamLoginPasswordField",
        "cancelSteamLoginCredentialAutofillSession",
        "autofillManager\.cancel\(\)",
        "SHOW PASSWORD",
        "HIDE PASSWORD",
        "toggleSteamLoginCredentialPasswordVisibility",
        "TYPE_TEXT_VARIATION_VISIBLE_PASSWORD",
        "boolean wideCredentialLayout = useSteamLoginCredentialWideLayout\(\)",
        "steamLoginCredentialShortHeightLayout",
        "boolean shortHeightCredentialLayout = wideCredentialLayout && useSteamLoginCredentialShortHeightLayout\(\)",
        "useSteamLoginCredentialWideLayout",
        "width > height && width >= dp\(640\)",
        "useSteamLoginCredentialShortHeightLayout",
        "height < dp\(430\)",
        "steamLoginCredentialUsableHeightPixels",
        "getWindow\(\)",
        "getDecorView\(\)",
        "getWindowVisibleDisplayFrame\(visibleFrame\)",
        "visibleWideLayout != steamLoginCredentialWideLayout",
        "visibleShortHeightLayout != steamLoginCredentialShortHeightLayout",
        "shortHeightCredentialLayout == steamLoginCredentialShortHeightLayout",
        "translateSteamLoginCredentialStatusForLayout",
        "steamLoginCredentialDefaultStatusText\(boolean shortHeightLayout\)",
        "steamLoginCredentialShownStatusText\(boolean shortHeightLayout\)",
        "Use Android password suggestions here\. Credentials clear after one Steam handoff\.",
        "Not stored by StS2 Mobile\.",
        "steamLoginCredentialDefaultStatusText",
        "steamLoginCredentialShownStatusText",
        "steamLoginCredentialPanelWidth\(wideCredentialLayout\)",
        "createSteamLoginCredentialInputRow\(wideCredentialLayout\)",
        "credentialFieldLayoutParams\(wideCredentialLayout\)",
        "credentialInlineActionLayoutParams\(wideCredentialLayout, 4\)",
        "credentialInlineActionLayoutParams\(wideCredentialLayout, 6\)",
        "credentialSubmitButtonLayoutParams\(wideCredentialLayout\)",
        "credentialCancelButtonLayoutParams\(wideCredentialLayout\)",
        "new LinearLayout\.LayoutParams\(dp\(178\), LinearLayout\.LayoutParams\.WRAP_CONTENT\)",
        "onConfigurationChanged",
        "Configuration newConfig",
        "reflowSteamLoginCredentialPanelForCurrentWindow",
        "wideCredentialLayout == steamLoginCredentialWideLayout",
        "clearSteamLoginCredentialVisibleFieldText",
        "parent\.removeView\(steamLoginCredentialOverlay\)",
        "clearSteamLoginCredentialViewReferences",
        "setSteamLoginCredentialPasswordVisibilityState",
        "onBackPressed",
        "dispatchKeyEvent",
        "KEYCODE_BACK",
        "dismissSteamLoginCredentialPanelFromBack",
        "isSteamLoginCredentialPanelVisible",
        "public boolean isSteamLoginCredentialPanelVisible",
        "hideKeyboardForSteamLoginCredentialPanel",
        "focusedView\.clearFocus\(\)",
        "styleSteamLoginCredentialButton",
        "field\.setMinHeight\(dp\(56\)\)",
        "field\.setPadding\(dp\(14\), 0, dp\(14\), 0\)",
        "field\.setBackground\(background\)",
        "button\.setMinHeight\(dp\(primary \? 60 : 56\)\)",
        "ScrollView",
        "steamLoginCredentialScrollView",
        "updateSteamLoginCredentialKeyboardInsets",
        "scheduleSteamLoginCredentialFocusScroll",
        "Gravity\.TOP \| Gravity\.CENTER_HORIZONTAL",
        "scroll\.setClipToPadding\(false\)",
        "buttons\.setOrientation\(wideCredentialLayout \? LinearLayout\.HORIZONTAL : LinearLayout\.VERTICAL\)",
        "buttonLayoutParams",
        "setSteamLoginCredentialStatus",
        "Android password suggestions may appear",
        "Enter your Steam username to continue",
        "Enter your Steam password to continue",
        "Submitting to Steam",
        "IME_ACTION_NEXT",
        "IME_ACTION_DONE",
        "setWebDomain",
        "structure\.setHint",
        "store\.steampowered\.com",
        "Steam password is never stored by StS2 Mobile",
        "pendingSteamLoginCredentialUsername",
        "pendingSteamLoginCredentialPassword",
        "clearSteamLoginCredentialPanelSensitiveFields",
        'steamLoginCredentialUsernameField\.setText\(\"\"\)',
        'steamLoginCredentialPasswordField\.setText\(\"\"\)',
        "STEAM_LOGIN_CREDENTIAL_RESULT_TTL_MS",
        "pendingSteamLoginCredentialExpiresAtMs",
        "clearPendingSteamLoginCredentialsLocked",
        "Native Steam login credential panel shown",
        "Native Steam login credentials submitted to managed login flow"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.StartupRecoveryReports.cs" `
    "adds public-sharing warning before startup recovery diagnostics content" `
    @(
        "AppendStartupRecoveryDiagnostics",
        "AppendPublicSharingWarning",
        "Data dir:"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.Construction.cs" `
    "labels startup recovery raw-log copy as review-before-sharing" `
    @(
        "COPY RAW LOG \(REVIEW BEFORE SHARING\)",
        "Raw logs can contain identifying data",
        "review/redact before sharing"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.Construction.cs" `
    "keeps startup recovery controls reachable with a scroll-safe Android layout" `
    @(
        "CreateScrollContainer",
        "ScrollContainer",
        "FollowFocus = true",
        "SetAnchorsPreset\(Control\.LayoutPreset\.FullRect\)",
        "CreateFrame",
        "MarginContainer",
        "RecoveryMargin",
        "RecoveryTopMargin",
        "UseCompactRecoveryCopy",
        "OperatingSystem\.IsAndroid\(\)",
        "SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill",
        "scroll\.AddChild\(frame\)",
        "frame\.AddChild\(box\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.cs" `
    "labels launcher support raw-log copy as review-before-sharing" `
    @(
        "COPY RAW LOG \(REVIEW BEFORE SHARING\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.Diagnostics.cs" `
    "warns before raw launcher error logs are copied for sharing" `
    @(
        "Public sharing warning",
        "review and redact this raw error log before posting publicly",
        "Review/redact before public posting",
        "Raw error log copied"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.Reports.cs" `
    "warns after startup recovery raw error logs are copied" `
    @(
        "Raw error log copied to clipboard",
        "Review/redact before public posting"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.Reports.cs" `
    "reports selected branch, marker, cache, and backup state" `
    @(
        "Selected game branch",
        "Selected game branch preference key",
        "Selected game branch source",
        "Selected game branch selection kind",
        "Selected game version note",
        "Steam branch selector mode",
        "Steam beta password entry supported",
        "Android credential provider model",
        "Native credential panel inline status configured",
        "Native credential panel keyboard-safe layout configured",
        "Native credential panel touch-target layout configured",
        "Native credential panel large field targets supported",
        "Native credential panel requests both Autofill fields",
        "Native credential panel focus Autofill requests supported",
        "Native credential panel task-led buttons supported",
        "Native credential panel responsive action rows supported",
        "Native credential panel orientation reflow supported",
        "Native credential panel short-height copy supported",
        "Native credential panel short-height reflow supported",
        "Native credential panel IME height reflow supported",
        "Native credential panel password visibility toggle supported",
        "Steam Guard one-shot code guidance supported",
        "Steam Guard alphanumeric keyboard supported",
        "Failed-login retry guidance supported",
        "Context-specific login recovery guidance supported",
        "Godot login field credential metadata configured",
        "Android keyboard credential hints configured",
        "Godot fields are native Android Autofill targets",
        "Password-manager suggestions device validated",
        "Native credential handoff popup supported",
        "Launcher stores Steam password for credential providers",
        "Native credential handoff result TTL seconds",
        "Android credential provider implementation note",
        "Android credential provider capability boundary",
        "Launcher portal UX model",
        "Launcher status-led portal supported",
        "Launcher phase-labeled status supported",
        "Launcher structured status chip supported",
        "Launcher guided next-action status supported",
        "Launcher error-first guided status supported",
        "Launcher titled state sections supported",
        "Launcher safe first-run guidance supported",
        "Launcher compact safe-flow guidance collapsible",
        "Launcher compact safe-flow toggle detail labels supported",
        "Launcher compact structured safe-flow toggle labels supported",
        "Launcher compact safe-flow bounded guide supported",
        "Launcher compact active-task safe-flow suppression supported",
        "Launcher mobile-first compact layout supported",
        "Launcher compact dense panel padding supported",
        "Launcher Android compact touch-scale floor supported",
        "Launcher compact dynamic content width supported",
        "Launcher tablet/wide content layout supported",
        "Launcher top-anchored portal content supported",
        "Launcher compact vertical status hero supported",
        "Launcher compact Steam Guard retry guidance supported",
        "Launcher compact Steam Guard bounded helper supported",
        "Launcher compact primary retry action supported",
        "Launcher compact structured retry action labels supported",
        "Launcher compact completed-auth section suppression supported",
        "Launcher touch-first action targets supported",
        "Launcher primary action wording supported",
        "Launcher consistent START GAME CTA supported",
        "Launcher compact launch detail label supported",
        "Launcher branded atmospheric background supported",
        "Launcher branded background explicit RGBA supported",
        "Launcher high-contrast rounded actions supported",
        "Launcher compact header chrome reduction supported",
        "Launcher compact condensed brand header supported",
        "Launcher compact single-line brand header supported",
        "Launcher compact readable brand subtitle supported",
        "Launcher compact section-header subtitle suppression supported",
        "Launcher compact low-profile section headers supported",
        "Launcher compact section-header task cues supported",
        "Launcher compact readable section-header cues supported",
        "Launcher compact install primary detail label supported",
        "Launcher compact version details collapsible",
        "Launcher compact version drawer bounded help label supported",
        "Launcher compact structured install-version action labels supported",
        "Launcher compact version summary cards supported",
        "Launcher compact ready-version summary panel supported",
        "Launcher compact ready-version headline supported",
        "Launcher compact responsive ready-version summary supported",
        "Launcher compact structured Play/Sync action labels supported",
        "Launcher compact ready-state install-section suppression supported",
        "Launcher compact cloud-safety guidance collapsible",
        "Launcher compact cloud-safety detail label supported",
        "Launcher compact cloud options collapsible",
        "Launcher primary cloud actions before cloud options",
        "Launcher compact cloud option detail labels supported",
        "Launcher safer Pull-before-Push cloud ordering supported",
        "Launcher compact dangerous Push detail labels supported",
        "Launcher compact armed Push warning detail label supported",
        "Launcher manual Push armed overwrite warning supported",
        "Launcher version-install/cloud-save separation guidance supported",
        "Launcher diagnostics console hidden by default",
        "Launcher diagnostics console auto-opens for diagnostics actions",
        "Launcher compact low-profile drawer toggles supported",
        "Launcher compact support tools grid supported",
        "Launcher compact low-profile diagnostics toggle supported",
        "Launcher compact diagnostics toggle detail labels supported",
        "Launcher compact structured diagnostics toggle labels supported",
        "Launcher compact diagnostics scroll-hosted supported",
        "Launcher startup fallback raw banner suppressed",
        "Launcher portal UX device validated",
        "Launcher portal UX implementation note",
        "Launcher portal UX validation boundary",
        "SteamKit debug logs opt-in enabled",
        "SteamKit debug logs sanitized for credentials/tokens",
        "Public sharing warning",
        "review and redact this diagnostics report before posting publicly",
        "Selected game version slot kind",
        "Selected game version slot directory",
        "Selected game branch marker install slot kind",
        "Selected game branch marker install slot directory",
        "Selected game branch marker expected install slot kind",
        "Selected game branch marker expected install slot directory",
        "Selected game branch marker has matching install slot provenance",
        "Selected game branch marker has depot manifests",
        "Selected game branch marker depot manifest entries",
        "Selected game branch marker ready",
        "Steam branch availability marker filename",
        "Steam branch availability marker path",
        "Steam branch availability marker present",
        "Steam branch availability UTC",
        "Steam branch availability selected branch",
        "Steam branch availability matches current selected branch",
        "Steam branch availability selected branch visibility",
        "Steam branch availability selected branch Windows depot manifests",
        "Steam branch availability selected branch downloadable",
        "Steam branch availability selected branch problem",
        "BranchAvailabilitySelectedBranchPasswordProtected",
        "passwordRequired=true",
        "selected branch is password-protected",
        "Steam branch availability visible branch count",
        "Steam branch availability visible branches",
        "branchMarkerExpectedInstallSlotKind",
        "branchMarkerExpectedInstallSlotDirectory",
        "branchMarkerMatchingInstallSlotProvenance",
        "importantSaveEvidenceCount",
        "Current selected branch for version marker comparison",
        "Game version cache cleanup marker filename",
        "Game version cache cleanup marker path",
        "Game version cache cleanup marker present",
        "Game version cache cleanup marker UTC",
        "Game version cache cleanup marker UTC parseable",
        "Game version cache cleanup marker selected branch",
        "Game version cache cleanup marker matches selected branch",
        "Game version cache cleanup marker selected version",
        "Game version cache cleanup marker selected version slot kind",
        "Game version cache cleanup marker selected version slot directory",
        "Game version cache cleanup marker game_versions present",
        "Game version cache cleanup marker runtime_packs present",
        "Game version cache cleanup marker selected runtime pack directory",
        "Game version cache cleanup marker selected runtime pack present before cleanup",
        "Game version cache cleanup marker removed count",
        "Game version cache cleanup marker removed runtime pack count",
        "Game version cache cleanup marker selected cache preserved where applicable",
        "Game version cache cleanup marker selected runtime pack preserved where applicable",
        "Game version redownload marker filename",
        "Game version redownload marker path",
        "Game version redownload marker present",
        "Game version redownload marker UTC parseable",
        "Game version redownload marker selected branch",
        "Game version redownload marker matches selected branch",
        "Game version redownload marker selected version",
        "Game version redownload marker selected version slot kind",
        "Game version redownload marker selected version slot directory",
        "Game version redownload marker game directory existed before delete",
        "Game version redownload marker game directory exists after delete",
        "Game version redownload marker download state directory existed before delete",
        "Game version redownload marker download state directory exists after delete",
        "Game version redownload marker selected directories cleared",
        "Branch switch marker filename",
        "Branch switch marker path",
        "Branch switch marker UTC",
        "Branch switch marker UTC parseable",
        "previous branch",
        "Branch switch selected branch",
        "Branch switch selected branch selection kind",
        "Branch switch selector mode",
        "Branch switch selected version",
        "Branch switch selected version slot kind",
        "Branch switch selected version slot directory",
        "Branch switch selected branch matches current selected branch",
        "Branch switch selected branch note",
        "Branch switch local backup forced",
        "Branch switch manual Push requires backup storage",
        "Branch switch warning acknowledged",
        "Branch switch non-public warning acknowledged",
        "Branch switch marker has required safety evidence",
        "Branch switch marker has required safety evidence for selected branch",
        "Manual Pull evidence marker filename",
        "Manual Pull evidence marker path",
        "Manual Pull evidence UTC",
        "Manual Pull evidence UTC parseable",
        "Manual Pull evidence selected branch",
        "Manual Pull evidence selected branch selection kind",
        "Manual Pull evidence selector mode",
        "Manual Pull evidence selected version",
        "Manual Pull evidence selected version slot kind",
        "Manual Pull evidence selected version slot directory",
        "Manual Pull completion flag recorded",
        "Manual Pull completed before Push",
        "Manual Pull evidence is after branch switch",
        "Manual Pull evidence matches selected branch",
        "Manual Pull completed after branch switch for selected version",
        "Manual Push evidence marker filename",
        "Manual Push evidence marker path",
        "Manual Push evidence UTC",
        "Latest manual Push evidence outcome",
        "Latest manual Push evidence UTC",
        "Latest manual Push evidence selected branch",
        "Latest manual Push evidence selected branch selection kind",
        "Latest manual Push evidence selector mode",
        "Latest manual Push evidence selected version",
        "Latest manual Push evidence selected version slot kind",
        "Latest manual Push evidence selected version slot directory",
        "Latest manual Push evidence reason",
        "Manual Push evidence UTC parseable",
        "Manual Push evidence selected branch",
        "Manual Push evidence selected version",
        "Manual Push evidence selected version slot kind",
        "Manual Push evidence selected version slot directory",
        "Manual Push evidence recorded local backup count",
        "Manual Push evidence recorded cloud backup count",
        "Manual Push evidence recorded latest local backup UTC",
        "Manual Push evidence recorded latest cloud backup UTC",
        "Manual Push evidence recorded important local save evidence count",
        "Manual Push evidence recorded baseline prerequisites satisfied",
        "Manual Push completion flag recorded",
        "Manual Push evidence is after branch switch",
        "Manual Push evidence matches selected branch",
        "Manual Push evidence recorded pre-Push backup evidence satisfied",
        "Manual Push completed after branch switch for selected version with backup evidence",
        "LatestManualPushEvidenceOutcome",
        "Manual Push blocked evidence marker filename",
        "Manual Push blocked evidence marker path",
        "Manual Push blocked evidence UTC",
        "Manual Push blocked evidence UTC parseable",
        "Manual Push blocked evidence selected branch",
        "Manual Push blocked evidence selected version",
        "Manual Push blocked evidence selected version slot kind",
        "Manual Push blocked evidence selected version slot directory",
        "Manual Push blocked evidence matches selected branch",
        "Manual Push blocked evidence recorded prerequisites satisfied",
        "Manual Push blocked evidence recorded local backup count",
        "Manual Push blocked evidence recorded cloud backup count",
        "Manual Push blocked evidence recorded latest local backup UTC",
        "Manual Push blocked evidence recorded latest cloud backup UTC",
        "Manual Push blocked evidence recorded important local save evidence count",
        "Manual Push blocked evidence recorded baseline prerequisites satisfied",
        "Manual Push blocked evidence recorded pre-Push backup evidence satisfied",
        "Manual Push blocked evidence reason",
        "Manual Push blocked before upload evidence recorded",
        "Important Android local save evidence count in bounded scan",
        "Important Android local save evidence present",
        "branchMarkerReady",
        "Push requires backup storage after branch switch",
        "Branch-switch manual Push prerequisites satisfied",
        "Pre-Push local backup evidence count",
        "Pre-Push cloud backup evidence count",
        "Latest pre-Push local backup UTC",
        "Latest pre-Push cloud backup UTC",
        "Pre-Push local backup evidence after branch switch",
        "Pre-Push cloud backup evidence after branch switch",
        "Branch-switch pre-Push backup evidence satisfied"
    )

Add-Check `
    "android\src\com\game\sts2launcher\GodotApp.java" `
    "routes native startup through selected branch readiness checks" `
    @(
        "Selected Steam branch",
        "Selected Steam branch note",
        "SteamBranchInfo\.selectorHelpText",
        "SteamBranchInfo\.gameDirectory",
        "Resolved startup game directory",
        "steam_branch\.txt",
        "hasInstallSlotProvenance",
        "Steam branch marker install slot kind",
        "Steam branch marker expected install slot kind",
        "Steam branch marker install slot directory",
        "Steam branch marker expected install slot directory",
        "Steam branch marker has matching install slot provenance",
        "depotManifestCount"
    )

Add-Check `
    "android\src\com\game\sts2launcher\SteamBranchInfo.java" `
    "centralizes native selected-branch guidance and branch cache directory naming" `
    @(
        "selectorHelpText",
        "installSlotKind",
        "installSlotDirectory",
        "gameDirectory",
        "stateDirectoryName",
        "stableBranchHash",
        "BETA_BRANCH",
        "Default/public Steam branch",
        "Private/password-protected branches may be inaccessible",
        "Failed downloads do not change Steam Cloud saves"
    )

Add-Check `
    "android\src\com\game\sts2launcher\LauncherActivity.java" `
    "logs selected branch guidance before native routing" `
    @(
        "logSelectedBranchBeforeRouting",
        "Selected Steam branch before routing",
        "Selected Steam branch note before routing",
        "SteamBranchInfo\.selectorHelpText",
        "Selected game version slot kind before routing",
        "Selected game version slot directory before routing",
        "SteamBranchInfo\.installSlotDirectory",
        "SteamBranchInfo\.gameDirectory",
        "hasInstallSlotProvenance",
        "Resolved game directory before routing",
        "Steam branch marker install slot kind before routing",
        "Steam branch marker expected install slot kind before routing",
        "Steam branch marker install slot directory before routing",
        "Steam branch marker expected install slot directory before routing",
        "Steam branch marker has matching install slot provenance before routing",
        "Steam branch marker depot manifest entries before routing",
        "Steam branch marker ready before routing"
    )

Add-Check `
    "android\src\com\game\sts2launcher\NativeFallbackActivity.java" `
    "shows branch marker readiness in native fallback diagnostics" `
    @(
        "steam_branch\.txt",
        "Selected Steam branch note",
        "SteamBranchInfo\.selectorHelpText",
        "SteamBranchInfo\.gameDirectory",
        "branch marker",
        "hasInstallSlotProvenance",
        "Steam branch marker install slot kind",
        "Steam branch marker expected install slot kind",
        "Steam branch marker install slot directory",
        "Steam branch marker expected install slot directory",
        "Steam branch marker has matching install slot provenance",
        "depotManifestCount"
    )

Add-Check `
    "android\src\com\game\sts2launcher\NativeFallbackActivity.java" `
    "keeps native fallback recovery controls visible before verbose diagnostics" `
    @(
        "boolean landscape",
        "boolean compactActionRows",
        "LinearLayout actions",
        "actions\.setOrientation\(compactActionRows \? LinearLayout\.VERTICAL : \(landscape \? LinearLayout\.HORIZONTAL : LinearLayout\.VERTICAL\)\)",
        "createFallbackActionRow",
        "addFallbackActionRow",
        "useCompactFallbackActionRows",
        "width < dp\(900\)",
        "Copy diagnostics",
        "Restart launcher",
        "Clear files",
        "Show diagnostics",
        "Hide diagnostics",
        "styleActionButton",
        "setMinHeight\(dp\(48\)\)",
        "GradientDrawable",
        "addActionButton",
        "root\.addView\(actions, actionsParams\)",
        "createDiagnosticsView",
        "diagnosticsView\.setVisibility\(View\.GONE\)",
        "diagnosticsView\.setVisibility\(show \? View\.VISIBLE : View\.GONE\)",
        "copyDiagnostics\(diagnosticsText\)",
        "root\.addView\(diagnosticsView"
    )

Add-Check `
    "docs\steam-version-selection-validation.md" `
    "keeps release blockers explicit" `
    @(
        "Release-readiness blockers",
        "Current release decision: not release-ready",
        "active install slot",
        "selected-cache-preserved aggregate",
        "Manual Pull completed before Push",
        "Current important Android local save evidence count",
        "Baseline manual Push prerequisites satisfied",
        "beta password",
        "save compatibility",
        "steam-version-selection-release-readiness\.md",
        "steam-version-selection-runbook\.md"
    )

Add-Check `
    "docs\steam-version-selection-release-readiness.md" `
    "tracks implementation status versus release evidence requirements" `
    @(
        "Current status",
        "Evidence required before release-candidate signoff",
        "Known release blockers",
        "Release rule",
        "Public/default branch",
        "Branch selector",
        "No silent public fallback",
        "Side-by-side storage",
        "Native startup routing",
        "Steam Cloud safety",
        "Autofill",
        "Artifact hygiene",
        "ARM64",
        "Pull-before-Push",
        "not release-candidate signed off"
    )

Add-Check `
    "docs\steam-version-selection-architecture.md" `
    "documents branch storage, readiness, cache, startup, and Push safety invariants" `
    @(
        "Steam branch dropdown",
        "SelectorInstallSlotHelpText",
        "active install slot",
        "Ready and download-required launcher status",
        "SteamBranchInfo\.selectorHelpText",
        "non-interactive helper text",
        "game_versions",
        "steam_branch\.txt",
        "Readiness rules",
        "Startup routing rules",
        "Selected game version note",
        "Selected game version slot kind",
        "Selected game version slot directory",
        "selected branch note",
        "Branch switch marker filename",
        "Cache cleanup rules",
        "Steam Cloud Push safety",
        "Release blockers"
    )

Add-Check `
    "docs\steam-version-selection-completion-audit.md" `
    "maps goal requirements to static and runtime evidence" `
    @(
        "Completion rule",
        "steam-version-selection-release-readiness\.md",
        "Requirement audit",
        "selected-version note",
        "account-visible Steam branch dropdown",
        "public/default always available",
        "Manual Pull completed before Push",
        "Current important Android local save evidence count",
        "Baseline manual Push prerequisites satisfied",
        "Manual Push evidence marker filename",
        "last_manual_cloud_push_blocked\.txt",
        "Manual Push completed after branch switch for selected version with backup evidence",
        "full local pre-Push coverage",
        "full cloud pre-Push coverage",
        "marker backup counts/timestamps",
        "fail-before-upload",
        "Runtime evidence still required",
        "Do not mark Steam beta/version selection release-ready yet"
    )

Add-ForbiddenCheck `
    "docs\steam-version-selection-completion-audit.md" `
    "does not describe the old manual selector model" `
    @(
        "manual Steam branch entry",
        "no arbitrary discovery"
    )

Add-Check `
    "scripts\android-adb-utils.ps1" `
    "resolves adb from explicit path, PATH, or common Android SDK roots" `
    @(
        "Resolve-AndroidAdbPath",
        "ANDROID_HOME",
        "ANDROID_SDK_ROOT",
        "platform-tools",
        "\.w40k-android-toolchain",
        "Pass -AdbPath"
    )

Add-Check `
    "scripts\capture-steam-version-selection-evidence.ps1" `
    "captures branch evidence with resolved adb and bounded backup scans" `
    @(
        "Resolve-AndroidAdbPath",
        "AdbPath",
        "quotedCommand",
        'run-as \$PackageName sh -c',
        "Invoke-DeviceShell",
        'sh -c \$quotedCommand',
        "echo local-pre-push:",
        "echo cloud-pre-push:",
        "last_game_version_cache_cleanup\.txt",
        "last_game_version_redownload\.txt",
        "last_steam_branch_availability\.txt",
        "marker-evidence-status\.txt",
        "Marker evidence status",
        "<missing marker>",
        "<empty marker>",
        "launcher-diagnostics-index\.txt",
        'Android/data/\$PackageName/files/diagnostics',
        'Invoke-DeviceShell -Command "find /storage/emulated/0/Android/data/\$PackageName/files/diagnostics',
        "timeout 10 find",
        "StS2Launcher/Saves -maxdepth 6",
        "pre-push-backup-counts\.txt"
    )

Add-ForbiddenCheck `
    "scripts\capture-steam-version-selection-evidence.ps1" `
    "does not let external diagnostics find run from device root" `
    @(
        'Invoke-AdbText\s+-Arguments\s+@\("shell",\s*"sh",\s*"-c",\s*"find /storage/emulated/0/Android/data/\$PackageName/files/diagnostics'
    )

Add-Check `
    "scripts\new-steam-version-selection-evidence.ps1" `
    "creates a structured artifact folder for ARM64 validation evidence" `
    @(
        "steam-version-selection",
        "branch-markers",
        "backup-evidence",
        "Resolve-RepoPath",
        "steam-version-selection-evidence-template\.md",
        "steam-version-selection-release-readiness\.md",
        "ARTIFACT_HYGIENE\.txt",
        "PUBLIC_SHARE_MANIFEST\.txt",
        "PUBLIC_EVIDENCE_REDACTION_REVIEW\.txt",
        "review-public-evidence-redaction\.ps1",
        "Preferred public artifacts",
        "Local-only or manual-review artifacts",
        "Manual Push evidence marker filename",
        "Do not run manual Push"
    )

Add-Check `
    "scripts\new-steam-version-selection-evidence.ps1" `
    "scaffolds non-secret device evidence review for version-selection validation" `
    @(
        "ARTIFACT_HYGIENE\.txt",
        "Raw logs and full launcher diagnostics are local-only",
        "PUBLIC_SHARE_MANIFEST\.txt",
        "PUBLIC_EVIDENCE_REDACTION_REVIEW\.txt",
        "Only sanitized diagnostics selected for public sharing",
        "Device notifications absent",
        "preferred public artifacts",
        "steam_branch\.txt",
        "last_game_branch_switch\.txt",
        "last_manual_cloud_pull\.txt",
        "last_manual_cloud_push\.txt",
        "last_manual_cloud_push_blocked\.txt",
        "last_steam_branch_availability\.txt",
        "last_game_version_cache_cleanup\.txt",
        "last_game_version_redownload\.txt",
        "backup-evidence",
        "Resolve-RepoPath",
        "branch-markers"
    )

Add-Check `
    "scripts\audit-steam-branch-guidance-parity.ps1" `
    "checks managed/native selected-branch guidance phrase parity" `
    @(
        "SteamGameBranch\.cs",
        "SteamBranchInfo\.java",
        "Default/public Steam branch",
        "Failed downloads do not change Steam Cloud saves",
        "Save compatibility is unproven"
    )

Add-Check `
    "scripts\export-public-evidence-redaction.ps1" `
    "exports a sanitized public-share evidence candidate without mutating raw evidence" `
    @(
        "SourceEvidenceDir",
        "PUBLIC_EVIDENCE_REDACTION_REVIEW\.txt",
        "PUBLIC_SHARE_MANIFEST\.txt",
        "Redact-PublicEvidenceText",
        "Raw evidence remains local",
        "Local user paths redacted",
        "Device identifiers redacted",
        "Only sanitized diagnostics selected for public sharing",
        "logs\\\\logcat-full",
        "logs\\\\logcat-steam-version-focused",
        "logcat-\(\?!steam-version-focused-redacted\)",
        "startup-routing-focused",
        "android-app-private",
        "redacted-device-serial",
        "redacted-email",
        "saveData",
        "IncludeImages"
    )

Add-Check `
    "scripts\review-public-evidence-redaction.ps1" `
    "fails public-share candidates without completed redaction review or with local-only artifacts" `
    @(
        "PUBLIC_EVIDENCE_REDACTION_REVIEW\.txt",
        "Screenshots manually reviewed",
        "Credential suggestions absent",
        "Device notifications absent",
        "Private save/profile contents absent",
        "Steam credentials absent",
        "Refresh/session tokens absent",
        "Local user paths redacted",
        "Device identifiers redacted",
        "Only sanitized diagnostics selected for public sharing",
        "logs\\\\logcat-full",
        "logs\\\\logcat-steam-version-focused",
        "logcat-\(\?!steam-version-focused-redacted\)",
        "startup-routing-focused",
        "Android package-private data path",
        "known connected device serial",
        "credential/token assignment",
        "Screenshot/image requires completed"
    )

Add-Check `
    "docs\steam-version-selection-tooling.md" `
    "documents static audit and evidence capture helper usage" `
    @(
        "audit-steam-version-selection\.ps1",
        "audit-steam-branch-guidance-parity\.ps1",
        "new-steam-version-selection-evidence\.ps1",
        "export-public-evidence-redaction\.ps1",
        "review-public-evidence-redaction\.ps1",
        "capture-steam-version-selection-evidence\.ps1",
        "steam-version-selection-release-readiness\.md",
        "ARTIFACT_HYGIENE\.txt",
        "local-only/raw-log",
        "PUBLIC_SHARE_MANIFEST\.txt",
        "PUBLIC_EVIDENCE_REDACTION_REVIEW\.txt",
        "Review public evidence redaction",
        "Export a sanitized public evidence candidate",
        "Raw evidence remains local",
        "Does not mutate artifacts and does not replace manual review",
        "safer public-sharing defaults",
        "logcat-steam-version-focused-redacted\.txt",
        "IncludeRawLogcat",
        "omitted by default",
        "normalize local path separators",
        "steamkit-debug-log-setting\.txt",
        "logcat-redaction-summary\.txt",
        "launcher-diagnostics-index\.txt",
        "AdbPath",
        "ANDROID_HOME",
        "ANDROID_SDK_ROOT",
        "\.w40k-android-toolchain",
        "last_game_branch_switch\.txt",
        "last_manual_cloud_push\.txt",
        "last_manual_cloud_push_blocked\.txt",
        "pre-push-backup-counts\.txt",
        "Artifact hygiene",
        "Do not store Steam credentials",
        "sts2_steamkit_debug_logs",
        "disabled by default",
        "Credential providers versus local credential handoff",
        "developer-only automation aids"
    )

Add-Check `
    "android\src\com\game\sts2launcher\GodotApp.java" `
    "requires selected branch provenance before consuming native game launch requests" `
    @(
        "boolean branchMarkerReady = isBranchMarkerReady\(selectedBranch\)",
        "boolean gamePckReady = isGamePckReady\(\)",
        "branchMarkerReady && gamePckReady && consumeGameLaunchRequest\(\)",
        "Blocking selected game version startup because branch marker provenance is missing or mismatched",
        "returning to launcher instead of falling back to another branch"
    )

Add-Check `
    "android\src\com\game\sts2launcher\GodotApp.java" `
    "keeps SteamKit debug logging opt-in at the Android boundary" `
    @(
        "ENV_STEAMKIT_DEBUG_LOGS",
        "sts2_steamkit_debug_logs",
        "setSteamKitDebugLogMode",
        "Sanitized SteamKit debug logging enabled",
        "STS2_STEAMKIT_DEBUG_LOGS"
    )

Add-Check `
    ".github\workflows\steam-version-selection-static-audit.yml" `
    "runs the static audit in CI" `
    @(
        "Steam Version Selection Static Audit",
        "pull_request",
        "workflow_dispatch",
        "audit-steam-version-selection\.ps1",
        "audit-steam-branch-guidance-parity\.ps1"
    )

Add-Check `
    ".github\workflows\overhaul-governance-ci.yml" `
    "requires Steam version-selection guardrail scaffolding" `
    @(
        "steam-version-selection-static-audit\.yml",
        "audit-steam-version-selection\.ps1",
        "audit-steam-branch-guidance-parity\.ps1",
        "steam-version-selection-validation\.md"
    )

Add-Check `
    ".github\PULL_REQUEST_TEMPLATE.md" `
    "prompts reviewers to call out Steam version-selection risk" `
    @(
        "Steam version-selection static audit run",
        "Steam branch guidance parity audit run",
        "Steam version-selection risk",
        "steam_branch\.txt",
        "Pull-after-switch"
    )

Add-Check `
    ".github\pull_request_template\pull_request_template.md" `
    "prompts reviewers to call out Steam version-selection risk" `
    @(
        "Steam version-selection static audit run",
        "Steam branch guidance parity audit run",
        "Steam version-selection risk",
        "steam_branch\.txt",
        "Pull-after-switch"
    )

Add-Check `
    "docs\steam-version-selection-runbook.md" `
    "orders destructive cloud validation behind Pull and backup gates" `
    @(
        "steam-version-selection-release-readiness\.md",
        "Cloud Pull gate",
        "Backup permission gate",
        "Pre-Push backup evidence",
        "Manual Push smoke test",
        "Optional auth diagnostics",
        "sts2_steamkit_debug_logs",
        "SteamKit debug logs sanitized for credentials/tokens"
    )

Add-Check `
    "docs\steam-version-selection-evidence-template.md" `
    "captures branch validation evidence" `
    @(
        "Selector mode",
        "Branch discovery",
        "Android credential provider model",
        "Launcher stores Steam password for credential providers",
        "SteamKit debug logs opt-in status",
        "disabled by default",
        "Steam branch dropdown option metadata",
        "Static guardrails",
        "steam-version-selection-release-readiness\.md",
        "audit-steam-version-selection\.ps1",
        "audit-steam-branch-guidance-parity\.ps1",
        "blocked states",
        "Selector helper text shows the active install slot",
        "selected game version note",
        "selected game version slot kind",
        "Native pre-routing logs selected branch",
        "selected branch note",
        "branch switch marker filename",
        "Branch switch marker records",
        "parseable UTC",
        "required-evidence status",
        "Branch switch previous branch",
        "Branch switch selected branch",
        "Branch switch selected version",
        "Branch switch selected version slot kind",
        "Branch switch selected version slot directory",
        "Branch switch selected branch matches current selected branch",
        "Branch switch selected branch note",
        "Branch switch local backup forced",
        "Branch switch manual Push requires backup storage",
        "Branch switch warning acknowledged",
        "Branch switch non-public warning acknowledged",
        "Branch switch marker has required safety evidence",
        "Branch switch marker has required safety evidence for selected branch",
        "Branch-switch manual Push prerequisites satisfied",
        "Pre-Push local backup evidence count",
        "Pre-Push cloud backup evidence count",
        "Latest pre-Push local backup UTC",
        "Latest pre-Push cloud backup UTC",
        "Pre-Push local backup evidence after branch switch",
        "Pre-Push cloud backup evidence after branch switch",
        "Branch-switch pre-Push backup evidence satisfied",
        "Manual Pull evidence marker filename",
        "Manual Push evidence marker filename",
        "last_manual_cloud_push_blocked\.txt",
        "Manual Pull evidence marker filename",
        "Manual Pull evidence marker path",
        "Manual Pull evidence UTC",
        "Manual Pull evidence UTC parseable",
        "Manual Pull evidence selected branch",
        "Manual Pull evidence selected version",
        "Manual Pull evidence selected version slot kind",
        "Manual Pull evidence selected version slot directory",
        "Manual Pull completion flag recorded",
        "Manual Pull completed before Push",
        "Manual Pull evidence is after branch switch",
        "Manual Pull evidence matches selected branch",
        "Manual Pull completed after branch switch",
        "Current important Android local save evidence count",
        "Current important Android local save evidence present",
        "Baseline manual Push prerequisites satisfied",
        "Manual Push evidence marker filename",
        "Manual Push evidence marker path",
        "Manual Push evidence UTC",
        "Latest manual Push evidence outcome",
        "Latest manual Push evidence UTC",
        "Latest manual Push evidence selected branch",
        "Latest manual Push evidence selected version",
        "Latest manual Push evidence selected version slot kind",
        "Latest manual Push evidence selected version slot directory",
        "Latest manual Push evidence reason",
        "Manual Push evidence UTC parseable",
        "Manual Push evidence selected branch",
        "Manual Push evidence selected version",
        "Manual Push evidence selected version slot kind",
        "Manual Push evidence selected version slot directory",
        "Manual Push evidence recorded local backup count",
        "Manual Push evidence recorded cloud backup count",
        "Manual Push evidence recorded latest local backup UTC",
        "Manual Push evidence recorded latest cloud backup UTC",
        "Manual Push evidence recorded important local save evidence count",
        "Manual Push evidence recorded baseline prerequisites satisfied",
        "Manual Push completion flag recorded",
        "Manual Push evidence is after branch switch",
        "Manual Push evidence matches selected branch",
        "Manual Push evidence recorded pre-Push backup evidence satisfied",
        "Manual Push completed after branch switch for selected version with backup evidence",
        "Manual Push blocked evidence marker filename",
        "Manual Push blocked evidence marker path",
        "Manual Push blocked evidence UTC",
        "Manual Push blocked evidence UTC parseable",
        "Manual Push blocked evidence selected branch",
        "Manual Push blocked evidence selected version",
        "Manual Push blocked evidence selected version slot kind",
        "Manual Push blocked evidence selected version slot directory",
        "Manual Push blocked evidence matches selected branch",
        "Manual Push blocked evidence recorded prerequisites satisfied",
        "Manual Push blocked evidence recorded local backup count",
        "Manual Push blocked evidence recorded cloud backup count",
        "Manual Push blocked evidence recorded latest local backup UTC",
        "Manual Push blocked evidence recorded latest cloud backup UTC",
        "Manual Push blocked evidence recorded important local save evidence count",
        "Manual Push blocked evidence recorded baseline prerequisites satisfied",
        "Manual Push blocked evidence recorded pre-Push backup evidence satisfied",
        "Manual Push blocked evidence reason",
        "Manual Push blocked before upload evidence recorded",
        "Early branch-switch Push gate blocks",
        "local/cloud pre-Push backup counts",
        "latest local/cloud backup UTC",
        "every important Android local save",
        "every existing important Steam Cloud save",
        "selected-cache-preserved aggregate",
        "Steam beta password",
        "save compatibility",
        "Artifact hygiene",
        "Steam credentials",
        "refresh tokens",
        "shared preferences",
        "device identifiers",
        "Raw full logcat was omitted by default",
        "IncludeRawLogcat",
        "sts2_steamkit_debug_logs",
        "logcat-steam-version-focused-redacted\.txt",
        "best-effort redacted file was manually reviewed before posting",
        "Redacted focused logcat includes its best-effort/manual-review warning header",
        "ARTIFACT_HYGIENE\.txt",
        "raw logs are treated as local-only",
        "PUBLIC_SHARE_MANIFEST\.txt",
        "preferred public artifacts",
        "logcat-redaction-summary\.txt",
        "focused-line and changed-line counts",
        "launcher-diagnostics-index\.txt",
        "full launcher diagnostics report attached publicly was manually reviewed/redacted",
        "Full launcher diagnostics and startup-recovery diagnostics reports include a public-sharing warning"
    )

Add-Check `
    "docs\steam-version-selection-user-guide.md" `
    "keeps tester-facing support boundaries and cloud safety rules visible" `
    @(
        "implemented for validation",
        "steam-version-selection-release-readiness\.md",
        "What is not supported yet",
        "REFRESH GAME VERSIONS",
        "Steam login credential entry",
        "Android credential provider model",
        "Godot login field credential metadata configured",
        "Godot fields are native Android Autofill targets",
        "Password-manager suggestions device validated",
        "Native credential handoff result TTL seconds",
        "Android credential provider capability boundary",
        "blocked states",
        "Steam beta password entry",
        "Selected game version note",
        "Selected game version slot kind",
        "Selected game version slot directory",
        "wrapped helper text",
        "active install slot",
        "Selected Steam branch note before routing",
        "Selected branch note",
        "Branch switch marker filename",
        "Branch switch marker path",
        "Branch switch marker UTC",
        "Branch switch marker UTC parseable",
        "Branch switch previous branch",
        "Branch switch selected branch",
        "Branch switch selected version",
        "Branch switch selected version slot kind",
        "Branch switch selected version slot directory",
        "Branch switch selected branch matches current selected branch",
        "Branch switch selected branch note",
        "Branch switch local backup forced",
        "Branch switch manual Push requires backup storage",
        "Branch switch warning acknowledged",
        "Branch switch non-public warning acknowledged",
        "Branch switch marker has required safety evidence",
        "Branch switch marker has required safety evidence for selected branch",
        "Branch-switch manual Push prerequisites satisfied",
        "Pre-Push local backup evidence count",
        "Pre-Push cloud backup evidence count",
        "Latest pre-Push local backup UTC",
        "Latest pre-Push cloud backup UTC",
        "Pre-Push local backup evidence after branch switch",
        "Pre-Push cloud backup evidence after branch switch",
        "Branch-switch pre-Push backup evidence satisfied",
        "Manual Pull evidence marker filename",
        "last_manual_cloud_push\.txt",
        "last_manual_cloud_push_blocked\.txt",
        "Manual Pull evidence marker filename",
        "Manual Pull evidence marker path",
        "Manual Pull evidence UTC",
        "Manual Pull evidence UTC parseable",
        "Manual Pull evidence selected branch",
        "Manual Pull evidence selected version",
        "Manual Pull evidence selected version slot kind",
        "Manual Pull evidence selected version slot directory",
        "Manual Pull completion flag recorded",
        "Manual Pull evidence is after branch switch",
        "Manual Pull evidence matches selected branch",
        "Manual Pull completed after branch switch",
        "Manual Push evidence marker filename",
        "Manual Push evidence marker path",
        "Manual Push evidence UTC",
        "Latest manual Push evidence outcome",
        "Latest manual Push evidence UTC",
        "Latest manual Push evidence selected branch",
        "Latest manual Push evidence selected version",
        "Latest manual Push evidence selected version slot kind",
        "Latest manual Push evidence selected version slot directory",
        "Latest manual Push evidence reason",
        "Manual Push evidence UTC parseable",
        "Manual Push evidence selected branch",
        "Manual Push evidence selected version",
        "Manual Push evidence selected version slot kind",
        "Manual Push evidence selected version slot directory",
        "Manual Push evidence recorded local backup count",
        "Manual Push evidence recorded cloud backup count",
        "Manual Push evidence recorded latest local backup UTC",
        "Manual Push evidence recorded latest cloud backup UTC",
        "Manual Push completion flag recorded",
        "Manual Push evidence is after branch switch",
        "Manual Push evidence matches selected branch",
        "Manual Push evidence recorded pre-Push backup evidence satisfied",
        "Manual Push completed after branch switch for selected version with backup evidence",
        "Manual Push blocked evidence marker filename",
        "Manual Push blocked evidence marker path",
        "Manual Push blocked evidence UTC",
        "Manual Push blocked evidence UTC parseable",
        "Manual Push blocked evidence selected branch",
        "Manual Push blocked evidence selected version",
        "Manual Push blocked evidence selected version slot kind",
        "Manual Push blocked evidence selected version slot directory",
        "Manual Push blocked evidence matches selected branch",
        "Manual Push blocked evidence reason",
        "Manual Push blocked before upload evidence recorded",
        "Game version cache cleanup marker selected cache preserved where applicable",
        "Pull from Cloud first",
        "steam_branch\.txt",
        "selectedBranchManifest",
        "publicManifest",
        "public-inherited",
        "manifestRequestBranch=public",
        "branch-integrity provenance",
        "Branch marker depots inherited from public",
        "Branch marker depots missing selected branch manifest",
        "Branch marker depot manifest rows",
        "Classification:",
        "Evidence readiness: not ready for final classification",
        "Clean redownload matches investigated branch: true",
        "Clean redownload selected directories cleared: true",
        "Public-vs-beta key asset comparison captured",
        "Steam credentials",
        "refresh tokens",
        "shared preferences",
        "device identifiers",
        "Release readiness"
    )

Add-Check `
    "docs\steam-version-selection-release-readiness.md" `
    "requires beta branch integrity evidence before release signoff" `
    @(
        "Beta branch integrity",
        "effective manifest",
        "selected-branch manifest",
        "public manifest",
        "manifest source",
        "inherits public",
        "file inventory",
        "key asset or PCK hashes",
        "Runtime patching now falls back to branch-local"
    )

Add-Check `
    "scripts\capture-steam-beta-integrity-evidence.ps1" `
    "captures public versus selected branch inventories and marker evidence" `
    @(
        "public-files\.tsv",
        "public-cache-tree\.txt",
        "selected inventory",
        "cache-tree\.txt",
        "sha256sum",
        "public-vs-",
        "key-assets\.tsv",
        "Changed key asset rows",
        "Art/bundle-like files",
        "Public sharing warning:",
        "ReviewSummary",
        "FailOnNotReady",
        "Resolve-AndroidAdbPath",
        "review-beta-integrity-summary\.ps1",
        "Classification:",
        "Evidence readiness:",
        "Evidence missing/weak:",
        "Classification inputs:",
        "clean redownload not proven",
        "Public branch marker:",
        "Selected branch marker:",
        "Clean redownload marker:",
        "Clean redownload selected directories cleared:",
        "Branch availability marker:",
        "Branch availability matches investigated branch:",
        "Branch availability selected branch Windows depot manifests:",
        "likely Steam branch availability issue",
        "Focused logcat:",
        "Public branch depot manifest rows",
        "Selected branch depot manifest rows",
        "steam_branch\.txt",
        "last_steam_branch_availability\.txt",
        "public-inherited",
        "runtime remote/config",
        "Read-Inventory",
        "Write-InventoryComparison"
    )

Add-Check `
    "scripts\review-beta-integrity-summary.ps1" `
    "reviews beta-integrity summary readiness without manually scanning the artifact" `
    @(
        "Evidence readiness:",
        "Evidence missing/weak:",
        "Public sharing warning present:",
        "Clean redownload matches investigated branch:",
        "Clean redownload selected directories cleared:",
        "Branch availability matches investigated branch:",
        "FailOnNotReady",
        "Exit code: 2",
        "Exit code: 3"
    )

Add-Check `
    "docs\steam-version-selection-tooling.md" `
    "documents beta-integrity evidence capture workflow" `
    @(
        "Capture beta branch integrity evidence",
        "capture-steam-beta-integrity-evidence\.ps1",
        "AdbPath",
        "review-beta-integrity-summary\.ps1",
        "ReviewSummary",
        "FailOnNotReady",
        "public-files\.tsv",
        "public-cache-tree\.txt",
        "<branch>-cache-tree\.txt",
        "public-vs-<branch>-comparison\.txt",
        "key-assets\.tsv",
        "Changed key asset rows",
        "manifestSource=selected",
        "public-inherited",
        "partial Steam branch",
        "Classification:",
        "Evidence readiness:",
        "review-beta-integrity-summary\.ps1",
        "clean-redownload proof",
        "public-sharing warning",
        "branch-availability evidence",
        "classification input metrics",
        "clean selected-branch redownload",
        "art assets look wrong"
    )

Add-Check `
    "docs\steam-beta-integrity-runtime-checklist.md" `
    "documents remaining runtime pass for beta-integrity classification" `
    @(
        "capture-steam-beta-integrity-evidence\.ps1",
        "ReviewSummary",
        "FailOnNotReady",
        "Evidence readiness:",
        "Clean redownload matches investigated branch: true",
        "Clean redownload selected directories cleared: true",
        "Changed key asset rows",
        "likely Steam partial branch",
        "likely Steam branch availability issue",
        "Do not mark beta branch integrity complete"
    )

Add-Check `
    "docs\steam-version-selection-evidence-template.md" `
    "captures beta-integrity evidence in validation packages" `
    @(
        "Public-vs-beta branch integrity",
        "Beta slot was clean-redownloaded",
        "clean-redownload fields",
        "branch-availability fields",
        "public/default and selected branch marker paths",
        "bounded public/default and selected branch depot manifest rows",
        "Focused beta-integrity logcat",
        "selectedBranchManifest",
        "publicManifest",
        "manifestSource",
        "manifestRequestBranch",
        "Selected beta cache tree captured",
        "Public-vs-beta inventory comparison captured",
        "public-vs-<branch>-key-assets\.tsv",
        "bounded changed key-asset rows",
        "SlayTheSpire2\.pck",
        "Affected art asset paths/hashes",
        "Classification:",
        "Evidence readiness:",
        "review-beta-integrity-summary\.ps1",
        "clean-redownload proof",
        "classification input metrics",
        "Steam partial branch",
        "runtime remote/config behavior",
        "Selected game branch marker depot manifest rows"
    )

Add-Check `
    "scripts\new-steam-version-selection-evidence.ps1" `
    "scaffolds beta-integrity inventory evidence folder" `
    @(
        "inventories",
        "capture-steam-beta-integrity-evidence\.ps1",
        "review-beta-integrity-summary\.ps1",
        "Evidence readiness: not ready for final classification",
        "SHA-256 comparison summaries"
    )

Add-Check `
    ".github\ISSUE_TEMPLATE\steam_version_selection_report.md" `
    "keeps public Steam version-selection reports free of secrets and identifiers" `
    @(
        "Release-readiness gate covered",
        "No silent fallback to public/default",
        "Public-vs-beta depot manifest integrity",
        "Public-vs-beta file inventory",
        "Did any game behavior, UI, or art asset look like public/mainline",
        "Was the beta slot clean-redownloaded",
        "Android/Samsung/password-manager suggestion behavior",
        "Public-share artifact hygiene reviewed",
        "Artifact hygiene",
        "Steam credentials",
        "refresh tokens",
        "shared preferences",
        "device identifiers",
        "local user paths",
        "Android credential provider model",
        "Launcher stores Steam password for credential providers",
        "SteamKit debug logs opt-in enabled",
        "SteamKit debug logs sanitized for credentials/tokens",
        "adb logcat",
        "redacting identifiers",
        "logcat-steam-version-focused-redacted\.txt",
        "avoid raw full logcat",
        "manually review it before posting",
        "Selected game version note",
        "Selected game version slot kind",
        "Selected game version slot directory",
        "Game version cache cleanup marker filename",
        "Game version cache cleanup marker path",
        "Game version cache cleanup marker present",
        "Game version cache cleanup marker UTC",
        "Game version cache cleanup marker selected branch",
        "Game version cache cleanup marker selected version",
        "Game version cache cleanup marker selected version slot kind",
        "Game version cache cleanup marker selected version slot directory",
        "Game version cache cleanup marker game_versions present",
        "Game version cache cleanup marker runtime_packs present",
        "Game version cache cleanup marker selected runtime pack directory",
        "Game version cache cleanup marker selected runtime pack present before cleanup",
        "Game version cache cleanup marker removed count",
        "Game version cache cleanup marker removed runtime pack count",
        "Game version cache cleanup marker selected cache preserved where applicable",
        "Game version cache cleanup marker selected runtime pack preserved where applicable",
        "Game version redownload marker filename",
        "Game version redownload marker path",
        "Game version redownload marker present",
        "Game version redownload marker selected branch",
        "Game version redownload marker selected version",
        "Game version redownload marker selected version slot kind",
        "Game version redownload marker selected version slot directory",
        "Selected game branch marker depots matching public",
        "Selected game branch marker depots differing from public",
        "Selected game branch marker depots without public comparison",
        "Selected game branch marker depots inherited from public",
        "Selected game branch marker depots missing selected branch manifest",
        "Selected game branch marker partial Steam branch evidence",
        "Selected game branch marker depot manifest rows",
        "manifestSource=<selected|public-inherited>",
        "manifestRequestBranch=<selected|public>",
        "Public-vs-beta file and art evidence",
        "Public-sharing warning reviewed",
        "SlayTheSpire2.pck",
        "Art/bundle-like files",
        "Branch switch marker filename",
        "Manual Pull evidence marker filename",
        "Manual Pull evidence marker path",
        "Manual Pull evidence UTC",
        "Manual Pull evidence UTC parseable",
        "Manual Pull evidence selected branch",
        "Manual Pull evidence selected version",
        "Manual Pull evidence selected version slot kind",
        "Manual Pull evidence selected version slot directory",
        "Manual Pull completed before Push",
        "Current important Android local save evidence count",
        "Current important Android local save evidence present",
        "Baseline manual Push prerequisites satisfied",
        "Manual Pull completed after branch switch",
        "Manual Push evidence marker filename",
        "Manual Push evidence marker path",
        "Manual Push evidence UTC",
        "Manual Push evidence UTC parseable",
        "Manual Push evidence selected branch",
        "Manual Push evidence selected branch selection kind",
        "Manual Push evidence selector mode",
        "Manual Push evidence selected version",
        "Manual Push evidence selected version slot kind",
        "Manual Push evidence selected version slot directory",
        "Manual Push evidence recorded important local save evidence count",
        "Manual Push evidence recorded baseline prerequisites satisfied",
        "Manual Push evidence recorded pre-Push backup evidence satisfied",
        "Manual Push completed after branch switch for selected version with backup evidence",
        "Manual Push blocked evidence marker filename",
        "Manual Push blocked evidence marker path",
        "Manual Push blocked evidence UTC",
        "Manual Push blocked evidence UTC parseable",
        "Manual Push blocked evidence selected branch",
        "Manual Push blocked evidence selected branch selection kind",
        "Manual Push blocked evidence selector mode",
        "Manual Push blocked evidence selected version",
        "Manual Push blocked evidence selected version slot kind",
        "Manual Push blocked evidence selected version slot directory",
        "Manual Push blocked evidence reason",
        "Manual Push blocked evidence recorded important local save evidence count",
        "Manual Push blocked evidence recorded baseline prerequisites satisfied",
        "Manual Push blocked evidence recorded prerequisites satisfied",
        "Manual Push blocked evidence recorded local backup count",
        "Manual Push blocked evidence recorded cloud backup count",
        "Manual Push blocked evidence recorded latest local backup UTC",
        "Manual Push blocked evidence recorded latest cloud backup UTC",
        "Manual Push blocked evidence recorded pre-Push backup evidence satisfied",
        "Manual Push blocked before upload evidence recorded",
        "Pre-Push local backup evidence count",
        "Pre-Push cloud backup evidence count",
        "Latest pre-Push local backup UTC",
        "Latest pre-Push cloud backup UTC",
        "Pre-Push local backup evidence after branch switch",
        "Pre-Push cloud backup evidence after branch switch",
        "Branch-switch pre-Push backup evidence satisfied",
        "Pull from Cloud"
    )

Add-Check `
    "docs\steam-version-selection-save-compatibility.md" `
    "tracks public/beta save compatibility and Push safety evidence" `
    @(
        "Compatibility matrix",
        "Push safety matrix",
        "Public/default",
        "Beta",
        "Pull from Cloud after the branch switch",
        "last_manual_cloud_push\.txt",
        "successful Push marker evidence"
    )

Add-Check `
    "OVERHAUL_ROADMAP.md" `
    "tracks Steam version selection as an active hardening phase" `
    @(
        "Steam version selection and branch cache hardening",
        "REFRESH GAME VERSIONS",
        "Login credential providers",
        "SteamKit debug logs disabled by default",
        "sts2_steamkit_debug_logs=1",
        "branch marker/provenance",
        "wrapped selector guidance",
        "managed/native guidance parity",
        "missing/private/password",
        "Pull-after-switch",
        "last_manual_cloud_push\.txt",
        "aggregate successful post-switch Push evidence"
    )

Add-Check `
    "docs\android-release-validation.md" `
    "keeps release signoff gated on branch/version evidence" `
    @(
        "Steam version-selection validation",
        "audit-steam-version-selection\.ps1",
        "audit-steam-branch-guidance-parity\.ps1",
        "steam_branch\.txt",
        "selected-PCK startup routing",
        "backup storage permission"
    )

Add-Check `
    "docs\android-steam-login-validation.md" `
    "defines Android Steam login validation proof contract" `
    @(
        "android-login-portal-evidence-template\.md",
        "integrated in-app native Steam credential panel",
        "real Android username and password fields",
        "no user-facing native credential handoff popup",
        "does not store or inject Steam passwords",
        "real `EditText` fields",
        "Android Autofill hints",
        "Steam web-domain metadata",
        "launcher portal uses explicit status and titled state sections",
        "Diagnostics are hidden behind the diagnostics console drawer",
        "Android warmup/loading uses a mobile-width compact panel with readable styled percentage progress.",
        "Android post-launch startup status uses a framed mobile-width readable card after the launcher closes.",
        "Native fallback keeps verbose diagnostics collapsed until explicitly requested",
        "Native fallback recovery actions split into two touch-friendly rows on narrow landscape screens.",
        "The compact diagnostics toggle uses a touch-safe two-line detail label without exposing diagnostics by default.",
        "Compact diagnostics lives inside the scroll body rather than consuming fixed phone viewport chrome, and explicit diagnostics actions scroll to it.",
        "The compact status card stays readable while using low-profile spacing so more current task content remains visible.",
        "The compact status card stacks the phase chip and guided next action so neither is squeezed on narrow screens.",
        "Compact status details keep normal progress text to one stable line while attention/failure guidance can expand.",
        "Compact safe-flow guidance starts collapsed behind a touch-safe two-line toggle that says `SAFE FLOW` and `Pull then play`.",
        "Compact safe-flow toggle renders SAFE FLOW / Pull then play as structured title/detail labels",
        "Compact sign-in, Steam Guard, and download task screens suppress the safe-flow drawer so the current primary controls stay higher in the viewport.",
        "The compact workflow strip stays in one dense row on narrow compact viewports instead of taking a second fixed header row.",
        "Tapping compact workflow step labels scrolls directly to the visible matching task section or the current safe fallback task.",
        "Compact recovery/tools actions use a two-column support grid when width allows and full-width stacked tools on narrow compact viewports.",
        "Compact Play and Sync shows the ready version, Pull-first guidance, and Push-locked state in a readable summary card that stays one line when width allows and becomes a readable two-line cloud-safety cue on narrow compact viewports.",
        "Compact Pull action renders .*PULL TO ANDROID / Download saves.* as a structured title/detail label",
        "Compact locked Push toggle renders STEAM PUSH LOCKED / Open overwrite and HIDE PUSH / Close overwrite as structured title/detail labels:",
        "Compact unlocked Push actions render .*PUSH TO STEAM / Upload Android.* and .*CONFIRM OVERWRITE / Final upload.* as structured title/detail labels after the Push overwrite drawer is explicitly opened",
        "Compact armed Push warning says STEAM CLOUD OVERWRITE / Confirm only after Pull/local saves are verified",
        "Compact Pull and locked Steam Push share one two-button row when width allows and stack with Pull first on narrow compact viewports.",
        "Compact Backup and Sync cloud options use detail labels and share one low-profile row when width allows and stack full-width on narrow compact viewports.",
        "Compact Android sign-in shows .*SIGN IN WITH STEAM.* before password-manager helper copy",
        "Compact Android sign-in CTA renders .*SIGN IN WITH STEAM / Android login.* as a structured title/detail label",
        "Compact Android sign-in uses a large primary .*SIGN IN WITH STEAM.* CTA and a readable two-line password-manager safety helper",
        "Compact Steam Guard submit action renders .*VERIFY CODE / Submit once.* as a structured title/detail label",
        "Compact Steam Guard retry keeps the rejected-code title short and moves latest-code guidance into the helper below the code controls",
        "Compact retry/failure state promotes .*TRY AGAIN.*Restart task.* primary recovery action while support tools remain secondary",
        "Compact raw-log copy keeps the short .*COPY LOG.* label but uses .*Review first.* detail text before copying diagnostics",
        "The compact current-task bar stays reachable, uses two-line .*GO TO.* navigation wording, and is touch-safe without wasting vertical space",
        "The compact current-task bar uses short title labels such as .*GO TO LOGIN.*, .*GO TO GUARD.*, .*GO TO FILES.*, and .*GO TO PLAY.*",
        "The compact current-task bar uses contextual detail labels such as .*Steam login.*, .*Verification code.*, .*Game files.*, and .*Play and saves.*.",
        "The compact current-task bar renders .*GO TO.* and contextual details as structured title/detail labels",
        "The compact inline current-task bar uses dense height while staying touch-safe, so the persistent header does not crowd active controls.",
        "The compact current-task bar and workflow strip share a tight sticky header instead of being separated as independent chrome rows.",
        "When width allows, the compact current-task bar and workflow strip share one inline sticky row, reducing header height while keeping controls readable and tappable.",
        "On narrow compact viewports, the stacked current-task row stays low-profile while remaining touch-safe.",
        "The compact sticky task header is grouped inside a low-profile toolbar shell so the persistent task controls read as one toolbar.",
        "On narrow compact viewports, the compact sticky task header stacks into a dense current-task row plus one dense workflow row instead of a two-row workflow grid.",
        "The compact sticky task header reflows between inline and stacked task/workflow layouts after Android rotation or keyboard viewport changes.",
        "The compact active task or last compact scroll target re-anchors after Android rotation or keyboard viewport changes without stealing focus from keyboard input fields.",
        "portal clearly exposes the next action",
        "Compact section headers keep title and readable task cue in one dense row without restoring bulky repeated subtitle cards",
        "Password-manager suggestions",
        "Steam Guard",
        "Failed login",
        "Successful return",
        "Native integrated credential panel supported:",
        "Native credential fields Autofill hints configured:",
        "Steam credential web domain configured:",
        "Native credential panel inline status configured:",
        "Native credential panel keyboard-safe layout configured:",
        "Native credential panel IME inset scroll supported:",
        "Native credential panel touch-target layout configured:",
        "Launcher Android-readable warmup screen supported:",
        "Launcher Android-readable startup status card supported:",
        "Launcher native fallback diagnostics collapsed by default:",
        "Launcher native fallback responsive recovery rows supported:",
        "Native credential panel requests both Autofill fields:",
        "Native credential panel focus Autofill requests supported:",
        "Native credential panel task-led buttons supported:",
        "Native credential panel responsive action rows supported:",
        "Native credential panel orientation reflow supported:",
        "Native credential panel short-height copy supported:",
        "Native credential panel short-height reflow supported:",
        "Native credential panel IME height reflow supported:",
        "Native credential panel password visibility toggle supported:",
        "Native credential panel password-focus button supported:",
        "Native credential panel Back dismiss supported:",
        "Native credential panel dismiss retry supported:",
        "Native credential panel dismiss hides keyboard:",
        "Native credential panel suppresses pre-auth save prompt:",
        "Steam Guard one-shot code guidance supported:",
        "Failed-login retry guidance supported:",
        "Context-specific login recovery guidance supported:",
        "Native credential handoff popup supported:",
        "Password-manager suggestions device validated:",
        "Launcher portal UX model:",
        "Launcher status-led portal supported:",
        "Launcher phase-labeled status supported:",
        "Launcher structured status chip supported:",
        "Launcher guided next-action status supported:",
        "Launcher error-first guided status supported:",
        "Launcher titled state sections supported:",
        "Launcher safe first-run guidance supported:",
        "Launcher compact safe-flow guidance collapsible:",
        "Launcher compact low-profile safe-flow toggle supported:",
        "Launcher compact safe-flow toggle detail labels supported:",
        "Launcher compact structured safe-flow toggle labels supported:",
        "Launcher compact safe-flow bounded guide supported:",
        "Launcher compact active-task safe-flow suppression supported:",
        "Launcher mobile-first compact layout supported:",
        "Launcher compact dense panel padding supported:",
        "Launcher compact dense vertical rhythm supported:",
        "Launcher rounded scaled metrics supported:",
        "Launcher Android compact touch-scale floor supported:",
        "Launcher compact dynamic content width supported:",
        "Launcher tablet/wide content layout supported:",
        "Launcher top-anchored portal content supported:",
        "Launcher compact vertical status hero supported:",
        "Launcher compact low-profile status card supported:",
        "Launcher compact status headline row supported:",
        "Launcher compact stacked status headline supported:",
        "Launcher viewport-aware compact status headline reflow supported:",
        "Launcher compact stable status detail row supported:",
        "Launcher compact sticky workflow step strip supported:",
        "Launcher compact low-profile workflow step strip supported:",
        "Launcher compact low-profile two-column workflow step strip supported:",
        "Launcher compact workflow step direct navigation supported:",
        "Launcher compact narrow workflow single-row supported:",
        "Launcher compact visible workflow step labels supported:",
        "Launcher compact workflow step number badges supported:",
        "Launcher compact current-task jump supported:",
        "Launcher compact sticky current-task bar supported:",
        "Launcher compact low-profile current-task bar supported:",
        "Launcher compact dense inline current-task bar supported:",
        "Launcher compact current-task shared touch height supported:",
        "Launcher compact low-profile stacked current-task bar supported:",
        "Launcher compact current-task context labels supported:",
        "Launcher compact structured current-task labels supported:",
        "Launcher compact touch-safe sticky header controls supported:",
        "Launcher compact grouped sticky task header supported:",
        "Launcher compact sticky task toolbar shell supported:",
        "Launcher compact inline sticky task header supported:",
        "Launcher compact responsive sticky task header supported:",
        "Launcher viewport-aware sticky task header reflow supported:",
        "Launcher viewport-aware compact task re-anchor supported:",
        "Launcher compact dense sticky task header supported:",
        "Launcher compact task-jump navigation labels supported:",
        "Launcher compact padded scroll anchors supported:",
        "Launcher keyboard-focused input scroll supported:",
        "Launcher compact contextual confirmation labels supported:",
        "Launcher compact scroll-safe confirmation dialogs supported:",
        "Launcher viewport-aware confirmation dialogs supported:",
        "Launcher compact Steam Guard large input supported:",
        "Launcher compact Steam Guard action-first layout supported:",
        "Launcher compact Steam Guard inline action row supported:",
        "Launcher compact responsive Steam Guard action layout supported:",
        "Launcher viewport-aware compact Steam Guard action row reflow supported:",
        "Launcher compact Steam Guard submit detail label supported:",
        "Launcher compact Steam Guard retry guidance supported:",
        "Launcher compact Steam Guard bounded helper supported:",
        "Launcher compact primary retry action supported:",
        "Launcher compact structured retry action labels supported:",
        "Launcher compact primary login action first supported:",
        "Launcher compact Android login primary CTA supported:",
        "Launcher compact Android login detail label supported:",
        "Launcher compact Android login helper detail label supported:",
        "Launcher compact completed-auth section suppression supported:",
        "Launcher touch-first action targets supported:",
        "Launcher primary action wording supported:",
        "Launcher consistent START GAME CTA supported:",
        "Launcher compact launch detail label supported:",
        "Launcher branded atmospheric background supported:",
        "Launcher branded background explicit RGBA supported:",
        "Launcher high-contrast rounded actions supported:",
        "Launcher compact header chrome reduction supported:",
        "Launcher compact condensed brand header supported:",
        "Launcher compact single-line brand header supported:",
        "Launcher compact readable brand subtitle supported:",
        "Launcher compact section-header subtitle suppression supported:",
        "Launcher compact low-profile section headers supported:",
        "Launcher compact single-row section headers supported:",
        "Launcher compact section-header task cues supported:",
        "Launcher compact readable section-header cues supported:",
        "Launcher compact install primary detail label supported:",
        "Launcher compact download progress hero supported:",
        "Launcher compact download progress status label supported:",
        "Launcher compact readable download progress bar supported:",
        "Launcher compact inline install-version controls supported:",
        "Launcher compact version details collapsible:",
        "Launcher compact version drawer bounded help label supported:",
        "Launcher compact structured install-version action labels supported:",
        "Launcher compact version summary cards supported:",
        "Launcher compact selected-version headline supported:",
        "Launcher compact responsive selected-version summary supported:",
        "Launcher compact ready-version summary panel supported:",
        "Launcher compact ready-version headline supported:",
        "Launcher compact responsive ready-version summary supported:",
        "Launcher compact structured Play/Sync action labels supported:",
        "Launcher compact ready-state install-section suppression supported:",
        "Launcher compact touch-safe version dropdown supported:",
        "Launcher compact touch-safe dropdown popup supported:",
        "Launcher compact cloud-safety guidance collapsible:",
        "Launcher compact cloud-safety detail label supported:",
        "Launcher compact cloud options collapsible:",
        "Launcher primary cloud actions before cloud options:",
        "Launcher compact cloud option detail labels supported:",
        "Launcher safer Pull-before-Push cloud ordering supported:",
        "Launcher compact cloud direction labels supported:",
        "Launcher compact cloud primary actions row supported:",
        "Launcher compact Pull detail label supported:",
        "Launcher compact responsive action rows supported:",
        "Launcher compact dangerous Push detail labels supported:",
        "Launcher compact armed Push warning detail label supported:",
        "Launcher manual Push armed overwrite warning supported:",
        "Launcher compact cloud options row supported:",
        "Launcher version-install/cloud-save separation guidance supported:",
        "Launcher diagnostics console hidden by default:",
        "Launcher diagnostics console auto-opens for diagnostics actions:",
        "Compact diagnostics toggle renders .*DIAGNOSTICS.*Log \+ export context.*structured title/detail labels",
        "Launcher compact low-profile drawer toggles supported:",
        "Launcher compact dense drawer toggle height supported:",
        "Launcher compact support tools grid supported:",
        "Launcher compact raw-log review label supported:",
        "Launcher compact low-profile diagnostics toggle supported:",
        "Launcher compact diagnostics toggle detail labels supported:",
        "Launcher compact structured diagnostics toggle labels supported:",
        "Launcher startup fallback raw banner suppressed:",
        "Launcher portal UX device validated:",
        "Launcher portal UX validation boundary:",
        "SteamKit debug logs sanitized for credentials/tokens:",
        "portal scaling/readability/next-action clarity",
        "hidden diagnostics behavior",
        "not complete until ARM64 evidence covers"
    )

Add-Check `
    "docs\android-login-portal-evidence-template.md" `
    "captures ARM64 login, portal, cloud, and launch proof without secrets" `
    @(
        "Do not use emulator evidence for signoff",
        "Steam username/email redacted",
        "Steam password absent",
        "Steam Guard code absent",
        "Native integrated Steam login panel opens automatically",
        "No USE ANDROID AUTOFILL popup/helper dialog visible",
        "Inline status guidance visible",
        "Native login panel remains usable when the keyboard is open",
        "Native login panel can scroll if keyboard or small screen reduces available height",
        "Native login panel keeps Sign In and Cancel reachable with the keyboard open",
        "Native login controls are stacked/full-width in portrait and use responsive wide rows in landscape",
        "Native login primary button says SIGN IN WITH STEAM",
        "Compact launcher sign-in shows SIGN IN WITH STEAM before helper copy",
        "Compact launcher sign-in says SIGN IN WITH STEAM / Android login",
        "Compact launcher sign-in uses a large primary SIGN IN WITH STEAM CTA and a readable two-line password-manager safety helper",
        "Native login panel requests suggestions for username and password fields",
        "Native login panel requests suggestions again when username/password fields gain focus",
        "Native login NEXT: PASSWORD control focuses password field",
        "Back/Cancel dismissal hides the soft keyboard before returning to launcher",
        "Provider does not prompt to save unverified credentials before Steam authentication",
        "Password visibility toggle shows/hides password without storing it",
        "Password visibility resets to hidden after submit/cancel/reopen",
        "Safe first-run flow guidance visible",
        "On compact phone layout, safe-flow guidance starts collapsed",
        "Compact safe-flow toggle says SAFE FLOW / Pull then play and is touch-safe",
        "Compact safe-flow toggle renders SAFE FLOW / Pull then play as structured title/detail labels",
        "Compact expanded safe-flow guide stays bounded and says Setup: sign in -> download version -> Pull saves",
        "Compact active task screens suppress the safe-flow drawer so primary controls stay higher",
        "Safe-flow guidance expands/collapses without hiding the primary task",
        "Compact phone layout uses most of the usable screen height",
        "Compact phone layout avoids excessive internal panel margins",
        "Compact phone shell uses dense panel padding",
        "Compact phone layout uses dense vertical spacing between repeated launcher regions",
        "Compact phone layout uses dynamic content width instead of a narrow fixed column",
        "Tablet/wide layout avoids a narrow fixed inner column",
        "Android warmup/loading screen uses a mobile-width compact panel with readable styled progress",
        "Android post-launch startup status uses a framed mobile-width readable card",
        "Native fallback verbose diagnostics collapsed until requested",
        "Native fallback recovery actions split into responsive rows on narrow landscape screens",
        "Portal task flow is top anchored rather than vertically stranded",
        "Compact phone status appears as a readable vertical next-step card",
        "Compact phone status card is low-profile but still readable",
        "Compact status card uses an inline phase and next-action headline where width allows",
        "Compact status card stacks phase and next action without squeezing either label on narrow compact screens",
        "Compact responsive numbered workflow step strip remains visible while scrolling",
        "Compact workflow step strip stays in one dense row on narrow compact viewports",
        "Compact workflow step strip uses low-profile single-row cells on narrow compact viewports",
        "Compact workflow step strip shows short visible labels, not only numbers/tooltips",
        "Compact workflow step strip separates step numbers into small badges next to readable labels",
        "Tapping compact workflow step labels scrolls directly to visible matching task sections or the current safe fallback task",
        "Compact workflow step strip is touch-safe but still readable",
        "Compact current-task bar remains reachable while scrolling",
        "Compact current-task bar uses GO TO navigation wording",
        "Compact current-task bar uses contextual task detail labels",
        "Compact current-task bar renders GO TO/context labels as structured title/detail labels",
        "Compact current-task bar is touch-safe but still compact",
        "Compact inline current-task bar is dense while remaining touch-safe",
        "Compact current-task bar and workflow strip share a tight sticky header",
        "Compact current-task bar and workflow strip share one inline sticky row when width allows",
        "Compact stacked current-task row is low-profile on narrow compact viewports",
        "Compact sticky task header is grouped inside a low-profile toolbar shell",
        "Compact sticky task header stacks on narrow compact viewports",
        "Compact sticky task header keeps the narrow workflow row dense enough to leave action content visible",
        "Compact sticky task header reflows between inline and stacked layouts after rotation or keyboard viewport changes",
        "Compact active task remains re-anchored after rotation or keyboard viewport changes",
        "Compact Game Install selected-version summary is a readable card with Cloud unchanged cue when width allows",
        "Compact Game Install selected-version summary becomes a two-line local-files-only cue on narrow compact viewports",
        "Compact install-version dropdown and refresh controls share one row when width allows",
        "Compact version drawer controls render CHANGE VERSION / Local files only and REFRESH VERSIONS / Update branch list as structured title/detail labels",
        "Compact expanded version helper says Install target / Launch target with short branch status",
        "Status card shows a clear guided next action for the current state",
        "Failure/blocked/crash statuses show attention/fix guidance before normal install/cloud/launch guidance",
        "Primary actions use clear task wording, for example sign in/start game/verify code",
        "Primary launch action consistently says START GAME",
        "Primary and secondary actions are large enough to tap comfortably",
        "Launcher background has visible branded atmosphere without reducing readability",
        "Buttons use high-contrast rounded action styling",
        "Compact phone header uses shortened subtitle/chrome",
        "Compact phone brand header is a single low-profile row",
        "Compact phone brand subtitle remains readable at phone scale",
        "Compact phone header leaves more first-action area visible",
        "Compact phone section headers avoid repeated subtitle blocks",
        "Compact phone section headers stay compact and leave controls visible",
        "Compact phone section headers keep title and readable task cue in one dense row without clipping the title",
        "Compact Game Install shows selected version and Download before optional version details",
        "Compact version dropdown is large enough to read and tap when expanded",
        "Opened compact game-version dropdown popup rows have larger spacing/padding and are touch-safe",
        "Compact phone version details start collapsed",
        "Version details expand/collapse without changing selected version",
        "Compact download progress appears directly below the disabled DOWNLOADING primary action",
        "Compact download progress uses a taller styled percentage bar",
        "Compact optional drawer toggles are low-profile but still tappable",
        "Compact optional drawer toggles use dense touch-safe height",
        "Compact optional drawer toggles are shorter than primary action buttons",
        "Compact Play/Sync action buttons render title/detail labels as structured two-line controls",
        "Compact launch CTA says START GAME / Selected version",
        "Compact recovery/tools actions use a two-column support grid when width allows",
        "Compact recovery/tools actions stack full-width on narrow compact viewports",
        "Compact phone cloud safety starts collapsed",
        "Compact cloud-safety cue appears before Pull/Push controls",
        "Compact expanded cloud-safety detail says Cloud target and PULL downloads to Android / PUSH can overwrite Steam",
        "Cloud safety expands/collapses while preserving Pull/Push controls",
        "Compact phone cloud options start collapsed",
        "Cloud options expand/collapse while preserving Pull/Push controls",
        "Compact Backup and Sync cloud options use detail labels and share one low-profile row when width allows",
        "Compact Backup and Sync cloud options stack full-width on narrow compact viewports",
        "Pull/Push controls appear before lower-frequency cloud options",
        "Pull from Cloud appears before Push to Cloud",
        "Compact cloud labels name Pull as Android-directed and Push as Steam-directed",
        "Compact Pull action says PULL TO ANDROID / Download saves",
        "Compact locked Push toggle renders STEAM PUSH LOCKED / Open overwrite and HIDE PUSH / Close overwrite as structured title/detail labels",
        "Compact unlocked Push actions say PUSH TO STEAM / Upload Android and CONFIRM OVERWRITE / Final upload",
        "Compact armed Push warning says STEAM CLOUD OVERWRITE / Confirm only after Pull/local saves are verified",
        "Compact Pull and locked Steam Push share one two-button row when width allows",
        "Compact Pull and locked Steam Push stack with Pull first on narrow compact viewports",
        "Armed Push state shows overwrite warning before final confirmation",
        "Branch, redownload, cache, and final Push confirmations use contextual confirm/cancel labels instead of generic OK/Cancel buttons",
        "Long compact confirmation warnings are scroll-safe and keep confirm/cancel buttons reachable",
        "Confirmation dialogs use the current visible viewport after rotation or keyboard-driven viewport changes",
        "Focused managed Steam Guard/fallback input stays visible above the Android soft keyboard",
        "Compact optional drawer toggles remain tappable without taking full primary-action height.",
        "Compact optional drawer toggles use a dense touch-safe height instead of the older tiny drawer rows.",
        "Compact drawer toggles and dense workflow controls share the same touch-safe compact height:",
        "Compact optional drawer toggles are visibly shorter than primary action buttons while still tappable.",
        "Diagnostics console hidden by default",
        "Compact diagnostics toggle uses a touch-safe two-line detail label",
        "Compact diagnostics toggle renders title/detail labels as structured controls",
        "Compact diagnostics is inside the scroll body rather than fixed root chrome",
        "Raw startup fallback failure text hidden from portal",
        "The compact workflow strip shows short visible step labels such as `SIGN IN`, `GUARD`, `FILES`, and `PLAY`; it does not rely on hover-only tooltips",
        "The compact workflow strip is touch-safe enough for Android while keeping step labels readable",
        "The compact game-version dropdown is large enough to read and tap when the version drawer is expanded",
        "Opening the compact game-version dropdown shows larger touch-safe popup row spacing and horizontal padding",
        "Compact Play and Sync ready-version summary is a readable card when width allows",
        "Compact Play and Sync ready-version summary becomes a readable two-line Pull-first, Push-locked cue on narrow compact viewports",
        "Username keyboard next action focuses password",
        "NEXT: PASSWORD button focuses password and requests password suggestions",
        "Password keyboard done action attempts submit",
        "Password-manager suggestions",
        "Samsung Pass",
        "Google Password Manager",
        "Steam Guard prompt visible",
        "Steam Guard field accepts alphanumeric input",
        "Compact Steam Guard code field and Verify button are large enough to tap comfortably",
        "Compact Steam Guard shows code field and VERIFY CODE before helper copy",
        "Compact Steam Guard code field and VERIFY CODE share one touch-safe action row when width allows",
        "Compact Steam Guard code field and VERIFY CODE stack full-width on narrow compact viewports",
        "Compact Steam Guard submit action says VERIFY CODE / Submit once",
        "Compact Steam Guard retry keeps the rejected-code title short and moves latest-code guidance into the helper below the code controls",
        "Compact retry/failure recovery button is primary and uses TRY AGAIN / Restart task labels",
        "Compact raw-log copy action says COPY LOG / Review first",
        "Wrong password produces recoverable failure",
        "Failed-login status gives clear retry guidance",
        "Failed-login status states Steam passwords are not stored",
        "Connection/session failure gives connection-specific recovery guidance",
        "Steam Guard section states code is submitted once and never stored",
        "Wrong Steam Guard code asks for the latest Steam Guard code",
        "Successful login returns to launcher",
        "Game version dropdown visible/readable",
        "Version/download guidance states local game files are separate from Steam Cloud saves",
        "Ready-state version details repeat that Steam Cloud saves move only through Pull/Push",
        "Play and Sync section appears when actions are available",
        "Pull from Cloud completed",
        "Push to Cloud guarded by confirmation",
        "Game launch completed",
        "Native credential panel inline status configured",
        "Native credential panel keyboard-safe layout configured",
        "Native credential panel IME inset scroll supported",
        "Native credential panel touch-target layout configured",
        "Native credential panel requests both Autofill fields",
        "Native credential panel focus Autofill requests supported",
        "Native credential panel task-led buttons supported",
        "Native credential panel responsive action rows supported",
        "Native credential panel orientation reflow supported",
        "Native credential panel short-height copy supported",
        "Native credential panel short-height reflow supported",
        "Native credential panel IME height reflow supported",
        "Native credential panel password visibility toggle supported",
        "Native credential panel password-focus button supported",
        "Native credential panel Back dismiss supported",
        "Native credential panel dismiss retry supported",
        "Native credential panel dismiss hides keyboard",
        "Native credential panel suppresses pre-auth save prompt",
        "Steam Guard one-shot code guidance supported",
        "Failed-login retry guidance supported",
        "Context-specific login recovery guidance supported",
        "Launcher portal UX model",
        "Launcher phase-labeled status supported",
        "Launcher structured status chip supported",
        "Launcher guided next-action status supported",
        "Launcher error-first guided status supported",
        "Launcher safe first-run guidance supported",
        "Launcher compact safe-flow guidance collapsible",
        "Launcher compact low-profile safe-flow toggle supported",
        "Launcher compact safe-flow toggle detail labels supported",
        "Launcher compact structured safe-flow toggle labels supported",
        "Launcher compact safe-flow bounded guide supported",
        "Launcher compact active-task safe-flow suppression supported",
        "Launcher mobile-first compact layout supported",
        "Launcher compact dense panel padding supported",
        "Launcher compact dense vertical rhythm supported",
        "Launcher rounded scaled metrics supported",
        "Launcher Android compact touch-scale floor supported",
        "Launcher Android-readable warmup screen supported",
        "Launcher Android-readable startup status card supported",
        "Launcher compact dynamic content width supported",
        "Launcher tablet/wide content layout supported",
        "Launcher top-anchored portal content supported",
        "Launcher compact vertical status hero supported",
        "Launcher compact low-profile status card supported",
        "Launcher compact status headline row supported",
        "Launcher compact stacked status headline supported",
        "Launcher viewport-aware compact status headline reflow supported",
        "Launcher compact stable status detail row supported",
        "Launcher compact sticky workflow step strip supported",
        "Launcher compact low-profile workflow step strip supported",
        "Launcher compact low-profile two-column workflow step strip supported",
        "Launcher compact workflow step direct navigation supported",
        "Launcher compact single-row numbered workflow step strip supported",
        "Launcher compact two-column workflow step strip supported",
        "Launcher compact narrow workflow single-row supported",
        "Launcher compact visible workflow step labels supported",
        "Launcher compact workflow step number badges supported",
        "Launcher compact current-task jump supported",
        "Launcher compact sticky current-task bar supported",
        "Launcher compact low-profile current-task bar supported",
        "Launcher compact dense inline current-task bar supported",
        "Launcher compact current-task shared touch height supported",
        "Launcher compact low-profile stacked current-task bar supported",
        "Launcher compact current-task context labels supported",
        "Launcher compact structured current-task labels supported",
        "Launcher compact current-task short title labels supported",
        "Launcher compact touch-safe sticky header controls supported",
        "Launcher compact grouped sticky task header supported",
        "Launcher compact sticky task toolbar shell supported",
        "Launcher compact inline sticky task header supported",
        "Launcher compact responsive sticky task header supported",
        "Launcher viewport-aware sticky task header reflow supported",
        "Launcher viewport-aware compact task re-anchor supported",
        "Launcher compact dense sticky task header supported",
        "Launcher compact task-jump navigation labels supported",
        "Launcher compact readable detail label font supported",
        "Launcher compact padded scroll anchors supported",
        "Launcher keyboard-focused input scroll supported",
        "Launcher compact selected-version headline supported",
        "Launcher compact responsive selected-version summary supported",
        "Launcher compact contextual confirmation labels supported",
        "Launcher compact scroll-safe confirmation dialogs supported",
        "Launcher viewport-aware confirmation dialogs supported",
        "Launcher compact Steam Guard large input supported",
        "Launcher compact Steam Guard action-first layout supported",
        "Launcher compact Steam Guard inline action row supported",
        "Launcher compact responsive Steam Guard action layout supported",
        "Launcher viewport-aware compact Steam Guard action row reflow supported",
        "Launcher compact Steam Guard submit detail label supported",
        "Launcher compact Steam Guard retry guidance supported",
        "Launcher compact Steam Guard bounded helper supported",
        "Launcher compact primary retry action supported",
        "Launcher compact structured retry action labels supported",
        "Launcher compact primary login action first supported",
        "Launcher compact Android login primary CTA supported",
        "Launcher compact Android login detail label supported",
        "Launcher compact Android login helper detail label supported",
        "Launcher compact completed-auth section suppression supported",
        "Launcher touch-first action targets supported",
        "Launcher primary action wording supported",
        "Launcher consistent START GAME CTA supported",
        "Launcher compact launch detail label supported",
        "Launcher branded atmospheric background supported",
        "Launcher branded background explicit RGBA supported",
        "Launcher high-contrast rounded actions supported",
        "Launcher compact header chrome reduction supported",
        "Launcher compact condensed brand header supported",
        "Launcher compact single-line brand header supported",
        "Launcher compact readable brand subtitle supported",
        "Launcher compact section-header subtitle suppression supported",
        "Launcher compact low-profile section headers supported",
        "Launcher compact single-row section headers supported",
        "Launcher compact section-header task cues supported",
        "Launcher compact install primary action first supported",
        "Launcher compact install primary detail label supported",
        "Launcher compact download progress hero supported",
        "Launcher compact download progress status label supported",
        "Launcher compact inline install-version controls supported",
        "Launcher compact version details collapsible",
        "Launcher compact structured install-version action labels supported",
        "Launcher compact ready-version summary panel supported",
        "Launcher compact ready-version headline supported",
        "Launcher compact responsive ready-version summary supported",
        "Launcher compact ready-state install-section suppression supported",
        "Launcher compact touch-safe version dropdown supported",
        "Launcher compact touch-safe dropdown popup supported",
        "Launcher compact cloud-safety cue before actions supported",
        "Launcher compact cloud-safety detail label supported",
        "Launcher compact cloud direction labels supported",
        "Launcher compact cloud primary actions row supported",
        "Launcher compact Pull detail label supported",
        "Launcher compact responsive action rows supported",
        "Launcher compact dangerous Push detail labels supported",
        "Launcher compact armed Push warning detail label supported",
        "Launcher compact raw-log review label supported",
        "Launcher compact cloud options row supported",
        "Launcher compact cloud option detail labels supported",
        "Launcher version-install/cloud-save separation guidance supported",
        "SteamKit debug logs sanitized for credentials/tokens",
        "Release signoff is not valid"
    )

Add-Check `
    "docs\steam-version-selection-release-note-snippet.md" `
    "describes the current polished launcher portal UX alongside version-selection limitations" `
    @(
        "cleaner status-led portal",
        "titled action sections",
        "hidden diagnostics drawer",
        "readable bounded compact diagnostics log viewport",
        "stronger branded header",
        "single-line compact brand",
        "readable compact brand subtitle",
        "responsive compact status headline row with stacked narrow-screen fallback",
        "viewport-aware compact status headline reflow after rotation or keyboard viewport changes",
        "stable one-line compact status detail row",
        "Android compact touch-scale floor for small-device readability",
        "Android-readable shader warmup/loading",
        "Android-readable post-launch startup status card",
        "responsive touch-safe compact sticky task header in a low-profile toolbar shell with a dense inline current-task button that uses the shared compact touch-safe control height",
        "contextual current-task detail labels",
        "low-profile stacked current-task row",
        "dense single-row compact workflow strip on narrow screens",
        "viewport-aware sticky task header reflow after rotation or keyboard viewport changes",
        "viewport-aware compact task re-anchoring after rotation or keyboard viewport changes",
        "readable step-number badges",
        "SIGN IN WITH STEAM / Android login",
        "VERIFY CODE / Submit once",
        "START GAME / Selected version",
        "structured Play/Sync title/detail action labels",
        "Cloud target / PULL downloads to Android / PUSH can overwrite Steam",
        "PULL TO ANDROID / Download saves",
        "structured locked-Push title/detail labels",
        "PUSH TO STEAM / Upload Android",
        "CONFIRM OVERWRITE / Final upload",
        "STEAM CLOUD OVERWRITE / Confirm only after Pull/local saves are verified",
        "responsive compact action rows",
        "responsive selected-version install summary",
        "touch-safe compact version dropdown popups",
        "inline compact install-version dropdown/refresh controls with structured title/detail labels",
        "version details with structured compact local-files-only drawer labels",
        "responsive ready-version summary",
        "large compact Android sign-in CTA with readable two-line",
        "responsive compact Steam Guard code controls",
        "viewport-aware compact Steam Guard code/action row reflow after rotation or keyboard viewport changes",
        "compact Steam Guard bounded two-line helper labels",
        "primary structured compact retry recovery",
        "compact support tool detail labels including raw-log review labeling",
        "short-height copy on cramped landscape screens",
        "short-height copy reflow when the landscape height class changes",
        "keyboard-reduced usable height",
        "scroll-safe compact confirmations",
        "bounded compact setup safe-flow panel",
        "collapsible setup safe-flow guidance with structured compact Pull-then-play title/detail labels that suppresses during active compact task screens",
        "dense compact drawer toggles",
        "structured compact title/detail labels",
        "native fallback recovery screens that keep verbose diagnostics collapsed until requested and split actions into responsive rows on narrow landscape screens",
        "dense compact vertical rhythm",
        "single-row compact section headers with readable task cues",
        "mobile-first compact panel sizing with dense compact shell padding",
        "Steam sign-in/Steam Guard/install/play-sync sections",
        "Android/Samsung/password-manager suggestion behavior"
    )

Add-Check `
    "docs\launcher-loading-screen-staging.md" `
    "documents Android-readable shader warmup staging" `
    @(
        "Android shader warmup uses the launcher compact touch-scale floor",
        "mobile-width compact panel",
        "styled percentage progress bar",
        "Android game startup status now uses a framed mobile-width status card",
        "Successful startup cleanup now frees the whole Android startup status root container"
    )

Add-Check `
    "docs\release-and-backport-policy.md" `
    "requires release notes to name branch/version limitations" `
    @(
        "Steam beta/version selection proof",
        "audit-steam-version-selection\.ps1",
        "audit-steam-branch-guidance-parity\.ps1",
        "beta password/private branch behavior",
        "save compatibility across branches"
    )

Add-Check `
    "docs\steam-version-selection-release-note-snippet.md" `
    "prevents release notes from overclaiming branch/version readiness" `
    @(
        "validation-stage",
        "Known limitations",
        "Do not say yet",
        "REFRESH GAME VERSIONS",
        "dropdown-first",
        "password-manager suggestion behavior",
        "SteamKit debug logs are disabled by default",
        "sts2_steamkit_debug_logs=1",
        "wrapped selector guidance",
        "managed/native selector-guidance parity",
        "audit-steam-version-selection\.ps1",
        "audit-steam-branch-guidance-parity\.ps1",
        "Password-protected beta branches",
        "Steam Cloud Push is safe",
        "last_manual_cloud_push\.txt",
        "aggregate successful post-switch Push evidence",
        "bounded two-line Install target / Launch target helper labels"
    )

Add-Check `
    "docs\current-android-status.md" `
    "keeps Android status current for version selection, credential providers, and credential-log hardening" `
    @(
        "Steam game version selection is in hardening",
        "steam-version-selection-release-readiness\.md",
        "android-steam-login-validation\.md",
        "discovery-led dropdown Steam branch selector",
        "password-manager login behavior",
        "does not store or inject Steam passwords",
        "SteamKit debug logs are disabled by default",
        "sts2_steamkit_debug_logs=1",
        "native fallback keeps verbose diagnostics collapsed until requested",
        "ARM64 device validation"
    )

Add-Check `
    "README.md" `
    "advertises version selection as published but not release-candidate signed off" `
    @(
        "implemented for validation",
        "steam-version-selection-release-readiness\.md",
        "not release-candidate signed off",
        "discovery-led dropdown selector",
        "REFRESH GAME VERSIONS",
        "public-inherited",
        "public-vs-beta integrity classification",
        "steam-beta-integrity-runtime-checklist\.md",
        "mixed beta/public behavior",
        "Autofill",
        "SteamKit debug logs are disabled by default",
        "Steam beta password entry",
        "Push backup evidence"
    )

if ($failures.Count -gt 0) {
    Write-Host "Steam version-selection static audit failed:"
    foreach ($failure in $failures) {
        Write-Host "FAIL $failure"
    }
    exit 1
}

Write-Host "Steam version-selection static audit passed: $passes checks."
