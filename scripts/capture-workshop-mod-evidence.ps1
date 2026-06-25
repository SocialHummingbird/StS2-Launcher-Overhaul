param(
    [string]$AdbPath = "adb",
    [string]$PackageName = "",
    [string]$DeviceSerial = "",
    [string]$OutputRoot = "artifacts\android",
    [ValidateSet("no-mods", "simple", "dependency", "broken", "public", "public-beta", "core-release")]
    [string]$Phase = "no-mods",
    [string]$RunLabel = "",
    [int]$WaitForDeviceSeconds = 0,
    [int]$WaitSeconds = 8,
    [switch]$Launch,
    [switch]$StartGame,
    [int]$StartGameTapX = 1092,
    [int]$StartGameTapY = 716,
    [int]$StartGameWaitSeconds = 25,
    [switch]$ClearLogcat,
    [switch]$IncludeRawLogcat,
    [switch]$Screenshot
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

function Save-AdbText([string]$Path, [string[]]$Arguments, [switch]$AllowFailure) {
    try {
        $text = (Invoke-AndroidAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments $Arguments) -join [Environment]::NewLine
        Save-Text -Path $Path -Text $text
    } catch {
        if (-not $AllowFailure) {
            throw
        }
        Save-Text -Path $Path -Text "CAPTURE_FAILED: $($_.Exception.Message)"
    }
}

function Save-RunAsText([string]$Path, [string]$Command, [switch]$AllowFailure) {
    Save-AdbText -Path $Path -Arguments @("shell", "run-as", $PackageName, "sh", "-c", $Command) -AllowFailure:$AllowFailure
}

function Invoke-RunAsCapture([string[]]$Arguments) {
    Invoke-AndroidAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments (@("shell", "run-as", $PackageName) + $Arguments)
}

function Save-RunAsArgsText([string]$Path, [string[]]$Arguments, [switch]$AllowFailure) {
    try {
        $text = (Invoke-RunAsCapture -Arguments $Arguments) -join [Environment]::NewLine
        Save-Text -Path $Path -Text $text
    } catch {
        if (-not $AllowFailure) {
            throw
        }
        Save-Text -Path $Path -Text "CAPTURE_FAILED: $($_.Exception.Message)"
    }
}

function Read-RunAsFile([string]$Path) {
    try {
        return (Invoke-RunAsCapture -Arguments @("cat", $Path)) -join [Environment]::NewLine
    } catch {
        return ""
    }
}

function Save-RunAsFileWithHeader([string]$OutputPath, [string[]]$Paths) {
    $lines = [System.Collections.Generic.List[string]]::new()
    foreach ($path in $Paths) {
        $lines.Add("===== $path")
        $content = Read-RunAsFile -Path $path
        if (-not [string]::IsNullOrWhiteSpace($content)) {
            $lines.Add($content)
        }
    }
    Save-Text -Path $OutputPath -Text ($lines -join [Environment]::NewLine)
}

function Add-CapturedLines([System.Collections.Generic.List[string]]$Lines, [object[]]$Captured) {
    foreach ($line in $Captured) {
        $Lines.Add([string]$line)
    }
}

function Save-Screenshot([string]$Path) {
    $devicePath = "/sdcard/sts2-workshop-evidence-$timestamp.png"
    try {
        Invoke-AndroidAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "screencap", "-p", $devicePath)
        Invoke-AndroidAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("pull", $devicePath, $Path)
    } finally {
        try {
            Invoke-AndroidAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "rm", "-f", $devicePath)
        } catch {
        }
    }
}

function Copy-FileIfPresent([string]$SourcePath, [string]$DestinationPath) {
    if (Test-Path -LiteralPath $SourcePath) {
        Copy-Item -LiteralPath $SourcePath -Destination $DestinationPath -Force
    }
}

function Get-DeviceWindowState {
    return (Invoke-AndroidAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "dumpsys", "window")) -join [Environment]::NewLine
}

