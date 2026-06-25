param(
    [string]$AdbPath = "adb",
    [string]$PackageName = "",
    [string]$DeviceSerial = "",
    [string]$OutputRoot = "artifacts\android",
    [string]$RunLabel = "",
    [int]$WaitForDeviceSeconds = 0,
    [int]$LauncherWaitSeconds = 8,
    [int]$GameWaitSeconds = 25,
    [int]$StartGameTapX = 1092,
    [int]$StartGameTapY = 716,
    [string]$DisabledModKey = "manual:/storage/emulated/0/StS2Launcher/Mods/3747532120:SavesMerger",
    [string]$DisabledModNamePattern = "SavesMerger",
    [switch]$SkipScreenshots,
    [switch]$SkipReview
)

$ErrorActionPreference = "Stop"
if (Get-Variable -Name PSNativeCommandUseErrorActionPreference -Scope Global -ErrorAction SilentlyContinue) {
    $Global:PSNativeCommandUseErrorActionPreference = $false
}

$root = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot "android-adb-utils.ps1")

function Save-Text([string]$Path, [string]$Text) {
    $parent = Split-Path -Parent $Path
    if ($parent) {
        New-Item -ItemType Directory -Force $parent | Out-Null
    }

    Set-Content -LiteralPath $Path -Value $Text -Encoding UTF8
}

function Invoke-AdbCapture([string[]]$Arguments) {
    Invoke-AndroidAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments $Arguments
}

function Invoke-Adb([string[]]$Arguments) {
    Invoke-AndroidAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments $Arguments
}

function Invoke-RunAsCapture([string[]]$Arguments) {
    Invoke-AdbCapture -Arguments (@("shell", "run-as", $PackageName) + $Arguments)
}

function Save-RunAsFile([string]$OutputPath, [string]$AppPath) {
    try {
        $text = (Invoke-RunAsCapture -Arguments @("cat", $AppPath)) -join [Environment]::NewLine
        Save-Text -Path $OutputPath -Text $text
    } catch {
        Save-Text -Path $OutputPath -Text "CAPTURE_FAILED: $($_.Exception.Message)"
    }
}

function Save-AdbText([string]$OutputPath, [string[]]$Arguments) {
    try {
        $text = (Invoke-AdbCapture -Arguments $Arguments) -join [Environment]::NewLine
        Save-Text -Path $OutputPath -Text $text
    } catch {
        Save-Text -Path $OutputPath -Text "CAPTURE_FAILED: $($_.Exception.Message)"
    }
}

function Save-Screenshot([string]$Path, [string]$Label) {
    $devicePath = "/sdcard/sts2-mod-selector-$Label.png"
    try {
        Invoke-Adb -Arguments @("shell", "screencap", "-p", $devicePath)
        Invoke-Adb -Arguments @("pull", $devicePath, $Path)
    } finally {
        try {
            Invoke-Adb -Arguments @("shell", "rm", "-f", $devicePath)
        } catch {
        }
    }
}

function Get-DeviceWindowState {
    return (Invoke-AdbCapture -Arguments @("shell", "dumpsys", "window")) -join [Environment]::NewLine
}

function Test-DeviceLocked([string]$WindowState) {
    return $WindowState -match '(?m)mScreenLocked:\s*true'
        -or $WindowState -match '(?m)mDreamingLockscreen=true'
}

function Assert-DeviceUnlocked([string]$Stage) {
    $windowState = Get-DeviceWindowState
    Save-Text -Path (Join-Path $diagnosticsDir "device-window-state-$Stage.txt") -Text $windowState
    if (Test-DeviceLocked -WindowState $windowState) {
        throw "Device is locked during mod selector evidence capture stage '$Stage'. Unlock the device and rerun. Partial artifact: $outputDir"
    }
}

function Set-AppPrivateSelection([string]$Label, [string]$JsonText) {
    $localTemp = New-TemporaryFile
    try {
        [System.IO.File]::WriteAllText(
            $localTemp,
            $JsonText,
            [System.Text.UTF8Encoding]::new($false)
        )
        $deviceTemp = "/data/local/tmp/sts2-mod-selection-$Label.json"
        Invoke-Adb -Arguments @("push", $localTemp, $deviceTemp)
        Invoke-Adb -Arguments @("shell", "chmod", "644", $deviceTemp)
        Invoke-Adb -Arguments @(
            "shell",
            "run-as",
            $PackageName,
            "sh",
            "-c",
            "'mkdir -p files/mods && cat $deviceTemp > files/mods/mod_selection.json'"
        )
        Invoke-Adb -Arguments @("shell", "rm", "-f", $deviceTemp)
    } finally {
        Remove-Item -LiteralPath $localTemp -Force -ErrorAction SilentlyContinue
    }
}

