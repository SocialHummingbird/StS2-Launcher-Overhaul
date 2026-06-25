param(
    [string]$TempRoot = "",
    [switch]$KeepArtifacts
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$reviewScript = Join-Path $PSScriptRoot "review-workshop-mod-evidence.ps1"
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

function New-WorkshopEvidenceBundle(
    [string]$BaseDir,
    [string]$Phase,
    [switch]$StagedPck,
    [switch]$Dependency,
    [switch]$Broken,
    [switch]$NoPck,
    [switch]$CaptureFailure,
    [switch]$MissingSourceEvidence,
    [switch]$NoLaunchRequested,
    [switch]$FallbackCrashLog,
    [switch]$UnlockedWithStagedPck,
    [switch]$MissingWorkshopModLoaderScan,
    [switch]$CachedDownloadReuse,
    [switch]$StaleDownloadArtifact,
    [switch]$DepotManifestSource
) {
    New-Item -ItemType Directory -Force -Path (Join-Path $BaseDir "diagnostics"), (Join-Path $BaseDir "logs") | Out-Null

    Save-TestText (Join-Path $BaseDir "summary.md") "# Workshop mod evidence summary`nCollector boundary: This collector does not press Steam Cloud Push and does not upload save data."
    Save-TestText (Join-Path $BaseDir "ARTIFACT_HYGIENE.txt") "Raw full logcat is omitted by default"
    Save-TestText (Join-Path $BaseDir "run-metadata.json") (@{
        generatedUtc = "2026-06-21T00:00:00.0000000Z"
        collector = "capture-workshop-mod-evidence.ps1"
        readOnlySteamCloud = $true
        phase = $Phase
        launchRequested = -not $NoLaunchRequested
    } | ConvertTo-Json -Depth 5)

    $manifest = [ordered]@{
        Version = 1
        SubscriptionQueryType = "subscriptions"
        SubscriptionQueryAttempts = @("subscriptions")
        SubscribedItemCount = if ($Dependency) { 1 } else { 0 }
        DependencyItemCount = if ($Dependency) { 1 } else { 0 }
        TotalItemCount = if ($StagedPck -or $Dependency -or $NoPck) { 1 } else { 0 }
        MissingDependencyItemCount = if ($Broken) { 1 } else { 0 }
        MissingDependencyIds = if ($Broken) { @(999999, 999998) } else { @() }
        Items = @()
    }

    if ($StagedPck -or $Dependency -or $NoPck) {
        $item = [ordered]@{
            PublishedFileId = if ($Dependency) { 222222 } else { 111111 }
            Title = if ($Dependency) { "Synthetic Dependency" } else { "Synthetic Simple Mod" }
            Status = if ($NoPck) { "staged-no-pck" } else { "staged" }
            ManifestId = if ($MissingSourceEvidence) { 0 } elseif ($DepotManifestSource) { 987654321 } else { 0 }
            DownloadUrlPresent = -not ($MissingSourceEvidence -or $DepotManifestSource)
            DownloadUrlHost = if ($MissingSourceEvidence -or $DepotManifestSource) { "" } else { "steamusercontent-a.example.invalid" }
            DownloadSourceKind = if ($MissingSourceEvidence) { "" } elseif ($DepotManifestSource) { "depot-manifest" } else { "direct-url" }
            ExpectedDownloadBytes = if ($MissingSourceEvidence) { 0 } else { 1024 }
            HContentFile = if ($MissingSourceEvidence) { 0 } else { 123456 }
            ReusedCachedDownload = [bool]$CachedDownloadReuse
            IsDependency = [bool]$Dependency
            RequiredByPublishedFileIds = if ($Dependency) { @(111111, 111112) } else { @() }
            StagedDirectory = "files/workshop_mods/staged/$Phase"
            SourceDirectory = "files/workshop_mods/downloads/$Phase"
            ContentSha256 = "0123456789abcdef"
        }
        $manifest.Items = @($item)
    }

    Save-TestText (Join-Path $BaseDir "diagnostics\workshop-manifest.json") ($manifest | ConvertTo-Json -Depth 8)
    Save-TestText (Join-Path $BaseDir "diagnostics\workshop-clear-marker.txt") "clearedAtUtc=2026-06-21T00:00:00.0000000Z`nremovedStagedDirectoryCount=1`nremovedStagedRootFileCount=0`nsteamCloudPushPerformed=false`ndownloadsPreserved=true"
    Save-TestText (Join-Path $BaseDir "diagnostics\workshop-marker-files.txt") "files/workshop_mods/workshop_sync_manifest.json"
    Save-TestText (Join-Path $BaseDir "diagnostics\workshop-marker-contents.txt") "===== files/workshop_mods/workshop_sync_manifest.json`n{}"
    $tree = "files/workshop_mods`nfiles/workshop_mods/staged"
    if ($StaleDownloadArtifact) {
        $tree += "`nfiles/workshop_mods/downloads/111111.tmp-0123456789abcdef0123456789abcdef"
    }
    Save-TestText (Join-Path $BaseDir "diagnostics\workshop-tree.txt") $tree
    Save-TestText (Join-Path $BaseDir "diagnostics\cloud-push-markers.txt") "===== files/last_manual_cloud_push.txt`nmissing"

    $runtimeBranch = if ($Phase -eq "public-beta" -or $Phase -eq "core-release") { $Phase } else { "public" }
    $runtimeSlot = [ordered]@{
        runtimeSlotId = "$runtimeBranch-synthetic-slot"
        branch = $runtimeBranch
        selectedBranch = $runtimeBranch
        pckPath = if ($runtimeBranch -ne "public") { "files/game_versions/$runtimeBranch-12345678/game/SlayTheSpire2.pck" } else { "files/game/SlayTheSpire2.pck" }
        pckSha256 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
        sourceAssemblyPath = "files/game/.godot/mono/publish/sts2.dll"
        sourceAssemblySha256 = "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"
        filesReady = $true
        playable = $true
        runtimeCompatible = $true
        patchCompatible = $true
    }
    Save-TestText (Join-Path $BaseDir "diagnostics\current_runtime_slot.json") ($runtimeSlot | ConvertTo-Json -Depth 6)
    Save-TestText (Join-Path $BaseDir "diagnostics\current_runtime_cache.txt") "Selected branch: $runtimeBranch`nRuntime ID: $runtimeBranch-synthetic-slot`nRuntime pack directory: files/runtime_packs/$runtimeBranch-synthetic-slot`nSelected PCK path: $($runtimeSlot.pckPath)`nSelected PCK SHA256: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa`nSelected source sts2.dll SHA256: cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc`nPublish cache active sts2.dll SHA256: dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd"
    Save-TestText (Join-Path $BaseDir "diagnostics\last_runtime_patch_validation.json") (@{
        status = "passed"
        runtimeSlotId = "$runtimeBranch-synthetic-slot"
        selectedBranch = $runtimeBranch
        selectedPckSha256 = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
        selectedSourceAssemblySha256 = "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"
        activeAndroidAssemblySha256 = "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd"
        runtimePackId = "$runtimeBranch-synthetic-pack"
        runtimePackStatus = "usable"
    } | ConvertTo-Json -Depth 6)
    Save-TestText (Join-Path $BaseDir "diagnostics\runtime-markers.txt") "===== files/current_runtime_slot.json`n$($runtimeSlot | ConvertTo-Json -Compress -Depth 6)`n===== files/current_runtime_cache.txt`nSelected branch: $runtimeBranch`nRuntime ID: $runtimeBranch-synthetic-slot"
    Save-TestText (Join-Path $BaseDir "diagnostics\runtime-pck-patch-markers.txt") "files/.android_pck_patch_v* <missing>"
    Save-TestText (Join-Path $BaseDir "diagnostics\runtime-hashes.txt") "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa  $($runtimeSlot.pckPath)`nsha256sum synthetic`ndddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd  files/.godot/mono/publish/$runtimeBranch/sts2.dll"
    Save-TestText (Join-Path $BaseDir "diagnostics\runtime-tree.txt") "files/game`nfiles/game/SlayTheSpire2.pck`nfiles/runtime_packs/$runtimeBranch-synthetic-slot"
    Save-TestText (Join-Path $BaseDir "diagnostics\selected_runtime_pack_compatibility.json") (@{
        packId = "$runtimeBranch-synthetic-pack"
        sourceRuntimeSlotId = "$runtimeBranch-synthetic-slot"
        sourceBranch = $runtimeBranch
        sourceAssemblySha256 = "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc"
        androidAssemblySha256 = "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd"
        generatedFromCleanDirectory = $true
        supportAssemblies = @()
        supportAssemblySha256 = @{}
    } | ConvertTo-Json -Depth 6)
    Save-TestText (Join-Path $BaseDir "diagnostics\selected_runtime_pack_patch_validation.json") (@{
        status = "passed"
        patchValidationStatus = "passed"
        runtimePackId = "$runtimeBranch-synthetic-pack"
    } | ConvertTo-Json -Depth 6)

    $hashes = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa  files/workshop_mods/workshop_sync_manifest.json`nsha256sum synthetic"
    if ($StagedPck -or $Dependency) {
        $hashes += "`nbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb  files/workshop_mods/staged/$Phase/mod.pck"
    }
    Save-TestText (Join-Path $BaseDir "diagnostics\workshop-hashes.txt") $hashes

    $rawStagedPckCount = if ($StagedPck -or $Dependency) { 1 } else { 0 }
    $manifestActivePckCount = if ($StagedPck -or $Dependency) { 1 } else { 0 }
    Save-TestText (Join-Path $BaseDir "diagnostics\workshop-derived-state.json") (@{
        manifestActivePckCount = $manifestActivePckCount
        rawStagedPckCount = $rawStagedPckCount
        workshopCloudPushLocked = if ($UnlockedWithStagedPck) { $false } else { ($rawStagedPckCount -gt 0) }
        steamCloudPushPerformed = $false
        source = "capture-workshop-mod-evidence.ps1"
    } | ConvertTo-Json -Depth 5)

    if ($CaptureFailure) {
        Save-TestText (Join-Path $BaseDir "diagnostics\workshop-tree.txt") "CAPTURE_FAILED: run-as: package not debuggable"
    }

    $launchLog = "Selected Steam branch $runtimeBranch`nRuntime pack loaded`nLoading PCK from: $($runtimeSlot.pckPath)"
    if (($StagedPck -or $Dependency) -and -not $MissingWorkshopModLoaderScan) {
        $launchLog += "`n[Mods] Scanning Workshop staged mods: files/workshop_mods/staged`nLoaded mod synthetic"
    }
    if ($CachedDownloadReuse) {
        $launchLog += "`n[Workshop] Using cached Workshop download for Synthetic Simple Mod (111111)"
    }
    if ($FallbackCrashLog) {
        $launchLog += "`nNativeFallbackActivity selected after runtime pack validation failed`nFATAL EXCEPTION: main"
    }
    Save-TestText (Join-Path $BaseDir "logs\logcat-workshop-filtered.txt") $launchLog
}

