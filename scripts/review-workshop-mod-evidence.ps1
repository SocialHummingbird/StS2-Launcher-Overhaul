param(
    [Parameter(Mandatory = $true)]
    [string]$EvidenceDir,
    [ValidateSet("", "no-mods", "simple", "dependency", "broken", "public", "public-beta", "core-release")]
    [string]$RequirePhase = "",
    [switch]$RequireCachedDownloadReuse,
    [switch]$RequireScreenshot,
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"

$resolvedEvidenceDir = (Resolve-Path -LiteralPath $EvidenceDir).ProviderPath
$failures = [System.Collections.Generic.List[string]]::new()
$passes = 0

function Join-EvidencePath([string]$RelativePath) {
    $normalized = $RelativePath -replace '[\\/]', [System.IO.Path]::DirectorySeparatorChar
    return Join-Path $resolvedEvidenceDir $normalized
}

function Read-EvidenceFile([string]$RelativePath) {
    $path = Join-EvidencePath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("Missing evidence file: $RelativePath")
        return $null
    }

    return Get-Content -LiteralPath $path -Raw
}

function Add-Pass([string]$Message) {
    $script:passes += 1
    if (-not $Quiet) {
        Write-Host "PASS $Message"
    }
}

function Require-FileExists([string]$RelativePath, [string]$Description) {
    $path = Join-EvidencePath $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        $failures.Add("$RelativePath - $Description - file is missing")
        return
    }

    Add-Pass "$RelativePath - $Description"
}

function Require-Pattern([string]$RelativePath, [string]$Description, [string]$Pattern) {
    $content = Read-EvidenceFile $RelativePath
    if ($null -eq $content) {
        return
    }

    if ($content -notmatch $Pattern) {
        $failures.Add("$RelativePath - $Description - missing pattern: $Pattern")
        return
    }

    Add-Pass "$RelativePath - $Description"
}

function Require-NoPattern([string]$RelativePath, [string]$Description, [string]$Pattern) {
    $content = Read-EvidenceFile $RelativePath
    if ($null -eq $content) {
        return
    }

    if ($content -match $Pattern) {
        $failures.Add("$RelativePath - $Description - forbidden pattern present: $Pattern")
        return
    }

    Add-Pass "$RelativePath - $Description"
}

function Require-AnyPattern([string]$RelativePath, [string]$Description, [string[]]$Patterns) {
    $content = Read-EvidenceFile $RelativePath
    if ($null -eq $content) {
        return
    }

    foreach ($pattern in $Patterns) {
        if ($content -match $pattern) {
            Add-Pass "$RelativePath - $Description"
            return
        }
    }

    $failures.Add("$RelativePath - $Description - none matched: $($Patterns -join ' OR ')")
}

function Require-JsonPattern([string]$RelativePath, [string]$Description, [string]$Pattern) {
    $content = Read-EvidenceFile $RelativePath
    if ($null -eq $content) {
        return
    }

    try {
        $null = $content | ConvertFrom-Json
    } catch {
        $failures.Add("$RelativePath - $Description - invalid JSON: $($_.Exception.Message)")
        return
    }

    if ($content -notmatch $Pattern) {
        $failures.Add("$RelativePath - $Description - missing JSON pattern: $Pattern")
        return
    }

    Add-Pass "$RelativePath - $Description"
}

function Read-JsonEvidence([string]$RelativePath, [string]$Description) {
    $content = Read-EvidenceFile $RelativePath
    if ($null -eq $content) {
        return $null
    }

    try {
        return $content | ConvertFrom-Json
    } catch {
        $failures.Add("$RelativePath - $Description - invalid JSON: $($_.Exception.Message)")
        return $null
    }
}

function Read-JsonPropertyValue([string]$RelativePath, [string]$PropertyName, [string]$Description) {
    $json = Read-JsonEvidence $RelativePath $Description
    if ($null -eq $json) {
        return $null
    }

    $property = $json.PSObject.Properties[$PropertyName]
    if ($null -eq $property -or [string]::IsNullOrWhiteSpace("$($property.Value)")) {
        $failures.Add("$RelativePath - $Description - missing JSON property: $PropertyName")
        return $null
    }

    return "$($property.Value)".Trim()
}