function Test-DeviceLocked([string]$WindowState) {
    return $WindowState -match '(?m)mScreenLocked:\s*true'
        -or $WindowState -match '(?m)mDreamingLockscreen=true'
}

function Assert-DeviceUnlocked([string]$Stage) {
    $windowState = Get-DeviceWindowState
    Save-Text -Path (Join-Path $diagnosticsDir "device-window-state-$Stage.txt") -Text $windowState
    if (Test-DeviceLocked -WindowState $windowState) {
        throw "Device is locked during Workshop evidence capture stage '$Stage'. Unlock the device and rerun. Partial artifact: $outputDir"
    }
}

$AdbPath = Resolve-AndroidAdbPath -AdbPath $AdbPath
$DeviceSerial = Resolve-AndroidTargetDevice -AdbPath $AdbPath -DeviceSerial $DeviceSerial -WaitForDeviceSeconds $WaitForDeviceSeconds
$PackageName = Resolve-AndroidInstalledLauncherPackageName -AdbPath $AdbPath -DeviceSerial $DeviceSerial -PackageName $PackageName

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$generatedUtc = (Get-Date).ToUniversalTime().ToString("O")
$safeRunLabel = ($RunLabel -replace '[^A-Za-z0-9._-]', '-').Trim('-')
$folderName = if ([string]::IsNullOrWhiteSpace($safeRunLabel)) {
    "workshop-mods-$Phase-$timestamp"
} else {
    "workshop-mods-$Phase-$safeRunLabel-$timestamp"
}

$outputDir = Join-Path $root (Join-Path $OutputRoot $folderName)
$diagnosticsDir = Join-Path $outputDir "diagnostics"
$logsDir = Join-Path $outputDir "logs"
$screenshotsDir = Join-Path $outputDir "screenshots"
New-Item -ItemType Directory -Force $diagnosticsDir, $logsDir, $screenshotsDir | Out-Null

$metadata = [ordered]@{
    generatedUtc = $generatedUtc
    collector = "capture-workshop-mod-evidence.ps1"
    readOnlySteamCloud = $true
    phase = $Phase
    runLabel = if ([string]::IsNullOrWhiteSpace($safeRunLabel)) { $null } else { $safeRunLabel }
    packageName = $PackageName
    deviceSerial = $DeviceSerial
    launchRequested = [bool]$Launch
    startGameRequested = [bool]$StartGame
    startGameTapX = if ($StartGame) { $StartGameTapX } else { $null }
    startGameTapY = if ($StartGame) { $StartGameTapY } else { $null }
    startGameWaitSeconds = if ($StartGame) { $StartGameWaitSeconds } else { $null }
    clearLogcatRequested = [bool]$ClearLogcat
    includeRawLogcat = [bool]$IncludeRawLogcat
    outputDirectory = $outputDir
}
Save-Text -Path (Join-Path $outputDir "run-metadata.json") -Text ($metadata | ConvertTo-Json -Depth 5)

$artifactHygiene = @"
Workshop mod evidence artifact hygiene

This collector does not press Steam Cloud Push and does not upload save data.
It reads app-private Workshop manifests, mod selector markers, clear markers, staged/downloaded file trees, package metadata, focused logcat, and optional screenshots.
Raw full logcat is omitted by default. If logs/logcat-full.txt exists, treat it as local-only until manually reviewed and redacted.
Captured app-private diagnostics can include local package paths, branch names, Workshop item IDs, hashes, and device/package metadata.
Workshop download source evidence should include only source kind, size, content handle, URL presence, and URL host. Raw signed Workshop download URLs should not be present.
"@
Save-Text -Path (Join-Path $outputDir "ARTIFACT_HYGIENE.txt") -Text $artifactHygiene

Assert-DeviceUnlocked -Stage "before-launch"

if ($ClearLogcat) {
    Invoke-AndroidAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("logcat", "-c")
}