function New-SelectionJson([string]$PlayMode, [hashtable]$EnabledMods) {
    [ordered]@{
        Version = 1
        PlayMode = $PlayMode
        EnabledMods = $EnabledMods
        UpdatedAtUtc = (Get-Date).ToUniversalTime().ToString("O")
    } | ConvertTo-Json -Depth 6
}

function Capture-FocusedLog([string]$Label) {
    $focusedPatterns = "Workshop|workshop|ModLoader|Loaded mod|Android mod scan|Play Vanilla|launcher-selected|Skipping disabled|selected mods|\[Mods\]|staged|missingDeps|Missing dependency|Manual Push blocked|Steam Cloud|Selected Steam branch|Selected game PCK|Loading PCK from|Runtime slot evidence|Runtime pack|runtime patch validation|Patch orchestration|Assembly cache|Exception thrown when calling mod initializer|MissingMethodException|JsonPropertyInfoValues|AndroidRuntime|FATAL EXCEPTION|NativeFallback|signal "
    $logcat = Invoke-AdbCapture -Arguments @("logcat", "-d", "-v", "time")
    $filtered = @($logcat | Select-String -Pattern $focusedPatterns | ForEach-Object { $_.Line })
    Save-Text -Path (Join-Path $logsDir "$Label-focused.txt") -Text ($filtered -join [Environment]::NewLine)
}

function Invoke-ModSelectorScenario([string]$Label, [string]$SelectionJson) {
    Write-Host "Running mod selector scenario: $Label"
    Assert-DeviceUnlocked -Stage "$Label-before-selection"
    Set-AppPrivateSelection -Label $Label -JsonText $SelectionJson
    Invoke-Adb -Arguments @("logcat", "-c")

    $component = Get-AndroidLauncherComponent -PackageName $PackageName
    Invoke-Adb -Arguments @("shell", "am", "force-stop", $PackageName)
    Invoke-Adb -Arguments @("shell", "am", "start", "-n", $component)
    Start-Sleep -Seconds $LauncherWaitSeconds

    Assert-DeviceUnlocked -Stage "$Label-before-start-game"
    if (-not $SkipScreenshots) {
        Save-Screenshot -Path (Join-Path $screenshotsDir "$Label-launcher-screen.png") -Label "$Label-launcher"
    }

    Invoke-Adb -Arguments @(
        "shell",
        "input",
        "tap",
        $StartGameTapX.ToString(),
        $StartGameTapY.ToString()
    )
    Start-Sleep -Seconds $GameWaitSeconds

    Save-RunAsFile -OutputPath (Join-Path $diagnosticsDir "$Label-mod-selection.json") -AppPath "files/mods/mod_selection.json"
    Save-RunAsFile -OutputPath (Join-Path $diagnosticsDir "$Label-last-mod-launch.json") -AppPath "files/mods/last_mod_launch.json"
    Save-RunAsFile -OutputPath (Join-Path $diagnosticsDir "$Label-last-automation.txt") -AppPath "files/last_launcher_automation.txt"
    Capture-FocusedLog -Label $Label

    if (-not $SkipScreenshots) {
        Assert-DeviceUnlocked -Stage "$Label-before-screenshot"
        Save-Screenshot -Path (Join-Path $screenshotsDir "$Label-screen.png") -Label $Label
    }
}

$AdbPath = Resolve-AndroidAdbPath -AdbPath $AdbPath
$DeviceSerial = Resolve-AndroidTargetDevice -AdbPath $AdbPath -DeviceSerial $DeviceSerial -WaitForDeviceSeconds $WaitForDeviceSeconds
$PackageName = Resolve-AndroidInstalledLauncherPackageName -AdbPath $AdbPath -DeviceSerial $DeviceSerial -PackageName $PackageName

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$generatedUtc = (Get-Date).ToUniversalTime().ToString("O")
$safeRunLabel = ($RunLabel -replace '[^A-Za-z0-9._-]', '-').Trim('-')
$folderName = if ([string]::IsNullOrWhiteSpace($safeRunLabel)) {
    "mod-selector-$timestamp"
} else {
    "mod-selector-$safeRunLabel-$timestamp"
}

