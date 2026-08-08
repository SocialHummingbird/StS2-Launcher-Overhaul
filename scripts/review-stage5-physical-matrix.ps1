[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$MatrixPath,
    [string]$OutputPath = "",
    [string]$AaptPath = "",
    [string]$ApkSignerPath = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$androidVerifier = Join-Path $PSScriptRoot "verify-stage5-android-save-bundle.ps1"
$steamManifestCapture = Join-Path $PSScriptRoot "new-stage5-live-steam-manifest.ps1"
. (Join-Path $PSScriptRoot "android-signing-utils.ps1")

$requiredCandidatePackage = "com.sts2launcher.overhaul.fork.local"
$requiredCandidateSignerSha256 =
    "fd0e3d5acf435c1d23bfc5c426e99aa9eb5808619ff1fc214ffca99cfac7e57a"
$requiredBaselineTag = "v0.2.416-startup-recovery-ime"
$requiredBaselineAsset =
    "StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk"
$requiredBaselineSha256 =
    "fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b"
$requiredMinimumVersionCode = 416001

function Assert-True {
    param(
        [Parameter(Mandatory = $true)][bool]$Condition,
        [Parameter(Mandatory = $true)][string]$Message
    )
    if (-not $Condition) {
        throw $Message
    }
}

function Assert-Equal {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [AllowEmptyString()][string]$Actual,
        [AllowEmptyString()][string]$Expected
    )
    if (-not [string]::Equals($Actual, $Expected, [StringComparison]::Ordinal)) {
        throw "$Label mismatch: expected '$Expected', got '$Actual'."
    }
}

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

function Get-FileSha256Hex {
    param([Parameter(Mandatory = $true)][string]$Path)
    return Get-Sha256Hex -Bytes ([IO.File]::ReadAllBytes($Path))
}

function Read-JsonFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    try {
        return [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($Path)) |
            ConvertFrom-Json -ErrorAction Stop
    } catch {
        throw "$Label is not valid UTF-8 JSON: $($_.Exception.Message)"
    }
}

function Resolve-MatrixPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "Evidence path is empty."
    }
    if ([IO.Path]::IsPathRooted($Path)) {
        return [IO.Path]::GetFullPath($Path)
    }
    return [IO.Path]::GetFullPath((Join-Path $script:MatrixDirectory $Path))
}

function Resolve-RepoPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "Recorded path is empty."
    }
    if ([IO.Path]::IsPathRooted($Path)) {
        return [IO.Path]::GetFullPath($Path)
    }
    return [IO.Path]::GetFullPath((Join-Path $script:RepoRoot $Path))
}

function Get-RequiredUtc {
    param(
        [AllowEmptyString()][string]$Value,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $parsed = [DateTimeOffset]::MinValue
    if ([string]::IsNullOrWhiteSpace($Value) -or
        -not [DateTimeOffset]::TryParse(
            $Value,
            [Globalization.CultureInfo]::InvariantCulture,
            [Globalization.DateTimeStyles]::RoundtripKind,
            [ref]$parsed
        )) {
        throw "$Label is not a valid timestamp: '$Value'."
    }
    return $parsed.ToUniversalTime()
}

function Normalize-SavePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $normalized = $Path.Replace('\', '/')
    if ([string]::IsNullOrWhiteSpace($normalized) -or
        $normalized.StartsWith('/') -or
        $normalized.EndsWith('/') -or
        $normalized.Contains('//') -or
        $normalized -match '(^|/)\.\.?(/|$)') {
        throw "Unsafe or non-canonical save path: '$Path'."
    }
    return $normalized
}

function New-FileMap {
    param(
        [Parameter(Mandatory = $true)]$Files,
        [Parameter(Mandatory = $true)][string]$Label,
        [switch]$IncludeRole
    )

    $map = [Collections.Generic.Dictionary[string, string]]::new(
        [StringComparer]::OrdinalIgnoreCase
    )
    foreach ($file in @($Files)) {
        $path = Normalize-SavePath -Path ([string]$file.path)
        if ($map.ContainsKey($path)) {
            throw "$Label has a case-insensitive duplicate path: $path"
        }
        $exists = $file.exists -is [bool] -and [bool]$file.exists
        if (-not ($file.exists -is [bool])) {
            throw "$Label has a non-boolean exists value for $path."
        }
        $size = [int64]$file.sizeBytes
        $hash = ([string]$file.sha256).ToLowerInvariant()
        if ($exists) {
            if ($size -lt 0 -or $hash -notmatch '^[0-9a-f]{64}$') {
                throw "$Label has invalid present-file metadata for $path."
            }
        } elseif ($size -ne 0 -or $hash) {
            throw "$Label has non-empty metadata for missing path $path."
        }
        $rolePrefix = if ($IncludeRole) { ([string]$file.role) + "`t" } else { "" }
        $map.Add(
            $path,
            "$rolePrefix$($exists.ToString().ToLowerInvariant())`t$size`t$hash"
        )
    }
    return $map
}

function Get-MapSignature {
    param([Parameter(Mandatory = $true)]$Map)

    $lines = @($Map.Keys | Sort-Object | ForEach-Object {
        "$_`t$($Map[$_])"
    })
    return Get-Sha256Hex -Bytes (
        [Text.Encoding]::UTF8.GetBytes(($lines -join "`n") + "`n")
    )
}

function Assert-MapsEqual {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)]$Left,
        [Parameter(Mandatory = $true)]$Right
    )

    if ($Left.Count -ne $Right.Count) {
        throw "$Label file-count mismatch: $($Left.Count) versus $($Right.Count)."
    }
    foreach ($path in $Left.Keys) {
        if (-not $Right.ContainsKey($path) -or
            -not [string]::Equals($Left[$path], $Right[$path], [StringComparison]::Ordinal)) {
            throw "$Label differs at $path."
        }
    }
}

function Assert-MapsDiffer {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)]$Left,
        [Parameter(Mandatory = $true)]$Right
    )
    if ((Get-MapSignature -Map $Left) -eq (Get-MapSignature -Map $Right)) {
        throw "$Label unexpectedly contains identical bytes/tombstones."
    }
}

function Assert-Context {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)]$Actual,
        [Parameter(Mandatory = $true)]$Expected
    )

    Assert-Equal -Label "$Label SteamID64" -Actual ([string]$Actual.steamId64) -Expected ([string]$Expected.steamId64)
    Assert-Equal -Label "$Label namespace" -Actual ([string]$Actual.saveNamespace).ToLowerInvariant() -Expected ([string]$Expected.saveNamespace)
    Assert-Equal -Label "$Label runtime identity" -Actual ([string]$Actual.runtimeIdentity) -Expected ([string]$Expected.runtimeIdentity)
    Assert-Equal -Label "$Label mod-set fingerprint" -Actual ([string]$Actual.modSetFingerprint) -Expected ([string]$Expected.modSetFingerprint)
}

function Assert-ExactProperties {
    param(
        [Parameter(Mandatory = $true)]$Object,
        [Parameter(Mandatory = $true)][string[]]$Names,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $actual = @($Object.PSObject.Properties.Name | Sort-Object)
    $expected = @($Names | Sort-Object)
    if (($actual -join "`n") -ne ($expected -join "`n")) {
        throw "$Label fields must be exactly: $($expected -join ', ')."
    }
}

function Get-AndroidSnapshot {
    param(
        [Parameter(Mandatory = $true)]$Record,
        [string]$RecoveryTreeSha256 = ""
    )

    if (-not $RecoveryTreeSha256) {
        return [pscustomobject]@{
            Context = $Record.Data.context
            Files = New-FileMap -Files $Record.Data.snapshot.files -Label "$($Record.Spec.id) current Android snapshot"
            CapturedUtc = Get-RequiredUtc -Value ([string]$Record.Data.snapshot.capturedUtc) -Label "$($Record.Spec.id) snapshot time"
        }
    }

    if ($RecoveryTreeSha256 -notmatch '^[0-9a-f]{64}$') {
        throw "Android recovery tree hash is not a SHA-256 value: '$RecoveryTreeSha256'."
    }
    $matches = @($Record.Data.recoverySnapshots | Where-Object {
        [string]$_.treeSha256 -eq $RecoveryTreeSha256
    })
    if ($matches.Count -ne 1) {
        throw "$($Record.Spec.id) does not contain exactly one recovery snapshot with tree SHA-256 $RecoveryTreeSha256."
    }
    $snapshot = $matches[0]
    if (-not [string]$snapshot.contextMarker) {
        throw "$($Record.Spec.id) recovery snapshot $RecoveryTreeSha256 has no exact SaveContext marker."
    }
    try {
        $marker = ([string]$snapshot.contextMarker) | ConvertFrom-Json -ErrorAction Stop
    } catch {
        throw "$($Record.Spec.id) recovery snapshot $RecoveryTreeSha256 has an invalid SaveContext marker."
    }
    $context = [pscustomobject]@{
        steamId64 = [string]$marker.SteamId64
        saveNamespace = ([string]$marker.SaveNamespace).ToLowerInvariant()
        runtimeIdentity = [string]$marker.RuntimeIdentity
        modSetFingerprint = [string]$marker.ModSetFingerprint
    }
    return [pscustomobject]@{
        Context = $context
        Files = New-FileMap -Files $snapshot.files -Label "$($Record.Spec.id) recovery snapshot $RecoveryTreeSha256"
        CapturedUtc = Get-RequiredUtc -Value ([string]$snapshot.capturedUtc) -Label "$($Record.Spec.id) recovery snapshot time"
    }
}

function Get-SteamSelectedMap {
    param([Parameter(Mandatory = $true)]$Record)

    $namespace = ([string]$Record.Data.selectedContext.saveNamespace).ToLowerInvariant()
    $role = "$namespace-save"
    $files = @($Record.Data.files | Where-Object {
        [string]$_.path -eq 'profile.save' -or [string]$_.role -eq $role
    })
    return New-FileMap -Files $files -Label "$($Record.Spec.id) selected Steam snapshot"
}

function Get-SteamFullMap {
    param([Parameter(Mandatory = $true)]$Record)
    return New-FileMap -Files $Record.Data.files -Label "$($Record.Spec.id) full Steam snapshot" -IncludeRole
}

function Test-HasSyncedLog {
    param([Parameter(Mandatory = $true)]$Collector)
    return [bool]($Collector.LogText -match '(?im)(\bSynced\b|synchronized and verified|automatic save reconciliation[^\r\n]*verified)')
}

function Assert-PendingPresent {
    param(
        [Parameter(Mandatory = $true)]$Collector,
        [Parameter(Mandatory = $true)]$Android
    )
    $pending = $Android.Data.launcherState.pending
    Assert-True -Condition ([bool]$pending.present) -Message "$($Android.Spec.id) verified export did not preserve a pending-sync record."
    Assert-True -Condition ([string]$pending.phase -in @('game-running', 'uploading', 'downloading')) -Message "$($Android.Spec.id) pending-sync phase is invalid."
    Assert-Context -Label "$($Android.Spec.id) pending context" -Actual $pending.context -Expected $Android.Data.context
    if ([bool]$Collector.Data.gates.runAsAvailable) {
        Assert-True -Condition ([int]$Collector.Data.gates.pendingSyncDocumentCount -gt 0) -Message "$($Collector.Spec.id) run-as evidence did not see the pending-sync record."
        Assert-True -Condition ([string]$pending.phase -in @($Collector.Data.gates.pendingSyncPhases)) -Message "$($Collector.Spec.id) run-as pending phase disagrees with the verified export."
    }
}

function Assert-PendingCleared {
    param(
        [Parameter(Mandatory = $true)]$Collector,
        [Parameter(Mandatory = $true)]$Android
    )
    Assert-True -Condition (-not [bool]$Android.Data.launcherState.pending.present) -Message "$($Android.Spec.id) verified export still has a pending-sync record."
    if ([bool]$Collector.Data.gates.runAsAvailable) {
        Assert-True -Condition ([int]$Collector.Data.gates.pendingSyncDocumentCount -eq 0) -Message "$($Collector.Spec.id) run-as evidence still has a pending-sync record."
    }
}

function Assert-NoSynced {
    param([Parameter(Mandatory = $true)]$Collector)
    Assert-True -Condition (-not [bool]$Collector.Data.gates.automaticSyncVerifiedLogSeen) -Message "$($Collector.Spec.id) reports verified synchronization on a non-success path."
    Assert-True -Condition (-not (Test-HasSyncedLog -Collector $Collector)) -Message "$($Collector.Spec.id) contains a false Synced log."
}

function Assert-CollectorAndroidBytes {
    param(
        [Parameter(Mandatory = $true)]$Collector,
        [Parameter(Mandatory = $true)]$Android,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (-not [bool]$Collector.Data.gates.runAsAvailable) {
        # The signed release candidate is normally non-debuggable. In that case the
        # independently verified in-app recovery export is the Android byte authority.
        return
    }

    $namespace = ([string]$Android.Data.context.saveNamespace).ToLowerInvariant()
    $prefix = if ($namespace -eq 'modded') { 'modded/' } else { '' }
    $captured = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::OrdinalIgnoreCase)
    $hashPath = Resolve-RepoPath -Path ([string]$Collector.Data.localSaveByteHashes)
    $lines = @([IO.File]::ReadAllLines($hashPath, [Text.Encoding]::UTF8))
    Assert-True -Condition ($lines.Count -gt 0 -and $lines[0].TrimStart([char]0xfeff) -eq "sha256`tsizeBytes`tdevicePath") -Message "$Label has an invalid collector local-hash header."
    foreach ($line in $lines | Select-Object -Skip 1) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $parts = $line -split "`t", 3
        Assert-True -Condition ($parts.Count -eq 3 -and $parts[0] -match '^[0-9a-fA-F]{64}$' -and $parts[1] -match '^[0-9]+$' -and $parts[2] -match '^files/') -Message "$Label has an invalid collector local-hash row."
        $path = Normalize-SavePath -Path $parts[2].Substring('files/'.Length)
        $selected = $path -eq 'profile.save' -or $path -match ('^' + [regex]::Escape($prefix) + 'profile[1-3]/saves/')
        if ($namespace -eq 'vanilla' -and $path.StartsWith('modded/', [StringComparison]::OrdinalIgnoreCase)) {
            $selected = $false
        }
        if (-not $selected) { continue }
        if ($captured.ContainsKey($path)) {
            throw "$Label collector repeats local path $path."
        }
        $captured.Add($path, "$($parts[1])`t$($parts[0].ToLowerInvariant())")
    }

    $expected = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($file in @($Android.Data.snapshot.files | Where-Object { [bool]$_.exists })) {
        $expected.Add([string]$file.path, "$([int64]$file.sizeBytes)`t$(([string]$file.sha256).ToLowerInvariant())")
    }
    Assert-MapsEqual -Label "$Label run-as/export cross-check" -Left $captured -Right $expected
}

