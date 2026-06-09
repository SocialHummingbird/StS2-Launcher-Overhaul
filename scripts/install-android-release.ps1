param(
    [string]$Repo = "SocialHummingbird/StS2-Launcher-Overhaul",
    [string]$ReleaseTag = "v0.2.177-login-a8729d6",
    [string]$AssetName = "StS2Launcher-v0.2.177-login-a8729d6-arm64-v8a.apk",
    [string]$AdbPath = "C:\Users\ap010\.w40k-android-toolchain\android-sdk\platform-tools\adb.exe",
    [string]$PackageName = "",
    [string]$ArtifactsDir = "",
    [string]$DeviceSerial = "",
    [int]$WaitForDeviceSeconds = 0,
    [int]$WaitSeconds = 15,
    [switch]$UninstallFirst,
    [switch]$ClearAppData,
    [switch]$Launch,
    [switch]$CaptureDiagnostics
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "android-apk-utils.ps1")
. (Join-Path $PSScriptRoot "android-adb-utils.ps1")

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
if (-not $ArtifactsDir) {
    $ArtifactsDir = Join-Path $root "artifacts\android"
}

Assert-AndroidAdbPath -AdbPath $AdbPath
New-Item -ItemType Directory -Force $ArtifactsDir | Out-Null

$device = Resolve-AndroidTargetDevice -AdbPath $AdbPath -DeviceSerial $DeviceSerial -WaitForDeviceSeconds $WaitForDeviceSeconds
$DeviceSerial = $device
$downloadDir = Join-Path $ArtifactsDir "github-release-$ReleaseTag"
$apkPath = Join-Path $downloadDir $AssetName
New-Item -ItemType Directory -Force $downloadDir | Out-Null

if (-not (Test-Path -LiteralPath $apkPath)) {
    Write-Host "Downloading $ReleaseTag/$AssetName from $Repo..."
    gh release download $ReleaseTag --repo $Repo --pattern $AssetName --dir $downloadDir --clobber
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to download release asset: $ReleaseTag/$AssetName"
    }
}

if (-not (Test-Path -LiteralPath $apkPath)) {
    throw "Downloaded APK not found: $apkPath"
}

$digest = Get-GitHubReleaseAssetDigest -Repo $Repo -ReleaseTag $ReleaseTag -AssetName $AssetName -AllowMissing
$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $apkPath).Hash.ToLowerInvariant()
if ($digest -and $digest.StartsWith("sha256:")) {
    $expected = $digest.Substring("sha256:".Length).ToLowerInvariant()
    if ($hash -ne $expected) {
        throw "APK checksum mismatch. Expected $expected from release digest, got $hash"
    }
    Write-Host "Checksum OK: $hash"
} else {
    Write-Host "Release digest unavailable; local SHA256: $hash"
}

if ([string]::IsNullOrWhiteSpace($PackageName)) {
    $PackageName = Get-AndroidApkPackageName -ApkPath $apkPath -AdbPath $AdbPath
    Write-Host "Resolved APK package: $PackageName"
}

if ($UninstallFirst) {
    Write-Host "Uninstalling existing $PackageName before install..."
    if ($DeviceSerial) {
        & $AdbPath "-s" $DeviceSerial "uninstall" $PackageName | Out-Host
    } else {
        & $AdbPath "uninstall" $PackageName | Out-Host
    }
}

Write-Host "Installing $apkPath on $device..."
Invoke-AndroidAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("install", "-r", $apkPath)

if ($ClearAppData) {
    Write-Host "Clearing app data for $PackageName..."
    Invoke-AndroidAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "pm", "clear", $PackageName)
}

if ($Launch) {
    $component = Get-AndroidLauncherComponent -PackageName $PackageName
    Write-Host "Launching $component..."
    Invoke-AndroidAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("logcat", "-c")
    Invoke-AndroidAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "am", "force-stop", $PackageName)
    Invoke-AndroidAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "am", "start", "-n", $component)
    Start-Sleep -Seconds $WaitSeconds
}

if ($CaptureDiagnostics) {
    $diagnosticsScript = Join-Path $PSScriptRoot "capture-android-diagnostics.ps1"
    if (-not (Test-Path -LiteralPath $diagnosticsScript)) {
        throw "Diagnostics script not found: $diagnosticsScript"
    }

    $diagnosticsArgs = @{
        AdbPath = $AdbPath
        PackageName = $PackageName
        ArtifactsDir = $ArtifactsDir
        DeviceSerial = $DeviceSerial
        WaitSeconds = $WaitSeconds
    }
    if (-not $Launch) {
        $diagnosticsArgs.Launch = $true
        $diagnosticsArgs.ClearLogcat = $true
    }

    & $diagnosticsScript @diagnosticsArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Diagnostic capture failed"
    }
}

Write-Host "Installed release APK: $ReleaseTag/$AssetName"
