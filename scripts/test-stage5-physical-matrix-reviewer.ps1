$ErrorActionPreference = "Stop"

$templateScript = Join-Path $PSScriptRoot 'new-stage5-physical-matrix-template.ps1'
$reviewer = Join-Path $PSScriptRoot 'review-stage5-physical-matrix.ps1'
$androidVerifier = Join-Path $PSScriptRoot 'verify-stage5-android-save-bundle.ps1'
$steamCapture = Join-Path $PSScriptRoot 'new-stage5-live-steam-manifest.ps1'
$inventoryScript = Join-Path $PSScriptRoot 'new-stage5-android-evidence-inventory.ps1'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) (
    'sts2-stage5-matrix-review-test-' + [Guid]::NewGuid().ToString('N')
)

$sourceCommit = '1234567890abcdef1234567890abcdef12345678'
$steamId64 = '76561198000000001'
$packageName = 'com.sts2launcher.overhaul.fork.local'
$signerSha256 = 'fd0e3d5acf435c1d23bfc5c426e99aa9eb5808619ff1fc214ffca99cfac7e57a'
$deviceSerial = 'fixture-device-01'
$contexts = [ordered]@{
    'vanilla-public' = [ordered]@{
        SteamId64 = $steamId64; SaveNamespace = 'vanilla'; RuntimeIdentity = 'public'; ModSetFingerprint = ''
    }
    'vanilla-public-beta' = [ordered]@{
        SteamId64 = $steamId64; SaveNamespace = 'vanilla'; RuntimeIdentity = 'public-beta'; ModSetFingerprint = ''
    }
    'modded-exact' = [ordered]@{
        SteamId64 = $steamId64; SaveNamespace = 'modded'; RuntimeIdentity = 'public'; ModSetFingerprint = 'mods-exact-001'
    }
    'modded-changed' = [ordered]@{
        SteamId64 = $steamId64; SaveNamespace = 'modded'; RuntimeIdentity = 'public'; ModSetFingerprint = 'mods-changed-002'
    }
}

function Get-Sha256Hex {
    param([Parameter(Mandatory = $true)][byte[]]$Bytes)
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        return (($sha.ComputeHash($Bytes) | ForEach-Object { $_.ToString('x2') }) -join '')
    } finally {
        $sha.Dispose()
    }
}

function Get-FileHashHex {
    param([Parameter(Mandatory = $true)][string]$Path)
    return Get-Sha256Hex -Bytes ([IO.File]::ReadAllBytes($Path))
}

function Write-JsonNoBom {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)]$Value
    )
    $parent = Split-Path -Parent $Path
    if ($parent) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
    [IO.File]::WriteAllText(
        $Path,
        ($Value | ConvertTo-Json -Depth 30),
        [Text.UTF8Encoding]::new($false)
    )
}

function Copy-JsonEvidence {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )
    $parent = Split-Path -Parent $Destination
    New-Item -ItemType Directory -Force -Path $parent | Out-Null
    [IO.File]::WriteAllBytes($Destination, [IO.File]::ReadAllBytes($Source))
}

function New-ContextMarker {
    param([Parameter(Mandatory = $true)]$Context)
    return ([ordered]@{
        Version = 1
        SteamId64 = [uint64]$Context.SteamId64
        SaveNamespace = [string]$Context.SaveNamespace
        RuntimeIdentity = [string]$Context.RuntimeIdentity
        ModSetFingerprint = [string]$Context.ModSetFingerprint
    } | ConvertTo-Json -Compress)
}

function New-State {
    param(
        [Parameter(Mandatory = $true)][string]$Namespace,
        [Parameter(Mandatory = $true)][string]$ProfileLabel,
        [Parameter(Mandatory = $true)][string]$ProgressLabel
    )
    $prefix = if ($Namespace -eq 'modded') { 'modded/' } else { '' }
    $state = [Collections.Generic.Dictionary[string, byte[]]]::new([StringComparer]::OrdinalIgnoreCase)
    $state.Add('profile.save', [Text.Encoding]::UTF8.GetBytes("profile:$ProfileLabel`r`n"))
    $state.Add("${prefix}profile1/saves/progress.save", [Text.Encoding]::UTF8.GetBytes("progress:$ProgressLabel`r`n"))
    return $state
}

