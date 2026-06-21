param(
    [string]$AdbPath = "adb",
    [string]$PackageName = "com.sts2launcher.overhaul.fork.dev",
    [string]$DeviceSerial = "",
    [string]$OutputRoot = "artifacts\android",
    [string]$RunLabel = "",
    [switch]$IncludeRawLogcat
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot "android-adb-utils.ps1")
. (Join-Path $PSScriptRoot "android-shell-utils.ps1")
. (Join-Path $PSScriptRoot "evidence-marker-utils.ps1")
. (Join-Path $PSScriptRoot "evidence-report-utils.ps1")
. (Join-Path $PSScriptRoot "capture-multi-version-runtime-evidence.helpers.ps1")
$AdbPath = Resolve-AndroidAdbPath -AdbPath $AdbPath
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$generatedUtc = (Get-Date).ToUniversalTime().ToString('O')
$safeRunLabel = ($RunLabel -replace '[^A-Za-z0-9._-]', '-').Trim('-')
if ([string]::IsNullOrWhiteSpace($safeRunLabel)) {
    $artifactFolderName = "multi-version-runtime-$timestamp"
} else {
    $artifactFolderName = "multi-version-runtime-$safeRunLabel-$timestamp"
}
$outputDir = Join-Path $root (Join-Path $OutputRoot $artifactFolderName)
$diagnosticsDir = Join-Path $outputDir "diagnostics"
$logsDir = Join-Path $outputDir "logs"
New-Item -ItemType Directory -Force $diagnosticsDir, $logsDir | Out-Null

$metadata = [ordered]@{
    generatedUtc = $generatedUtc
    packageName = $PackageName
    runLabel = if ([string]::IsNullOrWhiteSpace($safeRunLabel)) { $null } else { $safeRunLabel }
    outputDirectory = $outputDir
    artifactFolderName = $artifactFolderName
    collector = "capture-multi-version-runtime-evidence.ps1"
    readOnly = $true
}
Save-Text -Path (Join-Path $outputDir "run-metadata.json") -Text ($metadata | ConvertTo-Json -Depth 4)

$artifactHygiene = @"
Multi-version runtime evidence artifact hygiene

This collector is read-only and does not mutate Steam Cloud or app data.
Raw full logcat is omitted by default. If logs/logcat-full.txt exists, treat it as local-only until manually reviewed and redacted.
Captured app-private marker files can include local app paths, selected branch names, runtime hashes, and device/package metadata.
Do not publicly attach full diagnostics without manual review.
"@
Save-Text -Path (Join-Path $outputDir "ARTIFACT_HYGIENE.txt") -Text $artifactHygiene

Save-AdbText -Path (Join-Path $logsDir "adb-devices.txt") -Arguments @("devices", "-l") -AllowFailure
Save-AdbText -Path (Join-Path $diagnosticsDir "package.txt") -Arguments @("shell", "dumpsys", "package", $PackageName) -AllowFailure
Save-RunAsText -Path (Join-Path $diagnosticsDir "run-as-pwd.txt") -Command "pwd" -AllowFailure

$markerFind = "find files -maxdepth 8 -type f \( -name 'steam_branch.txt' -o -name 'release_info.json' -o -name 'compatibility.json' -o -name 'patch_validation.json' -o -name '.android_patch_validation.json' -o -name 'current_runtime_slot.json' -o -name 'current_runtime_cache.txt' -o -name 'current_android_save_origin.txt' -o -name 'last_runtime_patch_validation.json' -o -name 'last_manual_cloud_pull.txt' -o -name 'last_manual_cloud_push.txt' -o -name 'last_manual_cloud_push_blocked.txt' -o -name 'last_game_branch_switch.txt' -o -name 'last_game_version_cache_cleanup.txt' -o -name 'last_game_version_redownload.txt' \) -print 2>/dev/null | sort"
Save-RunAsText -Path (Join-Path $diagnosticsDir "runtime-marker-files.txt") -Command $markerFind -AllowFailure

$markerDump = "$markerFind | while IFS= read -r f; do echo `"===== `$f`"; sed -n '1,160p' `"`$f`" 2>/dev/null || true; done"
Save-RunAsText -Path (Join-Path $diagnosticsDir "runtime-marker-contents.txt") -Command $markerDump -AllowFailure

$hashCommand = "find files -maxdepth 9 -type f \( -name 'SlayTheSpire2.pck' -o -name 'sts2.dll' -o -name 'compatibility.json' -o -name 'patch_validation.json' -o -name '.android_patch_validation.json' -o -name 'current_runtime_slot.json' -o -name 'current_runtime_cache.txt' -o -name 'last_runtime_patch_validation.json' \) -print -exec sha256sum '{}' \; -exec ls -l '{}' \; 2>/dev/null | sort"
Save-RunAsText -Path (Join-Path $diagnosticsDir "runtime-hashes.txt") -Command $hashCommand -AllowFailure

$treeCommand = "find files/game files/game_versions files/runtime_packs files/.godot/mono/publish -maxdepth 5 -print 2>/dev/null | sort"
Save-RunAsText -Path (Join-Path $diagnosticsDir "runtime-tree.txt") -Command $treeCommand -AllowFailure

$focusedPatterns = "Selected Steam branch|Selected game version slot|Selected game PCK|Loading PCK from|runtime pack|Runtime pack|Assembly cache runtime|Assembly cache diagnostics|Copying game assemblies|Copied runtime-pack|startup patch orchestration|Patch orchestration|runtime patch validation|AndroidRuntime|FATAL EXCEPTION|signal "
Save-AdbText -Path (Join-Path $logsDir "logcat-runtime-focused.txt") -Arguments @("logcat", "-d") -AllowFailure
$focusedLogPath = Join-Path $logsDir "logcat-runtime-focused.txt"
if (Test-Path -LiteralPath $focusedLogPath) {
    $focused = Get-Content -LiteralPath $focusedLogPath | Select-String -Pattern $focusedPatterns
    Save-Text -Path (Join-Path $logsDir "logcat-runtime-filtered.txt") -Text (($focused | ForEach-Object { $_.Line }) -join [Environment]::NewLine)
}