function Assert-SuccessCollector {
    param([Parameter(Mandatory = $true)]$Collector)
    Assert-True -Condition ([bool]$Collector.Data.gates.automaticSyncVerifiedLogSeen) -Message "$($Collector.Spec.id) has no verified automatic-sync log."
    Assert-True -Condition (Test-HasSyncedLog -Collector $Collector) -Message "$($Collector.Spec.id) has no Synced log to pair with live Steam bytes."
    Assert-True -Condition (-not [bool]$Collector.Data.gates.automaticSyncConflictLogSeen) -Message "$($Collector.Spec.id) unexpectedly reports a conflict."
    Assert-True -Condition (-not [bool]$Collector.Data.gates.commitFailureSeen) -Message "$($Collector.Spec.id) unexpectedly reports commit failure."
    Assert-True -Condition (-not [bool]$Collector.Data.gates.readBackMismatchSeen) -Message "$($Collector.Spec.id) unexpectedly reports read-back mismatch."
}

$resolvedMatrix = (Resolve-Path -LiteralPath $MatrixPath).Path
$script:MatrixDirectory = Split-Path -Parent $resolvedMatrix
$script:RepoRoot = $repoRoot
$matrixBytes = [IO.File]::ReadAllBytes($resolvedMatrix)
$matrixSha256 = Get-Sha256Hex -Bytes $matrixBytes
$matrix = Read-JsonFile -Path $resolvedMatrix -Label 'Stage 5 physical matrix'

Assert-True -Condition ([int]$matrix.schemaVersion -eq 1) -Message 'Stage 5 matrix schemaVersion must be 1.'
Assert-Equal -Label 'Stage 5 matrix kind' -Actual ([string]$matrix.kind) -Expected 'stage5-physical-save-matrix'

$binding = $matrix.binding
$candidate = $binding.candidate
Assert-True -Condition ([string]$candidate.sourceCommit -match '^[0-9a-f]{40}$') -Message 'Matrix candidate sourceCommit must be a lowercase 40-character commit.'
Assert-True -Condition ([string]$candidate.apkSha256 -match '^[0-9a-f]{64}$') -Message 'Matrix candidate APK hash must be lowercase SHA-256.'
Assert-True -Condition ([string]$candidate.buildInfoSha256 -match '^[0-9a-f]{64}$') -Message 'Matrix candidate build-info hash must be lowercase SHA-256.'
Assert-True -Condition ([string]$candidate.signerSha256 -match '^[0-9a-f]{64}$') -Message 'Matrix candidate signer hash must be lowercase SHA-256.'
Assert-True -Condition ([string]$candidate.updateBaselineApkSha256 -match '^[0-9a-f]{64}$') -Message 'Matrix update-baseline APK hash must be lowercase SHA-256.'
Assert-True -Condition ([string]$candidate.packageName -match '^[A-Za-z][A-Za-z0-9_]*(\.[A-Za-z][A-Za-z0-9_]*)+$') -Message 'Matrix candidate package name is invalid.'
Assert-True -Condition (-not [string]::IsNullOrWhiteSpace([string]$candidate.versionName)) -Message 'Matrix candidate versionName is empty.'
Assert-True -Condition ([int64]$candidate.versionCode -gt 0) -Message 'Matrix candidate versionCode must be positive.'
Assert-True -Condition ([string]$candidate.candidateRunId -match '^[0-9]+$') -Message 'Matrix candidate run id is invalid.'
Assert-True -Condition ([string]$candidate.candidateRunAttempt -match '^[1-9][0-9]*$') -Message 'Matrix candidate run attempt is invalid.'
Assert-Equal -Label 'Matrix candidate ABI' -Actual ([string]$candidate.abi) -Expected 'arm64-v8a'
Assert-Equal -Label 'Matrix candidate signing channel' -Actual ([string]$candidate.signingChannel) -Expected 'release'
Assert-True -Condition (-not [string]::IsNullOrWhiteSpace([string]$candidate.releaseTag)) -Message 'Matrix candidate release tag is empty.'
Assert-True -Condition (-not [string]::IsNullOrWhiteSpace([string]$candidate.updateBaselineTag)) -Message 'Matrix update-baseline tag is empty.'
Assert-True -Condition (-not [string]::IsNullOrWhiteSpace([string]$candidate.updateBaselineAssetName)) -Message 'Matrix update-baseline asset name is empty.'
Assert-Equal -Label 'Required affected-user package' -Actual ([string]$candidate.packageName) -Expected $requiredCandidatePackage
Assert-Equal -Label 'Required v0.2.416 update signer' -Actual ([string]$candidate.signerSha256) -Expected $requiredCandidateSignerSha256
Assert-True -Condition ([int64]$candidate.versionCode -gt $requiredMinimumVersionCode) -Message "Matrix candidate versionCode must exceed the published v0.2.416 versionCode $requiredMinimumVersionCode."
Assert-Equal -Label 'Required update-baseline tag' -Actual ([string]$candidate.updateBaselineTag) -Expected $requiredBaselineTag
Assert-Equal -Label 'Required update-baseline asset' -Actual ([string]$candidate.updateBaselineAssetName) -Expected $requiredBaselineAsset
Assert-Equal -Label 'Required update-baseline APK SHA-256' -Actual ([string]$candidate.updateBaselineApkSha256) -Expected $requiredBaselineSha256
Assert-True -Condition ([string]$binding.steamId64 -match '^[0-9]{17}$') -Message 'Matrix SteamID64 is invalid.'
Assert-True -Condition ([string]$binding.device.serialSha256 -match '^[0-9a-f]{64}$') -Message 'Matrix device serial hash is invalid.'
foreach ($field in @('manufacturer', 'model', 'androidApi', 'abiList', 'buildFingerprint')) {
    Assert-True -Condition (-not [string]::IsNullOrWhiteSpace([string]$binding.device.$field)) -Message "Matrix device $field is empty."
}
Assert-True -Condition ([string]$binding.device.abiList -match '(^|,)arm64-v8a(,|$)') -Message 'Bound physical device does not advertise arm64-v8a.'

$candidateApkPath = Resolve-MatrixPath -Path ([string]$candidate.apkPath)
$buildInfoPath = Resolve-MatrixPath -Path ([string]$candidate.buildInfoPath)
Assert-True -Condition (Test-Path -LiteralPath $candidateApkPath -PathType Leaf) -Message 'Exact candidate APK is missing.'
Assert-True -Condition (Test-Path -LiteralPath $buildInfoPath -PathType Leaf) -Message 'Candidate build-info sidecar is missing.'
Assert-Equal -Label 'Exact candidate APK SHA-256' -Actual (Get-FileSha256Hex -Path $candidateApkPath) -Expected ([string]$candidate.apkSha256)
Assert-Equal -Label 'Candidate build-info SHA-256' -Actual (Get-FileSha256Hex -Path $buildInfoPath) -Expected ([string]$candidate.buildInfoSha256)

$buildInfo = [Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
foreach ($line in [IO.File]::ReadAllLines($buildInfoPath, [Text.Encoding]::UTF8)) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    if ($line -notmatch '^([a-z][a-z0-9_]*)=(.*)$') {
        throw "Candidate build-info has an invalid line: $line"
    }
    if ($buildInfo.ContainsKey($Matches[1])) {
        throw "Candidate build-info repeats key $($Matches[1])."
    }
    $buildInfo.Add($Matches[1], $Matches[2])
}
$expectedBuildInfo = [ordered]@{
    version_name = [string]$candidate.versionName
    version_code = [string]$candidate.versionCode
    package_name = [string]$candidate.packageName
    abi = [string]$candidate.abi
    signing_channel = [string]$candidate.signingChannel
    signer_sha256 = ([string]$candidate.signerSha256).ToUpperInvariant()
    release_tag = [string]$candidate.releaseTag
    source_commit = [string]$candidate.sourceCommit
    candidate_run_id = [string]$candidate.candidateRunId
    candidate_run_attempt = [string]$candidate.candidateRunAttempt
    apk_sha256 = ([string]$candidate.apkSha256).ToLowerInvariant()
    update_baseline_tag = [string]$candidate.updateBaselineTag
    update_baseline_asset_name = [string]$candidate.updateBaselineAssetName
    update_baseline_apk_sha256 = ([string]$candidate.updateBaselineApkSha256).ToLowerInvariant()
}
Assert-True -Condition ($buildInfo.Count -eq $expectedBuildInfo.Count) -Message 'Candidate build-info must contain exactly the release-candidate identity fields.'
foreach ($key in $expectedBuildInfo.Keys) {
    Assert-True -Condition ($buildInfo.ContainsKey($key)) -Message "Candidate build-info omits $key."
    $actualValue = $buildInfo[$key]
    if ($key -eq 'signer_sha256') { $actualValue = $actualValue.ToUpperInvariant() }
    if ($key -in @('apk_sha256', 'update_baseline_apk_sha256')) { $actualValue = $actualValue.ToLowerInvariant() }
    Assert-Equal -Label "Candidate build-info $key" -Actual $actualValue -Expected ([string]$expectedBuildInfo[$key])
}