function Merge-State {
    param([Parameter(Mandatory = $true)][object[]]$States)
    $merged = [Collections.Generic.Dictionary[string, byte[]]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($state in $States) {
        foreach ($path in $state.Keys) {
            if ($merged.ContainsKey($path)) {
                if ((Get-Sha256Hex -Bytes $merged[$path]) -ne (Get-Sha256Hex -Bytes $state[$path])) {
                    throw "Fixture state collision at $path."
                }
            } else {
                $merged.Add($path, $state[$path])
            }
        }
    }
    return $merged
}

function New-Snapshot {
    param(
        [Parameter(Mandatory = $true)]$Context,
        [Parameter(Mandatory = $true)]$State,
        [Parameter(Mandatory = $true)][string]$Label
    )
    $entries = [Collections.Generic.List[object]]::new()
    $files = [Collections.Generic.List[object]]::new()
    $paths = [Collections.Generic.List[string]]::new()
    $paths.Add('profile.save')
    $prefix = if ([string]$Context.SaveNamespace -eq 'modded') { 'modded/' } else { '' }
    foreach ($profileId in 1..3) {
        foreach ($name in @('progress.save', 'prefs', 'prefs.save', 'current_run.save', 'current_run_mp.save')) {
            $paths.Add("${prefix}profile${profileId}/saves/$name")
        }
    }
    foreach ($path in $paths) {
        if ($State.ContainsKey($path)) {
            $bytes = $State[$path]
            $hash = Get-Sha256Hex -Bytes $bytes
            $entries.Add([ordered]@{ Path = $path; Exists = $true; Sha256 = $hash; ByteSha256 = $hash })
            $files.Add([ordered]@{ Path = $path; ContentBase64 = [Convert]::ToBase64String($bytes); ByteSha256 = $hash })
        } else {
            $entries.Add([ordered]@{ Path = $path; Exists = $false; Sha256 = ''; ByteSha256 = '' })
        }
    }
    return [ordered]@{
        Version = 2
        ContextMarker = New-ContextMarker -Context $Context
        Coverage = 'full'
        SourceKind = 'fixture'
        SourceLabel = $Label
        CapturedUtc = '2026-01-01T00:00:00.0000000+00:00'
        Manifest = [ordered]@{ Version = 1; Entries = @($entries) }
        Files = @($files)
    }
}

function New-AndroidBase {
    param(
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][string]$ContextId,
        [Parameter(Mandatory = $true)]$State,
        [bool]$CloudSyncEnabled = $true,
        [string]$PendingPhase = '',
        [string]$RecoveryPhase = '',
        $RecoveryState = $null
    )
    $context = $contexts[$ContextId]
    $bundle = [ordered]@{
        Version = 2
        CreatedUtc = '2026-01-01T00:00:00+00:00'
        OriginalSourcesWereModified = $false
        SteamWasContacted = $false
        CloudSyncEnabled = $CloudSyncEnabled
        SelectedSaveContext = $context
        CurrentAndroidSnapshot = New-Snapshot -Context $context -State $State -Label $Key
        RecoverySnapshots = @()
        RecoveryHoldJson = ''
        RecoveryJournalJson = ''
        AutomaticSyncPendingJson = ''
        AutomaticSyncBaselineJson = ''
        AutomaticSyncBeforeGameSnapshotJson = ''
    }
    if ($PendingPhase) {
        $bundle.AutomaticSyncPendingJson = ([ordered]@{
            Version = 1
            Phase = $PendingPhase
            ContextMarker = New-ContextMarker -Context $context
        } | ConvertTo-Json -Compress)
    }
    if ($RecoveryPhase) {
        $bundle.RecoveryJournalJson = ([ordered]@{
            Version = 2
            Phase = $RecoveryPhase
            SaveNamespace = [string]$context.SaveNamespace
            RuntimeIdentity = [string]$context.RuntimeIdentity
            ModSetFingerprint = [string]$context.ModSetFingerprint
            TargetContextMarker = New-ContextMarker -Context $context
            SourceSnapshotPath = '.sts2-launcher/recovery/source.json'
            SourceSnapshotSha256 = ('b' * 64)
            UndoSnapshotPath = '.sts2-launcher/recovery/undo.json'
            UndoSnapshotSha256 = ('c' * 64)
            AppliedSnapshotPath = '.sts2-launcher/recovery/applied.json'
            AppliedSnapshotSha256 = ('d' * 64)
            AppliedManifest = [ordered]@{ Version = 1; Entries = @() }
            CreatedUtc = '2026-01-01T00:00:00Z'
            UpdatedUtc = '2026-01-01T00:01:00Z'
        } | ConvertTo-Json -Compress -Depth 8)
    }
    if ($null -ne $RecoveryState) {
        $recoverySnapshot = New-Snapshot -Context $context -State $RecoveryState -Label 'transfer-destination-backup'
        $reported = 'e' * 64
        $bundle.RecoverySnapshots = @([ordered]@{
            CandidateId = $reported
            SourceKind = 'TransferBackup'
            SourceLabel = 'transfer-destination-backup'
            Classification = 'ExactContext'
            Coverage = 'full'
            SnapshotPath = '.sts2-launcher/recovery/transfer-backup.json'
            SnapshotSha256 = $reported
            Snapshot = $recoverySnapshot
        })
    }
    $bundlePath = Join-Path $testRoot "android-bases/$Key-bundle.json"
    $manifestPath = Join-Path $testRoot "android-bases/$Key-manifest.json"
    Write-JsonNoBom -Path $bundlePath -Value $bundle
    $namespace = if ([string]$context.SaveNamespace -eq 'modded') { 'Modded' } else { 'Vanilla' }
    & $androidVerifier `
        -BundlePath $bundlePath `
        -OutputPath $manifestPath `
        -Namespace $namespace `
        -ExpectedSteamId64 $steamId64 `
        -ExpectedRuntimeIdentity ([string]$context.RuntimeIdentity) `
        -ExpectedModSetFingerprint ([string]$context.ModSetFingerprint) *> $null
    return $manifestPath
}