if ($IncludeRawLogcat) {
    Save-AdbText -Path (Join-Path $logsDir "logcat-full.txt") -Arguments @("logcat", "-d") -AllowFailure
}

$runtimeValidationText = Invoke-RunAsText -Command "cat files/last_runtime_patch_validation.json 2>/dev/null || true" -AllowFailure
$runtimeSlotText = Invoke-RunAsText -Command "cat files/current_runtime_slot.json 2>/dev/null || true" -AllowFailure
$runtimeCacheText = Invoke-RunAsText -Command "cat files/current_runtime_cache.txt 2>/dev/null || true" -AllowFailure
$saveOriginText = Invoke-RunAsText -Command "cat files/current_android_save_origin.txt 2>/dev/null || true" -AllowFailure
$runtimeValidationPath = Join-Path $diagnosticsDir "last_runtime_patch_validation.json"
$runtimeSlotPath = Join-Path $diagnosticsDir "current_runtime_slot.json"
$runtimeCachePath = Join-Path $diagnosticsDir "current_runtime_cache.txt"
$saveOriginPath = Join-Path $diagnosticsDir "current_android_save_origin.txt"
Save-Text -Path $runtimeValidationPath -Text $runtimeValidationText
Save-Text -Path $runtimeSlotPath -Text $runtimeSlotText
Save-Text -Path $runtimeCachePath -Text $runtimeCacheText
Save-Text -Path $saveOriginPath -Text $saveOriginText

$runtimeValidation = Read-JsonFile -Path $runtimeValidationPath
$runtimeSlotEvidence = Read-JsonFile -Path $runtimeSlotPath
$runtimeSlotPckActualSha256 = "<missing>"
$runtimeSlotSourceAssemblyActualSha256 = "<missing>"
if ($runtimeSlotEvidence) {
    $runtimeSlotPckActualSha256 = Read-DeviceSha256 -Path "$($runtimeSlotEvidence.pckPath)"
    $runtimeSlotSourceAssemblyActualSha256 = Read-DeviceSha256 -Path "$($runtimeSlotEvidence.sourceAssemblyPath)"
}
$runtimeCacheBranch = Read-MarkerValueFromText -Text $runtimeCacheText -Prefix "Selected branch:"
$runtimeCacheBranchRequiresRuntimePack = Read-MarkerValueFromText -Text $runtimeCacheText -Prefix "Selected branch requires runtime pack:"
$runtimeCacheId = Read-MarkerValueFromText -Text $runtimeCacheText -Prefix "Runtime ID:"
$runtimeCacheSource = Read-MarkerValueFromText -Text $runtimeCacheText -Prefix "Runtime source:"
$runtimePackDirectory = Read-MarkerValueFromText -Text $runtimeCacheText -Prefix "Runtime pack directory:"
$runtimeCachePck = Read-MarkerValueFromText -Text $runtimeCacheText -Prefix "Selected PCK SHA256:"
$runtimeCacheSelectedSource = Read-MarkerValueFromText -Text $runtimeCacheText -Prefix "Selected source sts2.dll SHA256:"
$runtimeCachePublishAssembly = Read-MarkerValueFromText -Text $runtimeCacheText -Prefix "Publish cache active sts2.dll SHA256:"
$saveOriginAction = Read-MarkerValueFromText -Text $saveOriginText -Prefix "Origin action:"
$saveOriginBranch = Read-MarkerValueFromText -Text $saveOriginText -Prefix "Selected branch:"
$saveOriginRuntimeSlotId = Read-MarkerValueFromText -Text $saveOriginText -Prefix "Selected runtime slot ID:"
$saveOriginPck = Read-MarkerValueFromText -Text $saveOriginText -Prefix "Selected PCK SHA256:"
$saveOriginSourceAssembly = Read-MarkerValueFromText -Text $saveOriginText -Prefix "Selected source sts2.dll SHA256:"
$saveOriginRuntimePlayable = Read-MarkerValueFromText -Text $saveOriginText -Prefix "Selected runtime playable:"
$saveOriginRuntimeVerified = Read-MarkerValueFromText -Text $saveOriginText -Prefix "Current Android local saves verified for selected runtime:"

$runtimePackManifestText = ""
$runtimePackValidationText = ""
if (-not [string]::IsNullOrWhiteSpace($runtimePackDirectory) -and -not $runtimePackDirectory.StartsWith("<")) {
    $runtimePackManifestText = Invoke-RunAsText -Command "cat '$runtimePackDirectory/compatibility.json' 2>/dev/null || true" -AllowFailure
    $runtimePackValidationText = Invoke-RunAsText -Command "cat '$runtimePackDirectory/patch_validation.json' 2>/dev/null || true" -AllowFailure
}
$runtimePackManifestPath = Join-Path $diagnosticsDir "selected_runtime_pack_compatibility.json"
$runtimePackValidationPath = Join-Path $diagnosticsDir "selected_runtime_pack_patch_validation.json"
Save-Text -Path $runtimePackManifestPath -Text $runtimePackManifestText
Save-Text -Path $runtimePackValidationPath -Text $runtimePackValidationText
$runtimePackManifest = Read-JsonFile -Path $runtimePackManifestPath
$runtimePackValidation = Read-JsonFile -Path $runtimePackValidationPath
$runtimePackClosedDllSet = Test-RuntimePackClosedDllSet -Manifest $runtimePackManifest -Directory $runtimePackDirectory
$runtimePackSourcePck = if ($runtimePackManifest) { ConvertTo-EvidenceString -Value $runtimePackManifest.sourcePckSha256 } else { "" }
$installedRuntimeComparison = Compare-InstalledRuntimeEvidence -RuntimeSlotEvidence $runtimeSlotEvidence -RuntimeValidation $runtimeValidation -RuntimePackManifest $runtimePackManifest -RuntimeCachePck $runtimeCachePck
$saveOriginComparison = Compare-SaveOriginEvidence -RuntimeValidation $runtimeValidation -RuntimePackManifest $runtimePackManifest -RuntimeCachePck $runtimeCachePck -SaveOriginRuntimeSlotId $saveOriginRuntimeSlotId -SaveOriginPck $saveOriginPck -SaveOriginSourceAssembly $saveOriginSourceAssembly -SaveOriginRuntimePlayable $saveOriginRuntimePlayable -SaveOriginRuntimeVerified $saveOriginRuntimeVerified
$runtimePackComparison = Compare-RuntimePackEvidence -RuntimePackManifest $runtimePackManifest -RuntimeValidation $runtimeValidation -RuntimeSlotEvidence $runtimeSlotEvidence -RuntimePackValidation $runtimePackValidation -RuntimePackClosedDllSet $runtimePackClosedDllSet -RuntimeCachePck $runtimeCachePck

