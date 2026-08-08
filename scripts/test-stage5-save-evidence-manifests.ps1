$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$androidVerifier = Join-Path $PSScriptRoot "verify-stage5-android-save-bundle.ps1"
$steamCapture = Join-Path $PSScriptRoot "new-stage5-live-steam-manifest.ps1"
$testRoot = Join-Path ([IO.Path]::GetTempPath()) (
    "sts2-stage5-save-evidence-test-" + [Guid]::NewGuid().ToString("N")
)

function Get-Sha256Hex {
    param([Parameter(Mandatory = $true)][byte[]]$Bytes)

    $sha256 = [Security.Cryptography.SHA256]::Create()
    try {
        return (($sha256.ComputeHash($Bytes) | ForEach-Object {
            $_.ToString("x2")
        }) -join "")
    } finally {
        $sha256.Dispose()
    }
}

function Write-JsonNoBom {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)]$Value
    )

    $parent = Split-Path -Parent $Path
    if ($parent) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }
    [IO.File]::WriteAllText(
        $Path,
        ($Value | ConvertTo-Json -Depth 20),
        [Text.UTF8Encoding]::new($false)
    )
}

function Add-SnapshotEntry {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [byte[]]$Bytes,
        [Parameter(Mandatory = $true)]$Manifest,
        [Parameter(Mandatory = $true)]$Files
    )

    if ($null -eq $Bytes) {
        $Manifest.Add([ordered]@{
            Path = $Path
            Exists = $false
            Sha256 = ""
            ByteSha256 = ""
        })
        return
    }
    $hash = Get-Sha256Hex -Bytes $Bytes
    $Manifest.Add([ordered]@{
        Path = $Path
        Exists = $true
        Sha256 = $hash
        ByteSha256 = $hash
    })
    $Files.Add([ordered]@{
        Path = $Path
        ContentBase64 = [Convert]::ToBase64String($Bytes)
        ByteSha256 = $hash
    })
}