$resolvedAapt = if ($AaptPath) { (Resolve-Path -LiteralPath $AaptPath).Path } else { Resolve-AndroidBuildTool 'aapt' }
$resolvedApkSigner = if ($ApkSignerPath) { (Resolve-Path -LiteralPath $ApkSignerPath).Path } else { Resolve-AndroidBuildTool 'apksigner' }
$apkIdentity = Get-AndroidApkIdentity -Path $candidateApkPath -Aapt $resolvedAapt -ApkSigner $resolvedApkSigner
Assert-Equal -Label 'Actual APK package' -Actual ([string]$apkIdentity.packageName) -Expected ([string]$candidate.packageName)
Assert-Equal -Label 'Actual APK version name' -Actual ([string]$apkIdentity.versionName) -Expected ([string]$candidate.versionName)
Assert-True -Condition ([int64]$apkIdentity.versionCode -eq [int64]$candidate.versionCode) -Message 'Actual APK version code differs from the candidate binding.'
Assert-Equal -Label 'Actual APK signer' -Actual ([string]$apkIdentity.signerSha256).ToLowerInvariant() -Expected ([string]$candidate.signerSha256)
$badging = Invoke-CheckedTool $resolvedAapt @('dump', 'badging', $candidateApkPath)
Assert-True -Condition ($badging -match "(?m)^native-code:.*'arm64-v8a'") -Message 'Actual APK does not contain the bound arm64-v8a native ABI.'

$contexts = [Collections.Generic.Dictionary[string, object]]::new(
    [StringComparer]::Ordinal
)
foreach ($context in @($matrix.contexts)) {
    $id = [string]$context.id
    if ($contexts.ContainsKey($id)) {
        throw "Duplicate expected SaveContext id: $id"
    }
    $contexts.Add($id, $context)
    Assert-Equal -Label "$id SteamID64" -Actual ([string]$context.steamId64) -Expected ([string]$binding.steamId64)
}
$requiredContexts = [ordered]@{
    'vanilla-public' = @('vanilla', 'public', '')
    'vanilla-public-beta' = @('vanilla', 'public-beta', '')
    'modded-exact' = @('modded', 'public', $null)
    'modded-changed' = @('modded', 'public', $null)
}
Assert-True -Condition ($contexts.Count -eq $requiredContexts.Count) -Message 'Matrix must define exactly four expected SaveContexts.'
foreach ($id in $requiredContexts.Keys) {
    Assert-True -Condition ($contexts.ContainsKey($id)) -Message "Matrix is missing expected SaveContext $id."
    $context = $contexts[$id]
    $expected = $requiredContexts[$id]
    Assert-Equal -Label "$id namespace" -Actual ([string]$context.saveNamespace).ToLowerInvariant() -Expected $expected[0]
    Assert-Equal -Label "$id runtime identity" -Actual ([string]$context.runtimeIdentity) -Expected $expected[1]
    if ($expected[0] -eq 'vanilla') {
        Assert-Equal -Label "$id mod-set fingerprint" -Actual ([string]$context.modSetFingerprint) -Expected ''
    } else {
        Assert-True -Condition (-not [string]::IsNullOrWhiteSpace([string]$context.modSetFingerprint)) -Message "$id requires a mod-set fingerprint."
    }
}
Assert-True -Condition (
    [string]$contexts['modded-exact'].modSetFingerprint -ne [string]$contexts['modded-changed'].modSetFingerprint
) -Message 'Exact and changed mod-set contexts must have different fingerprints.'

$evidence = [Collections.Generic.Dictionary[string, object]]::new(
    [StringComparer]::Ordinal
)
$seenEvidencePaths = [Collections.Generic.HashSet[string]]::new(
    [StringComparer]::OrdinalIgnoreCase
)
$usedEvidence = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$syncedProofs = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)

foreach ($spec in @($matrix.evidence)) {
    $id = [string]$spec.id
    Assert-True -Condition ($id -match '^[a-z0-9][a-z0-9-]{2,95}$') -Message "Invalid evidence id: '$id'."
    if ($evidence.ContainsKey($id)) {
        throw "Duplicate evidence id: $id"
    }
    $kind = [string]$spec.kind
    Assert-True -Condition ($kind -in @('android-manifest', 'steam-manifest', 'collector')) -Message "$id has unsupported evidence kind '$kind'."
    $path = Resolve-MatrixPath -Path ([string]$spec.path)
    Assert-True -Condition (Test-Path -LiteralPath $path -PathType Leaf) -Message "$id evidence file is missing: $path"
    Assert-True -Condition ($seenEvidencePaths.Add($path)) -Message "Evidence path is reused by more than one id: $path"
    $expectedHash = ([string]$spec.sha256).ToLowerInvariant()
    Assert-True -Condition ($expectedHash -match '^[0-9a-f]{64}$') -Message "$id has an invalid recorded SHA-256."
    Assert-Equal -Label "$id evidence SHA-256" -Actual (Get-FileSha256Hex -Path $path) -Expected $expectedHash
    $data = Read-JsonFile -Path $path -Label $id
    $record = [pscustomobject]@{
        Spec = $spec
        Path = $path
        Data = $data
        LogText = ''
    }
    $evidence.Add($id, $record)
}

function Validate-AndroidManifest {
    param([Parameter(Mandatory = $true)]$Record)

    $data = $Record.Data
    Assert-True -Condition ([int]$data.schemaVersion -eq 1) -Message "$($Record.Spec.id) Android schemaVersion must be 1."
    Assert-Equal -Label "$($Record.Spec.id) kind" -Actual ([string]$data.kind) -Expected 'stage5-android-save-manifest'
    Assert-Equal -Label "$($Record.Spec.id) authority" -Actual ([string]$data.authority) -Expected 'verified-recovery-export-current-android-snapshot'
    Assert-True -Condition (-not [bool]$data.originalSourcesWereModified) -Message "$($Record.Spec.id) reports modified original sources."
    Assert-True -Condition (-not [bool]$data.steamWasContacted) -Message "$($Record.Spec.id) recovery export contacted Steam."
    Assert-True -Condition ($data.cloudSyncEnabled -is [bool]) -Message "$($Record.Spec.id) has no boolean Cloud Sync setting evidence."
    [void](Get-RequiredUtc -Value ([string]$data.verifiedUtc) -Label "$($Record.Spec.id) verification time")

    $bundlePath = [IO.Path]::GetFullPath([string]$data.sourceBundle)
    Assert-True -Condition (Test-Path -LiteralPath $bundlePath -PathType Leaf) -Message "$($Record.Spec.id) source recovery bundle is missing: $bundlePath"
    $bundleHash = ([string]$data.sourceBundleSha256).ToLowerInvariant()
    Assert-True -Condition ($bundleHash -match '^[0-9a-f]{64}$') -Message "$($Record.Spec.id) source bundle hash is invalid."
    Assert-Equal -Label "$($Record.Spec.id) source bundle SHA-256" -Actual (Get-FileSha256Hex -Path $bundlePath) -Expected $bundleHash

    $namespace = switch (([string]$data.context.saveNamespace).ToLowerInvariant()) {
        'vanilla' { 'Vanilla' }
        'modded' { 'Modded' }
        default { throw "$($Record.Spec.id) has an invalid Android namespace." }
    }
    $temporaryOutput = Join-Path ([IO.Path]::GetTempPath()) (
        "sts2-stage5-review-" + [Guid]::NewGuid().ToString('N') + '.json'
    )
    try {
        & $script:AndroidVerifier `
            -BundlePath $bundlePath `
            -OutputPath $temporaryOutput `
            -Namespace $namespace `
            -ExpectedSteamId64 ([string]$data.context.steamId64) `
            -ExpectedRuntimeIdentity ([string]$data.context.runtimeIdentity) `
            -ExpectedModSetFingerprint ([string]$data.context.modSetFingerprint) *> $null
        $regenerated = Read-JsonFile -Path $temporaryOutput -Label "$($Record.Spec.id) regenerated Android manifest"
    } finally {
        if (Test-Path -LiteralPath $temporaryOutput) {
            Remove-Item -LiteralPath $temporaryOutput -Force
        }
    }

    Assert-Context -Label "$($Record.Spec.id) Android context" -Actual $data.context -Expected $regenerated.context
    Assert-Equal -Label "$($Record.Spec.id) current tree hash" -Actual ([string]$data.snapshot.treeSha256) -Expected ([string]$regenerated.snapshot.treeSha256)
    Assert-MapsEqual `
        -Label "$($Record.Spec.id) regenerated current Android snapshot" `
        -Left (New-FileMap -Files $data.snapshot.files -Label "$($Record.Spec.id) current snapshot") `
        -Right (New-FileMap -Files $regenerated.snapshot.files -Label "$($Record.Spec.id) regenerated current snapshot")
    [void](Get-RequiredUtc -Value ([string]$data.snapshot.capturedUtc) -Label "$($Record.Spec.id) current snapshot time")

    $recordedRecovery = @($data.recoverySnapshots)
    $regeneratedRecovery = @($regenerated.recoverySnapshots)
    Assert-True -Condition ($recordedRecovery.Count -eq $regeneratedRecovery.Count) -Message "$($Record.Spec.id) recovery snapshot count differs from its source bundle."
    for ($index = 0; $index -lt $regeneratedRecovery.Count; $index++) {
        $expectedSnapshot = $regeneratedRecovery[$index]
        $actualSnapshot = $recordedRecovery[$index]
        $tree = [string]$expectedSnapshot.treeSha256
        Assert-True -Condition ($tree -match '^[0-9a-f]{64}$') -Message "$($Record.Spec.id) recovery snapshot has no recomputed tree identity."
        foreach ($field in @('reportedSnapshotId', 'reportedSnapshotSha256', 'sourceKind', 'sourceLabel', 'classification', 'snapshotPath', 'saveNamespace', 'contextMarker', 'capturedUtc', 'treeSha256')) {
            Assert-Equal -Label "$($Record.Spec.id) recovery $tree $field" -Actual ([string]$actualSnapshot.$field) -Expected ([string]$expectedSnapshot.$field)
        }
        Assert-MapsEqual `
            -Label "$($Record.Spec.id) regenerated recovery snapshot $tree" `
            -Left (New-FileMap -Files $actualSnapshot.files -Label "$($Record.Spec.id) recovery $tree") `
            -Right (New-FileMap -Files $expectedSnapshot.files -Label "$($Record.Spec.id) regenerated recovery $tree")
    }

    foreach ($stateName in @('pending', 'baseline', 'beforeGameSnapshot', 'recoveryHold', 'recoveryJournal')) {
        $actualState = $data.launcherState.$stateName
        $expectedState = $regenerated.launcherState.$stateName
        Assert-True -Condition ($actualState.present -is [bool]) -Message "$($Record.Spec.id) launcherState.$stateName has no boolean presence flag."
        Assert-True -Condition ([bool]$actualState.present -eq [bool]$expectedState.present) -Message "$($Record.Spec.id) launcherState.$stateName presence differs from the source bundle."
        foreach ($field in @('sha256', 'phase', 'treeSha256')) {
            if ($null -ne $actualState.PSObject.Properties[$field] -or $null -ne $expectedState.PSObject.Properties[$field]) {
                Assert-Equal -Label "$($Record.Spec.id) launcherState.$stateName.$field" -Actual ([string]$actualState.$field) -Expected ([string]$expectedState.$field)
            }
        }
        if ($null -ne $actualState.context -or $null -ne $expectedState.context) {
            Assert-True -Condition ($null -ne $actualState.context -and $null -ne $expectedState.context) -Message "$($Record.Spec.id) launcherState.$stateName context presence differs from its source bundle."
            Assert-Context -Label "$($Record.Spec.id) launcherState.$stateName context" -Actual $actualState.context -Expected $expectedState.context
        }
    }
}

