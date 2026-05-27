param(
    [string]$AdbPath = "C:\Users\ap010\.w40k-android-toolchain\android-sdk\platform-tools\adb.exe",
    [string]$PackageName = "com.sts2launcher.overhaul.fork.dev",
    [string]$ArtifactsDir = "",
    [int]$WaitSeconds = 10,
    [switch]$ClearAppData,
    [string]$ApkPath = "",
    [string]$DeviceSerial = "",
    [int]$WaitForDeviceSeconds = 0
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
if (-not $ArtifactsDir) {
    $ArtifactsDir = Join-Path $root "artifacts\android"
}

if (-not (Test-Path -LiteralPath $AdbPath)) {
    throw "ADB not found: $AdbPath"
}

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

function Get-TargetDevice() {
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

function Find-TargetDevice() {
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

function Resolve-ApkForAbi([string]$AbiList) {
    if ($ApkPath) {
        if (-not (Test-Path -LiteralPath $ApkPath)) {
            throw "APK not found: $ApkPath"
        }
        return (Resolve-Path $ApkPath).Path
    }

    $universalApk = Get-ChildItem -LiteralPath $ArtifactsDir -Filter "*universal*.apk" |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if ($universalApk) {
        return $universalApk.FullName
    }

    if ($AbiList -match "x86_64") {
        $pattern = "*x86_64.apk"
    } elseif ($AbiList -match "arm64-v8a") {
        $pattern = "*arm64-v8a.apk"
    } else {
        throw "Unsupported device ABI list: $AbiList"
    }

    $apk = Get-ChildItem -LiteralPath $ArtifactsDir -Filter $pattern |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if (-not $apk) {
        throw "No APK matching $pattern found in $ArtifactsDir"
    }

    return $apk.FullName
}

function Test-ApkChecksum([string]$Path) {
    $checksumPath = "$Path.sha256"
    if (-not (Test-Path -LiteralPath $checksumPath)) {
        Write-Host "Checksum sidecar not found: $checksumPath"
        return
    }

    $expected = ((Get-Content -LiteralPath $checksumPath -TotalCount 1) -split "\s+")[0]
    if (-not $expected) {
        throw "Checksum sidecar is empty or invalid: $checksumPath"
    }

    $actual = (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash.ToLowerInvariant()
    if ($actual -ne $expected.ToLowerInvariant()) {
        throw "APK checksum mismatch for $Path. Expected $expected, got $actual"
    }

    Write-Host "Checksum OK: $checksumPath"
}

$device = Get-TargetDevice
$DeviceSerial = $device
$abiList = (& $AdbPath "-s" $DeviceSerial shell getprop ro.product.cpu.abilist).Trim()
$apk = Resolve-ApkForAbi $abiList
$component = "$PackageName/com.game.sts2launcher.LauncherActivity"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$fullLogPath = Join-Path $ArtifactsDir "logcat-smoke-$timestamp-full.txt"
$filteredLogPath = Join-Path $ArtifactsDir "logcat-smoke-$timestamp-filtered.txt"
$summaryPath = Join-Path $ArtifactsDir "logcat-smoke-$timestamp-summary.txt"

Write-Host "Device: $device"
Write-Host "ABI list: $abiList"
Write-Host "APK: $apk"
Test-ApkChecksum $apk

Invoke-Adb "install" "-r" $apk
if ($ClearAppData) {
    Invoke-Adb "shell" "pm" "clear" $PackageName
}

Invoke-Adb "logcat" "-c"
Invoke-Adb "shell" "am" "force-stop" $PackageName
Invoke-Adb "shell" "am" "start" "-n" $component

Start-Sleep -Seconds $WaitSeconds

$patterns = "STS2Mobile|Routing to native x86 fallback|Showing native x86 fallback|InitEngine|\.NET:|\.NET assemblies not found|Unable to find the \.NET assemblies directory|api_assemblies_dir|Missing required cache file|Assembly setup failed|FORTIFY|FATAL|crash|AndroidRuntime|Exception"
$fullLog = @(& $AdbPath "-s" $DeviceSerial logcat -d -v time)
$fullLog | Set-Content -LiteralPath $fullLogPath -Encoding UTF8

$filteredLog = @($fullLog |
    Select-String -Pattern $patterns |
    ForEach-Object { $_.Line })
$filteredLog | Set-Content -LiteralPath $filteredLogPath -Encoding UTF8

Write-Host "Full logcat: $fullLogPath"
Write-Host "Filtered logcat: $filteredLogPath"

$log = $filteredLog
if ($log -match "\.NET assemblies not found|Unable to find the \.NET assemblies directory") {
    $result = ".NET assembly directory failure observed; inspect filtered and full logcat"
} elseif ($log -match "Assembly setup failed|Missing required cache file") {
    $result = "Java assembly cache setup failure observed; inspect filtered and full logcat"
} elseif ($log -match "Routing to native x86 fallback|Showing native x86 fallback") {
    $result = "native x86 fallback route observed"
} elseif ($log -match "FORTIFY|FATAL|AndroidRuntime|crash") {
    $result = "crash/error markers observed; inspect filtered and full logcat"
} elseif ($log -match "STS2Mobile|InitEngine|\.NET:") {
    $result = "launcher/game runtime emitted logs without filtered crash markers"
} else {
    $result = "no relevant filtered logs observed; increase -WaitSeconds or inspect full logcat"
}

Write-Host "Result: $result"

@(
    "Timestamp: $timestamp"
    "Device: $device"
    "ABI list: $abiList"
    "APK: $apk"
    "Full logcat: $fullLogPath"
    "Filtered logcat: $filteredLogPath"
    "Result: $result"
) | Set-Content -LiteralPath $summaryPath -Encoding UTF8

Write-Host "Summary: $summaryPath"
