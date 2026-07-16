param(
    [string]$AdbPath = "$(Join-Path $env:USERPROFILE '.w40k-android-toolchain\android-sdk\platform-tools\adb.exe')",
    [string]$DeviceSerial = "",
    [string]$ApkPath = "",
    [string]$PackageName = "com.sts2launcher.overhaul.fork.local",
    [int]$StartupWaitSeconds = 12,
    [int]$RotationWaitSeconds = 4,
    [int]$WaitForDeviceSeconds = 0
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "android-adb-utils.ps1")

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
if (-not $ApkPath) {
    $ApkPath = Join-Path $root "artifacts\android\StS2Launcher-v0.2.399-powervr-mod-runtime-hashfix-activation-evidence-local-arm64-v8a.apk"
}
if (-not (Test-Path -LiteralPath $ApkPath -PathType Leaf)) {
    throw "Launcher UI APK not found: $ApkPath"
}
$ApkPath = (Resolve-Path -LiteralPath $ApkPath).Path

Assert-AndroidAdbPath -AdbPath $AdbPath
$DeviceSerial = Resolve-AndroidTargetDevice `
    -AdbPath $AdbPath `
    -DeviceSerial $DeviceSerial `
    -WaitForDeviceSeconds $WaitForDeviceSeconds

function Invoke-Adb([string[]]$Arguments) {
    Invoke-AndroidAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments $Arguments
}

function Invoke-AdbCapture([string[]]$Arguments) {
    return Invoke-AndroidAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments $Arguments
}

function Assert-DeviceUnlocked {
    $policyDump = (Invoke-AdbCapture @("shell", "dumpsys", "window", "policy")) -join [Environment]::NewLine
    if ($policyDump -match '\bmIsShowing=true\b') {
        throw "The Android device is locked. Unlock it and leave the screen on before capturing launcher UI evidence."
    }
}

function Assert-LauncherOwnsFocus {
    $windowDump = (Invoke-AdbCapture @("shell", "dumpsys", "window", "displays")) -join [Environment]::NewLine
    if ($windowDump -notmatch "mCurrentFocus=.*$([regex]::Escape($PackageName))") {
        throw "The launcher does not own display focus. Dismiss the keyguard or system overlay before capturing UI evidence."
    }
}

function Capture-Screenshot([string]$LocalPath, [string]$Label, [string]$PhysicalDisplayId) {
    $devicePath = "/sdcard/sts2-launcher-ui-$Label.png"
    try {
        Invoke-Adb @("shell", "screencap", "-d", $PhysicalDisplayId, "-p", $devicePath)
        Invoke-Adb @("pull", $devicePath, $LocalPath)
        if (-not (Test-Path -LiteralPath $LocalPath -PathType Leaf) -or
            (Get-Item -LiteralPath $LocalPath).Length -eq 0) {
            throw "Android screenshot was not captured: $LocalPath"
        }
    } finally {
        try {
            Invoke-Adb @("shell", "rm", "-f", $devicePath)
        } catch {
        }
    }
}

Invoke-Adb @("shell", "input", "keyevent", "KEYCODE_WAKEUP")
Start-Sleep -Seconds 1
Invoke-Adb @("shell", "wm", "dismiss-keyguard")
Start-Sleep -Seconds 1
Assert-DeviceUnlocked

$abiList = ((Invoke-AdbCapture @("shell", "getprop", "ro.product.cpu.abilist")) -join "").Trim()
if ($abiList -notmatch "arm64-v8a") {
    throw "Physical launcher UI validation requires ARM64; device reports: $abiList"
}

$checksumPath = "$ApkPath.sha256"
if (-not (Test-Path -LiteralPath $checksumPath -PathType Leaf)) {
    throw "APK checksum sidecar not found: $checksumPath"
}
$expectedHash = ((Get-Content -LiteralPath $checksumPath -TotalCount 1) -split "\s+")[0].ToLowerInvariant()
$actualHash = (Get-FileHash -LiteralPath $ApkPath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualHash -ne $expectedHash) {
    throw "APK checksum mismatch. Expected $expectedHash, got $actualHash"
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outputDir = Join-Path $root "artifacts\android\launcher-ui-device-$timestamp"
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null
$component = Get-AndroidLauncherComponent -PackageName $PackageName
$originalUserRotation = ((Invoke-AdbCapture @("shell", "wm", "user-rotation", "-d", "0")) -join "").Trim()
$originalFixedRotation = ((Invoke-AdbCapture @("shell", "wm", "fixed-to-user-rotation", "-d", "0")) -join "").Trim()
$displayDump = (Invoke-AdbCapture @("shell", "dumpsys", "display")) -join [Environment]::NewLine
$activeDisplayMatch = [regex]::Match(
    $displayDump,
    'DisplayDeviceInfo\{[^\r\n]*uniqueId="local:(\d+)"[^\r\n]*state ON'
)
if (-not $activeDisplayMatch.Success) {
    throw "Could not resolve the active physical display for screenshot capture."
}
$physicalDisplayId = $activeDisplayMatch.Groups[1].Value

Write-Host "Device: $DeviceSerial"
Write-Host "ABI list: $abiList"
Write-Host "APK: $ApkPath"
Write-Host "SHA256: $actualHash"
Write-Host "Evidence: $outputDir"
Write-Host "Active physical display: $physicalDisplayId"

try {
    Invoke-Adb @("install", "-r", $ApkPath)
    Invoke-Adb @("logcat", "-c")
    Invoke-Adb @("shell", "am", "force-stop", $PackageName)
    Invoke-Adb @("shell", "am", "start", "-n", $component)
    Start-Sleep -Seconds $StartupWaitSeconds
    Assert-LauncherOwnsFocus

    Invoke-Adb @("shell", "wm", "fixed-to-user-rotation", "-d", "0", "enabled")
    Invoke-Adb @("shell", "wm", "user-rotation", "-d", "0", "lock", "0")
    Start-Sleep -Seconds $RotationWaitSeconds
    $rotationZeroPath = Join-Path $outputDir "launcher-rotation-0.png"
    Capture-Screenshot -LocalPath $rotationZeroPath -Label "rotation-0" -PhysicalDisplayId $physicalDisplayId
    ((Invoke-AdbCapture @("shell", "dumpsys", "window", "displays")) -join [Environment]::NewLine) |
        Set-Content -LiteralPath (Join-Path $outputDir "window-rotation-0.txt") -Encoding UTF8

    Invoke-Adb @("shell", "wm", "user-rotation", "-d", "0", "lock", "1")
    Start-Sleep -Seconds $RotationWaitSeconds
    $rotationOnePath = Join-Path $outputDir "launcher-rotation-1.png"
    Capture-Screenshot -LocalPath $rotationOnePath -Label "rotation-1" -PhysicalDisplayId $physicalDisplayId
    ((Invoke-AdbCapture @("shell", "dumpsys", "window", "displays")) -join [Environment]::NewLine) |
        Set-Content -LiteralPath (Join-Path $outputDir "window-rotation-1.txt") -Encoding UTF8

    Add-Type -AssemblyName System.Drawing
    $rotationZeroImage = [Drawing.Image]::FromFile($rotationZeroPath)
    $rotationOneImage = [Drawing.Image]::FromFile($rotationOnePath)
    try {
        $rotationZeroSize = "$($rotationZeroImage.Width)x$($rotationZeroImage.Height)"
        $rotationOneSize = "$($rotationOneImage.Width)x$($rotationOneImage.Height)"
        if (($rotationZeroImage.Width -gt $rotationZeroImage.Height) -eq
            ($rotationOneImage.Width -gt $rotationOneImage.Height)) {
            throw "Window-manager rotations did not produce distinct portrait and landscape viewports: $rotationZeroSize, $rotationOneSize"
        }
    } finally {
        $rotationZeroImage.Dispose()
        $rotationOneImage.Dispose()
    }

    if ($rotationZeroSize -match '^(\d+)x(\d+)$' -and [int]$Matches[1] -lt [int]$Matches[2]) {
        $portraitFile = "launcher-rotation-0.png"
        $landscapeFile = "launcher-rotation-1.png"
    } else {
        $portraitFile = "launcher-rotation-1.png"
        $landscapeFile = "launcher-rotation-0.png"
    }

    $logcat = (Invoke-AdbCapture @("logcat", "-d", "-v", "threadtime"))
    $focusedPattern = "STS2Mobile|LauncherUI|launcher ui|AndroidRuntime|FATAL EXCEPTION|Fatal signal|SIGSEGV|SIGABRT|\bANR\b|has died|lowmemorykiller|lmkd"
    $focused = @($logcat | Where-Object { $_ -match $focusedPattern })
    $focused | Set-Content -LiteralPath (Join-Path $outputDir "logcat-focused.txt") -Encoding UTF8

    $fatalPattern = "AndroidRuntime.*FATAL|FATAL EXCEPTION|Fatal signal|SIGSEGV|SIGABRT|ANR in $([regex]::Escape($PackageName))|Process $([regex]::Escape($PackageName)).*has died"
    $fatal = @($focused | Where-Object { $_ -match $fatalPattern })
    $packageDump = (Invoke-AdbCapture @("shell", "dumpsys", "package", $PackageName))
    $versionLine = @($packageDump | Where-Object { $_ -match "versionName=" } | Select-Object -First 1)

    @(
        "Timestamp: $timestamp"
        "Device: $DeviceSerial"
        "ABI list: $abiList"
        "APK: $ApkPath"
        "SHA256: $actualHash"
        "Package: $PackageName"
        "Installed: $($versionLine -join '')"
        "Active physical display: $physicalDisplayId"
        "Rotation 0 viewport: $rotationZeroSize"
        "Rotation 1 viewport: $rotationOneSize"
        "Portrait screenshot: $portraitFile"
        "Landscape screenshot: $landscapeFile"
        "Focused logcat: logcat-focused.txt"
        "Fatal markers: $($fatal.Count)"
        "Steam Cloud Push: not run"
    ) | Set-Content -LiteralPath (Join-Path $outputDir "summary.txt") -Encoding UTF8

    if ($fatal.Count -gt 0) {
        throw "Fatal Android markers found; inspect $outputDir\logcat-focused.txt"
    }
} finally {
    if ($originalUserRotation -eq "free") {
        try {
            Invoke-Adb @("shell", "wm", "user-rotation", "-d", "0", "free")
        } catch {
            Write-Warning "Could not restore user rotation after validation: $($_.Exception.Message)"
        }
    } elseif ($originalUserRotation -match "lock\s+([0-3])") {
        try {
            Invoke-Adb @("shell", "wm", "user-rotation", "-d", "0", "lock", $Matches[1])
        } catch {
            Write-Warning "Could not restore user rotation after validation: $($_.Exception.Message)"
        }
    }
    if ($originalFixedRotation -match "^(default|enabled|disabled|enabled_if_no_auto_rotation)$") {
        try {
            Invoke-Adb @("shell", "wm", "fixed-to-user-rotation", "-d", "0", $originalFixedRotation)
        } catch {
            Write-Warning "Could not restore fixed-to-user-rotation after validation: $($_.Exception.Message)"
        }
    }
}

Write-Host "Launcher UI device validation captured: $outputDir"