$outputDir = Join-Path $root (Join-Path $OutputRoot $folderName)
$diagnosticsDir = Join-Path $outputDir "diagnostics"
$logsDir = Join-Path $outputDir "logs"
$screenshotsDir = Join-Path $outputDir "screenshots"
New-Item -ItemType Directory -Force $diagnosticsDir, $logsDir, $screenshotsDir | Out-Null

$metadata = [ordered]@{
    generatedUtc = $generatedUtc
    collector = "capture-mod-selector-evidence.ps1"
    readOnlySteamCloud = $true
    packageName = $PackageName
    deviceSerial = $DeviceSerial
    launcherWaitSeconds = $LauncherWaitSeconds
    gameWaitSeconds = $GameWaitSeconds
    startGameTapX = $StartGameTapX
    startGameTapY = $StartGameTapY
    disabledModKey = $DisabledModKey
    disabledModNamePattern = $DisabledModNamePattern
    screenshotsCaptured = -not [bool]$SkipScreenshots
    outputDirectory = $outputDir
}
Save-Text -Path (Join-Path $outputDir "run-metadata.json") -Text ($metadata | ConvertTo-Json -Depth 6)
Save-Text -Path (Join-Path $outputDir "ARTIFACT_HYGIENE.txt") -Text @"
Mod selector evidence artifact hygiene

This collector does not press Steam Cloud Push and does not upload save data.
It writes only the launcher-owned app-private mod selector file, starts the launcher/game for vanilla/modded/disabled-mod scenarios, and reads app-private selector markers plus focused package logs and screenshots.
Focused logcat is filtered to launcher/runtime/mod lines. Treat raw device state and screenshots as local test evidence.
"@

Save-AdbText -OutputPath (Join-Path $logsDir "adb-devices.txt") -Arguments @("devices", "-l")
Save-AdbText -OutputPath (Join-Path $diagnosticsDir "package.txt") -Arguments @("shell", "dumpsys", "package", $PackageName)
Save-AdbText -OutputPath (Join-Path $diagnosticsDir "abi-list.txt") -Arguments @("shell", "getprop", "ro.product.cpu.abilist")
Save-AdbText -OutputPath (Join-Path $diagnosticsDir "model.txt") -Arguments @("shell", "getprop", "ro.product.model")

$vanillaSelection = New-SelectionJson -PlayMode "vanilla" -EnabledMods @{}
$moddedSelection = New-SelectionJson -PlayMode "modded" -EnabledMods @{}
$disabledSelection = New-SelectionJson -PlayMode "modded" -EnabledMods @{
    $DisabledModKey = $false
}

Invoke-ModSelectorScenario -Label "final-vanilla" -SelectionJson $vanillaSelection
Invoke-ModSelectorScenario -Label "final-modded" -SelectionJson $moddedSelection
Invoke-ModSelectorScenario -Label "final-disabled-savesmerger" -SelectionJson $disabledSelection

$summary = [System.Collections.Generic.List[string]]::new()
$summary.Add("# Mod selector evidence summary")
$summary.Add("")
$summary.Add("Generated UTC: $generatedUtc")
$summary.Add("Package: $PackageName")
$summary.Add("Device: $DeviceSerial")
$summary.Add("Output: $outputDir")
$summary.Add("Collector boundary: This collector does not press Steam Cloud Push and does not upload save data.")
$summary.Add("")
$summary.Add("| Scenario | Selection marker | Launch marker | Focused log | Screenshot |")
$summary.Add("| --- | --- | --- | --- | --- |")
foreach ($label in @("final-vanilla", "final-modded", "final-disabled-savesmerger")) {
    $summary.Add("| $label | diagnostics/$label-mod-selection.json | diagnostics/$label-last-mod-launch.json | logs/$label-focused.txt | screenshots/$label-screen.png |")
}
Save-Text -Path (Join-Path $outputDir "summary.md") -Text ($summary -join [Environment]::NewLine)

if (-not $SkipReview) {
    if (-not $SkipScreenshots) {
        & (Join-Path $PSScriptRoot "review-mod-selector-evidence.ps1") `
            -EvidenceDir $outputDir `
            -DisabledModNamePattern $DisabledModNamePattern `
            -RequireScreenshots
    } else {
        & (Join-Path $PSScriptRoot "review-mod-selector-evidence.ps1") `
            -EvidenceDir $outputDir `
            -DisabledModNamePattern $DisabledModNamePattern
    }
}

Write-Host "Mod selector evidence captured: $outputDir"
