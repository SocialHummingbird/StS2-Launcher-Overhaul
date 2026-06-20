param(
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "static-audit-utils.ps1")
Initialize-StaticAudit -ScriptRoot $PSScriptRoot -Quiet:$Quiet

Add-Check `
    "scripts\static-audit-utils.ps1" `
    "keeps shared static audit harness isolated from version-selection contracts" `
    @(
        "Initialize-StaticAudit",
        "Resolve-RepoPath",
        "Read-RepoFile",
        "Add-Check",
        "Add-ForbiddenCheck",
        "Complete-StaticAudit",
        "StaticAuditFailures",
        "StaticAuditPasses",
        "StaticAuditQuiet",
        "ThrowOnFailure"
    )

Add-Check `
    "scripts\evidence-marker-utils.ps1" `
    "centralizes marker-text parsing for release evidence scripts" `
    @(
        "Read-MarkerValueFromText",
        "Read-MarkerIntFromText",
        "Read-MarkerRowsFromText",
        "Read-BranchFromMarkerText",
        "MissingValue",
        "OrdinalIgnoreCase",
        "\[int\]::TryParse",
        'return @\(\$MissingValue\)'
    )

Add-Check `
    "scripts\android-shell-utils.ps1" `
    "centralizes Android shell quoting for run-as evidence capture" `
    @(
        "ConvertTo-AndroidShellSingleQuoted",
        "ConvertTo-AndroidShellPathSingleQuoted",
        "Unsupported single quote in device path",
        "-split",
        "-join",
        "return ConvertTo-AndroidShellSingleQuoted"
    )

Add-Check `
    "scripts\evidence-path-utils.ps1" `
    "centralizes repo-relative and evidence-relative path helpers" `
    @(
        "Resolve-EvidenceRepoPath",
        "Get-EvidenceRelativePath",
        "ConvertTo-EvidenceSafeFileName",
        "IsPathRooted",
        "MakeRelativeUri",
        "UnescapeDataString",
        "DirectorySeparatorChar",
        "empty",
        "\[\^A-Za-z0-9\._-\]"
    )

Add-Check `
    "scripts\evidence-redaction-utils.ps1" `
    "centralizes public evidence and focused log redaction patterns" `
    @(
        "ConvertTo-RedactedEvidenceText",
        "ConvertTo-RedactedLogLine",
        "Get-EvidenceTextFileExtensions",
        "Get-EvidenceImageFileExtensions",
        "Get-EvidenceLocalOnlyPathPatterns",
        "Test-EvidenceLocalOnlyPath",
        "Get-EvidenceSensitiveTextChecks",
        "Get-PublicEvidenceRedactionReviewFields",
        "Format-PublicEvidenceRedactionReviewFields",
        "Screenshots manually reviewed",
        "Credential suggestions absent",
        "Only sanitized diagnostics selected for public sharing",
        "redacted-local-path",
        "android-app-private",
        "redacted-device-serial",
        "redacted-email",
        "Bearer <redacted>",
        "logs\\\\logcat-full",
        "logs\\\\logcat-steam-version-focused",
        "logcat-\(\?!steam-version-focused-redacted\)",
        "startup-routing-focused",
        "credential/token assignment",
        "Android package-private data path",
        "known connected device serial",
        "saveData",
        "profileContent"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherMarkerFile.cs" `
    "declares shared marker-file sentinel values" `
    @(
        "internal static partial class LauncherMarkerFile",
        "MissingFileValue = ""<none>""",
        "MissingLineValue = ""<missing>""",
        "ReadFailedValue = ""<read failed>"""
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherMarkerFile.Read.cs" `
    "centralizes scalar marker-file value parsing" `
    @(
        "ReadValue",
        "ReadOptionalValue",
        "File\.ReadLines",
        "StringComparison\.OrdinalIgnoreCase"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherMarkerFile.Typed.cs" `
    "centralizes typed marker-file parsing" `
    @(
        "ReadInt",
        "NumberStyles\.Integer",
        "CultureInfo\.InvariantCulture",
        "ReadUtc",
        "UtcParseable",
        "DateTimeStyles\.AdjustToUniversal",
        "ReadBoolFlag"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherMarkerFile.Values.cs" `
    "centralizes marker-file repeated-value reads" `
    @(
        "ReadJoinedValues",
        "ReadValues",
        "File\.ReadLines",
        "valueFormatter",
        "maxValues"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherMarkerFile.Predicates.cs" `
    "centralizes marker-file predicates and counts" `
    @(
        "CountLines",
        "HasLine",
        "HasConcreteValue"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherGameFiles.Markers.cs" `
    "keeps game-file marker evidence readers as thin shared-helper wrappers" `
    @(
        "ReadMarkerValue",
        "LauncherMarkerFile\.ReadValue",
        "ReadMarkerInt",
        "LauncherMarkerFile\.ReadInt",
        "MarkerUtcParseable",
        "LauncherMarkerFile\.UtcParseable",
        "MarkerHasLine",
        "LauncherMarkerFile\.HasLine"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherUI.cs" `
    "keeps launcher root control as a thin partial wrapper around MVC state" `
    @(
        "internal sealed partial class LauncherUI : Control",
        "AutoLaunchVariable",
        "AutoSafeLaunchVariable",
        "LauncherZIndex",
        "DefaultViewportSize",
        "ConcurrentQueue<Action>",
        "LauncherModel _model",
        "LauncherView _view",
        "LauncherController _controller",
        "SetGameMode",
        "WaitForLaunch"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherUI.Lifecycle.cs" `
    "isolates launcher lifecycle registration and controller startup" `
    @(
        "internal void Initialize",
        "AndroidBridgeDispatcher\.RegisterCurrentThread",
        "LauncherLayoutProfile\.ForViewport",
        "ResolveLauncherDataDirectory",
        "new LauncherController",
        "tree\.AutoAcceptQuit = false",
        "tree\.ProcessFrame \+= OnProcessFrame",
        "Callable\.From\(StartControllerSafely\)\.CallDeferred",
        "StartControllerSafely",
        "AutoLaunchIfRequested",
        "TreeExiting \+= OnExitTree",
        "AndroidBridgeDispatcher\.UnregisterCurrentThread"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherUI.MainThread.cs" `
    "isolates main-thread queue pumping from lifecycle wiring" `
    @(
        "OnProcessFrame",
        "AndroidBridgeDispatcher\.Pump",
        "DrainMainThreadActions",
        "SyncViewportSize",
        "UpdateKeyboardOffset",
        "EnqueueMainThreadAction",
        "_mainThreadActions\.Enqueue",
        "_mainThreadActions\.TryDequeue",
        "UI update error"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherUI.Viewport.cs" `
    "isolates viewport synchronization and fallback sizing" `
    @(
        "SyncViewportSize",
        "GetViewportSize",
        "DistanceSquaredTo",
        "UpdateViewportSize",
        "DefaultViewportSize"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherUI.Paths.cs" `
    "isolates launcher data-directory resolution for Android and desktop" `
    @(
        "ResolveLauncherDataDirectory",
        "OperatingSystem\.IsAndroid",
        "AndroidGodotAppBridge\.GetInternalFilesDirPath",
        "STS2_ANDROID_FILES_DIR",
        "System\.Environment\.GetEnvironmentVariable",
        "OS\.GetDataDir",
        "BootstrapTrace\.ResolveFallbackDataDirectory"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherUI.AutoLaunch.cs" `
    "isolates launch-request environment handling from controller startup" `
    @(
        "AutoLaunchIfRequested",
        "_inGameMode",
        "Environment\.GetEnvironmentVariable\(AutoLaunchVariable\)",
        "Environment\.SetEnvironmentVariable\(AutoLaunchVariable, ""0""\)",
        "Environment\.GetEnvironmentVariable\(AutoSafeLaunchVariable\)",
        "Environment\.SetEnvironmentVariable\(AutoSafeLaunchVariable, ""0""\)",
        "LaunchSafe",
        "_model\.Launch"
    )

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
    "src\STS2Mobile\Launcher\LauncherBranchCatalog.Option.cs" `
    "describes selectable Steam branch option identity and metadata fields" `
    @(
        "internal readonly partial struct BranchOption",
        "SteamGameBranch\.Normalize",
        "MetadataVisible",
        "WindowsManifestDepotCount",
        "PasswordRequired",
        "BuildId",
        "Description",
        "Source"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchCatalog.Option.Label.cs" `
    "keeps selectable Steam branch dropdown label formatting isolated" `
    @(
        "Label",
        "DropdownLabelWithMetadata",
        "SteamGameBranch\.DropdownLabel",
        "\(installed\)",
        "\(password\)",
        "\(unavailable\)",
        "\(ready\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchCatalog.Option.Status.cs" `
    "keeps selectable Steam branch status text and download blockers isolated" `
    @(
        "StatusText",
        "Download blocked: Steam marks this branch as password-protected",
        "password gate still blocks this launcher from downloading it",
        "Download blocked: this branch is visible to this account, but no Windows depot manifest was exposed",
        "Download blocked: this branch was not listed in Steam branch metadata",
        "Steam app-info metadata has not been captured",
        "SteamGameBranch\.Public"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchCatalog.Read.cs" `
    "reads account-visible Steam branch catalog and merges it with installed branch slots" `
    @(
        "BranchAvailabilityMarkerPath",
        "VisibleBranchPrefix",
        "ReadVisibleBranches",
        "ReadVisibleBranchNames",
        "ReadSelectableBranches",
        "SourceDescription",
        "Steam app-info visible branch catalog",
        "GroupBy"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchCatalog.Read.Installed.cs" `
    "discovers locally installed non-public branch slots without adding public duplicates" `
    @(
        "ReadInstalledBranches",
        "LauncherStorageNames\.GameVersionsDirectory",
        "BranchMarkerBranchPrefix = ""Branch:""",
        "SteamGameInstallPaths\.BranchMarkerFileName",
        "SteamGameBranch\.Normalize",
        "SteamGameBranch\.Public",
        "SteamGameBranch\.StateDirectoryName",
        "local install",
        "ReadMarkerValue",
        "LauncherMarkerFile\.ReadValue",
        "missingFileValue: string\.Empty",
        "missingLineValue: string\.Empty",
        "readFailedValue: string\.Empty"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchCatalog.Read.Marker.cs" `
    "parses Steam app-info branch marker metadata into dropdown options" `
    @(
        "BranchOptionFromMarkerValue",
        "ParseMetadata",
        "metadataVisible",
        "windowsManifestDepots",
        "passwordRequired",
        "buildId",
        "description",
        "Steam app-info"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchCatalog.Read.Merge.cs" `
    "keeps branch option replacement and fallback merge behavior isolated" `
    @(
        "AddIfMissing",
        "AddOrReplace",
        "StringComparison\.OrdinalIgnoreCase",
        "FindIndex",
        "options\[existingIndex\] = option"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchCatalog.Dropdown.cs" `
    "builds branch dropdown options without treating gated branches as available" `
    @(
        "DropdownOptions",
        "DropdownOptionLabels",
        "new\(SteamGameBranch\.Public, source: ""fallback""\)",
        "AddOrReplace\(options, branch\)",
        "AddIfMissing\(options, new BranchOption\(selectedBranch, source: ""saved selection""\)\)",
        "SteamGameBranch\.Public"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchCatalog.Dropdown.Status.cs" `
    "keeps selected branch dropdown status and download blockers isolated" `
    @(
        "SelectedOptionStatus",
        "SelectedOptionCompactStatus",
        "SelectedOptionDownloadProblem",
        "Password branch blocked",
        "Ready in Steam catalog",
        "Refresh before download",
        "saved selection",
        "not listed in the latest Steam app-info catalog",
        "private, inaccessible, password-protected, or unavailable",
        "Download blocked: selected saved branch was not listed",
        "Download blocked: selected branch is password-protected",
        "Download blocked: selected branch has no Windows depot manifest",
        "SteamGameBranch\.Public"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchCatalog.Dropdown.Metadata.cs" `
    "keeps branch dropdown diagnostics metadata formatting isolated" `
    @(
        "DropdownOptionMetadata",
        "DropdownOptions\(selectedBranch, discoveredBranches\)",
        "metadataVisible=",
        "windowsManifestDepots=",
        "passwordRequired=",
        "buildId=",
        "ValueOrUnknown",
        "ValueOrNone"
    )

Add-ForbiddenCheck `
    "src\STS2Mobile\Launcher\LauncherBranchCatalog.Dropdown.cs" `
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
    "src\STS2Mobile\ModEntry.RuntimeFiles.cs" `
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
    "declares launcher preference keys and typed preference storage roots" `
    @(
        "LocalBackupPreferenceKey",
        "CloudSyncPreferenceKey",
        "game_branch",
        "GameBranchPreference",
        "BooleanPreference",
        "OperatingSystem\.IsAndroid"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPreferences.GameBranch.cs" `
    "persists selected Steam game branch through normalized storage" `
    @(
        "ReadGameBranch",
        "SaveGameBranch",
        "GameBranchPreferenceExists",
        "SteamGameBranch\.Normalize",
        "SteamGameBranch\.Public",
        "GameBranchPreference\.WriteText"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPreferences.ActionPreferences.cs" `
    "keeps aggregate launcher action preferences typed" `
    @(
        "internal readonly struct ActionPreferences",
        "LocalBackupEnabled",
        "CloudSyncEnabled",
        "GameBranch",
        "ReadActionPreferences",
        "LoadAndApplyActionPreferences"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPreferences.BooleanPreference.cs" `
    "isolates boolean preference read apply and save behavior" `
    @(
        "internal BooleanPreference",
        "Func<bool> defaultValue",
        "Action<bool> apply",
        "Action<bool>\? beforeSave = null",
        "LoadAndApply",
        "BeforeSave\?\.Invoke",
        "Storage\.WriteBoolean"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPreferences.LocalBackup.cs" `
    "keeps local-backup preference effects isolated from branch selection" `
    @(
        "SaveLocalBackupEnabled",
        "RequestStoragePermissionForLocalBackup",
        "AppPaths\.HasStoragePermission",
        "AppPaths\.RequestStoragePermission",
        "CloudSyncCoordinator\.SetLocalBackupEnabled",
        "AppPaths\.EnsureExternalDirectories"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPreferences.CloudSync.cs" `
    "keeps cloud-sync preference application isolated from branch selection" `
    @(
        "LoadAndApplyCloudSyncEnabled",
        "SaveCloudSyncEnabled",
        "ApplyCloudSync",
        "LauncherCloudSaveState\.SetCloudSyncEnabled"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.Construction.Version.Dropdown.cs" `
    "constructs selected-version selector controls before downloading" `
    @(
        "OptionButton",
        "BuildBranchDropdown",
        "ItemSelected"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.Branches.cs" `
    "keeps install branch selection and compact drawer mechanics isolated" `
    @(
        "SetGameBranch",
        "SetAvailableBranches",
        "UpdateBranchHelpText",
        "LauncherBranchDropdown\.Populate",
        "LauncherBranchDropdown\.NormalizeSelection",
        "selection\.Changed",
        "LauncherBranchDropdown\.TryGetBranch",
        "CollapseCompactBranchDetailsAfterSelection",
        "ApplyBranchControlVisibility",
        "_branchDetailsExpanded = true",
        "_branchDetailsExpanded = false",
        "GameBranchChanged\?\.Invoke"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.Branches.Text.cs" `
    "shows selector limitations before downloading a selected version" `
    @(
        "SelectedOptionStatus",
        "UpdateBranchHelpText",
        "SteamGameBranch\.SelectorInstallSlotHelpText"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\LauncherBranchDropdown.cs" `
    "centralizes shared Steam branch dropdown population and safe selection lookup" `
    @(
        "internal static class LauncherBranchDropdown",
        "SelectionUpdate",
        "NormalizeSelection",
        "SteamGameBranch\.Normalize",
        "NormalizeAvailableBranches",
        "LauncherBranchCatalog\.DropdownOptions",
        "dropdown\.Clear\(\)",
        "branchOptions\.Clear\(\)",
        "dropdown\.AddItem\(option\.Label\)",
        "dropdown\.Select\(selectedIndex\)",
        "TryGetBranch",
        "index < 0 \|\| index >= branchOptions\.Count"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Branches.cs" `
    "keeps ready/action branch selection and visibility mechanics isolated" `
    @(
        "SetGameBranch",
        "SetAvailableBranches",
        "UpdateBranchHelpText",
        "PopulateBranchDropdown",
        "LauncherBranchDropdown\.NormalizeSelection",
        "selection\.Changed",
        "LauncherBranchDropdown\.TryGetBranch",
        "LauncherBranchDropdown\.Populate",
        "CollapseCompactBranchDetailsAfterSelection",
        "_branchDetailsExpanded = false",
        "ApplyBranchControlVisibility",
        "GameBranchChanged\?\.Invoke"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Branches.Text.cs" `
    "updates selector help text in ready/action state" `
    @(
        "_branchHelpLabel",
        "SelectedOptionStatus",
        "UpdateBranchHelpText",
        "SteamGameBranch\.SelectorInstallSlotHelpText",
        "Version/download actions affect local game files only",
        "Steam Cloud saves move only through Pull/Push"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Branch.cs" `
    "creates dropdown branch selector and wrapped selector help text in ready/action state" `
    @(
        "OptionButton",
        "ItemSelected",
        "ApplyGameBranch",
        "branchHelpLabel",
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
    "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportLauncherPreferences.cs" `
    "surfaces partial Steam branch and inherited-public depot evidence" `
    @(
        "Selected game branch marker depots matching public",
        "Selected game branch marker depots differing from public",
        "Selected game branch marker depots without public comparison",
        "Selected game branch marker depots inherited from public",
        "Selected game branch marker depots missing selected branch manifest",
        "Selected game branch marker partial Steam branch evidence",
        "Selected game branch marker depot manifest rows"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchMarkers.cs" `
    "parses partial Steam branch and inherited-public depot marker evidence" `
    @(
        "BranchMarkerPartialSteamBranchEvidence",
        "ReadBranchMarkerValues",
        "LauncherMarkerFile\.ReadJoinedValues",
        "LauncherMarkerFile\.CountLines",
        "LauncherMarkerFile\.ReadInt",
        "selected branch inherits public depot manifests"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchAvailabilityStatus.cs" `
    "orchestrates compact Steam branch availability diagnoses in launcher failure status" `
    @(
        "BranchAvailabilityMarkerPath",
        "CompactFailureMessage",
        "Clear",
        "Failed to clear Steam branch availability marker",
        "ReadDiagnosis",
        "RemoveRawBranchAvailabilitySummary"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchAvailabilityStatus.Fields.cs" `
    "defines Steam branch availability marker prefixes and compact-status constants" `
    @(
        "SelectedBranchPrefix",
        "SelectedBranchVisibilityPrefix",
        "SelectedBranchWindowsDepotManifestsPrefix",
        "VisibleBranchPrefix",
        "VisibleBranchOverflowCountPrefix",
        "RawSelectedBranchVisibilitySummaryMarker",
        "MaxVisibleBranchesInStatus",
        "PasswordRequiredTrueMarker",
        "ZeroWindowsManifestsMarker",
        "Selected branch visibility:",
        "Windows depot manifests for selected branch:",
        "Visible branch:",
        "passwordRequired=true",
        "windowsManifestDepots=0"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchAvailabilityStatus.Read.cs" `
    "reads Steam branch availability markers for launcher failure diagnosis" `
    @(
        "ReadDiagnosis",
        "BranchAvailabilityMarkerPath",
        "File\.ReadAllLines",
        "SelectedBranchPrefix",
        "SelectedBranchVisibilityPrefix",
        "SelectedBranchWindowsDepotManifestsPrefix",
        "VisibleBranchPrefix",
        "VisibleBranchOverflowCountPrefix",
        "MarkerBranchMatchesCurrentSelection",
        "MarkerValueMatchesBranch",
        "VisibleBranchStatus",
        "SelectedStatus",
        "ReadValue",
        "ReadValues",
        "Branch availability:"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchAvailabilityStatus.Format.cs" `
    "formats compact Steam branch availability user-facing status" `
    @(
        "SelectedStatus",
        "VisibleBranchStatus",
        "MarkerValuePasswordProtected",
        "password-protected",
        "Steam beta password entry is supported",
        "no Windows manifest",
        "ZeroWindowsManifestsMarker",
        "RemoveRawBranchAvailabilitySummary",
        "RawSelectedBranchVisibilitySummaryMarker"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchAvailabilityStatus.Match.cs" `
    "matches Steam branch availability marker rows against the selected branch" `
    @(
        "MarkerBranchMatchesCurrentSelection",
        "LauncherPreferences\.ReadGameBranch",
        "MarkerValueMatchesBranch",
        "MarkerValuePasswordProtected",
        "SteamGameBranch\.Normalize",
        "PasswordRequiredTrueMarker"
    )

Add-Check `
    "src\STS2Mobile\Steam\DepotDownloader.BranchAvailability.Report.cs" `
    "builds account-visible Steam branch availability from app info" `
    @(
        "BranchAvailabilityReport",
        "Visible Steam branches",
        "Selected branch visibility",
        "Windows depot manifests for selected branch",
        "visible branches",
        "DepotIsWindowsCompatible"
    )

Add-Check `
    "src\STS2Mobile\Steam\DepotDownloader.BranchAvailability.Marker.cs" `
    "persists Steam branch availability marker evidence" `
    @(
        "WriteMarker",
        "BranchAvailabilityMarkerPath",
        "MaxBranchAvailabilityMarkerBranches",
        "Visible branch overflow count",
        "Selected branch visibility",
        "Windows depot manifests for selected branch"
    )

Add-Check `
    "src\STS2Mobile\Steam\DepotDownloader.BranchAvailability.Model.cs" `
    "formats branch availability marker values safely" `
    @(
        "MaxBranchAvailabilityMarkerValueLength",
        "SafeMarkerValue",
        "passwordRequired",
        "DownloadabilityText",
        "password-protected",
        "!PasswordRequired\.Equals\(""true""",
        "windowsManifestDepots",
        "metadataVisible"
    )