function New-SteamBase {
    param(
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][string]$ContextId,
        [Parameter(Mandatory = $true)]$State,
        [string]$CapturedUtc = '2026-01-01T02:00:00.0000000+00:00'
    )
    $context = $contexts[$ContextId]
    $root = Join-Path $testRoot "steam-roots/$Key"
    New-Item -ItemType Directory -Force -Path $root | Out-Null
    foreach ($path in $State.Keys) {
        $full = Join-Path $root $path.Replace('/', [IO.Path]::DirectorySeparatorChar)
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $full) | Out-Null
        [IO.File]::WriteAllBytes($full, $State[$path])
    }
    $markerPath = Join-Path $root ".sts2-launcher/contexts/$([string]$context.SaveNamespace).json"
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $markerPath) | Out-Null
    [IO.File]::WriteAllText(
        $markerPath,
        (New-ContextMarker -Context $context),
        [Text.UTF8Encoding]::new($false)
    )
    $syncEvidence = Join-Path $testRoot "steam-sync/$Key.txt"
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $syncEvidence) | Out-Null
    [IO.File]::WriteAllText(
        $syncEvidence,
        "fixture independent Steam client download: $Key",
        [Text.UTF8Encoding]::new($false)
    )
    $manifestPath = Join-Path $testRoot "steam-bases/$Key-manifest.json"
    $namespace = if ([string]$context.SaveNamespace -eq 'modded') { 'Modded' } else { 'Vanilla' }
    & $steamCapture `
        -SteamCloudRoot $root `
        -SelectedNamespace $namespace `
        -ExpectedSteamId64 $steamId64 `
        -ExpectedRuntimeIdentity ([string]$context.RuntimeIdentity) `
        -ExpectedModSetFingerprint ([string]$context.ModSetFingerprint) `
        -SteamSyncEvidencePath $syncEvidence `
        -CaptureMethod 'deterministic retained fixture' `
        -RetainedImmutableSource `
        -OutputPath $manifestPath *> $null
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $manifest.capturedUtc = $CapturedUtc
    Write-JsonNoBom -Path $manifestPath -Value $manifest
    return $manifestPath
}

function Get-CollectorLog {
    param(
        [Parameter(Mandatory = $true)][int]$Row,
        [Parameter(Mandatory = $true)][string]$Phase
    )
    switch ($Phase) {
        'after-quit-sync' {
            return @(
                'NGame.Quit completed final local saves; restarting launcher',
                'Automatic save sync: resuming the pending post-game reconciliation.',
                'Local and Steam saves were synchronized and verified'
            ) -join "`n"
        }
        'before-play-reconcile' { return 'Local and Steam saves were synchronized and verified' }
        'divergence-conflict' { return 'Automatic save synchronization conflict: both changed differently; pending-sync remains' }
        'after-modded-sync' { return 'Local and Steam saves were synchronized and verified' }
        'changed-mod-set-blocked' { return 'Automatic save synchronization conflict: Steam Cloud save-context mismatch for mod set' }
        'after-switch-to-beta' { return 'Local and Steam saves were synchronized and verified' }
        'after-switch-to-public' { return 'Local and Steam saves were synchronized and verified' }
        'offline-pending' { return 'Automatic save sync pending-sync: network offline; connection unavailable' }
        'retry-complete' { return 'Local and Steam saves were synchronized and verified' }
        'pending-before-force-stop' { return 'Automatic save sync pending-sync: uploading' }
        'after-restart' {
            return @(
                "ActivityManager: Force stopping $packageName appid=12345 user=0: fixture",
                "ActivityManager: Start proc 4242:$packageName/u0a123 for activity $packageName/.MainActivity",
                'Automatic save sync: resuming the pending post-game reconciliation.',
                'Local and Steam saves were synchronized and verified'
            ) -join "`n"
        }
        'commit-failure' { return 'Automatic save sync pending-sync: commit failed because file_committed=false' }
        'readback-failure' { return 'Automatic save sync pending-sync: remote read-back hash mismatch; could not be verified' }
        'after-restore' { return '[Recovery] Restore completed on Android only' }
        'after-undo' { return '[Recovery] Undo completed byte-for-byte on Android' }
        default { throw "Unknown fixture collector phase $Phase."
        }
    }
}