if ($Launch) {
    $component = Get-AndroidLauncherComponent -PackageName $PackageName
    Invoke-AndroidAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "am", "force-stop", $PackageName)
    Invoke-AndroidAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("shell", "am", "start", "-n", $component)
    Start-Sleep -Seconds $WaitSeconds
}

if ($StartGame) {
    if (-not $Launch) {
        throw "-StartGame requires -Launch so run metadata and log timing identify the launcher-started game session."
    }

    Assert-DeviceUnlocked -Stage "before-start-game"
    Invoke-AndroidAdb -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @(
        "shell",
        "input",
        "tap",
        $StartGameTapX.ToString(),
        $StartGameTapY.ToString()
    )
    Start-Sleep -Seconds $StartGameWaitSeconds
}

if ($Screenshot) {
    Assert-DeviceUnlocked -Stage "before-screenshot"
    $phaseScreenshotPath = Join-Path $screenshotsDir "$Phase.png"
    Save-Screenshot -Path $phaseScreenshotPath
    if (-not [string]::IsNullOrWhiteSpace($safeRunLabel)) {
        Copy-FileIfPresent -SourcePath $phaseScreenshotPath -DestinationPath (Join-Path $screenshotsDir "$safeRunLabel-screen.png")
    }
}

Save-AdbText -Path (Join-Path $logsDir "adb-devices.txt") -Arguments @("devices", "-l") -AllowFailure
Save-AdbText -Path (Join-Path $diagnosticsDir "package.txt") -Arguments @("shell", "dumpsys", "package", $PackageName) -AllowFailure
Save-AdbText -Path (Join-Path $diagnosticsDir "abi-list.txt") -Arguments @("shell", "getprop", "ro.product.cpu.abilist") -AllowFailure
Save-AdbText -Path (Join-Path $diagnosticsDir "manufacturer.txt") -Arguments @("shell", "getprop", "ro.product.manufacturer") -AllowFailure
Save-AdbText -Path (Join-Path $diagnosticsDir "model.txt") -Arguments @("shell", "getprop", "ro.product.model") -AllowFailure
Save-AdbText -Path (Join-Path $diagnosticsDir "android-sdk.txt") -Arguments @("shell", "getprop", "ro.build.version.sdk") -AllowFailure

$markerPaths = @(
    "files/workshop_mods/workshop_sync_manifest.json",
    "files/workshop_mods/last_workshop_mod_clear.txt",
    "files/last_launcher_automation.txt",
    "files/current_runtime_slot.json",
    "files/current_runtime_cache.txt",
    "files/last_runtime_patch_validation.json",
    "files/mods/mod_selection.json",
    "files/mods/last_mod_launch.json",
    "files/last_manual_cloud_push.txt",
    "files/last_manual_cloud_push_blocked.txt"
)
$existingMarkerPaths = [System.Collections.Generic.List[string]]::new()
foreach ($path in $markerPaths) {
    $content = Read-RunAsFile -Path $path
    if (-not [string]::IsNullOrWhiteSpace($content)) {
        $existingMarkerPaths.Add($path)
    }
}
if ($existingMarkerPaths.Count -eq 0) {
    $existingMarkerPaths.Add("files/workshop_mods <missing>")
}
Save-Text -Path (Join-Path $diagnosticsDir "workshop-marker-files.txt") -Text ($existingMarkerPaths -join [Environment]::NewLine)
Save-RunAsFileWithHeader -OutputPath (Join-Path $diagnosticsDir "workshop-marker-contents.txt") -Paths $existingMarkerPaths