Add-Check `
    "src\STS2Mobile\Steam\DepotDownloader.BranchAvailability.Builder.cs" `
    "parses Steam branch metadata including beta password flags" `
    @(
        "BranchAvailabilityBuilder",
        "pwdrequired",
        "password_required"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherGameFiles.BranchMarker.cs" `
    "blocks ambiguous non-public caches without branch marker readiness" `
    @(
        "BranchMarkerReady",
        "HasBranchMetadataProblem",
        "BranchMarkerBranchPrefix",
        "BranchMarkerHasDepotManifestProvenance",
        "BranchMarkerHasInstallSlotProvenance",
        "BranchMarkerHasIntegrityProvenance"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherGameFiles.BranchMarker.Fields.cs" `
    "keeps branch marker field prefixes centralized for readiness and integrity checks" `
    @(
        "BranchMarkerBranchPrefix = ""Branch:""",
        "BranchMarkerDepotManifestCountPrefix = ""Depot manifest count:""",
        "BranchMarkerDepotManifestRowPrefix = ""Depot manifest:""",
        "BranchMarkerDepotsMatchingPublicPrefix = ""Depot manifests matching public count:""",
        "BranchMarkerDepotsDifferingFromPublicPrefix = ""Depot manifests differing from public count:""",
        "BranchMarkerDepotsWithoutPublicComparisonPrefix = ""Depot manifests without public comparison count:""",
        "BranchMarkerDepotsInheritedFromPublicPrefix = ""Depot manifests inherited from public count:""",
        "BranchMarkerDepotsMissingSelectedManifestPrefix = ""Depot manifests missing selected branch manifest count:""",
        "BranchMarkerInstallSlotKindPrefix = ""Install slot kind:""",
        "BranchMarkerInstallSlotDirectoryPrefix = ""Install slot directory:"""
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherGameFiles.BranchMarker.Provenance.cs" `
    "checks branch marker depot, integrity, and install-slot provenance through centralized fields" `
    @(
        "BranchMarkerHasDepotManifestProvenance",
        "BranchMarkerDepotManifestRowPrefix",
        "BranchMarkerHasIntegrityProvenance",
        "BranchMarkerDepotsMatchingPublicPrefix",
        "BranchMarkerDepotsDifferingFromPublicPrefix",
        "BranchMarkerDepotsWithoutPublicComparisonPrefix",
        "BranchMarkerDepotsInheritedFromPublicPrefix",
        "BranchMarkerDepotsMissingSelectedManifestPrefix",
        "BranchMarkerHasInstallSlotProvenance",
        "BranchMarkerInstallSlotKindPrefix",
        "BranchMarkerInstallSlotDirectoryPrefix",
        "MarkerPathsEquivalent"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherGameFiles.BranchMarker.Paths.cs" `
    "isolates Android private-path alias matching for branch marker install-slot provenance" `
    @(
        "NormalizeMarkerPath",
        "MarkerPathsEquivalent",
        "AndroidAppPrivatePathAlias",
        "AndroidDataUserPrefix",
        "AndroidDataDataPrefix",
        "sourceRootPrefix",
        "aliasRootPrefix"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherGameFiles.BranchIntegrity.cs" `
    "surfaces ambiguous non-public cache integrity evidence" `
    @(
        "BranchIntegritySummary",
        "BranchMarkerDepotManifestCountPrefix",
        "BranchMarkerDepotsMatchingPublicPrefix",
        "BranchMarkerDepotsDifferingFromPublicPrefix",
        "BranchMarkerDepotsInheritedFromPublicPrefix",
        "BranchMarkerDepotsMissingSelectedManifestPrefix",
        "BranchMarkerDepotsWithoutPublicComparisonPrefix",
        "Selected branch appears partial",
        "inherits public content",
        "Selected branch depot manifests all match public",
        "Depot manifest"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherGameFiles.Readiness.cs" `
    "reports readiness failure when selected branch metadata is unsafe" `
    @(
        "ReadinessProblem",
        "HasBranchMetadataProblem"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherGameFiles.Redownload.cs" `
    "orchestrates selected-version redownload cleanup without touching other branch caches" `
    @(
        "DeleteDownloadedState",
        "SteamGameBranch\.Normalize",
        "GameDirectoryPath\(dataDir, branch\)",
        "SteamGameInstallPaths\.DownloadStateDirectoryPath\(dataDir, branch\)",
        "GameRuntimeSlot\.RuntimePackDirectoryPath\(dataDir, branch\)",
        "WriteRedownloadMarker",
        "DeleteDirectory\(runtimePackDirectory\)",
        "LauncherRuntimeSlotEvidence\.Clear\(dataDir\)",
        "LauncherRuntimeCacheEvidence\.Clear\(dataDir\)",
        "LauncherRuntimePatchValidationEvidence\.Clear\(dataDir\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherGameFiles.Redownload.Marker.Fields.cs" `
    "centralizes selected-version redownload marker prefixes" `
    @(
        "RedownloadMarkerUtcPrefix = ""UTC:""",
        "RedownloadMarkerSelectedBranchPrefix = ""Selected branch:""",
        "RedownloadMarkerVersionSlotKindPrefix = ""Selected version slot kind:""",
        "RedownloadMarkerVersionSlotDirectoryPrefix = ""Selected version slot directory:""",
        "RedownloadMarkerGameDirectoryPrefix = ""Deleted game directory:""",
        "RedownloadMarkerGameDirectoryExistedPrefix = ""Game directory existed before delete:""",
        "RedownloadMarkerGameDirectoryExistsAfterDeletePrefix = ""Game directory exists after delete:""",
        "RedownloadMarkerDownloadStateDirectoryPrefix = ""Deleted download state directory:""",
        "RedownloadMarkerDownloadStateDirectoryExistedPrefix = ""Download state directory existed before delete:""",
        "RedownloadMarkerDownloadStateDirectoryExistsAfterDeletePrefix = ""Download state directory exists after delete:""",
        "RedownloadMarkerRuntimePackDirectoryPrefix = ""Deleted runtime pack directory:""",
        "RedownloadMarkerRuntimePackDirectoryExistedPrefix = ""Runtime pack directory existed before delete:""",
        "RedownloadMarkerRuntimePackDirectoryExistsAfterDeletePrefix = ""Runtime pack directory exists after delete:"""
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherGameFiles.Redownload.Marker.Read.cs" `
    "reads selected-version redownload cleanup marker evidence" `
    @(
        "RedownloadMarkerFileName",
        "RedownloadMarkerUtcParseable",
        "RedownloadMarkerVersionSlotKind",
        "RedownloadMarkerVersionSlotDirectory",
        "last_game_version_redownload\.txt",
        "RedownloadMarkerRuntimePackDirectory",
        "RedownloadMarkerSelectedDirectoriesCleared"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherGameFiles.Redownload.Marker.Write.cs" `
    "writes selected-version redownload cleanup marker evidence" `
    @(
        "WriteRedownloadMarker",
        "RedownloadMarkerSelectedBranchPrefix",
        "RedownloadMarkerVersionSlotKindPrefix",
        "RedownloadMarkerVersionSlotDirectoryPrefix",
        "RedownloadMarkerGameDirectoryPrefix",
        "RedownloadMarkerGameDirectoryExistsAfterDeletePrefix",
        "RedownloadMarkerDownloadStateDirectoryPrefix",
        "RedownloadMarkerDownloadStateDirectoryExistsAfterDeletePrefix",
        "RedownloadMarkerRuntimePackDirectoryPrefix",
        "RedownloadMarkerRuntimePackDirectoryExistsAfterDeletePrefix",
        "File\.WriteAllLines",
        "Failed to write game version redownload marker"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherGameFiles.CacheCleanup.cs" `
    "orchestrates inactive cache cleanup while preserving selected non-public cache state" `
    @(
        "DeleteInactiveVersionCaches",
        "NewCacheCleanupMarkerLines",
        "DeleteInactiveRuntimePacks",
        "WriteCacheCleanupMarker",
        "Removed count",
        "Removed runtime pack count",
        "Removing inactive game version cache",
        "Preserving selected game version cache",
        "Preserved selected cache",
        "selected branch"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherGameFiles.CacheCleanup.Markers.cs" `
    "records and reads cache cleanup marker evidence for selected cache/runtime pack preservation" `
    @(
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
        "Selected runtime pack directory",
        "Selected runtime pack present before cleanup",
        "Runtime packs directory present"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherGameFiles.CacheCleanup.RuntimePacks.cs" `
    "removes inactive runtime-pack caches while preserving the selected runtime pack" `
    @(
        "RuntimePackDirectoryPathForStateDirectory",
        "DeleteInactiveRuntimePacks",
        "runtime_packs",
        "Preserved selected runtime pack",
        "Removed orphan runtime pack",
        "existsAfterDelete"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherModel.Downloads.cs" `
    "keeps launcher download model state and events centralized" `
    @(
        "_downloadCts",
        "_downloader",
        "_downloadRunning",
        "DownloadProgressChanged",
        "DownloadLogReceived",
        "DownloadCompleted",
        "DownloadFailed",
        "DownloadCancelled",
        "UpdateCheckCompleted",
        "UpdateCheckFailed",
        "BranchCatalogRefreshCompleted",
        "BranchCatalogRefreshFailed",
        "DownloadIsRunning",
        "Interlocked\.CompareExchange"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherModel.Downloads.Action.cs" `
    "centralizes depot connection actions for download, update check, and branch refresh" `
    @(
        "NotConnectedMessage = ""Not connected""",
        "DepotConnectionAction",
        "Download\(LauncherModel model\)",
        "BeginDownload\(connection\)",
        "RunDownloadAsync\(\)",
        "UpdateCheck\(LauncherModel model\)",
        "CheckForUpdatesWithConnectionAsync",
        "BranchCatalogRefresh\(LauncherModel model\)",
        "RefreshBranchCatalogWithConnectionAsync",
        "FailNotConnected"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherModel.Downloads.RunGuard.cs" `
    "isolates download concurrency guard acquisition and release" `
    @(
        "DownloadRunGuard",
        "TryAcquire\(LauncherModel model\)",
        "Interlocked\.Exchange\(ref model\._downloadRunning, 1\) == 0",
        "Release\(\)",
        "Interlocked\.Exchange\(ref Model\._downloadRunning, 0\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherModel.Downloads.Start.cs" `
    "routes public download, update check, and branch refresh starts through depot connection actions" `
    @(
        "StartDownloadAsync",
        "DownloadRunGuard\.TryAcquire\(this\)",
        "Download already running",
        "DepotConnectionAction\.Download\(this\)",
        "run\.Release\(\)",
        "CheckForUpdatesAsync",
        "DepotConnectionAction\.UpdateCheck\(this\)",
        "RefreshBranchCatalogAsync",
        "DepotConnectionAction\.BranchCatalogRefresh\(this\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherModel.Downloads.Catalog.cs" `
    "keeps update checks and branch catalog refreshes as non-download depot workflows" `
    @(
        "CheckForUpdatesWithConnectionAsync",
        "using var downloader = CreateDownloader\(connection\)",
        "CheckForUpdatesAsync",
        "RaiseUpdateCheckCompleted",
        "RaiseUpdateCheckFailed",
        "RefreshBranchCatalogWithConnectionAsync",
        "RefreshBranchCatalogAsync",
        "RaiseBranchCatalogRefreshCompleted",
        "RaiseBranchCatalogRefreshFailed"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherModel.Downloads.Connection.cs" `
    "keeps depot connection validation before download-related operations" `
    @(
        "RunWithDepotConnectionAsync",
        "GetDepotConnectionAsync",
        "EnsureConnectedAsync",
        "_steamSession\.TryGetConnection",
        "action\.FailNotConnected",
        "action\.RunAsync\(connection\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherModel.SessionAuth.cs" `
    "keeps launcher session authentication entry points together" `
    @(
        "ConnectAsync",
        "ConnectSavedCredentialsAndVerifyAsync",
        "SessionState\.Connecting",
        "LoginAsync\(string username, string password\)",
        "LoginWithTimeoutAsync",
        "SessionState\.Authenticating",
        "RunLoginAttemptAsync",
        "LoginAndVerifyAsync",
        "RaiseLogReceived",
        "RaiseCodeNeeded",
        "SubmitCode\(string code\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherModel.SessionAuth.Result.cs" `
    "isolates session attempt result application and stale-failure logging" `
    @(
        "ConnectionAttemptResult",
        "Ignored stale session failure",
        "PatchHelper\.Log",
        "SessionState\.LoggedIn",
        "SessionState\.Failed",
        "_connectionResolved = Succeeded",
        "SetSessionState\(State, Failure\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherModel.SessionAuth.Attempt.cs" `
    "centralizes session attempt identity and ownership-verification transition handling" `
    @(
        "RunConnectionAttemptAsync",
        "BeginSessionAttempt\(state\)",
        "CreateConnectionAttemptResult",
        "IsCurrentSessionAttempt\(attemptId\)",
        "ApplyConnectionAttemptResult",
        "BeginOwnershipVerification",
        "SessionState\.VerifyingOwnership"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherModel.SessionAuth.Connection.cs" `
    "reuses an existing logged-in Steam connection before starting depot connection attempts" `
    @(
        "EnsureConnectedAsync",
        "IsLoggedIn",
        "_steamSession\.TryGetConnection",
        "RunConnectionAttemptAsync",
        "SessionState\.Connecting",
        "_steamSession\.EnsureConnectedAsync"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherSteamSession.Connection.cs" `
    "keeps saved Steam connection retry policy centralized" `
    @(
        "SavedConnectionVerifyAttempts = 3"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherSteamSession.Connection.SavedCredentials.cs" `
    "retries saved-credential login and verifies ownership before adopting a Steam connection" `
    @(
        "ConnectSavedCredentialsAndVerifyAsync",
        "SavedConnectionVerifyAttempts",
        "Retrying saved Steam connection",
        "UseConnectionAndVerifyOwnershipAsync",
        "CreateSavedCredentialConnection",
        "Saved Steam connection attempt",
        "Could not connect to Steam"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherSteamSession.Connection.Ensure.cs" `
    "validates existing or saved Steam connections before depot operations" `
    @(
        "EnsureConnectedAsync",
        "EnsureExistingConnectionAsync",
        "AdoptSavedConnectionAfterAccessCheckAsync",
        "_connection != null",
        "_credentialStore\.TryCreateConnection",
        "No saved credentials",
        "EnsureAppAccessTokenNotDeniedAsync",
        "DropConnection",
        "Retrying Steam access check",
        "AdoptConnectionAfterVerificationAsync"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherSteamSession.Connection.Adoption.cs" `
    "adopts verified Steam connections and disposes failed candidates" `
    @(
        "AdoptConnectionAfterVerificationAsync",
        "beforeAdoptAsync",
        "UseConnection\(connection\)",
        "adopted = true",
        "if \(!adopted\)",
        "connection\.Dispose\(\)",
        "DropConnection",
        "ReferenceEquals\(_connection, connection\)",
        "CreateSavedCredentialConnection",
        "throw new InvalidOperationException\(""No saved credentials""\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.Downloads.Actions.cs" `
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
    "src\STS2Mobile\Launcher\LauncherController.Downloads.Execution.cs" `
    "logs selected-branch integrity summary after non-public downloads" `
    @(
        "BranchIntegritySummary",
        "integritySummary",
        "AppendLog"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.Automation.cs" `
    "keeps launcher automation entry point thin around request consumption and execution" `
    @(
        "AutomationFileName = ""launcher_automation_action\.txt""",
        "AutomationMarkerFileName = ""last_launcher_automation\.txt""",
        "TryStartAutomation",
        "LauncherAutomationRequest\.TryConsume",
        "RunAutomationAsync"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.Automation.Request.cs" `
    "parses and consumes launcher automation requests from data-directory files" `
    @(
        "LauncherAutomationRequest",
        "Path\.Combine\(dataDir, AutomationFileName\)",
        "File\.ReadAllLines",
        "File\.Delete",
        "ReadValue\(lines, ""action""\)",
        "ReadValue\(lines, ""branch""\)",
        "SteamGameBranch\.Public",
        "RefreshCatalog",
        "CheckUpdates",
        "Redownload",
        "Download",
        "LaunchSafe"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.Automation.Run.cs" `
    "runs launcher automation through normal branch, catalog, download, and safe-launch paths" `
    @(
        "RunAutomationAsync",
        "WriteAutomationMarker\(request, ""started""\)",
        "LauncherPreferences\.SaveGameBranch",
        "LauncherBranchAvailabilityStatus\.Clear",
        "RefreshGameBranchOptions",
        "RefreshBranchCatalogAsync",
        "CheckForUpdatesAsync",
        "ResetGameFilesForRedownload",
        "StartDownloadAsync",
        "RefreshSelectedRuntimeSlotEvidence",
        "LaunchSafe",
        "WriteAutomationMarker\(request, ""completed""\)",
        "WriteAutomationMarker\(request, ""failed"""
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.Automation.Marker.cs" `
    "records launcher automation status without driving cloud push side effects" `
    @(
        "WriteAutomationMarker",
        "Path\.Combine\(_model\.DataDir, AutomationMarkerFileName\)",
        "UTC:",
        "Status:",
        "Action:",
        "Selected branch:",
        "Requested branch:",
        "Message:",
        "SteamGameBranch\.Normalize\(LauncherPreferences\.ReadGameBranch\(\)\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.LocalLogin.cs" `
    "keeps local Steam credential handoff state and polling constants centralized" `
    @(
        "LocalLoginPollDelayMs = 500",
        "LocalLoginPollTimeout = TimeSpan\.FromSeconds\(180\)",
        "_localLoginHandoffStarted"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.LocalLogin.Start.cs" `
    "starts Android local credential handoff once and supports immediate credential consumption" `
    @(
        "StartLocalLoginHandoff",
        "TryStartImmediateLocalLoginHandoff",
        "OperatingSystem\.IsAndroid",
        "Interlocked\.CompareExchange",
        "Volatile\.Read",
        "ConsumeLocalSteamCredentials",
        "Volatile\.Write\(ref _localLoginHandoffStarted, 0\)",
        "Starting immediate local Steam credential handoff",
        "SessionState\.Authenticating",
        "StartObservedLocalLoginTask"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.LocalLogin.Handoff.cs" `
    "wraps local credential handoff execution with failure display and flag reset" `
    @(
        "RunLocalLoginHandoffAsync",
        "WatchLocalLoginHandoffAsync",
        "RunLocalLoginAsync",
        "LoginFormFailure\.LocalCredentialHandoff\(\)\.Show",
        "Volatile\.Write\(ref _localLoginHandoffStarted, 0\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.LocalLogin.Watch.cs" `
    "polls for local credential files only while the connection is pending" `
    @(
        "WatchLocalLoginHandoffAsync",
        "DateTime\.UtcNow \+ LocalLoginPollTimeout",
        "_model\.IsConnectionPending\(\)",
        "ConsumeLocalSteamCredentials",
        "Task\.Delay\(LocalLoginPollDelayMs\)",
        "Local Steam credential handoff watcher timed out",
        "connection no longer pending"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.LocalLogin.Run.cs" `
    "performs local Steam credential login through the normal model timeout path" `
    @(
        "RunLocalLoginAsync",
        "Consumed local Steam credential file",
        "SessionState\.Authenticating",
        "localLogin\.LoginAsync\(_model, StartConnectionTimeout\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Dialog.Buttons.cs" `
    "accepts contextual confirmation button labels" `
    @(
        "confirmText",
        "cancelText",
        "DialogButtonText",
        "BuildDialogButtons",
        "TryGetPressedPointerPosition"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Dialog.Message.cs" `
    "keeps compact confirmation warning messages scroll-safe" `
    @(
        "CompactDialogWidthRatio = 0\.9f",
        "CompactDialogMaxMessageHeightRatio = 0\.44f",
        "CompactDialogMessageMinScrollHeight = 96",
        "BuildDialogMessageArea",
        "ShouldScrollDialogMessage",
        "DialogMessageScrollHeight",
        "new ScrollContainer",
        "DialogMessageWidth\(profile\)",
        "profile\.ViewportSize\.Y \* CompactDialogMaxMessageHeightRatio"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Dialog.ButtonFactory.cs" `
    "keeps compact confirmation button sizing isolated below scroll-safe warnings" `
    @(
        "BuildDialogButton",
        "ApplyDialogButtonLayout",
        "DialogMessageWidth\(profile\)",
        "button\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill",
        "LauncherComponentTheme\.DialogButtonWidth",
        "button\.Pressed \+= callback"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Dialog.Pointer.cs" `
    "keeps touch and mouse confirmation dialog pointer forwarding isolated" `
    @(
        "TryGetPressedPointerPosition",
        "InputEventScreenTouch",
        "InputEventMouseButton",
        "Pressed: true",
        "position = touch\.Position",
        "position = mouse\.Position"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Behavior.Confirmation.cs" `
    "exposes contextual confirmation button label overloads" `
    @(
        "confirmText",
        "cancelText",
        "BuildConfirmationDialog"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Behavior.Confirmation.cs" `
    "sizes confirmation dialogs from the current visible viewport" `
    @(
        "CurrentConfirmationProfile",
        "GetVisibleRect\(\)\.Size",
        "LauncherLayoutProfile\.ForViewport\(viewportSize\)",
        "BuildConfirmationDialog\(message,\s*CurrentConfirmationProfile\(\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.Downloads.Actions.cs" `
    "labels redownload and cache confirmations with explicit compact actions" `
    @(
        "Redownload Version",
        "Keep Files",
        "Delete Cache",
        "Clear Cache",
        "Keep Cache"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.Startup.BranchSwitch.cs" `
    "labels branch-switch confirmation with explicit compact actions" `
    @(
        "Switch Version",
        "Keep Current"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.CloudSync.Request.Factory.cs" `
    "labels final cloud confirmation with explicit compact actions" `
    @(
        "Push to Cloud",
        "Cancel Push",
        "Pull from Cloud",
        "Cancel Pull"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.UpdateChecks.cs" `
    "keeps update-check button labels and running state centralized" `
    @(
        "UpdateCheckFailedButtonText = ""Check Failed""",
        "UpdateCheckBlockedButtonText = ""Check Blocked""",
        "UpToDateButtonText = ""Up to Date""",
        "UpdateGameFilesButtonText = ""Update Selected Version""",
        "_updateCheckRunning"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.UpdateChecks.ViewUpdate.cs" `
    "formats update-check view changes without running update logic" `
    @(
        "UpdateCheckViewUpdate",
        "Completed\(bool hasUpdate\)",
        "UpdateGameFilesButtonText",
        "Update available for selected game version",
        "Selected game version is up to date",
        "Failed\(string message\)",
        "UpdateCheckFailedButtonText",
        "Blocked\(string message\)",
        "UpdateCheckBlockedButtonText",
        "Check Blocked",
        "Update check blocked for selected game version",
        "view\.AppendLog",
        "view\.HideActions",
        "view\.ShowDownloadAction",
        "view\.SetStatus",
        "view\.SetUpdateButtonText"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.UpdateChecks.Run.cs" `
    "runs selected-version update checks with busy-state and failure recovery" `
    @(
        "RunUpdateCheck",
        "RunUpdateCheckAsync",
        "_updateCheckRunning",
        "SetUpdateCheckBusy\(busy: true\)",
        "CheckForUpdatesAsync",
        "PatchHelper\.Log",
        "FailUpdateCheck\(ex\.Message\)",
        "SetUpdateCheckBusy\(busy: false\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.UpdateChecks.Workflow.cs" `
    "blocks selected-version update checks for known unavailable branches while preserving app update checks" `
    @(
        "CheckForAppUpdatesAsync",
        "SelectedOptionDownloadProblem",
        "Update check blocked:",
        "LauncherBranchCatalog\.ReadVisibleBranches",
        "_model\.CheckForUpdatesAsync",
        "await appUpdateTask"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.UpdateChecks.Results.cs" `
    "applies update-check completion and failure events after refreshing branch options" `
    @(
        "CompleteUpdateCheck",
        "FailUpdateCheck",
        "RefreshGameBranchOptions",
        "UpdateCheckViewUpdate\.Completed",
        "UpdateCheckViewUpdate\.Failed",
        "LauncherBranchAvailabilityStatus\.CompactFailureMessage"
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
    "keeps branch-switch safety marker identity isolated" `
    @(
        "internal static partial class LauncherBranchSwitchSafety",
        "last_game_branch_switch\.txt",
        "internal const string MarkerFileName",
        "MarkerPath",
        "HasMarker"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.Fields.cs" `
    "centralizes branch-switch safety marker field prefixes" `
    @(
        "UtcPrefix = ""UTC:""",
        "PreviousBranchPrefix = ""Previous branch:""",
        "SelectedBranchPrefix = ""Selected branch:""",
        "SelectedBranchSelectionKindPrefix = ""Selected branch selection kind:""",
        "SelectorModePrefix = ""Steam branch selector mode:""",
        "SelectedVersionPrefix = ""Selected version:""",
        "SelectedVersionSlotKindPrefix = ""Selected version slot kind:""",
        "SelectedVersionSlotDirectoryPrefix = ""Selected version slot directory:""",
        "SelectedBranchNotePrefix = ""Selected branch note:""",
        "LocalBackupForcedPrefix = ""Local backup forced on:""",
        "ManualPushRequiresBackupStoragePrefix = ""Manual Push requires backup storage:""",
        "WarningAcknowledgedPrefix = ""Warning acknowledged:""",
        "NonPublicBranchWarningAcknowledgedPrefix = ""Non-public branch warning acknowledged:"""
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.Read.cs" `
    "reads and parses branch-switch safety marker evidence" `
    @(
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
        "ReadMarkerValue",
        "LauncherMarkerFile\.ReadValue",
        "LauncherMarkerFile\.HasConcreteValue",
        "ReadMarkerBool",
        "TryReadMarkerUtc",
        "DateTimeStyles\.AdjustToUniversal"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.Gates.cs" `
    "enforces branch-switch safety gates before manual Push" `
    @(
        "HasRequiredEvidence",
        "SelectedBranchMatches",
        "ManualPushPrerequisitesSatisfied",
        "SteamGameBranch\.Normalize",
        "LauncherCloudSyncEvidence\.HasManualPullAfterBranchSwitch",
        "LauncherLocalSaveEvidence\.HasImportantSaveEvidence",
        "LauncherSaveOriginEvidence\.CurrentLocalSavesMatchSelectedRuntime",
        "AppPaths\.HasStoragePermission"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBranchSwitchSafety.Write.cs" `
    "writes branch-switch safety marker and marks save-origin pending" `
    @(
        "WriteMarker",
        "Local backup forced on:",
        "Manual Push requires backup storage:",
        "Warning acknowledged:",
        "Non-public branch warning acknowledged:",
        "Selected branch selection kind:",
        "Steam branch selector mode:",
        "Selected branch note",
        "Selected version:",
        "Selected version slot kind:",
        "Selected version slot directory:",
        "SelectorHelpText",
        "WriteBranchSwitchPendingOrigin",
        "beta password entry is not implemented",
        "Failed to write branch switch safety marker",
        "Local backup forced on",
        "Manual Push requires backup storage"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.CloudSync.PushSafety.cs" `
    "keeps manual Push entry points routed through shared safety gates" `
    @(
        "CloudPushPressed",
        "CanArmCloudPush",
        "CloudPushSafetyContext\.Create",
        "CanPushWithBaselineEvidence\(pushContext\)",
        "CanPushAfterBranchSwitch\(pushContext\)",
        "ManualCloudSyncRequest\.Push"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.CloudSync.PushSafety.Context.cs" `
    "captures selected branch context for Push safety markers" `
    @(
        "CloudPushSafetyContext",
        "LauncherPreferences\.ReadGameBranch\(\)",
        "SteamGameBranch\.DisplayName",
        "SelectedBranch",
        "SelectedVersion",
        "WriteBlockedMarker",
        "WriteManualPushBlockedMarker"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.CloudSync.PushSafety.Baseline.cs" `
    "guards baseline manual Push until Pull, local save, and save-origin evidence match" `
    @(
        "CanPushWithBaselineEvidence",
        "Pull from Cloud must complete for the selected game version before Push",
        "selected game version \{pushContext\.SelectedVersion\}",
        "no Android local save evidence exists before Push",
        "LastManualPullCompletionRecorded",
        "LastManualPullMatchesSelectedBranch",
        "WriteBlockedMarker",
        "LauncherLocalSaveEvidence\.HasImportantSaveEvidence",
        "LauncherSaveOriginEvidence\.CurrentLocalSavesMatchSelectedRuntime",
        "Manual Push blocked: Android local save origin evidence does not match the selected runtime"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.CloudSync.PushSafety.BranchSwitch.cs" `
    "guards manual Push after branch switches until backup storage is available" `
    @(
        "CanPushAfterBranchSwitch",
        "BranchSwitchSafety",
        "LauncherBranchSwitchSafety\.HasRequiredEvidence",
        "branch switch marker is missing required safety evidence",
        "does not match the selected game version",
        "no current Pull-after-switch evidence exists",
        "no Android local save evidence exists",
        "backup storage permission is unavailable",
        "LauncherCloudSyncEvidence\.HasManualPullAfterBranchSwitch",
        "WriteBlockedMarker",
        "Pull from Cloud must complete after this game-version switch before Push",
        "HasManualPullAfterBranchSwitch",
        "LauncherLocalSaveEvidence\.HasImportantSaveEvidence",
        "no Android local save files were found",
        "LauncherSaveOriginEvidence\.CurrentLocalSavesMatchSelectedRuntime",
        "Manual Push blocked: save-origin evidence is missing or belongs to a different selected runtime after branch switch",
        "backup storage",
        "Push"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.CloudSync.Request.cs" `
    "keeps manual cloud sync request state and confirmation properties typed" `
    @(
        "CloudSyncTimeoutMs = 180_000",
        "private readonly partial struct ManualCloudSyncRequest",
        "ConfirmationMessage",
        "ConfirmText",
        "CancelText",
        "BypassConfirmation",
        "Func<Task<string>> run",
        "Action<Exception>\? onFailed = null"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.CloudSync.Request.Factory.cs" `
    "routes manual Pull and Push requests through explicit markers and request callbacks" `
    @(
        "ManualCloudSyncRequest Push",
        "ManualCloudSyncRequest Pull",
        "PushConfirmationMessage\(dataDir, selectedBranch\)",
        "LauncherCloudSaveState\.ManualPushAllAsync",
        "LauncherCloudSaveState\.ManualPullAllAsync",
        "WriteManualPushMarker",
        "WriteManualPushBlockedMarker",
        "WriteManualPullMarker",
        "Pull Steam Cloud saves to Android local storage"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.CloudSync.Request.PushConfirmation.cs" `
    "warns final Push confirmation about selected version, branch switches, and save overwrite risk" `
    @(
        "Selected version slot:",
        "Pull-after-switch for",
        "Android local save evidence",
        "local pre-Push backup",
        "cloud pre-Push backup",
        "A game version switch was recorded",
        "cross-version/destructive",
        "LauncherBranchSwitchSafety\.HasMarker",
        "SteamGameInstallPaths\.VersionSlotKind",
        "Pull from Cloud first and verify the Android saves exist before pushing"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.CloudSync.Request.Lifecycle.cs" `
    "keeps manual cloud sync lifecycle UI updates and timeout execution isolated" `
    @(
        "ShowStarted",
        "ShowComplete",
        "ShowFailed",
        "ShowFinished",
        "RunWithTimeoutAsync",
        "SetPushPullDisabled\(true\)",
        "SetPushPullDisabled\(false\)",
        "Open Help & Reports for details",
        "PatchHelper\.Log",
        "LauncherTimeout\.RunOrThrowAsync",
        "CloudSyncTimeoutMs"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.CloudSync.Execution.cs" `
    "executes Pull/Push cloud sync requests without bypassing Push confirmation" `
    @(
        "ManualCloudSyncRequest\.Pull\(",
        "RequestCloudSync",
        "ExecuteCloudSyncAsync",
        "request\.BypassConfirmation",
        "ShowConfirmation",
        "RunOnMainThread"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.cs" `
    "declares manual cloud evidence marker filenames and paths" `
    @(
        "last_manual_cloud_pull\.txt",
        "LastManualPushMarkerFileName",
        "last_manual_cloud_push_blocked\.txt"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Pull.cs" `
    "records successful manual Pull evidence for branch-switch Push safety" `
    @(
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
        "WriteManualPullMarker",
        "Manual Pull completed before branch-switch Push",
        "Selected version:",
        "Selected branch note"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Push.cs" `
    "reads successful manual Push timestamps from the Push marker" `
    @(
        "LastManualPushUtc",
        "LastManualPushUtcParseable",
        "LastManualPushMarkerPath",
        "CultureInfo\.InvariantCulture"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Push.Latest.cs" `
    "selects latest manual Push evidence between completed and blocked outcomes" `
    @(
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
        "Manual Push completed",
        "LatestManualPushEvidenceBlocked"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Push.Read.cs" `
    "reads completed manual Push marker metadata and backup evidence" `
    @(
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
        "LastManualPushPrePushBackupEvidenceSatisfied",
        "Pre-Push local backup evidence count:",
        "Pre-Push cloud backup evidence count:",
        "Latest pre-Push local backup UTC:",
        "Latest pre-Push cloud backup UTC:",
        "Important Android local save evidence count:",
        "Baseline manual Push prerequisites satisfied:",
        "Manual Push completed after branch-switch safety gates"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Push.Safety.cs" `
    "requires completed selected-branch Push evidence after branch switches" `
    @(
        "LastManualPushIsAfterBranchSwitch",
        "LastManualPushMatchesSelectedBranch",
        "HasManualPushAfterBranchSwitch",
        'LatestManualPushEvidenceOutcome\(dataDir\), "completed"',
        "LastManualPushCompletionRecorded",
        "LastManualPushPrePushBackupEvidenceSatisfied",
        "SteamGameBranch\.Normalize"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Push.Write.cs" `
    "records successful manual Push evidence after save-origin and backup gates" `
    @(
        "WriteManualPushMarker",
        "LauncherSaveOriginEvidence\.WriteManualPushOrigin",
        "Pre-Push local backup evidence count:",
        "Pre-Push cloud backup evidence count:",
        "Latest pre-Push local backup UTC:",
        "Latest pre-Push cloud backup UTC:",
        "Important Android local save evidence count:",
        "Baseline manual Push prerequisites satisfied:",
        "Branch-switch pre-Push backup evidence satisfied:",
        "Manual Push completed after branch-switch safety gates",
        "Selected version:",
        "Selected branch note"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.BlockedPush.cs" `
    "records blocked-before-upload manual Push evidence" `
    @(
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
        "Selected version:",
        "Selected branch note"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Markers.cs" `
    "keeps manual cloud evidence marker parsing isolated" `
    @(
        "HasCompletionFlag",
        'string\? ReadSelectedBranch',
        "ReadMarkerValue",
        "LauncherMarkerFile\.ReadOptionalValue",
        "ReadUtc",
        "LauncherMarkerFile\.ReadUtc",
        "LauncherMarkerFile\.ReadBoolFlag",
        "SanitizeSingleLine"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherLocalSaveEvidence.cs" `
    "exposes bounded local save evidence counts for branch-switch Push safety" `
    @(
        "internal static partial class LauncherLocalSaveEvidence",
        "MaxFilesToInspect = 1000",
        "MaxDirectoriesToInspect = 250",
        "IgnoredDirectoryNames",
        "HasImportantSaveEvidence",
        "CountImportantSaveEvidence"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherLocalSaveEvidence.Classify.cs" `
    "classifies non-empty Android save artifacts as important local save evidence" `
    @(
        "IsImportantSaveEvidence",
        "Path\.GetRelativePath",
        "ToLowerInvariant",
        "FileHasContent",
        "\.save",
        "\.save\.backup",
        "\.run",
        "\.bak",
        "prefs",
        "prefs\.save",
        "prefs\.backup",
        "prefs\.save\.backup",
        "new FileInfo\(file\)\.Length > 0"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherLocalSaveEvidence.Enumeration.cs" `
    "walks local save directories with runtime-directory exclusions and scan limits" `
    @(
        "EnumerateFilesSafely",
        "new Stack<string>",
        "MaxFilesToInspect",
        "MaxDirectoriesToInspect",
        "IsIgnoredRuntimeDirectory",
        "IgnoredDirectoryNames",
        "StringComparison\.OrdinalIgnoreCase",
        "SafeFiles",
        "SafeDirectories"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherLocalSaveEvidence.FileSystem.cs" `
    "swallows filesystem enumeration failures when scanning local save evidence" `
    @(
        "SafeFiles",
        "Directory\.GetFiles",
        "SafeDirectories",
        "Directory\.GetDirectories",
        "Array\.Empty<string>\(\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBackupEvidence.cs" `
    "exposes local/cloud pre-Push backup evidence after branch switching" `
    @(
        "internal static partial class LauncherBackupEvidence",
        "local-pre-push",
        "cloud-pre-push",
        "MaxBackupFilesToInspect",
        "LocalPrePushBackupCount",
        "CloudPrePushBackupCount",
        "LatestLocalPrePushBackupUtc",
        "LatestCloudPrePushBackupUtc",
        "HasLocalPrePushBackupAfterBranchSwitch",
        "HasCloudPrePushBackupAfterBranchSwitch",
        "HasPrePushBackupEvidenceAfterBranchSwitch"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBackupEvidence.Counts.cs" `
    "counts pre-Push backup files through bounded backup enumeration" `
    @(
        "CountBackups",
        "EnumerateBackups\(source\)",
        "count\+\+"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBackupEvidence.BranchSwitch.cs" `
    "compares pre-Push backup timestamps against branch-switch evidence" `
    @(
        "HasBackupAfterBranchSwitch",
        "TryReadBranchSwitchUtc",
        "LauncherBranchSwitchSafety\.MarkerUtc",
        "DateTimeStyles\.AdjustToUniversal",
        "TryReadBackupUtc\(backupPath, source, out var backupUtc\)",
        "backupUtc >= branchSwitchUtc"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBackupEvidence.Enumeration.cs" `
    "enumerates pre-Push backup files without walking unbounded backup trees" `
    @(
        "EnumerateBackups",
        "Directory\.Exists\(BackupDirectory\)",
        "Array\.Empty<string>\(\)",
        "Directory\.EnumerateFiles",
        "\*\.\{source\}\.bak",
        "SearchOption\.AllDirectories",
        "inspected >= MaxBackupFilesToInspect"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherBackupEvidence.Timestamp.cs" `
    "parses backup UTC evidence from backup filenames before falling back to file mtime" `
    @(
        "LatestBackupUtc",
        "TryReadBackupUtc",
        "ToString\(""O"", CultureInfo\.InvariantCulture\)",
        "<none>",
        "EndsWith\(suffix, StringComparison\.OrdinalIgnoreCase\)",
        "DateTimeOffset\.FromUnixTimeSeconds",
        "File\.GetLastWriteTimeUtc"
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
    "src\STS2Mobile\Launcher\LauncherController.Startup.BranchSwitch.cs" `
    "uses centralized selector guidance in branch-switch confirmation" `
    @(
        "BranchSwitchConfirmationMessage",
        "SteamGameBranch\.SelectorInstallSlotHelpText",
        "LauncherBranchCatalog\.ReadVisibleBranches",
        "SelectedOptionStatus",
        "SelectedOptionDownloadProblem",
        "AppendLog\(STS2Mobile\.Steam\.SteamGameBranch\.SelectorInstallSlotHelpText",
        "Steam Cloud Push will require backup storage permission"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.Startup.RuntimeEvidence.cs" `
    "uses selected-version runtime evidence in ready and download-required status copy" `
    @(
        "SelectedVersionReadyStatus",
        "SelectedVersionDownloadRequiredStatus",
        "SteamGameInstallPaths\.VersionSlotKind",
        "Active install slot",
        "RefreshSelectedRuntimeSlotEvidence",
        "LauncherRuntimeSlotEvidence\.Write"
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
    "src\STS2Mobile\Launcher\Sections\LoginSection.Submission.cs" `
    "clears captured Steam password from Godot login UI before authentication handoff" `
    @(
        "var password = _passwordField\.Text",
        "_passwordField\.Text = """"",
        "LoginRequested\?\.Invoke\(username, password\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\LoginSection.cs" `
    "wires Godot fallback credential fields and the Android native panel entry point" `
    @(
        "ConfigureUsernameField",
        "ConfigurePasswordField",
        "VirtualKeyboardType\.EmailAddress",
        "VirtualKeyboardType\.Password",
        "Sign in with Steam",
        "credentialHelpLabel",
        "MoveChild\(_nativeLoginButton, credentialHelpLabel\.GetIndex\(\)\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\LoginSection.NativePanel.cs" `
    "uses integrated native Steam login panel instead of a separate credential popup on Android" `
    @(
        "ShowSteamLoginCredentialPanel",
        "TryConsumeSteamLoginCredentialResult",
        "IsSteamLoginCredentialPanelVisible",
        "StopNativeCredentialPolling\(hidePanel: false\)",
        "HideSteamLoginCredentialPanel",
        "OpenNativeCredentialPanel",
        "PollNativeCredentialResult",
        "LoginRequested"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\LoginSection.Help.cs" `
    "keeps native Steam login help explicit about integrated panel and password storage boundaries" `
    @(
        "integrated Steam login panel",
        "does not store your Steam password",
        "Password manager can appear\.",
        "Steam password is not stored\.",
        "LauncherSectionMetrics\.CompactCredentialHelpHeight"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\LoginSection.State.cs" `
    "clears and hides native credential panel when login state is disabled or password is cleared" `
    @(
        "ClearPassword",
        "_passwordField\.Text = """"",
        "StopNativeCredentialPolling\(hidePanel: true\)",
        "SetFormVisible",
        "OpenNativeCredentialPanel"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\CompactButtonDetailLabelSpec.cs" `
    "keeps compact two-line button label configuration typed" `
    @(
        "internal readonly struct CompactButtonDetailLabelSpec",
        "BodyName",
        "TitleName",
        "DetailName",
        "TitleFontSize",
        "DetailFontSize",
        "HorizontalMargin",
        "VerticalMargin",
        "Default\(",
        "LauncherSectionMetrics\.CompactDetailButtonFontSize",
        "LauncherSectionMetrics\.CompactDetailLabelFontSize",
        "horizontalMargin: 6",
        "verticalMargin: 4"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\CompactButtonDetailLabels.cs" `
    "centralizes reusable compact two-line button label application" `
    @(
        "CompactButtonDetailLabels",
        "Apply",
        "TrySplitText",
        "Hide\(Button button, CompactButtonDetailLabelSpec spec\)",
        "button\.Text = text",
        "labels\.Title\.Text = title",
        "labels\.Detail\.Text = detail"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\CompactButtonDetailLabels.Text.cs" `
    "keeps compact two-line button label parsing isolated from Godot controls" `
    @(
        "TrySplitText",
        "IndexOf\('\\n'\)",
        "Trim\(\)",
        "title\.Length > 0 && detail\.Length > 0"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\CompactButtonDetailLabels.Controls.cs" `
    "keeps compact two-line button label Godot control construction isolated" `
    @(
        "Ensure\(",
        "BuildBody",
        "BuildLabel",
        "LauncherComponentTheme\.TextPrimary",
        "LauncherComponentTheme\.TextSecondary",
        "Control\.LayoutPreset\.FullRect",
        "LauncherViewLayoutMetrics\.ScaleInt",
        "TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\LoginSection.CompactNativeButton.cs" `
    "wires compact Android Steam sign-in to shared two-line CTA labels" `
    @(
        "CompactNativeLoginButtonHeight = LauncherSectionMetrics\.CodeInputHeight",
        "CompactNativeLoginLabels",
        "CompactButtonDetailLabelSpec",
        "CompactNativeLoginText",
        "SetCompactNativeLoginButtonText",
        "Sign in with Steam",
        "Android login",
        "CompactButtonDetailLabels\.Apply"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\LoginSection.Help.cs" `
    "uses a readable two-line compact Android credential helper" `
    @(
        "LauncherSectionMetrics\.CompactCredentialHelpHeight",
        "TextServer\.AutowrapMode\.WordSmart",
        "ClipText = false",
        "VerticalAlignment\.Center",
        "ScaleInt\(LauncherSectionMetrics\.CompactCredentialHelpHeight, scale\)",
        "Password manager can appear\.",
        "Steam password is not stored\."
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\LauncherSectionSetup.cs" `
    "frames hidden launcher states through a small section setup entrypoint" `
    @(
        "ConfigureHiddenSection",
        "internal static partial class LauncherSectionSetup",
        "bool compact",
        "compactCue",
        "accent",
        "LauncherSectionMetrics\.CompactSectionSeparation",
        "LauncherSectionMetrics\.SectionSeparation",
        "section\.Visible = false",
        "BuildSectionHeader\(title, subtitle, scale, accent, compact, compactCue\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\LauncherSectionSetup.Header.cs" `
    "builds desktop launcher section headers separately from compact mobile headers" `
    @(
        "BuildSectionHeader",
        "if \(compact\)",
        "BuildCompactSectionHeader\(title, CompactCueText\(compactCue, subtitle\), subtitle, scale, accent\)",
        "new PanelContainer",
        "new VBoxContainer",
        "BuildHeaderStyle\(scale, compact\)",
        "BuildDesktopSectionAccent",
        "BuildDesktopSectionTitle",
        "AddDesktopSectionSubtitle",
        "fontSize: 13",
        "AutowrapMode = TextServer\.AutowrapMode\.WordSmart",
        "LauncherComponentTheme\.TextSecondary"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\LauncherSectionSetup.Header.Compact.cs" `
    "keeps compact launcher section headers readable and fixed-size on mobile" `
    @(
        "BuildCompactSectionHeader",
        "CompactSectionHeaderMinHeight = 42",
        "CompactSectionHeaderCueFontSize = 12",
        "CompactSectionHeaderTitleFontSize = 14",
        "CompactSectionHeaderTitleMinWidth = 106",
        "CompactSectionHeaderAccentWidth = 3",
        "new HBoxContainer",
        "BuildCompactSectionAccent",
        "BuildCompactSectionTitle",
        "BuildCompactSectionCue",
        "CompactCueText",
        "TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
        "TooltipText = tooltip",
        "SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\LauncherSectionSetup.Header.Style.cs" `
    "isolates section header style metrics for compact and desktop layouts" `
    @(
        "BuildHeaderStyle\(float scale, bool compact\)",
        "LauncherStyleBoxes\.MakeFilled",
        "compact \? 6 : 8",
        "SetBorderWidthAll",
        "compact \? 7 : 10",
        "compact \? 4 : 8",
        "compact \? 4 : 9"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\LoginSection.cs" `
    "uses explicit compact Steam sign-in section cue text" `
    @(
        "Steam Sign-in",
        "Steam account"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\CodeSection.cs" `
    "uses explicit compact Steam Guard section cue text" `
    @(
        "Steam Guard",
        "Current code"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.cs" `
    "uses explicit compact game install section cue text" `
    @(
        "Game Install",
        "Local files"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.cs" `
    "uses explicit compact play and sync section cue text" `
    @(
        "Play and Sync",
        "Play safely"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.StatusCapsule.cs" `
    "wraps launcher status in a readable portal status capsule" `
    @(
        "BuildStatusCapsule",
        "BuildCompactStatusCapsule",
        "new PanelContainer",
        "BuildStatusStyle\(scale, compact: false\)",
        "BuildStatusPhaseStyle\(scale, compact: false\)",
        "statusAccent\.CustomMinimumSize",
        "statusLabel\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.StatusCapsule.Styles.cs" `
    "keeps status capsule visual styles in focused helpers" `
    @(
        "BuildStatusStyle",
        "BuildStatusPhaseStyle",
        "BuildCompactStatusDetailButtonStyle",
        "SetBorderWidthAll"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.FirstRunGuide.cs" `
    "builds the quick-start safe-flow guide panel" `
    @(
        "BuildFirstRunGuide",
        "BuildFirstRunGuidePanel",
        "CompactSafeFlowGuideTitleHeight",
        "CompactSafeFlowGuideTitleFontSize",
        "BuildFirstRunGuideStyle\(scale, compact\)",
        "`"Quick start guide`"",
        "new PanelContainer",
        "AddCompactSafeFlowSteps\(body, scale\)",
        "choose a game version, get Steam saves, then start the game",
        "Upload stays locked until you deliberately open it after checking local saves"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.FirstRunGuide.Steps.cs" `
    "declares typed compact quick-start safe-flow step rows" `
    @(
        "AddCompactSafeFlowSteps",
        "private readonly record struct CompactSafeFlowStepSpec",
        "CompactSafeFlowSteps",
        "foreach \(var step in CompactSafeFlowSteps\)",
        "BuildCompactSafeFlowStep\(scale, step\)",
        "CompactSafeFlowGuideStepHeight",
        "CompactSafeFlowGuideStepAccentWidth",
        "CompactSafeFlowGuideStepNumberWidth",
        "CompactSafeFlowGuideStepRadius",
        "CompactSafeFlowGuideStepHorizontalMargin",
        "CompactSafeFlowGuideStepVerticalMargin",
        "`"Sign in`"",
        "`"Steam account`"",
        "`"Get files`"",
        "`"Version on Android`"",
        "`"Get saves`"",
        "`"Steam to Android`"",
        "`"Play`"",
        "`"Ready version`"",
        "`"Upload locked`"",
        "Review before uploading"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.FirstRunGuide.StepCard.cs" `
    "builds bounded compact quick-start safe-flow step card layout" `
    @(
        "BuildCompactSafeFlowStep",
        "BuildCompactSafeFlowStepText",
        "CompactSafeFlowStepSpec step",
        "step\.Title",
        "step\.Detail",
        "step\.Accent",
        "CustomMinimumSize = new Vector2"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.FirstRunGuide.StepCard.Decor.cs" `
    "builds compact quick-start safe-flow step accent and marker decoration" `
    @(
        "BuildCompactSafeFlowStepAccent",
        "BuildCompactSafeFlowStepMarker",
        "Color accent",
        "step\.Marker",
        "step\.Accent",
        "CompactSafeFlowGuideStepAccentWidth",
        "CompactSafeFlowGuideStepNumberWidth"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.FirstRunGuide.StepCard.Labels.cs" `
    "builds bounded compact quick-start safe-flow step title and detail labels" `
    @(
        "BuildCompactSafeFlowStepTitle",
        "BuildCompactSafeFlowStepDetail",
        "TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
        "LauncherComponentTheme\.TextPrimary",
        "LauncherComponentTheme\.TextSecondary"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.FirstRunGuide.StepStyle.cs" `
    "keeps bounded compact quick-start safe-flow step styling isolated" `
    @(
        "BuildCompactSafeFlowStepStyle",
        "Color accent",
        "CompactSafeFlowGuideStepRadius",
        "CompactSafeFlowGuideStepHorizontalMargin",
        "CompactSafeFlowGuideStepVerticalMargin",
        "LauncherStyleBoxes\.MakeFilled",
        "SetBorderWidthAll"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.FirstRunGuide.Toggle.cs" `
    "builds the collapsible compact quick-start guide toggle shell" `
    @(
        "BuildCollapsedFirstRunGuide",
        "BuildFirstRunGuidePanel\(scale, compact: true\)",
        "toggle\.Pressed \+= \(\) =>",
        "guide\.Visible = !guide\.Visible",
        "`"Quick Start`"",
        "`"Get saves first`"",
        "`"Hide Guide`"",
        "`"Safe order`"",
        "LauncherSectionMetrics\.CompactDrawerToggleHeight",
        "LauncherSectionMetrics\.CompactDetailButtonFontSize",
        "CompactButtonDetailLabelSpec",
        "CompactSafeFlowToggleLabels",
        "CompactSafeFlowToggleBodyName",
        "CompactSafeFlowToggleTitleName",
        "CompactSafeFlowToggleDetailName",
        "CompactButtonDetailLabelSpec\.Default"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.FirstRunGuide.Toggle.Text.cs" `
    "sets compact quick-start guide toggle text through structured labels" `
    @(
        "SetCompactSafeFlowToggleText",
        "CompactButtonDetailLabels\.Apply",
        '\$"\{title\}\\n\{detail\}"',
        "enabled: true",
        "CompactSafeFlowToggleLabels"
    )
Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.LogColumn.cs" `
    "keeps diagnostics hidden behind a clearly labeled Help & Reports drawer" `
    @(
        "Hidden by default",
        "Create a help report",
        "Problem details and help reports",
        "Review before sharing",
        "drawer\.Visible = false",
        "SetDiagnosticsToggleText",
        "LauncherSectionMetrics\.CompactDrawerToggleHeight",
        "BuildLogView\(profile\)",
        "return \(log, drawer, toggle\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.LogColumn.Toggle.cs" `
    "renders compact diagnostics drawer toggle labels as structured title/detail controls" `
    @(
        "Show Help & Reports",
        "Hide Help & Reports",
        "DiagnosticsToggleText",
        "SetDiagnosticsToggleText",
        "CompactButtonDetailLabels\.Apply",
        "CompactButtonDetailLabelSpec",
        "CompactDiagnosticsToggleLabels",
        "CompactDiagnosticsToggleBodyName",
        "CompactDiagnosticsToggleTitleName",
        "CompactDiagnosticsToggleDetailName",
        "CompactButtonDetailLabelSpec\.Default",
        "enabled: false",
        "enabled: true",
        "`"Help & Reports\\nPrivate until opened`"",
        "`"Hide Help\\nBack to launcher`""
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.Result.cs" `
    "uses a typed primary-column layout result instead of a large positional tuple" `
    @(
        "LauncherViewPrimaryColumn",
        "CompactStatusDetailsButton",
        "WorkflowStepNumberLabels",
        "CompactStickyTaskHeader",
        "PrimaryScroll",
        "CompactDiagnosticsHost"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.StatusResult.cs" `
    "keeps primary status result fields isolated from full column layout result fields" `
    @(
        "LauncherViewPrimaryStatus",
        "StyledLabel Phase",
        "StyledLabel Action",
        "StyledLabel Message",
        "ColorRect Accent",
        "Control Capsule",
        "CompactDetailButton",
        "CompactHeadline",
        "CompactPhasePanel"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.BodyResult.cs" `
    "keeps primary body scroll result isolated from full column layout result fields" `
    @(
        "LauncherViewPrimaryBody",
        "ScrollContainer PrimaryScroll",
        "VBoxContainer Body"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.cs" `
    "hosts compact diagnostics inside the primary scroll body instead of fixed root chrome" `
    @(
        "VBoxContainer compactDiagnosticsHost",
        "compactDiagnosticsHost = new VBoxContainer",
        "compactDiagnosticsHost\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill",
        "left\.AddChild\(compactDiagnosticsHost\)",
        "compactDiagnosticsHost"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.LogColumn.Sizing.cs" `
    "bounds the compact diagnostics log viewport from the current launcher profile" `
    @(
        "CompactDiagnosticsLogViewportHeightRatio = 0\.28f",
        "CompactDiagnosticsLogMinHeight = 220",
        "CompactDiagnosticsLogMaxHeight = 340",
        "DiagnosticsLogHeight\(LauncherLayoutProfile profile\)",
        "profile\.ViewportSize\.Y \* CompactDiagnosticsLogViewportHeightRatio",
        "Math\.Clamp\(viewportHeight, minHeight, maxHeight\)"
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
    "src\STS2Mobile\Launcher\LauncherView.Layout.LogColumn.Sizing.cs" `
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
    "hosts compact diagnostics drawer under the primary compact body" `
    @(
        "DiagnosticsDrawer",
        "DiagnosticsToggle",
        "var diagnosticsRoot = profile\.Compact",
        "primary\.CompactDiagnosticsHost"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Diagnostics.cs" `
    "can reveal the hidden Help & Reports drawer after explicit diagnostics actions" `
    @(
        "ShowDiagnosticsConsole",
        "DiagnosticsDrawer\.Visible = true",
        "SetDiagnosticsToggleText\(DiagnosticsToggle, _profile, visible: true\)",
        "ScrollCompactPrimaryTo\(DiagnosticsDrawer\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.Diagnostics.cs" `
    "opens Help & Reports when problem summary or launcher-log actions write output" `
    @(
        "ShowDiagnosticsSummary",
        "CopyRawLogToClipboard",
        "Last problem opened",
        "view\.ShowDiagnosticsConsole\(\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.Diagnostics.Export.cs" `
    "opens Help & Reports after manual help report export writes output" `
    @(
        "ShowDiagnosticsExportResult",
        "Help report ready",
        "Help report saved",
        "_view\.ShowDiagnosticsConsole\(\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.cs" `
    "keeps launcher shell rooted as a polished portal layout" `
    @(
        "BuildShell",
        "StyledPanel",
        "ScreenBackground",
        "BuildBrandHeader\(profile\)",
        "CompactRootColumnSeparation",
        "RootColumnSeparation",
        "panel\.AddContent\(content\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.BrandHeader.cs" `
    "keeps brand-header entry point split between compact and desktop layouts" `
    @(
        "CompactBrandTitleFontSize = 18",
        "CompactBrandSubtitleFontSize = 12",
        "CompactBrandRowSeparation = 6",
        "CompactBrandHeaderSeparation = 2",
        "BuildCompactBrandHeader",
        "BuildDesktopBrandCopy",
        "BuildBrandDivider",
        "profile\.Compact",
        "if \(profile\.Compact\)",
        "return BuildCompactBrandHeader\(profile\)",
        "BuildBrandMark\(scale, compact: false\)",
        "BuildBrandDivider\(scale, height: 2\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.BrandHeader.Desktop.cs" `
    "presents desktop launcher brand copy as a polished Steam/version/cloud portal" `
    @(
        "BuildDesktopBrandCopy",
        "StS2 Mobile",
        "Sign in\. Save safely\. Play\.",
        "fontSize: 26",
        "fontSize: 11",
        "HorizontalAlignment\.Left",
        "LauncherComponentTheme\.TextPrimary",
        "LauncherComponentTheme\.CyanAccent"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.BrandHeader.Compact.cs" `
    "presents compact launcher brand copy in one condensed mobile row" `
    @(
        "BuildCompactBrandHeader",
        "CompactBrandHeaderSeparation",
        "CompactBrandRowSeparation",
        "BuildBrandMark\(scale, compact: true\)",
        "BuildCompactBrandTitle",
        "BuildCompactBrandSubtitle",
        "BuildBrandDivider\(scale, height: 1\)",
        "StS2 Mobile",
        "Saves safe\. Ready to play\.",
        "fontSize: CompactBrandTitleFontSize",
        "fontSize: CompactBrandSubtitleFontSize",
        "title\.ClipText = true",
        "subtitle\.ClipText = true",
        "CyanAccent"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.BrandMark.cs" `
    "renders compact and desktop launcher brand mark consistently" `
    @(
        "CompactBrandMarkHeight = 26",
        "BuildBrandMark",
        "BuildBrandMarkStripe",
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
    "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.Support.cs" `
    "adds compact-only bottom scroll breathing room for phone gesture areas" `
    @(
        "BuildCompactBottomScrollSpacer",
        "LauncherViewLayoutMetrics\.ScaleInt\(72, scale\)",
        "MouseFilterEnum\.Ignore"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.StatusCapsule.cs" `
    "centralizes compact status capsule sizing constants" `
    @(
        "CompactStatusBodySeparation = 5",
        "CompactStatusAccentHeight = 3",
        "CompactStatusHeadlineSeparation = 3",
        "CompactStatusHeadlineInlineSeparation = 6",
        "CompactStatusPhaseInlineWidth = 112",
        "CompactStatusPhaseHorizontalMargin = 7",
        "CompactStatusPhaseVerticalMargin = 3",
        "CompactStatusActionMinHeight = 24",
        "CompactStatusDetailHeight = 44",
        "CompactStatusDetailCueWidth = 62",
        "CompactStatusDetailCueFontSize = LauncherSectionMetrics\.CompactDetailLabelFontSize",
        "CompactStatusDetailHorizontalMargin = 8",
        "CompactStatusDetailVerticalMargin = 5",
        "CompactStatusDetailRowGap = 6",
        "CompactStatusDetailRadius = 7"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.StatusCapsule.Compact.cs" `
    "uses a low-profile compact status card with responsive headline and narrow stacked fallback" `
    @(
        "BuildCompactStatusCapsule",
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
    "src\STS2Mobile\Launcher\LauncherView.Behavior.Responsive.cs" `
    "reflows the compact status headline after viewport changes" `
    @(
        "private void UpdateCompactStatusHeadline\(Vector2 viewportSize\)",
        "_compactStatusHeadline",
        "_compactStatusPhasePanel",
        "LauncherLayoutProfile\.ForViewport\(viewportSize\)",
        "ApplyCompactStatusHeadlineLayout"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.StatusCapsule.Detail.cs" `
    "keeps normal compact status details to a stable one-line row" `
    @(
        "LauncherViewLayoutMetrics\.ScaleInt\(CompactStatusDetailHeight, scale\)",
        "statusLabel\.AutowrapMode = TextServer\.AutowrapMode\.Off",
        "statusLabel\.ClipText = true",
        "statusLabel\.TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Status.cs" `
    "uses short compact status details with tap-to-expand full details for touch devices" `
    @(
        "var fullMessage = LauncherPortalStatusFormatter\.MessageFor\(text\)",
        "_profile\.Compact",
        "LauncherPortalStatusFormatter\.CompactMessageFor\(text\)",
        "_statusLabel\.TooltipText = fullMessage",
        "_compactStatusDetailsButton",
        "_compactStatusDetailsCueLabel",
        "WireCompactStatusDetailToggle",
        "ToggleCompactStatusDetails",
        "ApplyCompactStatusDetailLayout",
        "ShouldAutoExpandCompactStatusDetails",
        "_compactStatusExpanded",
        "_compactStatusFullMessage",
        "_compactStatusShortMessage",
        "_compactStatusDetailsButton\.Pressed \+= ToggleCompactStatusDetails",
        "_compactStatusDetailsButton\.Disabled = !hasFullDetails",
        "_compactStatusDetailsButton\.MouseDefaultCursorShape = hasFullDetails",
        "_compactStatusDetailsCueLabel\.Visible = hasFullDetails",
        "_compactStatusDetailsCueLabel\.Text = expanded \? `"Hide`" : `"Details`"",
        "_compactStatusPhase",
        "`"Attention`"",
        "TextServer\.AutowrapMode\.WordSmart",
        "TextServer\.AutowrapMode\.Off",
        "_statusLabel\.ClipText = !expanded"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.StatusCapsule.Detail.cs" `
    "renders compact status details as a discoverable touch-safe Details cue" `
    @(
        "BuildCompactStatusDetailButton",
        "ApplyCompactStatusDetailButtonStyle",
        "TooltipText = `"Show full launcher status`"",
        "MouseDefaultCursorShape = Control\.CursorShape\.PointingHand",
        "detailCue",
        "`"Details`"",
        "LauncherComponentTheme\.CyanAccent"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.StatusCapsule.Styles.cs" `
    "styles compact status detail control for touch-safe affordance states" `
    @(
        "ApplyCompactStatusDetailButtonStyle",
        "BuildCompactStatusDetailButtonStyle",
        "CompactStatusDetailRadius",
        "CompactStatusDetailHorizontalMargin",
        "CompactStatusDetailVerticalMargin",
        "SetBorderWidthAll"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.StatusCapsule.Compact.cs" `
    "wires compact status detail toggle into the capsule layout" `
    @(
        "Button CompactDetailButton",
        "StyledLabel CompactDetailCue",
        "var detailButton = BuildCompactStatusDetailButton",
        "var detailRow = BuildCompactStatusDetailRow",
        "var detailCue = BuildCompactStatusDetailCue",
        "detailButton\.AddChild\(detailRow\)",
        "body\.AddChild\(detailButton\)",
        "return \(panel, headline, phasePanel, detailButton, detailCue\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.CompactWorkflow.cs" `
    "orchestrates the touch-safe responsive compact workflow step strip" `
    @(
        "BuildCompactWorkflowStrip",
        "CompactWorkflowStepHeight = LauncherSectionMetrics\.CompactDetailButtonHeight",
        "CompactWorkflowStepDenseHeight = LauncherSectionMetrics\.CompactDetailButtonHeight",
        "CompactWorkflowStepLabelFontSize = 13",
        "CompactWorkflowStepDetailFontSize = LauncherSectionMetrics\.CompactDetailLabelFontSize",
        "CompactWorkflowStepNumberFontSize = LauncherSectionMetrics\.CompactDetailLabelFontSize",
        "CompactWorkflowStepNumberMinWidth = 20",
        "CompactWorkflowStepAccentHeight = 2",
        "CompactWorkflowStepSeparation = 0",
        "CompactWorkflowStepCellGap = 3",
        "CompactWorkflowStepNumberGap = 3",
        "CompactWorkflowStepHorizontalMargin = 5",
        "CompactWorkflowStepVerticalMargin = 4",
        "GridContainer",
        "bool denseNarrowWorkflow",
        "Columns = CompactWorkflowStepNames\.Length",
        "var stepHeight = denseNarrowWorkflow",
        "\? CompactWorkflowStepDenseHeight",
        ": CompactWorkflowStepHeight",
        "new LauncherViewCompactWorkflowStrip",
        "BuildCompactWorkflowStepCell",
        "numberLabels\[i\] = cell\.NumberLabel",
        "labels\[i\] = cell\.Label",
        "detailLabels\[i\] = cell\.DetailLabel",
        "accents\[i\] = cell\.Accent",
        "buttons\[i\] = cell\.Button",
        "grid\.AddChild\(cell\.Button\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.CompactWorkflow.Result.cs" `
    "uses typed compact workflow strip and step-cell layout results instead of out-parameter construction" `
    @(
        "LauncherViewCompactWorkflowStrip",
        "LauncherViewCompactWorkflowStepCell",
        "StepNumberLabels",
        "StepLabels",
        "StepDetailLabels",
        "StepAccents",
        "StepButtons",
        "NumberLabel",
        "DetailLabel",
        "ColorRect Accent"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.CompactWorkflow.Cells.cs" `
    "builds compact workflow step cells from typed button, label, detail, and accent parts" `
    @(
        "BuildCompactWorkflowStepCell",
        "BuildCompactWorkflowStepButton",
        "BuildCompactWorkflowStepBody",
        "BuildCompactWorkflowLabelRow",
        "BuildCompactWorkflowNumberLabel",
        "BuildCompactWorkflowLabel",
        "BuildCompactWorkflowDetailLabel",
        "BuildCompactWorkflowAccent",
        "new LauncherViewCompactWorkflowStepCell",
        "button\.AddChild\(body\)",
        "labelRow\.AddChild\(numberLabel\)",
        "body\.AddChild\(detail\)",
        "body\.AddChild\(accent\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.CompactWorkflow.Cells.Body.cs" `
    "builds compact workflow cell body, label row, and accent layout chrome" `
    @(
        "BuildCompactWorkflowStepBody",
        "BuildCompactWorkflowLabelRow",
        "BuildCompactWorkflowAccent",
        "SetAnchorsPreset\(Control\.LayoutPreset\.FullRect\)",
        "new HBoxContainer",
        "OffsetLeft",
        "OffsetRight",
        "LauncherViewLayoutMetrics\.ScaleInt\(CompactWorkflowStepAccentHeight, scale\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.CompactWorkflow.Cells.Labels.cs" `
    "builds compact workflow number, title, and detail labels without hover-only hints" `
    @(
        "BuildCompactWorkflowNumberLabel",
        "BuildCompactWorkflowLabel",
        "BuildCompactWorkflowDetailLabel",
        "CompactWorkflowStepNumbers\[index\]",
        "CompactWorkflowStepNames\[index\]",
        "CompactWorkflowStepDetails\[index\]",
        "label\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill",
        "label\.TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
        "detail\.TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
        "fontSize: CompactWorkflowStepNumberFontSize",
        "fontSize: CompactWorkflowStepLabelFontSize",
        "fontSize: CompactWorkflowStepDetailFontSize"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.CompactWorkflow.Style.cs" `
    "keeps compact workflow step buttons touch-targeted and state-styled" `
    @(
        "BuildCompactWorkflowStepButton\(int index, float scale, int height\)",
        "ApplyWorkflowStepButtonStyle",
        "CompactWorkflowStepTooltips",
        "MouseDefaultCursorShape = Control\.CursorShape\.PointingHand",
        "Go to \{CompactWorkflowStepTooltips\[index\]\}",
        "LauncherViewLayoutMetrics\.ScaleInt\(height, scale\)",
        "LauncherComponentTheme\.StateNormal",
        "LauncherComponentTheme\.StateHover",
        "LauncherComponentTheme\.StatePressed",
        "LauncherComponentTheme\.StateDisabled",
        "BuildWorkflowStepStyle"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.CompactWorkflow.Navigation.cs" `
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
        "BuildPrimaryColumnBody\(profile, root\)",
        "if \(!profile\.Compact\)",
        "left\.AddChild\(workflowStrip\.Strip\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.CompactTaskHeader.Button.cs" `
    "adds a low-profile compact current-task jump button through shared two-line labels" `
    @(
        "CompactCurrentTaskButtonLabels",
        "CompactButtonDetailLabelSpec",
        "BuildCompactCurrentTaskButton",
        "SetCompactCurrentTaskButtonText",
        "CompactCurrentTaskButtonBodyName",
        "CompactCurrentTaskButtonTitleName",
        "CompactCurrentTaskButtonDetailName",
        "CompactButtonDetailLabelSpec\.Default",
        "CompactButtonDetailLabels\.Apply",
        "enabled: true",
        'SetCompactCurrentTaskButtonText\(button, scale, "Start here", "Setup guide"\)',
        "LauncherSectionMetrics\.CompactDetailButtonHeight",
        "LauncherSectionMetrics\.CompactDetailButtonFontSize",
        "LauncherButtonStyles\.ApplySupportAction",
        "compactCurrentTaskButton"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.cs" `
    "anchors the compact current-task jump button outside the scrolling body so it remains reachable" `
    @(
        "var compactCurrentTaskButton = BuildCompactCurrentTaskButton\(scale, profile\.Compact\)",
        "if \(profile\.Compact\)",
        "BuildCompactStickyTaskHeader\(profile, compactCurrentTaskButton, workflowStrip\.Strip\)",
        "BuildPrimaryColumnBody\(profile, root\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.Body.cs" `
    "builds the primary scroll container and centered body after compact sticky chrome" `
    @(
        "BuildPrimaryColumnBody",
        "new ScrollContainer",
        "leftScroll\.FollowFocus = true",
        "root\.AddChild\(leftScroll\)",
        "new MarginContainer",
        "leftFrame\.AddChild\(left\)",
        "LauncherViewLayoutMetrics\.CompactPrimaryColumnSeparation",
        "LauncherViewLayoutMetrics\.PrimaryColumnSeparation",
        "return new LauncherViewPrimaryBody"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.CompactTaskHeader.cs" `
    "builds the compact current-task button and workflow strip as one viewport-reflowable sticky header grid" `
    @(
        "CompactStickyTaskHeaderInlineGap = 6",
        "CompactStickyTaskHeaderStackGap = 3",
        "CompactStickyTaskButtonMinWidth = 176",
        "CompactInlineCurrentTaskHeight = LauncherSectionMetrics\.CompactDetailButtonHeight",
        "CompactStackedCurrentTaskHeight = CompactWorkflowStepDenseHeight",
        "CompactStickyTaskHeaderStackWidth = 560",
        "CompactStickyTaskHeaderGridName",
        "CompactStickyTaskToolbarRadius = 7",
        "CompactStickyTaskToolbarHorizontalMargin = 5",
        "CompactStickyTaskToolbarVerticalMargin = 4",
        "GridContainer Header",
        "Control workflowStrip",
        "return \(WrapCompactStickyTaskHeader\(scale, header\), header\)",
        "BuildCompactStickyTaskHeader",
        "new GridContainer",
        "Name = CompactStickyTaskHeaderGridName",
        "ApplyCompactStickyTaskHeaderLayout",
        "header\.AddChild\(compactCurrentTaskButton\)",
        "header\.AddChild\(workflowStrip\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.CompactTaskHeader.Style.cs" `
    "wraps the compact sticky task header in a low-profile toolbar shell" `
    @(
        "WrapCompactStickyTaskHeader",
        "BuildCompactStickyTaskHeaderStyle",
        "new PanelContainer",
        "BuildCompactStickyTaskHeaderStyle\(scale\)",
        "LauncherComponentTheme\.Panel",
        "LauncherStyleBoxes\.MakeFilled",
        "CompactStickyTaskToolbarRadius",
        "SetBorderWidthAll",
        "LauncherViewLayoutMetrics\.ScaleInt\(CompactStickyTaskToolbarHorizontalMargin, scale\)",
        "LauncherViewLayoutMetrics\.ScaleInt\(CompactStickyTaskToolbarVerticalMargin, scale\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.CompactTaskHeader.Layout.cs" `
    "stacks the compact sticky task header on narrow compact viewports so task and workflow controls stay readable" `
    @(
        "ShouldStackCompactStickyTaskHeader",
        "profile\.ContentMaxWidth < LauncherViewLayoutMetrics\.ScaleInt",
        "ApplyCompactStickyTaskHeaderLayout",
        "header\.Columns = stacked \? 1 : 2",
        "stacked \? CompactStickyTaskHeaderStackGap : CompactStickyTaskHeaderInlineGap",
        "stacked \? CompactStickyTaskHeaderStackGap : CompactStickyTaskHeaderInlineGap",
        "compactCurrentTaskButton\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill",
        "LauncherViewLayoutMetrics\.ScaleInt\(CompactStackedCurrentTaskHeight, scale\)",
        "workflowStrip\.SizeFlagsVertical = Control\.SizeFlags\.ShrinkBegin",
        "compactCurrentTaskButton\.SizeFlagsHorizontal = Control\.SizeFlags\.ShrinkBegin",
        "LauncherViewLayoutMetrics\.ScaleInt\(CompactStickyTaskButtonMinWidth, scale\)",
        "LauncherViewLayoutMetrics\.ScaleInt\(CompactInlineCurrentTaskHeight, scale\)",
        "workflowStrip\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Behavior.Responsive.cs" `
    "reflows the compact sticky task header after Android viewport changes" `
    @(
        "UpdateCompactStickyTaskHeader\(Vector2 viewportSize\)",
        "LauncherLayoutProfile\.ForViewport\(viewportSize\)",
        "ApplyCompactStickyTaskHeaderLayout",
        "_compactStickyTaskHeader",
        "_compactWorkflowStrip"
)

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.CompactWorkflow.Data.cs" `
    "defines compact workflow step labels, details, and navigation enum" `
    @(
        "CompactWorkflowStepNames",
        "CompactWorkflowStepNumbers",
        "CompactWorkflowStepDetails",
        "CompactWorkflowStep",
        '"Sign in"',
        '"Verify"',
        '"Files"',
        '"Play"',
        '"1"',
        '"2"',
        '"3"',
        '"4"',
        "CompactWorkflowStepTooltips",
        "Sign in",
        "Steam Guard",
        "Game files",
        "Saves safe",
        "Open sign-in",
        "Open Steam Guard",
        "Open game files",
        "Open play and saves"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.CompactWorkflow.State.cs" `
    "updates compact workflow active/completed step colors" `
    @(
        "SetCompactWorkflowStep",
        "_workflowStepNumberLabels",
        "_workflowStepNumberLabels\[i\]\.AddThemeColorOverride",
        "_workflowStepDetailLabels\[i\]\.AddThemeColorOverride",
        "LauncherComponentTheme\.OrangeHot",
        "LauncherComponentTheme\.CyanAccent",
        "LauncherComponentTheme\.CyanDim",
        "LauncherComponentTheme\.TextMuted"
)

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.CompactWorkflow.Navigation.cs" `
    "wires the compact current-task jump button without invoking launcher actions directly" `
    @(
        "_compactCurrentTaskButton",
        "WireCompactCurrentTaskNavigation",
        "ScrollCompactPrimaryTo\(_compactCurrentTaskTarget\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.CompactWorkflow.State.cs" `
    "tracks the compact current-task jump target without invoking launcher actions directly" `
    @(
        "_compactCurrentTaskButton",
        "_compactCurrentTaskTarget",
        "SetCompactCurrentTask",
        "SetCompactCurrentTaskButtonText",
        "string detail",
        "SetCompactCurrentTaskButtonText\(_compactCurrentTaskButton, _scale, text, detail\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Auth.cs" `
    "updates compact workflow steps during auth section transitions" `
    @(
        "SetCompactWorkflowStep\(CompactWorkflowStep\.SignIn\)",
        "SetCompactWorkflowStep\(CompactWorkflowStep\.Code\)",
        "SetLoginFormVisible",
        "ShowCodePrompt"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Download.cs" `
    "updates compact workflow steps during download section transitions" `
    @(
        "SetCompactWorkflowStep\(CompactWorkflowStep\.Files\)",
        "ShowDownloadProgress",
        "SetDownloadProgress"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Actions.cs" `
    "updates compact workflow steps during play and retry section transitions" `
    @(
        "SetCompactWorkflowStep\(CompactWorkflowStep\.SignIn\)",
        "SetCompactWorkflowStep\(CompactWorkflowStep\.Play\)",
        "ShowLaunchActions",
        "ShowRetry"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Auth.cs" `
    "updates compact current-task jump button during auth transitions" `
    @(
        'SetCompactCurrentTask\("Sign in", Login, "Steam account"\)',
        'SetCompactCurrentTask\("Verify", Code, "Steam Guard code"\)'
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Download.cs" `
    "updates compact current-task jump button during download transitions" `
    @(
        'SetCompactCurrentTask\("Files", Download, "Download version"\)',
        "ShowDownloadAction",
        "ShowDownloadProgress"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Actions.cs" `
    "updates compact current-task jump button during play and retry transitions" `
    @(
        'SetCompactCurrentTask\("Retry", Actions\.RetryScrollTarget, "Restart safely"\)',
        'SetCompactCurrentTask\("Play", Actions\.ReadyScrollTarget, "Play and saves"\)'
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Auth.cs" `
    "labels compact auth current-task jumps as navigation rather than direct launcher actions" `
    @(
        '"Sign in"',
        '"Verify"'
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Download.cs" `
    "labels compact download current-task jumps as navigation rather than direct launcher actions" `
    @(
        '"Files"',
        "Download version"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Actions.cs" `
    "labels compact play current-task jumps as navigation rather than direct launcher actions" `
    @(
        '"Retry"',
        '"Play"'
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Primary.cs" `
    "promotes compact retry recovery to a primary structured action" `
    @(
        'compact \? CompactRetryButtonText\(\) : "RETRY"',
        "LauncherButtonStyles\.ApplyPrimaryAction\(retryButton, scale\)",
        "SetCompactActionButtonText\(retryButton, retryButton\.Text\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.ReadySummary.cs" `
    "labels compact retry recovery as TRY AGAIN with restart-task detail" `
    @(
        "CompactRetryButtonText",
        'CompactPlaySyncDrawerText\("Try Again", "Restart task"\)'
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.ReadySummary.cs" `
    "labels compact launch CTA with selected-version detail" `
    @(
        "CompactLaunchButtonText\(string text\)",
        "CompactLaunchButtonText",
        "Start Game",
        "Ready version"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Visibility.cs" `
    "applies compact launch CTA text to the launch button" `
    @(
        "SetCompactActionButtonText\(_launchButton",
        "CompactLaunchButtonText\(text\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Auth.cs" `
    "suppresses compact first-run safe-flow guidance during active auth states" `
    @(
        "SetFirstRunGuideVisible\(false\)",
        "SetLoginFormVisible\(bool visible, bool disabled\)[\s\S]*SetFirstRunGuideVisible\(false\)[\s\S]*HideCompactCompletedAuthSections",
        "ShowCodePrompt\(bool wasIncorrect\)[\s\S]*SetFirstRunGuideVisible\(false\)",
        "SetLoginFormVisible",
        "FirstRunGuide\.Visible = !_profile\.Compact \|\| visible"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Download.cs" `
    "suppresses compact first-run safe-flow guidance during active download states" `
    @(
        "ShowDownloadAction\(string buttonText\)[\s\S]*SetFirstRunGuideVisible\(false\)",
        "ShowDownloadAction"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Actions.cs" `
    "restores compact setup guidance only when no active action section is shown" `
    @(
        "SetFirstRunGuideVisible\(true\)",
        "SetFirstRunGuideVisible\(false\)",
        "ShowLaunchActions",
        "ShowRetry",
        "HideActions"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\CodeSection.cs" `
    "orchestrates compact Steam Guard code entry layout and submission wiring" `
    @(
        "bool compactStackedActionRows = false",
        "_compactStackedActionRows = compact && compactStackedActionRows",
        "CreateCodePromptLabel\(scale, compact\)",
        "CreateCodeHelpLabel\(scale, compact\)",
        "CreateCodeField\(scale, compact\)",
        "CreateCodeSubmitButton\(scale, compact\)",
        "BuildCompactCodeActionRow",
        "GridContainer _compactCodeActionRow",
        "codeActionParent\.AddChild\(_codeField\)",
        "codeActionParent\.AddChild\(submitButton\)",
        "MoveChild\(_codeHelpLabel, compactCodeActionRow\.GetIndex\(\) \+ 1\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\CodeSection.Labels.cs" `
    "isolates compact Steam Guard prompt and help label construction" `
    @(
        "CreateCodePromptLabel\(float scale, bool compact\)",
        "CreateCodeHelpLabel\(float scale, bool compact\)",
        "CompactCodePromptHeight",
        "CompactCodeHelpHeight",
        "AutowrapMode = TextServer\.AutowrapMode\.Off",
        "AutowrapMode = TextServer\.AutowrapMode\.WordSmart",
        "ConfigureCompactCodeLabel",
        "TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\CodeSection.Input.cs" `
    "isolates Steam Guard code input sizing and keyboard configuration" `
    @(
        "CreateCodeField\(float scale, bool compact\)",
        "compact \? `"ABC123`" : `"Steam Guard code`"",
        "VirtualKeyboardType\.Default",
        "CodeInputHeight\(bool compact\)",
        "CodeInputFontSize\(bool compact\)",
        "CodeInputHeight",
        "CodeInputFontSize",
        "field\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\CodeSection.SubmitButton.cs" `
    "isolates Steam Guard verification button construction and compact labels" `
    @(
        "CreateCodeSubmitButton\(float scale, bool compact\)",
        "CodeSubmitFontSize",
        "CompactCodeSubmitText",
        "SetCompactCodeSubmitButtonText",
        "height: CodeInputHeight\(compact\)",
        "Verify Code",
        "button\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\CodeSection.SubmitButton.cs" `
    "wires compact Steam Guard submit to shared two-line labels" `
    @(
        "CompactCodeSubmitLabels",
        "CompactButtonDetailLabelSpec",
        "CompactCodeSubmitText",
        "Verify Code",
        "Submit once",
        "SetCompactCodeSubmitButtonText",
        "CompactButtonDetailLabels\.Apply"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\CodeSection.Layout.cs" `
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
    "src\STS2Mobile\Launcher\LauncherView.Behavior.Responsive.cs" `
    "updates compact section responsive rows after viewport changes" `
    @(
        "private void UpdateCompactSectionResponsiveRows\(Vector2 viewportSize\)",
        "LauncherLayoutProfile\.ForViewport\(viewportSize\)",
        "Code\.UpdateViewportProfile\(profile\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\CodeSection.Prompt.cs" `
    "keeps compact Steam Guard retry guidance short and readable" `
    @(
        "CompactIncorrectPrompt",
        "Code rejected",
        "CompactIncorrectHelp",
        "Use newest Steam Guard code",
        "Old codes can expire; spaces removed",
        "One-shot submit; code is not stored",
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
    "src\STS2Mobile\Launcher\LauncherView.Sections.Download.cs" `
    "restores compact install section for downloads" `
    @(
        "ShowDownloadAction",
        "SetCompactReadyInstallSectionVisible\(true\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Actions.cs" `
    "suppresses compact install section after launch-ready" `
    @(
        "SetCompactReadyInstallSectionVisible",
        "ShowLaunchActions",
        "SetCompactReadyInstallSectionVisible\(false\)",
        "!_profile\.Compact",
        "Download\.Visible = visible"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Auth.cs" `
    "defines completed compact auth section suppression" `
    @(
        "HideCompactCompletedAuthSections",
        "ShowCodePrompt",
        "HideCompactCompletedAuthSections\(showCode: true\)",
        "Login\.SetFormVisible\(false, disabled: true\)",
        "Code\.Visible = showCode"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Download.cs" `
    "suppresses completed compact auth sections before download work" `
    @(
        "ShowDownloadAction",
        "HideCompactCompletedAuthSections\(showCode: false\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Actions.cs" `
    "suppresses completed compact auth sections before play or retry work" `
    @(
        "ShowRetry",
        "ShowLaunchActions",
        "HideCompactCompletedAuthSections\(showCode: false\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Scroll.cs" `
    "defines compact active-section scrolling and anchor padding" `
    @(
        "CompactScrollAnchorTopPadding = 14",
        "ScrollCompactPrimaryTo",
        "ApplyCompactScrollAnchorPadding",
        "!_profile\.Compact",
        "Callable\.From",
        "PrimaryScroll\.EnsureControlVisible\(target\)",
        "PrimaryScroll\.ScrollVertical",
        "LauncherViewLayoutMetrics\.ScaleInt\(CompactScrollAnchorTopPadding, _scale\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Auth.cs" `
    "scrolls compact auth transitions to the active section" `
    @(
        "ScrollCompactPrimaryTo\(Login\)",
        "ScrollCompactPrimaryTo\(Code\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Download.cs" `
    "scrolls compact download transitions to the active section" `
    @(
        "ScrollCompactPrimaryTo\(Download\)",
        "SetCompactCurrentTask"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Actions.cs" `
    "scrolls compact play and retry transitions to the active section" `
    @(
        "ScrollCompactPrimaryTo\(Actions\.RetryScrollTarget\)",
        "ScrollCompactPrimaryTo\(Actions\.ReadyScrollTarget\)",
        "ScrollCompactPrimaryTo\(FirstRunGuide\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Sections.Scroll.cs" `
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
    "initializes compact workflow scroll anchors to the first-run guide" `
    @(
        "_compactScrollAnchorTarget",
        "_compactScrollAnchorTarget = FirstRunGuide",
        "_compactCurrentTaskTarget = FirstRunGuide"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.CompactWorkflow.State.cs" `
    "tracks compact scroll anchor state separately from the current task target" `
    @(
        "_compactScrollAnchorTarget",
        "_compactCurrentTaskTarget",
        "SetCompactCurrentTask",
        "_compactCurrentTaskTarget = target",
        "_compactScrollAnchorTarget = target"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Behavior.Reanchor.cs" `
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
    "src\STS2Mobile\Launcher\Sections\ActionSection.Visibility.cs" `
    "anchors compact ready/retry scrolling to the actual primary controls" `
    @(
        "ReadyScrollTarget",
        "_compact \? _cloudGroup : _launchButton",
        "RetryScrollTarget",
        "_retryButton"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.cs" `
    "centralizes compact install control sizing constants and width class state" `
    @(
        "CompactDownloadActionBodyName",
        "CompactDownloadActionTitleName",
        "CompactDownloadActionDetailName",
        "CompactDownloadActionHeight = LauncherSectionMetrics\.CodeInputHeight",
        "CompactVersionHelpHeight",
        "CompactVersionHelpFontSize",
        "_compactSelectedVersionLabel",
        "_compactSelectedVersionPanel",
        "_compactVersionControlsRow",
        "BuildCompactVersionControlsRow",
        "CompactSelectedVersionBranchLimit = 18",
        "CompactSelectedVersionStackedBranchLimit = 28",
        "_compactStackedActionRows = compact && compactStackedActionRows",
        "compactStackedActionRows = false"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.Construction.Version.cs" `
    "constructs the compact install version details toggle" `
    @(
        "BuildBranchDetailsToggle",
        "Show Version Details",
        "LauncherSectionMetrics\.CompactDrawerToggleHeight",
        "LauncherButtonStyles\.ApplySupportAction",
        "button\.Pressed \+= ToggleBranchDetails"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.Construction.Version.Selected.cs" `
    "constructs compact selected-version summary controls with local-file cues" `
    @(
        "BuildCompactSelectedVersionPanel",
        "BuildCompactSelectedVersionLabel",
        "ApplySelectedVersionSummaryButtonStyle",
        "button\.Pressed \+= OpenCompactBranchDetailsFromSelectedVersion",
        "TooltipText = `"Change game version for local files`"",
        "MouseDefaultCursorShape = Control\.CursorShape\.PointingHand",
        "CompactVersionSummaryFontSize",
        "CompactVersionSummaryHeight",
        "CompactStackedVersionSummaryHeight",
        "CompactVersionSummaryHorizontalMargin",
        "CompactVersionSummaryVerticalMargin",
        "label\.ClipText = compact",
        "label\.TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
        "label\.CustomMinimumSize = new Vector2",
        "AutowrapMode\.Off",
        "TextServer\.AutowrapMode\.WordSmart",
        "TextServer\.OverrunBehavior\.TrimEllipsis",
        "ClipText = compact && !_compactStackedActionRows"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.Construction.Version.Dropdown.cs" `
    "constructs and places the install-version dropdown in compact and full layouts" `
    @(
        "BuildBranchDropdown",
        "_compactVersionControlsRow\.AddChild\(_branchDropdown\)",
        "AddChild\(_compactVersionControlsRow\)",
        "AddChild\(_branchDropdown\)",
        "dropdown\.ItemSelected \+= ApplyGameBranch"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.Construction.Version.Refresh.cs" `
    "constructs refresh-version and bounded branch help controls" `
    @(
        "BuildRefreshBranchesButton",
        "_compactVersionControlsRow\.AddChild\(_refreshBranchesButton\)",
        "RefreshGameVersionsRequested\?\.Invoke",
        "SetCompactVersionActionButtonText",
        "Refresh Versions",
        "Update branch list",
        "BuildBranchHelpLabel",
        "CompactVersionHelpFontSize",
        "CompactVersionHelpHeight"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.Branches.Text.cs" `
    "states that version downloads affect local files and do not mutate Steam Cloud saves" `
    @(
        "_branchDetailsExpanded",
        "Download/update changes local files for the selected game version only",
        "does not change Steam Cloud saves",
        "SelectedOptionStatus",
        "SelectorInstallSlotHelpText",
        "CompactInstallVersionHelpText",
        "Selected version:",
        "Install slot:",
        "Downloads do not change Steam Cloud saves",
        "SetCompactVersionActionButtonText",
        "ApplyBranchControlVisibility\(\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactVersion.cs" `
    "builds compact selected-version layout rows and primary-action placement" `
    @(
        "compactStackedActionRows \? new VBoxContainer\(\) : new HBoxContainer\(\)",
        "BuildCompactVersionControlsRow",
        "MoveCompactPrimaryInstallControlsBeforeVersionDetails",
        "MoveChild\(_compactSelectedVersionPanel, _branchDetailsToggle\.GetIndex\(\)\)",
        "MoveChild\(_downloadButton, _branchDetailsToggle\.GetIndex\(\)\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactVersion.Summary.cs" `
    "renders compact selected-version summary local-file copy" `
    @(
        "CompactSelectedVersionStackedBranchLimit",
        "CompactSelectedVersionHeadline",
        "Files for:",
        "SelectedOptionCompactStatus",
        "Saves unchanged",
        "Cloud unchanged",
        "Change version",
        "Change",
        "CompactInstallFileScope",
        "Default files",
        "Separate files"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactVersion.Summary.Style.cs" `
    "keeps compact selected-version summary card skinning isolated from copy generation" `
    @(
        "ApplySelectedVersionSummaryButtonStyle",
        "LauncherComponentTheme\.StateNormal",
        "LauncherComponentTheme\.StateHover",
        "LauncherComponentTheme\.StatePressed",
        "LauncherComponentTheme\.StateDisabled",
        "BuildSelectedVersionSummaryStyle",
        "CompactVersionSummaryRadius",
        "CompactVersionSummaryHorizontalMargin",
        "CompactVersionSummaryVerticalMargin",
        "Color body,",
        "Color border",
        "BuildSelectedVersionSummaryStyle\(float scale, bool compact\)",
        "compact \? LauncherSectionMetrics\.CompactVersionSummaryRadius : 8",
        "compact \? LauncherSectionMetrics\.CompactVersionSummaryHorizontalMargin : 12",
        "compact \? LauncherSectionMetrics\.CompactVersionSummaryVerticalMargin : 9"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactVersion.ActionButton.cs" `
    "renders compact version action labels as structured title/detail controls" `
    @(
        "SetCompactVersionActionButtonText",
        "CompactButtonDetailLabels\.Apply",
        "CompactButtonDetailLabelSpec",
        "CompactVersionActionLabels",
        "CompactVersionActionBodyName",
        "CompactVersionActionTitleName",
        "CompactVersionActionDetailName",
        "CompactButtonDetailLabelSpec\.Default",
        "enabled: false",
        "enabled: true",
        '\$"\{title\}\\n\{detail\}"'
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactDownload.Text.cs" `
    "maps compact download action copy to local-file-only title/detail labels" `
    @(
        "CompactDownloadButtonText",
        "CompactDownloadButtonTitleDetail",
        "`"Download Version`"",
        "`"Redownload Version`"",
        "`"Retry Download`"",
        "`"Downloading\.\.\.`"",
        "Local files only",
        "Rebuild local files",
        "Steam files"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.cs" `
    "passes compact width class into the Game Install section for responsive selected-version summaries" `
    @(
        "new DownloadSection\(scale, profile\.Compact, profile\.CompactStackedActionRows\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.cs" `
    "keeps compact version drawer toggle before expanded install-version controls" `
    @(
        "_branchDetailsToggle = BuildBranchDetailsToggle",
        "AddChild\(_branchDetailsToggle\)",
        "_branchDropdown = BuildBranchDropdown",
        "(?s)_branchDetailsToggle = BuildBranchDetailsToggle.*AddChild\(_branchDetailsToggle\).*_branchDropdown = BuildBranchDropdown"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.Construction.Version.Dropdown.cs" `
    "uses primary compact touch-target sizing for the install-version dropdown" `
    @(
        "BuildBranchDropdown",
        "compact \? LauncherSectionMetrics\.PrimaryButtonHeight : LauncherSectionMetrics\.SecondaryButtonHeight",
        "compact \? LauncherSectionMetrics\.PrimaryButtonFontSize : LauncherSectionMetrics\.SecondaryButtonFontSize",
        "ApplyDropdownAction",
        "(?s)ApplyDropdownAction\(\s*dropdown,\s*scale,.*?,\s*compact\s*\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactVersion.cs" `
    "puts the compact install primary action before optional version details" `
    @(
        "MoveCompactPrimaryInstallControlsBeforeVersionDetails",
        "MoveChild\(_compactSelectedVersionPanel, _branchDetailsToggle\.GetIndex\(\)\)",
        "MoveChild\(_downloadButton, _branchDetailsToggle\.GetIndex\(\)\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactDownload.ActionButton.cs" `
    "renders compact install primary actions as structured title/detail labels" `
    @(
        "CompactDownloadActionLabels",
        "CompactButtonDetailLabelSpec",
        "CompactDownloadActionBodyName",
        "CompactDownloadActionTitleName",
        "CompactDownloadActionDetailName",
        "CompactDownloadActionTitleFontSize",
        "CompactDownloadActionDetailFontSize",
        "CompactDownloadActionHorizontalMargin",
        "CompactDownloadActionVerticalMargin",
        "SetCompactDownloadButtonText",
        "CompactButtonDetailLabels\.Apply"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactDownload.Text.cs" `
    "keeps compact install primary-action copy as structured title/detail text" `
    @(
        "CompactDownloadButtonTitleDetail",
        "CompactDownloadButtonText",
        "`"DOWNLOAD SELECTED VERSION`"",
        "`"Download Version`"",
        "`"Local files only`"",
        "`"REDOWNLOAD SELECTED VERSION`"",
        "`"Redownload Version`"",
        "`"Rebuild local files`"",
        "`"Retry Download`"",
        "`"Downloading\.\.\.`"",
        "`"Steam files`""
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactDownload.cs" `
    "promotes compact download progress controls directly under the active primary action" `
    @(
        "MoveCompactProgressControlsNearPrimaryAction",
        "MoveChild\(_progressLabel, _downloadButton\.GetIndex\(\) \+ 1\)",
        "MoveChild\(_progressBar, _progressLabel\.GetIndex\(\) \+ 1\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.CompactDownload.Text.cs" `
    "keeps compact download progress copy concise and bounded" `
    @(
        "CompactDownloadProgressButtonText",
        "CompactDownloadProgressText",
        "CompactDownloadProgressDetail",
        "NormalizeCompactProgressText",
        "Downloading selected version"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.Construction.Download.cs" `
    "constructs compact download progress controls with readable mobile sizing" `
    @(
        "new StyledProgressBar\(scale, compact\)",
        "BuildProgressLabel",
        "label\.AutowrapMode = TextServer\.AutowrapMode\.WordSmart",
        "label\.ClipText = compact",
        "label\.TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
        "label\.CustomMinimumSize = new Vector2",
        "compact\s*\?\s*LauncherSectionMetrics\.SecondaryButtonFontSize",
        "compact\s*\?\s*LauncherComponentTheme\.CyanAccent"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\DownloadSection.Progress.cs" `
    "updates compact download progress state next to the disabled primary action" `
    @(
        "_progressLabel\.Text = _compact \? CompactDownloadProgressText\(text\) : text",
        "SetCompactDownloadButtonText\(_downloadButton, CompactDownloadProgressButtonText\(\)\)",
        "_compactSelectedVersionPanel\.Disabled = true",
        "_compactSelectedVersionPanel\.Disabled = false"
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
    "src\STS2Mobile\Launcher\LauncherStartupFlow.StartupMode.cs" `
    "keeps startup safe-mode decisions in a small marker-driven shell" `
    @(
        "private sealed partial class StartupMode",
        "CreateFromMarkers",
        "PreviousStartupPhase\.FromMarkers",
        "ConsumeManualSafeLaunchMarker",
        "SafeLaunchRequested",
        "ShouldForceLocalSaves",
        "PhaseSettingsAndSaves",
        "PhaseGameStartup",
        "ShouldSkipShaderWarmup",
        "PhaseShaderWarmup",
        "SafeLaunchMessage"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherStartupFlow.StartupMode.PreviousPhase.cs" `
    "isolates previous startup phase marker reads and comparisons" `
    @(
        "PreviousStartupPhase",
        "LauncherLaunchMarkers\.ReadStartupPhase",
        "StringComparison\.OrdinalIgnoreCase",
        "Matches\(string phase\)",
        "DescribePreviousStall\(string message\)",
        "\$""\{message\} \{Phase\}"""
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherStartupFlow.StartupMode.SaveModePlan.cs" `
    "isolates startup local-save safe-mode application" `
    @(
        "StartupSaveModePlan",
        "Loading settings and saves in local-only safe mode",
        "Loading settings and saves",
        "LauncherPreferences\.LoadAndApplyCloudSyncEnabled",
        "LauncherCloudSaveState\.DisableCloudSyncForLaunch",
        "PatchHelper\.Log\(ReasonLog\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\ShaderWarmupScreen.cs" `
    "keeps shader warmup screen root limited to state and entry point" `
    @(
        "internal sealed partial class ShaderWarmupScreen : Control",
        "WarmupVersion = 5",
        "TaskCompletionSource<bool> _tcs",
        "Label _statusLabel",
        "Label _detailLabel",
        "ProgressBar _progressBar",
        "RunAsync",
        "Initialize\(\)",
        "await _tcs\.Task"
    )

Add-Check `
    "src\STS2Mobile\Launcher\ShaderWarmupScreen.Run.cs" `
    "isolates shader warmup screen initialization and deferred execution" `
    @(
        "private void Initialize\(\)",
        "ZIndex = 100",
        "GetViewport\(\)\?\.GetVisibleRect\(\)\.Size",
        "BuildUI\(vpSize\)",
        "PatchHelper\.Log\(Message\.ScreenInitialized\)",
        "PatchHelper\.Log\(Message\.ScreenBuildFailed\(ex\)\)",
        "_tcs\?\.TrySetResult\(false\)",
        "Callable\.From\(RunWarmup\)\.CallDeferred\(\)",
        "RunWarmupTaskAsync",
        "PatchHelper\.Log\(Message\.RunFailed\(ex\)\)",
        "_tcs\?\.TrySetResult\(true\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\ShaderWarmupScreen.WarmupRun.cs" `
    "isolates shader warmup run context and completion reporting" `
    @(
        "WarmupCompletion",
        "MaterialCount",
        "ElapsedMilliseconds",
        "WarmupRun",
        "SceneTree Tree",
        "ShaderWarmupProgress Progress",
        "Stopwatch Stopwatch",
        "CompleteAndReport",
        "Progress\.Complete\(completion\)",
        "PatchHelper\.Log\(Message\.Completed\(completion\)\)",
        "CreateWarmupRun",
        "Stopwatch\.StartNew\(\)",
        "CreateProgress",
        "ShaderWarmupProgress\.ForLabels"
    )

Add-Check `
    "src\STS2Mobile\Launcher\ShaderWarmupScreen.Execution.cs" `
    "isolates shader warmup collection, rendering, and marker completion flow" `
    @(
        "RunWarmupAsync",
        "CreateWarmupRun",
        "CollectWarmupMaterialsAsync",
        "materials\.Count == 0",
        "MarkWarmupComplete\(\)",
        "RenderWarmupMaterialsAsync",
        "warmup\.CompleteAndReport\(materials\.Count\)",
        "WaitFinishDelayAsync",
        "progress\.ShowScanning\(\)",
        "ShaderWarmupMaterialScanner\.CollectAsync",
        "PatchHelper\.Log\(Message\.Collected\(materials\.Count\)\)",
        "progress\.ShowCompiling\(\)",
        "ShaderWarmupRenderer\.ForScreen",
        "WriteWarmupVersion\(\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\ShaderWarmupScreen.Timing.cs" `
    "isolates shader warmup frame and finish-delay waits" `
    @(
        "WaitPostDrawAsync",
        "RenderingServer\.SignalName\.FramePostDraw",
        "WaitFinishDelayAsync",
        "GetTree\(\)\.CreateTimer\(0\.5\)",
        "SceneTreeTimer\.SignalName\.Timeout"
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
    "routes startup status creation between Android card and legacy label" `
    @(
        "internal static partial class LauncherStartupStatus",
        "OperatingSystem\.IsAndroid\(\)",
        "CreateAndroidStatusCard\(parent, viewportSize\)",
        "CreateLegacyLabel\(parent, viewportSize\)",
        "Startup status label creation failed",
        "CalculateSafeMargin"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherStartupStatus.Android.cs" `
    "composes an Android-readable framed startup status card after launcher close" `
    @(
        "internal static partial class LauncherStartupStatus",
        "AndroidMinimumScale = 1\.06f",
        "AndroidWidthRatio = 0\.94f",
        "AndroidMessageFontSize = 18",
        "AndroidPanelHeight = 98",
        "CreateAndroidStatusCard",
        "PanelContainer",
        "BuildAndroidPanelStyle",
        "CreateAndroidTitleLabel",
        "CreateAndroidMessageLabel",
        "CalculateAndroidPanelWidth",
        "MouseFilterEnum\.Ignore"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherStartupStatus.Android.Metrics.cs" `
    "isolates Android startup status scale and width math" `
    @(
        "CalculateAndroidScale",
        "ReferenceShortEdge",
        "AndroidMinimumScale",
        "AndroidMaximumScale",
        "Math\.Clamp",
        "CalculateAndroidPanelWidth",
        "AndroidWidthRatio"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherStartupStatus.Android.Labels.cs" `
    "isolates Android startup status title and message labels" `
    @(
        "CreateAndroidTitleLabel",
        "Starting Game",
        "AndroidTitleFontSize",
        "LauncherComponentTheme\.OrangeHot",
        "CreateAndroidMessageLabel",
        "MessageNodeName",
        "AndroidMessageFontSize",
        "AutowrapMode = TextServer\.AutowrapMode\.WordSmart",
        "LauncherComponentTheme\.TextPrimary"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherStartupStatus.Android.Style.cs" `
    "isolates Android startup status card frame styling" `
    @(
        "BuildAndroidPanelStyle",
        "LauncherStyleBoxes\.MakeFilled",
        "PanelBackground",
        "0\.92f",
        "AndroidPanelRadius",
        "LauncherComponentTheme\.CyanDim",
        "AndroidPanelHorizontalMargin",
        "AndroidPanelVerticalMargin"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherStartupStatus.Legacy.cs" `
    "keeps the desktop startup status label fallback isolated" `
    @(
        "CreateLegacyLabel",
        "CalculateFontSize",
        "AutowrapMode = TextServer\.AutowrapMode\.WordSmart",
        "Control\.LayoutPreset\.TopWide",
        "font_size",
        "new Color\(0\.55f, 0\.85f, 1f\)"
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
    "keeps compact action construction ordered as branch details before ready/cloud/support controls" `
    @(
        "BuildBranchControls\(scale, compact\)",
        "_branchDetailsToggle = branchControls\.DetailsToggle",
        "_branchDropdown = branchControls\.Dropdown",
        "BuildReadyVersionSummaryControls\(scale, compact\)",
        "SetGameBranch\(_gameBranch\)",
        "BuildCloudControls\(scale, compact\)",
        "_cloudSafetyToggle = cloudControls\.CloudSafetyToggle",
        "_cloudOptionsToggle = cloudControls\.CloudOptionsToggle",
        "BuildSupportControls\(scale, compact, supportToolsParent\)",
        "_supportToggle = supportControls\.SupportToggle",
        "(?s)BuildBranchControls\(scale, compact\).*BuildReadyVersionSummaryControls\(scale, compact\).*SetGameBranch\(_gameBranch\).*BuildCloudControls\(scale, compact\).*BuildSupportControls\(scale, compact, supportToolsParent\).*ArrangeCompactReadyStatePriority\(\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Branch.cs" `
    "uses primary compact touch-target sizing for the ready-state version dropdown" `
    @(
        "compact \? LauncherSectionMetrics\.PrimaryButtonHeight : LauncherSectionMetrics\.SecondaryButtonHeight",
        "compact \? LauncherSectionMetrics\.PrimaryButtonFontSize : LauncherSectionMetrics\.SecondaryButtonFontSize",
        "ApplyDropdownAction",
        "(?s)ApplyDropdownAction\(\s*branchDropdown,\s*scale,.*?,\s*compact\s*\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Components\LauncherButtonStyles.cs" `
    "keeps launcher button action presets as the public styling API" `
    @(
        "internal static partial class LauncherButtonStyles",
        "ApplyPrimaryAction",
        "ApplySafeAction",
        "ApplySupportAction",
        "ApplyCloudPullAction",
        "ApplyDangerAction",
        "LauncherComponentTheme\.OrangeAccent",
        "LauncherComponentTheme\.CyanAccent",
        "filled: false",
        "new Color\(0\.07f, 0\.18f, 0\.15f\)",
        "new Color\(0\.22f, 0\.07f, 0\.07f\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Components\LauncherButtonStyles.Dropdown.cs" `
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
    "src\STS2Mobile\Launcher\Components\LauncherButtonStyles.State.cs" `
    "keeps launcher button state styleboxes and text colors isolated" `
    @(
        "private static void Apply",
        "BuildButtonStateStyle",
        "button\.ClipText = true",
        "TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
        "LauncherComponentTheme\.StateNormal",
        "LauncherComponentTheme\.StateHover",
        "LauncherComponentTheme\.StatePressed",
        "LauncherComponentTheme\.StateDisabled",
        "FontHoverColor",
        "FontPressedColor",
        "FontDisabledColor",
        "LauncherStyleBoxes\.MakeFilled",
        "LauncherStyleBoxes\.MakeOutline",
        "BorderWidthBottom = width",
        "LauncherComponentTheme\.TextMuted"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Ready.cs" `
    "configures compact ready-version summary as a readable responsive summary card" `
    @(
        "new Button",
        "TooltipText = `"Open save safety check`"",
        "MouseDefaultCursorShape = Control\.CursorShape\.PointingHand",
        "ApplyReadyVersionSummaryButtonStyle",
        "readyVersionSummaryPanel\.Pressed \+= OpenCompactCloudSafetyFromReadySummary",
        "ApplyReadyVersionSummaryButtonStyle\(readyVersionSummaryPanel, scale, compact\)",
        "CompactVersionSummaryFontSize",
        "VerticalAlignment\.Center",
        "_compactStackedActionRows\s*\?\s*TextServer\.AutowrapMode\.WordSmart",
        "readyVersionSummaryLabel\.ClipText = compact && !_compactStackedActionRows",
        "readyVersionSummaryLabel\.SetAnchorsPreset\(Control\.LayoutPreset\.FullRect\)",
        "readyVersionSummaryLabel\.OffsetLeft",
        "readyVersionSummaryLabel\.OffsetRight",
        "TextServer\.OverrunBehavior\.TrimEllipsis",
        "CompactStackedVersionSummaryHeight",
        "CompactVersionSummaryHeight"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.ReadySummary.cs" `
    "uses responsive compact ready-version copy with Save Check and Upload-locked state" `
    @(
        "CompactReadySummaryBranchLimit",
        "CompactReadyStackedSummaryBranchLimit",
        "CompactReadyVersionSummary\(\)",
        "CompactReadyVersionHelpText\(\)",
        "SelectedOptionCompactStatus",
        "Play version:",
        "Saves: Get/Upload",
        "CompactReadyFileScope",
        "Default files",
        "Separate files",
        "_compactStackedActionRows",
        "Ready:",
        "Save Check \| Upload locked",
        "no auto cloud upload"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.ReadySummary.Style.cs" `
    "keeps ready-version summary card skinning isolated from copy generation" `
    @(
        "ApplyReadyVersionSummaryButtonStyle",
        "LauncherComponentTheme\.StateNormal",
        "LauncherComponentTheme\.StateHover",
        "LauncherComponentTheme\.StatePressed",
        "LauncherComponentTheme\.StateDisabled",
        "CompactVersionSummaryRadius",
        "CompactVersionSummaryHorizontalMargin",
        "CompactVersionSummaryVerticalMargin",
        "Color body,",
        "Color border",
        "BuildReadyVersionSummaryStyle\(float scale, bool compact\)",
        "compact \? LauncherSectionMetrics\.CompactVersionSummaryRadius : 8",
        "compact \? LauncherSectionMetrics\.CompactVersionSummaryHorizontalMargin : 12",
        "compact \? LauncherSectionMetrics\.CompactVersionSummaryVerticalMargin : 9"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.CloudSafety.cs" `
    "opens compact cloud safety details from the ready-version summary" `
    @(
        "OpenCompactCloudSafetyFromReadySummary",
        "_cloudSafetyExpanded = true",
        "UpdateBranchHelpText\(\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Layout.cs" `
    "prioritizes compact ready state as summary, cloud safety actions, launch, then version management" `
    @(
        "ArrangeCompactCloudGroupPriority",
        "launchParent\?\.RemoveChild\(_launchButton\)",
        "_cloudGroup\.AddChild\(_launchButton\)",
        "MoveChildAfter\(_cloudGroup, _launchButton, _pushPullRow\)",
        "MoveChildAfter\(_cloudGroup, _cloudOptionsToggle, _launchButton\)",
        "MoveChildAfter\(_cloudGroup, _compactCloudOptionsRow, _cloudOptionsToggle\)",
        "ArrangeCompactReadyStatePriority",
        "var readyPrimaryPath = _launchButton\.GetParent\(\) == _cloudGroup",
        "MoveChild\(_readyVersionSummaryPanel, _branchDetailsToggle\.GetIndex\(\)\)",
        "MoveAfter\(_branchDetailsToggle, readyPrimaryPath\)",
        "MoveAfter\(_branchDropdown, _branchDetailsToggle\)",
        "MoveAfter\(_branchHelpLabel, _branchDropdown\)",
        "MoveCompactCloudSafetyCueBeforeCloudActions",
        "private static void MoveChildAfter\(Node parent, Node child, Node previous\)",
        "var previousIndex = previous\.GetIndex\(\)",
        "child\.GetIndex\(\) < previousIndex",
        "previousIndex \+ 1"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Branches.Text.cs" `
    "uses compact Play and Sync drawer detail labels for version controls" `
    @(
        "Version target",
        "Hide Save Check",
        "CompactCloudSafetyDetailText",
        "Keep active"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.CloudSafety.cs" `
    "uses compact Play and Sync drawer detail labels for cloud-save safety" `
    @(
        "CompactPlaySyncDrawerText",
        "Save Check",
        "Get saves first",
        "CompactCloudSafetyDetailText",
        "Saves for:",
        "Get Steam saves before upload\. Upload can overwrite Steam\."
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.CloudOptions.cs" `
    "uses compact Play and Sync drawer detail labels for save settings" `
    @(
        "CompactPlaySyncDrawerText",
        "Save settings",
        "Backup and cloud"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Support.Foundation.cs" `
    "packs compact recovery and support tools into a responsive grid that becomes full-width on narrow compact viewports" `
    @(
        "BuildSupportFoundation",
        "BuildCompactSupportToolsGrid\(scale, compact, compactStackedActionRows\)",
        "if \(compact\)",
        "supportGroup\.AddChild\(supportToolsGrid\)",
        "supportToolsParent = compact",
        "new SupportFoundation\(supportGroup, supportToolsGrid, supportToolsParent\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Support.Types.cs" `
    "uses typed support construction return values instead of long tuple signatures" `
    @(
        "private readonly struct SupportFoundation",
        "internal VBoxContainer Group",
        "internal GridContainer ToolsGrid",
        "internal Container ToolsParent",
        "private readonly struct SupportControls",
        "internal Button SupportToggle",
        "internal Button UpdateButton",
        "internal Button RefreshVersionsButton",
        "internal Button RedownloadButton",
        "internal Button ClearCachedVersionsButton",
        "internal Button DiagnosticsButton",
        "internal Button ShowLastErrorButton",
        "internal Button CopyRawLogButton"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Support.cs" `
    "orchestrates support toggle and tool button construction through focused helpers" `
    @(
        "private SupportControls BuildSupportControls",
        "BuildSupportToggle",
        "AddChild\(_supportGroup\)",
        "return new SupportControls",
        "BuildUpdateSupportButton",
        "BuildRefreshVersionsSupportButton",
        "BuildRedownloadSupportButton",
        "BuildClearCachedVersionsSupportButton",
        "BuildDiagnosticsSupportButton",
        "BuildShowLastErrorSupportButton",
        "BuildCopyRawLogSupportButton",
        "SupportToggleText\(\)",
        "ToggleSupportOptions",
        "SetCompactActionButtonText\(supportToggle, supportToggle\.Text\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Support.Tools.cs" `
    "keeps compact update version and cache tool labels in focused button builders" `
    @(
        "AddCompactSupportToolButton",
        "`"Check Files`"",
        "`"Game Versions`"",
        "`"Repair Files`"",
        "`"Free Space`"",
        "`"Updates`"",
        "`"Refresh list`"",
        "`"Rebuild game`"",
        "`"Old versions`""
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Support.DiagnosticsTools.cs" `
    "keeps compact report problem and launcher-log support labels isolated" `
    @(
        "AddCompactSupportToolButton",
        "`"Help Report`"",
        "`"Last Problem`"",
        "`"Copy Log`"",
        "`"Share details`"",
        "`"Open details`"",
        "`"Review first`"",
        "`"Create Help Report`"",
        "`"Show Last Problem`"",
        "`"Copy Launcher Log \(Review First\)`""
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Support.cs" `
    "keeps compact support drawer grid and support toggle copy responsive" `
    @(
        "GridContainer",
        "Columns = compactStackedActionRows \? 1 : 2",
        "`"Fixes & Help`"",
        "`"Hide Fixes`"",
        "`"Repair tools`"",
        "`"Back to play`""
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Buttons.cs" `
    "keeps base ActionSection hidden button factories and push/pull buttons isolated" `
    @(
        "AddPrimaryHiddenButton",
        "AddSecondaryHiddenButton",
        "AddHiddenButton",
        "new StyledButton",
        "button\.Visible = false",
        "button\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill",
        "AddPushPullButton",
        "LauncherSectionMetrics\.SecondaryButtonHeight"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.CompactActionButton.cs" `
    "uses readable fill-width buttons inside the compact support grid" `
    @(
        "AddCompactSupportToolButton",
        "SetCompactActionButtonText",
        "CompactButtonDetailLabels\.Apply",
        "CompactButtonDetailLabelSpec",
        "CompactActionButtonLabels",
        "CompactActionButtonBodyName",
        "CompactActionButtonTitleName",
        "CompactActionButtonDetailName",
        "CompactButtonDetailLabelSpec\.Default",
        "_compact",
        "CompactSupportToolHeight",
        "CompactSupportToolFontSize",
        "CompactSupportToolText"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Branches.Text.cs" `
    "keeps dynamic compact version and safety drawer labels synced with structured title/detail button labels" `
    @(
        "SetCompactActionButtonText\(_branchDetailsToggle",
        "SetCompactActionButtonText\(_cloudSafetyToggle"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.CloudOptions.cs" `
    "keeps dynamic compact save-settings drawer label synced with structured title/detail button labels" `
    @(
        "SetCompactActionButtonText\(_cloudOptionsToggle",
        "Backup \{OnOff\(_localBackupEnabled\)\} / Cloud \{OnOff\(_cloudSyncEnabled\)\}"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.cs" `
    "keeps update button text routed through structured compact button labels" `
    @(
        "SetCompactActionButtonText\(_updateButton, text\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Toggles.cs" `
    "keeps compact Save Backup and Cloud Sync option toggles synced with structured title/detail button labels" `
    @(
        "SetCompactActionButtonText\(button, text\)",
        "CompactCloudOptionText",
        "Local safety",
        "Steam saves"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Primary.cs" `
    "keeps compact safe start as a support-grid action with Cloud-off detail" `
    @(
        "AddCompactSupportToolButton",
        "supportToolsParent",
        "`"Safe Start`"",
        "`"Cloud off`""
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.cs" `
    "keeps cloud construction as typed orchestration rather than one mixed UI method" `
    @(
        "private readonly record struct CloudControls",
        "private readonly record struct CloudPrimaryActionControls",
        "private readonly record struct CloudSafetyControls",
        "private readonly record struct CloudOptionControls",
        "BuildCloudPrimaryActionControls\(cloudGroup, scale, compact\)",
        "BuildCloudSafetyControls\(cloudGroup, scale, compact\)",
        "BuildCloudOptionControls\(cloudGroup, scale, compact\)",
        "return new CloudControls"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.PrimaryActions.cs" `
    "uses explicit compact cloud direction labels while keeping upload locked" `
    @(
        "CompactCloudPullText\(\)",
        "CompactCloudPushToggleText\(expanded: false\)",
        "CompactCloudPushDangerText\(\)",
        "CompactCloudPushConfirmText\(\)",
        "SetCompactActionButtonText\(pullButton, pullButton\.Text\)",
        "SetCompactActionButtonText\(pushButton, pushButton\.Text\)",
        "SetCompactActionButtonText\(confirmPushButton, confirmPushButton\.Text\)",
        "BuildCloudPushConfirmationLabel\(scale, compact\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.PushConfirmation.cs" `
    "keeps compact cloud Push confirmation warning bounded and overwrite-explicit" `
    @(
        "BuildCloudPushConfirmationLabel",
        "CompactCloudPushWarningText\(\)",
        "Confirming Push uploads Android saves to Steam Cloud",
        "can overwrite remote Steam Cloud saves",
        "CompactCloudPushWarningFontSize",
        "CompactCloudPushWarningHeight",
        "pushConfirmationLabel\.ClipText = compact",
        "pushConfirmationLabel\.TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
        "pushConfirmationLabel\.CustomMinimumSize = new Vector2",
        "LauncherComponentTheme\.OrangeHot",
        "pushConfirmationLabel\.Visible = false"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.CloudSafety.cs" `
    "defines compact Pull label as an explicit title/detail Android download action" `
    @(
        "CompactCloudPullText",
        "Get Steam Saves",
        "Download to Android"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.CloudSafety.cs" `
    "defines compact dangerous Push labels as explicit title/detail actions" `
    @(
        "CompactCloudPushDangerText",
        "Upload to Steam",
        "Overwrite cloud",
        "CompactCloudPushConfirmText",
        "Confirm Upload",
        "Overwrite cloud",
        "CompactCloudPushWarningText",
        "Steam Cloud overwrite",
        "Confirm only after Pull/local saves are verified"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.CloudPush.cs" `
    "keeps compact Push relock label direction-aware and structured after reset" `
    @(
        "CompactCloudPushToggleText",
        "SetCompactActionButtonText\(_cloudPushToggle, _compact"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.ConstructionHelpers.cs" `
    "packs compact Get Steam Saves and locked Steam upload into responsive action rows" `
    @(
        "BuildCompactCloudPrimaryActionsRow",
        "compactStackedActionRows",
        "new VBoxContainer\(\) : new HBoxContainer\(\)",
        "CompactCloudPrimaryActionSeparation",
        "parent\.AddChild\(row\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.PrimaryActions.cs" `
    "preserves get-saves-first order when wiring compact cloud actions" `
    @(
        "cloudPrimaryActionsParent = compact",
        "BuildCompactCloudPrimaryActionsRow\(pushPullRow, scale, _compactStackedActionRows\)",
        "CompactCloudPullText\(\)",
        "CompactCloudPushToggleText\(expanded: false\)",
        "CompactCloudPushDangerText\(\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPortalStatusFormatter.cs" `
    "declares status formatter UX support flags" `
    @(
        "internal static partial class LauncherPortalStatusFormatter",
        "PhaseLabelStatusSupported\s*=\s*true",
        "StructuredStatusChipSupported\s*=\s*true",
        "GuidedNextActionStatusSupported\s*=\s*true",
        "ErrorFirstGuidedStatusSupported\s*=\s*true",
        "CompactPlainLanguageStatusCopySupported\s*=\s*true",
        "CompactShortStatusDetailsSupported\s*=\s*true"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPortalStatusFormatter.Message.cs" `
    "formats compact launcher status text as plain-language user copy" `
    @(
        "MessageFor",
        "CompactMessageFor",
        "CompactMessageMaxChars = 86",
        "ShortenCompactMessage",
        "Waiting for launcher state",
        "Sign in with Steam to continue",
        "Signing in to Steam",
        "Checking game ownership",
        "Download this game version to play",
        "Ready to play this version",
        "Signed in\. Checking game files",
        "Get Steam saves before uploading",
        "Upload blocked\. Check save safety first",
        "Runtime files need repair\. Redownload this version",
        "Last launch failed\. Open details or try Safe Start"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPortalStatusFormatter.Action.cs" `
    "formats launcher status next-action labels" `
    @(
        "ActionFor",
        "Fix Required",
        "Verify Code",
        "Install Game",
        "Start Game",
        "Choose Version",
        "Sync Saves",
        "Review Details",
        "Next Step"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPortalStatusFormatter.Phase.cs" `
    "classifies launcher status into clear phase labels" `
    @(
        "PhaseFor",
        "Attention",
        "Steam",
        "Version",
        "Install",
        "Cloud",
        "Ready",
        "Details",
        "Status"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPortalStatusFormatter.Color.cs" `
    "maps launcher status phases to portal colors" `
    @(
        "ColorFor",
        "LauncherComponentTheme\.OrangeHot",
        "LauncherComponentTheme\.CyanAccent",
        "LauncherComponentTheme\.OrangeAccent",
        "new Color\(0\.36f, 0\.9f, 0\.42f\)",
        "LauncherComponentTheme\.TextSecondary"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPortalStatusFormatter.Predicates.cs" `
    "centralizes launcher status text predicates" `
    @(
        "ContainsAny",
        "StringComparison\.OrdinalIgnoreCase",
        "ContainsFailure",
        "Could not",
        "IsDownloadRequiredStatus",
        "IsReadyStatus",
        "Runtime pairing is verified",
        "Active install slot"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPortalUxSupport.Status.cs" `
    "declares status, safe-flow, and compact layout portal UX support flags" `
    @(
        "StatusLedPortalSupported\s*=\s*true",
        "PhaseLabelStatusSupported",
        "StructuredStatusChipSupported",
        "GuidedNextActionStatusSupported",
        "ErrorFirstGuidedStatusSupported",
        "CompactPlainLanguageStatusCopySupported",
        "TitledStateSectionsSupported\s*=\s*true",
        "SafeFirstRunGuidanceSupported\s*=\s*true",
        "CompactSafeFlowCollapsibleSupported\s*=\s*true",
        "CompactLowProfileSafeFlowToggleSupported\s*=\s*true",
        "CompactSafeFlowToggleDetailLabelsSupported\s*=\s*true",
        "CompactStructuredSafeFlowToggleLabelsSupported\s*=\s*true",
        "CompactSafeFlowBoundedGuideSupported\s*=\s*true",
        "CompactPlainLanguageQuickStartLabelsSupported\s*=\s*true",
        "MobileFirstCompactLayoutSupported\s*=\s*true",
        "CompactDensePanelPaddingSupported\s*=\s*true",
        "CompactDenseVerticalRhythmSupported\s*=\s*true",
        "RoundedScaledLauncherMetricsSupported\s*=\s*true",
        "AndroidCompactTouchScaleFloorSupported\s*=\s*true",
        "AndroidReadableWarmupScreenSupported\s*=\s*true",
        "AndroidReadableStartupStatusCardSupported\s*=\s*true",
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
        "CompactShortStatusDetailsSupported",
        "CompactStatusTapToExpandDetailsSupported\s*=\s*true",
        "CompactTouchSafeStatusDetailButtonSupported\s*=\s*true",
        "CompactStatusDetailCueSupported\s*=\s*true"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPortalUxSupport.Workflow.cs" `
    "declares compact workflow and sticky task-header portal UX support flags" `
    @(
        "CompactWorkflowStepStripSupported\s*=\s*true",
        "CompactTwoColumnWorkflowStripSupported\s*=\s*false",
        "CompactSingleRowNumberedWorkflowStripSupported\s*=\s*true",
        "CompactNarrowWorkflowSingleRowSupported\s*=\s*true",
        "CompactVisibleWorkflowStepLabelsSupported\s*=\s*true",
        "CompactWorkflowStepDetailLabelsSupported\s*=\s*true",
        "CompactWorkflowStepNumberBadgesSupported\s*=\s*true",
        "CompactReadableWorkflowStepNumberBadgesSupported\s*=\s*true",
        "CompactWorkflowUnifiedTouchHeightSupported\s*=\s*true",
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
        "CompactReadableDetailLabelFontSupported\s*=\s*true"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPortalUxSupport.AuthChrome.cs" `
    "declares compact auth, confirmation, branding, and section chrome support flags" `
    @(
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
        "CompactExplicitSectionHeaderCuesSupported\s*=\s*true"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPortalUxSupport.InstallCloud.cs" `
    "declares install, ready-state, cloud-safety, drawer, and scroll support flags" `
    @(
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
        "CompactSelectedVersionSummaryShortcutSupported\s*=\s*true",
        "CompactSelectedVersionHeadlineSupported\s*=\s*true",
        "CompactResponsiveSelectedVersionSummarySupported\s*=\s*true",
        "CompactStructuredInstallVersionActionLabelsSupported\s*=\s*true",
        "CompactReadyVersionSummarySupported\s*=\s*true",
        "CompactReadyVersionSummaryPanelSupported\s*=\s*true",
        "CompactReadyVersionSummaryShortcutSupported\s*=\s*true",
        "CompactReadyVersionSummaryHeadlineSupported\s*=\s*true",
        "CompactResponsiveReadyVersionSummarySupported\s*=\s*true",
        "CompactReadyStatePrioritySupported\s*=\s*true",
        "CompactReadyStateCloudOptionsBelowLaunchSupported\s*=\s*true",
        "CompactPlaySyncDrawerDetailLabelsSupported\s*=\s*true",
        "CompactStructuredPlaySyncActionLabelsSupported\s*=\s*true",
        "CompactPlainLanguagePlaySyncLabelsSupported\s*=\s*true",
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
        "CompactActiveTaskSafeFlowSuppressionSupported\s*=\s*true"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPortalUxSupport.Diagnostics.cs" `
    "declares diagnostics, fallback recovery, startup recovery, and validation-boundary support flags" `
    @(
        "NativeFallbackRecoveryActionsStyledSupported\s*=\s*true",
        "NativeFallbackDiagnosticsCollapsedSupported\s*=\s*true",
        "NativeFallbackResponsiveRecoveryRowsSupported\s*=\s*true",
        "StartupRecoveryScrollSafeControlsSupported\s*=\s*true",
        "StartupRecoveryStructuredCompactActionsSupported\s*=\s*true",
        "VersionInstallCloudSeparationGuidanceSupported\s*=\s*true",
        "DiagnosticsConsoleHiddenByDefault\s*=\s*true",
        "DiagnosticsConsoleAutoOpensForDiagnosticsActionsSupported\s*=\s*true",
        "CompactLowProfileDiagnosticsToggleSupported\s*=\s*true",
        "CompactDiagnosticsToggleDetailLabelsSupported\s*=\s*true",
        "CompactStructuredDiagnosticsToggleLabelsSupported\s*=\s*true",
        "PlainLanguageHelpReportCopySupported\s*=\s*true",
        "CompactDiagnosticsScrollHostedSupported\s*=\s*true",
        "CompactReadableDiagnosticsLogSupported\s*=\s*true",
        "CompactBoundedDiagnosticsLogViewportSupported\s*=\s*true",
        "ViewportAwareDiagnosticsLogResizeSupported\s*=\s*true",
        "StartupFallbackRawBannerSuppressed\s*=\s*true",
        "PortalUxDeviceValidated\s*=\s*false"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPortalUxSupport.cs" `
    "keeps secret-safe portal UX narrative and validation boundary text" `
    @(
        "Status-led launcher portal",
        "compact plain-language status copy for sign-in, ownership check, install-needed, and ready-to-play states",
        "Steam sign-in",
        "Steam Guard",
        "game install",
        "play/sync",
        "low-profile status card",
        "stacked phase and next-action labels",
        "stable one-line non-attention detail row",
        "compact touch-safe responsive numbered workflow step strip with visible two-line step labels, separate readable number badges, and a unified 62px touch height",
        "stays one dense row even on narrow compact viewports",
        "tappable compact workflow steps that directly navigate to visible or fallback task sections",
        "subdued inline current-task jump button and two-line workflow cells that share the 62px compact touch height in a low-profile toolbar shell around the touch-safe sticky compact task header",
        "reflows between inline and stacked layouts after Android rotation or keyboard viewport changes",
        "compact current-task jump button",
        "touch-safe sticky compact task bar",
        "structured two-line app-like task title/detail labels",
        "contextual task detail labels",
        "compact touch-safe confirmation dialogs with wider scroll-safe warning text, contextual confirm/cancel labels, and current-viewport sizing after rotation or keyboard viewport changes",
        "compact Android sign-in primary action before helper copy",
        "compact Android sign-in CTA promoted to a large primary action with Sign in with Steam / Android login detail labels before the readable two-line password-manager safety helper",
        "larger compact Steam Guard code entry and Verify Code controls before helper copy, inline where width allows and stacked full-width on narrow compact viewports",
        "compact Steam Guard submit detail labels that keep Verify Code / Submit once action-first",
        "compact Steam Guard bounded two-line helper labels that keep one-shot/no-storage and newest-code retry guidance below the action row",
        "compact failure recovery promoted to a primary Try Again / Restart task action while cloud controls remain hidden",
        "compact completed-auth section suppression",
        "consistent Start Game primary CTA with compact Start Game / Ready version detail labels",
        "Android compact touch-scale floor for small-device readability",
        "Android-readable shader warmup/loading screen",
        "Android-readable startup status card",
        "verbose native fallback diagnostics collapsed until requested",
        "responsive recovery rows on narrow landscape screens",
        "bounded compact quick-start guide panel",
        "collapsible compact quick-start guidance with structured Quick Start / Get saves first title/detail labels",
        "compact active-task safe-flow suppression after setup",
        "compact install step ordering with selected-version summary and a large Download Version / Local files only primary action before optional version details",
        "compact install primary action detail labels for download, redownload, retry, and disabled downloading states",
        "compact install-version drawer controls with structured title/detail labels that keep the version dropdown and refresh action inline where width allows and stack on narrow compact viewports",
        "collapsible compact version details with structured version-file drawer labels and bounded two-line Files for / Play version helper labels",
        "compact download progress promoted directly under the disabled Downloading primary action with a stable two-line Downloading selected version status label",
        "compact readable selected-version and ready-version summary cards",
        "compact responsive selected-version summary that stays explicit about Cloud unchanged/local-file scope and opens the version drawer from a touch-safe Change shortcut",
        "compact ready-version summary",
        "compact ready-version summary panel",
        "compact responsive ready-version summary with Save Check shortcut opens Save Check from a touch-safe Upload-locked cue without unlocking Push",
        "CompactReadyStatePriorityDescription",
        "compact ready-state priority that keeps the ready summary, Save Check shortcut, and Get-saves-first cloud controls before Start Game while moving version management below the primary launch path",
        "CompactReadyStateCloudOptionsBelowLaunchDescription",
        "compact ready-state cloud options stay below Start Game as an optional save-settings drawer after Get-saves-first cloud controls",
        "compact Play/Sync drawer detail labels rendered as structured title/detail action labels",
        "CompactPlainLanguagePlaySyncLabelsDescription",
        "compact Play/Sync uses plain-language save copy such as Get Steam Saves, Upload Locked, Save Check, and Fixes & Help while keeping upload collapsed and explicit",
        "compact user-facing support tool labels such as Safe Start / Cloud off, Check Files / Updates, Game Versions / Refresh list, Repair Files / Rebuild game, Free Space / Old versions, Help Report / Share details, Last Problem / Open details, and Copy Log / Review first",
        "compact responsive action rows that keep save get/upload actions, save settings, and support tools side-by-side where space allows and stack them into full-width controls on narrow compact viewports",
        "compact ready-state install-section suppression",
        "touch-safe compact version dropdowns with larger opened popup row spacing/padding",
        "collapsible compact Save Check / Get saves first cloud-safety drawer labels shown before get/upload actions with a stable two-line Saves for / Get Steam saves before upload / Upload can overwrite Steam detail label",
        "compact cloud action labels that name Android as the Pull destination and Steam as the Push destination",
        "compact Pull detail labels that keep Get Steam Saves / Download to Android direction-explicit",
        "compact locked Push toggle labels that stay structured as Upload Locked / Review first and Hide Upload / Keep locked",
        "compact dangerous Push detail labels that keep Upload to Steam / Overwrite cloud and Confirm Upload / Overwrite cloud direction-explicit after unlock",
        "compact armed Push overwrite warning rendered as a stable Steam Cloud overwrite / Confirm only after Pull/local saves are verified label",
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
        "compact scroll-hosted Help & Reports drawer with structured touch-safe drawer title/detail labels, plain-language help-report and launcher-log status copy, a readable bounded compact diagnostics log viewport, and viewport-aware diagnostics log resizing",
        "compact dense vertical rhythm with single-row section headers and explicit short task cues such as Steam account, Current code, Local files, and Play safely",
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
    "src\STS2Mobile\Launcher\LauncherPortalUxSupport.Features.cs" `
    "builds launcher UI hardening diagnostics feature registry from category-owned reports" `
    @(
        "FeatureReports => BuildFeatureReports",
        "new List<LauncherPortalUxFeature>",
        "AddStatusFeatureReports\(features\)",
        "AddWorkflowFeatureReports\(features\)",
        "AddAuthChromeFeatureReports\(features\)",
        "AddInstallCloudFeatureReports\(features\)",
        "AddDiagnosticsFeatureReports\(features\)",
        "return features\.ToArray\(\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPortalUxSupport.Features.Status.cs" `
    "keeps status and safe-flow feature diagnostics beside status support flags" `
    @(
        "AddStatusFeatureReports",
        "status-led portal supported",
        "CompactStackedStatusHeaderSupported",
        "ViewportAwareCompactStatusHeadlineReflowSupported",
        "CompactShortStatusDetailsSupported",
        "CompactTouchSafeStatusDetailButtonSupported",
        "CompactStatusDetailCueSupported"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPortalUxSupport.Features.Workflow.cs" `
    "keeps workflow and current-task feature diagnostics beside workflow support flags" `
    @(
        "AddWorkflowFeatureReports",
        "CompactWorkflowStepStripSupported",
        "CompactWorkflowStepNumberBadgesSupported",
        "CompactCurrentTaskJumpSupported",
        "CompactStickyTaskToolbarShellSupported",
        "ViewportAwareCompactTaskReanchorSupported",
        "CompactReadableDetailLabelFontSupported"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPortalUxSupport.Features.AuthChrome.cs" `
    "keeps sign-in, action, and chrome feature diagnostics beside auth/chrome support flags" `
    @(
        "AddAuthChromeFeatureReports",
        "CompactSteamGuardLargeInputSupported",
        "ViewportAwareCompactSteamGuardActionRowReflowSupported",
        "CompactAndroidLoginPrimaryCtaSupported",
        "ConsistentStartGameCtaSupported",
        "BrandedAtmosphericBackgroundSupported",
        "CompactSingleRowSectionHeadersSupported",
        "CompactExplicitSectionHeaderCuesSupported"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPortalUxSupport.Features.InstallCloud.cs" `
    "keeps install, launch, and cloud feature diagnostics beside install/cloud support flags" `
    @(
        "AddInstallCloudFeatureReports",
        "CompactInstallPrimaryActionFirstSupported",
        "CompactSelectedVersionSummarySupported",
        "CompactReadyVersionSummarySupported",
        "CompactPlainLanguagePlaySyncLabelsSupported",
        "CompactCloudPushDangerDetailLabelsSupported",
        "CompactSupportToolsGridSupported",
        "CompactRawLogReviewLabelSupported",
        "ViewportAwareKeyboardOffsetSupported",
        "CompactActiveTaskSafeFlowSuppressionSupported"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherPortalUxSupport.Features.Diagnostics.cs" `
    "keeps fallback, diagnostics, and validation feature diagnostics beside diagnostics support flags" `
    @(
        "AddDiagnosticsFeatureReports",
        "NativeFallbackRecoveryActionsStyledSupported",
        "StartupRecoveryScrollSafeControlsSupported",
        "VersionInstallCloudSeparationGuidanceSupported",
        "DiagnosticsConsoleHiddenByDefault",
        "PlainLanguageHelpReportCopySupported",
        "ViewportAwareDiagnosticsLogResizeSupported",
        "PortalUxDeviceValidated"
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
        "ReanchorCompactScrollTargetAfterViewportChange\(\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Behavior.Keyboard.cs" `
    "updates Android keyboard offset and panel position from the visible viewport" `
    @(
        "UpdateKeyboardOffset\(\)",
        "DisplayServer\.VirtualKeyboardGetHeight\(\)",
        "_keyboardOffset = Math\.Min",
        "_panelBaseY - _keyboardOffset",
        "_keyboardOffset = 0f",
        "_panelBaseY"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherView.Behavior.Keyboard.cs" `
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
    "src\STS2Mobile\ModEntry.StandaloneLauncher.cs" `
    "suppresses raw startup fallback banner behind the launcher portal" `
    @(
        "Startup fallback raw banner suppressed",
        "launcher diagnostics retain the startup failure detail"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Visibility.cs" `
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
    "src\STS2Mobile\Launcher\Sections\ActionSection.Visibility.SecondaryState.cs" `
    "models ready, retry, and hidden secondary button visibility presets" `
    @(
        "SecondaryButtonVisibility",
        "LaunchReady\(bool showUpdate\)",
        "Retry\(\)",
        "Hidden\(\)",
        "redownload: true",
        "support: true",
        "safeLaunch: true",
        "launch: true"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Visibility.Secondary.cs" `
    "applies secondary ready/retry/hidden button visibility to the action section" `
    @(
        "ShowLaunchButtons",
        "ShowRetryButtons",
        "HideSecondaryButtons",
        "SetSecondaryButtonsVisible",
        "ShowUpdateButton\(visibility\.Update\)",
        "_redownloadButton\.Visible = visibility\.Redownload",
        "_branchControlsAvailable = visibility\.Branch",
        "ApplyBranchControlVisibility",
        "SetSupportButtonsVisible\(visibility\.Support\)",
        "_readyVersionSummaryPanel\.Visible = _compact && visibility\.Launch"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Visibility.Support.cs" `
    "keeps compact update and support-tool visibility together" `
    @(
        "ShowUpdateButton",
        "CompactSupportToolText\(""Check Files"", ""Updates""\)",
        "Check for Updates",
        "SetSupportButtonsVisible",
        "_supportExpanded = false",
        "_supportGroup\.Visible = false",
        "SupportToggleText\(\)",
        "_diagnosticsButton\.Visible = visible",
        "_refreshVersionsButton\.Visible = visible",
        "_clearCachedVersionsButton\.Visible = visible",
        "_showLastErrorButton\.Visible = visible",
        "_copyRawLogButton\.Visible = visible"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.PrimaryActions.cs" `
    "uses explicit Steam Cloud direction and overwrite-risk wording in the portal" `
    @(
        "pushPullRow",
        "pushButton",
        "confirmPushButton",
        "pushConfirmationLabel",
        "Push Locked",
        "CompactCloudPushDangerText\(\)",
        "CompactCloudPushConfirmText\(\)",
        "Pull Saves from Steam Cloud",
        "BuildCloudPushConfirmationLabel\(scale, compact\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.CloudPush.cs" `
    "keeps Steam Cloud Push behind an explicit arm and confirm flow" `
    @(
        "CloudPushArmRequested",
        "CloudPushArmRequested\?\.Invoke\(\) == false",
        "ArmCloudPush",
        "ConfirmCloudPush",
        "ResetCloudPushArm"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Branches.Text.cs" `
    "uses explicit Steam Cloud direction wording in ready/action state" `
    @(
        "_readyVersionSummaryLabel",
        "Ready version:",
        "Start Game and Pull/Push use this version",
        "Push stays locked until explicitly opened",
        "SteamGameInstallPaths\.VersionSlotKind",
        "Pull copies Steam Cloud saves to Android",
        "Push copies Android saves to Steam Cloud",
        "can overwrite remote saves",
        "Version/download actions affect local game files only",
        "Steam Cloud saves move only through Pull/Push"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Visibility.Cloud.cs" `
    "collapses compact cloud drawers when cloud controls hide" `
    @(
        "SetCloudControlsVisible",
        "_cloudGroup\.Visible = visible",
        "ApplyCloudOptionVisibility\(visible\)",
        "_pushPullRow\.Visible = visible",
        "_cloudSafetyExpanded = false",
        "_cloudPushExpanded = false",
        "ResetCloudPushArm\(visible\)",
        "UpdateBranchHelpText",
        "SetPushPullDisabled",
        "ResetCloudPushArm\(_pushPullRow\.Visible\)",
        "_pushButton\.Disabled = disabled",
        "_cloudPushToggle\.Disabled = disabled",
        "_confirmPushButton\.Disabled = disabled",
        "_pullButton\.Disabled = disabled"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.CloudOptions.cs" `
    "collapses compact cloud option drawer when cloud controls hide" `
    @(
        "_cloudOptionsExpanded = false"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Toggles.cs" `
    "uses compact cloud option labels that fit phone touch targets" `
    @(
        "_compact",
        "CompactCloudOptionText",
        "Save Backup",
        "Local safety",
        "Cloud Sync",
        "Steam saves",
        "Local Backup: \{OnOff\(value\)\}",
        "Game Cloud Sync: \{OnOff\(value\)\}"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.ConstructionHelpers.cs" `
    "packs compact Save Backup and Cloud Sync options into responsive action rows" `
    @(
        "BuildCompactCloudOptionsRow",
        "compactStackedActionRows",
        "CompactCloudOptionToggleSeparation",
        "Visible = false",
        "new VBoxContainer\(\) : new HBoxContainer\(\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.Options.cs" `
    "wires compact Save Backup and Cloud Sync options into the cloud group" `
    @(
        "compactCloudOptionsRow = BuildCompactCloudOptionsRow",
        "_compactStackedActionRows",
        "AddCompactSupportToolButton\(cloudOptionsParent, ""Save Backup Off""",
        "AddCompactSupportToolButton\(cloudOptionsParent, ""Cloud Sync Off"""
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.PrimaryActions.cs" `
    "keeps Pull and Push cloud actions beside the confirmation warning" `
    @(
        "cloudGroup.AddChild\(pushPullRow\)",
        "Pull Saves from Steam Cloud",
        "Push Locked",
        "CompactCloudPushConfirmText\(\)",
        "BuildCloudPushConfirmationLabel\(scale, compact\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Cloud.Safety.cs" `
    "shows cloud-save safety guidance beside Pull/Push actions" `
    @(
        "cloudSafetyLabel",
        "OrangeHot",
        "CompactCloudSafetyDetailHeight",
        "TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
        "cloudGroup.AddChild\(cloudSafetyLabel\)",
        "CompactCloudSafetySummary\(\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Layout.cs" `
    "moves compact cloud-safety cue before Pull/Push controls" `
    @(
        "MoveCompactCloudSafetyCueBeforeCloudActions",
        "_cloudGroup\.MoveChild\(_cloudSafetyToggle, 0\)",
        "MoveChildAfter\(_cloudGroup, _cloudSafetyLabel, _cloudSafetyToggle\)",
        "MoveChildAfter\(_cloudGroup, _pushPullRow, _cloudSafetyLabel\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\CodeSection.cs" `
    "wires Steam Guard code input to alphanumeric normalization and one-shot submit" `
    @(
        "TextChanged \+= NormalizeCodeText",
        "TextSubmitted \+= _ => OnSubmit\(\)",
        "submitButton\.Pressed \+= OnSubmit",
        "CodeSubmitted"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\CodeSection.Submission.cs" `
    "normalizes Steam Guard codes to uppercase alphanumeric text before submitting once" `
    @(
        "NormalizeCodeText",
        "NormalizeCode\(string text\)",
        "char\.IsLetterOrDigit",
        "char\.ToUpperInvariant",
        "CodeSubmitted\?\.Invoke\(code\)"
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
        "Sign in with Steam",
        "`"Next`"",
        "button\.setAllCaps\(false\)",
        "focusSteamLoginPasswordField",
        "cancelSteamLoginCredentialAutofillSession",
        "autofillManager\.cancel\(\)",
        "Show password",
        "Hide password",
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
    "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.Text.cs" `
    "labels startup recovery launcher-log copy as review-before-sharing" `
    @(
        "If startup stalls, restart the app, try Safe Start, or create a help report",
        "Review logs before sharing"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.Buttons.cs" `
    "labels startup recovery launcher-log button as review-before-sharing" `
    @(
        "Copy Launcher Log \(Review First\)",
        "Review first"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.CompactButton.cs" `
    "uses structured compact startup recovery button labels instead of raw debug labels" `
    @(
        "CompactRecoveryButtonBody",
        "CompactRecoveryButtonTitle",
        "CompactRecoveryButtonDetail",
        "AddCompactRecoveryButtonLabels",
        "CompactButtonDetailLabels\.Apply",
        "CompactButtonDetailLabelSpec",
        "CompactRecoveryButtonLabels",
        '\$"\{titleText\}\\n\{detailText\}"'
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.Buttons.cs" `
    "uses compact startup recovery action copy instead of raw debug labels" `
    @(
        "Restart App",
        "Open launcher",
        "Safe Start",
        "Cloud off",
        "Help Report",
        "Share details",
        "Copy Log",
        "Review first",
        "Hide Help",
        "Keep waiting"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.Layout.cs" `
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
        "SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.Construction.cs" `
    "wires startup recovery scroll hierarchy in order" `
    @(
        "CreateScrollContainer",
        "CreateFrame",
        "CreateContainer",
        "scroll\.AddChild\(frame\)",
        "frame\.AddChild\(box\)"
    )

Add-Check `
    "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Support.DiagnosticsTools.cs" `
    "labels launcher support log copy as review-before-sharing" `
    @(
        "Copy Launcher Log \(Review First\)",
        "Create Help Report",
        "Show Last Problem"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherController.Diagnostics.cs" `
    "warns before launcher logs are copied for sharing" `
    @(
        "Public sharing warning",
        "review and redact this launcher log before posting publicly",
        "Review/redact before public posting",
        "Launcher log copied"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherStartupRecoveryControlPanel.Reports.cs" `
    "warns after startup recovery launcher logs are copied" `
    @(
        "Launcher log copied to clipboard",
        "Review/redact before public posting"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.Reports.cs" `
    "keeps the diagnostics report shell and public-sharing warning" `
    @(
        "Public sharing warning",
        "review and redact this diagnostics report before posting publicly",
        "AppendLauncherPreferences",
        "AppendFullReportDiagnostics"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportLauncherPreferences.cs" `
    "reports selected branch, credential, portal UX, and selected branch marker state" `
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
        "AppendLauncherPortalUxFeatureReports",
        "Launcher portal UX implementation note",
        "Launcher portal UX validation boundary",
        "SteamKit debug logs opt-in enabled",
        "SteamKit debug logs sanitized for credentials/tokens",
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
        "AppendBranchAvailability",
        "AppendBranchSwitchSafety",
        "AppendCachedGameVersions"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchAvailability.cs" `
    "reports selected Steam branch availability state" `
    @(
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
        "Steam branch availability visible branches"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchAvailability.Marker.cs" `
    "parses selected Steam branch availability marker state" `
    @(
        "ReadBranchAvailabilityMarkerValue",
        "ReadBranchAvailabilityMarkerValues",
        "BranchAvailabilityMarkerMatchesSelectedBranch",
        "BranchAvailabilitySelectedBranchDownloadable",
        "BranchAvailabilitySelectedBranchProblem",
        "BranchAvailabilitySelectedBranchManifestCount",
        "BranchAvailabilitySelectedBranchPasswordProtected",
        "BranchAvailabilityMarkerValueMatchesBranch",
        "LauncherMarkerFile\.ReadJoinedValues",
        "LauncherMarkerFile\.ReadValues",
        "passwordRequired=true",
        "selected branch is password-protected"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportCachedGameVersions.cs" `
    "reports cached game-version cleanup and redownload evidence" `
    @(
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
        "branchMarkerExpectedInstallSlotKind",
        "branchMarkerExpectedInstallSlotDirectory",
        "branchMarkerMatchingInstallSlotProvenance",
        "branchMarkerDepotsMatchingPublic",
        "branchMarkerDepotsDifferingFromPublic",
        "branchMarkerDepotsInheritedFromPublic",
        "branchMarkerDepotsMissingSelectedManifest",
        "branchMarkerReady",
        "Cached non-public game versions"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchSwitchSafety.cs" `
    "orchestrates branch-switch cloud-sync and backup safety diagnostics" `
    @(
        "AppendBranchSwitchSafety",
        "selectedBranch = LauncherPreferences\.ReadGameBranch\(\)",
        "importantSaveEvidenceCount",
        "AppendBranchSwitchMarkerEvidence",
        "AppendManualPullEvidence",
        "AppendCurrentLocalSaveEvidence",
        "AppendSaveOriginEvidence",
        "AppendManualPushEvidence",
        "AppendManualPushBlockedEvidence",
        "AppendBranchSwitchBackupEvidence"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchSwitchSafety.Marker.cs" `
    "reports branch-switch marker safety state" `
    @(
        "AppendBranchSwitchMarkerEvidence",
        "Branch switch marker filename",
        "Branch switch marker path",
        "Branch switch marker present",
        "Branch switch marker UTC",
        "Branch switch marker UTC parseable",
        "Branch switch previous branch",
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
        "Push requires backup storage after branch switch"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchSwitchSafety.Pull.cs" `
    "reports Manual Pull evidence for branch-switch Push safety" `
    @(
        "AppendManualPullEvidence",
        "Manual Pull evidence marker filename",
        "Manual Pull evidence marker path",
        "Manual Pull evidence marker present",
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
        "Manual Pull completed after branch switch for selected version"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchSwitchSafety.SaveOrigin.cs" `
    "reports Android save-origin and current local-save evidence for branch-switch Push safety" `
    @(
        "AppendCurrentLocalSaveEvidence",
        "Current important Android local save evidence count",
        "Current important Android local save evidence present",
        "AppendSaveOriginEvidence",
        "Android save-origin marker filename",
        "Android save-origin marker path",
        "Android save-origin marker present",
        "Android save-origin selected runtime slot ID",
        "Android save-origin selected PCK SHA256",
        "Android save-origin selected source sts2\.dll SHA256",
        "Android save-origin selected runtime playable at origin",
        "Android save-origin matches selected branch",
        "Android save-origin selected runtime slot ID matches current runtime",
        "Android save-origin current selected runtime is playable",
        "Android local saves verified for selected branch",
        "Android local saves verified for selected runtime",
        "Baseline manual Push prerequisites satisfied"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchSwitchSafety.Push.cs" `
    "reports Manual Push evidence for branch-switch Push safety" `
    @(
        "AppendManualPushEvidence",
        "Manual Push evidence marker filename",
        "Manual Push evidence marker path",
        "Manual Push evidence marker present",
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
        "LatestManualPushEvidenceOutcome"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchSwitchSafety.BlockedPush.cs" `
    "reports blocked Manual Push evidence for branch-switch Push safety" `
    @(
        "AppendManualPushBlockedEvidence",
        "Manual Push blocked evidence marker filename",
        "Manual Push blocked evidence marker path",
        "Manual Push blocked evidence marker present",
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
        "Manual Push blocked before upload evidence recorded"
    )

Add-Check `
    "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchSwitchSafety.Backup.cs" `
    "reports backup storage and pre-Push backup evidence for branch-switch safety" `
    @(
        "AppendBranchSwitchBackupEvidence",
        "Important Android local save evidence count in bounded scan",
        "Important Android local save evidence present",
        "Backup storage permission available",
        "Backup storage directory",
        "Backup storage directory exists",
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
        "android-shell-utils\.ps1",
        "evidence-path-utils\.ps1",
        "evidence-redaction-utils\.ps1",
        "AdbPath",
        "Resolve-EvidenceRepoPath",
        "ConvertTo-AndroidShellSingleQuoted",
        "ConvertTo-AndroidShellPathSingleQuoted",
        "ConvertTo-RedactedLogLine",
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
        "evidence-path-utils\.ps1",
        "evidence-redaction-utils\.ps1",
        "Resolve-EvidenceRepoPath",
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
        "Format-PublicEvidenceRedactionReviewFields",
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
        "Resolve-EvidenceRepoPath",
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
        "evidence-path-utils\.ps1",
        "evidence-redaction-utils\.ps1",
        "PUBLIC_EVIDENCE_REDACTION_REVIEW\.txt",
        "PUBLIC_SHARE_MANIFEST\.txt",
        "Resolve-EvidenceRepoPath",
        "Get-EvidenceRelativePath",
        "ConvertTo-RedactedEvidenceText",
        "Format-PublicEvidenceRedactionReviewFields",
        "Get-EvidenceTextFileExtensions",
        "Get-EvidenceImageFileExtensions",
        "Test-EvidenceLocalOnlyPath",
        "Raw evidence remains local",
        "IncludeImages"
    )

Add-Check `
    "scripts\review-public-evidence-redaction.ps1" `
    "fails public-share candidates without completed redaction review or with local-only artifacts" `
    @(
        "evidence-path-utils\.ps1",
        "evidence-redaction-utils\.ps1",
        "PUBLIC_EVIDENCE_REDACTION_REVIEW\.txt",
        "Get-PublicEvidenceRedactionReviewFields",
        "\[regex\]::Escape",
        "Get-EvidenceTextFileExtensions",
        "Get-EvidenceImageFileExtensions",
        "Get-EvidenceSensitiveTextChecks",
        "Test-EvidenceLocalOnlyPath",
        "Get-EvidenceRelativePath",
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
        "Refresh Game Versions",
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
        "android-shell-utils\.ps1",
        "evidence-path-utils\.ps1",
        "evidence-marker-utils\.ps1",
        "ConvertTo-AndroidShellSingleQuoted",
        "ConvertTo-AndroidShellPathSingleQuoted",
        "Resolve-EvidenceRepoPath",
        "ConvertTo-EvidenceSafeFileName",
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
        "Refresh Game Versions",
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
        "Diagnostics are hidden behind the Help & Reports drawer",
        "Android warmup/loading uses a mobile-width compact panel with readable styled percentage progress.",
        "Android post-launch startup status uses a framed mobile-width readable card after the launcher closes.",
        "Native fallback keeps verbose diagnostics collapsed until explicitly requested",
        "Native fallback recovery actions split into two touch-friendly rows on narrow landscape screens.",
        "Startup recovery compact actions render as structured user-facing controls: .*Restart App.*Open launcher.*Safe Start.*Cloud off.*Help Report.*Share details.*Copy Log.*Review first.*Hide Help.*Keep waiting",
        "The compact diagnostics toggle uses a touch-safe two-line detail label without exposing diagnostics by default.",
        "Compact diagnostics lives inside the scroll body rather than consuming fixed phone viewport chrome, and explicit diagnostics actions scroll to it.",
        "The compact status card stays readable while using low-profile spacing so more current task content remains visible.",
        "The compact status card stacks the phase chip and guided next action so neither is squeezed on narrow screens.",
        "Compact status details keep normal progress text to one stable line while attention/failure guidance can expand.",
        "Compact status uses short mobile detail copy and expands full raw status when tapped.",
        "Compact status exposes a visible `Details` / `Hide` cue in a touch-safe row.",
        "Compact sign-in status says `Sign in with Steam to continue` instead of exposing the raw credential prompt.",
        "Compact download-needed status says `Download this game version to play` and the next action reads `Install Game`.",
        "Compact ready status says `Ready to play this version` and the next action reads `Start Game`.",
        "Compact quick-start guidance starts collapsed behind a touch-safe two-line toggle that says `Quick Start` and `Get saves first`.",
        "Compact quick-start toggle renders Quick Start / Get saves first as structured title/detail labels",
        "Compact expanded quick-start guide renders each step inside a bounded row card.",
        "Compact sign-in, Steam Guard, and download task screens suppress the quick-start drawer so the current primary controls stay higher in the viewport.",
        "The compact brand subtitle remains readable at phone scale and uses plain app copy instead of pipe-separated command-line-style labels.",
        "The compact workflow strip stays in one dense row on narrow compact viewports instead of taking a second fixed header row.",
        "Tapping compact workflow step labels scrolls directly to the visible matching task section or the current safe fallback task.",
        "Compact recovery/tools actions use a two-column support grid when width allows and full-width stacked tools on narrow compact viewports.",
        "Compact Play and Sync shows the ready version, Save Check guidance, and Upload-locked state in a readable touch-safe summary card that opens Save Check without unlocking Push.",
        "Compact ready state prioritizes the ready summary, Save Check shortcut, Get-saves-first cloud controls, and Start Game before version management.",
        "Compact ready state keeps save backup and cloud sync options below Start Game as optional controls.",
        "Compact Pull action renders .*Get Steam Saves / Download to Android.* as a structured title/detail label",
        "Compact locked upload toggle renders Upload Locked / Review first and Hide Upload / Keep locked as structured title/detail labels:",
        "Compact unlocked Push actions render .*Upload to Steam / Overwrite cloud.* and .*Confirm Upload / Overwrite cloud.* as structured title/detail labels after the upload overwrite drawer is explicitly opened",
        "Compact armed Push warning says Steam Cloud overwrite / Confirm only after Pull/local saves are verified",
        "Compact Get Steam Saves and locked Steam upload share one two-button row when width allows and stack with Get Steam Saves first on narrow compact viewports.",
        "Compact Save Backup and Cloud Sync options use Local safety and Steam saves detail labels, share one low-profile row when width allows, and stack full-width on narrow compact viewports.",
        "Compact Android sign-in shows .*Sign in with Steam.* before password-manager helper copy",
        "Compact Android sign-in CTA renders .*Sign in with Steam / Android login.* as a structured title/detail label",
        "Compact Android sign-in uses a large primary .*Sign in with Steam.* CTA and a readable two-line password-manager safety helper",
        "Compact Steam Guard submit action renders .*Verify Code / Submit once.* as a structured title/detail label",
        "Compact Steam Guard retry keeps the rejected-code title short and moves latest-code guidance into the helper below the code controls",
        "Compact retry/failure state promotes .*Try Again.*Restart task.* primary recovery action while support tools remain secondary",
        "Compact launcher-log copy keeps the short .*Copy Log.* label but uses .*Review first.* detail text before copying diagnostics",
        "The compact current-task bar stays reachable, uses app-like task title wording, and is touch-safe without wasting vertical space",
        "The compact current-task bar uses short title labels such as .*Sign in.*, .*Verify.*, .*Files.*, and .*Play.* without a status prefix",
        "The compact current-task bar uses contextual detail labels such as .*Steam account.*, .*Steam Guard code.*, .*Download version.*, and .*Play and saves.*.",
        "The compact current-task bar renders task names and contextual details as structured title/detail labels",
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
        "Compact section headers use explicit short task cues such as .*Steam account.*, .*Current code.*, .*Local files.*, and .*Play safely.* instead of clipped desktop subtitle sentences",
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
        "Launcher startup recovery structured compact actions supported:",
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
        "Launcher compact plain-language status copy supported:",
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
        "Launcher compact short status details supported:",
        "Launcher compact sticky workflow step strip supported:",
        "Launcher compact low-profile workflow step strip supported:",
        "Launcher compact low-profile two-column workflow step strip supported:",
        "Launcher compact workflow step direct navigation supported:",
        "Launcher compact narrow workflow single-row supported:",
        "Launcher compact visible workflow step labels supported:",
        "Launcher compact workflow step detail labels supported:",
        "Launcher compact workflow step number badges supported:",
        "Launcher compact workflow unified touch height supported:",
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
        "Launcher consistent Start Game CTA supported:",
        "Launcher compact launch detail label supported:",
        "Launcher branded atmospheric background supported:",
        "Launcher branded background explicit RGBA supported:",
        "Launcher high-contrast rounded actions supported:",
        "Launcher compact header chrome reduction supported:",
        "Launcher compact condensed brand header supported:",
        "Launcher compact single-line brand header supported:",
        "Launcher compact readable brand subtitle supported:",
        "The compact brand subtitle remains readable at phone scale and uses plain app copy instead of pipe-separated command-line-style labels.",
        "Launcher compact section-header subtitle suppression supported:",
        "Launcher compact low-profile section headers supported:",
        "Launcher compact single-row section headers supported:",
        "Launcher compact section-header task cues supported:",
        "Launcher compact readable section-header cues supported:",
        "Launcher compact explicit section-header cues supported:",
        "Launcher compact install primary detail label supported:",
        "Launcher compact download progress hero supported:",
        "Launcher compact download progress status label supported:",
        "Launcher compact readable download progress bar supported:",
        "Launcher compact inline install-version controls supported:",
        "Launcher compact version details collapsible:",
        "Launcher compact version drawer bounded help label supported:",
        "Launcher compact structured install-version action labels supported:",
        "Launcher compact version summary cards supported:",
        "Launcher compact selected-version summary shortcut supported:",
        "Launcher compact selected-version headline supported:",
        "Launcher compact responsive selected-version summary supported:",
        "Launcher compact ready-version summary panel supported:",
        "Launcher compact ready-version summary shortcut supported:",
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
        "Launcher Help & Reports drawer hidden by default:",
        "Launcher Help & Reports drawer auto-opens for diagnostics actions:",
        "Compact diagnostics toggle renders .*Help & Reports.*Private until opened.*structured title/detail labels",
        "Launcher compact low-profile drawer toggles supported:",
        "Launcher compact dense drawer toggle height supported:",
        "Launcher compact support tools grid supported:",
        "Launcher compact launcher-log review label supported:",
        "Launcher plain-language help report copy supported:",
        "Launcher startup recovery structured compact actions supported:",
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
        "Native login panel keeps Sign in and Cancel reachable with the keyboard open",
        "Native login controls are stacked/full-width in portrait and use responsive wide rows in landscape",
        "Native login primary button says Sign in with Steam",
        "Native login action buttons render sentence-case labels instead of Android all-caps transformations",
        "Compact launcher sign-in shows Sign in with Steam before helper copy",
        "Compact launcher sign-in says Sign in with Steam / Android login",
        "Compact launcher sign-in uses a large primary Sign in with Steam CTA and a readable two-line password-manager safety helper",
        "Native login panel requests suggestions for username and password fields",
        "Native login panel requests suggestions again when username/password fields gain focus",
        "Native login Next control focuses password field",
        "Back/Cancel dismissal hides the soft keyboard before returning to launcher",
        "Provider does not prompt to save unverified credentials before Steam authentication",
        "Password visibility toggle shows/hides password without storing it",
        "Password visibility resets to hidden after submit/cancel/reopen",
        "Compact sign-in status says Sign in with Steam to continue instead of raw credential prompt",
        "Compact download-needed status says Download this game version to play and the next action reads Install Game",
        "Compact ready status says Ready to play this version and the next action reads Start Game",
        "Launcher compact plain-language status copy supported",
        "Quick-start guide visible",
        "On compact phone layout, quick-start guidance starts collapsed",
        "Compact quick-start toggle says Quick Start / Get saves first and is touch-safe",
        "Compact quick-start toggle renders Quick Start / Get saves first as structured title/detail labels",
        "Compact expanded quick-start guide stays bounded and shows Sign in, Get files, Get saves, Play, and Upload locked rows",
        "Compact expanded quick-start guide renders each step inside a bounded row card",
        "Compact active task screens suppress the quick-start drawer so primary controls stay higher",
        "Quick-start guidance expands/collapses without hiding the primary task",
        "Compact phone layout uses most of the usable screen height",
        "Compact phone layout avoids excessive internal panel margins",
        "Compact phone shell uses dense panel padding",
        "Compact phone layout uses dense vertical spacing between repeated launcher regions",
        "Compact phone layout uses dynamic content width instead of a narrow fixed column",
        "Tablet/wide layout avoids a narrow fixed inner column",
        "Android warmup/loading screen uses a mobile-width compact panel with readable styled progress",
        "Android post-launch startup status uses a framed mobile-width readable card",
        "Native fallback verbose diagnostics collapsed until requested",
        "Startup recovery compact actions render Restart App / Open launcher, Safe Start / Cloud off, Help Report / Share details, Copy Log / Review first, and Hide Help / Keep waiting",
        "Native fallback recovery actions split into responsive rows on narrow landscape screens",
        "Portal task flow is top anchored rather than vertically stranded",
        "Compact phone status appears as a readable vertical next-step card",
        "Compact phone status card is low-profile but still readable",
        "Compact status card uses an inline phase and next-action headline where width allows",
        "Compact status card stacks phase and next action without squeezing either label on narrow compact screens",
        "Compact status uses short mobile detail copy and expands full raw status when tapped",
        "Compact status exposes a visible Details / Hide cue in a touch-safe row",
        "Launcher compact touch-safe status detail button supported",
        "Launcher compact status detail cue supported",
        "Compact responsive numbered workflow step strip remains visible while scrolling",
        "Compact workflow step strip stays in one dense row on narrow compact viewports",
        "Compact workflow step strip uses unified 62px touch-height cells on narrow compact viewports",
        "Compact workflow step strip shows two-line visible labels, not only numbers/tooltips",
        "Sign in / Account, Verify / Steam Guard, Files / Game files, and Play / Saves safe",
        "Compact workflow step strip separates step numbers into small badges next to readable labels",
        "Launcher compact workflow step detail labels supported",
        "Launcher compact workflow unified touch height supported",
        "Tapping compact workflow step labels scrolls directly to visible matching task sections or the current safe fallback task",
        "Compact workflow step strip uses the same larger touch height as compact task actions",
        "Compact current-task bar remains reachable while scrolling",
        "Compact current-task bar uses app-like task title wording",
        "Compact current-task bar uses contextual task detail labels",
        "Compact current-task bar renders task/context labels as structured title/detail labels",
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
        "Compact Game Install selected-version summary is a readable touch-safe card with Cloud unchanged cue and Change / Change version shortcut",
        "Compact Game Install selected-version summary shortcut opens the version drawer without changing branches or cloud saves",
        "Compact install-version dropdown and refresh controls share one row when width allows",
        "Compact version drawer controls render Change Version / Local files only and Refresh Versions / Update branch list as structured title/detail labels",
        "Compact expanded version helper says Files for / Play version with Default files or Separate files and short branch status",
        "Status card shows a clear guided next action for the current state",
        "Failure/blocked/crash statuses show attention/fix guidance before normal install/cloud/launch guidance",
        "Primary actions use clear task wording, for example sign in/start game/verify code",
        "Primary launch action consistently says Start Game",
        "Primary and secondary actions are large enough to tap comfortably",
        "Launcher background has visible branded atmosphere without reducing readability",
        "Buttons use high-contrast rounded action styling",
        "Compact phone header uses shortened subtitle/chrome",
        "Compact phone brand header is a single low-profile row",
        "Compact phone brand subtitle remains readable at phone scale",
        "Compact phone brand subtitle uses plain app copy instead of pipe-separated labels",
        "Compact phone header leaves more first-action area visible",
        "Compact phone section headers avoid repeated subtitle blocks",
        "Compact phone section headers stay compact and leave controls visible",
        "Compact phone section headers keep title and readable task cue in one dense row without clipping the title",
        "Compact phone section headers use explicit short cues Steam account, Current code, Local files, and Play safely instead of clipped desktop subtitles",
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
        "Compact launch CTA says Start Game / Ready version",
        "Compact recovery/tools actions use a two-column support grid when width allows",
        "Compact recovery/tools actions stack full-width on narrow compact viewports",
        "Compact phone cloud safety starts collapsed",
        "Compact collapsed cloud-safety drawer reads Save Check / Get saves first so it does not look like the Get Steam Saves action",
        "Compact cloud-safety cue appears before Pull/Push controls",
        "Compact expanded cloud-safety detail says Saves for and Get Steam saves before upload / Upload can overwrite Steam",
        "Cloud safety expands/collapses while preserving Pull/Push controls",
        "Compact phone cloud options start collapsed",
        "Cloud options expand/collapse while preserving Pull/Push controls",
        "Compact Save Backup and Cloud Sync options use Local safety and Steam saves detail labels and share one low-profile row when width allows",
        "Compact Save Backup and Cloud Sync options stack full-width on narrow compact viewports",
        "Pull/Push controls appear before lower-frequency cloud options",
        "Pull from Cloud appears before Push to Cloud",
        "Compact cloud labels name Pull as Android-directed and Push as Steam-directed",
        "Compact Pull action says Get Steam Saves / Download to Android",
        "Compact locked Push toggle renders Upload Locked / Review first and Hide Upload / Keep locked as structured title/detail labels",
        "Compact unlocked Push actions say Upload to Steam / Overwrite cloud and Confirm Upload / Overwrite cloud",
        "Compact armed Push warning says Steam Cloud overwrite / Confirm only after Pull/local saves are verified",
        "Compact Get Steam Saves and locked Steam upload share one two-button row when width allows",
        "Compact Get Steam Saves and locked Steam upload stack with Get Steam Saves first on narrow compact viewports",
        "Armed Push state shows overwrite warning before final confirmation",
        "Branch, redownload, cache, and final Push confirmations use contextual confirm/cancel labels instead of generic OK/Cancel buttons",
        "Long compact confirmation warnings are scroll-safe and keep confirm/cancel buttons reachable",
        "Confirmation dialogs use the current visible viewport after rotation or keyboard-driven viewport changes",
        "Focused managed Steam Guard/fallback input stays visible above the Android soft keyboard",
        "Compact optional drawer toggles remain tappable without taking full primary-action height.",
        "Compact optional drawer toggles use a dense touch-safe height instead of the older tiny drawer rows.",
        "Compact drawer toggles and dense workflow controls share the same touch-safe compact height:",
        "Compact optional drawer toggles are visibly shorter than primary action buttons while still tappable.",
        "Help & Reports drawer hidden by default",
        "Compact diagnostics toggle uses a touch-safe two-line detail label",
        "Compact diagnostics toggle renders title/detail labels as structured controls",
        "Compact diagnostics is inside the scroll body rather than fixed root chrome",
        "Raw startup fallback failure text hidden from portal",
        "The compact workflow strip shows visible step labels such as Sign in / Account, Verify / Steam Guard, Files / Game files, and Play / Saves safe; it does not rely on hover-only tooltips",
        "The compact workflow strip is touch-safe enough for Android while keeping two-line step labels readable",
        "The compact game-version dropdown is large enough to read and tap when the version drawer is expanded",
        "Opening the compact game-version dropdown shows larger touch-safe popup row spacing and horizontal padding",
        "Compact Play and Sync ready-version summary is a readable touch-safe card with Save Check and Upload locked cues",
        "Compact Play and Sync ready-version summary shortcut opens Save Check without unlocking Push",
        "Compact Play and Sync keeps the ready summary, Save Check shortcut, Get-saves-first cloud controls, and Start Game before version management",
        "Compact Play and Sync keeps save backup and cloud sync options below Start Game as optional controls",
        "Username keyboard next action focuses password",
        "Next button focuses password and requests password suggestions",
        "Password keyboard done action attempts submit",
        "Password-manager suggestions",
        "Samsung Pass",
        "Google Password Manager",
        "Steam Guard prompt visible",
        "Steam Guard field accepts alphanumeric input",
        "Compact Steam Guard code field and Verify button are large enough to tap comfortably",
        "Compact Steam Guard shows code field and Verify Code before helper copy",
        "Compact Steam Guard code field and Verify Code share one touch-safe action row when width allows",
        "Compact Steam Guard code field and Verify Code stack full-width on narrow compact viewports",
        "Compact Steam Guard submit action says Verify Code / Submit once",
        "Compact Steam Guard retry keeps the rejected-code title short and moves latest-code guidance into the helper below the code controls",
        "Compact retry/failure recovery button is primary and uses Try Again / Restart task labels",
        "Compact launcher-log copy action says Copy Log / Review first",
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
        "Launcher compact short status details supported",
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
        "Launcher compact workflow step detail labels supported",
        "Launcher compact workflow step number badges supported",
        "Launcher compact workflow unified touch height supported",
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
        "Launcher compact selected-version summary shortcut supported",
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
        "Launcher consistent Start Game CTA supported",
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
        "Launcher compact explicit section-header cues supported",
        "Launcher compact install primary action first supported",
        "Launcher compact install primary detail label supported",
        "Launcher compact download progress hero supported",
        "Launcher compact download progress status label supported",
        "Launcher compact inline install-version controls supported",
        "Launcher compact version details collapsible",
        "Launcher compact structured install-version action labels supported",
        "Launcher compact ready-version summary panel supported",
        "Launcher compact ready-version summary shortcut supported",
        "Launcher compact ready-version headline supported",
        "Launcher compact responsive ready-version summary supported",
        "Launcher compact ready-state priority supported",
        "Launcher compact ready-state cloud options below launch supported",
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
        "Launcher compact launcher-log review label supported",
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
        "hidden Help & Reports drawer",
        "plain-language help-report and launcher-log status copy",
        "readable bounded compact diagnostics log viewport",
        "stronger branded header",
        "single-line compact brand",
        "readable compact brand subtitle",
        "plain-language readable compact brand subtitle",
        "responsive compact status headline row with stacked narrow-screen fallback",
        "viewport-aware compact status headline reflow after rotation or keyboard viewport changes",
        "stable one-line compact status detail row with short mobile copy, tap-to-expand full status, and a visible Details/Hide cue",
        "Android compact touch-scale floor for small-device readability",
        "Android-readable shader warmup/loading",
        "Android-readable post-launch startup status card",
        "responsive touch-safe compact sticky task header in a low-profile toolbar shell with a subdued inline current-task button and two-line workflow step cells using a shared 62px touch height",
        "contextual current-task detail labels",
        "readable stacked current-task row",
        "two-line single-row compact workflow strip on narrow screens",
        "viewport-aware sticky task header reflow after rotation or keyboard viewport changes",
        "viewport-aware compact task re-anchoring after rotation or keyboard viewport changes",
        "readable step-number badges",
        "Sign in with Steam / Android login",
        "Verify Code / Submit once",
        "Start Game / Ready version",
        "structured Play/Sync title/detail action labels",
        "Save Check / Get saves first",
        "Saves for / Get Steam saves before upload / Upload can overwrite Steam",
        "Get Steam Saves / Download to Android",
        "structured locked-Push title/detail labels",
        "Upload to Steam / Overwrite cloud",
        "Confirm Upload / Overwrite cloud",
        "Steam Cloud overwrite / Confirm only after Pull/local saves are verified",
        "responsive compact action rows",
        "responsive selected-version install summary",
        "touch-safe compact version dropdown popups",
        "inline compact install-version dropdown/refresh controls with structured title/detail labels",
        "version details with structured compact version-file drawer labels",
        "responsive ready-version summary",
        "compact ready-state priority that keeps the ready summary, Save Check shortcut, and Get-saves-first cloud controls before Start Game while moving version management below the primary launch path",
        "compact ready-state cloud options stay below Start Game as an optional save-settings drawer after Get-saves-first cloud controls",
        "large compact Android sign-in CTA with readable two-line",
        "responsive compact Steam Guard code controls",
        "viewport-aware compact Steam Guard code/action row reflow after rotation or keyboard viewport changes",
        "compact Steam Guard bounded two-line helper labels",
        "primary structured compact retry recovery",
        "structured compact startup recovery actions",
        "compact user-facing support tool labels such as Safe Start / Cloud off, Check Files / Updates, Game Versions / Refresh list, Repair Files / Rebuild game, Free Space / Old versions, Help Report / Share details, Last Problem / Open details, and Copy Log / Review first",
        "short-height copy on cramped landscape screens",
        "short-height copy reflow when the landscape height class changes",
        "keyboard-reduced usable height",
        "scroll-safe compact confirmations",
        "bounded compact quick-start guide panel",
        "collapsible quick-start guidance with structured compact Quick Start / Get saves first title/detail labels and bounded guide row cards that suppress during active compact task screens",
        "dense compact drawer toggles",
        "structured compact title/detail labels",
        "native fallback recovery screens that keep verbose diagnostics collapsed until requested and split actions into responsive rows on narrow landscape screens",
        "dense compact vertical rhythm",
        "single-row compact section headers with explicit short task cues such as Steam account, Current code, Local files, and Play safely",
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
        "Refresh Game Versions",
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
        "bounded two-line Files for / Play version helper labels"
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
        "structured compact startup recovery actions",
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
        "Refresh Game Versions",
        "public-inherited",
        "public-vs-beta integrity classification",
        "steam-beta-integrity-runtime-checklist\.md",
        "mixed beta/public behavior",
        "Autofill",
        "SteamKit debug logs are disabled by default",
        "Steam beta password entry",
        "Push backup evidence"
    )

Complete-StaticAudit `
    -FailureHeading "Steam version-selection static audit failed:" `
    -SuccessMessage "Steam version-selection static audit passed: {0} checks."