$validationLines = [System.Collections.Generic.List[string]]::new()
$validationLines.Add("# Multi-version runtime validation report")
$validationLines.Add("")
$validationLines.Add("Generated UTC: $((Get-Date).ToUniversalTime().ToString('O'))")
$validationLines.Add("Package: $PackageName")
$validationLines.Add("Output: $outputDir")
$validationLines.Add("")
$validationLines.Add("This report is generated from captured evidence only. It does not mutate Steam Cloud, app data, installed game files, or device state.")
$validationLines.Add("")
$validationLines.Add("| Area | Status | Evidence | Required next action |")
$validationLines.Add("| --- | --- | --- | --- |")

if ($runtimeSlotEvidence) {
    if ("$($runtimeSlotEvidence.filesReady)" -eq "True" -or "$($runtimeSlotEvidence.filesReady)" -eq "true") {
        Add-ValidationRow -Lines $validationLines -Area "Installed runtime slot evidence" -Status "ready" -Evidence "slot=$($runtimeSlotEvidence.runtimeSlotId); pack=$($runtimeSlotEvidence.runtimePackUsabilityStatus); patch=$($runtimeSlotEvidence.patchCompatibilityStatus)" -RequiredNextAction "Use as the installed-slot baseline for subsequent launch validation."
    } else {
        Add-ValidationRow -Lines $validationLines -Area "Installed runtime slot evidence" -Status "not-ready" -Evidence "slot=$($runtimeSlotEvidence.runtimeSlotId); problem=$($runtimeSlotEvidence.readinessProblem)" -RequiredNextAction "Resolve readiness problem before launch/release classification."
    }
} else {
    Add-ValidationRow -Lines $validationLines -Area "Installed runtime slot evidence" -Status "missing" -Evidence "current_runtime_slot.json missing or unreadable" -RequiredNextAction "Download or redownload selected version to regenerate runtime slot evidence."
}

if ($runtimeSlotEvidence) {
    $slotMarkerPck = "$($runtimeSlotEvidence.pckSha256)"
    $slotMarkerSourceAssembly = "$($runtimeSlotEvidence.sourceAssemblySha256)"
    $runtimePackSourcePck = if ($runtimePackManifest) { "$($runtimePackManifest.sourcePckSha256)" } else { "" }
    $slotPckMatchesSelectedFile = -not [string]::IsNullOrWhiteSpace($slotMarkerPck) -and $slotMarkerPck -eq $runtimeSlotPckActualSha256
    $slotPckMatchesRuntimePackSource = -not [string]::IsNullOrWhiteSpace($slotMarkerPck) -and -not [string]::IsNullOrWhiteSpace($runtimePackSourcePck) -and $slotMarkerPck -eq $runtimePackSourcePck
    $slotPckFresh = $slotPckMatchesSelectedFile -or $slotPckMatchesRuntimePackSource
    $slotSourceAssemblyFresh = -not [string]::IsNullOrWhiteSpace($slotMarkerSourceAssembly) -and $slotMarkerSourceAssembly -eq $runtimeSlotSourceAssemblyActualSha256
    if ($slotPckFresh -and $slotSourceAssemblyFresh) {
        Add-ValidationRow -Lines $validationLines -Area "Runtime slot marker matches selected files" -Status "fresh" -Evidence "markerPck=$slotMarkerPck; actualPck=$runtimeSlotPckActualSha256; runtimePackSourcePck=$runtimePackSourcePck; pckMatchesSelectedFile=$slotPckMatchesSelectedFile; pckMatchesRuntimePackSource=$slotPckMatchesRuntimePackSource; markerSourceAssembly=$slotMarkerSourceAssembly; actualSourceAssembly=$runtimeSlotSourceAssemblyActualSha256" -RequiredNextAction "Use marker hashes as proof that current_runtime_slot.json still describes selected source files; for Android-patched PCKs, use runtime/cache validation for the mounted PCK hash."
    } else {
        Add-ValidationRow -Lines $validationLines -Area "Runtime slot marker matches selected files" -Status "stale-or-unreadable" -Evidence "pckFresh=$slotPckFresh; sourceAssemblyFresh=$slotSourceAssemblyFresh; markerPck=$slotMarkerPck; actualPck=$runtimeSlotPckActualSha256; runtimePackSourcePck=$runtimePackSourcePck; pckMatchesSelectedFile=$slotPckMatchesSelectedFile; pckMatchesRuntimePackSource=$slotPckMatchesRuntimePackSource; markerSourceAssembly=$slotMarkerSourceAssembly; actualSourceAssembly=$runtimeSlotSourceAssemblyActualSha256" -RequiredNextAction "Redownload selected version or regenerate runtime-slot evidence before launching/classifying branch assets."
    }
} else {
    Add-ValidationRow -Lines $validationLines -Area "Runtime slot marker matches selected files" -Status "missing" -Evidence "current_runtime_slot.json missing or unreadable" -RequiredNextAction "Download or redownload selected version to regenerate selected-file hash evidence."
}

