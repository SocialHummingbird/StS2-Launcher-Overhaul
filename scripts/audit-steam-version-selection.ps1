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
        "SelectedOptionDownloadProblem",
        "DropdownLabelWithMetadata",
        "StatusText",
        "windowsManifestDepots",
        "passwordRequired",
        "buildId",
        "\(password\)",
        "\(unavailable\)",
        "\(ready\)",
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
        "NativeCredentialPanelRequestsBothAutofillFields\s*=\s*true",
        "NativeCredentialPanelFocusAutofillRequestsSupported\s*=\s*true",
        "NativeCredentialPanelTaskLedButtonsSupported\s*=\s*true",
        "NativeCredentialPanelPasswordVisibilityToggleSupported\s*=\s*true",
        "NativeCredentialPanelPasswordFocusButtonSupported\s*=\s*true",
        "NativeCredentialPanelBackDismissSupported\s*=\s*true",
        "NativeCredentialPanelDismissRetrySupported\s*=\s*true",
        "NativeCredentialPanelDismissHidesKeyboardSupported\s*=\s*true",
        "NativeCredentialPanelSuppressesPreAuthSavePrompt\s*=\s*true",
        "SteamGuardOneShotCodeGuidanceSupported\s*=\s*true",
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
        "keyboard-safe scrollable top-weighted layout with IME inset padding and focus scrolling",
        "branded task-led full-width touch controls",
        "manual password visibility toggle that resets to hidden",
        "one-shot Steam Guard code guidance",
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
    "src\STS2Mobile\Launcher\Sections\LauncherSectionSetup.cs" `
    "frames launcher states with explicit titled portal sections" `
    @(
        "ConfigureHiddenSection",
        "BuildSectionHeader",
        "bool compact",
        "!compact",
        "title",
        "subtitle",
        "accent",
        "BuildHeaderStyle",
        "StyledLabel",
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
        "SHOW SAFE FLOW",
        "HIDE SAFE FLOW",
        "profile\.Compact",
        "Safe flow: Sign in -> Download version -> Pull saves -> Launch",
        "Safe first-run flow",
        "Pull saves before any Push",
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
        "drawer\.Visible = false"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.cs" `
    "presents launcher as a polished Steam/version/cloud portal instead of a generic debug shell" `
    @(
        "StS2 Mobile",
        "STEAM \| CLOUD \| PLAY",
        "STEAM LOGIN\s*\|\s*VERSION SLOTS\s*\|\s*CLOUD SAVES",
        "BuildBrandMark",
        "profile\.Compact",
        "compact \? 40 : 50",
        "profile\.Compact \? 1 : 2",
        "OrangeAccent",
        "CyanAccent"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherLayoutProfile.cs" `
    "uses more available phone screen space for compact launcher layouts" `
    @(
        "panelWidth = compact \? 0\.98f",
        "panelHeight = compact \? 0\.98f",
        "Math\.Min\(safeViewport\.X \* 0\.94f, 1120f\)",
        "Math\.Min\(safeViewport\.X \* 0\.84f, 1180f\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Components\StyledPanel.cs" `
    "reduces compact-mode panel padding and avoids a short fixed-height phone panel" `
    @(
        "MaxHeight = 2200f",
        "compact \? 12",
        "BuildStyle\(scale, compact\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.cs" `
    "states that version downloads affect local files and do not mutate Steam Cloud saves" `
    @(
        "SHOW VERSION DETAILS",
        "HIDE VERSION DETAILS",
        "CompactDownloadButtonText",
        "REFRESH VERSIONS",
        "DOWNLOAD",
        "ToggleBranchDetails",
        "_branchDetailsExpanded",
        "Download/update changes local files for the selected game version only",
        "does not change Steam Cloud saves",
        "SelectedOptionStatus",
        "SelectorInstallSlotHelpText"
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
        "MobileFirstCompactLayoutSupported\s*=\s*true",
        "CompactDynamicContentWidthSupported\s*=\s*true",
        "TabletWideContentLayoutSupported\s*=\s*true",
        "PortalTopAnchoredContentSupported\s*=\s*true",
        "CompactVerticalStatusHeroSupported\s*=\s*true",
        "TouchFirstActionTargetsSupported\s*=\s*true",
        "PrimaryActionWordingSupported\s*=\s*true",
        "ConsistentStartGameCtaSupported\s*=\s*true",
        "BrandedAtmosphericBackgroundSupported\s*=\s*true",
        "BrandedBackgroundExplicitRgbaSupported\s*=\s*true",
        "HighContrastRoundedActionsSupported\s*=\s*true",
        "CompactHeaderChromeReductionSupported\s*=\s*true",
        "CompactSectionHeaderSubtitleSuppressionSupported\s*=\s*true",
        "CompactVersionDetailsCollapsibleSupported\s*=\s*true",
        "CompactCloudSafetyCollapsibleSupported\s*=\s*true",
        "CompactCloudOptionsCollapsibleSupported\s*=\s*true",
        "PrimaryCloudActionsBeforeCloudOptionsSupported\s*=\s*true",
        "SaferPullBeforePushOrderingSupported\s*=\s*true",
        "ManualPushArmedOverwriteWarningSupported\s*=\s*true",
        "VersionInstallCloudSeparationGuidanceSupported\s*=\s*true",
        "DiagnosticsConsoleHiddenByDefault\s*=\s*true",
        "StartupFallbackRawBannerSuppressed\s*=\s*true",
        "PortalUxDeviceValidated\s*=\s*false",
        "Status-led launcher portal",
        "Steam sign-in",
        "Steam Guard",
        "game install",
        "play/sync",
        "ARM64 visual validation"
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
        "Push Saves",
        "Confirm Cloud Overwrite",
        "Pull Saves",
        "PushButtonText",
        "PushConfirmButtonText",
        "Pull Saves from Steam Cloud",
        "CloudPushArmRequested",
        "CloudPushArmRequested\?\.Invoke\(\) == false",
        "ArmCloudPush",
        "ConfirmCloudPush",
        "ResetCloudPushArm",
        "Pull/local saves are verified",
        "can overwrite remote Steam Cloud saves"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.cs" `
    "uses explicit Steam Cloud direction wording in ready/action state" `
    @(
        "Pull copies Steam Cloud saves to Android",
        "Push copies Android saves to Steam Cloud",
        "can overwrite remote saves",
        "Version/download actions affect local game files only",
        "Steam Cloud saves move only through Pull/Push"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.cs" `
    "shows cloud-save safety guidance beside Pull/Push actions" `
    @(
        "_cloudSafetyLabel",
        "_cloudGroup.AddChild\(_pushPullRow\)",
        "OrangeHot",
        "Pull Saves from Steam Cloud",
        "PushButtonText",
        "PushConfirmButtonText",
        "SHOW CLOUD OPTIONS"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\CodeSection.cs" `
    "uses numeric keyboard for Steam Guard code entry" `
    @(
        "VirtualKeyboardType\.Number",
        "Enter Steam Guard code",
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
        "onBackPressed",
        "dispatchKeyEvent",
        "KEYCODE_BACK",
        "dismissSteamLoginCredentialPanelFromBack",
        "isSteamLoginCredentialPanelVisible",
        "public boolean isSteamLoginCredentialPanelVisible",
        "hideKeyboardForSteamLoginCredentialPanel",
        "focusedView\.clearFocus\(\)",
        "styleSteamLoginCredentialButton",
        "ScrollView",
        "steamLoginCredentialScrollView",
        "updateSteamLoginCredentialKeyboardInsets",
        "scheduleSteamLoginCredentialFocusScroll",
        "Gravity\.TOP \| Gravity\.CENTER_HORIZONTAL",
        "scroll\.setClipToPadding\(false\)",
        "buttons\.setOrientation\(LinearLayout\.VERTICAL\)",
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
        "Native credential panel requests both Autofill fields",
        "Native credential panel focus Autofill requests supported",
        "Native credential panel task-led buttons supported",
        "Native credential panel password visibility toggle supported",
        "Steam Guard one-shot code guidance supported",
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
        "Launcher mobile-first compact layout supported",
        "Launcher compact dynamic content width supported",
        "Launcher tablet/wide content layout supported",
        "Launcher top-anchored portal content supported",
        "Launcher compact vertical status hero supported",
        "Launcher touch-first action targets supported",
        "Launcher primary action wording supported",
        "Launcher consistent START GAME CTA supported",
        "Launcher branded atmospheric background supported",
        "Launcher branded background explicit RGBA supported",
        "Launcher high-contrast rounded actions supported",
        "Launcher compact header chrome reduction supported",
        "Launcher compact section-header subtitle suppression supported",
        "Launcher compact version details collapsible",
        "Launcher compact cloud-safety guidance collapsible",
        "Launcher compact cloud options collapsible",
        "Launcher primary cloud actions before cloud options",
        "Launcher safer Pull-before-Push cloud ordering supported",
        "Launcher manual Push armed overwrite warning supported",
        "Launcher version-install/cloud-save separation guidance supported",
        "Launcher diagnostics console hidden by default",
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
        "portal clearly exposes the next action",
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
        "Native credential panel requests both Autofill fields:",
        "Native credential panel focus Autofill requests supported:",
        "Native credential panel task-led buttons supported:",
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
        "Launcher mobile-first compact layout supported:",
        "Launcher compact dynamic content width supported:",
        "Launcher tablet/wide content layout supported:",
        "Launcher top-anchored portal content supported:",
        "Launcher compact vertical status hero supported:",
        "Launcher touch-first action targets supported:",
        "Launcher primary action wording supported:",
        "Launcher consistent START GAME CTA supported:",
        "Launcher branded atmospheric background supported:",
        "Launcher branded background explicit RGBA supported:",
        "Launcher high-contrast rounded actions supported:",
        "Launcher compact header chrome reduction supported:",
        "Launcher compact section-header subtitle suppression supported:",
        "Launcher compact version details collapsible:",
        "Launcher compact cloud-safety guidance collapsible:",
        "Launcher compact cloud options collapsible:",
        "Launcher primary cloud actions before cloud options:",
        "Launcher safer Pull-before-Push cloud ordering supported:",
        "Launcher manual Push armed overwrite warning supported:",
        "Launcher version-install/cloud-save separation guidance supported:",
        "Launcher diagnostics console hidden by default:",
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
        "Native login controls are stacked/full-width and easy to tap",
        "Native login primary button says SIGN IN WITH STEAM",
        "Native login panel requests suggestions for username and password fields",
        "Native login panel requests suggestions again when username/password fields gain focus",
        "Native login NEXT: PASSWORD control focuses password field",
        "Back/Cancel dismissal hides the soft keyboard before returning to launcher",
        "Provider does not prompt to save unverified credentials before Steam authentication",
        "Password visibility toggle shows/hides password without storing it",
        "Password visibility resets to hidden after submit/cancel/reopen",
        "Safe first-run flow guidance visible",
        "On compact phone layout, safe-flow guidance starts collapsed",
        "Safe-flow guidance expands/collapses without hiding the primary task",
        "Compact phone layout uses most of the usable screen height",
        "Compact phone layout avoids excessive internal panel margins",
        "Compact phone layout uses dynamic content width instead of a narrow fixed column",
        "Tablet/wide layout avoids a narrow fixed inner column",
        "Portal task flow is top anchored rather than vertically stranded",
        "Compact phone status appears as a readable vertical next-step card",
        "Status card shows a clear guided next action for the current state",
        "Failure/blocked/crash statuses show attention/fix guidance before normal install/cloud/launch guidance",
        "Primary actions use clear task wording, for example sign in/start game/verify code",
        "Primary launch action consistently says START GAME",
        "Primary and secondary actions are large enough to tap comfortably",
        "Launcher background has visible branded atmosphere without reducing readability",
        "Buttons use high-contrast rounded action styling",
        "Compact phone header uses shortened subtitle/chrome",
        "Compact phone section headers avoid repeated subtitle blocks",
        "Compact phone version details start collapsed",
        "Version details expand/collapse without changing selected version",
        "Compact phone cloud safety starts collapsed",
        "Cloud safety expands/collapses while preserving Pull/Push controls",
        "Compact phone cloud options start collapsed",
        "Cloud options expand/collapse while preserving Pull/Push controls",
        "Pull/Push controls appear before lower-frequency cloud options",
        "Pull from Cloud appears before Push to Cloud",
        "Armed Push state shows overwrite warning before final confirmation",
        "Diagnostics console hidden by default",
        "Raw startup fallback failure text hidden from portal",
        "Username keyboard next action focuses password",
        "NEXT: PASSWORD button focuses password and requests password suggestions",
        "Password keyboard done action attempts submit",
        "Password-manager suggestions",
        "Samsung Pass",
        "Google Password Manager",
        "Steam Guard prompt visible",
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
        "Launcher mobile-first compact layout supported",
        "Launcher compact dynamic content width supported",
        "Launcher tablet/wide content layout supported",
        "Launcher top-anchored portal content supported",
        "Launcher compact vertical status hero supported",
        "Launcher touch-first action targets supported",
        "Launcher primary action wording supported",
        "Launcher consistent START GAME CTA supported",
        "Launcher branded atmospheric background supported",
        "Launcher branded background explicit RGBA supported",
        "Launcher high-contrast rounded actions supported",
        "Launcher compact header chrome reduction supported",
        "Launcher compact section-header subtitle suppression supported",
        "Launcher compact version details collapsible",
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
        "stronger branded header",
        "Steam sign-in/Steam Guard/install/play-sync sections",
        "Android/Samsung/password-manager suggestion behavior"
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
        "aggregate successful post-switch Push evidence"
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