Save-RunAsArgsText -Path (Join-Path $diagnosticsDir "workshop-manifest.json") -Arguments @("cat", "files/workshop_mods/workshop_sync_manifest.json") -AllowFailure
Save-RunAsArgsText -Path (Join-Path $diagnosticsDir "workshop-clear-marker.txt") -Arguments @("cat", "files/workshop_mods/last_workshop_mod_clear.txt") -AllowFailure
Save-RunAsArgsText -Path (Join-Path $diagnosticsDir "launcher-automation-marker.txt") -Arguments @("cat", "files/last_launcher_automation.txt") -AllowFailure
Save-RunAsArgsText -Path (Join-Path $diagnosticsDir "mod-selection.json") -Arguments @("cat", "files/mods/mod_selection.json") -AllowFailure
Save-RunAsArgsText -Path (Join-Path $diagnosticsDir "last-mod-launch.json") -Arguments @("cat", "files/mods/last_mod_launch.json") -AllowFailure
if (-not [string]::IsNullOrWhiteSpace($safeRunLabel)) {
    Copy-FileIfPresent -SourcePath (Join-Path $diagnosticsDir "mod-selection.json") -DestinationPath (Join-Path $diagnosticsDir "$safeRunLabel-mod-selection.json")
    Copy-FileIfPresent -SourcePath (Join-Path $diagnosticsDir "last-mod-launch.json") -DestinationPath (Join-Path $diagnosticsDir "$safeRunLabel-last-mod-launch.json")
}
Save-RunAsArgsText -Path (Join-Path $diagnosticsDir "workshop-tree.txt") -Arguments @("find", "files/workshop_mods", "-maxdepth", "6", "-print") -AllowFailure
if ((Get-Content -Raw -LiteralPath (Join-Path $diagnosticsDir "workshop-tree.txt")) -notmatch "files/workshop_mods") {
    Save-Text -Path (Join-Path $diagnosticsDir "workshop-tree.txt") -Text "files/workshop_mods <missing>"
}

$workshopFileList = @(
    Invoke-RunAsCapture -Arguments @("find", "files/workshop_mods", "-maxdepth", "8", "-type", "f", "-print") |
        ForEach-Object { ([string]$_).Trim() } |
        Where-Object { $_ -match '\.pck$|workshop_sync_manifest\.json$|last_workshop_mod_clear\.txt$' }
)
$workshopHashes = [System.Collections.Generic.List[string]]::new()
foreach ($path in $workshopFileList) {
    $workshopHashes.Add($path)
    Add-CapturedLines -Lines $workshopHashes -Captured @(Invoke-RunAsCapture -Arguments @("sha256sum", $path))
    Add-CapturedLines -Lines $workshopHashes -Captured @(Invoke-RunAsCapture -Arguments @("ls", "-l", $path))
}
if ($workshopHashes.Count -eq 0) {
    $workshopHashes.Add("files/workshop_mods <missing>")
    $workshopHashes.Add("sha256sum <none>")
}
Save-Text -Path (Join-Path $diagnosticsDir "workshop-hashes.txt") -Text ($workshopHashes -join [Environment]::NewLine)

Save-RunAsArgsText -Path (Join-Path $diagnosticsDir "current_runtime_slot.json") -Arguments @("cat", "files/current_runtime_slot.json") -AllowFailure
Save-RunAsArgsText -Path (Join-Path $diagnosticsDir "current_runtime_cache.txt") -Arguments @("cat", "files/current_runtime_cache.txt") -AllowFailure
Save-RunAsArgsText -Path (Join-Path $diagnosticsDir "last_runtime_patch_validation.json") -Arguments @("cat", "files/last_runtime_patch_validation.json") -AllowFailure
Save-RunAsFileWithHeader -OutputPath (Join-Path $diagnosticsDir "runtime-markers.txt") -Paths @(
    "files/current_runtime_slot.json",
    "files/current_runtime_cache.txt",
    "files/last_runtime_patch_validation.json"
)