function Invoke-ReviewShouldPass([string]$EvidenceDir, [string]$Phase) {
    & $reviewScript -EvidenceDir $EvidenceDir -RequirePhase $Phase -Quiet | Out-Null
}

function Invoke-ReviewShouldFail([string]$EvidenceDir, [string]$Phase, [string]$Description) {
    try {
        & $reviewScript -EvidenceDir $EvidenceDir -RequirePhase $Phase -Quiet | Out-Null
    } catch {
        Write-Host "PASS negative case rejected: $Description"
        return
    }

    throw "Expected Workshop evidence review to fail: $Description"
}

$resolvedTempRoot = (Resolve-Path -LiteralPath (New-Item -ItemType Directory -Force -Path $TempRoot)).ProviderPath
$runRoot = Join-Path $resolvedTempRoot ("workshop-reviewer-tests-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

try {
    foreach ($phase in @("no-mods", "simple", "dependency", "broken", "public", "public-beta", "core-release")) {
        $dir = Join-Path $runRoot $phase
        New-WorkshopEvidenceBundle `
            -BaseDir $dir `
            -Phase $phase `
            -StagedPck:($phase -eq "simple" -or $phase -eq "public" -or $phase -eq "public-beta" -or $phase -eq "core-release") `
            -Dependency:($phase -eq "dependency") `
            -Broken:($phase -eq "broken")
        Invoke-ReviewShouldPass -EvidenceDir $dir -Phase $phase
        Write-Host "PASS positive phase accepted: $phase"
    }

    $optionalPhaseDir = Join-Path $runRoot "optional-phase"
    New-WorkshopEvidenceBundle -BaseDir $optionalPhaseDir -Phase "simple" -StagedPck
    & $reviewScript -EvidenceDir $optionalPhaseDir -Quiet | Out-Null
    Write-Host "PASS optional RequirePhase accepted"

    $cachedReuseDir = Join-Path $runRoot "optional-cached-reuse"
    New-WorkshopEvidenceBundle -BaseDir $cachedReuseDir -Phase "simple" -StagedPck -CachedDownloadReuse
    & $reviewScript -EvidenceDir $cachedReuseDir -RequirePhase simple -RequireCachedDownloadReuse -Quiet | Out-Null
    Write-Host "PASS optional cached download reuse accepted"

    $depotManifestSourceDir = Join-Path $runRoot "optional-depot-manifest-source"
    New-WorkshopEvidenceBundle -BaseDir $depotManifestSourceDir -Phase "simple" -StagedPck -DepotManifestSource
    & $reviewScript -EvidenceDir $depotManifestSourceDir -RequirePhase simple -Quiet | Out-Null
    Write-Host "PASS optional depot-manifest source accepted"

    $brokenNoPckDir = Join-Path $runRoot "broken-no-pck"
    New-WorkshopEvidenceBundle -BaseDir $brokenNoPckDir -Phase "broken" -NoPck
    Invoke-ReviewShouldPass -EvidenceDir $brokenNoPckDir -Phase "broken"
    Write-Host "PASS broken no-PCK phase accepted"

    $brokenNoPckCachedReuseDir = Join-Path $runRoot "broken-no-pck-cached-reuse"
    New-WorkshopEvidenceBundle -BaseDir $brokenNoPckCachedReuseDir -Phase "broken" -NoPck -CachedDownloadReuse
    & $reviewScript -EvidenceDir $brokenNoPckCachedReuseDir -RequirePhase broken -RequireCachedDownloadReuse -Quiet | Out-Null
    Write-Host "PASS broken no-PCK cached download reuse accepted"

    $dirtyNoModsDir = Join-Path $runRoot "negative-no-mods-staged-pck"
    New-WorkshopEvidenceBundle -BaseDir $dirtyNoModsDir -Phase "no-mods" -StagedPck
    Invoke-ReviewShouldFail -EvidenceDir $dirtyNoModsDir -Phase "no-mods" -Description "no-mods evidence with staged PCK"

    $captureFailureDir = Join-Path $runRoot "negative-capture-failure"
    New-WorkshopEvidenceBundle -BaseDir $captureFailureDir -Phase "simple" -StagedPck -CaptureFailure
    Invoke-ReviewShouldFail -EvidenceDir $captureFailureDir -Phase "simple" -Description "capture failure marker in diagnostics"

    $missingSourceDir = Join-Path $runRoot "negative-missing-source-evidence"
    New-WorkshopEvidenceBundle -BaseDir $missingSourceDir -Phase "simple" -StagedPck -MissingSourceEvidence
    Invoke-ReviewShouldFail -EvidenceDir $missingSourceDir -Phase "simple" -Description "staged simple mod without usable source provenance"

    $rawUrlDir = Join-Path $runRoot "negative-raw-download-url"
    New-WorkshopEvidenceBundle -BaseDir $rawUrlDir -Phase "simple" -StagedPck
    $rawUrlManifestPath = Join-Path $rawUrlDir "diagnostics\workshop-manifest.json"
    $rawUrlManifest = Get-Content -Raw -LiteralPath $rawUrlManifestPath | ConvertFrom-Json
    $rawUrlManifest.Items[0] | Add-Member -NotePropertyName "DownloadUrl" -NotePropertyValue "https://steamusercontent-a.example.invalid/signed-token"
    Save-TestText $rawUrlManifestPath ($rawUrlManifest | ConvertTo-Json -Depth 8)
    Invoke-ReviewShouldFail -EvidenceDir $rawUrlDir -Phase "simple" -Description "manifest containing raw Workshop download URL"

    $noLaunchDir = Join-Path $runRoot "negative-no-launch-requested"
    New-WorkshopEvidenceBundle -BaseDir $noLaunchDir -Phase "public-beta" -NoLaunchRequested
    Invoke-ReviewShouldFail -EvidenceDir $noLaunchDir -Phase "public-beta" -Description "public-beta evidence without app launch request"

    $fallbackCrashDir = Join-Path $runRoot "negative-fallback-crash-log"
    New-WorkshopEvidenceBundle -BaseDir $fallbackCrashDir -Phase "public-beta" -FallbackCrashLog
    Invoke-ReviewShouldFail -EvidenceDir $fallbackCrashDir -Phase "public-beta" -Description "public-beta evidence containing NativeFallback/crash log"

    $modInitializerErrorDir = Join-Path $runRoot "negative-mod-initializer-error"
    New-WorkshopEvidenceBundle -BaseDir $modInitializerErrorDir -Phase "public-beta"
    Add-Content -LiteralPath (Join-Path $modInitializerErrorDir "logs\logcat-workshop-filtered.txt") -Value "Exception thrown when calling mod initializer of type BaseLib.BaseLibMain: System.MissingMethodException: Method not found: void System.Text.Json.Serialization.Metadata.JsonPropertyInfoValues``1.set_IsProperty(bool)"
    Invoke-ReviewShouldFail -EvidenceDir $modInitializerErrorDir -Phase "public-beta" -Description "public-beta evidence containing mod initializer MissingMethodException"

    $unlockedWithStagedPckDir = Join-Path $runRoot "negative-staged-pck-unlocked"
    New-WorkshopEvidenceBundle -BaseDir $unlockedWithStagedPckDir -Phase "simple" -StagedPck -UnlockedWithStagedPck
    Invoke-ReviewShouldFail -EvidenceDir $unlockedWithStagedPckDir -Phase "simple" -Description "staged Workshop PCK without derived Cloud Push lock"

    $missingLoaderScanDir = Join-Path $runRoot "negative-missing-workshop-loader-scan"
    New-WorkshopEvidenceBundle -BaseDir $missingLoaderScanDir -Phase "simple" -StagedPck -MissingWorkshopModLoaderScan
    Invoke-ReviewShouldFail -EvidenceDir $missingLoaderScanDir -Phase "simple" -Description "staged Workshop PCK without Workshop mod-loader scan log"

    $missingCachedReuseDir = Join-Path $runRoot "negative-missing-cached-reuse"
    New-WorkshopEvidenceBundle -BaseDir $missingCachedReuseDir -Phase "simple" -StagedPck
    try {
        & $reviewScript -EvidenceDir $missingCachedReuseDir -RequirePhase simple -RequireCachedDownloadReuse -Quiet | Out-Null
    } catch {
        Write-Host "PASS negative case rejected: requested cached Workshop reuse without manifest/log evidence"
        $missingCachedReuseDir = ""
    }
    if (-not [string]::IsNullOrWhiteSpace($missingCachedReuseDir)) {
        throw "Expected Workshop evidence review to fail: requested cached Workshop reuse without manifest/log evidence"
    }

    $staleDownloadArtifactDir = Join-Path $runRoot "negative-stale-download-artifact"
    New-WorkshopEvidenceBundle -BaseDir $staleDownloadArtifactDir -Phase "simple" -StagedPck -StaleDownloadArtifact
    Invoke-ReviewShouldFail -EvidenceDir $staleDownloadArtifactDir -Phase "simple" -Description "stale Workshop download temp artifact in evidence tree"

    $wrongBetaPckPathDir = Join-Path $runRoot "negative-public-beta-public-pck-path"
    New-WorkshopEvidenceBundle -BaseDir $wrongBetaPckPathDir -Phase "public-beta"
    $wrongBetaCachePath = Join-Path $wrongBetaPckPathDir "diagnostics\current_runtime_cache.txt"
    $wrongBetaCache = (Get-Content -Raw -LiteralPath $wrongBetaCachePath) -replace 'files/game_versions/public-beta-12345678/game/SlayTheSpire2\.pck', 'files/game/SlayTheSpire2.pck'
    Save-TestText $wrongBetaCachePath $wrongBetaCache
    Invoke-ReviewShouldFail -EvidenceDir $wrongBetaPckPathDir -Phase "public-beta" -Description "public-beta evidence with public selected PCK path"

    $missingActiveDllHashDir = Join-Path $runRoot "negative-public-beta-missing-active-dll-hash"
    New-WorkshopEvidenceBundle -BaseDir $missingActiveDllHashDir -Phase "public-beta"
    Save-TestText (Join-Path $missingActiveDllHashDir "diagnostics\runtime-hashes.txt") "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa  files/game_versions/public-beta-12345678/game/SlayTheSpire2.pck"
    Invoke-ReviewShouldFail -EvidenceDir $missingActiveDllHashDir -Phase "public-beta" -Description "public-beta evidence missing active sts2.dll hash"

    $missingSupportHashDir = Join-Path $runRoot "negative-public-beta-missing-runtime-pack-support-hash"
    New-WorkshopEvidenceBundle -BaseDir $missingSupportHashDir -Phase "public-beta"
    Save-TestText (Join-Path $missingSupportHashDir "diagnostics\selected_runtime_pack_compatibility.json") (@{
        packId = "public-beta-synthetic-pack"
        sourceRuntimeSlotId = "public-beta-synthetic-slot"
        sourceBranch = "public-beta"
        generatedFromCleanDirectory = $true
        supportAssemblies = @("Support.dll")
    } | ConvertTo-Json -Depth 6)
    Invoke-ReviewShouldFail -EvidenceDir $missingSupportHashDir -Phase "public-beta" -Description "public-beta runtime pack without support assembly hashes"

    $failedRuntimePackValidationDir = Join-Path $runRoot "negative-public-beta-failed-runtime-pack-validation"
    New-WorkshopEvidenceBundle -BaseDir $failedRuntimePackValidationDir -Phase "public-beta"
    Save-TestText (Join-Path $failedRuntimePackValidationDir "diagnostics\selected_runtime_pack_patch_validation.json") (@{
        status = "failed"
        patchValidationStatus = "failed"
        runtimePackId = "public-beta-synthetic-pack"
    } | ConvertTo-Json -Depth 6)
    Invoke-ReviewShouldFail -EvidenceDir $failedRuntimePackValidationDir -Phase "public-beta" -Description "public-beta runtime pack validation failure"

    $publicValidationBranchMismatchDir = Join-Path $runRoot "negative-public-validation-branch-mismatch"
    New-WorkshopEvidenceBundle -BaseDir $publicValidationBranchMismatchDir -Phase "public"
    $publicValidationPath = Join-Path $publicValidationBranchMismatchDir "diagnostics\last_runtime_patch_validation.json"
    $publicValidation = Get-Content -Raw -LiteralPath $publicValidationPath | ConvertFrom-Json
    $publicValidation.selectedBranch = "public-beta"
    Save-TestText $publicValidationPath ($publicValidation | ConvertTo-Json -Depth 6)
    Invoke-ReviewShouldFail -EvidenceDir $publicValidationBranchMismatchDir -Phase "public" -Description "public evidence with public-beta runtime validation branch"

    $betaValidationBranchMismatchDir = Join-Path $runRoot "negative-public-beta-validation-branch-mismatch"
    New-WorkshopEvidenceBundle -BaseDir $betaValidationBranchMismatchDir -Phase "public-beta"
    $betaValidationPath = Join-Path $betaValidationBranchMismatchDir "diagnostics\last_runtime_patch_validation.json"
    $betaValidation = Get-Content -Raw -LiteralPath $betaValidationPath | ConvertFrom-Json
    $betaValidation.selectedBranch = "public"
    Save-TestText $betaValidationPath ($betaValidation | ConvertTo-Json -Depth 6)
    Invoke-ReviewShouldFail -EvidenceDir $betaValidationBranchMismatchDir -Phase "public-beta" -Description "public-beta evidence with public runtime validation branch"

    $publicPckHashMismatchDir = Join-Path $runRoot "negative-public-pck-hash-mismatch"
    New-WorkshopEvidenceBundle -BaseDir $publicPckHashMismatchDir -Phase "public"
    $publicPckValidationPath = Join-Path $publicPckHashMismatchDir "diagnostics\last_runtime_patch_validation.json"
    $publicPckValidation = Get-Content -Raw -LiteralPath $publicPckValidationPath | ConvertFrom-Json
    $publicPckValidation.selectedPckSha256 = "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"
    Save-TestText $publicPckValidationPath ($publicPckValidation | ConvertTo-Json -Depth 6)
    Invoke-ReviewShouldFail -EvidenceDir $publicPckHashMismatchDir -Phase "public" -Description "public runtime validation selected PCK hash mismatch"

    $betaActiveDllHashMismatchDir = Join-Path $runRoot "negative-public-beta-active-dll-hash-mismatch"
    New-WorkshopEvidenceBundle -BaseDir $betaActiveDllHashMismatchDir -Phase "public-beta"
    $betaDllValidationPath = Join-Path $betaActiveDllHashMismatchDir "diagnostics\last_runtime_patch_validation.json"
    $betaDllValidation = Get-Content -Raw -LiteralPath $betaDllValidationPath | ConvertFrom-Json
    $betaDllValidation.activeAndroidAssemblySha256 = "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"
    Save-TestText $betaDllValidationPath ($betaDllValidation | ConvertTo-Json -Depth 6)
    Invoke-ReviewShouldFail -EvidenceDir $betaActiveDllHashMismatchDir -Phase "public-beta" -Description "public-beta active Android sts2.dll hash mismatch"

    $betaPackAndroidHashMismatchDir = Join-Path $runRoot "negative-public-beta-runtime-pack-android-hash-mismatch"
    New-WorkshopEvidenceBundle -BaseDir $betaPackAndroidHashMismatchDir -Phase "public-beta"
    $betaPackManifestPath = Join-Path $betaPackAndroidHashMismatchDir "diagnostics\selected_runtime_pack_compatibility.json"
    $betaPackManifest = Get-Content -Raw -LiteralPath $betaPackManifestPath | ConvertFrom-Json
    $betaPackManifest.androidAssemblySha256 = "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"
    Save-TestText $betaPackManifestPath ($betaPackManifest | ConvertTo-Json -Depth 6)
    Invoke-ReviewShouldFail -EvidenceDir $betaPackAndroidHashMismatchDir -Phase "public-beta" -Description "public-beta runtime pack Android sts2.dll hash mismatch"

    $betaPackIdMismatchDir = Join-Path $runRoot "negative-public-beta-runtime-pack-id-mismatch"
    New-WorkshopEvidenceBundle -BaseDir $betaPackIdMismatchDir -Phase "public-beta"
    $betaPackIdManifestPath = Join-Path $betaPackIdMismatchDir "diagnostics\selected_runtime_pack_compatibility.json"
    $betaPackIdManifest = Get-Content -Raw -LiteralPath $betaPackIdManifestPath | ConvertFrom-Json
    $betaPackIdManifest.packId = "wrong-public-beta-synthetic-pack"
    Save-TestText $betaPackIdManifestPath ($betaPackIdManifest | ConvertTo-Json -Depth 6)
    Invoke-ReviewShouldFail -EvidenceDir $betaPackIdMismatchDir -Phase "public-beta" -Description "public-beta runtime pack ID mismatch"

    $betaPackSlotMismatchDir = Join-Path $runRoot "negative-public-beta-runtime-pack-slot-mismatch"
    New-WorkshopEvidenceBundle -BaseDir $betaPackSlotMismatchDir -Phase "public-beta"
    $betaPackSlotManifestPath = Join-Path $betaPackSlotMismatchDir "diagnostics\selected_runtime_pack_compatibility.json"
    $betaPackSlotManifest = Get-Content -Raw -LiteralPath $betaPackSlotManifestPath | ConvertFrom-Json
    $betaPackSlotManifest.sourceRuntimeSlotId = "wrong-public-beta-synthetic-slot"
    Save-TestText $betaPackSlotManifestPath ($betaPackSlotManifest | ConvertTo-Json -Depth 6)
    Invoke-ReviewShouldFail -EvidenceDir $betaPackSlotMismatchDir -Phase "public-beta" -Description "public-beta runtime pack source slot mismatch"

    $betaPackReportIdMismatchDir = Join-Path $runRoot "negative-public-beta-runtime-pack-report-id-mismatch"
    New-WorkshopEvidenceBundle -BaseDir $betaPackReportIdMismatchDir -Phase "public-beta"
    $betaPackReportPath = Join-Path $betaPackReportIdMismatchDir "diagnostics\selected_runtime_pack_patch_validation.json"
    $betaPackReport = Get-Content -Raw -LiteralPath $betaPackReportPath | ConvertFrom-Json
    $betaPackReport.runtimePackId = "wrong-public-beta-synthetic-pack"
    Save-TestText $betaPackReportPath ($betaPackReport | ConvertTo-Json -Depth 6)
    Invoke-ReviewShouldFail -EvidenceDir $betaPackReportIdMismatchDir -Phase "public-beta" -Description "public-beta runtime pack validation report ID mismatch"

    $publicWrongLoadedPckDir = Join-Path $runRoot "negative-public-loaded-beta-pck"
    New-WorkshopEvidenceBundle -BaseDir $publicWrongLoadedPckDir -Phase "public"
    Save-TestText (Join-Path $publicWrongLoadedPckDir "logs\logcat-workshop-filtered.txt") "Selected Steam branch public`nRuntime pack loaded`nLoading PCK from: files/game_versions/public-beta-12345678/game/SlayTheSpire2.pck"
    Invoke-ReviewShouldFail -EvidenceDir $publicWrongLoadedPckDir -Phase "public" -Description "public evidence with public-beta loaded PCK log"

    $betaWrongLoadedPckDir = Join-Path $runRoot "negative-public-beta-loaded-public-pck"
    New-WorkshopEvidenceBundle -BaseDir $betaWrongLoadedPckDir -Phase "public-beta"
    Save-TestText (Join-Path $betaWrongLoadedPckDir "logs\logcat-workshop-filtered.txt") "Selected Steam branch public-beta`nRuntime pack loaded`nLoading PCK from: files/game/SlayTheSpire2.pck"
    Invoke-ReviewShouldFail -EvidenceDir $betaWrongLoadedPckDir -Phase "public-beta" -Description "public-beta evidence with public loaded PCK log"

    Write-Host "Workshop mod evidence reviewer regression tests passed: $runRoot"
} finally {
    if (-not $KeepArtifacts) {
        $resolvedRunRoot = (Resolve-Path -LiteralPath $runRoot).ProviderPath
        if ($resolvedRunRoot.StartsWith($resolvedTempRoot, [StringComparison]::OrdinalIgnoreCase)) {
            Remove-Item -LiteralPath $resolvedRunRoot -Recurse -Force
        }
    }
}
