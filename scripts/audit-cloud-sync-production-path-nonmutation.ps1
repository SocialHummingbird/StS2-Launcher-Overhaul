param()

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$programPath = Join-Path $root "tools\CloudSyncProductionPathProbe\Program.cs"
$storePath = Join-Path $root "tools\CloudSyncProductionPathProbe\InMemoryCloudSaveStore.cs"
$manualSyncPath = Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.cs"
$contextPath = Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.Context.cs"
$appPathsPath = Join-Path $root "src\STS2Mobile\AppPaths.cs"
$tracePath = Join-Path $root "src\STS2Mobile\BootstrapTrace.cs"

foreach ($path in @(
    $programPath,
    $storePath,
    $manualSyncPath,
    $contextPath,
    $appPathsPath,
    $tracePath
)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required Stage 8 audit source is missing: $path"
    }
}

$program = Get-Content -LiteralPath $programPath -Raw
$store = Get-Content -LiteralPath $storePath -Raw
$manualSync = Get-Content -LiteralPath $manualSyncPath -Raw
$context = Get-Content -LiteralPath $contextPath -Raw
$appPaths = Get-Content -LiteralPath $appPathsPath -Raw
$trace = Get-Content -LiteralPath $tracePath -Raw

$requiredScenarios = @(
    "SlowPullReportsProgressAsync",
    "MissingCredentialsFailClosedAsync",
    "UnavailableFileIsSkippedAsync",
    "BranchMismatchBlocksPushAsync",
    "SelectedModsBlockPushAsync",
    "CancellationDrainsWithoutMutationAsync",
    "FailedPullRecoversOnRetryAsync",
    "InMemoryPushUsesProductionPathAsync"
)
foreach ($scenario in $requiredScenarios) {
    if ($program -notmatch [regex]::Escape($scenario)) {
        throw "Stage 8 production-path scenario is missing: $scenario"
    }
}

foreach ($requiredPattern in @(
    "CloudSyncCoordinator\.SetLocalBackupEnabled\(false\)",
    "LauncherCloudSaveState\.ClearCredentials\(\)",
    "cloud\.WriteCount == 0",
    "ActiveOperations == 0",
    "HasToken=False",
    "real Steam Cloud Push was used"
)) {
    if ($program -notmatch $requiredPattern) {
        throw "Stage 8 non-mutation assertion is missing: $requiredPattern"
    }
}

$probeSource = $program + [Environment]::NewLine + $store
foreach ($forbiddenPattern in @(
    "CloudSaveStoreFactory",
    "SteamKit2CloudSaveStore",
    "SaveCredentials\s*\(",
    "HttpClient",
    "HttpWebRequest",
    "WebRequest\.Create",
    "System\.Net\.Sockets",
    "new\s+Socket\s*\("
)) {
    if ($probeSource -match $forbiddenPattern) {
        throw "Stage 8 probe contains a forbidden production mutation dependency: $forbiddenPattern"
    }
}

foreach ($requiredPattern in @(
    "ManualPushAllAsync\(\s*ISaveStore local,\s*ICloudSaveStore cloud,",
    "ManualPullAllAsync\(\s*ISaveStore local,\s*ICloudSaveStore cloud,"
)) {
    if ($manualSync -notmatch $requiredPattern) {
        throw "Injectable production manual-sync route is missing: $requiredPattern"
    }
}

if ($context -notmatch "CreateManualSyncContext\(\s*ISaveStore local,\s*ICloudSaveStore cloud,") {
    throw "Injected stores no longer reach the production ManualSyncContext."
}
if ($trace -notmatch "STS2_BOOTSTRAP_TRACE_FILE") {
    throw "Native-independent command-line trace routing is missing."
}
if ($appPaths -notmatch "HasStoragePermission\(\)\s*\{\s*if \(!OperatingSystem\.IsAndroid\(\)\)\s*return false;") {
    throw "Storage permission checks are not guarded outside Android."
}

Write-Host "Stage 8 non-mutation audit passed."
Write-Host "The validation probe has no credentials, network store, or network transport dependency."
