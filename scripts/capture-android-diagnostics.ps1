param(
    [string]$AdbPath = "C:\Users\ap010\.w40k-android-toolchain\android-sdk\platform-tools\adb.exe",
    [string]$PackageName = "com.sts2launcher.overhaul.fork.dev",
    [string]$ArtifactsDir = "",
    [string]$DeviceSerial = "",
    [int]$WaitForDeviceSeconds = 0,
    [int]$WaitSeconds = 8,
    [switch]$Launch,
    [switch]$ClearLogcat
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

function Invoke-AdbCapture {
    param(
        [Parameter(ValueFromRemainingArguments = $true)]
        [string[]]$Arguments
    )

    if ($DeviceSerial) {
        return @(& $AdbPath "-s" $DeviceSerial @Arguments 2>&1)
    }

    return @(& $AdbPath @Arguments 2>&1)
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

$device = Get-TargetDevice
$DeviceSerial = $device
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outDir = Join-Path $ArtifactsDir "phone-diagnostics-$timestamp"
New-Item -ItemType Directory -Force $outDir | Out-Null

if ($ClearLogcat) {
    Invoke-Adb "logcat" "-c"
}

if ($Launch) {
    $component = "$PackageName/com.game.sts2launcher.LauncherActivity"
    Invoke-Adb "shell" "am" "force-stop" $PackageName
    Invoke-Adb "shell" "am" "start" "-n" $component
    Start-Sleep -Seconds $WaitSeconds
}

$patterns = "STS2Mobile|Assembly cache diagnostics|Assembly cache required file|\.NET:|\.NET assemblies not found|Unable to find the \.NET assemblies directory|api_assemblies_dir|Missing required cache file|Assembly setup failed|AndroidRuntime|FATAL|FORTIFY|Exception|crash"

Invoke-AdbCapture "devices" "-l" | Set-Content -LiteralPath (Join-Path $outDir "adb-devices.txt") -Encoding UTF8
Invoke-AdbCapture "shell" "getprop" "ro.product.cpu.abilist" | Set-Content -LiteralPath (Join-Path $outDir "abi-list.txt") -Encoding UTF8
Invoke-AdbCapture "shell" "getprop" "ro.product.manufacturer" | Set-Content -LiteralPath (Join-Path $outDir "manufacturer.txt") -Encoding UTF8
Invoke-AdbCapture "shell" "getprop" "ro.product.model" | Set-Content -LiteralPath (Join-Path $outDir "model.txt") -Encoding UTF8
Invoke-AdbCapture "shell" "getprop" "ro.build.version.sdk" | Set-Content -LiteralPath (Join-Path $outDir "android-sdk.txt") -Encoding UTF8
Invoke-AdbCapture "shell" "getprop" "ro.build.fingerprint" | Set-Content -LiteralPath (Join-Path $outDir "build-fingerprint.txt") -Encoding UTF8
Invoke-AdbCapture "shell" "pm" "path" $PackageName | Set-Content -LiteralPath (Join-Path $outDir "pm-path.txt") -Encoding UTF8
Invoke-AdbCapture "shell" "dumpsys" "package" $PackageName | Set-Content -LiteralPath (Join-Path $outDir "dumpsys-package.txt") -Encoding UTF8

$runAsCommand = "echo RUN_AS_OK; id; pwd; ls -la files 2>&1; ls -la files/game 2>&1; ls -la files/.godot 2>&1; ls -la files/.godot/mono 2>&1; ls -la files/.godot/mono/publish 2>&1; ls -la files/.godot/mono/publish/arm64 2>&1; ls -la files/.godot/mono/publish/x86_64 2>&1; du -a files/.godot/mono/publish 2>&1"
Invoke-AdbCapture "shell" "run-as" $PackageName "sh" "-c" $runAsCommand | Set-Content -LiteralPath (Join-Path $outDir "run-as-files.txt") -Encoding UTF8

$fullLog = Invoke-AdbCapture "logcat" "-d" "-v" "time"
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
} elseif ($filteredLog -match "Assembly cache diagnostics|Assembly cache required file") {
    $result = "assembly diagnostics captured"
} elseif ($filteredLog -match "AndroidRuntime|FATAL|FORTIFY|Exception|crash") {
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
