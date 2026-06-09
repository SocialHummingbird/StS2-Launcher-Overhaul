param(
    [string]$DeviceSerial = "",
    [string]$AdbPath = "$(Join-Path $env:USERPROFILE '.w40k-android-toolchain\android-sdk\platform-tools\adb.exe')",
    [string]$CredentialsPath = "tmp\steam-login.local.json",
    [string]$ApkPath = "",
    [string]$PackageName = "",
    [string]$GuardCode = "",
    [switch]$PromptForGuardCode,
    [switch]$WaitForManualGuardCode,
    [switch]$WaitForManualGuardSubmit,
    [switch]$WaitForPostGuardResult,
    [int]$LoginResultTimeoutSeconds = 240,
    [int]$PostGuardResultTimeoutSeconds = 240,
    [switch]$SkipCryptoPatchVerification
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "android-apk-utils.ps1")

function Get-ConnectedPhysicalDeviceSerial {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath
    )

    $devices = @(
        & $AdbPath devices |
            Select-Object -Skip 1 |
            ForEach-Object {
                $line = ([string]$_).Trim()
                if ($line -match "^([^\s]+)\s+device(?:\s+.*)?$") {
                    $Matches[1]
                }
            } |
            Where-Object {
                -not [string]::IsNullOrWhiteSpace($_) -and -not $_.StartsWith("emulator-")
            }
    )

    if ($devices.Count -eq 1) {
        return $devices[0]
    }

    if ($devices.Count -gt 1) {
        throw "Multiple physical Android devices are connected: $($devices -join ', '). Pass -DeviceSerial explicitly."
    }

    throw "No physical Android device is connected. Connect a phone/tablet with USB debugging enabled."
}

function Invoke-PhysicalLoginAdbCapture {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
        [Parameter(Mandatory = $true)]
        [string]$DeviceSerial,
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $output = @(& $AdbPath -s $DeviceSerial @Arguments 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "adb $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }

    return $output
}

function Get-PhysicalDeviceAbiList {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
        [Parameter(Mandatory = $true)]
        [string]$DeviceSerial
    )

    $abiList = ((Invoke-PhysicalLoginAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "getprop", "ro.product.cpu.abilist")) -join "").Trim()
    if (-not [string]::IsNullOrWhiteSpace($abiList)) {
        return $abiList
    }

    return ((Invoke-PhysicalLoginAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "getprop", "ro.product.cpu.abi")) -join "").Trim()
}

function Get-PhysicalDeviceProperty {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
        [Parameter(Mandatory = $true)]
        [string]$DeviceSerial,
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    return ((Invoke-PhysicalLoginAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "getprop", $Name)) -join "").Trim()
}

function Assert-PhysicalDeviceSupportsArm64 {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
        [Parameter(Mandatory = $true)]
        [string]$DeviceSerial
    )

    $abiList = Get-PhysicalDeviceAbiList -AdbPath $AdbPath -DeviceSerial $DeviceSerial
    if ($abiList -notmatch "(^|,)arm64-v8a(,|$)") {
        throw "Physical-device validation requires an ARM64 Android target. Device $DeviceSerial reports ABI list: $abiList"
    }

    return $abiList
}

function Get-ApkNativeCodeLine {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ApkPath,
        [Parameter(Mandatory = $true)]
        [string]$AdbPath
    )

    $badging = Invoke-AndroidAaptBadging -ApkPath $ApkPath -AdbPath $AdbPath
    return $badging | Where-Object { ([string]$_).StartsWith("native-code:") } | Select-Object -First 1
}

function Assert-ApkSupportsArm64 {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ApkPath,
        [Parameter(Mandatory = $true)]
        [string]$AdbPath
    )

    $nativeCodeLine = Get-ApkNativeCodeLine -ApkPath $ApkPath -AdbPath $AdbPath
    if (-not $nativeCodeLine) {
        throw "Physical-device validation requires an APK with arm64-v8a native code. $ApkPath did not report native-code badging."
    }

    if ($nativeCodeLine -notmatch "'arm64-v8a'") {
        throw "Physical-device validation requires an ARM64-capable APK. $ApkPath reports $nativeCodeLine"
    }
}

function Test-ApkSupportsArm64 {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ApkPath,
        [Parameter(Mandatory = $true)]
        [string]$AdbPath
    )

    $nativeCodeLine = Get-ApkNativeCodeLine -ApkPath $ApkPath -AdbPath $AdbPath
    if (-not $nativeCodeLine) {
        return $false
    }

    return $nativeCodeLine -match "'arm64-v8a'"
}

