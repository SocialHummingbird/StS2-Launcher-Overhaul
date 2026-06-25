param(
    [string]$TempRoot = "",
    [switch]$KeepArtifacts
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$reviewScript = Join-Path $PSScriptRoot "review-mod-selector-evidence.ps1"
if ([string]::IsNullOrWhiteSpace($TempRoot)) {
    $TempRoot = Join-Path $root "tmp"
}

function Save-TestText([string]$Path, [string]$Text) {
    $parent = Split-Path -Parent $Path
    if ($parent) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }

    Set-Content -LiteralPath $Path -Value $Text -Encoding UTF8
}

function Save-TestJson([string]$Path, $Value) {
    Save-TestText $Path ($Value | ConvertTo-Json -Depth 10)
}

function New-ModSelectorEvidenceBundle(
    [string]$BaseDir,
    [switch]$MissingCloudFields,
    [switch]$DisabledStillSelected,
    [switch]$VanillaScansMods,
    [switch]$PushPerformed,
    [switch]$CrashLog
) {
    New-Item -ItemType Directory -Force -Path `
        (Join-Path $BaseDir "diagnostics"), `
        (Join-Path $BaseDir "logs"), `
        (Join-Path $BaseDir "screenshots") | Out-Null

    $baseLib = [ordered]@{
        Key = "workshop:3737335127"
        Id = "3737335127"
        Title = "BaseLib"
        Source = "Workshop"
        Path = "/data/user/0/com.example/files/workshop_mods/staged/3737335127"
        IsDependency = $false
        IsRequiredDependency = $false
    }
    $quickRestart = [ordered]@{
        Key = "workshop:3737322022"
        Id = "3737322022"
        Title = "Quick Restart 2"
        Source = "Workshop"
        Path = "/data/user/0/com.example/files/workshop_mods/staged/3737322022"
        IsDependency = $false
        IsRequiredDependency = $false
    }
    $savesMerger = [ordered]@{
        Key = "manual:/storage/emulated/0/StS2Launcher/Mods/3747532120:SavesMerger"
        Id = "SavesMerger"
        Title = "SavesMerger"
        Source = "Manual"
        Path = "/storage/emulated/0/StS2Launcher/Mods/3747532120"
        IsDependency = $false
        IsRequiredDependency = $false
    }

    $vanilla = [ordered]@{
        version = 1
        generatedAtUtc = "2026-06-25T00:00:00.0000000Z"
        playMode = "vanilla"
        scannedRoots = if ($VanillaScansMods) { 2 } else { 0 }
        enabledMods = 0
        status = "Android mod scan skipped by launcher Play Vanilla mode"
        selectionPath = "/data/user/0/com.example/files/mods/mod_selection.json"
        workshopModdedSaveCloudPushLocked = $false
        steamCloudPushPerformed = [bool]$PushPerformed
        selectedMods = @()
    }
    $modded = [ordered]@{
        version = 1
        generatedAtUtc = "2026-06-25T00:01:00.0000000Z"
        playMode = "modded"
        scannedRoots = 2
        enabledMods = 3
        status = "Android mod scan completed with launcher-selected mods"
        selectionPath = "/data/user/0/com.example/files/mods/mod_selection.json"
        workshopModdedSaveCloudPushLocked = $true
        steamCloudPushPerformed = [bool]$PushPerformed
        selectedMods = @($baseLib, $quickRestart, $savesMerger)
    }
    $disabledMods = if ($DisabledStillSelected) {
        @($baseLib, $quickRestart, $savesMerger)
    } else {
        @($baseLib, $quickRestart)
    }
    $disabled = [ordered]@{
        version = 1
        generatedAtUtc = "2026-06-25T00:02:00.0000000Z"
        playMode = "modded"
        scannedRoots = 2
        enabledMods = $disabledMods.Count
        status = "Android mod scan completed with launcher-selected mods"
        selectionPath = "/data/user/0/com.example/files/mods/mod_selection.json"
        workshopModdedSaveCloudPushLocked = $true
        steamCloudPushPerformed = [bool]$PushPerformed
        selectedMods = $disabledMods
    }

    if ($MissingCloudFields) {
        foreach ($marker in @($vanilla, $modded, $disabled)) {
            $marker.Remove("workshopModdedSaveCloudPushLocked")
            $marker.Remove("steamCloudPushPerformed")
        }
    }

    Save-TestJson (Join-Path $BaseDir "diagnostics\final-vanilla-last-mod-launch.json") $vanilla
    Save-TestJson (Join-Path $BaseDir "diagnostics\final-modded-last-mod-launch.json") $modded
    Save-TestJson (Join-Path $BaseDir "diagnostics\final-disabled-savesmerger-last-mod-launch.json") $disabled

    Save-TestJson (Join-Path $BaseDir "diagnostics\final-vanilla-mod-selection.json") @{
        Version = 1
        PlayMode = "vanilla"
        EnabledMods = @{}
    }
    Save-TestJson (Join-Path $BaseDir "diagnostics\final-modded-mod-selection.json") @{
        Version = 1
        PlayMode = "modded"
        EnabledMods = @{}
    }
    Save-TestJson (Join-Path $BaseDir "diagnostics\final-disabled-savesmerger-mod-selection.json") @{
        Version = 1
        PlayMode = "modded"
        EnabledMods = @{
            "manual:/storage/emulated/0/StS2Launcher/Mods/3747532120:SavesMerger" = $false
        }
    }

    $crashText = if ($CrashLog) { "`nNativeFallbackActivity`nFATAL EXCEPTION: main" } else { "" }
    $pushText = if ($PushPerformed) { "`nSteam Cloud Push performed" } else { "" }
    Save-TestText (Join-Path $BaseDir "logs\final-vanilla-focused.txt") "[Mods] Android mod scan skipped: launcher Play Vanilla mode is selected$crashText$pushText"
    Save-TestText (Join-Path $BaseDir "logs\final-modded-focused.txt") "[Mods] Scanning Workshop staged mods`nLoaded mod BaseLib`nLoaded mod SavesMerger$crashText$pushText"
    Save-TestText (Join-Path $BaseDir "logs\final-disabled-savesmerger-focused.txt") "[Mods] Scanning Workshop staged mods`nLoaded mod BaseLib`nSkipping disabled launcher-selected mod: SavesMerger$crashText$pushText"

    foreach ($label in @("final-vanilla", "final-modded", "final-disabled-savesmerger")) {
        Save-TestText (Join-Path $BaseDir "screenshots\$label-screen.png") "synthetic png placeholder"
    }
}

