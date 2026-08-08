[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('PreInstall', 'PostInstall')]
    [string]$Mode,
    [Parameter(Mandatory = $true)]
    [string]$CandidateApkPath,
    [Parameter(Mandatory = $true)]
    [string]$CandidateChecksumPath,
    [Parameter(Mandatory = $true)]
    [string]$CandidateBuildInfoPath,
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9a-fA-F]{40}$')]
    [string]$ExpectedSourceCommit,
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[1-9][0-9]*$')]
    [string]$ExpectedCandidateRunId,
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[1-9][0-9]*$')]
    [string]$ExpectedCandidateRunAttempt,
    [string]$PriorManifestPath = '',
    [string]$AdbPath = 'adb',
    [string]$AaptPath = '',
    [string]$ApkSignerPath = '',
    [string]$GitPath = 'git',
    [string]$OutputRoot = 'artifacts\stage5-affected-device-preflight'
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'android-signing-utils.ps1')

$requiredPackage = 'com.sts2launcher.overhaul.fork.local'
$baselineVersionCode = [int64]416001
$baselineVersionName = '0.2.416-startup-recovery-ime-local'
$baselineSignerSha256 = 'FD0E3D5ACF435C1D23BFC5C426E99AA9EB5808619FF1FC214FFCA99CFAC7E57A'
$baselineApkSha256 = 'fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b'
$baselineTag = 'v0.2.416-startup-recovery-ime'
$baselineAssetName = 'StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk'

function Get-Sha256 {
    param([Parameter(Mandatory = $true)][string]$Path)

    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Resolve-RequiredFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Description
    )

    $resolved = Resolve-Path -LiteralPath $Path -ErrorAction Stop
    if (-not (Test-Path -LiteralPath $resolved.Path -PathType Leaf)) {
        throw "$Description is not a file: $Path"
    }
    return $resolved.Path
}

function Invoke-ExternalCapture {
    param(
        [Parameter(Mandatory = $true)][string]$Executable,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )

    $oldPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = @(& $Executable @Arguments 2>&1 | ForEach-Object { [string]$_ })
        $exitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $oldPreference
    }

    return [pscustomobject]@{
        ExitCode = [int]$exitCode
        Lines = @($output)
        Text = ($output -join "`n").Trim()
    }
}

function Invoke-RequiredExternal {
    param(
        [Parameter(Mandatory = $true)][string]$Executable,
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [Parameter(Mandatory = $true)][string]$Description
    )

    $result = Invoke-ExternalCapture -Executable $Executable -Arguments $Arguments
    if ($result.ExitCode -ne 0) {
        throw "$Description failed with exit code $($result.ExitCode): $($result.Text)"
    }
    return $result
}

function Read-CandidateChecksum {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$ApkName
    )

    $lines = @([IO.File]::ReadAllLines($Path) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($lines.Count -ne 1) {
        throw 'Candidate checksum must contain exactly one non-empty SHA-256 line.'
    }
    $match = [regex]::Match($lines[0].Trim(), '^([0-9a-fA-F]{64})(?:[ \t]+\*?(.+))?$')
    if (-not $match.Success) {
        throw 'Candidate checksum is not a valid SHA-256 sidecar.'
    }
    if ($match.Groups[2].Success -and $match.Groups[2].Value -ne $ApkName) {
        throw "Candidate checksum names '$($match.Groups[2].Value)', expected '$ApkName'."
    }
    return $match.Groups[1].Value.ToLowerInvariant()
}

function Read-BuildInfo {
    param([Parameter(Mandatory = $true)][string]$Path)

    $values = @{}
    foreach ($line in [IO.File]::ReadAllLines($Path)) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $match = [regex]::Match($line, '^([a-z0-9_]+)=(.*)$')
        if (-not $match.Success) {
            throw "Malformed candidate build-info line: $line"
        }
        $key = $match.Groups[1].Value
        if ($values.ContainsKey($key)) {
            throw "Duplicate candidate build-info field: $key"
        }
        $values[$key] = $match.Groups[2].Value
    }
    return $values
}

