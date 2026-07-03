param(
    [string]$AdbPath = "adb",
    [string]$PackageName = "com.sts2launcher.overhaul.fork.local",
    [string]$DeviceSerial = "",
    [string]$OutputRoot = "artifacts\android",
    [string]$RunLabel = "",
    [int]$WaitForDeviceSeconds = 0,
    [int]$LaunchWaitSeconds = 90,
    [switch]$SkipScreenshot,
    [switch]$SkipSaveValidation,
    [switch]$LeaveTestState
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
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    Set-Content -LiteralPath $Path -Value $Text -Encoding UTF8
}

function Invoke-Adb([string[]]$Arguments) {
    Invoke-AndroidAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments $Arguments
}

function Invoke-AdbCapture([string[]]$Arguments) {
    Invoke-AndroidAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments $Arguments
}

function Invoke-RunAsSh([string]$Command) {
    Invoke-Adb -Arguments @("shell", "run-as", $PackageName, "sh", "-c", $Command)
}

function Invoke-RunAsShCapture([string]$Command) {
    Invoke-AdbCapture -Arguments @("shell", "run-as", $PackageName, "sh", "-c", $Command)
}

function Set-AppPrivateJsonFile([string]$DevicePath, [string]$JsonText) {
    $localTemp = New-TemporaryFile
    try {
        [System.IO.File]::WriteAllText(
            $localTemp,
            $JsonText,
            [System.Text.UTF8Encoding]::new($false)
        )
        $deviceTemp = "/data/local/tmp/sts2-public-beta-modded-validation.json"
        Invoke-Adb -Arguments @("push", $localTemp, $deviceTemp)
        Invoke-Adb -Arguments @("shell", "chmod", "644", $deviceTemp)
        $deviceParent = $DevicePath -replace '/[^/]+$', ''
        if ($deviceParent -eq $DevicePath) {
            $deviceParent = "."
        }
        Invoke-RunAsSh "mkdir -p $deviceParent && cat $deviceTemp > $DevicePath"
        Invoke-Adb -Arguments @("shell", "rm", "-f", $deviceTemp)
    } finally {
        Remove-Item -LiteralPath $localTemp -Force -ErrorAction SilentlyContinue
    }
}

$AdbPath = Resolve-AndroidAdbPath -AdbPath $AdbPath
$DeviceSerial = Resolve-AndroidTargetDevice -AdbPath $AdbPath -DeviceSerial $DeviceSerial -WaitForDeviceSeconds $WaitForDeviceSeconds
$PackageName = Resolve-AndroidInstalledLauncherPackageName -AdbPath $AdbPath -DeviceSerial $DeviceSerial -PackageName $PackageName

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$safeRunLabel = if ([string]::IsNullOrWhiteSpace($RunLabel)) {
    "public-beta-modded-savemerger-$timestamp"
} else {
    ($RunLabel -replace '[^A-Za-z0-9._-]', '-').Trim('-')
}
$outputDir = Join-Path $root (Join-Path $OutputRoot "public-beta-modded-validation-$safeRunLabel")
$diagnosticsDir = Join-Path $outputDir "diagnostics"
New-Item -ItemType Directory -Force -Path $diagnosticsDir | Out-Null

$selectionBackupPath = "files/mods/mod_selection.codex-backup-$timestamp.json"
$moddedBackupPath = "files/modded.codex-backup-$timestamp"
$newModdedPath = "files/modded.codex-newcopy-test-$timestamp"
$cleanupLines = [System.Collections.Generic.List[string]]::new()
$vanillaRestoreSelection = [ordered]@{
    Version = 1
    PlayMode = "vanilla"
    EnabledMods = [ordered]@{}
    UpdatedAtUtc = (Get-Date).ToUniversalTime().ToString("O")
} | ConvertTo-Json -Depth 6

