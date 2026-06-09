param(
    [string]$DeviceSerial = $env:ANDROID_SERIAL,
    [int]$WaitSeconds = 90,
    [switch]$DumpSaveFiles
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$collector = Join-Path $scriptDir "collect-android-save-validation.ps1"

if (-not (Test-Path -LiteralPath $collector)) {
    throw "Collector script not found: $collector"
}

$collectorArgs = @{
    DeviceSerial = $DeviceSerial
    ClearLogcat = $true
    EnableVerboseSaveDiagnostics = $true
    WaitSeconds = $WaitSeconds
}

if ($DumpSaveFiles) {
    $collectorArgs.DumpSaveFiles = $true
}

Write-Host "Starting Android save validation capture."
Write-Host "Device: $DeviceSerial"
Write-Host "Wait: $WaitSeconds seconds"
Write-Host "While the collector waits:"
Write-Host "  1. Open or focus the launcher on the phone."
Write-Host "  2. Tap Pull from Cloud."
Write-Host "  3. If pull writes files, tap START GAME / PLAY normally."
Write-Host "  4. Proceed far enough to check save/profile visibility."

& $collector @collectorArgs