if ($runtimeSlotEvidence -and $runtimeValidation) {
    if ($installedRuntimeComparison.Matched) {
        Add-ValidationRow -Lines $validationLines -Area "Installed slot matches runtime patch validation" -Status "matched" -Evidence "installedSlot=$($installedRuntimeComparison.InstalledRuntimeSlotId); validationSlot=$($installedRuntimeComparison.ValidatedRuntimeSlotId); runtimePackSourceSlot=$($installedRuntimeComparison.RuntimePackSourceSlot); branch=$($installedRuntimeComparison.InstalledBranch); installedPck=$($installedRuntimeComparison.InstalledPck); validationPck=$($installedRuntimeComparison.ValidatedPck); runtimePackSourcePck=$($installedRuntimeComparison.RuntimePackSourcePck); cachePck=$runtimeCachePck; sourceAssembly=$($installedRuntimeComparison.InstalledSourceAssembly)" -RequiredNextAction "Use this pairing as proof that download-time readiness and launch-time patch validation refer to the same matched runtime unit."
    } else {
        Add-ValidationRow -Lines $validationLines -Area "Installed slot matches runtime patch validation" -Status "mismatch" -Evidence "installedSlot=$($installedRuntimeComparison.InstalledRuntimeSlotId); validationSlot=$($installedRuntimeComparison.ValidatedRuntimeSlotId); runtimePackSourceSlot=$($installedRuntimeComparison.RuntimePackSourceSlot); installedBranch=$($installedRuntimeComparison.InstalledBranch); validationBranch=$($installedRuntimeComparison.ValidatedBranch); installedPck=$($installedRuntimeComparison.InstalledPck); validationPck=$($installedRuntimeComparison.ValidatedPck); runtimePackSourcePck=$($installedRuntimeComparison.RuntimePackSourcePck); cachePck=$runtimeCachePck; installedSourceAssembly=$($installedRuntimeComparison.InstalledSourceAssembly); validationSourceAssembly=$($installedRuntimeComparison.ValidatedSourceAssembly); slotDirect=$($installedRuntimeComparison.SlotMatchesRuntimeValidation); slotRuntimePackSource=$($installedRuntimeComparison.SlotMatchesRuntimePackSource); pckDirect=$($installedRuntimeComparison.PckMatchesRuntimeValidation); pckRuntimePackSource=$($installedRuntimeComparison.PckMatchesRuntimePackSource); validationPckCache=$($installedRuntimeComparison.RuntimeValidationPckMatchesCache)" -RequiredNextAction "Treat runtime evidence as stale or mixed until the selected version is redownloaded and relaunched."
    }
}

if ($runtimeValidation) {
    $runtimeStatus = "$($runtimeValidation.status)"
    if ($runtimeStatus -eq "passed") {
        Add-ValidationRow -Lines $validationLines -Area "Runtime patch validation" -Status "passed" -Evidence "status=$runtimeStatus; slot=$($runtimeValidation.runtimeSlotId); branch=$($runtimeValidation.selectedBranch)" -RequiredNextAction "Keep as launch evidence for this exact runtime slot."
    } elseif ($runtimeStatus -eq "passed_with_noncritical_failures") {
        Add-ValidationRow -Lines $validationLines -Area "Runtime patch validation" -Status "review" -Evidence "status=$runtimeStatus; failed=$($runtimeValidation.failedPatchCount); slot=$($runtimeValidation.runtimeSlotId)" -RequiredNextAction "Review failureMessages before release classification."
    } elseif ($runtimeStatus -eq "critical_failed") {
        Add-ValidationRow -Lines $validationLines -Area "Runtime patch validation" -Status "blocked" -Evidence "status=$runtimeStatus; failed=$($runtimeValidation.failedPatchCount); slot=$($runtimeValidation.runtimeSlotId)" -RequiredNextAction "Fix patch/runtime compatibility before marking this branch playable."
    } else {
        Add-ValidationRow -Lines $validationLines -Area "Runtime patch validation" -Status "unknown" -Evidence "status=$runtimeStatus; slot=$($runtimeValidation.runtimeSlotId)" -RequiredNextAction "Inspect last_runtime_patch_validation.json."
    }

    if ("$($runtimeValidation.selectedPckSha256)" -eq $runtimeCachePck -and "$($runtimeValidation.activeAndroidAssemblySha256)" -eq $runtimeCachePublishAssembly) {
        Add-ValidationRow -Lines $validationLines -Area "Prepared cache matches runtime" -Status "matched" -Evidence "PCK and active sts2.dll hashes match runtime validation marker; nativeRuntimeId=$runtimeCacheId" -RequiredNextAction "Use with launch screenshot/log evidence for branch classification."
        Add-ValidationRow -Lines $validationLines -Area "Canonical slot bound to native cache identity" -Status "bound" -Evidence "canonicalSlot=$($runtimeValidation.runtimeSlotId); nativeRuntimeId=$runtimeCacheId; branch=$runtimeCacheBranch; PCK/active assembly matched" -RequiredNextAction "Treat native cache identity as evidence for this canonical runtime slot."
    } else {
        Add-ValidationRow -Lines $validationLines -Area "Prepared cache matches runtime" -Status "mismatch-or-missing" -Evidence "runtimePck=$($runtimeValidation.selectedPckSha256); cachePck=$runtimeCachePck; runtimeActive=$($runtimeValidation.activeAndroidAssemblySha256); cacheActive=$runtimeCachePublishAssembly" -RequiredNextAction "Restart app or investigate native cache routing before asset diagnosis."
        Add-ValidationRow -Lines $validationLines -Area "Canonical slot bound to native cache identity" -Status "unbound" -Evidence "canonicalSlot=$($runtimeValidation.runtimeSlotId); nativeRuntimeId=$runtimeCacheId; branch=$runtimeCacheBranch" -RequiredNextAction "Do not use native cache evidence for branch classification until cache/runtime hashes match."
    }

    if ($saveOriginComparison.Matched) {
        Add-ValidationRow -Lines $validationLines -Area "Steam Cloud Push save-origin safety" -Status "matched" -Evidence "saveOriginAction=$saveOriginAction; slot=$saveOriginRuntimeSlotId; runtimePlayable=$saveOriginRuntimePlayable; savesVerified=$saveOriginRuntimeVerified; pckDirect=$($saveOriginComparison.PckMatchesRuntime); pckRuntimePackSource=$($saveOriginComparison.PckMatchesRuntimePackSource)" -RequiredNextAction "Push still requires user intent and backup evidence; this only proves selected-runtime save origin."
    } else {
        Add-ValidationRow -Lines $validationLines -Area "Steam Cloud Push save-origin safety" -Status "do-not-push" -Evidence "identityMatches=$($saveOriginComparison.IdentityMatches); runtimePlayable=$saveOriginRuntimePlayable; savesVerified=$saveOriginRuntimeVerified; saveSlot=$saveOriginRuntimeSlotId; runtimeSlot=$($runtimeValidation.runtimeSlotId); savePck=$saveOriginPck; runtimePck=$($runtimeValidation.selectedPckSha256); runtimePackSourcePck=$($saveOriginComparison.RuntimePackSourcePck); pckDirect=$($saveOriginComparison.PckMatchesRuntime); pckRuntimePackSource=$($saveOriginComparison.PckMatchesRuntimePackSource)" -RequiredNextAction "Pull from Cloud for a playable selected runtime and verify local saves before any Push."
    }
} else {
    Add-ValidationRow -Lines $validationLines -Area "Runtime patch validation" -Status "missing" -Evidence "last_runtime_patch_validation.json missing or unreadable" -RequiredNextAction "Launch selected branch and recapture evidence."
    Add-ValidationRow -Lines $validationLines -Area "Prepared cache matches runtime" -Status "unknown" -Evidence "runtime validation marker unavailable; cacheBranch=$runtimeCacheBranch; cacheRuntimeId=$runtimeCacheId" -RequiredNextAction "Capture runtime validation before diagnosing branch bleed."
    Add-ValidationRow -Lines $validationLines -Area "Canonical slot bound to native cache identity" -Status "unknown" -Evidence "runtime validation marker unavailable; nativeRuntimeId=$runtimeCacheId" -RequiredNextAction "Capture runtime validation before binding native cache evidence to a canonical slot."
    Add-ValidationRow -Lines $validationLines -Area "Steam Cloud Push save-origin safety" -Status "do-not-push" -Evidence "runtime validation marker unavailable; saveOriginAction=$saveOriginAction" -RequiredNextAction "Do not Push until selected-runtime Pull/save-origin evidence exists."
}

