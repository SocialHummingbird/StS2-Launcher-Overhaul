param(
    [string]$DeviceSerial = "",
    [string]$AdbPath = "C:\Users\ap010\.w40k-android-toolchain\android-sdk\platform-tools\adb.exe",
    [string]$CredentialsPath = "tmp\steam-login.local.json",
    [string]$ApkPath = "",
    [string]$PackageName = "",
    [string]$GuardCode = "",
    [switch]$PromptForGuardCode,
    [switch]$WaitForManualGuardCode,
    [switch]$WaitForManualGuardSubmit,
    [switch]$WaitForPostGuardResult,
    [int]$LoginResultTimeoutSeconds = 240,
    [int]$PostGuardResultTimeoutSeconds = 240,
    [switch]$SkipCryptoPatchVerification
)

$ErrorActionPreference = "Stop"

function Get-ConnectedPhysicalDeviceSerial {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath
    )

    $devices = @(
        & $AdbPath devices |
            Select-Object -Skip 1 |
            ForEach-Object {
                $line = ([string]$_).Trim()
                if ($line -match "^([^\s]+)\s+device(?:\s+.*)?$") {
                    $Matches[1]
                }
            } |
            Where-Object {
                -not [string]::IsNullOrWhiteSpace($_) -and -not $_.StartsWith("emulator-")
            }
    )

    if ($devices.Count -eq 1) {
        return $devices[0]
    }

    if ($devices.Count -gt 1) {
        throw "Multiple physical Android devices are connected: $($devices -join ', '). Pass -DeviceSerial explicitly."
    }

    throw "No physical Android device is connected. Connect a phone/tablet with USB debugging enabled."
}

if (-not (Test-Path -LiteralPath $AdbPath)) {
    throw "adb not found: $AdbPath"
}

if ([string]::IsNullOrWhiteSpace($DeviceSerial)) {
    $DeviceSerial = Get-ConnectedPhysicalDeviceSerial -AdbPath $AdbPath
}

if ($DeviceSerial.StartsWith("emulator-")) {
    throw "Physical-device validation cannot use emulator serial: $DeviceSerial"
}

$safeDeviceSerial = $DeviceSerial -replace '[^A-Za-z0-9_.-]', '_'
$outputPrefix = "artifacts\android\physical-login-$safeDeviceSerial"
$summaryPath = "$outputPrefix-summary.txt"
$logcatPath = "$outputPrefix-logcat.txt"
$screenshotPath = "$outputPrefix.png"

New-Item -ItemType Directory -Force (Split-Path -Parent $summaryPath) | Out-Null
@(
    "Device serial: $DeviceSerial",
    "APK path: $ApkPath",
    "Package override: $PackageName",
    "Credentials path: $CredentialsPath",
    "Logcat path: $logcatPath",
    "Screenshot path: $screenshotPath"
) | Set-Content -LiteralPath $summaryPath -Encoding UTF8

$boundaryArgs = @(
    "-DeviceSerial", $DeviceSerial,
    "-AdbPath", $AdbPath,
    "-CredentialsPath", $CredentialsPath,
    "-OutputLogcatPath", $logcatPath,
    "-OutputScreenshotPath", $screenshotPath,
    "-LoginResultTimeoutSeconds", $LoginResultTimeoutSeconds,
    "-PostGuardResultTimeoutSeconds", $PostGuardResultTimeoutSeconds
)

if (-not [string]::IsNullOrWhiteSpace($ApkPath)) {
    $boundaryArgs += @("-ApkPath", $ApkPath)
}

if (-not [string]::IsNullOrWhiteSpace($PackageName)) {
    $boundaryArgs += @("-PackageName", $PackageName)
}

if (-not [string]::IsNullOrWhiteSpace($GuardCode)) {
    $boundaryArgs += @("-GuardCode", $GuardCode)
}

if ($PromptForGuardCode) {
    $boundaryArgs += "-PromptForGuardCode"
}

if ($WaitForManualGuardCode) {
    $boundaryArgs += "-WaitForManualGuardCode"
}

if ($WaitForManualGuardSubmit) {
    $boundaryArgs += "-WaitForManualGuardSubmit"
}

if ($WaitForPostGuardResult) {
    $boundaryArgs += "-WaitForPostGuardResult"
}

if ($SkipCryptoPatchVerification) {
    $boundaryArgs += "-SkipCryptoPatchVerification"
}

Write-Host "Running physical-device Steam login validation on $DeviceSerial"
Write-Host "Physical validation summary: $summaryPath"
& (Join-Path $PSScriptRoot "test-login-boundary.ps1") @boundaryArgs
