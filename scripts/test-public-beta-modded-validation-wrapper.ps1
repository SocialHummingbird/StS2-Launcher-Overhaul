param()

$ErrorActionPreference = "Stop"

$scriptPath = Join-Path $PSScriptRoot "run-public-beta-modded-validation.ps1"
$tokens = $null
$errors = $null
[System.Management.Automation.Language.Parser]::ParseFile(
    (Resolve-Path -LiteralPath $scriptPath),
    [ref]$tokens,
    [ref]$errors
) | Out-Null

if ($errors.Count -gt 0) {
    foreach ($errorRecord in $errors) {
        Write-Host "PARSE FAIL $($errorRecord.Message)"
    }

    throw "run-public-beta-modded-validation.ps1 has parser errors."
}

$content = Get-Content -Raw -LiteralPath $scriptPath
$checks = [ordered]@{
    "writes validation-result.json" = 'validation-result\.json'
    "records capture failures" = 'Workshop evidence capture failed'
    "records review failures" = 'Workshop evidence review failed'
    "records save-validation failures" = 'Save validation capture failed'
    "records setup/run failures" = 'Public-beta modded validation setup/run failed'
    "uses centralized Android shell quoting" = 'ConvertTo-AndroidShellSingleQuoted'
    "uses named capture splat" = '\$captureArgs\s*=\s*@\{'
    "uses named review splat" = '\$reviewArgs\s*=\s*@\{'
    "passes public-beta review phase" = 'RequirePhase\s*=\s*"public-beta"'
    "requires fresh modded launch marker" = 'Assert-PublicBetaModdedLaunchMarker'
    "rejects stale modded marker" = 'Modded launch marker is stale'
    "requires BaseLib evidence" = '"BaseLib"'
    "requires Quick Restart evidence" = 'Quick\\s\*Restart\|QuickRestart'
    "requires SavesMerger evidence" = '"SavesMerger"'
    "throws on validation failure" = 'Public-beta modded validation failed'
    "declares no Steam Cloud Push in result" = 'steamCloudPushPerformed\s*=\s*\$false'
    "keeps artifact hygiene text" = 'This script does not press Steam Cloud Push'
}

$failures = [System.Collections.Generic.List[string]]::new()
foreach ($entry in $checks.GetEnumerator()) {
    if ($content -notmatch $entry.Value) {
        $failures.Add($entry.Key)
        continue
    }

    Write-Host "PASS $($entry.Key)"
}

if ($content -match '(?i)(PushToCloud|Manual Push completed|Steam Cloud Push performed)') {
    $failures.Add("contains a Steam Cloud Push execution marker")
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) {
        Write-Host "FAIL $failure"
    }

    throw "Public-beta modded validation wrapper regression test failed."
}

$root = Split-Path -Parent $PSScriptRoot
$runLabel = "no-device-regression-$([guid]::NewGuid().ToString("N"))"
$relativeOutputRoot = "tmp\public-beta-modded-validation-wrapper-tests"
$expectedOutputDir = Join-Path $root (Join-Path $relativeOutputRoot "public-beta-modded-validation-$runLabel")
$expectedResult = Join-Path $expectedOutputDir "diagnostics\validation-result.json"
$threwExpectedFailure = $false

try {
    & $scriptPath `
        -DeviceSerial "codex-missing-device-$runLabel" `
        -OutputRoot $relativeOutputRoot `
        -RunLabel $runLabel `
        -WaitForDeviceSeconds 0 `
        -SkipScreenshot `
        -SkipSaveValidation
} catch {
    if ($_.Exception.Message -match "Public-beta modded validation failed") {
        $threwExpectedFailure = $true
    } else {
        throw
    }
}

if (-not $threwExpectedFailure) {
    throw "Wrapper no-device regression did not throw the expected validation failure."
}

if (-not (Test-Path -LiteralPath $expectedResult)) {
    throw "Wrapper no-device regression did not write validation-result.json at $expectedResult"
}

$result = Get-Content -Raw -LiteralPath $expectedResult | ConvertFrom-Json
if ("$($result.status)" -ne "failed") {
    throw "Wrapper no-device regression result status was not failed."
}

if ([bool]$result.steamCloudPushPerformed -ne $false) {
    throw "Wrapper no-device regression result did not preserve Steam Cloud Push safety."
}

if (@($result.failures).Count -lt 1) {
    throw "Wrapper no-device regression did not record a failure reason."
}

Write-Host "PASS no-device run writes failed validation-result.json"
Write-Host "Public-beta modded validation wrapper regression test passed."
