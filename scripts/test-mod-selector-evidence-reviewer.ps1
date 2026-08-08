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
    [switch]$MissingPushEvidence,
    [switch]$DisabledStillSelected,
    [switch]$VanillaScansMods,
    [switch]$PushPerformed,
    [switch]$CrashLog,
    [switch]$MissingRootSnapshots,
    [switch]$MissingActivationEvidence,
    [switch]$QuickRestartNoTargets,
    [switch]$SavesMergerClaimsPayload
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
        Root = [ordered]@{
            Exists = $true
            ManifestCount = 1
            PckCount = 1
            DllCount = 1
        }
    }
    $quickRestart = [ordered]@{
        Key = "workshop:3737322022"
        Id = "3737322022"
        Title = "Quick Restart 2"
        Source = "Workshop"
        Path = "/data/user/0/com.example/files/workshop_mods/staged/3737322022"
        IsDependency = $false
        IsRequiredDependency = $false
        Root = [ordered]@{
            Exists = $true
            ManifestCount = 1
            PckCount = 1
            DllCount = 1
        }
    }
    $savesMerger = [ordered]@{
        Key = "manual:/storage/emulated/0/StS2Launcher/Mods/3747532120:SavesMerger"
        Id = "SavesMerger"
        Title = "SavesMerger"
        Source = "Manual"
        Path = "/storage/emulated/0/StS2Launcher/Mods/3747532120"
        IsDependency = $false
        IsRequiredDependency = $false
        Root = [ordered]@{
            Exists = $true
            ManifestCount = 1
            PckCount = 1
            DllCount = 1
        }
    }

    if ($MissingRootSnapshots) {
        foreach ($mod in @($baseLib, $quickRestart, $savesMerger)) {
            $mod.Remove("Root")
        }
    }

    $baseLibActivation = [ordered]@{
        Id = "BaseLib"
        Title = "BaseLib"
        Path = $baseLib.Path
        State = "Loaded"
        Assembly = "BaseLib"
        ErrorCount = 0
        ManifestHasDll = $true
        ManifestHasPck = $true
        PayloadReady = $true
        RuntimeLoadSucceeded = $true
        HarmonyPatchTypeCount = 12
        HarmonyTargetCount = 4
        CompatibilityMode = "partial-android"
        ActivationStatus = "partial-android-compatibility"
        CompatibilityLimit = "Android-safe BaseLib initialization skips the full upstream PatchAll surface."
        InGameEffectVerified = $false
    }
    $quickRestartActivation = [ordered]@{
        Id = "QuickRestart"
        Title = "Quick Restart 2"
        Path = $quickRestart.Path
        State = "Loaded"
        Assembly = "QuickRestart"
        ErrorCount = 0
        ManifestHasDll = $true
        ManifestHasPck = $true
        PayloadReady = $true
        RuntimeLoadSucceeded = $true
        HarmonyPatchTypeCount = 3
        HarmonyTargetCount = if ($QuickRestartNoTargets) { 0 } else { 3 }
        CompatibilityMode = "native"
        ActivationStatus = if ($QuickRestartNoTargets) { "payload-loaded-unverified" } else { "runtime-patches-installed" }
        CompatibilityLimit = "Runtime patches are installed, but no in-game action was exercised by this marker."
        InGameEffectVerified = $false
    }
    $savesMergerActivation = [ordered]@{
        Id = "SavesMerger"
        Title = "SavesMerger"
        Path = $savesMerger.Path
        State = "Loaded"
        Assembly = ""
        ErrorCount = 0
        ManifestHasDll = $true
        ManifestHasPck = $true
        PayloadReady = [bool]$SavesMergerClaimsPayload
        RuntimeLoadSucceeded = $true
        HarmonyPatchTypeCount = 0
        HarmonyTargetCount = 0
        CompatibilityMode = "launcher-substitute"
        ActivationStatus = "launcher-compatibility-substitute"
        CompatibilityLimit = "Launcher save-path patches substitute for the mod; its DLL/PCK is not loaded."
        InGameEffectVerified = $false
    }

    $vanilla = [ordered]@{
        version = 2
        generatedAtUtc = "2026-06-25T00:00:00.0000000Z"
        playMode = "vanilla"
        scannedRoots = if ($VanillaScansMods) { 2 } else { 0 }
        enabledMods = 0
        payloadReadyMods = 0
        runtimePatchedMods = 0
        partialCompatibilityMods = 0
        compatibilitySubstituteMods = 0
        failedMods = 0
        inGameVerifiedMods = 0
        status = "Android mod scan skipped by launcher Play Vanilla mode"
        selectionPath = "/data/user/0/com.example/files/mods/mod_selection.json"
        steamCloudPushPerformed = [bool]$PushPerformed
        activationEvidence = @()
        selectedMods = @()
    }
    $moddedMods = if ($SavesMergerClaimsPayload) {
        @($baseLib, $quickRestart, $savesMerger)
    } else {
        @($baseLib, $quickRestart)
    }
    $moddedActivation = if ($SavesMergerClaimsPayload) {
        @($baseLibActivation, $quickRestartActivation, $savesMergerActivation)
    } else {
        @($baseLibActivation, $quickRestartActivation)
    }
    $modded = [ordered]@{
        version = 2
        generatedAtUtc = "2026-06-25T00:01:00.0000000Z"
        playMode = "modded"
        scannedRoots = 2
        enabledMods = $moddedMods.Count
        payloadReadyMods = if ($SavesMergerClaimsPayload) { 3 } else { 2 }
        runtimePatchedMods = if ($QuickRestartNoTargets) { 1 } else { 2 }
        partialCompatibilityMods = 1
        compatibilitySubstituteMods = if ($SavesMergerClaimsPayload) { 1 } else { 0 }
        failedMods = 0
        inGameVerifiedMods = 0
        status = "Android mod load attempt completed; inspect per-mod activation evidence"
        selectionPath = "/data/user/0/com.example/files/mods/mod_selection.json"
        steamCloudPushPerformed = [bool]$PushPerformed
        activationEvidence = $moddedActivation
        selectedMods = $moddedMods
    }
    $disabledMods = if ($DisabledStillSelected) {
        @($baseLib, $quickRestart, $savesMerger)
    } else {
        @($baseLib, $quickRestart)
    }
    $disabled = [ordered]@{
        version = 2
        generatedAtUtc = "2026-06-25T00:02:00.0000000Z"
        playMode = "modded"
        scannedRoots = 2
        enabledMods = $disabledMods.Count
        payloadReadyMods = 2
        runtimePatchedMods = if ($QuickRestartNoTargets) { 1 } else { 2 }
        partialCompatibilityMods = 1
        compatibilitySubstituteMods = 0
        failedMods = 0
        inGameVerifiedMods = 0
        status = "Android mod load attempt completed; inspect per-mod activation evidence"
        selectionPath = "/data/user/0/com.example/files/mods/mod_selection.json"
        steamCloudPushPerformed = [bool]$PushPerformed
        activationEvidence = @($baseLibActivation, $quickRestartActivation)
        selectedMods = $disabledMods
    }

    if ($MissingActivationEvidence) {
        $modded.Remove("activationEvidence")
    }

    if ($MissingPushEvidence) {
        foreach ($marker in @($vanilla, $modded, $disabled)) {
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
    Save-TestText (Join-Path $BaseDir "logs\final-modded-focused.txt") "[Mods] Scanning Workshop staged mods`n[Mods] Activation evidence: selected=$($moddedMods.Count) payloadReady=$($modded.payloadReadyMods) runtimePatched=2 partial=1 substitutes=$($modded.compatibilitySubstituteMods) failed=0 inGameVerified=0$crashText$pushText"
    Save-TestText (Join-Path $BaseDir "logs\final-disabled-savesmerger-focused.txt") "[Mods] Scanning Workshop staged mods`n[Mods] Activation evidence: selected=$($disabledMods.Count) payloadReady=2 runtimePatched=2 partial=1 substitutes=0 failed=0 inGameVerified=0$crashText$pushText"

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

    $missingPushEvidenceDir = Join-Path $runRoot "negative-missing-push-evidence"
    New-ModSelectorEvidenceBundle -BaseDir $missingPushEvidenceDir -MissingPushEvidence
    Invoke-ReviewShouldFail -EvidenceDir $missingPushEvidenceDir -Description "missing Steam Cloud Push evidence field"

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

    $missingRootSnapshotsDir = Join-Path $runRoot "negative-missing-root-snapshots"
    New-ModSelectorEvidenceBundle -BaseDir $missingRootSnapshotsDir -MissingRootSnapshots
    Invoke-ReviewShouldFail -EvidenceDir $missingRootSnapshotsDir -Description "selected mod root snapshots missing"

    $missingActivationDir = Join-Path $runRoot "negative-missing-activation-evidence"
    New-ModSelectorEvidenceBundle -BaseDir $missingActivationDir -MissingActivationEvidence
    Invoke-ReviewShouldFail -EvidenceDir $missingActivationDir -Description "selected mods without runtime activation evidence"

    $quickRestartNoTargetsDir = Join-Path $runRoot "negative-quick-restart-no-harmony-targets"
    New-ModSelectorEvidenceBundle -BaseDir $quickRestartNoTargetsDir -QuickRestartNoTargets
    Invoke-ReviewShouldFail -EvidenceDir $quickRestartNoTargetsDir -Description "Quick Restart payload without installed Harmony targets"

    $savesMergerPayloadDir = Join-Path $runRoot "negative-savesmerger-payload-claim"
    New-ModSelectorEvidenceBundle -BaseDir $savesMergerPayloadDir -SavesMergerClaimsPayload
    Invoke-ReviewShouldFail -EvidenceDir $savesMergerPayloadDir -Description "deprecated SavesMerger still selected for runtime activation"
} finally {
    if (-not $KeepArtifacts -and (Test-Path -LiteralPath $runRoot)) {
        Remove-Item -LiteralPath $runRoot -Recurse -Force
    } elseif ($KeepArtifacts) {
        Write-Host "Kept test artifacts: $runRoot"
    }
}

Write-Host "Mod selector evidence reviewer tests passed."
