param()

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$programPath = Join-Path $root "tools\CloudSyncProductionPathProbe\Program.cs"
$storePath = Join-Path $root "tools\CloudSyncProductionPathProbe\InMemoryCloudSaveStore.cs"
$automaticScenariosPath = Join-Path $root "tools\CloudSyncProductionPathProbe\AutomaticSyncScenarios.cs"
$recoveryScenariosPath = Join-Path $root "tools\CloudSyncProductionPathProbe\SaveRecoveryScenarios.cs"
$recoveryRestorePath = Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.Recovery.Restore.cs"
$cancellableStorePath = Join-Path $root "src\STS2Mobile\Steam\CancellableSaveStore.cs"
$androidStoreFilesPath = Join-Path $root "src\STS2Mobile\Steam\AndroidLocalSaveStore.Files.cs"
$androidStoreIoPath = Join-Path $root "src\STS2Mobile\Steam\AndroidLocalSaveStore.FileIo.cs"
$manualSyncPath = Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.cs"
$contextPath = Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.Context.cs"
$planPath = Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.Plan.cs"
$transferPath = Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.Transfer.cs"
$saveContextPath = Join-Path $root "src\STS2Mobile\Steam\SaveContext.cs"
$appPathsPath = Join-Path $root "src\STS2Mobile\AppPaths.cs"
$tracePath = Join-Path $root "src\STS2Mobile\BootstrapTrace.cs"
$automaticResultPath = Join-Path $root "src\STS2Mobile\Steam\AutomaticSyncResult.cs"
$automaticPaths = @(
    (Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.AutomaticSync.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.AutomaticSync.Model.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.AutomaticSync.Manifest.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.AutomaticSync.Persistence.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.AutomaticSync.Snapshots.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.AutomaticSync.Reconcile.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.AutomaticSync.Retry.cs")
)

foreach ($path in @(
    $programPath,
    $storePath,
    $automaticScenariosPath,
    $recoveryScenariosPath,
    $recoveryRestorePath,
    $cancellableStorePath,
    $androidStoreFilesPath,
    $androidStoreIoPath,
    $manualSyncPath,
    $contextPath,
    $planPath,
    $transferPath,
    $saveContextPath,
    $appPathsPath,
    $tracePath,
    $automaticResultPath
) + $automaticPaths) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required Stage 8 audit source is missing: $path"
    }
}

$program = Get-Content -LiteralPath $programPath -Raw
$store = Get-Content -LiteralPath $storePath -Raw
$automaticScenarios = Get-Content -LiteralPath $automaticScenariosPath -Raw
$recoveryScenarios = Get-Content -LiteralPath $recoveryScenariosPath -Raw
$recoveryRestore = Get-Content -LiteralPath $recoveryRestorePath -Raw
$cancellableStore = Get-Content -LiteralPath $cancellableStorePath -Raw
$androidStoreFiles = Get-Content -LiteralPath $androidStoreFilesPath -Raw
$androidStoreIo = Get-Content -LiteralPath $androidStoreIoPath -Raw
$manualSync = Get-Content -LiteralPath $manualSyncPath -Raw
$context = Get-Content -LiteralPath $contextPath -Raw
$plan = Get-Content -LiteralPath $planPath -Raw
$transfer = Get-Content -LiteralPath $transferPath -Raw
$saveContext = Get-Content -LiteralPath $saveContextPath -Raw
$appPaths = Get-Content -LiteralPath $appPathsPath -Raw
$trace = Get-Content -LiteralPath $tracePath -Raw
$automaticResult = Get-Content -LiteralPath $automaticResultPath -Raw
$automatic = ($automaticPaths | ForEach-Object {
    Get-Content -LiteralPath $_ -Raw
}) -join [Environment]::NewLine

