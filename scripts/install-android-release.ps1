param(
    [string]$Repo = "SocialHummingbird/StS2-Launcher-Overhaul",
    [string]$ReleaseTag = "v0.2.88-apk-native-verify",
    [string]$AssetName = "StS2Launcher-v0.2.88-universal-phone.apk",
    [string]$AdbPath = "C:\Users\ap010\.w40k-android-toolchain\android-sdk\platform-tools\adb.exe",
    [string]$PackageName = "com.sts2launcher.overhaul.fork.dev",
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

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
if (-not $ArtifactsDir) {
    $ArtifactsDir = Join-Path $root "artifacts\android"
}

if (-not (Test-Path -LiteralPath $AdbPath)) {
    throw "ADB not found: $AdbPath"
}

New-Item -ItemType Directory -Force $ArtifactsDir | Out-Null

function Invoke-Adb {
    param(
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$Arguments
    )

    if ($DeviceSerial) {
        & $AdbPath "-s" $DeviceSerial @Arguments
    } else {
        & $AdbPath @Arguments
    }
    if ($LASTEXITCODE -ne 0) {
        throw "adb $($Arguments -join ' ') failed"
    }
}

function Find-TargetDevice {
    if ($DeviceSerial) {
        $matchingLine = @(& $AdbPath devices | Select-Object -Skip 1 | Where-Object { $_ -match "^$([regex]::Escape($DeviceSerial))\s+device$" })
        if ($matchingLine.Count -eq 0) {
            return $null
        }
        return $DeviceSerial
    }

    $deviceLines = @(& $AdbPath devices | Select-Object -Skip 1 | Where-Object { $_ -match "\tdevice$" })
    if ($deviceLines.Count -eq 0) {
        return $null
    }
    if ($deviceLines.Count -gt 1) {
        $serials = $deviceLines | ForEach-Object { ($_ -split "\s+")[0] }
        throw "Multiple Android devices/emulators attached. Pass -DeviceSerial with one of: $($serials -join ', ')"
    }
    return ($deviceLines[0] -split "\s+")[0]
}

function Get-TargetDevice {
    $deadline = (Get-Date).AddSeconds($WaitForDeviceSeconds)

    while ($true) {
        $device = Find-TargetDevice
        if ($device) {
            return $device
        }

        if ((Get-Date) -ge $deadline) {
            if ($DeviceSerial) {
                throw "Requested Android device/emulator is not attached or not in 'device' state: $DeviceSerial"
            }
            throw "No attached Android device/emulator."
        }

        Start-Sleep -Seconds 1
    }
}

function Get-ReleaseAssetUrl([string]$Repo, [string]$ReleaseTag, [string]$AssetName) {
    $releaseJson = gh release view $ReleaseTag --repo $Repo --json assets
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to read GitHub release: $Repo $ReleaseTag"
    }

    $release = $releaseJson | ConvertFrom-Json
    $asset = @($release.assets | Where-Object { $_.name -eq $AssetName } | Select-Object -First 1)
    if (-not $asset) {
        $names = @($release.assets | ForEach-Object { $_.name })
        throw "Release asset not found: $AssetName. Available assets: $($names -join ', ')"
    }

    return $asset.url
}

function Get-ReleaseAssetDigest([string]$Repo, [string]$ReleaseTag, [string]$AssetName) {
    $releaseJson = gh release view $ReleaseTag --repo $Repo --json assets
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to read GitHub release: $Repo $ReleaseTag"
    }

    $release = $releaseJson | ConvertFrom-Json
    $asset = @($release.assets | Where-Object { $_.name -eq $AssetName } | Select-Object -First 1)
    if (-not $asset) {
        return ""
    }

    return $asset.digest
}

$device = Get-TargetDevice
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

$digest = Get-ReleaseAssetDigest -Repo $Repo -ReleaseTag $ReleaseTag -AssetName $AssetName
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

if ($UninstallFirst) {
    Write-Host "Uninstalling existing $PackageName before install..."
    if ($DeviceSerial) {
        & $AdbPath "-s" $DeviceSerial "uninstall" $PackageName | Out-Host
    } else {
        & $AdbPath "uninstall" $PackageName | Out-Host
    }
}

Write-Host "Installing $apkPath on $device..."
Invoke-Adb "install" "-r" $apkPath

if ($ClearAppData) {
    Write-Host "Clearing app data for $PackageName..."
    Invoke-Adb "shell" "pm" "clear" $PackageName
}

if ($Launch) {
    $component = "$PackageName/com.game.sts2launcher.LauncherActivity"
    Write-Host "Launching $component..."
    Invoke-Adb "logcat" "-c"
    Invoke-Adb "shell" "am" "force-stop" $PackageName
    Invoke-Adb "shell" "am" "start" "-n" $component
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