Save-RunAsArgsText -Path (Join-Path $diagnosticsDir "runtime-tree.txt") -Arguments @("find", "files/game", "files/game_versions", "files/runtime_packs", "files/.godot/mono/publish", "-maxdepth", "5", "-print") -AllowFailure
$runtimePckPatchMarkers = @(
    Invoke-RunAsCapture -Arguments @("find", "files", "-maxdepth", "9", "-type", "f", "-name", ".android_pck_patch_v*", "-print") |
        ForEach-Object { ([string]$_).Trim() } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
)
if ($runtimePckPatchMarkers.Count -gt 0) {
    Save-RunAsFileWithHeader -OutputPath (Join-Path $diagnosticsDir "runtime-pck-patch-markers.txt") -Paths $runtimePckPatchMarkers
} else {
    Save-Text -Path (Join-Path $diagnosticsDir "runtime-pck-patch-markers.txt") -Text "files/.android_pck_patch_v* <missing>"
}
$runtimeFileList = @(
    Invoke-RunAsCapture -Arguments @("find", "files", "-maxdepth", "9", "-type", "f", "-print") |
        ForEach-Object { ([string]$_).Trim() } |
        Where-Object { $_ -match 'SlayTheSpire2\.pck$|sts2\.dll$|System\.Text\.Json\.dll$|compatibility\.json$|patch_validation\.json$|\.android_patch_validation\.json$|\.android_pck_patch_v\d+$|current_runtime_slot\.json$|current_runtime_cache\.txt$|last_runtime_patch_validation\.json$' }
)
$runtimeHashes = [System.Collections.Generic.List[string]]::new()
foreach ($path in $runtimeFileList) {
    $runtimeHashes.Add($path)
    Add-CapturedLines -Lines $runtimeHashes -Captured @(Invoke-RunAsCapture -Arguments @("sha256sum", $path))
    Add-CapturedLines -Lines $runtimeHashes -Captured @(Invoke-RunAsCapture -Arguments @("ls", "-l", $path))
}
Save-Text -Path (Join-Path $diagnosticsDir "runtime-hashes.txt") -Text ($runtimeHashes -join [Environment]::NewLine)

$runtimeCacheText = Get-Content -Raw -LiteralPath (Join-Path $diagnosticsDir "current_runtime_cache.txt")
$runtimePackDir = [regex]::Match($runtimeCacheText, '(?m)^Runtime pack directory:\s*(.+?)\s*$').Groups[1].Value.Trim()
if (-not [string]::IsNullOrWhiteSpace($runtimePackDir) -and $runtimePackDir -ne "<none>" -and $runtimePackDir -ne "<missing>") {
    Save-RunAsArgsText -Path (Join-Path $diagnosticsDir "selected_runtime_pack_compatibility.json") -Arguments @("cat", "$runtimePackDir/compatibility.json") -AllowFailure
    Save-RunAsArgsText -Path (Join-Path $diagnosticsDir "selected_runtime_pack_patch_validation.json") -Arguments @("cat", "$runtimePackDir/patch_validation.json") -AllowFailure
} else {
    Save-Text -Path (Join-Path $diagnosticsDir "selected_runtime_pack_compatibility.json") -Text ""
    Save-Text -Path (Join-Path $diagnosticsDir "selected_runtime_pack_patch_validation.json") -Text ""
}

Save-RunAsFileWithHeader -OutputPath (Join-Path $diagnosticsDir "cloud-push-markers.txt") -Paths @(
    "files/last_manual_cloud_push.txt",
    "files/last_manual_cloud_push_blocked.txt"
)

$focusedPatterns = "Workshop|workshop|ModLoader|Loaded mod|Android mod scan|Play Vanilla|launcher-selected|Skipping disabled|selected mods|\[Mods\]|staged|missingDeps|Missing dependency|Manual Push blocked|Steam Cloud|Selected Steam branch|Selected game PCK|Loading PCK from|Runtime slot evidence|Runtime pack|runtime patch validation|Patch orchestration|Assembly cache|Exception thrown when calling mod initializer|MissingMethodException|JsonPropertyInfoValues|AndroidRuntime|FATAL EXCEPTION|signal "
$logcat = Invoke-AndroidAdbCapture -AdbPath $AdbPath -DeviceSerial $DeviceSerial -Arguments @("logcat", "-d", "-v", "time")
$filtered = @($logcat | Select-String -Pattern $focusedPatterns | ForEach-Object { $_.Line })
Save-Text -Path (Join-Path $logsDir "logcat-workshop-filtered.txt") -Text ($filtered -join [Environment]::NewLine)
if (-not [string]::IsNullOrWhiteSpace($safeRunLabel)) {
    Save-Text -Path (Join-Path $logsDir "$safeRunLabel-focused.txt") -Text ($filtered -join [Environment]::NewLine)
}
if ($IncludeRawLogcat) {
    Save-Text -Path (Join-Path $logsDir "logcat-full.txt") -Text ($logcat -join [Environment]::NewLine)
}