$requiredScenarios = @(
    "SuccessfulPushAsync",
    "SuccessfulPullAsync",
    "ImmutablePushSnapshotAsync",
    "ModdedAllowlistIsolationAsync",
    "CommitStyleFailureAsync",
    "FileCommittedFalseIsRejectedAsync",
    "AuthenticationFailureAsync",
    "ConnectionLossAsync",
    "DownloadFailureAsync",
    "RemoteReadBackMismatchAsync",
    "PushCurrentRunTombstoneAsync",
    "PullCurrentRunTombstoneAsync",
    "TombstoneDeleteFailureAsync",
    "AccountMismatchAsync",
    "NamespaceMismatchAsync",
    "RuntimeMismatchAsync",
    "ModSetMismatchAsync"
)
foreach ($scenario in $requiredScenarios) {
    if ($program -notmatch [regex]::Escape($scenario)) {
        throw "Stage 8 production-path scenario is missing: $scenario"
    }
}

$requiredAutomaticScenarios = @(
    "ExplicitFirstSourceChoiceBothWaysAsync",
    "ThreeWayDecisionMatrixAsync",
    "DurableBeforeGameStateAsync",
    "QuitStyleRecoveryAsync",
    "OfflineRetentionAndFreshRemoteReadAsync",
    "InterruptedUploadStatesAsync",
    "InterruptedPullStatesAsync",
    "PersistenceEdgeRestartMatrixAsync",
    "ContextSeparationAndLaunchGateAsync",
    "ResultSemanticsAsync",
    "RawByteSnapshotAsync",
    "SnapshotCorruptionIsRejectedAsync",
    "RetainedSnapshotPruningAsync",
    "LegacyBeforeGameFallbackAsync"
)
foreach ($scenario in $requiredAutomaticScenarios) {
    if ($automaticScenarios -notmatch [regex]::Escape($scenario)) {
        throw "Stage 3 production-path scenario is missing: $scenario"
    }
}

foreach ($requiredPattern in @(
    "CloudSyncCoordinator\.SetLocalBackupEnabled\(false\)",
    "cloud\.WriteCount == 0 && cloud\.DeleteCount == 0",
    "ExpectNoSuccessClaim",
    "invalidate the context marker before writing data",
    "real Steam Cloud operation was used"
)) {
    if ($program -notmatch $requiredPattern) {
        throw "Stage 8 non-mutation assertion is missing: $requiredPattern"
    }
}

