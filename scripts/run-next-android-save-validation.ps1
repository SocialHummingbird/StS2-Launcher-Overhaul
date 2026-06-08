param(
    [string]$DeviceSerial = "RFCY70XQE7F",
    [int]$WaitSeconds = 120,
    [switch]$DumpSaveFiles
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$buildScript = Join-Path $scriptDir "build-android-local.ps1"
$captureScript = Join-Path $scriptDir "start-android-save-validation-capture.ps1"

if (-not (Test-Path -LiteralPath $buildScript)) {
    throw "Build script not found: $buildScript"
}

if (-not (Test-Path -LiteralPath $captureScript)) {
    throw "Capture script not found: $captureScript"
}

Write-Host "Building and installing Android APK for $DeviceSerial..."
& $buildScript -Install -DeviceSerial $DeviceSerial

$captureArgs = @{
    DeviceSerial = $DeviceSerial
    WaitSeconds = $WaitSeconds
}

if ($DumpSaveFiles) {
    $captureArgs.DumpSaveFiles = $true
}

Write-Host "Build/install finished. Starting validation capture..."
& $captureScript @captureArgs
