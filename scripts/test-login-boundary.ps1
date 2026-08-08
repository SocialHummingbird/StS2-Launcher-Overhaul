param(
    [string]$PackageName = "",
    [string]$AdbPath = "$(Join-Path $env:USERPROFILE '.w40k-android-toolchain\android-sdk\platform-tools\adb.exe')",
    [string]$DeviceSerial = "",
    [string]$CredentialsPath = "tmp\steam-login.local.json",
    [string]$OutputLogcatPath = "tmp\login-boundary-logcat.txt",
    [string]$OutputScreenshotPath = "tmp\login-boundary.png",
    [string]$ApkPath = "",
    [string]$GuardCode = "",
    [switch]$PromptForGuardCode,
    [switch]$WaitForManualGuardCode,
    [switch]$WaitForManualGuardSubmit,
    [switch]$WaitForPostGuardResult,
    [int]$ManualGuardWaitSeconds = 90,
    [int]$PostGuardResultTimeoutSeconds = 180,
    [int]$PostGuardPollSeconds = 3,
    [int]$PostLoginWaitSeconds = 35,
    [int]$LoginResultTimeoutSeconds = 180,
    [int]$LoginResultPollSeconds = 3,
    [switch]$SkipCryptoPatchVerification
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "steam-login-utils.ps1")
. (Join-Path $PSScriptRoot "android-apk-utils.ps1")

function Invoke-BoundaryAdb {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [switch]$AllowFailure
    )

    if (-not [string]::IsNullOrWhiteSpace($DeviceSerial)) {
        & $AdbPath -s $DeviceSerial @Arguments
    } else {
        & $AdbPath @Arguments
    }

    if (-not $AllowFailure -and $LASTEXITCODE -ne 0) {
        throw "adb $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path -LiteralPath $AdbPath)) {
    throw "adb not found: $AdbPath"
}

if (-not [string]::IsNullOrWhiteSpace($DeviceSerial)) {
    $env:ANDROID_SERIAL = $DeviceSerial
    Write-Host "Using Android device serial: $DeviceSerial"
}

if (-not (Test-Path -LiteralPath $CredentialsPath)) {
    throw "Credentials file not found: $CredentialsPath"
}

if (-not $ApkPath) {
    $selectedApk = Select-AndroidApk -Directory "android\build\outputs\apk\mono\release" -AdbPath $AdbPath -PackageName $PackageName
    $ApkPath = $selectedApk.Path
}

if (-not (Test-Path -LiteralPath $ApkPath)) {
    throw "APK not found: $ApkPath"
}

if ([string]::IsNullOrWhiteSpace($PackageName)) {
    $PackageName = Get-AndroidApkPackageName -ApkPath $ApkPath -AdbPath $AdbPath
    Write-Host "Resolved APK package: $PackageName"
}

if (-not $SkipCryptoPatchVerification) {
    & (Join-Path $PSScriptRoot "verify-android-apk-crypto-patches.ps1") -ApkPath $ApkPath
    if ($LASTEXITCODE -ne 0) {
        throw "APK Android crypto patch verification failed with exit code $LASTEXITCODE."
    }
}

$creds = Get-Content -LiteralPath $CredentialsPath -Raw | ConvertFrom-Json
$hasStoredSteamGuard = (-not [string]::IsNullOrWhiteSpace([string]$creds.shared_secret)) -or (-not [string]::IsNullOrWhiteSpace([string]$creds.guard_code))
$hasDirectSteamGuard = -not [string]::IsNullOrWhiteSpace($GuardCode)
$requirePostSteamGuard = $hasStoredSteamGuard -or $hasDirectSteamGuard -or $PromptForGuardCode -or $WaitForManualGuardCode -or $WaitForManualGuardSubmit -or $WaitForPostGuardResult

if ($requirePostSteamGuard -and $OutputLogcatPath -eq "tmp\login-boundary-logcat.txt") {
    $OutputLogcatPath = "tmp\login-boundary-post-2fa-logcat.txt"
}

if ($requirePostSteamGuard -and $OutputScreenshotPath -eq "tmp\login-boundary.png") {
    $OutputScreenshotPath = "tmp\login-boundary-post-2fa.png"
}

New-Item -ItemType Directory -Force (Split-Path -Parent $OutputLogcatPath) | Out-Null
New-Item -ItemType Directory -Force (Split-Path -Parent $OutputScreenshotPath) | Out-Null

Write-Host "Installing APK: $ApkPath"
Invoke-BoundaryAdb -Arguments @("uninstall", $PackageName) -AllowFailure | Out-Null
Invoke-BoundaryAdb -Arguments @("install", $ApkPath) | Out-Null

Invoke-BoundaryAdb -Arguments @("logcat", "-c")

.\scripts\login-emulator.ps1 `
    -CredentialsPath $CredentialsPath `
    -PackageName $PackageName `
    -AdbPath $AdbPath `
    -DeviceSerial $DeviceSerial `
    -GuardCode $GuardCode `
    -PromptForGuardCode:$PromptForGuardCode `
    -UseLocalCredentialFile `
    -Launch

$boundaryLoginResult = $null
if ($WaitForPostGuardResult) {
    Write-Host "Waiting up to $PostGuardResultTimeoutSeconds seconds for auth/ownership success, unsupported target, or a crash signature. If Steam Guard is required, use scripts\submit-steam-guard-and-capture.ps1 or enter the code in the emulator."
    $boundaryLoginResult = Wait-SteamLoginPostGuardResult -AdbPath $AdbPath -DeviceSerial $DeviceSerial -TimeoutSeconds $PostGuardResultTimeoutSeconds -PollSeconds $PostGuardPollSeconds
} elseif ($WaitForManualGuardSubmit) {
    Read-Host "Enter the Steam Guard code directly in the emulator, submit it, then press Enter here to capture post-2FA evidence"
    Start-Sleep -Seconds $PostLoginWaitSeconds
} elseif ($WaitForManualGuardCode) {
    Write-Host "Enter the Steam Guard code directly in the emulator. Waiting $ManualGuardWaitSeconds seconds before capturing post-2FA evidence..."
    Start-Sleep -Seconds $ManualGuardWaitSeconds
} else {
    Write-Host "Waiting up to $LoginResultTimeoutSeconds seconds for auth success, auth failure, Steam Guard request, unsupported target, or a crash signature."
    $boundaryLoginResult = Wait-SteamLoginResult -AdbPath $AdbPath -DeviceSerial $DeviceSerial -TimeoutSeconds $LoginResultTimeoutSeconds -PollSeconds $LoginResultPollSeconds
    Write-Host "Login result wait completed: $boundaryLoginResult"
}

Invoke-BoundaryAdb -Arguments @("logcat", "-d", "-v", "time") > $OutputLogcatPath
Invoke-BoundaryAdb -Arguments @("shell", "screencap", "-p", "/sdcard/sts2-login-boundary.png") | Out-Null
Invoke-BoundaryAdb -Arguments @("pull", "/sdcard/sts2-login-boundary.png", $OutputScreenshotPath) | Out-Null

if ($boundaryLoginResult -eq "unsupported-target") {
    Write-Error "Steam login validation target unsupported. Captured logcat: $OutputLogcatPath. Captured screenshot: $OutputScreenshotPath. Use a supported ARM64 Android device/build for authoritative login validation."
    exit 1
}

.\scripts\check-login-crash-log.ps1 -LogcatPath $OutputLogcatPath -RequirePostSteamGuard:$requirePostSteamGuard

Write-Host "Captured logcat: $OutputLogcatPath"
Write-Host "Captured screenshot: $OutputScreenshotPath"