function Resolve-PhysicalLoginApkPath {
    param(
        [string]$ApkPath = "",
        [Parameter(Mandatory = $true)]
        [string]$AdbPath
    )

    if (-not [string]::IsNullOrWhiteSpace($ApkPath)) {
        return $ApkPath
    }

    $selectedApk = Select-AndroidApk -Directory "android\build\outputs\apk\mono\release" -AdbPath $AdbPath -TargetAbi "arm64-v8a"
    return $selectedApk.Path
}

if (-not (Test-Path -LiteralPath $AdbPath)) {
    throw "adb not found: $AdbPath"
}

if ([string]::IsNullOrWhiteSpace($DeviceSerial)) {
    $DeviceSerial = Get-ConnectedPhysicalDeviceSerial -AdbPath $AdbPath
}

if ($DeviceSerial.StartsWith("emulator-")) {
    throw "Physical-device validation cannot use emulator serial: $DeviceSerial"
}

$deviceAbiList = Assert-PhysicalDeviceSupportsArm64 -AdbPath $AdbPath -DeviceSerial $DeviceSerial
$deviceModel = Get-PhysicalDeviceProperty -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Name "ro.product.model"
$deviceManufacturer = Get-PhysicalDeviceProperty -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Name "ro.product.manufacturer"
$deviceSdk = Get-PhysicalDeviceProperty -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Name "ro.build.version.sdk"

$ApkPath = Resolve-PhysicalLoginApkPath -ApkPath $ApkPath -AdbPath $AdbPath
if (-not [string]::IsNullOrWhiteSpace($ApkPath)) {
    Assert-ApkSupportsArm64 -ApkPath $ApkPath -AdbPath $AdbPath
    $apkPackageName = Get-AndroidApkPackageName -ApkPath $ApkPath -AdbPath $AdbPath
    if ([string]::IsNullOrWhiteSpace($PackageName)) {
        $PackageName = $apkPackageName
    } elseif ($PackageName -ne $apkPackageName) {
        throw "PackageName mismatch. APK $ApkPath declares $apkPackageName but -PackageName was $PackageName."
    }
}

$safeDeviceSerial = $DeviceSerial -replace '[^A-Za-z0-9_.-]', '_'
$outputPrefix = "artifacts\android\physical-login-$safeDeviceSerial"
$summaryPath = "$outputPrefix-summary.txt"
$logcatPath = "$outputPrefix-logcat.txt"
$screenshotPath = "$outputPrefix.png"

New-Item -ItemType Directory -Force (Split-Path -Parent $summaryPath) | Out-Null
@(
    "Validation target: physical ARM64 Android device",
    "Validation note: headed x86 emulator results are non-authoritative for Steam login",
    "Device serial: $DeviceSerial",
    "Device manufacturer: $deviceManufacturer",
    "Device model: $deviceModel",
    "Device Android SDK: $deviceSdk",
    "Device ABI list: $deviceAbiList",
    "APK path: $ApkPath",
    "Resolved package: $PackageName",
    "Credentials path: $CredentialsPath",
    "Logcat path: $logcatPath",
    "Screenshot path: $screenshotPath"
) | Set-Content -LiteralPath $summaryPath -Encoding UTF8

$boundaryArgs = @{
    DeviceSerial = $DeviceSerial
    AdbPath = $AdbPath
    CredentialsPath = $CredentialsPath
    OutputLogcatPath = $logcatPath
    OutputScreenshotPath = $screenshotPath
    LoginResultTimeoutSeconds = $LoginResultTimeoutSeconds
    PostGuardResultTimeoutSeconds = $PostGuardResultTimeoutSeconds
}

if (-not [string]::IsNullOrWhiteSpace($ApkPath)) {
    $boundaryArgs.ApkPath = $ApkPath
}

if (-not [string]::IsNullOrWhiteSpace($PackageName)) {
    $boundaryArgs.PackageName = $PackageName
}

if (-not [string]::IsNullOrWhiteSpace($GuardCode)) {
    $boundaryArgs.GuardCode = $GuardCode
}

if ($PromptForGuardCode) {
    $boundaryArgs.PromptForGuardCode = $true
}

if ($WaitForManualGuardCode) {
    $boundaryArgs.WaitForManualGuardCode = $true
}

if ($WaitForManualGuardSubmit) {
    $boundaryArgs.WaitForManualGuardSubmit = $true
}

if ($WaitForPostGuardResult) {
    $boundaryArgs.WaitForPostGuardResult = $true
}

if ($SkipCryptoPatchVerification) {
    $boundaryArgs.SkipCryptoPatchVerification = $true
}

Write-Host "Running physical-device Steam login validation on $DeviceSerial"
Write-Host "Physical validation summary: $summaryPath"
& (Join-Path $PSScriptRoot "test-login-boundary.ps1") @boundaryArgs