New-Item -ItemType Directory -Path $testRoot | Out-Null
try {
    $specialBytes = [byte[]](0xef, 0xbb, 0xbf) +
        [Text.Encoding]::UTF8.GetBytes("{`"ironclad`":10}`r`n") +
        [byte[]](0)
    $recoverySpecialBytes = [byte[]](0xef, 0xbb, 0xbf) +
        [Text.Encoding]::UTF8.GetBytes("{`"ironclad`":7}`r`n") +
        [byte[]](0)

    $bundlePath = Join-Path $testRoot "current-save-bundle.json"
    $androidManifestPath = Join-Path $testRoot "android-manifest.json"
    $entries = [Collections.Generic.List[object]]::new()
    $files = [Collections.Generic.List[object]]::new()
    Add-SnapshotEntry -Path 'profile.save' -Bytes $specialBytes -Manifest $entries -Files $files
    foreach ($profileId in 1..3) {
        foreach ($name in @(
            'progress.save',
            'prefs',
            'prefs.save',
            'current_run.save',
            'current_run_mp.save'
        )) {
            $bytes = if ($profileId -eq 1 -and $name -eq 'progress.save') {
                $specialBytes
            } else {
                $null
            }
            Add-SnapshotEntry `
                -Path "profile${profileId}/saves/$name" `
                -Bytes $bytes `
                -Manifest $entries `
                -Files $files
        }
    }
    $recoveryEntries = [Collections.Generic.List[object]]::new()
    $recoveryFiles = [Collections.Generic.List[object]]::new()
    Add-SnapshotEntry `
        -Path 'profile.save' `
        -Bytes $recoverySpecialBytes `
        -Manifest $recoveryEntries `
        -Files $recoveryFiles
    foreach ($profileId in 1..3) {
        foreach ($name in @(
            'progress.save',
            'prefs',
            'prefs.save',
            'current_run.save',
            'current_run_mp.save'
        )) {
            $bytes = if ($profileId -eq 1 -and $name -eq 'progress.save') {
                $recoverySpecialBytes
            } else {
                $null
            }
            Add-SnapshotEntry `
                -Path "profile${profileId}/saves/$name" `
                -Bytes $bytes `
                -Manifest $recoveryEntries `
                -Files $recoveryFiles
        }
    }
    $recoverySnapshotId = Get-Sha256Hex -Bytes $recoverySpecialBytes
    $contextMarker = [ordered]@{
        Version = 1
        SteamId64 = 76561198000000001
        SaveNamespace = 'vanilla'
        RuntimeIdentity = 'public'
        ModSetFingerprint = ''
    } | ConvertTo-Json -Compress
    $pendingJson = [ordered]@{
        Version = 1
        Phase = 'game-running'
        ContextMarker = $contextMarker
    } | ConvertTo-Json -Compress
    $baselineJson = [ordered]@{
        Version = 1
        ContextMarker = $contextMarker
    } | ConvertTo-Json -Compress
    $recoveryJournalJson = [ordered]@{
        Version = 2
        Phase = 'validation-required'
        SaveNamespace = 'vanilla'
        RuntimeIdentity = 'public'
        ModSetFingerprint = ''
        TargetContextMarker = $contextMarker
        SourceSnapshotSha256 = $recoverySnapshotId
        UndoSnapshotSha256 = $recoverySnapshotId
        AppliedSnapshotSha256 = $recoverySnapshotId
    } | ConvertTo-Json -Compress
    $bundle = [ordered]@{
        Version = 2
        OriginalSourcesWereModified = $false
        SteamWasContacted = $false
        CloudSyncEnabled = $true
        SelectedSaveContext = [ordered]@{
            SteamId64 = 76561198000000001
            SaveNamespace = 'Vanilla'
            RuntimeIdentity = 'public'
            ModSetFingerprint = ''
        }
        CurrentAndroidSnapshot = [ordered]@{
            Version = 2
            ContextMarker = $contextMarker
            Coverage = 'full'
            SourceKind = 'local-recovery'
            SourceLabel = 'support-export-current-android'
            CapturedUtc = [DateTime]::UtcNow.ToString('o')
            Manifest = [ordered]@{ Version = 1; Entries = @($entries) }
            Files = @($files)
        }
        RecoverySnapshots = @([ordered]@{
            CandidateId = $recoverySnapshotId
            SourceKind = 'transfer-backup'
            SourceLabel = 'pre-download destination backup'
            Classification = 'exact-context'
            SnapshotPath = '.sts2-launcher/recovery/imported/fixture.json'
            SnapshotSha256 = $recoverySnapshotId
            Snapshot = [ordered]@{
                Version = 2
                Coverage = 'full'
                SourceKind = 'transfer-backup'
                SourceLabel = 'pre-download destination backup'
                CapturedUtc = [DateTime]::UtcNow.ToString('o')
                Manifest = [ordered]@{
                    Version = 1
                    Entries = @($recoveryEntries)
                }
                Files = @($recoveryFiles)
            }
        })
        RecoveryHoldJson = ''
        RecoveryJournalJson = $recoveryJournalJson
        AutomaticSyncPendingJson = $pendingJson
        AutomaticSyncBaselineJson = $baselineJson
        AutomaticSyncBeforeGameSnapshotJson = ''
    }
    Write-JsonNoBom -Path $bundlePath -Value $bundle

    & $androidVerifier `
        -BundlePath $bundlePath `
        -OutputPath $androidManifestPath `
        -Namespace Vanilla `
        -ExpectedSteamId64 '76561198000000001' `
        -ExpectedRuntimeIdentity public `
        -ExpectedModSetFingerprint '' `
        -RequireCloudSyncEnabled
    $androidManifest = Get-Content -LiteralPath $androidManifestPath -Raw |
        ConvertFrom-Json
    if ($androidManifest.kind -ne 'stage5-android-save-manifest' -or
        $androidManifest.snapshot.files.Count -ne 16 -or
        $androidManifest.recoverySnapshots.Count -ne 1 -or
        -not $androidManifest.launcherState.pending.present -or
        $androidManifest.launcherState.pending.phase -ne 'game-running' -or
        $androidManifest.launcherState.pending.context.steamId64 -ne
            '76561198000000001' -or
        -not $androidManifest.launcherState.baseline.present -or
        $androidManifest.launcherState.recoveryHold.present -or
        $androidManifest.launcherState.recoveryJournal.phase -ne
            'validation-required' -or
        $androidManifest.recoverySnapshots[0].treeSha256 -eq
            $androidManifest.snapshot.treeSha256 -or
        @($androidManifest.recoverySnapshots[0].files | Where-Object {
            $_.exists -and $_.sha256 -eq
                (Get-Sha256Hex -Bytes $recoverySpecialBytes)
        }).Count -ne 2 -or
        @($androidManifest.snapshot.files | Where-Object {
            $_.exists -and $_.sha256 -eq (Get-Sha256Hex -Bytes $specialBytes)
        }).Count -ne 2) {
        throw "Android recovery-bundle verifier did not preserve current and recovery raw bytes/tombstones."
    }

    $badAndroidBundlePath = Join-Path $testRoot 'bad-android-context-bundle.json'
    $badAndroidOutputPath = Join-Path $testRoot 'bad-android-context-manifest.json'
    $badAndroidBundle = Get-Content -LiteralPath $bundlePath -Raw | ConvertFrom-Json
    $badAndroidBundle.CurrentAndroidSnapshot.ContextMarker = ([ordered]@{
        Version = 1
        SteamId64 = 76561198000000001
        SaveNamespace = 'vanilla'
        RuntimeIdentity = 'public-beta'
        ModSetFingerprint = ''
    } | ConvertTo-Json -Compress)
    Write-JsonNoBom -Path $badAndroidBundlePath -Value $badAndroidBundle
    $androidContextRejected = $false
    try {
        & $androidVerifier `
            -BundlePath $badAndroidBundlePath `
            -OutputPath $badAndroidOutputPath `
            -Namespace Vanilla `
            -ExpectedSteamId64 '76561198000000001' `
            -ExpectedRuntimeIdentity public `
            -ExpectedModSetFingerprint '' `
            -RequireCloudSyncEnabled
    } catch {
        $androidContextRejected = $true
    }
    if (-not $androidContextRejected -or
        (Test-Path -LiteralPath $badAndroidOutputPath)) {
        throw "Android recovery-bundle verifier did not reject a snapshot context mismatch."
    }

    foreach ($propertyName in @(
        'OriginalSourcesWereModified',
        'SteamWasContacted',
        'CloudSyncEnabled'
    )) {
        $missingBooleanBundlePath = Join-Path $testRoot (
            "missing-$($propertyName.ToLowerInvariant())-bundle.json"
        )
        $missingBooleanOutputPath = Join-Path $testRoot (
            "missing-$($propertyName.ToLowerInvariant())-manifest.json"
        )
        $missingBooleanBundle = Get-Content -LiteralPath $bundlePath -Raw |
            ConvertFrom-Json
        $missingBooleanBundle.PSObject.Properties.Remove($propertyName)
        Write-JsonNoBom `
            -Path $missingBooleanBundlePath `
            -Value $missingBooleanBundle
        $missingBooleanRejected = $false
        try {
            & $androidVerifier `
                -BundlePath $missingBooleanBundlePath `
                -OutputPath $missingBooleanOutputPath `
                -Namespace Vanilla `
                -ExpectedSteamId64 '76561198000000001' `
                -ExpectedRuntimeIdentity public `
                -ExpectedModSetFingerprint '' `
                -RequireCloudSyncEnabled
        } catch {
            $missingBooleanRejected = $true
        }
        if (-not $missingBooleanRejected -or
            (Test-Path -LiteralPath $missingBooleanOutputPath)) {
            throw "Android recovery-bundle verifier did not reject missing $propertyName."
        }

        $stringBooleanBundlePath = Join-Path $testRoot (
            "string-$($propertyName.ToLowerInvariant())-bundle.json"
        )
        $stringBooleanOutputPath = Join-Path $testRoot (
            "string-$($propertyName.ToLowerInvariant())-manifest.json"
        )
        $stringBooleanBundle = Get-Content -LiteralPath $bundlePath -Raw |
            ConvertFrom-Json
        $stringBooleanBundle.$propertyName = 'false'
        Write-JsonNoBom `
            -Path $stringBooleanBundlePath `
            -Value $stringBooleanBundle
        $stringBooleanRejected = $false
        try {
            & $androidVerifier `
                -BundlePath $stringBooleanBundlePath `
                -OutputPath $stringBooleanOutputPath `
                -Namespace Vanilla `
                -ExpectedSteamId64 '76561198000000001' `
                -ExpectedRuntimeIdentity public `
                -ExpectedModSetFingerprint '' `
                -RequireCloudSyncEnabled
        } catch {
            $stringBooleanRejected = $true
        }
        if (-not $stringBooleanRejected -or
            (Test-Path -LiteralPath $stringBooleanOutputPath)) {
            throw "Android recovery-bundle verifier accepted non-boolean $propertyName."
        }
    }

    foreach ($versionTarget in @(
        'bundle',
        'current-snapshot',
        'recovery-snapshot',
        'before-game-snapshot'
    )) {
        $versionThreeBundlePath = Join-Path $testRoot (
            "version-3-$versionTarget-bundle.json"
        )
        $versionThreeOutputPath = Join-Path $testRoot (
            "version-3-$versionTarget-manifest.json"
        )
        $versionThreeBundle = Get-Content -LiteralPath $bundlePath -Raw |
            ConvertFrom-Json
        switch ($versionTarget) {
            'bundle' {
                $versionThreeBundle.Version = 3
            }
            'current-snapshot' {
                $versionThreeBundle.CurrentAndroidSnapshot.Version = 3
            }
            'recovery-snapshot' {
                $versionThreeBundle.RecoverySnapshots[0].Snapshot.Version = 3
            }
            'before-game-snapshot' {
                $beforeGameSnapshot = Get-Content -LiteralPath $bundlePath -Raw |
                    ConvertFrom-Json |
                    Select-Object -ExpandProperty CurrentAndroidSnapshot
                $beforeGameSnapshot.Version = 3
                $versionThreeBundle.AutomaticSyncBeforeGameSnapshotJson =
                    $beforeGameSnapshot | ConvertTo-Json -Depth 20 -Compress
            }
        }
        Write-JsonNoBom `
            -Path $versionThreeBundlePath `
            -Value $versionThreeBundle
        $versionThreeRejected = $false
        try {
            & $androidVerifier `
                -BundlePath $versionThreeBundlePath `
                -OutputPath $versionThreeOutputPath `
                -Namespace Vanilla `
                -ExpectedSteamId64 '76561198000000001' `
                -ExpectedRuntimeIdentity public `
                -ExpectedModSetFingerprint '' `
                -RequireCloudSyncEnabled
        } catch {
            $versionThreeRejected = $true
        }
        if (-not $versionThreeRejected -or
            (Test-Path -LiteralPath $versionThreeOutputPath)) {
            throw "Android recovery-bundle verifier accepted Version 3 for $versionTarget."
        }
    }

    $restoreBundlePath = Join-Path $testRoot 'restore-held-bundle.json'
    $restoreManifestPath = Join-Path $testRoot 'restore-held-manifest.json'
    $restoreBundle = Get-Content -LiteralPath $bundlePath -Raw | ConvertFrom-Json
    $restoreBundle.CloudSyncEnabled = $false
    $restoreBundle.AutomaticSyncPendingJson = ''
    Write-JsonNoBom -Path $restoreBundlePath -Value $restoreBundle
    & $androidVerifier `
        -BundlePath $restoreBundlePath `
        -OutputPath $restoreManifestPath `
        -Namespace Vanilla `
        -ExpectedSteamId64 '76561198000000001' `
        -ExpectedRuntimeIdentity public `
        -ExpectedModSetFingerprint ''
    $restoreManifest = Get-Content -LiteralPath $restoreManifestPath -Raw |
        ConvertFrom-Json
    if ($restoreManifest.cloudSyncEnabled -or
        $restoreManifest.launcherState.pending.present -or
        $restoreManifest.launcherState.recoveryJournal.phase -ne
            'validation-required') {
        throw "Android verifier did not preserve a recovery-held, sync-disabled state."
    }

    $steamRoot = Join-Path $testRoot "steam-remote"
    $progressPath = Join-Path `
        (Join-Path (Join-Path $steamRoot 'profile1') 'saves') `
        'progress.save'
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $progressPath) | Out-Null
    [IO.File]::WriteAllBytes($progressPath, $specialBytes)
    $contextPath = Join-Path `
        (Join-Path (Join-Path $steamRoot '.sts2-launcher') 'contexts') `
        'vanilla.json'
    Write-JsonNoBom -Path $contextPath -Value ([ordered]@{
        Version = 1
        SteamId64 = 76561198000000001
        SaveNamespace = 'vanilla'
        RuntimeIdentity = 'public'
        ModSetFingerprint = ''
    })
    $steamEvidence = Join-Path $testRoot "steam-sync.log"
    [IO.File]::WriteAllText(
        $steamEvidence,
        "fixture: Steam client sync completed",
        [Text.UTF8Encoding]::new($false)
    )
    $steamManifestPath = Join-Path $testRoot "steam-manifest.json"
    & $steamCapture `
        -SteamCloudRoot $steamRoot `
        -SelectedNamespace Vanilla `
        -ExpectedSteamId64 '76561198000000001' `
        -ExpectedRuntimeIdentity public `
        -ExpectedModSetFingerprint '' `
        -SteamSyncEvidencePath $steamEvidence `
        -CaptureMethod 'deterministic fixture' `
        -RetainedImmutableSource `
        -OutputPath $steamManifestPath
    $steamManifest = Get-Content -LiteralPath $steamManifestPath -Raw |
        ConvertFrom-Json
    $remoteProgress = @($steamManifest.files | Where-Object {
        $_.path -eq 'profile1/saves/progress.save'
    })
    if ($steamManifest.kind -ne 'stage5-live-steam-save-manifest' -or
        $steamManifest.appId -ne 2868840 -or
        $remoteProgress.Count -ne 1 -or
        -not $remoteProgress[0].exists -or
        $remoteProgress[0].sha256 -ne (Get-Sha256Hex -Bytes $specialBytes)) {
        throw "Live Steam manifest did not preserve the exact fixture bytes."
    }

    $badOutput = Join-Path $testRoot "bad-context.json"
    $rejected = $false
    try {
        & $steamCapture `
            -SteamCloudRoot $steamRoot `
            -SelectedNamespace Vanilla `
            -ExpectedSteamId64 '76561198000000001' `
            -ExpectedRuntimeIdentity public-beta `
            -ExpectedModSetFingerprint '' `
            -SteamSyncEvidencePath $steamEvidence `
            -CaptureMethod 'deterministic fixture' `
            -RetainedImmutableSource `
            -OutputPath $badOutput
    } catch {
        $rejected = $true
    }
    if (-not $rejected -or (Test-Path -LiteralPath $badOutput)) {
        throw "Live Steam manifest did not reject a context mismatch before writing output."
    }

    Write-Host "Stage 5 save evidence manifest tests passed: 7/7"
} finally {
    $resolvedTestRoot = [IO.Path]::GetFullPath($testRoot)
    $tempPrefix = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd(
        [IO.Path]::DirectorySeparatorChar
    ) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedTestRoot.StartsWith($tempPrefix, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolvedTestRoot) -notmatch '^sts2-stage5-save-evidence-test-[0-9a-f]{32}$') {
        throw "Refusing to clean an unexpected Stage 5 test path: $resolvedTestRoot"
    }
    if (Test-Path -LiteralPath $resolvedTestRoot) {
        Remove-Item -LiteralPath $resolvedTestRoot -Recurse -Force
    }
}
