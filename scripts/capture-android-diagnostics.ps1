param(
    [string]$AdbPath = "$(Join-Path $env:USERPROFILE '.w40k-android-toolchain\android-sdk\platform-tools\adb.exe')",
    [string]$PackageName = "",
    [string]$ArtifactsDir = "",
    [string]$DeviceSerial = "",
    [int]$WaitForDeviceSeconds = 0,
    [int]$WaitSeconds = 8,
    [switch]$Launch,
    [switch]$ClearLogcat
)

$ErrorActionPreference = "Stop"
if (Get-Variable -Name PSNativeCommandUseErrorActionPreference -Scope Global -ErrorAction SilentlyContinue) {
    $Global:PSNativeCommandUseErrorActionPreference = $false
}

. (Join-Path $PSScriptRoot "android-adb-utils.ps1")

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
if (-not $ArtifactsDir) {
    $ArtifactsDir = Join-Path $root "artifacts\android"
}

Assert-AndroidAdbPath -AdbPath $AdbPath

$device = Resolve-AndroidTargetDevice -AdbPath $AdbPath -DeviceSerial $DeviceSerial -WaitForDeviceSeconds $WaitForDeviceSeconds
$DeviceSerial = $device
$PackageName = Resolve-AndroidInstalledLauncherPackageName -AdbPath $AdbPath -DeviceSerial $DeviceSerial -PackageName $PackageName
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outDir = Join-Path $ArtifactsDir "phone-diagnostics-$timestamp"
New-Item -ItemType Directory -Force $outDir | Out-Null

if ($ClearLogcat) {
    Invoke-AndroidAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("logcat", "-c")
}

if ($Launch) {
    $component = Get-AndroidLauncherComponent -PackageName $PackageName
    Invoke-AndroidAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "am", "force-stop", $PackageName)
    Invoke-AndroidAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "am", "start", "-n", $component)
    Start-Sleep -Seconds $WaitSeconds
}

$patterns = "Routing to native x86 fallback|Showing native x86 fallback|Android startup freshness|Assembly cache diagnostics|Assembly cache required file|expectedSource|expectedBytes|\.NET:|\.NET assemblies not found|Unable to find the \.NET assemblies directory|api_assemblies_dir|Missing required cache file|Assembly setup failed|\[Launcher\]|\[Cloud\]|AndroidRuntime|FATAL EXCEPTION|FORTIFY|F/libc|crash"

Invoke-AndroidAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("devices", "-l") | Set-Content -LiteralPath (Join-Path $outDir "adb-devices.txt") -Encoding UTF8
Invoke-AndroidAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "getprop", "ro.product.cpu.abilist") | Set-Content -LiteralPath (Join-Path $outDir "abi-list.txt") -Encoding UTF8
Invoke-AndroidAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "getprop", "ro.product.manufacturer") | Set-Content -LiteralPath (Join-Path $outDir "manufacturer.txt") -Encoding UTF8
Invoke-AndroidAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "getprop", "ro.product.model") | Set-Content -LiteralPath (Join-Path $outDir "model.txt") -Encoding UTF8
Invoke-AndroidAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "getprop", "ro.build.version.sdk") | Set-Content -LiteralPath (Join-Path $outDir "android-sdk.txt") -Encoding UTF8
Invoke-AndroidAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "getprop", "ro.build.fingerprint") | Set-Content -LiteralPath (Join-Path $outDir "build-fingerprint.txt") -Encoding UTF8
Invoke-AndroidAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "pm", "path", $PackageName) | Set-Content -LiteralPath (Join-Path $outDir "pm-path.txt") -Encoding UTF8
Invoke-AndroidAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "dumpsys", "package", $PackageName) | Set-Content -LiteralPath (Join-Path $outDir "dumpsys-package.txt") -Encoding UTF8

$runAsCommand = "echo RUN_AS_OK; id; pwd; ls -la files 2>&1; ls -la files/game 2>&1; ls -la files/.godot 2>&1; ls -la files/.godot/mono 2>&1; ls -la files/.godot/mono/publish 2>&1; ls -la files/.godot/mono/publish/arm64 2>&1; ls -la files/.godot/mono/publish/x86_64 2>&1; du -a files/.godot/mono/publish 2>&1"
Invoke-AndroidAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "run-as", $PackageName, "sh", "-c", $runAsCommand) | Set-Content -LiteralPath (Join-Path $outDir "run-as-files.txt") -Encoding UTF8

$fullLog = Invoke-AndroidAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("logcat", "-d", "-v", "time")
$fullLogPath = Join-Path $outDir "logcat-full.txt"
$filteredLogPath = Join-Path $outDir "logcat-filtered.txt"
$fullLog | Set-Content -LiteralPath $fullLogPath -Encoding UTF8

$filteredLog = @($fullLog |
    Select-String -Pattern $patterns |
    ForEach-Object { $_.Line })
$filteredLog | Set-Content -LiteralPath $filteredLogPath -Encoding UTF8

if ($filteredLog -match "\.NET assemblies not found|Unable to find the \.NET assemblies directory") {
    $result = ".NET assembly directory failure observed"
} elseif ($filteredLog -match "Assembly setup failed|Missing required cache file") {
    $result = "Java assembly cache setup failure observed"
} elseif ($filteredLog -match "Routing to native x86 fallback|Showing native x86 fallback") {
    $result = "native x86 fallback route observed"
} elseif ($filteredLog -match "Assembly cache diagnostics|Assembly cache required file") {
    $result = "assembly diagnostics captured"
} elseif ($filteredLog -match "AndroidRuntime|FATAL EXCEPTION|FORTIFY|F/libc|crash") {
    $result = "crash/error markers observed"
} elseif ($filteredLog -match "STS2Mobile|\.NET:") {
    $result = "launcher/runtime logs captured without filtered crash markers"
} else {
    $result = "no relevant filtered logs observed"
}

@(
    "Timestamp: $timestamp"
    "Device: $device"
    "Package: $PackageName"
    "Launched: $Launch"
    "Cleared logcat: $ClearLogcat"
    "Output directory: $outDir"
    "Full logcat: $fullLogPath"
    "Filtered logcat: $filteredLogPath"
    "Result: $result"
) | Set-Content -LiteralPath (Join-Path $outDir "summary.txt") -Encoding UTF8

Write-Host "Android diagnostics captured: $outDir"
Write-Host "Result: $result"
