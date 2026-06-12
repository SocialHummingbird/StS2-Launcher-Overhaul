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
        "StableBranchHash",
        "safePrefix",
        "TrimEnd",
        "StableBranchHash\(branch\)"
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
        "StatusText",
        "windowsManifestDepots",
        "passwordRequired",
        "buildId",
        "Steam app-info visible branch catalog",
        "Distinct",
        "SteamGameBranch\.Public"
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
        "OptionButton",
        "_branchDropdown",
        "LauncherBranchCatalog\.DropdownOptions",
        "SteamGameBranch\.SelectorInstallSlotHelpText",
        "ItemSelected"
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
        "CacheCleanupMarkerRemovedCount",
        "CacheCleanupMarkerSelectedCachePreservedWhereApplicable",
        "last_game_version_cache_cleanup\.txt",
        "Preserved selected cache",
        "Removed count",
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
        "ClearCachedVersionsPressed",
        "ClearCachedVersions",
        "DeleteInactiveVersionCaches",
        "Selected version preserved",
        "SteamGameBranch\.DisplayName"
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
        "BranchSwitchSafety",
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
        'string\? ReadSelectedBranch',
        "return false",
        "Manual Pull completed before branch-switch Push",
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
        "AppendLog\(STS2Mobile\.Steam\.SteamGameBranch\.SelectorInstallSlotHelpText",
        "SelectedVersionReadyStatus",
        "SelectedVersionDownloadRequiredStatus",
        "SteamGameInstallPaths\.VersionSlotKind",
        "Active install slot",
        "Steam Cloud Push will require backup storage permission"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherAutofillSupport.cs" `
    "declares Android password-manager Autofill support without app-owned password storage" `
    @(
        "AppStoresSteamPassword\s*=\s*false",
        "NativeAndroidAutofillOverlaySupported\s*=\s*true",
        "GodotFieldAutofillHintsConfigured\s*=\s*true",
        "NativeDialogResultTtlSeconds",
        "Android/Samsung/password-manager Autofill only",
        "must not store or inject Steam passwords",
        "native one-shot Autofill dialog",
        "autofill_hint",
        "credential_storage_owner",
        "android_autofill_provider"
    )

Add-Check `
    "src\STS2Mobile\AndroidGodotAppBridge.cs" `
    "bridges native Android Autofill dialog results without persistence" `
    @(
        "showSteamLoginAutofillDialog",
        "consumeSteamLoginAutofillResult",
        "TryConsumeSteamLoginAutofillResult",
        "DecodeBase64Utf8"
    )

Add-Check `
    "android\src\com\game\sts2launcher\GodotApp.java" `
    "shows native Android Autofill login dialog without logging or storing credentials" `
    @(
        "showSteamLoginAutofillDialog",
        "EditText",
        "setAutofillHints",
        "AUTOFILL_HINT_USERNAME",
        "AUTOFILL_HINT_PASSWORD",
        "consumeSteamLoginAutofillResult",
        "pendingAutofillLoginUsername",
        "pendingAutofillLoginPassword",
        "AUTOFILL_LOGIN_RESULT_TTL_MS",
        "clearAutofillLoginLocked"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\LoginSection.cs" `
    "marks Steam login fields for password-manager Autofill metadata" `
    @(
        "ConfigureUsernameField",
        "ConfigurePasswordField",
        "USE ANDROID AUTOFILL",
        "ShowSteamLoginAutofillDialog",
        "TryConsumeSteamLoginAutofillResult",
        "ClearPassword",
        "LoginRequested"
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
        "Android credential Autofill provider model",
        "Godot login field Autofill hints configured",
        "Native Android Autofill overlay supported",
        "Launcher stores Steam password for Autofill",
        "Native Android Autofill result TTL seconds",
        "Android credential Autofill implementation note",
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
        "Game version cache cleanup marker removed count",
        "Game version cache cleanup marker selected cache preserved where applicable",
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
        "beta password",
        "save compatibility",
        "steam-version-selection-runbook\.md"
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
        "Requirement audit",
        "selected-version note",
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

Add-Check `
    "scripts\new-steam-version-selection-evidence.ps1" `
    "creates a structured artifact folder for ARM64 validation evidence" `
    @(
        "steam-version-selection",
        "branch-markers",
        "backup-evidence",
        "Resolve-RepoPath",
        "steam-version-selection-evidence-template\.md",
        "Manual Push evidence marker filename",
        "Do not run manual Push"
    )