$probeSource = $program + [Environment]::NewLine + $store `
    + [Environment]::NewLine + $automaticScenarios
foreach ($forbiddenPattern in @(
    "CloudSaveStoreFactory",
    "new\s+SteamKit2CloudSaveStore",
    "SteamKit2CloudSaveStore\.GetOrCreate",
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
    "RecoverAutomaticSyncAsync\(\s*ISaveStore local,\s*ICloudSaveStore cloud,",
    "ReconcileAutomaticSyncAsync\(\s*ISaveStore local,\s*ICloudSaveStore cloud,",
    "BeginAutomaticGameSessionAsync\(\s*ISaveStore local,\s*ICloudSaveStore cloud,",
    "AutomaticSyncPendingPath",
    "WriteAtomicAutomaticSyncDocumentAsync",
    "CaptureAutomaticSnapshotAsync",
    "IsPathwiseBlendOf",
    "RunTransferAsync\("
)) {
    if ($automatic -notmatch $requiredPattern) {
        throw "Stage 3 automatic-sync invariant is missing: $requiredPattern"
    }
}
foreach ($requiredPattern in @(
    "GameSessionPrepared",
    "CanStartGame",
    "PendingRecoveryRequired",
    "SourceChoiceRequired",
    "Conflict"
)) {
    if ($automaticResult -notmatch $requiredPattern) {
        throw "Stage 3 launch-gate result is missing: $requiredPattern"
    }
}
foreach ($forbiddenPattern in @(
    "Microsoft\.Data\.Sqlite",
    "DbContext",
    "SQLite",
    "Outbox",
    "BackgroundService",
    "StartForegroundService",
    "Android\.App\.Service",
    "GetLastModifiedTime",
    "SetLastModifiedTime",
    "onPause",
    "onStop",
    "SemanticMerge"
)) {
    if ($automatic -match $forbiddenPattern) {
        throw "Stage 3 automatic sync contains forbidden machinery: $forbiddenPattern"
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

if ($plan -notmatch "new ManualSyncContext\(\s*local,\s*cloud,") {
    throw "Injected stores no longer reach the production ManualSyncContext."
}
foreach ($requiredPattern in @(
    "CaptureSourceSnapshotAsync",
    "BackupDestinationsAsync",
    "ReadSourceFileBytesAsync",
    "ReadDestinationFileBytesAsync",
    "WriteDestinationFileBytesAsync",
    "WriteAndVerifyLocalBytesAsync",
    "RequireHash",
    "DeleteRemoteFileAsync\(context\.MarkerPath\)"
)) {
    if ($transfer -notmatch $requiredPattern) {
        throw "Trustworthy transfer invariant is missing: $requiredPattern"
    }
}
foreach ($requiredPattern in @(
    "SteamId64",
    "SaveNamespace",
    "RuntimeIdentity",
    "ModSetFingerprint",
    "RequireExactMatch"
)) {
    if ($saveContext -notmatch $requiredPattern) {
        throw "SaveContext invariant is missing: $requiredPattern"
    }
}
if ($trace -notmatch "STS2_BOOTSTRAP_TRACE_FILE") {
    throw "Native-independent command-line trace routing is missing."
}
if ($appPaths -notmatch "HasStoragePermission\(\)\s*\{\s*if \(!OperatingSystem\.IsAndroid\(\)\)\s*return false;") {
    throw "Storage permission checks are not guarded outside Android."
}

foreach ($requiredPattern in @(
    "IRecoverySaveStore",
    "WriteRecoveryBytesAsync",
    "DeleteRecoveryFileAsync"
)) {
    if ($cancellableStore -notmatch $requiredPattern) {
        throw "Recovery-only save-store route is missing: $requiredPattern"
    }
}
foreach ($requiredPattern in @(
    "IRecoverySaveStore\.WriteRecoveryFileBytesAsync",
    "IRecoverySaveStore\.DeleteRecoveryFileAsync"
)) {
    if ($androidStoreFiles -notmatch $requiredPattern) {
        throw "Android recovery-only store implementation is missing: $requiredPattern"
    }
}
$androidRecoveryWrite = [regex]::Match(
    $androidStoreIo,
    "private async Task WriteRecoveryBytesFileAsync[\s\S]*?\r?\n    \}"
).Value
$androidRecoveryDelete = [regex]::Match(
    $androidStoreIo,
    "private Task DeleteRecoveryFileDirectAsync[\s\S]*?\r?\n    \}"
).Value
if (-not $androidRecoveryWrite -or -not $androidRecoveryDelete) {
    throw "Android recovery-only raw write/delete methods are missing."
}
if (($androidRecoveryWrite + $androidRecoveryDelete) -match "MirrorLocalSave") {
    throw "Recovery raw writes/deletes must not mirror into external or legacy save evidence."
}
foreach ($requiredPattern in @(
    "CancellableSaveStore\.WriteRecoveryBytesAsync",
    "CancellableSaveStore\.DeleteRecoveryFileAsync",
    "RequireNoCaseFoldCollidingHistoryNames"
)) {
    if ($recoveryRestore -notmatch $requiredPattern) {
        throw "Recovery application safety invariant is missing: $requiredPattern"
    }
}
foreach ($forbiddenPattern in @(
    "CancellableSaveStore\.WriteBytesAsync\(",
    "CancellableSaveStore\.DeleteFileAsync\("
)) {
    if ($recoveryRestore -match $forbiddenPattern) {
        throw "Recovery application still uses a normal mirrored mutation route: $forbiddenPattern"
    }
}
foreach ($requiredPattern in @(
    "CaseFoldHistoryCollisionBlocksBeforeMutationAsync",
    "RecoveryWriteCount > 0",
    "RecoveryDeleteCount > 0",
    "RawWriteCount == 0"
)) {
    if ($recoveryScenarios -notmatch [regex]::Escape($requiredPattern)) {
        throw "Recovery non-mutation probe assertion is missing: $requiredPattern"
    }
}

Write-Host "Stage 2/3/4 non-mutation audit passed."
Write-Host "The validation probe has no credentials, network store, or network transport dependency."