$manifestText = Get-Content -Raw -LiteralPath (Join-Path $diagnosticsDir "workshop-manifest.json")
$clearText = Get-Content -Raw -LiteralPath (Join-Path $diagnosticsDir "workshop-clear-marker.txt")
$hashText = Get-Content -Raw -LiteralPath (Join-Path $diagnosticsDir "workshop-hashes.txt")
$runtimeHashText = Get-Content -Raw -LiteralPath (Join-Path $diagnosticsDir "runtime-hashes.txt")
$cloudPushText = Get-Content -Raw -LiteralPath (Join-Path $diagnosticsDir "cloud-push-markers.txt")
$runtimeText = Get-Content -Raw -LiteralPath (Join-Path $diagnosticsDir "runtime-markers.txt")
$modLaunchText = Get-Content -Raw -LiteralPath (Join-Path $diagnosticsDir "last-mod-launch.json")
$modSelectionText = Get-Content -Raw -LiteralPath (Join-Path $diagnosticsDir "mod-selection.json")

$manifestActivePckCount = 0
try {
    $manifest = $manifestText | ConvertFrom-Json
    $manifestItems = @($manifest.Items)
    $manifestActivePckCount = @(
        $manifestItems | Where-Object {
            $_.Status -eq "staged" -and $_.HasPck -eq $true
        }
    ).Count
} catch {
    $manifestActivePckCount = -1
}

$rawStagedPckPaths = @(
    [regex]::Matches($hashText, 'files/workshop_mods/staged/\S+?\.pck') |
        ForEach-Object { $_.Value } |
        Sort-Object -Unique
)
$rawStagedPckCount = $rawStagedPckPaths.Count
$workshopCloudPushLocked = [Math]::Max($manifestActivePckCount, $rawStagedPckCount) -gt 0
$derivedState = [ordered]@{
    manifestActivePckCount = $manifestActivePckCount
    rawStagedPckCount = $rawStagedPckCount
    workshopCloudPushLocked = $workshopCloudPushLocked
    steamCloudPushPerformed = $false
    source = "capture-workshop-mod-evidence.ps1"
}
Save-Text -Path (Join-Path $diagnosticsDir "workshop-derived-state.json") -Text ($derivedState | ConvertTo-Json -Depth 5)