if ($runtimeCacheSource -eq "runtime-pack") {
    Add-ValidationRow -Lines $validationLines -Area "Runtime source" -Status "runtime-pack" -Evidence "cacheBranch=$runtimeCacheBranch; requiresPack=$runtimeCacheBranchRequiresRuntimePack; source=$runtimeCacheSource; selectedSourceSha=$runtimeCacheSelectedSource" -RequiredNextAction "Confirm runtime pack manifest/report hashes in diagnostics/runtime-marker-contents.txt."
    if ($runtimeValidation -and $runtimePackManifest) {
        if ($runtimePackComparison.Matched) {
            Add-ValidationRow -Lines $validationLines -Area "Selected runtime pack manifest" -Status "matched" -Evidence "pack=$($runtimePackManifest.packId); sourceSlot=$($runtimePackManifest.sourceRuntimeSlotId); runtimeSlot=$($runtimeValidation.runtimeSlotId); sourcePck=$($runtimePackManifest.sourcePckSha256); runtimePck=$($runtimeValidation.selectedPckSha256); cachePck=$runtimeCachePck; slotDirect=$($runtimePackComparison.SlotMatchesRuntimeValidation); slotInstalled=$($runtimePackComparison.SlotMatchesInstalledSlot); pckDirect=$($runtimePackComparison.PckMatchesRuntimeValidation); pckInstalledSource=$($runtimePackComparison.PckMatchesInstalledSource); pckCache=$($runtimePackComparison.PckMatchesSelectedCache); patch=$($runtimePackManifest.patchValidationStatus); clean=$($runtimePackManifest.generatedFromCleanDirectory); runtimePackClosedDllSet=$($runtimePackClosedDllSet.Status); reportMatches=$($runtimePackComparison.ReportMatches)" -RequiredNextAction "Keep compatibility manifest/report with this runtime evidence."
        } else {
            Add-ValidationRow -Lines $validationLines -Area "Selected runtime pack manifest" -Status "mismatch" -Evidence "packSlot=$($runtimePackManifest.sourceRuntimeSlotId); runtimeSlot=$($runtimeValidation.runtimeSlotId); installedSlot=$($runtimeSlotEvidence.runtimeSlotId); packPck=$($runtimePackManifest.sourcePckSha256); runtimePck=$($runtimeValidation.selectedPckSha256); installedPck=$($runtimeSlotEvidence.pckSha256); cachePck=$runtimeCachePck; slotDirect=$($runtimePackComparison.SlotMatchesRuntimeValidation); slotInstalled=$($runtimePackComparison.SlotMatchesInstalledSlot); pckDirect=$($runtimePackComparison.PckMatchesRuntimeValidation); pckInstalledSource=$($runtimePackComparison.PckMatchesInstalledSource); pckCache=$($runtimePackComparison.PckMatchesSelectedCache); packAndroid=$($runtimePackManifest.androidAssemblySha256); runtimeAndroid=$($runtimeValidation.activeAndroidAssemblySha256); patch=$($runtimePackManifest.patchValidationStatus); clean=$($runtimePackManifest.generatedFromCleanDirectory); runtimePackClosedDllSet=$($runtimePackClosedDllSet.Status); reportMatches=$($runtimePackComparison.ReportMatches); $($runtimePackClosedDllSet.Evidence)" -RequiredNextAction "Reject this runtime pack for release; rebuild or redownload selected branch/runtime pack."
        }
    } elseif ($runtimePackManifest) {
        Add-ValidationRow -Lines $validationLines -Area "Selected runtime pack manifest" -Status "needs-runtime-validation" -Evidence "pack=$($runtimePackManifest.packId); slot=$($runtimePackManifest.sourceRuntimeSlotId); patch=$($runtimePackManifest.patchValidationStatus)" -RequiredNextAction "Launch selected branch and recapture runtime validation before release classification."
    } else {
        Add-ValidationRow -Lines $validationLines -Area "Selected runtime pack manifest" -Status "missing-or-unreadable" -Evidence "runtimePackDirectory=$runtimePackDirectory" -RequiredNextAction "Inspect runtime-pack directory and regenerate selected runtime pack."
    }
} elseif ($runtimeCacheSource -eq "selected-game") {
    Add-ValidationRow -Lines $validationLines -Area "Runtime source" -Status "selected-game" -Evidence "cacheBranch=$runtimeCacheBranch; requiresPack=$runtimeCacheBranchRequiresRuntimePack; source=$runtimeCacheSource; selectedSourceSha=$runtimeCacheSelectedSource" -RequiredNextAction "For non-public branches, this should be treated as invalid; regenerate runtime-pack evidence."
    Add-ValidationRow -Lines $validationLines -Area "Selected runtime pack manifest" -Status "not-active" -Evidence "runtimeSource=$runtimeCacheSource; runtimePackDirectory=$runtimePackDirectory" -RequiredNextAction "No active runtime-pack manifest to classify for this launch."
} elseif ($runtimeCacheSource -eq "no-usable-runtime") {
    Add-ValidationRow -Lines $validationLines -Area "Runtime source" -Status "blocked-no-usable-runtime" -Evidence "cacheBranch=$runtimeCacheBranch; requiresPack=$runtimeCacheBranchRequiresRuntimePack; source=$runtimeCacheSource; selectedSourceSha=$runtimeCacheSelectedSource" -RequiredNextAction "Install or regenerate a selected-runtime-matched runtime pack before classifying non-public branch assets."
    Add-ValidationRow -Lines $validationLines -Area "Selected runtime pack manifest" -Status "missing-or-rejected" -Evidence "runtimeSource=$runtimeCacheSource; runtimePackDirectory=$runtimePackDirectory" -RequiredNextAction "Inspect compatibility.json/patch_validation.json and selected PCK/source assembly hashes."
} else {
    Add-ValidationRow -Lines $validationLines -Area "Runtime source" -Status "missing-or-unknown" -Evidence "cacheBranch=$runtimeCacheBranch; source=$runtimeCacheSource" -RequiredNextAction "Inspect current_runtime_cache.txt and native logcat."
    Add-ValidationRow -Lines $validationLines -Area "Selected runtime pack manifest" -Status "unknown" -Evidence "runtimeSource=$runtimeCacheSource; runtimePackDirectory=$runtimePackDirectory" -RequiredNextAction "Resolve runtime source before classifying runtime-pack compatibility."
}

