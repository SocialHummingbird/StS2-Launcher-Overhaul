param(
    [string]$PackageName = "com.sts2launcher.overhaul.fork.dev",
    [string]$AdbPath = "C:\Users\ap010\.w40k-android-toolchain\android-sdk\platform-tools\adb.exe",
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

if (-not (Test-Path -LiteralPath $AdbPath)) {
    throw "adb not found: $AdbPath"
}

if (-not (Test-Path -LiteralPath $CredentialsPath)) {
    throw "Credentials file not found: $CredentialsPath"
}

if (-not $ApkPath) {
    $latestApk = Get-ChildItem -LiteralPath "android\build\outputs\apk\mono\release" -Filter "StS2Launcher-v*.apk" |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if (-not $latestApk) {
        throw "No APK found in android\build\outputs\apk\mono\release"
    }

    $ApkPath = $latestApk.FullName
}

if (-not (Test-Path -LiteralPath $ApkPath)) {
    throw "APK not found: $ApkPath"
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
& $AdbPath uninstall $PackageName | Out-Null
& $AdbPath install -r $ApkPath | Out-Null

& $AdbPath logcat -c

.\scripts\login-emulator.ps1 `
    -CredentialsPath $CredentialsPath `
    -PackageName $PackageName `
    -AdbPath $AdbPath `
    -GuardCode $GuardCode `
    -PromptForGuardCode:$PromptForGuardCode `
    -UseLocalCredentialFile `
    -Launch

if ($WaitForPostGuardResult) {
    Write-Host "Waiting up to $PostGuardResultTimeoutSeconds seconds for auth/ownership success or a crash signature. If Steam Guard is required, use scripts\submit-steam-guard-and-capture.ps1 or enter the code in the emulator."
    Wait-SteamLoginPostGuardResult -AdbPath $AdbPath -TimeoutSeconds $PostGuardResultTimeoutSeconds -PollSeconds $PostGuardPollSeconds
} elseif ($WaitForManualGuardSubmit) {
    Read-Host "Enter the Steam Guard code directly in the emulator, submit it, then press Enter here to capture post-2FA evidence"
    Start-Sleep -Seconds $PostLoginWaitSeconds
} elseif ($WaitForManualGuardCode) {
    Write-Host "Enter the Steam Guard code directly in the emulator. Waiting $ManualGuardWaitSeconds seconds before capturing post-2FA evidence..."
    Start-Sleep -Seconds $ManualGuardWaitSeconds
} else {
    Write-Host "Waiting up to $LoginResultTimeoutSeconds seconds for auth success, auth failure, Steam Guard request, or a crash signature."
    $loginResult = Wait-SteamLoginResult -AdbPath $AdbPath -TimeoutSeconds $LoginResultTimeoutSeconds -PollSeconds $LoginResultPollSeconds
    Write-Host "Login result wait completed: $loginResult"
}

& $AdbPath logcat -d -v time > $OutputLogcatPath
& $AdbPath shell screencap -p /sdcard/sts2-login-boundary.png | Out-Null
& $AdbPath pull /sdcard/sts2-login-boundary.png $OutputScreenshotPath | Out-Null

.\scripts\check-login-crash-log.ps1 -LogcatPath $OutputLogcatPath -RequirePostSteamGuard:$requirePostSteamGuard

Write-Host "Captured logcat: $OutputLogcatPath"
Write-Host "Captured screenshot: $OutputScreenshotPath"
