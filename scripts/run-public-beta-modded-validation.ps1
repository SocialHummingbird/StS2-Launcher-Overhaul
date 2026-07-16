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
. (Join-Path $PSScriptRoot "android-shell-utils.ps1")

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
    $quotedCommand = ConvertTo-AndroidShellSingleQuoted $Command
    Invoke-Adb -Arguments @("shell", "run-as $PackageName sh -c $quotedCommand")
}

function Invoke-RunAsShCapture([string]$Command) {
    $quotedCommand = ConvertTo-AndroidShellSingleQuoted $Command
    Invoke-AdbCapture -Arguments @("shell", "run-as $PackageName sh -c $quotedCommand")
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

function Find-ModActivation($ActivationEvidence, [string]$Pattern) {
    foreach ($activation in @($ActivationEvidence)) {
        $text = @($activation.Id, $activation.Title, $activation.Path, $activation.Assembly) -join " "
        if ($text -match $Pattern) {
            return $activation
        }
    }

    return $null
}

function Assert-PublicBetaModdedLaunchMarker([string]$EvidenceDirectory, [datetime]$RunStartedUtc) {
    $markerPath = Join-Path $EvidenceDirectory "diagnostics\last-mod-launch.json"
    if (-not (Test-Path -LiteralPath $markerPath)) {
        $validationFailures.Add("Modded launch marker was not captured: $markerPath")
        return
    }

    try {
        $marker = Get-Content -Raw -LiteralPath $markerPath | ConvertFrom-Json
    } catch {
        $validationFailures.Add("Modded launch marker is not valid JSON: $($_.Exception.Message)")
        return
    }

    $generatedAt = [datetime]::MinValue
    if (-not [datetime]::TryParse("$($marker.generatedAtUtc)", [ref]$generatedAt)) {
        $validationFailures.Add("Modded launch marker is missing a readable generatedAtUtc value.")
        return
    }

    if ($generatedAt.ToUniversalTime() -lt $RunStartedUtc) {
        $validationFailures.Add("Modded launch marker is stale: generatedAtUtc=$($marker.generatedAtUtc), runStartedUtc=$($RunStartedUtc.ToString("O")).")
    }

    if ("$($marker.playMode)" -ne "modded") {
        $validationFailures.Add("Modded launch marker did not record modded play mode: playMode=$($marker.playMode).")
    }

    if ([int]$marker.version -lt 2) {
        $validationFailures.Add("Modded launch marker predates per-mod activation evidence: version=$($marker.version).")
    }

    if ([int]$marker.enabledMods -lt 3) {
        $validationFailures.Add("Modded launch marker recorded fewer than three enabled mods: enabledMods=$($marker.enabledMods).")
    }

    if ([int]$marker.scannedRoots -lt 3) {
        $validationFailures.Add("Modded launch marker recorded fewer than three scanned roots: scannedRoots=$($marker.scannedRoots).")
    }

    $selectedMods = @($marker.selectedMods)
    $selectedText = ($selectedMods | ForEach-Object { @($_.Key, $_.Id, $_.Title, $_.Path) -join " " }) -join "`n"
    foreach ($requiredPattern in @("BaseLib", "Quick\s*Restart|QuickRestart", "SavesMerger")) {
        if ($selectedText -notmatch $requiredPattern) {
            $validationFailures.Add("Modded launch marker is missing selected mod evidence matching '$requiredPattern'.")
        }
    }


    $activationEvidence = @($marker.activationEvidence)
    if ($activationEvidence.Count -ne $selectedMods.Count) {
        $validationFailures.Add("Modded launch marker activation count does not match selected mod count: activation=$($activationEvidence.Count), selected=$($selectedMods.Count).")
    }

    if ([int]$marker.failedMods -ne 0) {
        $validationFailures.Add("Modded launch marker records failed runtime loads: failedMods=$($marker.failedMods).")
    }
    if ([int]$marker.inGameVerifiedMods -ne 0) {
        $validationFailures.Add("Startup activation marker unexpectedly claims in-game verification: inGameVerifiedMods=$($marker.inGameVerifiedMods).")
    }
    if ([int]$marker.payloadReadyMods -lt 2) {
        $validationFailures.Add("Modded launch marker records fewer than two payload-ready mods: payloadReadyMods=$($marker.payloadReadyMods).")
    }
    if ([int]$marker.runtimePatchedMods -lt 1) {
        $validationFailures.Add("Modded launch marker records no installed runtime patches: runtimePatchedMods=$($marker.runtimePatchedMods).")
    }
    if ([int]$marker.partialCompatibilityMods -lt 1) {
        $validationFailures.Add("Modded launch marker does not classify BaseLib partial compatibility.")
    }
    if ([int]$marker.compatibilitySubstituteMods -lt 1) {
        $validationFailures.Add("Modded launch marker does not classify the SavesMerger launcher substitute.")
    }

    $baseLibActivation = Find-ModActivation $activationEvidence "BaseLib"
    if ($null -eq $baseLibActivation) {
        $validationFailures.Add("Modded launch marker is missing BaseLib activation evidence.")
    } elseif (
        "$($baseLibActivation.CompatibilityMode)" -ne "partial-android" -or
        "$($baseLibActivation.ActivationStatus)" -ne "partial-android-compatibility" -or
        [bool]$baseLibActivation.PayloadReady -ne $true -or
        [bool]$baseLibActivation.RuntimeLoadSucceeded -ne $true -or
        [int]$baseLibActivation.ErrorCount -ne 0
    ) {
        $validationFailures.Add("BaseLib activation evidence does not prove an error-free partial Android runtime load.")
    }

    $quickRestartActivation = Find-ModActivation $activationEvidence "Quick\s*Restart|QuickRestart"
    if ($null -eq $quickRestartActivation) {
        $validationFailures.Add("Modded launch marker is missing Quick Restart activation evidence.")
    } elseif (
        "$($quickRestartActivation.CompatibilityMode)" -ne "native" -or
        "$($quickRestartActivation.ActivationStatus)" -ne "runtime-patches-installed" -or
        [bool]$quickRestartActivation.PayloadReady -ne $true -or
        [bool]$quickRestartActivation.RuntimeLoadSucceeded -ne $true -or
        [int]$quickRestartActivation.ErrorCount -ne 0 -or
        [int]$quickRestartActivation.HarmonyTargetCount -lt 1
    ) {
        $validationFailures.Add("Quick Restart activation evidence does not prove an error-free payload load with installed Harmony targets.")
    }

    $savesMergerActivation = Find-ModActivation $activationEvidence "SavesMerger|UnifiedSavePath"
    if ($null -eq $savesMergerActivation) {
        $validationFailures.Add("Modded launch marker is missing SavesMerger compatibility evidence.")
    } elseif (
        "$($savesMergerActivation.CompatibilityMode)" -ne "launcher-substitute" -or
        "$($savesMergerActivation.ActivationStatus)" -ne "launcher-compatibility-substitute" -or
        [bool]$savesMergerActivation.PayloadReady -ne $false -or
        [bool]$savesMergerActivation.RuntimeLoadSucceeded -ne $true
    ) {
        $validationFailures.Add("SavesMerger evidence does not truthfully describe the launcher compatibility substitute.")
    }

    foreach ($mod in $selectedMods) {
        $name = (@($mod.Key, $mod.Id, $mod.Title) | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Select-Object -First 1)
        if ([string]::IsNullOrWhiteSpace([string]$name)) {
            $name = "<unknown>"
        }

        $rootProperty = $mod.PSObject.Properties["Root"]
        if ($null -eq $rootProperty) {
            $validationFailures.Add("Modded launch marker selected mod $name is missing Root snapshot.")
            continue
        }

        $root = $rootProperty.Value
        if ([bool]$root.Exists -ne $true) {
            $validationFailures.Add("Modded launch marker selected mod $name root does not exist.")
        }

        if ([int]$root.ManifestCount -lt 1) {
            $validationFailures.Add("Modded launch marker selected mod $name root has no manifest JSON.")
        }
    }
}

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
$validationFailures = [System.Collections.Generic.List[string]]::new()
$latestWorkshopEvidencePath = ""
$deviceStateMayBeMutated = $false
$runStartedUtc = (Get-Date).ToUniversalTime()
$vanillaRestoreSelection = [ordered]@{
    Version = 1
    PlayMode = "vanilla"
    EnabledMods = [ordered]@{}
    UpdatedAtUtc = (Get-Date).ToUniversalTime().ToString("O")
} | ConvertTo-Json -Depth 6

try {
    $AdbPath = Resolve-AndroidAdbPath -AdbPath $AdbPath
    $DeviceSerial = Resolve-AndroidTargetDevice -AdbPath $AdbPath -DeviceSerial $DeviceSerial -WaitForDeviceSeconds $WaitForDeviceSeconds
    $PackageName = Resolve-AndroidInstalledLauncherPackageName -AdbPath $AdbPath -DeviceSerial $DeviceSerial -PackageName $PackageName

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
    $deviceStateMayBeMutated = $true
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

    $captureArgs = @{
        AdbPath = $AdbPath
        PackageName = $PackageName
        DeviceSerial = $DeviceSerial
        OutputRoot = $OutputRoot
        Phase = "public-beta"
        RunLabel = $safeRunLabel
        WaitSeconds = $LaunchWaitSeconds
        Launch = $true
        ClearLogcat = $true
    }
    if (-not $SkipScreenshot) {
        $captureArgs["Screenshot"] = $true
    }

    try {
        & (Join-Path $PSScriptRoot "capture-workshop-mod-evidence.ps1") @captureArgs
    } catch {
        $validationFailures.Add("Workshop evidence capture failed: $($_.Exception.Message)")
        Save-Text -Path (Join-Path $diagnosticsDir "capture-workshop-result.txt") -Text "CAPTURE_FAILED: $($_.Exception.Message)"
    }

    $latestWorkshopEvidence = Get-ChildItem -LiteralPath (Join-Path $root $OutputRoot) -Directory |
        Where-Object { $_.Name -like "*$safeRunLabel*" } |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($latestWorkshopEvidence) {
        $latestWorkshopEvidencePath = $latestWorkshopEvidence.FullName
        $reviewArgs = @{
            EvidenceDir = $latestWorkshopEvidence.FullName
            RequirePhase = "public-beta"
        }
        if (-not $SkipScreenshot) {
            $reviewArgs["RequireScreenshot"] = $true
        }

        try {
            & (Join-Path $PSScriptRoot "review-workshop-mod-evidence.ps1") @reviewArgs |
                Tee-Object -FilePath (Join-Path $diagnosticsDir "review-workshop-result.txt")
        } catch {
            $validationFailures.Add("Workshop evidence review failed: $($_.Exception.Message)")
            Save-Text -Path (Join-Path $diagnosticsDir "review-workshop-result.txt") -Text "REVIEW_FAILED: $($_.Exception.Message)"
        }

        Assert-PublicBetaModdedLaunchMarker -EvidenceDirectory $latestWorkshopEvidence.FullName -RunStartedUtc $runStartedUtc
    } else {
        $validationFailures.Add("Workshop evidence capture directory was not found for run label $safeRunLabel.")
    }

    if (-not $SkipSaveValidation) {
        try {
            & (Join-Path $PSScriptRoot "collect-android-save-validation.ps1") `
                -AdbPath $AdbPath `
                -PackageName $PackageName `
                -DeviceSerial $DeviceSerial `
                -OutputRoot $OutputRoot `
                -LogcatTailLines 100000 `
                -DumpSaveFiles
        } catch {
            $validationFailures.Add("Save validation capture failed: $($_.Exception.Message)")
            Save-Text -Path (Join-Path $diagnosticsDir "save-validation-result.txt") -Text "SAVE_VALIDATION_FAILED: $($_.Exception.Message)"
        }
    }

    Save-Text -Path (Join-Path $diagnosticsDir "post-launch-state.txt") -Text (
        (Invoke-RunAsShCapture "ls -ld files/modded $moddedBackupPath 2>/dev/null; cat files/mods/last_mod_launch.json 2>/dev/null; cat files/last_launcher_automation.txt 2>/dev/null") -join [Environment]::NewLine
    )
} catch {
    $validationFailures.Add("Public-beta modded validation setup/run failed: $($_.Exception.Message)")
    Save-Text -Path (Join-Path $diagnosticsDir "setup-run-result.txt") -Text "SETUP_OR_RUN_FAILED: $($_.Exception.Message)"
} finally {
    if (-not $LeaveTestState) {
        if ($deviceStateMayBeMutated) {
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
            $cleanupLines.Add("skipped cleanup because device state was not mutated")
        }
    } else {
        $cleanupLines.Add("left test state in place because -LeaveTestState was used")
    }

    Save-Text -Path (Join-Path $diagnosticsDir "cleanup.txt") -Text ($cleanupLines -join [Environment]::NewLine)
}

$validationResult = [ordered]@{
    generatedUtc = (Get-Date).ToUniversalTime().ToString("O")
    status = if ($validationFailures.Count -eq 0) { "passed" } else { "failed" }
    outputDirectory = $outputDir
    workshopEvidenceDirectory = $latestWorkshopEvidencePath
    steamCloudPushPerformed = $false
    failures = @($validationFailures)
    cleanup = @($cleanupLines)
} | ConvertTo-Json -Depth 5
Save-Text -Path (Join-Path $diagnosticsDir "validation-result.json") -Text $validationResult

Write-Host "Public-beta modded validation wrapper artifacts: $outputDir"
Write-Host "This script does not press Steam Cloud Push."
if ($validationFailures.Count -gt 0) {
    foreach ($failure in $validationFailures) {
        Write-Host "VALIDATION_FAILED $failure"
    }

    throw "Public-beta modded validation failed. Artifacts were preserved at $outputDir"
}

Write-Host "Public-beta modded validation passed."