if ($IncludeRawLogcat) {
    Add-ValidationRow -Lines $validationLines -Area "Artifact hygiene" -Status "raw-logcat-present" -Evidence "logs/logcat-full.txt requested" -RequiredNextAction "Manually redact before sharing publicly."
} else {
    Add-ValidationRow -Lines $validationLines -Area "Artifact hygiene" -Status "shareable-after-review" -Evidence "raw full logcat omitted by default" -RequiredNextAction "Still manually review diagnostics for paths/account metadata before sharing."
}

$runtimeMarkerContentsPath = Join-Path $diagnosticsDir "runtime-marker-contents.txt"
$runtimeMarkerContents = ""
if (Test-Path -LiteralPath $runtimeMarkerContentsPath) {
    $runtimeMarkerContents = Get-Content -Raw -LiteralPath $runtimeMarkerContentsPath
}

$runtimeSlotReady = $runtimeSlotEvidence -and (Test-EvidenceTrue -Value $runtimeSlotEvidence.filesReady)
$installedRuntimeMatches = $installedRuntimeComparison.Matched
$cacheMatchesRuntime = Test-RuntimeCacheMatchesValidation -RuntimeValidation $runtimeValidation -RuntimeCacheBranch $runtimeCacheBranch -RuntimeCachePck $runtimeCachePck -RuntimeCacheSelectedSource $runtimeCacheSelectedSource -RuntimeCachePublishAssembly $runtimeCachePublishAssembly
$runtimePackMatchesRuntime = $runtimePackComparison.Matched
$saveOriginMatchesRuntime = $saveOriginComparison.IdentityMatches

$validationLines.Add("")
$validationLines.Add("## Mixed/split asset hypothesis matrix")
$validationLines.Add("")
$validationLines.Add("Status values: confirmed, ruled out, likely, unknown, needs device-only validation.")
$validationLines.Add("")
$validationLines.Add("| Hypothesis | Status | Evidence | Next proof |")
$validationLines.Add("| --- | --- | --- | --- |")

if ($runtimeMarkerContents -match "Depot manifests inherited from public count:\s*[1-9]" -or $runtimeMarkerContents -match "Depot manifests matching public count:\s*[1-9]") {
    Add-HypothesisRow -Lines $validationLines -Hypothesis "Steam branch partial/shared content" -Status "likely" -Evidence "Branch marker reports one or more public-inherited or public-matching depot manifests." -NextProof "Compare public and selected branch depot manifests and asset hashes before treating matching assets as launcher bleed."
} elseif ($runtimeMarkerContents -match "Depot manifests differing from public count:\s*[1-9]") {
    Add-HypothesisRow -Lines $validationLines -Hypothesis "Steam branch partial/shared content" -Status "unknown" -Evidence "Branch marker reports selected-branch-specific depot differences, but this does not prove every visible asset should differ." -NextProof "Compare the exact reported asset paths against public and selected branch PCK/file inventories."
} else {
    Add-HypothesisRow -Lines $validationLines -Hypothesis "Steam branch partial/shared content" -Status "unknown" -Evidence "No parsed public-vs-selected depot inheritance evidence found in captured marker contents." -NextProof "Refresh game versions, redownload selected branch, and capture branch marker provenance."
}

if ($runtimeSlotEvidence -and -not $runtimeSlotReady) {
    Add-HypothesisRow -Lines $validationLines -Hypothesis "Stale/incomplete downloader cache" -Status "confirmed" -Evidence "Installed runtime slot evidence is not ready: $($runtimeSlotEvidence.readinessProblem)" -NextProof "Force selected-version redownload and confirm current_runtime_slot.json becomes ready for the selected runtime."
} elseif ($installedRuntimeMatches) {
    Add-HypothesisRow -Lines $validationLines -Hypothesis "Stale/incomplete downloader cache" -Status "ruled out" -Evidence "Installed-slot marker matches runtime patch validation on branch/source assembly and on direct hashes or source-PCK plus Android-patched cache hash." -NextProof "Only reopen this hypothesis if later file hashes or branch markers change."
} else {
    Add-HypothesisRow -Lines $validationLines -Hypothesis "Stale/incomplete downloader cache" -Status "unknown" -Evidence "Installed-slot marker and runtime validation are missing or do not form a complete matched pair." -NextProof "Redownload selected version, launch it once, and recapture evidence."
}