function Read-MarkerValue([string]$RelativePath, [string]$Prefix, [string]$Description) {
    $content = Read-EvidenceFile $RelativePath
    if ($null -eq $content) {
        return $null
    }

    foreach ($line in ($content -split "`r?`n")) {
        if ($line.StartsWith($Prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            $value = $line.Substring($Prefix.Length).Trim()
            if ([string]::IsNullOrWhiteSpace($value)) {
                $failures.Add("$RelativePath - $Description - marker value is empty: $Prefix")
                return $null
            }

            return $value
        }
    }

    $failures.Add("$RelativePath - $Description - missing marker prefix: $Prefix")
    return $null
}

function Require-JsonMarkerMatch(
    [string]$JsonPath,
    [string]$JsonProperty,
    [string]$MarkerPath,
    [string]$MarkerPrefix,
    [string]$Description
) {
    $jsonValue = Read-JsonPropertyValue $JsonPath $JsonProperty $Description
    $markerValue = Read-MarkerValue $MarkerPath $MarkerPrefix $Description
    if ($null -eq $jsonValue -or $null -eq $markerValue) {
        return
    }

    if ($jsonValue -ne $markerValue) {
        $failures.Add("$Description - mismatch: $JsonPath.$JsonProperty=$jsonValue; $MarkerPath marker $MarkerPrefix $markerValue")
        return
    }

    Add-Pass $Description
}

function Require-JsonPropertyMatch(
    [string]$LeftPath,
    [string]$LeftProperty,
    [string]$RightPath,
    [string]$RightProperty,
    [string]$Description
) {
    $leftValue = Read-JsonPropertyValue $LeftPath $LeftProperty $Description
    $rightValue = Read-JsonPropertyValue $RightPath $RightProperty $Description
    if ($null -eq $leftValue -or $null -eq $rightValue) {
        return
    }

    if ($leftValue -ne $rightValue) {
        $failures.Add("$Description - mismatch: $LeftPath.$LeftProperty=$leftValue; $RightPath.$RightProperty=$rightValue")
        return
    }

    Add-Pass $Description
}

function Read-RuntimeHashForPathPattern([string]$PathPattern, [string]$Description) {
    $content = Read-EvidenceFile "diagnostics/runtime-hashes.txt"
    if ($null -eq $content) {
        return $null
    }

    foreach ($line in ($content -split "`r?`n")) {
        $match = [regex]::Match($line, '^(?<hash>[a-fA-F0-9]{64})\s+(?<path>.+?)\s*$')
        if ($match.Success -and $match.Groups["path"].Value -match $PathPattern) {
            return $match.Groups["hash"].Value.ToLowerInvariant()
        }
    }

    $failures.Add("diagnostics/runtime-hashes.txt - $Description - missing SHA256 for path pattern: $PathPattern")
    return $null
}

function Read-PckPatchMarkers {
    $content = Read-EvidenceFile "diagnostics/runtime-pck-patch-markers.txt"
    if ($null -eq $content) {
        return @()
    }

    $markers = [System.Collections.Generic.List[object]]::new()
    $currentPath = ""
    $currentLines = [System.Collections.Generic.List[string]]::new()
    foreach ($line in ($content -split "`r?`n")) {
        if ($line.StartsWith("===== ", [System.StringComparison]::Ordinal)) {
            if (-not [string]::IsNullOrWhiteSpace($currentPath) -and $currentLines.Count -gt 0) {
                $markers.Add([pscustomobject]@{
                    Path = $currentPath
                    Text = ($currentLines -join [Environment]::NewLine)
                })
            }

            $currentPath = $line.Substring(6).Trim()
            $currentLines.Clear()
            continue
        }

        if (-not [string]::IsNullOrWhiteSpace($currentPath)) {
            $currentLines.Add($line)
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($currentPath) -and $currentLines.Count -gt 0) {
        $markers.Add([pscustomobject]@{
            Path = $currentPath
            Text = ($currentLines -join [Environment]::NewLine)
        })
    }

    return @($markers)
}

function Read-PckPatchMarkerForPathPattern([string]$PathPattern, [string]$Description) {
    foreach ($marker in Read-PckPatchMarkers) {
        if ($marker.Path -match $PathPattern) {
            try {
                return $marker.Text | ConvertFrom-Json
            } catch {
                $failures.Add("diagnostics/runtime-pck-patch-markers.txt - $Description - invalid JSON for $($marker.Path): $($_.Exception.Message)")
                return $null
            }
        }
    }

    $failures.Add("diagnostics/runtime-pck-patch-markers.txt - $Description - missing marker for path pattern: $PathPattern")
    return $null
}

function Require-SelectedPckHashEvidence([string]$PhaseLabel, [string]$SelectedPckPathPattern, [string]$PatchMarkerPathPattern) {
    $cacheHash = Read-MarkerValue "diagnostics/current_runtime_cache.txt" "Selected PCK SHA256:" "$PhaseLabel selected PCK hash evidence"
    $actualHash = Read-RuntimeHashForPathPattern $SelectedPckPathPattern "$PhaseLabel selected mounted PCK hash"
    if ($null -eq $cacheHash -or $null -eq $actualHash) {
        return
    }

    $cacheHash = $cacheHash.ToLowerInvariant()
    $actualHash = $actualHash.ToLowerInvariant()
    if ($cacheHash -eq $actualHash) {
        Add-Pass "$PhaseLabel runtime cache selected PCK hash matches mounted PCK file"
        return
    }

    $marker = Read-PckPatchMarkerForPathPattern $PatchMarkerPathPattern "$PhaseLabel Android PCK patch marker explains source/mounted hash pair"
    if ($null -eq $marker) {
        return
    }

    $sourceHash = "$($marker.sourcePckSha256)".Trim().ToLowerInvariant()
    $androidHash = "$($marker.androidPckSha256)".Trim().ToLowerInvariant()
    if ([string]::IsNullOrWhiteSpace($sourceHash) -or [string]::IsNullOrWhiteSpace($androidHash)) {
        $failures.Add("diagnostics/runtime-pck-patch-markers.txt - $PhaseLabel Android PCK patch marker lacks sourcePckSha256/androidPckSha256")
        return
    }

    if ($sourceHash -ne $cacheHash -or $androidHash -ne $actualHash) {
        $failures.Add("$PhaseLabel selected PCK hash mismatch: cache/source=$cacheHash markerSource=$sourceHash mounted=$actualHash markerAndroid=$androidHash")
        return
    }

    Add-Pass "$PhaseLabel runtime cache source PCK hash matches mounted Android PCK via patch marker"
}

function Require-WorkshopUsableSourceEvidence([string]$PhaseLabel) {
    Require-JsonPattern "diagnostics/workshop-manifest.json" "$PhaseLabel item has usable download source kind" '(?i)"DownloadSourceKind"\s*:\s*"(direct-url|ugc-hcontent|depot-manifest)"'
    Require-JsonPattern "diagnostics/workshop-manifest.json" "$PhaseLabel item records expected download bytes field" '(?i)"ExpectedDownloadBytes"\s*:\s*[0-9]+'
    Require-JsonPattern "diagnostics/workshop-manifest.json" "$PhaseLabel item records UGC content handle field" '(?i)"HContentFile"\s*:\s*[0-9]+'
    Require-JsonPattern "diagnostics/workshop-manifest.json" "$PhaseLabel item records fresh-vs-cached update state" '(?i)"ReusedCachedDownload"\s*:\s*(true|false)'
    Require-JsonPattern "diagnostics/workshop-manifest.json" "$PhaseLabel item records URL-backed or depot-manifest source provenance" '(?is)(("DownloadSourceKind"\s*:\s*"(direct-url|ugc-hcontent)".*?"DownloadUrlPresent"\s*:\s*true.*?"DownloadUrlHost"\s*:\s*"[^"]+")|("DownloadUrlPresent"\s*:\s*true.*?"DownloadUrlHost"\s*:\s*"[^"]+".*?"DownloadSourceKind"\s*:\s*"(direct-url|ugc-hcontent)")|("DownloadSourceKind"\s*:\s*"depot-manifest".*?"ManifestId"\s*:\s*[1-9][0-9]*)|("ManifestId"\s*:\s*[1-9][0-9]*.*?"DownloadSourceKind"\s*:\s*"depot-manifest"))'
}

function Require-LaunchEvidence([string]$PhaseLabel) {
    Require-JsonPattern "run-metadata.json" "$PhaseLabel capture launched the app" '(?i)"launchRequested"\s*:\s*true'
    Require-Pattern "logs/logcat-workshop-filtered.txt" "$PhaseLabel launch/runtime log is captured" "Loading PCK from|Selected Steam branch|Runtime|Workshop|ModLoader"
    Require-NoPattern "logs/logcat-workshop-filtered.txt" "$PhaseLabel launch avoided fallback/crash signatures" "(?i)NativeFallback|FATAL EXCEPTION|AndroidRuntime.*FATAL|SIGSEGV|signal 11|Unhandled exception"
    Require-NoPattern "logs/logcat-workshop-filtered.txt" "$PhaseLabel launch avoided mod initializer errors" "(?i)Exception thrown when calling mod initializer|MissingMethodException|JsonPropertyInfoValues"
}

function Require-WorkshopModLoaderScanEvidence([string]$PhaseLabel) {
    Require-Pattern "logs/logcat-workshop-filtered.txt" "$PhaseLabel scanned the Android Workshop mod root" "Scanning Workshop staged mods|Workshop staged mods|ModLoader|Loaded mod"
}

function Require-WorkshopLoadedModEvidence([string]$PhaseLabel) {
    Require-JsonPattern "diagnostics/workshop-manifest.json" "$PhaseLabel Workshop manifest is readable" "\{"
    Require-JsonPattern "diagnostics/workshop-manifest.json" "$PhaseLabel Workshop manifest has staged item" '(?i)"Status"\s*:\s*"staged"'
    Require-WorkshopUsableSourceEvidence "$PhaseLabel Workshop mod"
    Require-Pattern "diagnostics/workshop-hashes.txt" "$PhaseLabel Workshop PCK hash is captured" "\.pck"
    Require-JsonPattern "diagnostics/workshop-derived-state.json" "$PhaseLabel Workshop derived state has active manifest PCK" '(?i)"manifestActivePckCount"\s*:\s*[1-9]'
    Require-JsonPattern "diagnostics/workshop-derived-state.json" "$PhaseLabel Workshop derived state has raw staged PCK" '(?i)"rawStagedPckCount"\s*:\s*[1-9]'
    Require-JsonPattern "diagnostics/workshop-derived-state.json" "$PhaseLabel Workshop derived state locks Cloud Push" '(?i)"workshopCloudPushLocked"\s*:\s*true'
    Require-WorkshopModLoaderScanEvidence "$PhaseLabel Workshop mod"
}

function Require-NonPublicRuntimeEvidence([string]$PhaseLabel) {
    $escapedPhase = [regex]::Escape($PhaseLabel)
    Require-Pattern "diagnostics/runtime-markers.txt" "$PhaseLabel runtime marker is captured" $escapedPhase
    Require-JsonPattern "diagnostics/current_runtime_slot.json" "$PhaseLabel runtime slot marker is readable" "\{"
    Require-JsonPattern "diagnostics/current_runtime_slot.json" "$PhaseLabel runtime slot is selected" $escapedPhase
    Require-JsonPattern "diagnostics/current_runtime_slot.json" "$PhaseLabel runtime slot is playable" '(?i)"playable"\s*:\s*true'
    Require-JsonPattern "diagnostics/current_runtime_slot.json" "$PhaseLabel runtime slot files are ready" '(?i)"filesReady"\s*:\s*true'
    Require-JsonPattern "diagnostics/current_runtime_slot.json" "$PhaseLabel runtime slot is runtime-compatible" '(?i)"runtimeCompatible"\s*:\s*true'
    Require-JsonPattern "diagnostics/current_runtime_slot.json" "$PhaseLabel runtime slot is patch-compatible" '(?i)"patchCompatible"\s*:\s*true'
    Require-Pattern "diagnostics/current_runtime_cache.txt" "$PhaseLabel runtime cache marker names selected branch" "(?m)^Selected branch:\s*$escapedPhase\s*$"
    Require-Pattern "diagnostics/current_runtime_cache.txt" "$PhaseLabel runtime cache marker names selected PCK path" "Selected PCK path:.*game_versions[/\\]$escapedPhase-"
    Require-Pattern "diagnostics/current_runtime_cache.txt" "$PhaseLabel runtime cache marker records selected PCK hash" "Selected PCK SHA256:"
    Require-Pattern "diagnostics/current_runtime_cache.txt" "$PhaseLabel runtime cache marker records active sts2.dll hash" "Publish cache active sts2\.dll SHA256:"
    Require-Pattern "diagnostics/runtime-hashes.txt" "$PhaseLabel runtime hashes include selected PCK" "game_versions[/\\]$escapedPhase-.+SlayTheSpire2\.pck"
    Require-Pattern "diagnostics/runtime-hashes.txt" "$PhaseLabel runtime hashes include active sts2.dll" "sts2\.dll"
    Require-SelectedPckHashEvidence `
        $PhaseLabel `
        "game_versions[/\\]$escapedPhase-.+SlayTheSpire2\.pck" `
        "game_versions[/\\]$escapedPhase-.+[/\\]game[/\\]\.android_pck_patch_v\d+"
    Require-JsonPattern "diagnostics/last_runtime_patch_validation.json" "$PhaseLabel runtime patch validation marker is readable" "\{"
    Require-JsonPattern "diagnostics/last_runtime_patch_validation.json" "$PhaseLabel runtime patch validation selected branch matches" "(?i)`"selectedBranch`"\s*:\s*`"$escapedPhase`""
    Require-JsonPattern "diagnostics/last_runtime_patch_validation.json" "$PhaseLabel runtime patch validation passed" '(?i)"status"\s*:\s*"passed"'
    Require-JsonMarkerMatch "diagnostics/last_runtime_patch_validation.json" "selectedPckSha256" "diagnostics/current_runtime_cache.txt" "Selected PCK SHA256:" "$PhaseLabel runtime validation selected PCK hash matches cache marker"
    Require-JsonMarkerMatch "diagnostics/last_runtime_patch_validation.json" "selectedSourceAssemblySha256" "diagnostics/current_runtime_cache.txt" "Selected source sts2.dll SHA256:" "$PhaseLabel runtime validation selected source sts2.dll hash matches cache marker"
    Require-JsonMarkerMatch "diagnostics/last_runtime_patch_validation.json" "activeAndroidAssemblySha256" "diagnostics/current_runtime_cache.txt" "Publish cache active sts2.dll SHA256:" "$PhaseLabel runtime validation active Android sts2.dll hash matches cache marker"
    Require-JsonPattern "diagnostics/selected_runtime_pack_compatibility.json" "$PhaseLabel selected runtime pack manifest is readable" "\{"
    Require-JsonPattern "diagnostics/selected_runtime_pack_compatibility.json" "$PhaseLabel runtime pack was generated cleanly" '(?i)"generatedFromCleanDirectory"\s*:\s*true'
    Require-JsonPattern "diagnostics/selected_runtime_pack_compatibility.json" "$PhaseLabel runtime pack declares support assemblies" '(?i)"supportAssemblies"\s*:'
    Require-JsonPattern "diagnostics/selected_runtime_pack_compatibility.json" "$PhaseLabel runtime pack declares support assembly hashes" '(?i)"supportAssemblySha256"\s*:'
    Require-JsonPropertyMatch "diagnostics/selected_runtime_pack_compatibility.json" "packId" "diagnostics/last_runtime_patch_validation.json" "runtimePackId" "$PhaseLabel runtime pack manifest ID matches runtime validation"
    Require-JsonPropertyMatch "diagnostics/selected_runtime_pack_compatibility.json" "sourceRuntimeSlotId" "diagnostics/last_runtime_patch_validation.json" "runtimeSlotId" "$PhaseLabel runtime pack source slot matches runtime validation slot"
    Require-JsonPropertyMatch "diagnostics/selected_runtime_pack_compatibility.json" "sourceAssemblySha256" "diagnostics/last_runtime_patch_validation.json" "selectedSourceAssemblySha256" "$PhaseLabel runtime pack source sts2.dll hash matches runtime validation"
    Require-JsonPropertyMatch "diagnostics/selected_runtime_pack_compatibility.json" "androidAssemblySha256" "diagnostics/last_runtime_patch_validation.json" "activeAndroidAssemblySha256" "$PhaseLabel runtime pack Android sts2.dll hash matches runtime validation"
    Require-JsonPattern "diagnostics/selected_runtime_pack_patch_validation.json" "$PhaseLabel selected runtime pack validation is readable" "\{"
    Require-JsonPropertyMatch "diagnostics/selected_runtime_pack_patch_validation.json" "runtimePackId" "diagnostics/selected_runtime_pack_compatibility.json" "packId" "$PhaseLabel runtime pack validation report ID matches manifest"
    Require-AnyPattern "diagnostics/selected_runtime_pack_patch_validation.json" "$PhaseLabel selected runtime pack validation passed" @(
        '(?i)"patchValidationStatus"\s*:\s*"passed"',
        '(?i)"status"\s*:\s*"passed"'
    )
    Require-Pattern "logs/logcat-workshop-filtered.txt" "$PhaseLabel launch/runtime log is captured" "Loading PCK from|$escapedPhase|Runtime"
    Require-Pattern "logs/logcat-workshop-filtered.txt" "$PhaseLabel launch loaded selected non-public PCK path" "Loading PCK from:.*game_versions[/\\]$escapedPhase-.+SlayTheSpire2\.pck"
}

Require-Pattern "summary.md" "has Workshop evidence summary" "Workshop mod evidence summary"
Require-Pattern "summary.md" "states Steam Cloud Push safety boundary" "does not press Steam Cloud Push"
Require-JsonPattern "run-metadata.json" "has readable run metadata" "\{"
Require-JsonPattern "run-metadata.json" "metadata identifies collector" '(?i)"collector"\s*:\s*"capture-workshop-mod-evidence\.ps1"'
Require-JsonPattern "run-metadata.json" "metadata records Steam Cloud read-only boundary" '(?i)"readOnlySteamCloud"\s*:\s*true'
Require-Pattern "ARTIFACT_HYGIENE.txt" "has artifact hygiene warning" "Raw full logcat is omitted by default"
Require-Pattern "diagnostics/workshop-marker-files.txt" "has Workshop marker file listing" "workshop_mods"
Require-Pattern "diagnostics/workshop-marker-contents.txt" "has Workshop marker contents capture" "workshop_mods"
Require-Pattern "diagnostics/workshop-tree.txt" "has Workshop tree capture" "workshop_mods"
Require-NoPattern "diagnostics/workshop-tree.txt" "has no stale Workshop download temp artifacts" 'files/workshop_mods/downloads/.+(\.download|\.tmp-|\.old-)'
Require-Pattern "diagnostics/workshop-hashes.txt" "has Workshop hash capture" "sha256sum|workshop_mods"
Require-JsonPattern "diagnostics/workshop-derived-state.json" "has derived Workshop safety state" "\{"
Require-JsonPattern "diagnostics/workshop-derived-state.json" "derived state proves Steam Cloud Push was not performed" '(?i)"steamCloudPushPerformed"\s*:\s*false'
Require-NoPattern "diagnostics/workshop-manifest.json" "omits raw Workshop download URLs" '(?i)"DownloadUrl"\s*:'
Require-Pattern "diagnostics/cloud-push-markers.txt" "captures Cloud Push markers for safety review" "last_manual_cloud_push"
Require-Pattern "diagnostics/runtime-markers.txt" "captures runtime markers for branch review" "current_runtime_slot|current_runtime_cache"
Require-FileExists "diagnostics/runtime-hashes.txt" "has selected runtime hash capture"
Require-FileExists "diagnostics/runtime-pck-patch-markers.txt" "has Android PCK patch marker capture"
Require-FileExists "logs/logcat-workshop-filtered.txt" "has focused Workshop logcat file"

foreach ($relativePath in @(
    "diagnostics/workshop-marker-files.txt",
    "diagnostics/workshop-marker-contents.txt",
    "diagnostics/workshop-tree.txt",
    "diagnostics/workshop-hashes.txt",
    "diagnostics/cloud-push-markers.txt",
    "diagnostics/runtime-markers.txt",
    "diagnostics/runtime-hashes.txt",
    "diagnostics/runtime-pck-patch-markers.txt",
    "diagnostics/current_runtime_slot.json",
    "diagnostics/current_runtime_cache.txt",
    "diagnostics/last_runtime_patch_validation.json"
)) {
    Require-NoPattern $relativePath "capture succeeded without adb/run-as failure marker" "CAPTURE_FAILED|run-as:|Permission denied|not debuggable"
}

if ($RequireScreenshot) {
    $screenshotDir = Join-EvidencePath "screenshots"
    $screenshots = @(Get-ChildItem -LiteralPath $screenshotDir -Filter "*.png" -ErrorAction SilentlyContinue)
    if ($screenshots.Count -eq 0) {
        $failures.Add("screenshots - required screenshot PNG is missing")
    } else {
        Add-Pass "screenshots - screenshot PNG present"
    }
}

if ($RequireCachedDownloadReuse) {
    Require-JsonPattern "diagnostics/workshop-manifest.json" "cached Workshop download reuse is recorded" '(?i)"ReusedCachedDownload"\s*:\s*true'
    Require-Pattern "logs/logcat-workshop-filtered.txt" "cached Workshop download reuse is logged" "Using cached Workshop download"
}

if (-not [string]::IsNullOrWhiteSpace($RequirePhase)) {
    Require-JsonPattern "run-metadata.json" "metadata phase matches required phase" "(?i)`"phase`"\s*:\s*`"$([regex]::Escape($RequirePhase))`""
    Require-LaunchEvidence $RequirePhase

    switch ($RequirePhase) {
        "no-mods" {
            Require-Pattern "diagnostics/workshop-clear-marker.txt" "has no-mods clear marker" "clearedAtUtc="
            Require-Pattern "diagnostics/workshop-clear-marker.txt" "clear marker proves no Steam Cloud Push" "steamCloudPushPerformed=false"
            Require-Pattern "diagnostics/workshop-clear-marker.txt" "clear marker preserves downloads" "downloadsPreserved=true"
            Require-Pattern "diagnostics/workshop-clear-marker.txt" "clear marker records removed staged directories" "removedStagedDirectoryCount="
            Require-Pattern "diagnostics/workshop-clear-marker.txt" "clear marker records removed staged root files" "removedStagedRootFileCount="
            Require-JsonPattern "diagnostics/workshop-manifest.json" "manifest is readable after clear" "\{"
            Require-JsonPattern "diagnostics/workshop-manifest.json" "manifest is empty after clear" '(?i)"Items"\s*:\s*\[\s*\]'
            Require-NoPattern "diagnostics/workshop-hashes.txt" "no staged Workshop PCK remains after clear" "files/workshop_mods/staged/.+\.pck"
            Require-JsonPattern "diagnostics/workshop-derived-state.json" "no-mods derived state has zero raw staged PCK files" '(?i)"rawStagedPckCount"\s*:\s*0'
            Require-JsonPattern "diagnostics/workshop-derived-state.json" "no-mods derived state leaves Workshop Cloud Push unlocked" '(?i)"workshopCloudPushLocked"\s*:\s*false'
        }
        "simple" {
            Require-JsonPattern "diagnostics/workshop-manifest.json" "simple mod manifest is readable" "\{"
            Require-JsonPattern "diagnostics/workshop-manifest.json" "simple mod has a staged item" '(?i)"Status"\s*:\s*"staged"'
            Require-WorkshopUsableSourceEvidence "simple mod"
            Require-Pattern "diagnostics/workshop-hashes.txt" "simple mod has staged PCK hash" "\.pck"
            Require-JsonPattern "diagnostics/workshop-derived-state.json" "simple mod derived state has active manifest PCK" '(?i)"manifestActivePckCount"\s*:\s*[1-9]'
            Require-JsonPattern "diagnostics/workshop-derived-state.json" "simple mod derived state has raw staged PCK" '(?i)"rawStagedPckCount"\s*:\s*[1-9]'
            Require-JsonPattern "diagnostics/workshop-derived-state.json" "simple mod derived state locks Workshop Cloud Push" '(?i)"workshopCloudPushLocked"\s*:\s*true'
            Require-WorkshopModLoaderScanEvidence "simple mod"
        }
        "dependency" {
            Require-JsonPattern "diagnostics/workshop-manifest.json" "dependency manifest is readable" "\{"
            Require-JsonPattern "diagnostics/workshop-manifest.json" "dependency manifest records discovered dependency count" '(?i)"DependencyItemCount"\s*:\s*[1-9]'
            Require-JsonPattern "diagnostics/workshop-manifest.json" "dependency item evidence is present" '(?i)"IsDependency"\s*:\s*true'
            Require-JsonPattern "diagnostics/workshop-manifest.json" "required-by parent IDs are present" '(?i)"RequiredByPublishedFileIds"\s*:\s*\[\s*[1-9]'
            Require-WorkshopUsableSourceEvidence "dependency mod"
            Require-JsonPattern "diagnostics/workshop-derived-state.json" "dependency mod derived state has active manifest PCK" '(?i)"manifestActivePckCount"\s*:\s*[1-9]'
            Require-JsonPattern "diagnostics/workshop-derived-state.json" "dependency mod derived state has raw staged PCK" '(?i)"rawStagedPckCount"\s*:\s*[1-9]'
            Require-JsonPattern "diagnostics/workshop-derived-state.json" "dependency mod derived state locks Workshop Cloud Push" '(?i)"workshopCloudPushLocked"\s*:\s*true'
            Require-WorkshopModLoaderScanEvidence "dependency mod"
        }
        "broken" {
            Require-JsonPattern "diagnostics/workshop-manifest.json" "broken mod manifest is readable" "\{"
            Require-AnyPattern "diagnostics/workshop-manifest.json" "broken mod evidence is explicit" @(
                '(?i)"MissingDependencyItemCount"\s*:\s*[1-9]',
                '(?i)"MissingDependencyIds"\s*:\s*\[\s*[1-9]',
                '(?i)"Status"\s*:\s*"[^"]*failed"',
                '(?i)"Status"\s*:\s*"unsupported"',
                '(?i)"Status"\s*:\s*"staged-no-pck"'
            )
        }
        "public" {
            Require-WorkshopLoadedModEvidence "public"
            Require-Pattern "diagnostics/runtime-markers.txt" "public runtime marker is captured" "public|Selected branch:"
            Require-JsonPattern "diagnostics/current_runtime_slot.json" "public runtime slot marker is readable" "\{"
            Require-JsonPattern "diagnostics/current_runtime_slot.json" "public runtime slot is selected" '(?i)"[^"]*branch[^"]*"\s*:\s*"public"'
            Require-Pattern "diagnostics/current_runtime_cache.txt" "public runtime cache marker names selected branch" "(?m)^Selected branch:\s*public\s*$"
            Require-Pattern "diagnostics/current_runtime_cache.txt" "public runtime cache marker names selected PCK path" "Selected PCK path:.*files[/\\]game[/\\]SlayTheSpire2\.pck"
            Require-Pattern "diagnostics/current_runtime_cache.txt" "public runtime cache marker records selected PCK hash" "Selected PCK SHA256:"
            Require-Pattern "diagnostics/current_runtime_cache.txt" "public runtime cache marker records active sts2.dll hash" "Publish cache active sts2\.dll SHA256:"
            Require-Pattern "diagnostics/runtime-hashes.txt" "public runtime hashes include selected PCK" "files[/\\]game[/\\]SlayTheSpire2\.pck"
            Require-Pattern "diagnostics/runtime-hashes.txt" "public runtime hashes include active sts2.dll" "sts2\.dll"
            Require-SelectedPckHashEvidence `
                "public" `
                "files[/\\]game[/\\]SlayTheSpire2\.pck" `
                "files[/\\]game[/\\]\.android_pck_patch_v\d+"
            Require-JsonPattern "diagnostics/last_runtime_patch_validation.json" "public runtime patch validation marker is readable" "\{"
            Require-JsonPattern "diagnostics/last_runtime_patch_validation.json" "public runtime patch validation selected branch matches" '(?i)"selectedBranch"\s*:\s*"public"'
            Require-JsonPattern "diagnostics/last_runtime_patch_validation.json" "public runtime patch validation passed" '(?i)"status"\s*:\s*"passed"'
            Require-JsonMarkerMatch "diagnostics/last_runtime_patch_validation.json" "selectedPckSha256" "diagnostics/current_runtime_cache.txt" "Selected PCK SHA256:" "public runtime validation selected PCK hash matches cache marker"
            Require-JsonMarkerMatch "diagnostics/last_runtime_patch_validation.json" "selectedSourceAssemblySha256" "diagnostics/current_runtime_cache.txt" "Selected source sts2.dll SHA256:" "public runtime validation selected source sts2.dll hash matches cache marker"
            Require-JsonMarkerMatch "diagnostics/last_runtime_patch_validation.json" "activeAndroidAssemblySha256" "diagnostics/current_runtime_cache.txt" "Publish cache active sts2.dll SHA256:" "public runtime validation active Android sts2.dll hash matches cache marker"
            Require-Pattern "logs/logcat-workshop-filtered.txt" "public launch/runtime log is captured" "Loading PCK from|Selected Steam branch|Runtime"
            Require-Pattern "logs/logcat-workshop-filtered.txt" "public launch loaded public PCK path" "Loading PCK from:.*files[/\\]game[/\\]SlayTheSpire2\.pck"
        }
        "public-beta" {
            Require-WorkshopLoadedModEvidence "public-beta"
            Require-NonPublicRuntimeEvidence "public-beta"
        }
        "core-release" {
            Require-WorkshopLoadedModEvidence "core-release"
            Require-NonPublicRuntimeEvidence "core-release"
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host ""
    Write-Host "Workshop mod evidence review failed:"
    foreach ($failure in $failures) {
        Write-Host "FAIL $failure"
    }
    throw "Workshop mod evidence review failed with $($failures.Count) failure(s)."
}

Write-Host "Workshop mod evidence review passed ($passes checks): $resolvedEvidenceDir"
