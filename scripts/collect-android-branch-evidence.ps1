param(
    [string]$PackageName = "com.sts2launcher.overhaul.fork.local",
    [string]$DeviceSerial = "",
    [string]$OutputDir = "",
    [string]$AndroidHome = "$(Join-Path $env:USERPROFILE '.w40k-android-toolchain\android-sdk')",
    [switch]$IncludeScreenshot
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent $PSCommandPath)
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $OutputDir = Join-Path $root "artifacts\android\branch-evidence-$stamp"
}

$adb = Join-Path $AndroidHome "platform-tools\adb.exe"
if (-not (Test-Path -LiteralPath $adb)) {
    throw "adb not found at $adb"
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

function Invoke-Adb {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$AdbArgs)

    if ([string]::IsNullOrWhiteSpace($DeviceSerial)) {
        & $adb @AdbArgs
    } else {
        & $adb -s $DeviceSerial @AdbArgs
    }
}

function Redact-Line {
    param([string]$Line)

    if ($null -eq $Line) {
        return ""
    }

    $redacted = $Line
    $redacted = $redacted -replace '[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}', '[redacted-email]'
    $redacted = $redacted -replace '(password|passwd|token|session|auth)[=: ][^ \t\r\n"]+', '$1=[redacted]'
    $redacted
}

function Save-Text {
    param(
        [string]$Path,
        [string[]]$Lines
    )

    $Lines | ForEach-Object { Redact-Line $_ } | Set-Content -Path $Path -Encoding UTF8
}

function Save-AdbText {
    param(
        [string]$Path,
        [string[]]$AdbArgs
    )

    $lines = Invoke-Adb @AdbArgs 2>&1
    Save-Text -Path $Path -Lines $lines
}

$manifest = @()
$manifest += "timestamp=$(Get-Date -Format o)"
$manifest += "package=$PackageName"
$manifest += "deviceSerial=$DeviceSerial"
$manifest += "outputDir=$OutputDir"
$manifest += "note=Read-only evidence collection. Does not clear app data, does not clear logcat, does not touch Steam Cloud."
Save-Text -Path (Join-Path $OutputDir "manifest.txt") -Lines $manifest

Save-AdbText -Path (Join-Path $OutputDir "devices.txt") -AdbArgs @("devices")
$deviceLines = @(
    "manufacturer=$((Invoke-Adb shell getprop ro.product.manufacturer 2>&1) -join '')",
    "model=$((Invoke-Adb shell getprop ro.product.model 2>&1) -join '')",
    "abi=$((Invoke-Adb shell getprop ro.product.cpu.abi 2>&1) -join '')",
    "sdk=$((Invoke-Adb shell getprop ro.build.version.sdk 2>&1) -join '')"
)
Save-Text -Path (Join-Path $OutputDir "device.txt") -Lines $deviceLines
Save-AdbText -Path (Join-Path $OutputDir "package.txt") -AdbArgs @("shell", "pm", "list", "packages", "--user", "0", "--show-versioncode", $PackageName)
Save-AdbText -Path (Join-Path $OutputDir "package-path.txt") -AdbArgs @("shell", "pm", "path", "--user", "0", $PackageName)
Save-AdbText -Path (Join-Path $OutputDir "foreground.txt") -AdbArgs @("shell", "dumpsys", "activity", "top")

try {
    Invoke-Adb shell uiautomator dump /sdcard/sts2-branch-evidence-window.xml | Out-Null
    Invoke-Adb pull /sdcard/sts2-branch-evidence-window.xml (Join-Path $OutputDir "window.xml") | Out-Null
} catch {
    Save-Text -Path (Join-Path $OutputDir "window-error.txt") -Lines @($_.Exception.Message)
}

if ($IncludeScreenshot) {
    try {
        $screenPath = Join-Path $OutputDir "screen.png"
        Invoke-Adb shell screencap -p /sdcard/sts2-branch-evidence-screen.png | Out-Null
        Invoke-Adb pull /sdcard/sts2-branch-evidence-screen.png $screenPath | Out-Null
    } catch {
        Save-Text -Path (Join-Path $OutputDir "screen-error.txt") -Lines @($_.Exception.Message)
    }
} else {
    Save-Text -Path (Join-Path $OutputDir "screen-skipped.txt") -Lines @("Screenshot skipped by default. Use -IncludeScreenshot only when a visual observation can be paired with selected branch, PCK path/hash, and runtime logs.")
}

$logLines = Invoke-Adb logcat -d -t 2000 -v time 2>&1 |
    Select-String -Pattern "$PackageName|STS2Mobile|Godot|godot|AndroidRuntime|Loading PCK|Selected Steam branch|Selected game|Resolved game|Steam branch marker|Assembly cache" |
    ForEach-Object { $_.Line }
Save-Text -Path (Join-Path $OutputDir "focused-logcat.txt") -Lines $logLines

try {
    Save-AdbText -Path (Join-Path $OutputDir "private-inventory.txt") -AdbArgs @("shell", "run-as", $PackageName, "find", "files", "-maxdepth", "4", "-print")
    Save-AdbText -Path (Join-Path $OutputDir "private-branch-markers.txt") -AdbArgs @("shell", "run-as", $PackageName, "find", "files", "\(", "-name", "steam_branch.txt", "-o", "-name", "release_info.json", "-o", "-name", "download_state", "-o", "-name", "android_patch_marker.txt", "\)", "-print", "-exec", "sed", "-n", "1,120p", "{}", "\;")
    Save-AdbText -Path (Join-Path $OutputDir "private-pck-hashes.txt") -AdbArgs @("shell", "run-as", $PackageName, "find", "files", "\(", "-name", "SlayTheSpire2.pck", "-o", "-name", "bootstrap.pck", "\)", "-print", "-exec", "sha256sum", "{}", "\;", "-exec", "ls", "-l", "{}", "\;")
    Save-AdbText -Path (Join-Path $OutputDir "private-assembly-hashes.txt") -AdbArgs @("shell", "run-as", $PackageName, "find", "files", "\(", "-path", "*/.godot/mono/publish/*/sts2.dll", "-o", "-path", "*/.godot/mono/publish/*/STS2Mobile.dll", "\)", "-print", "-exec", "sha256sum", "{}", "\;", "-exec", "ls", "-l", "{}", "\;")
} catch {
    Save-Text -Path (Join-Path $OutputDir "private-evidence-error.txt") -Lines @($_.Exception.Message)
}

Write-Host "Evidence written to $OutputDir"