if ($cacheMatchesRuntime) {
    Add-HypothesisRow -Lines $validationLines -Hypothesis "Wrong launch path" -Status "ruled out" -Evidence "Runtime validation branch/PCK/source/active Android assembly match the native runtime-cache marker." -NextProof "Pair this evidence with the visual observation screenshot for final classification."
} elseif ($runtimeValidation) {
    Add-HypothesisRow -Lines $validationLines -Hypothesis "Wrong launch path" -Status "confirmed" -Evidence "Runtime validation exists but does not match native cache marker: runtimeBranch=$($runtimeValidation.selectedBranch); cacheBranch=$runtimeCacheBranch; runtimePck=$($runtimeValidation.selectedPckSha256); cachePck=$runtimeCachePck." -NextProof "Inspect launcher routing and native Loading PCK from lines before diagnosing game-content causes."
} else {
    Add-HypothesisRow -Lines $validationLines -Hypothesis "Wrong launch path" -Status "unknown" -Evidence "Runtime validation marker is missing, so mounted runtime cannot be tied to selected branch." -NextProof "Launch selected branch and recapture last_runtime_patch_validation.json plus focused logcat."
}

if ($cacheMatchesRuntime) {
    Add-HypothesisRow -Lines $validationLines -Hypothesis "Shared assembly/runtime cache" -Status "ruled out" -Evidence "Publish-cache active sts2.dll hash matches the runtime validation active Android assembly hash." -NextProof "Only reopen if a later branch switch changes active assembly without changing runtime cache marker."
} elseif ($runtimeValidation) {
    Add-HypothesisRow -Lines $validationLines -Hypothesis "Shared assembly/runtime cache" -Status "confirmed" -Evidence "Runtime validation active Android assembly does not match publish-cache marker or cache marker is stale." -NextProof "Restart app and inspect native assembly-cache refresh logs."
} else {
    Add-HypothesisRow -Lines $validationLines -Hypothesis "Shared assembly/runtime cache" -Status "unknown" -Evidence "Runtime validation marker is missing." -NextProof "Launch selected branch and recapture runtime/cache markers."
}

Add-HypothesisRow -Lines $validationLines -Hypothesis "In-process branch switch reuse" -Status "needs device-only validation" -Evidence "This collector captures one device snapshot and cannot prove a public -> beta -> public -> beta restart sequence by itself." -NextProof "Run the branch-switch sequence without clearing app data and capture focused logcat around each launch."

if ($runtimePackMatchesRuntime -or ($runtimeValidation -and "$($runtimeValidation.runtimePackStatus)" -eq "usable")) {
    Add-HypothesisRow -Lines $validationLines -Hypothesis "Android PCK patch side effect" -Status "unknown" -Evidence "Runtime pack/validation evidence is matched, but raw pre-patch versus patched PCK hash comparison is not captured here." -NextProof "Capture raw downloaded PCK hash before Android patching and compare it to patched/runtime PCK hash."
} else {
    Add-HypothesisRow -Lines $validationLines -Hypothesis "Android PCK patch side effect" -Status "needs device-only validation" -Evidence "Runtime pack/validation evidence is missing or mismatched, so patch side effects cannot be isolated." -NextProof "First produce matched runtime-pack/cache evidence, then compare raw versus patched PCK hashes."
}

if ($cacheMatchesRuntime -and $runtimeValidation -and "$($runtimeValidation.status)" -eq "passed") {
    Add-HypothesisRow -Lines $validationLines -Hypothesis "Godot import/resource mismatch" -Status "unknown" -Evidence "Launcher path/cache evidence is matched, but no exact missing resource path or PCK inventory comparison is captured in this report." -NextProof "Pair visual/log asset errors with selected PCK directory listing for the exact resource paths."
} else {
    Add-HypothesisRow -Lines $validationLines -Hypothesis "Godot import/resource mismatch" -Status "needs device-only validation" -Evidence "Launcher runtime/cache proof is incomplete, so import/resource diagnosis would be premature." -NextProof "First rule out launch path and runtime cache mismatch."
}

if ($runtimeValidation -and -not [string]::IsNullOrWhiteSpace($saveOriginText) -and -not $saveOriginMatchesRuntime) {
    Add-HypothesisRow -Lines $validationLines -Hypothesis "Save/config asset reference mismatch" -Status "confirmed" -Evidence "Save-origin marker does not match selected runtime: saveSlot=$saveOriginRuntimeSlotId; runtimeSlot=$($runtimeValidation.runtimeSlotId)." -NextProof "Test selected branch with clean local saves, then repeat after Pull/restore."
} elseif ($saveOriginMatchesRuntime) {
    Add-HypothesisRow -Lines $validationLines -Hypothesis "Save/config asset reference mismatch" -Status "unknown" -Evidence "Save-origin marker matches selected runtime, but matching origin does not prove save contents are compatible with changed beta assets." -NextProof "Compare clean-save launch against cloud/restored-save launch and capture asset error paths."
} else {
    Add-HypothesisRow -Lines $validationLines -Hypothesis "Save/config asset reference mismatch" -Status "unknown" -Evidence "No selected-runtime save-origin comparison is available." -NextProof "Capture clean-save and cloud-restored-save launches separately."
}

$validationLines.Add("")
$validationLines.Add("Required evidence files:")
$validationLines.Add("- diagnostics/last_runtime_patch_validation.json")
$validationLines.Add("- diagnostics/current_runtime_slot.json")
$validationLines.Add("- diagnostics/current_runtime_cache.txt")
$validationLines.Add("- diagnostics/current_android_save_origin.txt")
$validationLines.Add("- diagnostics/selected_runtime_pack_compatibility.json")
$validationLines.Add("- diagnostics/selected_runtime_pack_patch_validation.json")
$validationLines.Add("- diagnostics/runtime-marker-contents.txt")
$validationLines.Add("- diagnostics/runtime-hashes.txt")
$validationLines.Add("- logs/logcat-runtime-filtered.txt")