function Invoke-ReviewShouldPass([string]$EvidenceDir) {
    & $reviewScript -EvidenceDir $EvidenceDir -Quiet | Out-Null
}

function Invoke-ReviewShouldFail([string]$EvidenceDir, [string]$Description) {
    try {
        & $reviewScript -EvidenceDir $EvidenceDir -Quiet | Out-Null
    } catch {
        Write-Host "PASS negative case rejected: $Description"
        return
    }

    throw "Expected mod selector evidence review to fail: $Description"
}

$resolvedTempRoot = (Resolve-Path -LiteralPath (New-Item -ItemType Directory -Force -Path $TempRoot)).ProviderPath
$runRoot = Join-Path $resolvedTempRoot ("mod-selector-reviewer-tests-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

try {
    $positiveDir = Join-Path $runRoot "positive"
    New-ModSelectorEvidenceBundle -BaseDir $positiveDir
    Invoke-ReviewShouldPass -EvidenceDir $positiveDir
    Write-Host "PASS positive mod selector evidence accepted"

    $screenshotDir = Join-Path $runRoot "positive-screenshots"
    New-ModSelectorEvidenceBundle -BaseDir $screenshotDir
    & $reviewScript -EvidenceDir $screenshotDir -RequireScreenshots -Quiet | Out-Null
    Write-Host "PASS positive screenshot requirement accepted"

    $missingCloudFieldsDir = Join-Path $runRoot "negative-missing-cloud-fields"
    New-ModSelectorEvidenceBundle -BaseDir $missingCloudFieldsDir -MissingCloudFields
    Invoke-ReviewShouldFail -EvidenceDir $missingCloudFieldsDir -Description "missing Cloud safety marker fields"

    $disabledStillSelectedDir = Join-Path $runRoot "negative-disabled-still-selected"
    New-ModSelectorEvidenceBundle -BaseDir $disabledStillSelectedDir -DisabledStillSelected
    Invoke-ReviewShouldFail -EvidenceDir $disabledStillSelectedDir -Description "disabled SavesMerger still selected"

    $vanillaScansDir = Join-Path $runRoot "negative-vanilla-scans"
    New-ModSelectorEvidenceBundle -BaseDir $vanillaScansDir -VanillaScansMods
    Invoke-ReviewShouldFail -EvidenceDir $vanillaScansDir -Description "vanilla mode scanned mod roots"

    $pushPerformedDir = Join-Path $runRoot "negative-push-performed"
    New-ModSelectorEvidenceBundle -BaseDir $pushPerformedDir -PushPerformed
    Invoke-ReviewShouldFail -EvidenceDir $pushPerformedDir -Description "Steam Cloud Push performed"

    $crashLogDir = Join-Path $runRoot "negative-crash-log"
    New-ModSelectorEvidenceBundle -BaseDir $crashLogDir -CrashLog
    Invoke-ReviewShouldFail -EvidenceDir $crashLogDir -Description "fallback/crash signature in focused logs"
} finally {
    if (-not $KeepArtifacts -and (Test-Path -LiteralPath $runRoot)) {
        Remove-Item -LiteralPath $runRoot -Recurse -Force
    } elseif ($KeepArtifacts) {
        Write-Host "Kept test artifacts: $runRoot"
    }
}

Write-Host "Mod selector evidence reviewer tests passed."