Add-Check `
    "scripts\capture-steam-version-selection-evidence.ps1" `
    "captures non-secret device evidence for version-selection validation" `
    @(
        "logcat-steam-version-focused",
        "steam_branch\.txt",
        "last_game_branch_switch\.txt",
        "manualPullMarkerFileName",
        "last_manual_cloud_push\.txt",
        "last_manual_cloud_push_blocked\.txt",
        "last_steam_branch_availability\.txt",
        "game-version-cache-tree\.txt",
        "game-version-cache-sizes\.txt",
        "last_game_version_cache_cleanup\.txt",
        "last_game_version_redownload\.txt",
        "branchAvailabilityMarkerFileName",
        "backup-evidence",
        "pre-push-backup-list\.txt",
        "pre-push-backup-counts\.txt",
        "local-pre-push",
        "cloud-pre-push",
        "branchSwitchMarkerFileName",
        "Resolve-RepoPath",
        "branch-markers",
        "avoids shared preferences",
        "run-as"
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
    "docs\steam-version-selection-tooling.md" `
    "documents static audit and evidence capture helper usage" `
    @(
        "audit-steam-version-selection\.ps1",
        "audit-steam-branch-guidance-parity\.ps1",
        "new-steam-version-selection-evidence\.ps1",
        "capture-steam-version-selection-evidence\.ps1",
        "normalize local path separators",
        "last_game_branch_switch\.txt",
        "last_manual_cloud_push\.txt",
        "last_manual_cloud_push_blocked\.txt",
        "pre-push-backup-counts\.txt",
        "Artifact hygiene",
        "Do not store Steam credentials",
        "Autofill versus local credential handoff",
        "developer-only automation aids"
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
        "Cloud Pull gate",
        "Backup permission gate",
        "Pre-Push backup evidence",
        "Manual Push smoke test"
    )

Add-Check `
    "docs\steam-version-selection-evidence-template.md" `
    "captures branch validation evidence" `
    @(
        "Selector mode",
        "Branch discovery",
        "Android credential Autofill",
        "Launcher stores Steam password for Autofill",
        "Steam branch dropdown option metadata",
        "Static guardrails",
        "audit-steam-version-selection\.ps1",
        "audit-steam-branch-guidance-parity\.ps1",
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
        "Manual Push blocked evidence recorded prerequisites satisfied",
        "Manual Push blocked evidence recorded local backup count",
        "Manual Push blocked evidence recorded cloud backup count",
        "Manual Push blocked evidence recorded latest local backup UTC",
        "Manual Push blocked evidence recorded latest cloud backup UTC",
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
        "Raw logs with identifying data"
    )

Add-Check `
    "docs\steam-version-selection-user-guide.md" `
    "keeps tester-facing support boundaries and cloud safety rules visible" `
    @(
        "implemented for validation",
        "What is not supported yet",
        "REFRESH GAME VERSIONS",
        "Steam login Autofill",
        "Android credential Autofill provider model",
        "Native Android Autofill result TTL seconds",
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
        "Steam credentials",
        "refresh tokens",
        "shared preferences",
        "device identifiers",
        "Release readiness"
    )

Add-Check `
    ".github\ISSUE_TEMPLATE\steam_version_selection_report.md" `
    "keeps public Steam version-selection reports free of secrets and identifiers" `
    @(
        "Artifact hygiene",
        "Steam credentials",
        "refresh tokens",
        "shared preferences",
        "device identifiers",
        "local user paths",
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
        "Game version cache cleanup marker removed count",
        "Game version cache cleanup marker selected cache preserved where applicable",
        "Game version redownload marker filename",
        "Game version redownload marker path",
        "Game version redownload marker present",
        "Game version redownload marker selected branch",
        "Game version redownload marker selected version",
        "Game version redownload marker selected version slot kind",
        "Game version redownload marker selected version slot directory",
        "Branch switch marker filename",
        "Manual Pull evidence marker filename",
        "Manual Pull evidence marker path",
        "Manual Pull evidence UTC",
        "Manual Pull evidence UTC parseable",
        "Manual Pull evidence selected branch",
        "Manual Pull evidence selected version",
        "Manual Pull evidence selected version slot kind",
        "Manual Pull evidence selected version slot directory",
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
        "Autofill",
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
        "Android Autofill",
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
    "README.md" `
    "advertises version selection as validation-stage, not release-signed" `
    @(
        "implemented for validation",
        "not release-signed",
        "dropdown-first selector",
        "REFRESH GAME VERSIONS",
        "Autofill",
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