try {
    Save-Text -Path (Join-Path $outputDir "run-metadata.json") -Text ([ordered]@{
        generatedUtc = (Get-Date).ToUniversalTime().ToString("O")
        collector = "run-public-beta-modded-validation.ps1"
        readOnlySteamCloud = $true
        packageName = $PackageName
        deviceSerial = $DeviceSerial
        launchWaitSeconds = $LaunchWaitSeconds
        leaveTestState = [bool]$LeaveTestState
        outputDirectory = $outputDir
    } | ConvertTo-Json -Depth 5)

    Save-Text -Path (Join-Path $outputDir "ARTIFACT_HYGIENE.txt") -Text @"
Public-beta modded validation artifact hygiene

This script does not press Steam Cloud Push and does not upload save data.
It writes launcher-owned app-private test markers, runs the public-beta safe-launch automation, captures Workshop/runtime/save diagnostics, and restores the original mod selector plus modded-save directory unless -LeaveTestState is used.
"@

    Invoke-RunAsSh "mkdir -p files/mods && if [ -f files/mods/mod_selection.json ]; then cp files/mods/mod_selection.json $selectionBackupPath; fi"
    Invoke-RunAsSh "if [ -d files/modded ]; then mv files/modded $moddedBackupPath; fi"

    $selection = [ordered]@{
        Version = 1
        PlayMode = "modded"
        EnabledMods = [ordered]@{
            "workshop:3737335127" = $true
            "workshop:3737322022" = $true
            "manual:/storage/emulated/0/StS2Launcher/Mods/3747532120:SavesMerger" = $true
        }
        UpdatedAtUtc = (Get-Date).ToUniversalTime().ToString("O")
    } | ConvertTo-Json -Depth 6
    Set-AppPrivateJsonFile -DevicePath "files/mods/mod_selection.json" -JsonText $selection

    Invoke-RunAsSh "printf 'branch=public-beta\naction=launchsafe\n' > files/launcher_automation_action.txt"
    Save-Text -Path (Join-Path $diagnosticsDir "pre-launch-state.txt") -Text (
        (Invoke-RunAsShCapture "ls -ld files/modded $moddedBackupPath 2>/dev/null; cat files/mods/mod_selection.json; cat files/launcher_automation_action.txt") -join [Environment]::NewLine
    )

    $captureArgs = @(
        "-AdbPath", $AdbPath,
        "-PackageName", $PackageName,
        "-DeviceSerial", $DeviceSerial,
        "-OutputRoot", $OutputRoot,
        "-Phase", "public-beta",
        "-RunLabel", $safeRunLabel,
        "-WaitSeconds", $LaunchWaitSeconds.ToString(),
        "-Launch",
        "-ClearLogcat"
    )
    if (-not $SkipScreenshot) {
        $captureArgs += "-Screenshot"
    }

    & (Join-Path $PSScriptRoot "capture-workshop-mod-evidence.ps1") @captureArgs

    $latestWorkshopEvidence = Get-ChildItem -LiteralPath (Join-Path $root $OutputRoot) -Directory |
        Where-Object { $_.Name -like "*$safeRunLabel*" } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($latestWorkshopEvidence) {
        try {
            & (Join-Path $PSScriptRoot "review-workshop-mod-evidence.ps1") `
                -EvidenceDir $latestWorkshopEvidence.FullName `
                -RequirePhase public-beta `
                -RequireScreenshot:(!$SkipScreenshot) |
                Tee-Object -FilePath (Join-Path $diagnosticsDir "review-workshop-result.txt")
        } catch {
            Save-Text -Path (Join-Path $diagnosticsDir "review-workshop-result.txt") -Text "REVIEW_FAILED: $($_.Exception.Message)"
        }
    }

    if (-not $SkipSaveValidation) {
        & (Join-Path $PSScriptRoot "collect-android-save-validation.ps1") `
            -AdbPath $AdbPath `
            -PackageName $PackageName `
            -DeviceSerial $DeviceSerial `
            -OutputRoot $OutputRoot `
            -LogcatTailLines 100000 `
            -DumpSaveFiles
    }

    Save-Text -Path (Join-Path $diagnosticsDir "post-launch-state.txt") -Text (
        (Invoke-RunAsShCapture "ls -ld files/modded $moddedBackupPath 2>/dev/null; cat files/mods/last_mod_launch.json 2>/dev/null; cat files/last_launcher_automation.txt 2>/dev/null") -join [Environment]::NewLine
    )
} finally {
    if (-not $LeaveTestState) {
        try {
            Invoke-RunAsSh "if [ -d files/modded ]; then mv files/modded $newModdedPath; fi; if [ -d $moddedBackupPath ]; then mv $moddedBackupPath files/modded; fi"
            $cleanupLines.Add("restored modded save directory; new copy, if any, moved to $newModdedPath")
        } catch {
            $cleanupLines.Add("failed to restore modded save directory: $($_.Exception.Message)")
        }

        try {
            Invoke-RunAsSh "if [ -f $selectionBackupPath ]; then cp $selectionBackupPath files/mods/mod_selection.json; rm -f $selectionBackupPath; else exit 42; fi"
            $cleanupLines.Add("restored previous mod selection")
        } catch {
            try {
                Set-AppPrivateJsonFile -DevicePath "files/mods/mod_selection.json" -JsonText $vanillaRestoreSelection
                $cleanupLines.Add("previous mod selection backup missing; restored vanilla selection")
            } catch {
                $cleanupLines.Add("failed to restore mod selection: $($_.Exception.Message)")
            }
        }

        try {
            Invoke-RunAsSh "rm -f files/launcher_automation_action.txt"
            $cleanupLines.Add("removed launcher automation marker")
        } catch {
            $cleanupLines.Add("failed to remove launcher automation marker: $($_.Exception.Message)")
        }
    } else {
        $cleanupLines.Add("left test state in place because -LeaveTestState was used")
    }

    Save-Text -Path (Join-Path $diagnosticsDir "cleanup.txt") -Text ($cleanupLines -join [Environment]::NewLine)
}

Write-Host "Public-beta modded validation wrapper complete: $outputDir"
Write-Host "This script does not press Steam Cloud Push."