function New-CollectorEvidence {
    param(
        [Parameter(Mandatory = $true)]$Spec,
        [Parameter(Mandatory = $true)][string]$CandidateApkPath,
        [Parameter(Mandatory = $true)][string]$CandidateApkSha256
    )
    $id = [string]$Spec.id
    $phase = [string]$Spec.phase
    $row = [int]$Spec.row
    $output = Join-Path $testRoot "collectors/$id"
    New-Item -ItemType Directory -Force -Path $output | Out-Null
    $log = Get-CollectorLog -Row $row -Phase $phase
    $logPath = Join-Path $output 'logcat.txt'
    $filteredPath = Join-Path $output 'filtered-logcat.txt'
    $summaryPath = Join-Path $output 'summary.txt'
    $packagePath = Join-Path $output 'package.txt'
    $localHashesPath = Join-Path $output 'local-save-byte-hashes.tsv'
    $stateHashesPath = Join-Path $output 'sync-recovery-state-byte-hashes.tsv'
    $persistedPath = Join-Path $output 'persisted-steam-byte-hashes.tsv'
    [IO.File]::WriteAllText($logPath, $log, [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText($filteredPath, $log, [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText($summaryPath, "fixture $id", [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText($packagePath, "Package [$packageName] versionName=fixture versionCode=500", [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText($localHashesPath, "sha256`tsizeBytes`tdevicePath`n", [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText($stateHashesPath, "sha256`tsizeBytes`tdevicePath`n", [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText($persistedPath, "documentPath`tmanifestRole`tsavePath`texists`thashKind`tsha256`n", [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText(
        (Join-Path $output 'private-storage-unavailable.txt'),
        'run-as unavailable; verified in-app recovery export is the Android byte authority',
        [Text.UTF8Encoding]::new($false)
    )

    $success = $phase -in @(
        'after-quit-sync', 'before-play-reconcile', 'after-modded-sync',
        'after-switch-to-beta', 'after-switch-to-public', 'retry-complete',
        'after-restart'
    )
    $conflict = $phase -in @('divergence-conflict', 'changed-mod-set-blocked')
    $pendingLog = $phase -in @(
        'divergence-conflict', 'offline-pending', 'pending-before-force-stop',
        'commit-failure', 'readback-failure'
    )
    $gates = [ordered]@{
        output = $output
        authorizedDevice = $true
        runAsAvailable = $false
        sourceCommit = $sourceCommit
        sourceWorktreeDirty = $false
        stage5Row = [string]$row
        evidencePhase = $phase
        logcatSince = '01-01 00:00:00.000'
        candidateApkSha256 = $CandidateApkSha256
        installedApkSha256 = $CandidateApkSha256
        candidateMatchesInstalled = $true
        localSaveByteHashCount = 0
        syncRecoveryStateFileHashCount = 0
        persistedSteamByteHashCount = 0
        persistedSteamLegacyTextHashCount = 0
        persistedSteamHashesAreLiveReadAtCapture = $false
        pendingSyncDocumentCount = 0
        pendingSyncPhases = @()
        recoveryJournalCount = 0
        automaticSyncPendingLogSeen = $pendingLog
        automaticSyncVerifiedLogSeen = $success
        automaticSyncConflictLogSeen = $conflict
        readBackMismatchSeen = $phase -eq 'readback-failure'
        commitFailureSeen = $phase -eq 'commit-failure'
        recoveryLogSeen = $phase -in @('after-restore', 'after-undo')
        localSaveBaseSeen = $false
        localSaveWrites = 0
        localSaveReads = 0
        localSaveExistsChecks = 0
        localOnlySaveManagerSeen = $false
        steamGameplaySaveManagerSeen = $false
        fatalExceptionSeen = $false
    }
    $inventoryPath = Join-Path $testRoot "collector-inventories/$id.json"
    $manifest = [ordered]@{
        schemaVersion = 2
        kind = 'stage5-android-save-validation-capture'
        capturedUtc = '2026-01-01T01:00:00.0000000+00:00'
        output = $output
        captureBinding = [ordered]@{
            sourceCommit = $sourceCommit
            sourceWorktreeDirty = $false
            candidateApkPath = $CandidateApkPath
            candidateApkSha256 = $CandidateApkSha256
            installedApkPath = "/data/app/$packageName/base.apk"
            installedApkSha256 = $CandidateApkSha256
            candidateMatchesInstalled = $true
            packageName = $packageName
            stage5Row = [string]$row
            evidencePhase = $phase
        }
        device = [ordered]@{
            serial = $deviceSerial
            serialSha256 = Get-Sha256Hex -Bytes ([Text.Encoding]::UTF8.GetBytes($deviceSerial))
            manufacturer = 'FixtureCo'
            model = 'Stage5Phone'
            androidApi = '36'
            abiList = 'arm64-v8a,armeabi-v7a'
            buildFingerprint = 'fixture/stage5/device:16/TEST/1:user/release-keys'
        }
        waitedSeconds = 0
        logcatSince = '01-01 00:00:00.000'
        clearedLogcatAfterPreservingBuffer = $false
        logcat = $logPath
        filteredLogcat = $filteredPath
        summary = $summaryPath
        package = $packagePath
        localSaveByteHashes = $localHashesPath
        syncRecoveryStateByteHashes = $stateHashesPath
        persistedSteamByteHashes = $persistedPath
        evidenceInventory = $inventoryPath
        evidenceLimitations = @('run-as unavailable; verified recovery export supplied separately')
        gates = $gates
    }
    $manifestPath = Join-Path $output 'manifest.json'
    Write-JsonNoBom -Path $manifestPath -Value $manifest
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $inventoryPath) | Out-Null
    & $inventoryScript `
        -EvidenceRoot $output `
        -OutputPath $inventoryPath `
        -SourceCommit $sourceCommit `
        -ApkSha256 $CandidateApkSha256 `
        -DeviceIdentity 'FixtureCo Stage5Phone; Android API 36; ABI arm64-v8a,armeabi-v7a' *> $null
    return [pscustomobject]@{
        ManifestPath = $manifestPath
        InventoryPath = $inventoryPath
        LogPath = $logPath
    }
}

function Assert-ReviewerRejects {
    param(
        [Parameter(Mandatory = $true)][string]$MatrixPath,
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][string]$Aapt,
        [Parameter(Mandatory = $true)][string]$ApkSigner
    )
    $rejected = $false
    try {
        & $reviewer -MatrixPath $MatrixPath -AaptPath $Aapt -ApkSignerPath $ApkSigner *> $null
    } catch {
        $rejected = $true
    }
    if (-not $rejected) { throw "$Label was accepted by the Stage 5 matrix reviewer." }
}

function New-FakeAndroidToolPair {
    param(
        [Parameter(Mandatory = $true)][string]$OutputDirectory,
        [Parameter(Mandatory = $true)][string]$PackageName,
        [Parameter(Mandatory = $true)][string]$VersionCode,
        [Parameter(Mandatory = $true)][string]$VersionName,
        [Parameter(Mandatory = $true)][string]$SignerSha256
    )

    $runningOnWindows = [Environment]::OSVersion.Platform -eq [PlatformID]::Win32NT
    $toolExtension = if ($runningOnWindows) { '.cmd' } else { '.sh' }
    $aapt = Join-Path $OutputDirectory "fake-aapt$toolExtension"
    $apkSigner = Join-Path $OutputDirectory "fake-apksigner$toolExtension"
    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

    if ($runningOnWindows) {
        [IO.File]::WriteAllLines($aapt, @(
            '@echo off',
            "echo package: name='$PackageName' versionCode='$VersionCode' versionName='$VersionName'",
            "echo native-code: 'arm64-v8a'",
            'exit /b 0'
        ), [Text.Encoding]::ASCII)
        [IO.File]::WriteAllLines($apkSigner, @(
            '@echo off',
            "echo Signer #1 certificate SHA-256 digest: $SignerSha256",
            'exit /b 0'
        ), [Text.Encoding]::ASCII)
    } else {
        [IO.File]::WriteAllLines($aapt, @(
            '#!/usr/bin/env sh',
            ('printf "%s\n" "package: name=' + "'$PackageName'" + ' versionCode=' + "'$VersionCode'" + ' versionName=' + "'$VersionName'" + '"'),
            'printf "%s\n" "native-code: ''arm64-v8a''"'
        ), [Text.UTF8Encoding]::new($false))
        [IO.File]::WriteAllLines($apkSigner, @(
            '#!/usr/bin/env sh',
            ('printf "%s\n" "Signer #1 certificate SHA-256 digest: ' + $SignerSha256 + '"')
        ), [Text.UTF8Encoding]::new($false))
        & chmod +x $aapt $apkSigner
        if ($LASTEXITCODE -ne 0) { throw 'Could not make fixture Android tools executable.' }
    }

    return [pscustomobject]@{
        Aapt = $aapt
        ApkSigner = $apkSigner
    }
}

function Assert-SelfConsistentLineageRejected {
    param(
        [Parameter(Mandatory = $true)][string]$ValidMatrixPath,
        [Parameter(Mandatory = $true)][string]$BaseBuildInfoPath,
        [Parameter(Mandatory = $true)][string]$FixtureRoot,
        [Parameter(Mandatory = $true)][string]$FixtureName,
        [Parameter(Mandatory = $true)][string]$MatrixProperty,
        [Parameter(Mandatory = $true)][string]$BuildInfoKey,
        [Parameter(Mandatory = $true)][string]$InvalidValue,
        [Parameter(Mandatory = $true)][string]$BuildInfoValue,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $invalidMatrix = Get-Content -LiteralPath $ValidMatrixPath -Raw | ConvertFrom-Json
    $invalidMatrix.binding.candidate.$MatrixProperty = $InvalidValue

    $fixtureDirectory = Join-Path $FixtureRoot "negative/lineage-$FixtureName"
    New-Item -ItemType Directory -Force -Path $fixtureDirectory | Out-Null
    $invalidBuildInfoPath = Join-Path $fixtureDirectory 'candidate.build-info.txt'
    $buildInfoLines = [Collections.Generic.List[string]]::new()
    $replacementCount = 0
    foreach ($line in [IO.File]::ReadAllLines($BaseBuildInfoPath, [Text.Encoding]::UTF8)) {
        if ($line.StartsWith("$BuildInfoKey=", [StringComparison]::Ordinal)) {
            $buildInfoLines.Add("$BuildInfoKey=$BuildInfoValue")
            $replacementCount++
        } else {
            $buildInfoLines.Add($line)
        }
    }
    if ($replacementCount -ne 1) {
        throw "Lineage fixture $FixtureName expected exactly one $BuildInfoKey build-info field."
    }
    [IO.File]::WriteAllLines(
        $invalidBuildInfoPath,
        $buildInfoLines,
        [Text.UTF8Encoding]::new($false)
    )
    $invalidMatrix.binding.candidate.buildInfoPath = $invalidBuildInfoPath
    $invalidMatrix.binding.candidate.buildInfoSha256 = Get-FileHashHex -Path $invalidBuildInfoPath

    $tools = New-FakeAndroidToolPair `
        -OutputDirectory (Join-Path $fixtureDirectory 'tools') `
        -PackageName ([string]$invalidMatrix.binding.candidate.packageName) `
        -VersionCode ([string]$invalidMatrix.binding.candidate.versionCode) `
        -VersionName ([string]$invalidMatrix.binding.candidate.versionName) `
        -SignerSha256 ([string]$invalidMatrix.binding.candidate.signerSha256)
    $invalidMatrixPath = Join-Path $fixtureDirectory 'matrix.json'
    Write-JsonNoBom -Path $invalidMatrixPath -Value $invalidMatrix
    Assert-ReviewerRejects `
        -MatrixPath $invalidMatrixPath `
        -Label $Label `
        -Aapt $tools.Aapt `
        -ApkSigner $tools.ApkSigner
}

New-Item -ItemType Directory -Path $testRoot | Out-Null
try {
    $candidatePath = Join-Path $testRoot 'candidate/Stage5-fixture.apk'
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $candidatePath) | Out-Null
    [IO.File]::WriteAllBytes($candidatePath, [Text.Encoding]::UTF8.GetBytes('fixture candidate apk bytes'))
    $candidateHash = Get-FileHashHex -Path $candidatePath
    $buildInfoPath = "$candidatePath.build-info.txt"
    $buildInfoLines = @(
        'version_name=0.2.417-stage5-fixture',
        'version_code=417003',
        "package_name=$packageName",
        'abi=arm64-v8a',
        'signing_channel=release',
        "signer_sha256=$($signerSha256.ToUpperInvariant())",
        'release_tag=v0.2.417-stage5-fixture',
        "source_commit=$sourceCommit",
        'candidate_run_id=123456789',
        'candidate_run_attempt=1',
        "apk_sha256=$candidateHash",
        'update_baseline_tag=v0.2.416-startup-recovery-ime',
        'update_baseline_asset_name=StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk',
        'update_baseline_apk_sha256=fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b'
    )
    [IO.File]::WriteAllLines($buildInfoPath, $buildInfoLines, [Text.UTF8Encoding]::new($false))

    $validTools = New-FakeAndroidToolPair `
        -OutputDirectory (Join-Path $testRoot 'tools') `
        -PackageName $packageName `
        -VersionCode '417003' `
        -VersionName '0.2.417-stage5-fixture' `
        -SignerSha256 $signerSha256
    $aaptPath = $validTools.Aapt
    $apkSignerPath = $validTools.ApkSigner

    $vA = New-State -Namespace vanilla -ProfileLabel 'A' -ProgressLabel 'A'
    $vB = New-State -Namespace vanilla -ProfileLabel 'B' -ProgressLabel 'B'
    $vC = New-State -Namespace vanilla -ProfileLabel 'C' -ProgressLabel 'C'
    $betaB = New-State -Namespace vanilla -ProfileLabel 'BETA' -ProgressLabel 'BETA'
    $modOld = New-State -Namespace modded -ProfileLabel 'MOD-SHARED' -ProgressLabel 'MOD-OLD'
    $modNew = New-State -Namespace modded -ProfileLabel 'MOD-SHARED' -ProgressLabel 'MOD-NEW'
    $modChanged = New-State -Namespace modded -ProfileLabel 'MOD-CHANGED' -ProgressLabel 'MOD-CHANGED'
    $r4Vanilla = New-State -Namespace vanilla -ProfileLabel 'MOD-SHARED' -ProgressLabel 'VANILLA-HOLD'
    $r4Before = Merge-State -States @($r4Vanilla, $modOld)
    $r4After = Merge-State -States @($r4Vanilla, $modNew)

    $androidBases = [ordered]@{}
    $androidBases.vA = New-AndroidBase -Key 'vA' -ContextId 'vanilla-public' -State $vA
    $androidBases.vB = New-AndroidBase -Key 'vB' -ContextId 'vanilla-public' -State $vB
    $androidBases.vC = New-AndroidBase -Key 'vC' -ContextId 'vanilla-public' -State $vC
    $androidBases.vBGamePending = New-AndroidBase -Key 'vB-game-pending' -ContextId 'vanilla-public' -State $vB -PendingPhase 'game-running'
    $androidBases.vBUploadPending = New-AndroidBase -Key 'vB-upload-pending' -ContextId 'vanilla-public' -State $vB -PendingPhase 'uploading'
    $androidBases.row2After = New-AndroidBase -Key 'row2-after-with-backup' -ContextId 'vanilla-public' -State $vB -RecoveryState $vA
    $androidBases.modNew = New-AndroidBase -Key 'mod-new' -ContextId 'modded-exact' -State $modNew
    $androidBases.modChanged = New-AndroidBase -Key 'mod-changed' -ContextId 'modded-changed' -State $modChanged
    $androidBases.betaB = New-AndroidBase -Key 'beta-B' -ContextId 'vanilla-public-beta' -State $betaB
    $androidBases.restored = New-AndroidBase -Key 'restored' -ContextId 'vanilla-public' -State $vC -CloudSyncEnabled:$false -RecoveryPhase 'validation-required'
    $androidBases.undone = New-AndroidBase -Key 'undone' -ContextId 'vanilla-public' -State $vA -CloudSyncEnabled:$false -RecoveryPhase 'undone'

    $steamBases = [ordered]@{}
    $steamBases.vA = New-SteamBase -Key 'vA' -ContextId 'vanilla-public' -State $vA
    $steamBases.vB = New-SteamBase -Key 'vB' -ContextId 'vanilla-public' -State $vB
    $steamBases.vC = New-SteamBase -Key 'vC' -ContextId 'vanilla-public' -State $vC
    $steamBases.betaB = New-SteamBase -Key 'beta-B' -ContextId 'vanilla-public-beta' -State $betaB
    $steamBases.modOld = New-SteamBase -Key 'mod-old' -ContextId 'modded-exact' -State $modOld
    $steamBases.r4Before = New-SteamBase -Key 'r4-before' -ContextId 'modded-exact' -State $r4Before
    $steamBases.r4After = New-SteamBase -Key 'r4-after' -ContextId 'modded-exact' -State $r4After

    $androidMapping = [ordered]@{
        'r1-android-after' = 'vB'
        'r2-android-before' = 'vA'; 'r2-android-after' = 'row2After'
        'r3-baseline-android' = 'vA'; 'r3-local-before' = 'vB'; 'r3-local-after' = 'vBGamePending'
        'r4-android-after' = 'modNew'
        'r5-local-before' = 'modChanged'; 'r5-local-after' = 'modChanged'
        'r6-public-before' = 'vA'; 'r6-beta-before' = 'betaB'; 'r6-beta-after' = 'betaB'; 'r6-public-after' = 'vA'
        'r7-baseline-android' = 'vA'; 'r7-local-offline' = 'vBUploadPending'; 'r7-local-after-retry' = 'vB'
        'r8-baseline-android' = 'vA'; 'r8-local-before-crash' = 'vBUploadPending'; 'r8-local-after-restart' = 'vB'
        'r9-commit-local-before' = 'vB'; 'r9-commit-local-after' = 'vBUploadPending'
        'r9-readback-local-before' = 'vB'; 'r9-readback-local-after' = 'vBUploadPending'
        'r10-android-original' = 'vA'; 'r10-android-restored' = 'restored'; 'r10-android-undone' = 'undone'
    }
    $steamMapping = [ordered]@{
        'r1-steam-before' = 'vA'; 'r1-steam-after' = 'vB'
        'r2-steam-before' = 'vB'; 'r2-steam-after' = 'vB'
        'r3-baseline-steam' = 'vA'; 'r3-remote-before' = 'vC'; 'r3-remote-after' = 'vC'
        'r4-steam-before' = 'r4Before'; 'r4-steam-after' = 'r4After'
        'r5-steam-before' = 'modOld'; 'r5-steam-after' = 'modOld'
        'r6-beta-steam-after' = 'betaB'; 'r6-public-steam-after' = 'vA'
        'r7-baseline-steam' = 'vA'; 'r7-remote-offline' = 'vA'; 'r7-remote-after-retry' = 'vB'
        'r8-baseline-steam' = 'vA'; 'r8-remote-before-restart' = 'vA'; 'r8-remote-after-restart' = 'vB'
        'r9-commit-steam-before' = 'vA'; 'r9-commit-steam-after' = 'vA'
        'r9-readback-steam-before' = 'vA'; 'r9-readback-steam-after' = 'vC'
        'r10-steam-before' = 'vA'; 'r10-steam-after-restore' = 'vA'; 'r10-steam-after-undo' = 'vA'
    }

    $templatePath = Join-Path $testRoot 'matrix-template.json'
    & $templateScript -OutputPath $templatePath *> $null
    $matrix = Get-Content -LiteralPath $templatePath -Raw | ConvertFrom-Json
    $matrix.binding.candidate.apkPath = $candidatePath
    $matrix.binding.candidate.apkSha256 = $candidateHash
    $matrix.binding.candidate.buildInfoPath = $buildInfoPath
    $matrix.binding.candidate.buildInfoSha256 = Get-FileHashHex -Path $buildInfoPath
    $matrix.binding.candidate.sourceCommit = $sourceCommit
    $matrix.binding.candidate.candidateRunId = '123456789'
    $matrix.binding.candidate.candidateRunAttempt = '1'
    $matrix.binding.candidate.signerSha256 = $signerSha256
    $matrix.binding.candidate.packageName = $packageName
    $matrix.binding.candidate.versionName = '0.2.417-stage5-fixture'
    $matrix.binding.candidate.versionCode = '417003'
    $matrix.binding.candidate.releaseTag = 'v0.2.417-stage5-fixture'
    $matrix.binding.candidate.updateBaselineTag = 'v0.2.416-startup-recovery-ime'
    $matrix.binding.candidate.updateBaselineAssetName = 'StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk'
    $matrix.binding.candidate.updateBaselineApkSha256 = 'fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b'
    $matrix.binding.steamId64 = $steamId64
    $matrix.binding.device.serialSha256 = Get-Sha256Hex -Bytes ([Text.Encoding]::UTF8.GetBytes($deviceSerial))
    $matrix.binding.device.manufacturer = 'FixtureCo'
    $matrix.binding.device.model = 'Stage5Phone'
    $matrix.binding.device.androidApi = '36'
    $matrix.binding.device.abiList = 'arm64-v8a,armeabi-v7a'
    $matrix.binding.device.buildFingerprint = 'fixture/stage5/device:16/TEST/1:user/release-keys'
    foreach ($expected in $matrix.contexts) {
        $source = $contexts[[string]$expected.id]
        $expected.steamId64 = [string]$source.SteamId64
        $expected.saveNamespace = [string]$source.SaveNamespace
        $expected.runtimeIdentity = [string]$source.RuntimeIdentity
        $expected.modSetFingerprint = [string]$source.ModSetFingerprint
    }

    $collectorFixtures = [ordered]@{}
    foreach ($spec in $matrix.evidence) {
        if ([string]$spec.kind -eq 'android-manifest') {
            $baseKey = [string]$androidMapping[[string]$spec.id]
            if (-not $baseKey) { throw "No Android fixture mapping for $($spec.id)." }
            $destination = Join-Path $testRoot ([string]$spec.path)
            Copy-JsonEvidence -Source $androidBases[$baseKey] -Destination $destination
            $spec.sha256 = Get-FileHashHex -Path $destination
        } elseif ([string]$spec.kind -eq 'steam-manifest') {
            $baseKey = [string]$steamMapping[[string]$spec.id]
            if (-not $baseKey) { throw "No Steam fixture mapping for $($spec.id)." }
            $destination = Join-Path $testRoot ([string]$spec.path)
            Copy-JsonEvidence -Source $steamBases[$baseKey] -Destination $destination
            $spec.sha256 = Get-FileHashHex -Path $destination
        } else {
            $fixture = New-CollectorEvidence -Spec $spec -CandidateApkPath $candidatePath -CandidateApkSha256 $candidateHash
            $collectorFixtures[[string]$spec.id] = $fixture
            $spec.path = $fixture.ManifestPath
            $spec.sha256 = Get-FileHashHex -Path $fixture.ManifestPath
            $spec.inventory.path = $fixture.InventoryPath
            $spec.inventory.sha256 = Get-FileHashHex -Path $fixture.InventoryPath
        }
    }

    $row2After = Get-Content -LiteralPath $androidBases.row2After -Raw | ConvertFrom-Json
    $row2Check = @($matrix.rows | Where-Object { [int]$_.row -eq 2 })[0].checks[0]
    $row2Check.refs.destinationBackupTreeSha256 = [string]$row2After.recoverySnapshots[0].treeSha256

    $validMatrixPath = Join-Path $testRoot 'valid-matrix.json'
    Write-JsonNoBom -Path $validMatrixPath -Value $matrix
    $reviewPath = Join-Path $testRoot 'valid-review.json'
    & $reviewer `
        -MatrixPath $validMatrixPath `
        -OutputPath $reviewPath `
        -AaptPath $aaptPath `
        -ApkSignerPath $apkSignerPath *> $null
    $review = Get-Content -LiteralPath $reviewPath -Raw | ConvertFrom-Json
    if ($review.result -ne 'passed' -or $review.rowsPassed -ne 10 -or $review.semanticChecksPassed -ne 11) {
        throw 'Valid nondebuggable Stage 5 fixture did not produce a complete pass report.'
    }

    $lineageCases = @(
        [pscustomobject]@{
            FixtureName = 'dev-package'
            MatrixProperty = 'packageName'
            BuildInfoKey = 'package_name'
            InvalidValue = 'com.sts2launcher.overhaul.fork.dev'
            BuildInfoValue = 'com.sts2launcher.overhaul.fork.dev'
            Label = 'Self-consistent .dev package lineage'
        },
        [pscustomobject]@{
            FixtureName = 'wrong-signer'
            MatrixProperty = 'signerSha256'
            BuildInfoKey = 'signer_sha256'
            InvalidValue = 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'
            BuildInfoValue = 'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA'
            Label = 'Self-consistent wrong signer lineage'
        },
        [pscustomobject]@{
            FixtureName = 'old-version'
            MatrixProperty = 'versionCode'
            BuildInfoKey = 'version_code'
            InvalidValue = '416001'
            BuildInfoValue = '416001'
            Label = 'Self-consistent version at the published baseline floor'
        },
        [pscustomobject]@{
            FixtureName = 'wrong-baseline-tag'
            MatrixProperty = 'updateBaselineTag'
            BuildInfoKey = 'update_baseline_tag'
            InvalidValue = 'v0.2.415-wrong-baseline'
            BuildInfoValue = 'v0.2.415-wrong-baseline'
            Label = 'Self-consistent wrong update-baseline tag'
        },
        [pscustomobject]@{
            FixtureName = 'wrong-baseline-asset'
            MatrixProperty = 'updateBaselineAssetName'
            BuildInfoKey = 'update_baseline_asset_name'
            InvalidValue = 'StS2Launcher-wrong-local-arm64-v8a.apk'
            BuildInfoValue = 'StS2Launcher-wrong-local-arm64-v8a.apk'
            Label = 'Self-consistent wrong update-baseline asset'
        },
        [pscustomobject]@{
            FixtureName = 'wrong-baseline-hash'
            MatrixProperty = 'updateBaselineApkSha256'
            BuildInfoKey = 'update_baseline_apk_sha256'
            InvalidValue = '0000000000000000000000000000000000000000000000000000000000000000'
            BuildInfoValue = '0000000000000000000000000000000000000000000000000000000000000000'
            Label = 'Self-consistent wrong update-baseline bytes'
        }
    )
    foreach ($lineageCase in $lineageCases) {
        Assert-SelfConsistentLineageRejected `
            -ValidMatrixPath $validMatrixPath `
            -BaseBuildInfoPath $buildInfoPath `
            -FixtureRoot $testRoot `
            -FixtureName $lineageCase.FixtureName `
            -MatrixProperty $lineageCase.MatrixProperty `
            -BuildInfoKey $lineageCase.BuildInfoKey `
            -InvalidValue $lineageCase.InvalidValue `
            -BuildInfoValue $lineageCase.BuildInfoValue `
            -Label $lineageCase.Label
    }

    $missingMatrix = Get-Content -LiteralPath $validMatrixPath -Raw | ConvertFrom-Json
    $row9 = @($missingMatrix.rows | Where-Object { [int]$_.row -eq 9 })[0]
    $row9.checks = @($row9.checks | Where-Object { [string]$_.id -ne 'r9-readback-failure' })
    $missingPath = Join-Path $testRoot 'missing-row9-subcase.json'
    Write-JsonNoBom -Path $missingPath -Value $missingMatrix
    Assert-ReviewerRejects -MatrixPath $missingPath -Label 'Missing row-9 read-back subcase' -Aapt $aaptPath -ApkSigner $apkSignerPath

    $tamperedMatrix = Get-Content -LiteralPath $validMatrixPath -Raw | ConvertFrom-Json
    $tamperedSpec = @($tamperedMatrix.evidence | Where-Object { [string]$_.id -eq 'r1-android-after' })[0]
    $tamperedFile = Join-Path $testRoot 'negative/tampered-android-manifest.json'
    Copy-JsonEvidence -Source (Resolve-Path (Join-Path $testRoot ([string]$tamperedSpec.path))).Path -Destination $tamperedFile
    [IO.File]::AppendAllText($tamperedFile, 'tampered', [Text.UTF8Encoding]::new($false))
    $tamperedSpec.path = $tamperedFile
    $tamperedPath = Join-Path $testRoot 'tampered-evidence.json'
    Write-JsonNoBom -Path $tamperedPath -Value $tamperedMatrix
    Assert-ReviewerRejects -MatrixPath $tamperedPath -Label 'Tampered recorded evidence' -Aapt $aaptPath -ApkSigner $apkSignerPath

    $caseRoot = Join-Path $testRoot 'case-variant-steam-root'
    $caseMarker = Join-Path $caseRoot '.sts2-launcher/contexts/vanilla.json'
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $caseMarker) | Out-Null
    [IO.File]::WriteAllText((Join-Path $caseRoot 'Profile.save'), 'case variant', [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText($caseMarker, (New-ContextMarker -Context $contexts['vanilla-public']), [Text.UTF8Encoding]::new($false))
    $caseSyncEvidence = Join-Path $testRoot 'case-variant-steam-sync.txt'
    [IO.File]::WriteAllText($caseSyncEvidence, 'fixture sync', [Text.UTF8Encoding]::new($false))
    $caseOutput = Join-Path $testRoot 'case-variant-steam-manifest.json'
    $caseRejected = $false
    try {
        & $steamCapture `
            -SteamCloudRoot $caseRoot `
            -SelectedNamespace Vanilla `
            -ExpectedSteamId64 $steamId64 `
            -ExpectedRuntimeIdentity public `
            -ExpectedModSetFingerprint '' `
            -SteamSyncEvidencePath $caseSyncEvidence `
            -CaptureMethod 'case-variant fixture' `
            -RetainedImmutableSource `
            -OutputPath $caseOutput *> $null
    } catch {
        $caseRejected = $true
    }
    if (-not $caseRejected -or (Test-Path -LiteralPath $caseOutput)) {
        throw 'Case-variant Steam Cloud path was not rejected before manifest creation.'
    }

    $unexpectedFile = Join-Path $testRoot 'steam-roots/vA/device-settings.json'
    [IO.File]::WriteAllText($unexpectedFile, '{}', [Text.UTF8Encoding]::new($false))
    Assert-ReviewerRejects -MatrixPath $validMatrixPath -Label 'Unexpected Steam Cloud file' -Aapt $aaptPath -ApkSigner $apkSignerPath
    Remove-Item -LiteralPath $unexpectedFile -Force

    $tamperedRemotePath = Join-Path $testRoot 'steam-roots/vA/profile1/saves/progress.save'
    $originalRemoteBytes = [IO.File]::ReadAllBytes($tamperedRemotePath)
    [IO.File]::WriteAllBytes($tamperedRemotePath, [Text.Encoding]::UTF8.GetBytes('post-capture tamper'))
    Assert-ReviewerRejects -MatrixPath $validMatrixPath -Label 'Post-capture retained Steam tamper' -Aapt $aaptPath -ApkSigner $apkSignerPath
    [IO.File]::WriteAllBytes($tamperedRemotePath, $originalRemoteBytes)

    $row3Collector = $collectorFixtures['r3-divergence-conflict']
    [IO.File]::AppendAllText($row3Collector.LogPath, "`nSynced", [Text.UTF8Encoding]::new($false))
    $resolvedInventory = [IO.Path]::GetFullPath($row3Collector.InventoryPath)
    $resolvedTestRoot = [IO.Path]::GetFullPath($testRoot).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedInventory.StartsWith($resolvedTestRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Refusing to replace a fixture inventory outside the Stage 5 test root.'
    }
    Remove-Item -LiteralPath $resolvedInventory -Force
    & $inventoryScript `
        -EvidenceRoot (Split-Path -Parent $row3Collector.ManifestPath) `
        -OutputPath $resolvedInventory `
        -SourceCommit $sourceCommit `
        -ApkSha256 $candidateHash `
        -DeviceIdentity 'FixtureCo Stage5Phone; Android API 36; ABI arm64-v8a,armeabi-v7a' *> $null
    $falseSyncedMatrix = Get-Content -LiteralPath $validMatrixPath -Raw | ConvertFrom-Json
    $falseSyncedSpec = @($falseSyncedMatrix.evidence | Where-Object { [string]$_.id -eq 'r3-divergence-conflict' })[0]
    $falseSyncedSpec.inventory.sha256 = Get-FileHashHex -Path $resolvedInventory
    $falseSyncedPath = Join-Path $testRoot 'false-synced.json'
    Write-JsonNoBom -Path $falseSyncedPath -Value $falseSyncedMatrix
    Assert-ReviewerRejects -MatrixPath $falseSyncedPath -Label 'Conflict with false Synced log' -Aapt $aaptPath -ApkSigner $apkSignerPath

    Write-Host 'Stage 5 physical matrix reviewer tests passed: 13/13'
} finally {
    $resolvedTestRoot = [IO.Path]::GetFullPath($testRoot)
    $tempPrefix = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd(
        [IO.Path]::DirectorySeparatorChar
    ) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedTestRoot.StartsWith($tempPrefix, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolvedTestRoot) -notmatch '^sts2-stage5-matrix-review-test-[0-9a-f]{32}$') {
        throw "Refusing to clean unexpected Stage 5 fixture root: $resolvedTestRoot"
    }
    if (Test-Path -LiteralPath $resolvedTestRoot) {
        Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
    }
}
