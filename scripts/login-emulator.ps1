param(
    [string]$CredentialsPath = "tmp\steam-login.local.json",
    [string]$PackageName = "",
    [string]$AdbPath = "$(Join-Path $env:USERPROFILE '.w40k-android-toolchain\android-sdk\platform-tools\adb.exe')",
    [string]$DeviceSerial = "",
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

if (-not [string]::IsNullOrWhiteSpace($DeviceSerial)) {
    $env:ANDROID_SERIAL = $DeviceSerial
    Write-Host "Using Android device serial: $DeviceSerial"
}

$PackageName = Resolve-SteamLauncherPackageName -AdbPath $AdbPath -DeviceSerial $DeviceSerial -PackageName $PackageName

$creds = Get-Content -LiteralPath $CredentialsPath -Raw | ConvertFrom-Json
if (-not $creds.username -or -not $creds.password) {
    throw "Credentials file must contain username and password."
}

$guardCode = Resolve-SteamGuardCode -GuardCode $GuardCode -PromptForGuardCode:$PromptForGuardCode -Credentials $creds

if ($UseLocalCredentialFile) {
    if ($Launch) {
        Invoke-SteamLoginAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "am", "force-stop", $PackageName) | Out-Null
    }

    Clear-SteamLoginHandoffFiles -AdbPath $AdbPath -DeviceSerial $DeviceSerial -PackageName $PackageName
    Write-SteamLoginCredentialFile -AdbPath $AdbPath -DeviceSerial $DeviceSerial -PackageName $PackageName -Username $creds.username -Password $creds.password
    Write-Host "Wrote local Steam credential handoff file."

    if ($guardCode) {
        Write-SteamGuardCodeFile -AdbPath $AdbPath -DeviceSerial $DeviceSerial -PackageName $PackageName -Code $guardCode
        Write-Host "Wrote local Steam Guard handoff file."
    }

    if ($Launch) {
        Start-SteamLauncherApp -AdbPath $AdbPath -DeviceSerial $DeviceSerial -PackageName $PackageName
        Start-Sleep -Seconds $StartupDelaySeconds
    }
} else {
    if ($Launch) {
        Start-SteamLauncherApp -AdbPath $AdbPath -DeviceSerial $DeviceSerial -PackageName $PackageName
        Start-Sleep -Seconds $StartupDelaySeconds
    }

    Invoke-SteamLoginAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "input", "tap", "860", "500") | Out-Null
    Start-Sleep -Milliseconds 700
    Send-AndroidInputText -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Text $creds.username

    Start-Sleep -Milliseconds 700
    Invoke-SteamLoginAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "input", "tap", "860", "605") | Out-Null
    Start-Sleep -Milliseconds 700
    Send-AndroidInputText -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Text $creds.password

    Start-Sleep -Milliseconds 700
    Invoke-SteamLoginAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "input", "tap", "860", "720") | Out-Null
}

if ($UseLocalCredentialFile) {
    if ($guardCode) {
        Write-Host "Submitted username/password and Steam Guard through local handoff."
    } else {
        Write-Host "Submitted username/password through local handoff. Pass -GuardCode, use -PromptForGuardCode, or add shared_secret/guard_code to $CredentialsPath to automate 2FA."
    }
} elseif ($guardCode) {
    Write-SteamGuardCodeFile -AdbPath $AdbPath -DeviceSerial $DeviceSerial -PackageName $PackageName -Code $guardCode
    Write-Host "Wrote local Steam Guard handoff file."
} else {
    Write-Host "Submitted username/password. Pass -GuardCode, use -PromptForGuardCode, or add shared_secret/guard_code to $CredentialsPath to automate 2FA."
}