$summary = [System.Collections.Generic.List[string]]::new()
$summary.Add("# Workshop mod evidence summary")
$summary.Add("")
$summary.Add("Generated UTC: $generatedUtc")
$summary.Add("Phase: $Phase")
$summary.Add("Package: $PackageName")
$summary.Add("Device: $DeviceSerial")
$summary.Add("Output: $outputDir")
$summary.Add("Collector boundary: This collector does not press Steam Cloud Push and does not upload save data.")
$summary.Add("")
$summary.Add("| Check | Observed | Evidence |")
$summary.Add("| --- | --- | --- |")
$summary.Add("| Workshop manifest present | $($manifestText -match 'PublishedFileId|Items|Version') | diagnostics/workshop-manifest.json |")
$summary.Add("| Workshop clear marker present | $($clearText -match 'clearedAtUtc=') | diagnostics/workshop-clear-marker.txt |")
$summary.Add("| Clear marker says Steam Cloud Push was not performed | $($clearText -match 'steamCloudPushPerformed=false') | diagnostics/workshop-clear-marker.txt |")
$summary.Add("| Staged Workshop PCK hash captured | $($hashText -match '\.pck') | diagnostics/workshop-hashes.txt |")
$summary.Add("| Subscription query evidence captured | $($manifestText -match 'SubscriptionQueryType|SubscriptionQueryAttempts') | diagnostics/workshop-manifest.json |")
$summary.Add("| Missing dependency evidence captured | $($manifestText -match 'MissingDependencyIds|MissingDependencyItemCount') | diagnostics/workshop-manifest.json |")
$summary.Add("| Download source/update provenance captured | $($manifestText -match 'DownloadSourceKind|DownloadUrlPresent|DownloadUrlHost|ExpectedDownloadBytes|HContentFile|ReusedCachedDownload') | diagnostics/workshop-manifest.json |")
$summary.Add("| Raw signed Workshop download URL omitted | $($manifestText -notmatch '""DownloadUrl""\s*:') | diagnostics/workshop-manifest.json |")
$summary.Add("| Stale Workshop download temp artifacts absent | $((Get-Content -Raw -LiteralPath (Join-Path $diagnosticsDir 'workshop-tree.txt')) -notmatch 'files/workshop_mods/downloads/.+(\.download|\.tmp-|\.old-)') | diagnostics/workshop-tree.txt |")
$summary.Add("| Derived Workshop Cloud Push lock state captured | $workshopCloudPushLocked | diagnostics/workshop-derived-state.json |")
$summary.Add("| Cloud Push marker captured for safety review | $($cloudPushText -match 'last_manual_cloud_push') | diagnostics/cloud-push-markers.txt |")
$summary.Add("| Mod selector launch marker captured | $($modLaunchText -match 'playMode|selectedMods|workshopModdedSaveCloudPushLocked') | diagnostics/last-mod-launch.json |")
$summary.Add("| Mod selector selection marker captured | $($modSelectionText -match 'PlayMode|EnabledMods') | diagnostics/mod-selection.json |")
$summary.Add("| Runtime marker captured for branch/non-public review | $($runtimeText -match 'current_runtime_slot|Runtime ID|selectedBranch|Selected branch') | diagnostics/runtime-markers.txt |")
$summary.Add("| Runtime selected PCK / active sts2.dll hashes captured | $($runtimeHashText -match 'SlayTheSpire2\.pck|sts2\.dll') | diagnostics/runtime-hashes.txt |")
$summary.Add("| Focused Workshop logcat captured | $($filtered.Count -gt 0) | logs/logcat-workshop-filtered.txt |")
$summary.Add("| Start Game requested from launcher | $([bool]$StartGame) | run-metadata.json |")
$summary.Add("")
$summary.Add("Phase guidance:")
$summary.Add("- no-mods: require `last_workshop_mod_clear.txt`, zero active staged PCK mods in diagnostics, and launch evidence.")
$summary.Add("- simple: require one staged Workshop PCK mod with item ID/path/hash, sanitized source provenance, derived Cloud Push lock, and launch log evidence that the Workshop staged mod root was scanned.")
$summary.Add("- dependency: require staged dependency item(s), required-by parent IDs, no duplicate shared dependency staging, derived Cloud Push lock, and launch log evidence that the Workshop staged mod root was scanned.")
$summary.Add("- broken: require missing dependency IDs, failed/unsupported item status, or `staged-no-pck` no-PCK content without treating sync as clean.")
$summary.Add("- public/public-beta/core-release: require selected branch, selected PCK path/hash, active sts2.dll hash, runtime cache marker, runtime patch validation marker, and matching launch log evidence in this artifact.")
Save-Text -Path (Join-Path $outputDir "summary.md") -Text ($summary -join [Environment]::NewLine)

Write-Host "Workshop mod evidence captured: $outputDir"
Write-Host "Phase: $Phase"