function Assert-BuildInfoValue {
    param(
        [Parameter(Mandatory = $true)][hashtable]$BuildInfo,
        [Parameter(Mandatory = $true)][string]$Key,
        [Parameter(Mandatory = $true)][AllowEmptyString()][string]$Expected
    )

    if (-not $BuildInfo.ContainsKey($Key)) {
        throw "Candidate build-info is missing: $Key"
    }
    if ([string]$BuildInfo[$Key] -cne $Expected) {
        throw "Candidate build-info $Key mismatch. Expected '$Expected', got '$($BuildInfo[$Key])'."
    }
}

function Get-OnlyRegexValue {
    param(
        [Parameter(Mandatory = $true)][string]$Text,
        [Parameter(Mandatory = $true)][string]$Pattern,
        [Parameter(Mandatory = $true)][string]$Description
    )

    $matches = [regex]::Matches($Text, $Pattern, [Text.RegularExpressions.RegexOptions]::Multiline)
    if ($matches.Count -ne 1) {
        throw "Expected exactly one $Description in package metadata; found $($matches.Count)."
    }
    return $matches[0].Groups[1].Value.Trim()
}

function Resolve-OnlyAuthorizedDevice {
    param([Parameter(Mandatory = $true)][string]$Adb)

    $result = Invoke-RequiredExternal -Executable $Adb -Arguments @('devices', '-l') -Description 'ADB device enumeration'
    $rows = @($result.Lines | Select-Object -Skip 1 | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    $authorized = @()
    foreach ($row in $rows) {
        $match = [regex]::Match($row, '^(\S+)\s+(\S+)(?:\s|$)')
        if (-not $match.Success) {
            throw "Unrecognized ADB device row: $row"
        }
        if ($match.Groups[2].Value -eq 'device') {
            $authorized += $match.Groups[1].Value
        }
    }
    if ($authorized.Count -ne 1) {
        throw "Expected exactly one authorized Android target; found $($authorized.Count)."
    }
    return [string]$authorized[0]
}

function Assert-AppProcessAbsent {
    param(
        [Parameter(Mandatory = $true)][string]$Adb,
        [Parameter(Mandatory = $true)][string]$Serial
    )

    $result = Invoke-ExternalCapture -Executable $Adb -Arguments @('-s', $Serial, 'shell', 'pidof', $requiredPackage)
    if ($result.ExitCode -eq 0 -or -not [string]::IsNullOrWhiteSpace($result.Text)) {
        throw "The affected app process is running ($($result.Text)). Close it normally before preflight; this script will not stop it."
    }
    if ($result.ExitCode -ne 1) {
        throw "Could not prove that the affected app process is absent; pidof exited $($result.ExitCode)."
    }
}

function Get-DeviceSnapshot {
    param(
        [Parameter(Mandatory = $true)][string]$Adb,
        [Parameter(Mandatory = $true)][string]$Serial
    )

    Assert-AppProcessAbsent -Adb $Adb -Serial $Serial
    $model = (Invoke-RequiredExternal -Executable $Adb -Arguments @('-s', $Serial, 'shell', 'getprop', 'ro.product.model') -Description 'Device model read').Text
    $fingerprint = (Invoke-RequiredExternal -Executable $Adb -Arguments @('-s', $Serial, 'shell', 'getprop', 'ro.build.fingerprint') -Description 'Device fingerprint read').Text
    if ([string]::IsNullOrWhiteSpace($model) -or [string]::IsNullOrWhiteSpace($fingerprint)) {
        throw 'Device model or fingerprint was empty.'
    }

    $pathResult = Invoke-RequiredExternal -Executable $Adb -Arguments @('-s', $Serial, 'shell', 'pm', 'path', $requiredPackage) -Description 'Installed package path read'
    $packagePaths = @($pathResult.Lines | ForEach-Object { $_.Trim() } | Where-Object { $_.StartsWith('package:') })
    if ($packagePaths.Count -ne 1) {
        throw "Expected one installed base APK path for $requiredPackage; found $($packagePaths.Count)."
    }
    $basePath = $packagePaths[0].Substring('package:'.Length)
    if ([string]::IsNullOrWhiteSpace($basePath) -or -not $basePath.EndsWith('/base.apk')) {
        throw "Installed package path is not an exact base.apk path: $basePath"
    }

    $uidResult = Invoke-RequiredExternal -Executable $Adb -Arguments @('-s', $Serial, 'shell', 'pm', 'list', 'packages', '-U', $requiredPackage) -Description 'Installed package UID read'
    $uidMatches = [regex]::Matches($uidResult.Text, "(?m)^package:$([regex]::Escape($requiredPackage))\s+uid:([0-9]+)\s*$")
    if ($uidMatches.Count -ne 1) {
        throw "Expected exactly one UID for the exact required package; found $($uidMatches.Count)."
    }
    $stableUid = $uidMatches[0].Groups[1].Value

    $dump = (Invoke-RequiredExternal -Executable $Adb -Arguments @('-s', $Serial, 'shell', 'dumpsys', 'package', $requiredPackage) -Description 'Installed package metadata read').Text
    if ($dump -notmatch "(?m)^\s*Package \[$([regex]::Escape($requiredPackage))\]") {
        throw "Package metadata does not identify the exact required package: $requiredPackage"
    }
    $versionCodeText = Get-OnlyRegexValue -Text $dump -Pattern '^\s*versionCode=([0-9]+)(?:\s|$)' -Description 'versionCode'
    $parsedVersionCode = [int64]0
    if (-not [int64]::TryParse($versionCodeText, [ref]$parsedVersionCode)) {
        throw "Invalid installed versionCode: $versionCodeText"
    }
    $dumpAppId = Get-OnlyRegexValue -Text $dump -Pattern '^\s*appId=([0-9]+)\s*$' -Description 'appId'
    if ($dumpAppId -cne $stableUid) {
        throw "Package UID $stableUid does not match dumpsys appId $dumpAppId."
    }

    return [ordered]@{
        serial = $Serial
        model = $model
        fingerprint = $fingerprint
        packageName = $requiredPackage
        versionCode = $parsedVersionCode
        uid = $stableUid
        firstInstallTime = Get-OnlyRegexValue -Text $dump -Pattern '^\s*firstInstallTime=(.+)$' -Description 'firstInstallTime'
        dataDir = Get-OnlyRegexValue -Text $dump -Pattern '^\s*dataDir=(\S+)\s*$' -Description 'dataDir'
        baseApkDevicePath = $basePath
    }
}

function Assert-SnapshotsStable {
    param(
        [Parameter(Mandatory = $true)]$Before,
        [Parameter(Mandatory = $true)]$After
    )

    foreach ($field in @('serial', 'model', 'fingerprint', 'packageName', 'versionCode', 'uid', 'firstInstallTime', 'dataDir', 'baseApkDevicePath')) {
        if ([string]$Before[$field] -cne [string]$After[$field]) {
            throw "Device package identity changed during read-only capture: $field."
        }
    }
}

function Read-VerifiedPriorManifest {
    param([Parameter(Mandatory = $true)][string]$Path)

    $resolved = Resolve-RequiredFile -Path $Path -Description 'Prior preflight manifest'
    $sidecar = "$resolved.sha256"
    if (-not (Test-Path -LiteralPath $sidecar -PathType Leaf)) {
        throw "Prior preflight manifest checksum is missing: $sidecar"
    }
    $expected = Read-CandidateChecksum -Path $sidecar -ApkName ([IO.Path]::GetFileName($resolved))
    $actual = Get-Sha256 -Path $resolved
    if ($actual -ne $expected) {
        throw "Prior preflight manifest checksum mismatch. Expected $expected, got $actual."
    }
    try {
        $manifest = [IO.File]::ReadAllText($resolved) | ConvertFrom-Json
    } catch {
        throw "Prior preflight manifest is not valid JSON: $($_.Exception.Message)"
    }
    if ([int]$manifest.schemaVersion -ne 1 -or [string]$manifest.mode -ne 'PreInstall' -or [string]$manifest.result -ne 'verified') {
        throw 'Prior manifest is not a verified PreInstall manifest with schemaVersion 1.'
    }
    return $manifest
}

$candidateApk = Resolve-RequiredFile -Path $CandidateApkPath -Description 'Candidate APK'
$candidateChecksum = Resolve-RequiredFile -Path $CandidateChecksumPath -Description 'Candidate checksum'
$candidateBuildInfo = Resolve-RequiredFile -Path $CandidateBuildInfoPath -Description 'Candidate build-info'
if ([IO.Path]::GetExtension($candidateApk) -cne '.apk') {
    throw "Candidate APK must have an .apk extension: $candidateApk"
}
$candidateHash = Get-Sha256 -Path $candidateApk
$sidecarHash = Read-CandidateChecksum -Path $candidateChecksum -ApkName ([IO.Path]::GetFileName($candidateApk))
if ($candidateHash -ne $sidecarHash) {
    throw "Candidate APK checksum mismatch. Sidecar says $sidecarHash, actual bytes are $candidateHash."
}

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$gitHead = (Invoke-RequiredExternal -Executable $GitPath -Arguments @('-C', $repositoryRoot, 'rev-parse', 'HEAD') -Description 'Current source commit read').Text.ToLowerInvariant()
$expectedCommit = $ExpectedSourceCommit.ToLowerInvariant()
if ($gitHead -notmatch '^[0-9a-f]{40}$' -or $gitHead -ne $expectedCommit) {
    throw "Candidate source expectation is not the current repository HEAD. Expected $expectedCommit, current $gitHead."
}

$candidateIdentity = Get-AndroidApkIdentity -Path $candidateApk -Aapt $AaptPath -ApkSigner $ApkSignerPath
if ([string]$candidateIdentity.packageName -ne $requiredPackage) {
    throw "Candidate package mismatch. Expected $requiredPackage, got $($candidateIdentity.packageName)."
}
if ([string]$candidateIdentity.signerSha256 -ne $baselineSignerSha256) {
    throw "Candidate signer mismatch. Expected $baselineSignerSha256, got $($candidateIdentity.signerSha256)."
}
if ([int64]$candidateIdentity.versionCode -le $baselineVersionCode) {
    throw "Candidate versionCode must be greater than baseline $baselineVersionCode; got $($candidateIdentity.versionCode)."
}

$buildInfo = Read-BuildInfo -Path $candidateBuildInfo
Assert-BuildInfoValue $buildInfo 'package_name' $requiredPackage
Assert-BuildInfoValue $buildInfo 'version_name' ([string]$candidateIdentity.versionName)
Assert-BuildInfoValue $buildInfo 'version_code' ([string]$candidateIdentity.versionCode)
Assert-BuildInfoValue $buildInfo 'abi' 'arm64-v8a'
Assert-BuildInfoValue $buildInfo 'signing_channel' 'release'
Assert-BuildInfoValue $buildInfo 'signer_sha256' $baselineSignerSha256
Assert-BuildInfoValue $buildInfo 'source_commit' $expectedCommit
Assert-BuildInfoValue $buildInfo 'candidate_run_id' $ExpectedCandidateRunId
Assert-BuildInfoValue $buildInfo 'candidate_run_attempt' $ExpectedCandidateRunAttempt
Assert-BuildInfoValue $buildInfo 'apk_sha256' $candidateHash
Assert-BuildInfoValue $buildInfo 'update_baseline_tag' $baselineTag
Assert-BuildInfoValue $buildInfo 'update_baseline_asset_name' $baselineAssetName
Assert-BuildInfoValue $buildInfo 'update_baseline_apk_sha256' $baselineApkSha256

if ($Mode -eq 'PreInstall' -and -not [string]::IsNullOrWhiteSpace($PriorManifestPath)) {
    throw 'PreInstall mode does not accept a prior manifest.'
}
if ($Mode -eq 'PostInstall' -and [string]::IsNullOrWhiteSpace($PriorManifestPath)) {
    throw 'PostInstall mode requires -PriorManifestPath from the verified PreInstall capture.'
}

$prior = $null
if ($Mode -eq 'PostInstall') {
    $prior = Read-VerifiedPriorManifest -Path $PriorManifestPath
    if ([string]$prior.baseline.packageName -cne $requiredPackage -or
        [int64]$prior.baseline.versionCode -ne $baselineVersionCode -or
        [string]$prior.baseline.versionName -cne $baselineVersionName -or
        [string]$prior.baseline.signerSha256 -cne $baselineSignerSha256 -or
        [string]$prior.baseline.apkSha256 -cne $baselineApkSha256 -or
        [string]$prior.device.packageName -cne $requiredPackage -or
        [int64]$prior.device.versionCode -ne $baselineVersionCode -or
        [string]$prior.device.pulledApkSha256 -cne $baselineApkSha256 -or
        [string]$prior.device.pulledApkSignerSha256 -cne $baselineSignerSha256 -or
        [string]::IsNullOrWhiteSpace([string]$prior.device.uid) -or
        [string]::IsNullOrWhiteSpace([string]$prior.device.firstInstallTime) -or
        [string]::IsNullOrWhiteSpace([string]$prior.device.dataDir)) {
        throw 'Prior manifest does not prove the exact pinned baseline and app-data identity.'
    }
    $resolvedPriorManifest = (Resolve-Path -LiteralPath $PriorManifestPath).Path
    $retainedPriorApk = Join-Path (Split-Path -Parent $resolvedPriorManifest) 'installed-base.apk'
    if (-not (Test-Path -LiteralPath $retainedPriorApk -PathType Leaf) -or
        (Get-Sha256 -Path $retainedPriorApk) -ne $baselineApkSha256 -or
        [IO.Path]::GetFullPath([string]$prior.device.pulledApkPath) -cne [IO.Path]::GetFullPath($retainedPriorApk)) {
        throw 'Prior manifest no longer has its exact retained baseline APK bytes.'
    }
    foreach ($binding in @(
        @('packageName', $requiredPackage),
        @('apkSha256', $candidateHash),
        @('checksumSha256', (Get-Sha256 -Path $candidateChecksum)),
        @('buildInfoSha256', (Get-Sha256 -Path $candidateBuildInfo)),
        @('sourceCommit', $expectedCommit),
        @('candidateRunId', $ExpectedCandidateRunId),
        @('candidateRunAttempt', $ExpectedCandidateRunAttempt)
    )) {
        $field = [string]$binding[0]
        $expected = [string]$binding[1]
        if ([string]$prior.candidate.$field -cne $expected) {
            throw "Prior manifest candidate binding mismatch: $field."
        }
    }
}

$adbCommand = Get-Command $AdbPath -ErrorAction Stop
$resolvedAdb = if (Test-Path -LiteralPath $AdbPath -PathType Leaf) { (Resolve-Path -LiteralPath $AdbPath).Path } else { $adbCommand.Source }
$serial = Resolve-OnlyAuthorizedDevice -Adb $resolvedAdb

$outputRootCandidate = $OutputRoot
if (-not [IO.Path]::IsPathRooted($outputRootCandidate)) {
    $outputRootCandidate = Join-Path (Get-Location).Path $outputRootCandidate
}
$outputRootPath = [IO.Path]::GetFullPath($outputRootCandidate)
New-Item -ItemType Directory -Force -Path $outputRootPath | Out-Null
$captureName = 'stage5-affected-device-{0}-{1}-{2}' -f $Mode.ToLowerInvariant(), [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmssfff'), [Guid]::NewGuid().ToString('N')
$evidenceDirectory = Join-Path $outputRootPath $captureName
New-Item -ItemType Directory -Path $evidenceDirectory | Out-Null

$before = Get-DeviceSnapshot -Adb $resolvedAdb -Serial $serial
$installedApkPath = Join-Path $evidenceDirectory 'installed-base.apk'
Invoke-RequiredExternal -Executable $resolvedAdb -Arguments @('-s', $serial, 'pull', [string]$before.baseApkDevicePath, $installedApkPath) -Description 'Installed base APK read' | Out-Null
if (-not (Test-Path -LiteralPath $installedApkPath -PathType Leaf) -or (Get-Item -LiteralPath $installedApkPath).Length -le 0) {
    throw 'ADB did not produce a non-empty installed base APK evidence file.'
}
$after = Get-DeviceSnapshot -Adb $resolvedAdb -Serial $serial
Assert-SnapshotsStable -Before $before -After $after

$installedHash = Get-Sha256 -Path $installedApkPath
$installedIdentity = Get-AndroidApkIdentity -Path $installedApkPath -Aapt $AaptPath -ApkSigner $ApkSignerPath
if ([string]$installedIdentity.packageName -ne $requiredPackage -or [int64]$installedIdentity.versionCode -ne [int64]$before.versionCode) {
    throw 'Pulled installed APK identity does not match the exact package metadata read from the device.'
}
if ([string]$installedIdentity.signerSha256 -ne $baselineSignerSha256) {
    throw "Installed APK signer does not match the affected .local lineage: $($installedIdentity.signerSha256)."
}

if ($Mode -eq 'PreInstall') {
    if ($installedHash -ne $baselineApkSha256 -or [int64]$installedIdentity.versionCode -ne $baselineVersionCode -or [string]$installedIdentity.versionName -ne $baselineVersionName) {
        throw 'PreInstall capture is not the exact pinned v0.2.416 .local baseline APK.'
    }
} else {
    if ($installedHash -ne $candidateHash -or [int64]$installedIdentity.versionCode -ne [int64]$candidateIdentity.versionCode -or [string]$installedIdentity.versionName -ne [string]$candidateIdentity.versionName) {
        throw 'PostInstall capture is not byte-for-byte the exact candidate APK.'
    }
    foreach ($field in @('serial', 'packageName', 'uid', 'firstInstallTime', 'dataDir')) {
        if ([string]$prior.device.$field -cne [string]$before[$field]) {
            throw "PostInstall app-data identity does not match PreInstall: $field."
        }
    }
}

$manifestPath = Join-Path $evidenceDirectory 'preflight-manifest.json'
$manifest = [ordered]@{
    schemaVersion = 1
    mode = $Mode
    result = 'verified'
    capturedAtUtc = [DateTime]::UtcNow.ToString('o')
    readOnlyDeviceOperations = @('devices -l', 'shell pidof', 'shell getprop', 'shell pm path', 'shell pm list packages -U', 'shell dumpsys package', 'pull base.apk')
    baseline = [ordered]@{
        packageName = $requiredPackage
        versionCode = $baselineVersionCode
        versionName = $baselineVersionName
        signerSha256 = $baselineSignerSha256
        apkSha256 = $baselineApkSha256
        releaseTag = $baselineTag
        assetName = $baselineAssetName
    }
    candidate = [ordered]@{
        apkPath = $candidateApk
        apkSha256 = $candidateHash
        checksumPath = $candidateChecksum
        checksumSha256 = Get-Sha256 -Path $candidateChecksum
        buildInfoPath = $candidateBuildInfo
        buildInfoSha256 = Get-Sha256 -Path $candidateBuildInfo
        packageName = [string]$candidateIdentity.packageName
        versionCode = [int64]$candidateIdentity.versionCode
        versionName = [string]$candidateIdentity.versionName
        signerSha256 = [string]$candidateIdentity.signerSha256
        sourceCommit = $expectedCommit
        candidateRunId = $ExpectedCandidateRunId
        candidateRunAttempt = $ExpectedCandidateRunAttempt
    }
    device = [ordered]@{
        serial = [string]$before.serial
        model = [string]$before.model
        fingerprint = [string]$before.fingerprint
        packageName = [string]$before.packageName
        versionCode = [int64]$before.versionCode
        uid = [string]$before.uid
        firstInstallTime = [string]$before.firstInstallTime
        dataDir = [string]$before.dataDir
        baseApkDevicePath = [string]$before.baseApkDevicePath
        pulledApkPath = $installedApkPath
        pulledApkSizeBytes = [int64](Get-Item -LiteralPath $installedApkPath).Length
        pulledApkSha256 = $installedHash
        pulledApkSignerSha256 = [string]$installedIdentity.signerSha256
    }
    priorManifest = if ($prior) { (Resolve-Path -LiteralPath $PriorManifestPath).Path } else { $null }
}

[IO.File]::WriteAllText($manifestPath, ($manifest | ConvertTo-Json -Depth 8), [Text.UTF8Encoding]::new($false))
$manifestHash = Get-Sha256 -Path $manifestPath
$manifestSidecar = "$manifestPath.sha256"
[IO.File]::WriteAllText($manifestSidecar, "$manifestHash  $([IO.Path]::GetFileName($manifestPath))`n", [Text.Encoding]::ASCII)

Write-Host "Stage 5 affected-device $Mode preflight verified without changing the device."
Write-Host "Evidence: $evidenceDirectory"
Write-Host "Manifest: $manifestPath"
Write-Host "Manifest SHA-256: $manifestHash"
