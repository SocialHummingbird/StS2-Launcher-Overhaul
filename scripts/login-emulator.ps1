param(
    [string]$CredentialsPath = "tmp\steam-login.local.json",
    [string]$PackageName = "com.sts2launcher.overhaul.fork.dev",
    [string]$AdbPath = "C:\Users\ap010\.w40k-android-toolchain\android-sdk\platform-tools\adb.exe",
    [string]$GuardCode = "",
    [switch]$PromptForGuardCode,
    [switch]$UseLocalCredentialFile,
    [switch]$Launch,
    [int]$StartupDelaySeconds = 15
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "steam-login-utils.ps1")

if (-not (Test-Path -LiteralPath $CredentialsPath)) {
    throw "Credentials file not found: $CredentialsPath"
}

if (-not (Test-Path -LiteralPath $AdbPath)) {
    throw "adb not found: $AdbPath"
}

$creds = Get-Content -LiteralPath $CredentialsPath -Raw | ConvertFrom-Json
if (-not $creds.username -or -not $creds.password) {
    throw "Credentials file must contain username and password."
}

if ($Launch) {
    & $AdbPath shell monkey -p $PackageName 1 | Out-Null
    Start-Sleep -Seconds $StartupDelaySeconds
}

if ($UseLocalCredentialFile) {
    Write-SteamLoginCredentialFile -AdbPath $AdbPath -PackageName $PackageName -Username $creds.username -Password $creds.password
    Write-Host "Wrote local Steam credential handoff file."
} else {
    & $AdbPath shell input tap 860 500 | Out-Null
    Start-Sleep -Milliseconds 700
    Send-AndroidInputText -AdbPath $AdbPath -Text $creds.username

    Start-Sleep -Milliseconds 700
    & $AdbPath shell input tap 860 605 | Out-Null
    Start-Sleep -Milliseconds 700
    Send-AndroidInputText -AdbPath $AdbPath -Text $creds.password

    Start-Sleep -Milliseconds 700
    & $AdbPath shell input tap 860 720 | Out-Null
}

$guardCode = Resolve-SteamGuardCode -GuardCode $GuardCode -PromptForGuardCode:$PromptForGuardCode -Credentials $creds
if ($guardCode) {
    Write-SteamGuardCodeFile -AdbPath $AdbPath -PackageName $PackageName -Code $guardCode
    Write-Host "Wrote local Steam Guard handoff file."
} else {
    if ($UseLocalCredentialFile) {
        Write-Host "Submitted username/password through local handoff. Pass -GuardCode, use -PromptForGuardCode, or add shared_secret/guard_code to $CredentialsPath to automate 2FA."
    } else {
        Write-Host "Submitted username/password. Pass -GuardCode, use -PromptForGuardCode, or add shared_secret/guard_code to $CredentialsPath to automate 2FA."
    }
}
