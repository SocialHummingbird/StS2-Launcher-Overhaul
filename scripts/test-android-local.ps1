param(
    [string]$AdbPath = "C:\Users\ap010\.w40k-android-toolchain\android-sdk\platform-tools\adb.exe",
    [string]$PackageName = "",
    [string]$ArtifactsDir = "",
    [int]$WaitSeconds = 10,
    [switch]$ClearAppData,
    [string]$ApkPath = "",
    [string]$DeviceSerial = "",
    [int]$WaitForDeviceSeconds = 0
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "android-adb-utils.ps1")
. (Join-Path $PSScriptRoot "android-apk-utils.ps1")

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
if (-not $ArtifactsDir) {
    $ArtifactsDir = Join-Path $root "artifacts\android"
}

Assert-AndroidAdbPath -AdbPath $AdbPath

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

$device = Resolve-AndroidTargetDevice -AdbPath $AdbPath -DeviceSerial $DeviceSerial -WaitForDeviceSeconds $WaitForDeviceSeconds
$DeviceSerial = $device
$abiList = (Invoke-AndroidAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "getprop", "ro.product.cpu.abilist") | Out-String).Trim()
$apk = Resolve-ApkForAbi $abiList
if ([string]::IsNullOrWhiteSpace($PackageName)) {
    $PackageName = Get-AndroidApkPackageName -ApkPath $apk -AdbPath $AdbPath
    Write-Host "Resolved APK package: $PackageName"
}
$component = Get-AndroidLauncherComponent -PackageName $PackageName
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$fullLogPath = Join-Path $ArtifactsDir "logcat-smoke-$timestamp-full.txt"
$filteredLogPath = Join-Path $ArtifactsDir "logcat-smoke-$timestamp-filtered.txt"
$summaryPath = Join-Path $ArtifactsDir "logcat-smoke-$timestamp-summary.txt"

Write-Host "Device: $device"
Write-Host "ABI list: $abiList"
Write-Host "APK: $apk"
Test-ApkChecksum $apk

Invoke-AndroidAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("install", "-r", $apk)
if ($ClearAppData) {
    Invoke-AndroidAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "pm", "clear", $PackageName)
}

Invoke-AndroidAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("logcat", "-c")
Invoke-AndroidAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "am", "force-stop", $PackageName)
Invoke-AndroidAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "am", "start", "-n", $component)

Start-Sleep -Seconds $WaitSeconds

$patterns = "STS2Mobile|Routing to native x86 fallback|Showing native x86 fallback|InitEngine|\.NET:|\.NET assemblies not found|Unable to find the \.NET assemblies directory|api_assemblies_dir|Missing required cache file|Assembly setup failed|FORTIFY|FATAL|crash|AndroidRuntime|Exception"
$fullLog = Invoke-AndroidAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("logcat", "-d", "-v", "time")
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