Save-Text -Path (Join-Path $outputDir "validation-report.md") -Text ($validationLines -join [Environment]::NewLine)

$summaryLines = [System.Collections.Generic.List[string]]::new()
$summaryLines.Add("# Multi-version runtime evidence summary")
$summaryLines.Add("")
$summaryLines.Add("Generated UTC: $generatedUtc")
$summaryLines.Add("Package: $PackageName")
$summaryLines.Add("Run label: $(if ([string]::IsNullOrWhiteSpace($safeRunLabel)) { '<none>' } else { $safeRunLabel })")
$summaryLines.Add("Output: $outputDir")
$summaryLines.Add("Collector boundary: This collector is read-only and does not mutate Steam Cloud or app data.")
$summaryLines.Add("")

if ($runtimeValidation) {
    $summaryLines.Add("Runtime patch validation status: $($runtimeValidation.status)")
    if ($runtimeSlotEvidence) {
        $summaryLines.Add("Installed runtime slot evidence ID: $($runtimeSlotEvidence.runtimeSlotId)")
        $summaryLines.Add("Installed runtime slot evidence ready: $($runtimeSlotEvidence.filesReady)")
    } else {
        $summaryLines.Add("Installed runtime slot evidence: missing or unreadable")
    }
    $summaryLines.Add("Runtime slot ID: $($runtimeValidation.runtimeSlotId)")
    $summaryLines.Add("Runtime selected branch: $($runtimeValidation.selectedBranch)")
    $summaryLines.Add("Runtime selected version: $($runtimeValidation.selectedVersion)")
    $summaryLines.Add("Runtime selected PCK SHA256: $($runtimeValidation.selectedPckSha256)")
    $summaryLines.Add("Runtime selected source sts2.dll SHA256: $($runtimeValidation.selectedSourceAssemblySha256)")
    $summaryLines.Add("Runtime active Android sts2.dll SHA256: $($runtimeValidation.activeAndroidAssemblySha256)")
    $summaryLines.Add("Runtime pack ID: $($runtimeValidation.runtimePackId)")
    $summaryLines.Add("Runtime pack status: $($runtimeValidation.runtimePackStatus)")
    if ($runtimePackManifest) {
        $summaryLines.Add("Selected runtime pack manifest ID: $($runtimePackManifest.packId)")
        $summaryLines.Add("Selected runtime pack manifest slot ID: $($runtimePackManifest.sourceRuntimeSlotId)")
        $summaryLines.Add("Selected runtime pack manifest patch status: $($runtimePackManifest.patchValidationStatus)")
        $summaryLines.Add("Selected runtime pack generated from clean directory: $($runtimePackManifest.generatedFromCleanDirectory)")
        $summaryLines.Add("Selected runtime pack support assemblies: $($runtimePackManifest.supportAssemblies -join ', ')")
        $summaryLines.Add("Selected runtime pack closed DLL set: $($runtimePackClosedDllSet.Status)")
    } else {
        $summaryLines.Add("Selected runtime pack manifest: missing or inactive")
    }
    $summaryLines.Add("Runtime patch counts: applied=$($runtimeValidation.appliedPatchCount) failed=$($runtimeValidation.failedPatchCount) total=$($runtimeValidation.totalPatchCount)")
} else {
    $summaryLines.Add("Runtime patch validation status: missing or unreadable")
}

$summaryLines.Add("")
if (-not [string]::IsNullOrWhiteSpace($saveOriginText)) {
    $summaryLines.Add("Save origin action: $saveOriginAction")
    $summaryLines.Add("Save origin branch: $saveOriginBranch")
    $summaryLines.Add("Save origin runtime slot ID: $saveOriginRuntimeSlotId")
    $summaryLines.Add("Save origin runtime playable: $saveOriginRuntimePlayable")
    $summaryLines.Add("Save origin selected-runtime saves verified: $saveOriginRuntimeVerified")
} else {
    $summaryLines.Add("Save origin marker: missing")
}

$summaryLines.Add("")
$summaryLines.Add("Evidence files:")
$summaryLines.Add("- run-metadata.json")
$summaryLines.Add("- diagnostics/runtime-marker-files.txt")
$summaryLines.Add("- diagnostics/runtime-marker-contents.txt")
$summaryLines.Add("- diagnostics/runtime-hashes.txt")
$summaryLines.Add("- diagnostics/runtime-tree.txt")
$summaryLines.Add("- diagnostics/current_runtime_slot.json")
$summaryLines.Add("- diagnostics/selected_runtime_pack_compatibility.json")
$summaryLines.Add("- diagnostics/selected_runtime_pack_patch_validation.json")
$summaryLines.Add("- logs/logcat-runtime-filtered.txt")
$summaryLines.Add("- validation-report.md")
$summaryLines.Add("")
$summaryLines.Add("Hypothesis matrix:")
$summaryLines.Add("- See validation-report.md for the mixed/split asset hypothesis matrix covering Steam branch partial/shared content, stale downloader cache, wrong launch path, shared assembly/runtime cache, in-process branch switch reuse, Android PCK patch side effects, Godot import/resource mismatch, and save/config asset reference mismatch.")
$summaryLines.Add("")
$summaryLines.Add("Classification guidance:")
$summaryLines.Add("- If selected branch, selected PCK hash, runtime pack ID/status, and active Android sts2.dll hash all line up, launcher routing/cache bleed is unlikely.")
$summaryLines.Add("- If canonical slot binding to native cache identity is unbound, do not use native cache evidence to classify branch bleed.")
$summaryLines.Add("- If runtime patch validation is critical_failed, inspect failureMessages before treating assets or saves as the cause.")
$summaryLines.Add("- If selected runtime-pack manifest slot/hash evidence does not match the runtime validation marker, reject the runtime pack and rebuild it.")
$summaryLines.Add("- If save-origin runtime slot ID does not match runtime validation slot ID, do not Push to Steam Cloud.")
$summaryLines.Add("- If runtime pack status is missing or not usable for a non-public branch, rebuild/download the selected branch and rerun validation.")

Save-Text -Path (Join-Path $outputDir "summary.md") -Text ($summaryLines -join [Environment]::NewLine)

Write-Host "Multi-version runtime evidence captured: $outputDir"