function Validate-SteamManifest {
    param([Parameter(Mandatory = $true)]$Record)

    $data = $Record.Data
    Assert-True -Condition ([int]$data.schemaVersion -eq 1) -Message "$($Record.Spec.id) Steam schemaVersion must be 1."
    Assert-Equal -Label "$($Record.Spec.id) kind" -Actual ([string]$data.kind) -Expected 'stage5-live-steam-save-manifest'
    Assert-Equal -Label "$($Record.Spec.id) authority" -Actual ([string]$data.authority) -Expected 'independent-live-steam-client-download'
    Assert-True -Condition ([int]$data.appId -eq 2868840) -Message "$($Record.Spec.id) has the wrong Steam app id."
    Assert-True -Condition (-not [string]::IsNullOrWhiteSpace([string]$data.captureMethod)) -Message "$($Record.Spec.id) has no independent Steam capture method."
    Assert-True -Condition ([bool]$data.sourceRootRetainedImmutable) -Message "$($Record.Spec.id) does not retain an immutable independent Steam download for review."
    [void](Get-RequiredUtc -Value ([string]$data.capturedUtc) -Label "$($Record.Spec.id) Steam capture time")

    $sourceRoot = [IO.Path]::GetFullPath([string]$data.sourceRoot)
    Assert-True -Condition (Test-Path -LiteralPath $sourceRoot -PathType Container) -Message "$($Record.Spec.id) retained Steam source root is missing."

    $syncEvidencePath = [IO.Path]::GetFullPath([string]$data.steamSyncEvidence.path)
    Assert-True -Condition (Test-Path -LiteralPath $syncEvidencePath -PathType Leaf) -Message "$($Record.Spec.id) Steam client evidence file is missing."
    Assert-True -Condition ([int64]$data.steamSyncEvidence.sizeBytes -eq (Get-Item -LiteralPath $syncEvidencePath).Length) -Message "$($Record.Spec.id) Steam client evidence size changed."
    Assert-Equal -Label "$($Record.Spec.id) Steam client evidence SHA-256" -Actual (Get-FileSha256Hex -Path $syncEvidencePath) -Expected ([string]$data.steamSyncEvidence.sha256).ToLowerInvariant()

    $files = @($data.files)
    $fullMap = New-FileMap -Files $files -Label "$($Record.Spec.id) Steam manifest" -IncludeRole
    $required = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    [void]$required.Add('profile.save')
    foreach ($namespace in @('vanilla', 'modded')) {
        $prefix = if ($namespace -eq 'modded') { 'modded/' } else { '' }
        foreach ($profileId in 1..3) {
            foreach ($name in @('progress.save', 'prefs', 'prefs.save', 'current_run.save', 'current_run_mp.save')) {
                [void]$required.Add("${prefix}profile${profileId}/saves/$name")
            }
        }
        [void]$required.Add(".sts2-launcher/contexts/$namespace.json")
    }
    foreach ($path in $required) {
        Assert-True -Condition ($fullMap.ContainsKey($path)) -Message "$($Record.Spec.id) Steam manifest omits $path."
    }
    foreach ($file in $files) {
        $path = Normalize-SavePath -Path ([string]$file.path)
        $role = [string]$file.role
        $valid = if ($path -eq 'profile.save') {
            $role -eq 'shared-save'
        } elseif ($path -match '^modded/profile[1-3]/saves/(progress\.save|prefs|prefs\.save|current_run\.save|current_run_mp\.save|history/[^/]+\.run)$') {
            $role -eq 'modded-save'
        } elseif ($path -match '^profile[1-3]/saves/(progress\.save|prefs|prefs\.save|current_run\.save|current_run_mp\.save|history/[^/]+\.run)$') {
            $role -eq 'vanilla-save'
        } elseif ($path -eq '.sts2-launcher/contexts/vanilla.json') {
            $role -eq 'vanilla-context-marker'
        } elseif ($path -eq '.sts2-launcher/contexts/modded.json') {
            $role -eq 'modded-context-marker'
        } else {
            $false
        }
        Assert-True -Condition $valid -Message "$($Record.Spec.id) has invalid Steam path/role metadata for $path."
    }

    $sorted = @($files | Sort-Object -Property @{ Expression = {
        ([string]$_.path).ToLowerInvariant()
    } }, @{ Expression = { [string]$_.path } })
    $treeLines = $sorted | ForEach-Object {
        "$($_.path)`t$($_.role)`t$($_.exists.ToString().ToLowerInvariant())`t$($_.sizeBytes)`t$($_.sha256)"
    }
    $treeHash = Get-Sha256Hex -Bytes ([Text.Encoding]::UTF8.GetBytes(($treeLines -join "`n") + "`n"))
    Assert-Equal -Label "$($Record.Spec.id) Steam tree SHA-256" -Actual $treeHash -Expected ([string]$data.treeSha256).ToLowerInvariant()

    $selectedNamespace = ([string]$data.selectedContext.saveNamespace).ToLowerInvariant()
    Assert-True -Condition ($selectedNamespace -in @('vanilla', 'modded')) -Message "$($Record.Spec.id) selected Steam namespace is invalid."
    Assert-Equal -Label "$($Record.Spec.id) selected marker path" -Actual ([string]$data.selectedContext.markerPath) -Expected ".sts2-launcher/contexts/$selectedNamespace.json"
    $marker = @($files | Where-Object { [string]$_.path -eq [string]$data.selectedContext.markerPath })
    Assert-True -Condition ($marker.Count -eq 1 -and [bool]$marker[0].exists) -Message "$($Record.Spec.id) selected Steam context marker is not present."

    $temporaryOutput = Join-Path ([IO.Path]::GetTempPath()) (
        "sts2-stage5-steam-review-" + [Guid]::NewGuid().ToString('N') + '.json'
    )
    $selectedNamespaceArgument = if ($selectedNamespace -eq 'vanilla') { 'Vanilla' } else { 'Modded' }
    try {
        & $script:SteamManifestCapture `
            -SteamCloudRoot $sourceRoot `
            -SelectedNamespace $selectedNamespaceArgument `
            -ExpectedSteamId64 ([string]$data.selectedContext.steamId64) `
            -ExpectedRuntimeIdentity ([string]$data.selectedContext.runtimeIdentity) `
            -ExpectedModSetFingerprint ([string]$data.selectedContext.modSetFingerprint) `
            -SteamSyncEvidencePath $syncEvidencePath `
            -CaptureMethod ([string]$data.captureMethod) `
            -RetainedImmutableSource `
            -OutputPath $temporaryOutput *> $null
        $regenerated = Read-JsonFile -Path $temporaryOutput -Label "$($Record.Spec.id) regenerated live Steam manifest"
    } finally {
        if (Test-Path -LiteralPath $temporaryOutput) {
            Remove-Item -LiteralPath $temporaryOutput -Force
        }
    }
    Assert-Context -Label "$($Record.Spec.id) regenerated Steam context" -Actual $data.selectedContext -Expected $regenerated.selectedContext
    Assert-Equal -Label "$($Record.Spec.id) regenerated Steam tree" -Actual ([string]$data.treeSha256) -Expected ([string]$regenerated.treeSha256)
    Assert-MapsEqual `
        -Label "$($Record.Spec.id) retained live Steam raw bytes" `
        -Left (New-FileMap -Files $data.files -Label "$($Record.Spec.id) recorded Steam" -IncludeRole) `
        -Right (New-FileMap -Files $regenerated.files -Label "$($Record.Spec.id) regenerated Steam" -IncludeRole)
}

function Resolve-InventoryRoot {
    param([Parameter(Mandatory = $true)][string]$RecordedRoot)
    return Resolve-RepoPath -Path $RecordedRoot
}

function Validate-CollectorInventory {
    param(
        [Parameter(Mandatory = $true)]$Record,
        [Parameter(Mandatory = $true)][string]$InventoryPath,
        [Parameter(Mandatory = $true)]$Inventory
    )

    Assert-True -Condition ([int]$Inventory.schemaVersion -eq 1) -Message "$($Record.Spec.id) inventory schemaVersion must be 1."
    Assert-Equal -Label "$($Record.Spec.id) inventory kind" -Actual ([string]$Inventory.kind) -Expected 'stage5-android-regression-evidence-inventory'
    Assert-Equal -Label "$($Record.Spec.id) inventory source commit" -Actual ([string]$Inventory.sourceCommit) -Expected ([string]$script:Binding.candidate.sourceCommit)
    Assert-Equal -Label "$($Record.Spec.id) inventory APK hash" -Actual ([string]$Inventory.apkSha256).ToLowerInvariant() -Expected ([string]$script:Binding.candidate.apkSha256)
    $expectedDeviceText = "$($script:Binding.device.manufacturer) $($script:Binding.device.model); Android API $($script:Binding.device.androidApi); ABI $($script:Binding.device.abiList)"
    Assert-Equal -Label "$($Record.Spec.id) inventory device identity" -Actual ([string]$Inventory.deviceIdentity) -Expected $expectedDeviceText

    $root = Resolve-InventoryRoot -RecordedRoot ([string]$Inventory.evidenceRoot)
    $collectorRoot = Resolve-RepoPath -Path ([string]$Record.Data.output)
    Assert-Equal -Label "$($Record.Spec.id) inventory evidence root" -Actual $root -Expected $collectorRoot
    Assert-True -Condition (Test-Path -LiteralPath $root -PathType Container) -Message "$($Record.Spec.id) inventory evidence root is missing."

    $rootPrefix = $root.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $recorded = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)
    $canonicalLines = [Collections.Generic.List[string]]::new()
    $totalBytes = [int64]0
    foreach ($entry in @($Inventory.entries)) {
        $relative = Normalize-SavePath -Path ([string]$entry.relativePath)
        if ($recorded.ContainsKey($relative)) {
            throw "$($Record.Spec.id) inventory has duplicate path $relative."
        }
        $recorded.Add($relative, $entry)
        $full = [IO.Path]::GetFullPath((Join-Path $root $relative.Replace('/', [IO.Path]::DirectorySeparatorChar)))
        Assert-True -Condition ($full.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) -Message "$($Record.Spec.id) inventory path escaped its root."
        Assert-True -Condition (Test-Path -LiteralPath $full -PathType Leaf) -Message "$($Record.Spec.id) inventory file is missing: $relative"
        $item = Get-Item -LiteralPath $full
        Assert-True -Condition ([int64]$entry.sizeBytes -eq $item.Length) -Message "$($Record.Spec.id) inventory size changed for $relative."
        Assert-Equal -Label "$($Record.Spec.id) inventory hash for $relative" -Actual (Get-FileSha256Hex -Path $full) -Expected ([string]$entry.sha256).ToLowerInvariant()
        $canonical = [ordered]@{
            relativePath = $relative
            sizeBytes = [int64]$entry.sizeBytes
            sha256 = ([string]$entry.sha256).ToLowerInvariant()
            modifiedUtc = [string]$entry.modifiedUtc
        }
        $canonicalLines.Add(($canonical | ConvertTo-Json -Compress))
        $totalBytes += $item.Length
    }
    $actualFiles = @(Get-ChildItem -LiteralPath $root -Recurse -File -Force)
    Assert-True -Condition ($actualFiles.Count -eq $recorded.Count) -Message "$($Record.Spec.id) evidence tree no longer matches its complete inventory."
    foreach ($file in $actualFiles) {
        $relative = $file.FullName.Substring($rootPrefix.Length).Replace('\', '/')
        Assert-True -Condition ($recorded.ContainsKey($relative)) -Message "$($Record.Spec.id) evidence tree has unrecorded file $relative."
    }
    Assert-True -Condition ([int]$Inventory.fileCount -eq $recorded.Count) -Message "$($Record.Spec.id) inventory fileCount is wrong."
    Assert-True -Condition ([int64]$Inventory.totalBytes -eq $totalBytes) -Message "$($Record.Spec.id) inventory totalBytes is wrong."
    $manifestHash = Get-Sha256Hex -Bytes ([Text.Encoding]::UTF8.GetBytes([string]::Join("`n", $canonicalLines)))
    Assert-Equal -Label "$($Record.Spec.id) inventory manifest SHA-256" -Actual $manifestHash -Expected ([string]$Inventory.manifestSha256).ToLowerInvariant()

    $expectedManifestPath = Join-Path $root 'manifest.json'
    Assert-Equal -Label "$($Record.Spec.id) collector manifest location" -Actual $Record.Path -Expected $expectedManifestPath
    Assert-True -Condition ($recorded.ContainsKey('manifest.json')) -Message "$($Record.Spec.id) inventory does not cover its collector manifest."

    foreach ($field in @('logcat', 'filteredLogcat', 'summary', 'package', 'localSaveByteHashes', 'syncRecoveryStateByteHashes', 'persistedSteamByteHashes')) {
        $referenced = Resolve-RepoPath -Path ([string]$Record.Data.$field)
        Assert-True -Condition ($referenced.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) -Message "$($Record.Spec.id) $field is outside its evidence root."
        $relative = $referenced.Substring($rootPrefix.Length).Replace('\', '/')
        Assert-True -Condition ($recorded.ContainsKey($relative)) -Message "$($Record.Spec.id) inventory does not cover $field."
    }
}

function Validate-Collector {
    param([Parameter(Mandatory = $true)]$Record)

    $data = $Record.Data
    $spec = $Record.Spec
    Assert-True -Condition ([int]$data.schemaVersion -eq 2) -Message "$($spec.id) collector schemaVersion must be 2."
    Assert-Equal -Label "$($spec.id) kind" -Actual ([string]$data.kind) -Expected 'stage5-android-save-validation-capture'
    Assert-True -Condition ([int]$spec.row -ge 1 -and [int]$spec.row -le 10) -Message "$($spec.id) has an invalid matrix row."
    Assert-True -Condition ([string]$spec.phase -match '^[a-z0-9][a-z0-9-]{0,63}$') -Message "$($spec.id) has an invalid matrix phase."

    foreach ($scope in @($data.captureBinding, $data.gates)) {
        Assert-Equal -Label "$($spec.id) source commit" -Actual ([string]$scope.sourceCommit) -Expected ([string]$script:Binding.candidate.sourceCommit)
        Assert-True -Condition (-not [bool]$scope.sourceWorktreeDirty) -Message "$($spec.id) was captured from a dirty source worktree."
        Assert-Equal -Label "$($spec.id) candidate APK hash" -Actual ([string]$scope.candidateApkSha256).ToLowerInvariant() -Expected ([string]$script:Binding.candidate.apkSha256)
        Assert-Equal -Label "$($spec.id) installed APK hash" -Actual ([string]$scope.installedApkSha256).ToLowerInvariant() -Expected ([string]$script:Binding.candidate.apkSha256)
        Assert-True -Condition ([bool]$scope.candidateMatchesInstalled) -Message "$($spec.id) candidate did not match the installed base APK."
        Assert-Equal -Label "$($spec.id) Stage 5 row" -Actual ([string]$scope.stage5Row) -Expected ([string]$spec.row)
        Assert-Equal -Label "$($spec.id) evidence phase" -Actual ([string]$scope.evidencePhase) -Expected ([string]$spec.phase)
    }
    Assert-Equal -Label "$($spec.id) package" -Actual ([string]$data.captureBinding.packageName) -Expected ([string]$script:Binding.candidate.packageName)
    Assert-True -Condition ([bool]$data.gates.authorizedDevice) -Message "$($spec.id) device was not authorized."
    if ([bool]$data.gates.runAsAvailable) {
        Assert-True -Condition ([int]$data.gates.localSaveByteHashCount -gt 0) -Message "$($spec.id) advertised run-as but captured no Android-local save hashes."
    } else {
        Assert-True -Condition ([int]$data.gates.localSaveByteHashCount -eq 0) -Message "$($spec.id) has local hashes despite run-as being unavailable."
    }
    Assert-True -Condition (-not [bool]$data.gates.steamGameplaySaveManagerSeen) -Message "$($spec.id) observed a Steam-backed gameplay SaveManager."
    Assert-True -Condition (-not [bool]$data.gates.fatalExceptionSeen) -Message "$($spec.id) contains a fatal exception."
    Assert-True -Condition (-not [bool]$data.clearedLogcatAfterPreservingBuffer) -Message "$($spec.id) cleared logcat during Stage 5 evidence collection."
    Assert-True -Condition (-not [string]::IsNullOrWhiteSpace([string]$data.logcatSince)) -Message "$($spec.id) is not scenario-windowed with LogcatSince."
    Assert-Equal -Label "$($spec.id) logcatSince gate" -Actual ([string]$data.gates.logcatSince) -Expected ([string]$data.logcatSince)

    foreach ($field in @('serialSha256', 'manufacturer', 'model', 'androidApi', 'abiList', 'buildFingerprint')) {
        Assert-Equal -Label "$($spec.id) device $field" -Actual ([string]$data.device.$field) -Expected ([string]$script:Binding.device.$field)
    }
    Assert-Equal -Label "$($spec.id) recomputed serial hash" -Actual (Get-Sha256Hex -Bytes ([Text.Encoding]::UTF8.GetBytes([string]$data.device.serial))) -Expected ([string]$script:Binding.device.serialSha256)

    $candidatePath = Resolve-RepoPath -Path ([string]$data.captureBinding.candidateApkPath)
    Assert-True -Condition (Test-Path -LiteralPath $candidatePath -PathType Leaf) -Message "$($spec.id) exact candidate APK is unavailable for hash review."
    Assert-Equal -Label "$($spec.id) exact candidate APK path" -Actual $candidatePath -Expected $script:CandidateApkPath
    Assert-Equal -Label "$($spec.id) exact candidate file hash" -Actual (Get-FileSha256Hex -Path $candidatePath) -Expected ([string]$script:Binding.candidate.apkSha256)

    $inventoryPath = Resolve-MatrixPath -Path ([string]$spec.inventory.path)
    Assert-True -Condition (Test-Path -LiteralPath $inventoryPath -PathType Leaf) -Message "$($spec.id) collector inventory is missing."
    $inventoryHash = ([string]$spec.inventory.sha256).ToLowerInvariant()
    Assert-True -Condition ($inventoryHash -match '^[0-9a-f]{64}$') -Message "$($spec.id) collector inventory hash is invalid."
    Assert-Equal -Label "$($spec.id) collector inventory SHA-256" -Actual (Get-FileSha256Hex -Path $inventoryPath) -Expected $inventoryHash
    Assert-Equal -Label "$($spec.id) collector inventory path" -Actual (Resolve-RepoPath -Path ([string]$data.evidenceInventory)) -Expected $inventoryPath
    $inventory = Read-JsonFile -Path $inventoryPath -Label "$($spec.id) collector inventory"
    Validate-CollectorInventory -Record $Record -InventoryPath $inventoryPath -Inventory $inventory

    $logPath = Resolve-RepoPath -Path ([string]$data.logcat)
    $Record.LogText = [Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($logPath))
    Assert-True -Condition ($Record.LogText -notmatch '(?im)dropped (save )?write|swallowed (exception|failure|error)|save write[^\r\n]*(ignored|discarded)') -Message "$($spec.id) log indicates a dropped write or swallowed failure."
}

$script:AndroidVerifier = $androidVerifier
$script:SteamManifestCapture = $steamManifestCapture
$script:Binding = $binding
$script:CandidateApkPath = $candidateApkPath
$seenInventoryPaths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($record in $evidence.Values) {
    switch ([string]$record.Spec.kind) {
        'android-manifest' { Validate-AndroidManifest -Record $record }
        'steam-manifest' { Validate-SteamManifest -Record $record }
        'collector' {
            $inventoryPath = Resolve-MatrixPath -Path ([string]$record.Spec.inventory.path)
            Assert-True -Condition ($seenInventoryPaths.Add($inventoryPath)) -Message "Collector inventory path is reused: $inventoryPath"
            Validate-Collector -Record $record
        }
    }
}

function Use-Evidence {
    param(
        [Parameter(Mandatory = $true)][string]$Id,
        [Parameter(Mandatory = $true)][string]$Kind,
        [Parameter(Mandatory = $true)][int]$Row,
        [string]$Phase = ""
    )

    Assert-True -Condition ($script:Evidence.ContainsKey($Id)) -Message "Check references unknown evidence id $Id."
    $record = $script:Evidence[$Id]
    Assert-Equal -Label "$Id evidence kind" -Actual ([string]$record.Spec.kind) -Expected $Kind
    if ($Kind -eq 'collector') {
        Assert-True -Condition ([int]$record.Spec.row -eq $Row) -Message "$Id is bound to Stage 5 row $($record.Spec.row), not row $Row."
        Assert-Equal -Label "$Id evidence phase" -Actual ([string]$record.Spec.phase) -Expected $Phase
    }
    if ($Kind -eq 'android-manifest' -and $Row -ne 10) {
        Assert-True -Condition ([bool]$record.Data.cloudSyncEnabled) -Message "$Id does not show the existing Cloud Sync setting enabled for Stage 5 row $Row."
    }
    [void]$script:UsedEvidence.Add($Id)
    return $record
}

function Use-Context {
    param([Parameter(Mandatory = $true)][string]$Id)
    Assert-True -Condition ($script:Contexts.ContainsKey($Id)) -Message "Check references unknown expected SaveContext $Id."
    return $script:Contexts[$Id]
}

function Assert-EvidenceContext {
    param(
        [Parameter(Mandatory = $true)]$Record,
        [Parameter(Mandatory = $true)]$Expected
    )
    $actual = if ([string]$Record.Spec.kind -eq 'android-manifest') {
        $Record.Data.context
    } else {
        $Record.Data.selectedContext
    }
    Assert-Context -Label $Record.Spec.id -Actual $actual -Expected $Expected
}

function Assert-SteamCapturedAfter {
    param(
        [Parameter(Mandatory = $true)]$Steam,
        [Parameter(Mandatory = $true)]$Collector,
        [Parameter(Mandatory = $true)][string]$Label
    )
    $steamTime = Get-RequiredUtc -Value ([string]$Steam.Data.capturedUtc) -Label "$($Steam.Spec.id) Steam capture time"
    $collectorTime = Get-RequiredUtc -Value ([string]$Collector.Data.capturedUtc) -Label "$($Collector.Spec.id) collector time"
    Assert-True -Condition ($steamTime -gt $collectorTime) -Message "$Label requires an independent live Steam capture later than the collector."
}

function Add-SyncedProof {
    param(
        [Parameter(Mandatory = $true)]$Collector,
        [Parameter(Mandatory = $true)]$Android,
        [Parameter(Mandatory = $true)]$Steam,
        [Parameter(Mandatory = $true)]$Context,
        [Parameter(Mandatory = $true)][string]$Label
    )

    Assert-EvidenceContext -Record $Android -Expected $Context
    Assert-EvidenceContext -Record $Steam -Expected $Context
    Assert-MapsEqual `
        -Label "$Label Android/live-Steam byte equality" `
        -Left (Get-AndroidSnapshot -Record $Android).Files `
        -Right (Get-SteamSelectedMap -Record $Steam)
    Assert-SteamCapturedAfter -Steam $Steam -Collector $Collector -Label $Label
    [void]$script:SyncedProofs.Add([string]$Collector.Spec.id)
}

function Assert-SteamMutationIsolated {
    param(
        [Parameter(Mandatory = $true)]$Before,
        [Parameter(Mandatory = $true)]$After,
        [Parameter(Mandatory = $true)][ValidateSet('vanilla', 'modded')][string]$Namespace,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $beforeFiles = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($file in @($Before.Data.files)) { $beforeFiles.Add([string]$file.path, $file) }
    $afterFiles = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($file in @($After.Data.files)) { $afterFiles.Add([string]$file.path, $file) }
    Assert-True -Condition ($beforeFiles.Count -eq $afterFiles.Count) -Message "$Label changed the Steam manifest path set."
    $namespaceSaveChanged = $false
    foreach ($path in $beforeFiles.Keys) {
        Assert-True -Condition ($afterFiles.ContainsKey($path)) -Message "$Label removed Steam manifest path $path."
        $beforeFile = $beforeFiles[$path]
        $afterFile = $afterFiles[$path]
        $beforeValue = "$($beforeFile.role)`t$($beforeFile.exists)`t$($beforeFile.sizeBytes)`t$($beforeFile.sha256)"
        $afterValue = "$($afterFile.role)`t$($afterFile.exists)`t$($afterFile.sizeBytes)`t$($afterFile.sha256)"
        if ($beforeValue -eq $afterValue) {
            continue
        }
        $role = [string]$beforeFile.role
        $allowed = $role -eq "$Namespace-save" -or
            $role -eq 'shared-save' -or
            $role -eq "$Namespace-context-marker"
        Assert-True -Condition $allowed -Message "$Label changed out-of-context Steam path $path ($role)."
        if ($role -eq "$Namespace-save") {
            $namespaceSaveChanged = $true
        }
    }
    Assert-True -Condition $namespaceSaveChanged -Message "$Label did not exercise a $Namespace save-file mutation."
}

function Assert-RequiredRefs {
    param(
        [Parameter(Mandatory = $true)]$Refs,
        [Parameter(Mandatory = $true)][string[]]$Names,
        [Parameter(Mandatory = $true)][string]$Label
    )
    Assert-ExactProperties -Object $Refs -Names $Names -Label "$Label refs"
    foreach ($name in $Names) {
        Assert-True -Condition (-not [string]::IsNullOrWhiteSpace([string]$Refs.$name)) -Message "$Label ref $name is empty."
    }
}

$script:Evidence = $evidence
$script:UsedEvidence = $usedEvidence
$script:Contexts = $contexts
$script:SyncedProofs = $syncedProofs

$requiredChecks = [ordered]@{
    '1' = [ordered]@{ 'r1-verified-upload' = 'verified-upload' }
    '2' = [ordered]@{ 'r2-safe-download' = 'safe-download-with-backup' }
    '3' = [ordered]@{ 'r3-divergence-preserved' = 'divergence-preserved' }
    '4' = [ordered]@{ 'r4-modded-namespace-isolation' = 'modded-namespace-isolation' }
    '5' = [ordered]@{ 'r5-changed-mod-set-blocked' = 'changed-mod-set-blocked' }
    '6' = [ordered]@{ 'r6-two-way-branch-isolation' = 'two-way-branch-isolation' }
    '7' = [ordered]@{ 'r7-offline-retry' = 'offline-retry' }
    '8' = [ordered]@{ 'r8-crash-resume' = 'crash-resume' }
    '9' = [ordered]@{
        'r9-commit-failure' = 'commit-failure-no-success'
        'r9-readback-failure' = 'readback-failure-no-success'
    }
    '10' = [ordered]@{ 'r10-restore-undo-exact' = 'restore-undo-exact' }
}

$rows = [Collections.Generic.Dictionary[int, object]]::new()
foreach ($row in @($matrix.rows)) {
    Assert-ExactProperties -Object $row -Names @('row', 'checks') -Label 'Matrix row'
    $number = [int]$row.row
    Assert-True -Condition ($number -ge 1 -and $number -le 10) -Message "Invalid Stage 5 row number $number."
    if ($rows.ContainsKey($number)) {
        throw "Duplicate Stage 5 row $number."
    }
    $rows.Add($number, $row)
}
Assert-True -Condition ($rows.Count -eq 10) -Message 'Stage 5 matrix must contain all ten rows exactly once.'

foreach ($number in 1..10) {
    Assert-True -Condition ($rows.ContainsKey($number)) -Message "Stage 5 matrix is missing row $number."
    $expectedChecks = $requiredChecks[[string]$number]
    $actualChecks = @($rows[$number].checks)
    Assert-True -Condition ($actualChecks.Count -eq $expectedChecks.Count) -Message "Stage 5 row $number has the wrong number of semantic checks."
    $seenChecks = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($check in $actualChecks) {
        Assert-ExactProperties -Object $check -Names @('id', 'type', 'refs') -Label "Stage 5 row $number check"
        $checkId = [string]$check.id
        Assert-True -Condition ($expectedChecks.Contains($checkId)) -Message "Stage 5 row $number has unknown or misplaced semantic check $checkId."
        Assert-Equal -Label "$checkId semantic type" -Actual ([string]$check.type) -Expected ([string]$expectedChecks[$checkId])
        Assert-True -Condition ($seenChecks.Add($checkId)) -Message "Stage 5 row $number repeats semantic check $checkId."
    }
    foreach ($checkId in $expectedChecks.Keys) {
        Assert-True -Condition ($seenChecks.Contains($checkId)) -Message "Stage 5 row $number omits semantic check $checkId."
    }
}

foreach ($number in 1..10) {
    foreach ($check in @($rows[$number].checks)) {
        $refs = $check.refs
        switch ([string]$check.type) {
            'verified-upload' {
                Assert-RequiredRefs -Refs $refs -Names @('context', 'steamBefore', 'androidAfter', 'steamAfter', 'collector') -Label $check.id
                $context = Use-Context -Id $refs.context
                $steamBefore = Use-Evidence -Id $refs.steamBefore -Kind 'steam-manifest' -Row $number
                $androidAfter = Use-Evidence -Id $refs.androidAfter -Kind 'android-manifest' -Row $number
                $steamAfter = Use-Evidence -Id $refs.steamAfter -Kind 'steam-manifest' -Row $number
                $collector = Use-Evidence -Id $refs.collector -Kind 'collector' -Row $number -Phase 'after-quit-sync'
                foreach ($record in @($steamBefore, $androidAfter, $steamAfter)) { Assert-EvidenceContext -Record $record -Expected $context }
                Assert-MapsDiffer -Label 'Row 1 upload' -Left (Get-SteamSelectedMap -Record $steamBefore) -Right (Get-SteamSelectedMap -Record $steamAfter)
                Assert-SteamMutationIsolated -Before $steamBefore -After $steamAfter -Namespace 'vanilla' -Label 'Row 1 upload'
                Assert-SuccessCollector -Collector $collector
                Assert-PendingCleared -Collector $collector -Android $androidAfter
                $quitIndex = $collector.LogText.IndexOf('NGame.Quit completed final local saves; restarting launcher', [StringComparison]::Ordinal)
                $postGameIndex = $collector.LogText.IndexOf('Automatic save sync: resuming the pending post-game reconciliation.', [StringComparison]::Ordinal)
                Assert-True -Condition ($quitIndex -ge 0 -and $postGameIndex -gt $quitIndex) -Message 'Row 1 did not prove normal Quit followed by launcher-owned post-game reconciliation.'
                Assert-CollectorAndroidBytes -Collector $collector -Android $androidAfter -Label 'Row 1 Android bytes'
                Add-SyncedProof -Collector $collector -Android $androidAfter -Steam $steamAfter -Context $context -Label 'Row 1 verified upload'
            }
            'safe-download-with-backup' {
                Assert-RequiredRefs -Refs $refs -Names @('context', 'androidBefore', 'steamBefore', 'androidAfter', 'destinationBackupTreeSha256', 'steamAfter', 'collector') -Label $check.id
                $context = Use-Context -Id $refs.context
                $androidBefore = Use-Evidence -Id $refs.androidBefore -Kind 'android-manifest' -Row $number
                $steamBefore = Use-Evidence -Id $refs.steamBefore -Kind 'steam-manifest' -Row $number
                $androidAfter = Use-Evidence -Id $refs.androidAfter -Kind 'android-manifest' -Row $number
                $steamAfter = Use-Evidence -Id $refs.steamAfter -Kind 'steam-manifest' -Row $number
                $collector = Use-Evidence -Id $refs.collector -Kind 'collector' -Row $number -Phase 'before-play-reconcile'
                foreach ($record in @($androidBefore, $steamBefore, $androidAfter, $steamAfter)) { Assert-EvidenceContext -Record $record -Expected $context }
                $beforeSnapshot = Get-AndroidSnapshot -Record $androidBefore
                $afterSnapshot = Get-AndroidSnapshot -Record $androidAfter
                $beforeTreeHash = [string]$androidBefore.Data.snapshot.treeSha256
                Assert-Equal -Label 'Row 2 declared destination-backup tree' -Actual ([string]$refs.destinationBackupTreeSha256).ToLowerInvariant() -Expected $beforeTreeHash
                $backupSnapshot = Get-AndroidSnapshot -Record $androidAfter -RecoveryTreeSha256 $beforeTreeHash
                Assert-Context -Label 'Row 2 destination backup context' -Actual $backupSnapshot.Context -Expected $context
                Assert-MapsEqual -Label 'Row 2 destination backup bytes' -Left $beforeSnapshot.Files -Right $backupSnapshot.Files
                Assert-MapsDiffer -Label 'Row 2 independent remote change' -Left $beforeSnapshot.Files -Right (Get-SteamSelectedMap -Record $steamBefore)
                Assert-MapsEqual -Label 'Row 2 downloaded Android bytes' -Left $afterSnapshot.Files -Right (Get-SteamSelectedMap -Record $steamBefore)
                Assert-MapsEqual -Label 'Row 2 Steam remained unchanged' -Left (Get-SteamFullMap -Record $steamBefore) -Right (Get-SteamFullMap -Record $steamAfter)
                Assert-SuccessCollector -Collector $collector
                Assert-PendingCleared -Collector $collector -Android $androidAfter
                Assert-CollectorAndroidBytes -Collector $collector -Android $androidAfter -Label 'Row 2 Android bytes'
                Add-SyncedProof -Collector $collector -Android $androidAfter -Steam $steamAfter -Context $context -Label 'Row 2 safe download'
            }
            'divergence-preserved' {
                Assert-RequiredRefs -Refs $refs -Names @('context', 'baselineAndroid', 'baselineSteam', 'localBefore', 'remoteBefore', 'localAfter', 'remoteAfter', 'collector') -Label $check.id
                $context = Use-Context -Id $refs.context
                $baselineAndroid = Use-Evidence -Id $refs.baselineAndroid -Kind 'android-manifest' -Row $number
                $baselineSteam = Use-Evidence -Id $refs.baselineSteam -Kind 'steam-manifest' -Row $number
                $localBefore = Use-Evidence -Id $refs.localBefore -Kind 'android-manifest' -Row $number
                $remoteBefore = Use-Evidence -Id $refs.remoteBefore -Kind 'steam-manifest' -Row $number
                $localAfter = Use-Evidence -Id $refs.localAfter -Kind 'android-manifest' -Row $number
                $remoteAfter = Use-Evidence -Id $refs.remoteAfter -Kind 'steam-manifest' -Row $number
                $collector = Use-Evidence -Id $refs.collector -Kind 'collector' -Row $number -Phase 'divergence-conflict'
                foreach ($record in @($baselineAndroid, $baselineSteam, $localBefore, $remoteBefore, $localAfter, $remoteAfter)) { Assert-EvidenceContext -Record $record -Expected $context }
                $baseline = (Get-AndroidSnapshot -Record $baselineAndroid).Files
                $local = (Get-AndroidSnapshot -Record $localBefore).Files
                $remote = Get-SteamSelectedMap -Record $remoteBefore
                Assert-MapsEqual -Label 'Row 3 baseline equality' -Left $baseline -Right (Get-SteamSelectedMap -Record $baselineSteam)
                Assert-MapsDiffer -Label 'Row 3 local change' -Left $baseline -Right $local
                Assert-MapsDiffer -Label 'Row 3 remote change' -Left $baseline -Right $remote
                Assert-MapsDiffer -Label 'Row 3 divergent changes' -Left $local -Right $remote
                Assert-MapsEqual -Label 'Row 3 local preserved' -Left $local -Right (Get-AndroidSnapshot -Record $localAfter).Files
                Assert-MapsEqual -Label 'Row 3 remote preserved' -Left (Get-SteamFullMap -Record $remoteBefore) -Right (Get-SteamFullMap -Record $remoteAfter)
                Assert-PendingPresent -Collector $collector -Android $localAfter
                Assert-True -Condition ([bool]$collector.Data.gates.automaticSyncConflictLogSeen) -Message 'Row 3 did not report a synchronization conflict.'
                Assert-NoSynced -Collector $collector
                Assert-CollectorAndroidBytes -Collector $collector -Android $localAfter -Label 'Row 3 Android bytes'
            }
            'modded-namespace-isolation' {
                Assert-RequiredRefs -Refs $refs -Names @('context', 'steamBefore', 'androidAfter', 'steamAfter', 'collector') -Label $check.id
                $context = Use-Context -Id $refs.context
                $steamBefore = Use-Evidence -Id $refs.steamBefore -Kind 'steam-manifest' -Row $number
                $androidAfter = Use-Evidence -Id $refs.androidAfter -Kind 'android-manifest' -Row $number
                $steamAfter = Use-Evidence -Id $refs.steamAfter -Kind 'steam-manifest' -Row $number
                $collector = Use-Evidence -Id $refs.collector -Kind 'collector' -Row $number -Phase 'after-modded-sync'
                foreach ($record in @($steamBefore, $androidAfter, $steamAfter)) { Assert-EvidenceContext -Record $record -Expected $context }
                Assert-MapsDiffer -Label 'Row 4 modded upload' -Left (Get-SteamSelectedMap -Record $steamBefore) -Right (Get-SteamSelectedMap -Record $steamAfter)
                Assert-SteamMutationIsolated -Before $steamBefore -After $steamAfter -Namespace 'modded' -Label 'Row 4 exact mod-set upload'
                Assert-SuccessCollector -Collector $collector
                Assert-PendingCleared -Collector $collector -Android $androidAfter
                Assert-CollectorAndroidBytes -Collector $collector -Android $androidAfter -Label 'Row 4 Android bytes'
                Add-SyncedProof -Collector $collector -Android $androidAfter -Steam $steamAfter -Context $context -Label 'Row 4 modded namespace sync'
            }
            'changed-mod-set-blocked' {
                Assert-RequiredRefs -Refs $refs -Names @('localContext', 'remoteContext', 'localBefore', 'steamBefore', 'localAfter', 'steamAfter', 'collector') -Label $check.id
                $localContext = Use-Context -Id $refs.localContext
                $remoteContext = Use-Context -Id $refs.remoteContext
                Assert-True -Condition ([string]$localContext.modSetFingerprint -ne [string]$remoteContext.modSetFingerprint) -Message 'Row 5 contexts do not exercise a changed mod set.'
                $localBefore = Use-Evidence -Id $refs.localBefore -Kind 'android-manifest' -Row $number
                $steamBefore = Use-Evidence -Id $refs.steamBefore -Kind 'steam-manifest' -Row $number
                $localAfter = Use-Evidence -Id $refs.localAfter -Kind 'android-manifest' -Row $number
                $steamAfter = Use-Evidence -Id $refs.steamAfter -Kind 'steam-manifest' -Row $number
                $collector = Use-Evidence -Id $refs.collector -Kind 'collector' -Row $number -Phase 'changed-mod-set-blocked'
                foreach ($record in @($localBefore, $localAfter)) { Assert-EvidenceContext -Record $record -Expected $localContext }
                foreach ($record in @($steamBefore, $steamAfter)) { Assert-EvidenceContext -Record $record -Expected $remoteContext }
                Assert-MapsEqual -Label 'Row 5 local bytes preserved' -Left (Get-AndroidSnapshot -Record $localBefore).Files -Right (Get-AndroidSnapshot -Record $localAfter).Files
                Assert-MapsEqual -Label 'Row 5 Steam bytes preserved' -Left (Get-SteamFullMap -Record $steamBefore) -Right (Get-SteamFullMap -Record $steamAfter)
                $blockedLog = [bool]$collector.Data.gates.automaticSyncConflictLogSeen -or $collector.LogText -match '(?im)save-context mismatch|mod[- ]set|mod set'
                Assert-True -Condition $blockedLog -Message 'Row 5 has no changed-mod-set block evidence in its log.'
                Assert-NoSynced -Collector $collector
                Assert-CollectorAndroidBytes -Collector $collector -Android $localAfter -Label 'Row 5 Android bytes'
            }
            'two-way-branch-isolation' {
                Assert-RequiredRefs -Refs $refs -Names @('publicContext', 'betaContext', 'publicBefore', 'betaBefore', 'betaAfter', 'betaSteamAfter', 'publicAfter', 'publicSteamAfter', 'betaCollector', 'publicCollector') -Label $check.id
                $publicContext = Use-Context -Id $refs.publicContext
                $betaContext = Use-Context -Id $refs.betaContext
                Assert-True -Condition ([string]$publicContext.runtimeIdentity -ne [string]$betaContext.runtimeIdentity) -Message 'Row 6 contexts do not exercise two branches.'
                $publicBefore = Use-Evidence -Id $refs.publicBefore -Kind 'android-manifest' -Row $number
                $betaBefore = Use-Evidence -Id $refs.betaBefore -Kind 'android-manifest' -Row $number
                $betaAfter = Use-Evidence -Id $refs.betaAfter -Kind 'android-manifest' -Row $number
                $betaSteamAfter = Use-Evidence -Id $refs.betaSteamAfter -Kind 'steam-manifest' -Row $number
                $publicAfter = Use-Evidence -Id $refs.publicAfter -Kind 'android-manifest' -Row $number
                $publicSteamAfter = Use-Evidence -Id $refs.publicSteamAfter -Kind 'steam-manifest' -Row $number
                $betaCollector = Use-Evidence -Id $refs.betaCollector -Kind 'collector' -Row $number -Phase 'after-switch-to-beta'
                $publicCollector = Use-Evidence -Id $refs.publicCollector -Kind 'collector' -Row $number -Phase 'after-switch-to-public'
                foreach ($record in @($publicBefore, $publicAfter, $publicSteamAfter)) { Assert-EvidenceContext -Record $record -Expected $publicContext }
                foreach ($record in @($betaBefore, $betaAfter, $betaSteamAfter)) { Assert-EvidenceContext -Record $record -Expected $betaContext }
                $publicBytes = (Get-AndroidSnapshot -Record $publicBefore).Files
                $betaBytes = (Get-AndroidSnapshot -Record $betaBefore).Files
                Assert-MapsDiffer -Label 'Row 6 public/beta fixtures' -Left $publicBytes -Right $betaBytes
                Assert-MapsEqual -Label 'Row 6 beta branch restored in beta direction' -Left $betaBytes -Right (Get-AndroidSnapshot -Record $betaAfter).Files
                Assert-MapsEqual -Label 'Row 6 public branch restored in public direction' -Left $publicBytes -Right (Get-AndroidSnapshot -Record $publicAfter).Files
                Assert-SuccessCollector -Collector $betaCollector
                Assert-SuccessCollector -Collector $publicCollector
                Assert-PendingCleared -Collector $betaCollector -Android $betaAfter
                Assert-PendingCleared -Collector $publicCollector -Android $publicAfter
                Assert-CollectorAndroidBytes -Collector $betaCollector -Android $betaAfter -Label 'Row 6 beta Android bytes'
                Assert-CollectorAndroidBytes -Collector $publicCollector -Android $publicAfter -Label 'Row 6 public Android bytes'
                Add-SyncedProof -Collector $betaCollector -Android $betaAfter -Steam $betaSteamAfter -Context $betaContext -Label 'Row 6 beta branch'
                Add-SyncedProof -Collector $publicCollector -Android $publicAfter -Steam $publicSteamAfter -Context $publicContext -Label 'Row 6 public branch'
            }
            'offline-retry' {
                Assert-RequiredRefs -Refs $refs -Names @('context', 'baselineAndroid', 'baselineSteam', 'localOffline', 'remoteOffline', 'localAfterRetry', 'remoteAfterRetry', 'offlineCollector', 'retryCollector') -Label $check.id
                $context = Use-Context -Id $refs.context
                $baselineAndroid = Use-Evidence -Id $refs.baselineAndroid -Kind 'android-manifest' -Row $number
                $baselineSteam = Use-Evidence -Id $refs.baselineSteam -Kind 'steam-manifest' -Row $number
                $localOffline = Use-Evidence -Id $refs.localOffline -Kind 'android-manifest' -Row $number
                $remoteOffline = Use-Evidence -Id $refs.remoteOffline -Kind 'steam-manifest' -Row $number
                $localAfterRetry = Use-Evidence -Id $refs.localAfterRetry -Kind 'android-manifest' -Row $number
                $remoteAfterRetry = Use-Evidence -Id $refs.remoteAfterRetry -Kind 'steam-manifest' -Row $number
                $offlineCollector = Use-Evidence -Id $refs.offlineCollector -Kind 'collector' -Row $number -Phase 'offline-pending'
                $retryCollector = Use-Evidence -Id $refs.retryCollector -Kind 'collector' -Row $number -Phase 'retry-complete'
                foreach ($record in @($baselineAndroid, $baselineSteam, $localOffline, $remoteOffline, $localAfterRetry, $remoteAfterRetry)) { Assert-EvidenceContext -Record $record -Expected $context }
                $baseline = (Get-AndroidSnapshot -Record $baselineAndroid).Files
                $local = (Get-AndroidSnapshot -Record $localOffline).Files
                Assert-MapsEqual -Label 'Row 7 baseline equality' -Left $baseline -Right (Get-SteamSelectedMap -Record $baselineSteam)
                Assert-MapsDiffer -Label 'Row 7 offline local gameplay' -Left $baseline -Right $local
                Assert-MapsEqual -Label 'Row 7 remote unchanged while offline' -Left (Get-SteamFullMap -Record $baselineSteam) -Right (Get-SteamFullMap -Record $remoteOffline)
                Assert-MapsEqual -Label 'Row 7 local bytes preserved through retry' -Left $local -Right (Get-AndroidSnapshot -Record $localAfterRetry).Files
                Assert-PendingPresent -Collector $offlineCollector -Android $localOffline
                Assert-NoSynced -Collector $offlineCollector
                Assert-True -Condition ($offlineCollector.LogText -match '(?im)offline|not connected|connection|network|authentication') -Message 'Row 7 offline capture has no connection-failure evidence.'
                Assert-SuccessCollector -Collector $retryCollector
                Assert-PendingCleared -Collector $retryCollector -Android $localAfterRetry
                Assert-CollectorAndroidBytes -Collector $offlineCollector -Android $localOffline -Label 'Row 7 offline Android bytes'
                Assert-CollectorAndroidBytes -Collector $retryCollector -Android $localAfterRetry -Label 'Row 7 retry Android bytes'
                Add-SyncedProof -Collector $retryCollector -Android $localAfterRetry -Steam $remoteAfterRetry -Context $context -Label 'Row 7 retry'
            }
            'crash-resume' {
                Assert-RequiredRefs -Refs $refs -Names @('context', 'baselineAndroid', 'baselineSteam', 'localBeforeCrash', 'remoteBeforeRestart', 'localAfterRestart', 'remoteAfterRestart', 'beforeCollector', 'afterCollector') -Label $check.id
                $context = Use-Context -Id $refs.context
                $baselineAndroid = Use-Evidence -Id $refs.baselineAndroid -Kind 'android-manifest' -Row $number
                $baselineSteam = Use-Evidence -Id $refs.baselineSteam -Kind 'steam-manifest' -Row $number
                $localBeforeCrash = Use-Evidence -Id $refs.localBeforeCrash -Kind 'android-manifest' -Row $number
                $remoteBeforeRestart = Use-Evidence -Id $refs.remoteBeforeRestart -Kind 'steam-manifest' -Row $number
                $localAfterRestart = Use-Evidence -Id $refs.localAfterRestart -Kind 'android-manifest' -Row $number
                $remoteAfterRestart = Use-Evidence -Id $refs.remoteAfterRestart -Kind 'steam-manifest' -Row $number
                $beforeCollector = Use-Evidence -Id $refs.beforeCollector -Kind 'collector' -Row $number -Phase 'pending-before-force-stop'
                $afterCollector = Use-Evidence -Id $refs.afterCollector -Kind 'collector' -Row $number -Phase 'after-restart'
                foreach ($record in @($baselineAndroid, $baselineSteam, $localBeforeCrash, $remoteBeforeRestart, $localAfterRestart, $remoteAfterRestart)) { Assert-EvidenceContext -Record $record -Expected $context }
                $baseline = (Get-AndroidSnapshot -Record $baselineAndroid).Files
                $local = (Get-AndroidSnapshot -Record $localBeforeCrash).Files
                Assert-MapsEqual -Label 'Row 8 baseline equality' -Left $baseline -Right (Get-SteamSelectedMap -Record $baselineSteam)
                Assert-MapsDiffer -Label 'Row 8 pending local change' -Left $baseline -Right $local
                Assert-MapsEqual -Label 'Row 8 remote before restart' -Left (Get-SteamFullMap -Record $baselineSteam) -Right (Get-SteamFullMap -Record $remoteBeforeRestart)
                Assert-MapsEqual -Label 'Row 8 local bytes survived force-stop' -Left $local -Right (Get-AndroidSnapshot -Record $localAfterRestart).Files
                Assert-PendingPresent -Collector $beforeCollector -Android $localBeforeCrash
                Assert-NoSynced -Collector $beforeCollector
                Assert-SuccessCollector -Collector $afterCollector
                Assert-PendingCleared -Collector $afterCollector -Android $localAfterRestart
                $escapedPackage = [regex]::Escape([string]$binding.candidate.packageName)
                $forceStop = [regex]::Match($afterCollector.LogText, "(?im)Force stopping[^\r\n]*$escapedPackage|am_force_stop[^\r\n]*$escapedPackage")
                $processStart = [regex]::Match($afterCollector.LogText, "(?im)(Start proc|am_proc_start|Start process)[^\r\n]*$escapedPackage")
                Assert-True -Condition ($forceStop.Success -and $processStart.Success -and $processStart.Index -gt $forceStop.Index) -Message 'Row 8 does not contain an Android force-stop followed by a new app process.'
                Assert-True -Condition ($afterCollector.LogText -match [regex]::Escape('Automatic save sync: resuming the pending post-game reconciliation.')) -Message 'Row 8 new launcher process did not log pending reconciliation recovery.'
                Assert-CollectorAndroidBytes -Collector $beforeCollector -Android $localBeforeCrash -Label 'Row 8 pre-force-stop Android bytes'
                Assert-CollectorAndroidBytes -Collector $afterCollector -Android $localAfterRestart -Label 'Row 8 resumed Android bytes'
                Add-SyncedProof -Collector $afterCollector -Android $localAfterRestart -Steam $remoteAfterRestart -Context $context -Label 'Row 8 resumed reconciliation'
            }
            'commit-failure-no-success' {
                Assert-RequiredRefs -Refs $refs -Names @('context', 'localBefore', 'steamBefore', 'localAfter', 'steamAfter', 'collector') -Label $check.id
                $context = Use-Context -Id $refs.context
                $localBefore = Use-Evidence -Id $refs.localBefore -Kind 'android-manifest' -Row $number
                $steamBefore = Use-Evidence -Id $refs.steamBefore -Kind 'steam-manifest' -Row $number
                $localAfter = Use-Evidence -Id $refs.localAfter -Kind 'android-manifest' -Row $number
                $steamAfter = Use-Evidence -Id $refs.steamAfter -Kind 'steam-manifest' -Row $number
                $collector = Use-Evidence -Id $refs.collector -Kind 'collector' -Row $number -Phase 'commit-failure'
                foreach ($record in @($localBefore, $steamBefore, $localAfter, $steamAfter)) { Assert-EvidenceContext -Record $record -Expected $context }
                Assert-MapsDiffer -Label 'Row 9 commit-failure attempted upload' -Left (Get-AndroidSnapshot -Record $localBefore).Files -Right (Get-SteamSelectedMap -Record $steamBefore)
                Assert-MapsEqual -Label 'Row 9 commit-failure local bytes preserved' -Left (Get-AndroidSnapshot -Record $localBefore).Files -Right (Get-AndroidSnapshot -Record $localAfter).Files
                Assert-PendingPresent -Collector $collector -Android $localAfter
                Assert-True -Condition ([bool]$collector.Data.gates.commitFailureSeen) -Message 'Row 9 commit subcase did not observe file_committed=false/commit failure.'
                Assert-NoSynced -Collector $collector
                Assert-SteamCapturedAfter -Steam $steamAfter -Collector $collector -Label 'Row 9 commit failure'
                Assert-CollectorAndroidBytes -Collector $collector -Android $localAfter -Label 'Row 9 commit-failure Android bytes'
            }
            'readback-failure-no-success' {
                Assert-RequiredRefs -Refs $refs -Names @('context', 'localBefore', 'steamBefore', 'localAfter', 'steamAfter', 'collector') -Label $check.id
                $context = Use-Context -Id $refs.context
                $localBefore = Use-Evidence -Id $refs.localBefore -Kind 'android-manifest' -Row $number
                $steamBefore = Use-Evidence -Id $refs.steamBefore -Kind 'steam-manifest' -Row $number
                $localAfter = Use-Evidence -Id $refs.localAfter -Kind 'android-manifest' -Row $number
                $steamAfter = Use-Evidence -Id $refs.steamAfter -Kind 'steam-manifest' -Row $number
                $collector = Use-Evidence -Id $refs.collector -Kind 'collector' -Row $number -Phase 'readback-failure'
                foreach ($record in @($localBefore, $steamBefore, $localAfter, $steamAfter)) { Assert-EvidenceContext -Record $record -Expected $context }
                $local = (Get-AndroidSnapshot -Record $localBefore).Files
                Assert-MapsDiffer -Label 'Row 9 read-back attempted upload' -Left $local -Right (Get-SteamSelectedMap -Record $steamBefore)
                Assert-MapsEqual -Label 'Row 9 read-back local bytes preserved' -Left $local -Right (Get-AndroidSnapshot -Record $localAfter).Files
                Assert-MapsDiffer -Label 'Row 9 read-back mismatch remains unequal' -Left $local -Right (Get-SteamSelectedMap -Record $steamAfter)
                Assert-PendingPresent -Collector $collector -Android $localAfter
                Assert-True -Condition ([bool]$collector.Data.gates.readBackMismatchSeen) -Message 'Row 9 read-back subcase did not observe a remote read-back mismatch.'
                Assert-NoSynced -Collector $collector
                Assert-SteamCapturedAfter -Steam $steamAfter -Collector $collector -Label 'Row 9 read-back failure'
                Assert-CollectorAndroidBytes -Collector $collector -Android $localAfter -Label 'Row 9 read-back Android bytes'
            }
            'restore-undo-exact' {
                Assert-RequiredRefs -Refs $refs -Names @('context', 'androidOriginal', 'androidRestored', 'androidUndone', 'steamBefore', 'steamAfterRestore', 'steamAfterUndo', 'restoreCollector', 'undoCollector') -Label $check.id
                $context = Use-Context -Id $refs.context
                $original = Use-Evidence -Id $refs.androidOriginal -Kind 'android-manifest' -Row $number
                $restored = Use-Evidence -Id $refs.androidRestored -Kind 'android-manifest' -Row $number
                $undone = Use-Evidence -Id $refs.androidUndone -Kind 'android-manifest' -Row $number
                $steamBefore = Use-Evidence -Id $refs.steamBefore -Kind 'steam-manifest' -Row $number
                $steamAfterRestore = Use-Evidence -Id $refs.steamAfterRestore -Kind 'steam-manifest' -Row $number
                $steamAfterUndo = Use-Evidence -Id $refs.steamAfterUndo -Kind 'steam-manifest' -Row $number
                $restoreCollector = Use-Evidence -Id $refs.restoreCollector -Kind 'collector' -Row $number -Phase 'after-restore'
                $undoCollector = Use-Evidence -Id $refs.undoCollector -Kind 'collector' -Row $number -Phase 'after-undo'
                foreach ($record in @($original, $restored, $undone, $steamBefore, $steamAfterRestore, $steamAfterUndo)) { Assert-EvidenceContext -Record $record -Expected $context }
                Assert-True -Condition ([bool]$original.Data.cloudSyncEnabled) -Message 'Row 10 original capture must begin with the existing Cloud Sync setting enabled.'
                Assert-True -Condition (-not [bool]$restored.Data.cloudSyncEnabled) -Message 'Row 10 Restore did not persistently disable Cloud Sync for local validation.'
                Assert-True -Condition (-not [bool]$undone.Data.cloudSyncEnabled) -Message 'Row 10 Undo unexpectedly re-enabled Cloud Sync.'
                Assert-True -Condition ([bool]$restored.Data.launcherState.recoveryJournal.present) -Message 'Row 10 restored export has no verified recovery journal.'
                Assert-Equal -Label 'Row 10 restored journal phase' -Actual ([string]$restored.Data.launcherState.recoveryJournal.phase) -Expected 'validation-required'
                Assert-Context -Label 'Row 10 restored journal context' -Actual $restored.Data.launcherState.recoveryJournal.context -Expected $context
                Assert-True -Condition (-not [bool]$restored.Data.launcherState.recoveryHold.present) -Message 'Row 10 Restore left the superseded recovery-hold file present.'
                Assert-True -Condition ([bool]$undone.Data.launcherState.recoveryJournal.present) -Message 'Row 10 undone export has no verified recovery journal.'
                Assert-Equal -Label 'Row 10 undone journal phase' -Actual ([string]$undone.Data.launcherState.recoveryJournal.phase) -Expected 'undone'
                Assert-Context -Label 'Row 10 undone journal context' -Actual $undone.Data.launcherState.recoveryJournal.context -Expected $context
                Assert-True -Condition (-not [bool]$undone.Data.launcherState.recoveryHold.present) -Message 'Row 10 Undo left the superseded recovery-hold file present.'
                $originalBytes = (Get-AndroidSnapshot -Record $original).Files
                Assert-MapsDiffer -Label 'Row 10 Restore changed Android bytes' -Left $originalBytes -Right (Get-AndroidSnapshot -Record $restored).Files
                Assert-MapsEqual -Label 'Row 10 Undo exact-byte round trip' -Left $originalBytes -Right (Get-AndroidSnapshot -Record $undone).Files
                Assert-MapsEqual -Label 'Row 10 Steam unchanged by Restore' -Left (Get-SteamFullMap -Record $steamBefore) -Right (Get-SteamFullMap -Record $steamAfterRestore)
                Assert-MapsEqual -Label 'Row 10 Steam unchanged by Undo' -Left (Get-SteamFullMap -Record $steamBefore) -Right (Get-SteamFullMap -Record $steamAfterUndo)
                foreach ($collector in @($restoreCollector, $undoCollector)) {
                    Assert-True -Condition ([bool]$collector.Data.gates.recoveryLogSeen) -Message "$($collector.Spec.id) has no recovery operation log."
                    Assert-NoSynced -Collector $collector
                }
                Assert-PendingCleared -Collector $restoreCollector -Android $restored
                Assert-PendingCleared -Collector $undoCollector -Android $undone
                Assert-CollectorAndroidBytes -Collector $restoreCollector -Android $restored -Label 'Row 10 restored Android bytes'
                Assert-CollectorAndroidBytes -Collector $undoCollector -Android $undone -Label 'Row 10 undone Android bytes'
            }
            default {
                throw "Unimplemented Stage 5 semantic check type: $($check.type)"
            }
        }
    }
}

foreach ($record in $evidence.Values | Where-Object { [string]$_.Spec.kind -eq 'collector' }) {
    if (Test-HasSyncedLog -Collector $record) {
        Assert-True -Condition ($syncedProofs.Contains([string]$record.Spec.id)) -Message "$($record.Spec.id) contains a Synced log without a later independent live-Steam equality check."
    }
}

Assert-True -Condition ($usedEvidence.Count -eq $evidence.Count) -Message 'Matrix contains evidence that is not consumed by a required semantic check.'
foreach ($id in $evidence.Keys) {
    Assert-True -Condition ($usedEvidence.Contains($id)) -Message "Matrix evidence $id is not consumed by a required semantic check."
}

$report = [ordered]@{
    schemaVersion = 1
    kind = 'stage5-physical-save-matrix-review'
    reviewedUtc = [DateTime]::UtcNow.ToString('o')
    result = 'passed'
    sourceMatrix = $resolvedMatrix
    sourceMatrixSha256 = $matrixSha256
    sourceCommit = [string]$candidate.sourceCommit
    candidateApkSha256 = [string]$candidate.apkSha256
    candidateBuildInfoSha256 = [string]$candidate.buildInfoSha256
    packageName = [string]$candidate.packageName
    versionName = [string]$candidate.versionName
    versionCode = [int64]$candidate.versionCode
    signerSha256 = [string]$candidate.signerSha256
    deviceSerialSha256 = [string]$binding.device.serialSha256
    rowsPassed = 10
    semanticChecksPassed = 11
    row9SubcasesPassed = 2
    syncedCollectorsWithLaterLiveSteamProof = $syncedProofs.Count
    evidenceFilesVerified = $evidence.Count
}

if ($OutputPath) {
    $resolvedOutput = [IO.Path]::GetFullPath($OutputPath)
    if (Test-Path -LiteralPath $resolvedOutput) {
        throw "Refusing to overwrite an existing Stage 5 matrix review: $resolvedOutput"
    }
    $parent = Split-Path -Parent $resolvedOutput
    if ($parent) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
    [IO.File]::WriteAllText(
        $resolvedOutput,
        ($report | ConvertTo-Json -Depth 8),
        [Text.UTF8Encoding]::new($false)
    )
}

Write-Host 'Stage 5 physical save matrix passed: 10/10 rows, 11/11 semantic checks.'
Write-Host "Every Synced collector has later independent live-Steam byte equality: $($syncedProofs.Count)/$($syncedProofs.Count)."
