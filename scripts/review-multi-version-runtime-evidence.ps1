param(
    [Parameter(Mandatory = $true)][string]$EvidenceDir,
    [switch]$RequirePublic,
    [switch]$RequirePublicBeta,
    [switch]$RequireBranchSwitch,
    [switch]$RequireSaveSafety,
    [switch]$RequireLaunchAttempt,
    [switch]$RequireResolvedClassification,
    [int]$MaxLaunchAttemptAgeMinutes = 0,
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "evidence-launch-attempt-phases.ps1")

$resolvedEvidenceDir = (Resolve-Path -LiteralPath $EvidenceDir).ProviderPath
$failures = New-Object System.Collections.Generic.List[string]
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

function Require-NoPattern([string]$RelativePath, [string]$Description, [string]$Pattern) {
    $content = Read-EvidenceFile $RelativePath
    if ($null -eq $content) {
        return
    }

    if ($content -match $Pattern) {
        $failures.Add("$RelativePath - $Description - forbidden unresolved evidence pattern present: $Pattern")
        return
    }

    Add-Pass "$RelativePath - $Description"
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

function Require-LaunchAttemptFreshness([int]$MaxAgeMinutes) {
    if ($MaxAgeMinutes -le 0) {
        return
    }

    $markerContent = Read-EvidenceFile "diagnostics/last_launch_attempt.txt"
    $metadataContent = Read-EvidenceFile "run-metadata.json"
    if ($null -eq $markerContent -or $null -eq $metadataContent) {
        return
    }

    $markerMatch = [regex]::Match($markerContent, "(?m)^UTC:\s*(.+?)\s*$")
    if (-not $markerMatch.Success) {
        $failures.Add("diagnostics/last_launch_attempt.txt - launch-attempt marker freshness - missing UTC timestamp")
        return
    }

    try {
        $metadata = $metadataContent | ConvertFrom-Json
    } catch {
        $failures.Add("run-metadata.json - launch-attempt marker freshness - invalid JSON: $($_.Exception.Message)")
        return
    }

    if ([string]::IsNullOrWhiteSpace([string]$metadata.generatedUtc)) {
        $failures.Add("run-metadata.json - launch-attempt marker freshness - missing generatedUtc")
        return
    }

    try {
        $markerUtc = [datetimeoffset]::Parse($markerMatch.Groups[1].Value.Trim(), [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::AssumeUniversal)
    } catch {
        $failures.Add("diagnostics/last_launch_attempt.txt - launch-attempt marker freshness - invalid UTC timestamp: $($markerMatch.Groups[1].Value.Trim())")
        return
    }

    try {
        $captureUtc = [datetimeoffset]::Parse([string]$metadata.generatedUtc, [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::AssumeUniversal)
    } catch {
        $failures.Add("run-metadata.json - launch-attempt marker freshness - invalid generatedUtc: $($metadata.generatedUtc)")
        return
    }

    $age = $captureUtc - $markerUtc
    if ($age.TotalMinutes -lt -5) {
        $failures.Add("diagnostics/last_launch_attempt.txt - launch-attempt marker freshness - marker timestamp is more than 5 minutes after capture time")
        return
    }

    if ($age.TotalMinutes -gt $MaxAgeMinutes) {
        $failures.Add("diagnostics/last_launch_attempt.txt - launch-attempt marker older than freshness window: age=$([math]::Round($age.TotalMinutes, 2)) minutes max=$MaxAgeMinutes minutes")
        return
    }

    Add-Pass "diagnostics/last_launch_attempt.txt - launch-attempt marker is fresh enough"
}

Require-Pattern "summary.md" "has multi-version evidence summary" "Multi-version runtime evidence"
Require-JsonPattern "run-metadata.json" "has readable run metadata" "\{"
Require-JsonPattern "run-metadata.json" "metadata identifies collector" '(?i)"collector"\s*:\s*"capture-multi-version-runtime-evidence\.ps1"'
Require-JsonPattern "run-metadata.json" "metadata records read-only collector boundary" '(?i)"readOnly"\s*:\s*true'
Require-Pattern "summary.md" "states read-only collector boundary" "does not mutate Steam Cloud or app data"
Require-Pattern "validation-report.md" "has validation report" "Multi-version runtime validation report"
Require-Pattern "validation-report.md" "has hypothesis matrix" "Mixed/split asset hypothesis matrix"
Require-Pattern "validation-report.md" "uses fixed hypothesis statuses" "confirmed, ruled out, likely, unknown, needs device-only validation"
Require-Pattern "validation-report.md" "classifies Steam branch partial/shared content" "Steam branch partial/shared content"
Require-Pattern "validation-report.md" "classifies stale downloader cache" "Stale/incomplete downloader cache"
Require-Pattern "validation-report.md" "classifies wrong launch path" "Wrong launch path"
Require-Pattern "validation-report.md" "classifies shared assembly/runtime cache" "Shared assembly/runtime cache"
Require-Pattern "validation-report.md" "classifies in-process branch switch reuse" "In-process branch switch reuse"
Require-Pattern "validation-report.md" "classifies Android PCK patch side effects" "Android PCK patch side effect"
Require-Pattern "validation-report.md" "classifies Godot import/resource mismatch" "Godot import/resource mismatch"
Require-Pattern "validation-report.md" "classifies save/config mismatch" "Save/config asset reference mismatch"
Require-Pattern "validation-report.md" "reports runtime-slot marker freshness" "Runtime slot marker matches selected files"
Require-Pattern "validation-report.md" "reports prepared runtime cache matching" "Prepared cache matches runtime"
Require-Pattern "validation-report.md" "reports canonical runtime/cache binding" "Canonical slot bound to native cache identity"

Require-JsonPattern "diagnostics/current_runtime_slot.json" "has readable runtime-slot marker" "\{"
Require-Pattern "diagnostics/current_runtime_cache.txt" "has prepared runtime-cache marker" "Runtime ID:"
Require-Pattern "diagnostics/current_runtime_cache.txt" "has selected branch in runtime-cache marker" "Selected branch:"
Require-Pattern "diagnostics/current_runtime_cache.txt" "has active publish-cache assembly hash" "Publish cache active sts2\.dll SHA256:"
Require-Pattern "logs/logcat-runtime-filtered.txt" "has focused runtime logcat" "Loading PCK from:|Selected PCK|Runtime slot evidence|Assembly cache"

if ($RequireLaunchAttempt) {
    $successfulPhaseRegex = Get-LaunchAttemptSuccessfulPhaseRegex
    $rejectedProofPhaseRegex = Get-LaunchAttemptRejectedProofPhaseRegex
    Require-Pattern "summary.md" "summarizes launch-attempt marker" "Launch attempt phase:"
    Require-Pattern "summary.md" "summarizes launch-attempt timestamp" "Launch attempt UTC:"
    Require-Pattern "summary.md" "summarizes launch-attempt ID" "(?m)^Launch attempt ID:\s*[0-9a-fA-F]{32}\s*$"
    Require-Pattern "summary.md" "summarizes launch-attempt action" "(?m)^Launch attempt action:\s*(normal|safe)\s*$"
    Require-Pattern "summary.md" "summarizes launch-attempt source" "(?m)^Launch attempt source:\s*(button|auto-launch|automation)\s*$"
    Require-Pattern "summary.md" "summarizes successful launch handoff phase" "(?m)^Launch attempt phase:\s*$successfulPhaseRegex\s*$"
    Require-Pattern "summary.md" "summarizes launch-attempt timings" "Launch attempt timings ms:"
    Require-Pattern "validation-report.md" "launch-attempt marker is captured" "\|\s*Start Game launch-attempt marker\s*\|\s*captured\s*\|"
    Require-Pattern "validation-report.md" "launch attempt matches runtime validation" "\|\s*Launch attempt matches runtime validation\s*\|\s*matched\s*\|"
    Require-NoPattern "logs/logcat-runtime-filtered.txt" "does not accept NativeFallback or Android fatal crash logs as launch-attempt proof" "(?i)\bNativeFallback(Activity)?\b|FATAL EXCEPTION|AndroidRuntime.*FATAL|ANR in"
    Require-Pattern "diagnostics/runtime-marker-files.txt" "has launch-attempt marker file evidence" "last_launch_attempt\.txt"
    Require-Pattern "diagnostics/runtime-marker-contents.txt" "has launch-attempt marker content evidence" "last_launch_attempt\.txt|StS2 (?:Launcher|Mobile) launch attempt"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "has launch-attempt header" "StS2 (?:Launcher|Mobile) launch attempt"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records launch-attempt timestamp" "(?m)^UTC:\s*\d{4}-\d{2}-\d{2}T"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records launch-attempt ID" "(?m)^Attempt ID:\s*[0-9a-fA-F]{32}\s*$"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records launch action" "(?m)^Action:\s*(normal|safe)\s*$"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records launch source" "(?m)^Source:\s*(button|auto-launch|automation)\s*$"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records ready launch state" "Files ready:\s*true"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "uses prepared readiness" "Prepared readiness used:\s*true"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records launch-attempt phase" "Phase:"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records successful launch handoff phase" "(?m)^Phase:\s*$successfulPhaseRegex\s*$"
    Require-NoPattern "diagnostics/last_launch_attempt.txt" "does not treat failed launch handoff as success" "(?m)^Phase:\s*$rejectedProofPhaseRegex\s*$"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records launch readiness cache status" "Readiness cache status:"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records concrete launch readiness cache status" "(?m)^Readiness cache status:\s*(fresh|fresh-cached|memory-cache-hit)\s*$"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records launch attempt timing" "Launch attempt elapsed ms:"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records measured launch attempt timing" "(?m)^Launch attempt elapsed ms:\s*\d+\s*$"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records launch readiness timing" "Launch readiness elapsed ms:"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records measured launch readiness timing" "(?m)^Launch readiness elapsed ms:\s*\d+\s*$"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records mod readiness timing" "Mod readiness elapsed ms:"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records measured mod readiness timing" "(?m)^Mod readiness elapsed ms:\s*\d+\s*$"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records selected branch" "Selected branch:"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records selected game directory" "Game directory:"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records selected PCK path" "PCK path:"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records selected PCK hash" "PCK SHA256:"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records selected source assembly path" "Source sts2\.dll path:"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records selected source assembly hash" "Source sts2\.dll SHA256:"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records active Android assembly path" "Active Android sts2\.dll path:"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records active Android assembly hash" "Active Android sts2\.dll SHA256:"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records runtime-pack directory" "Runtime pack directory:"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records runtime-pack manifest path" "Runtime pack manifest path:"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records runtime-pack usability" "Runtime pack usable:"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records runtime cache marker presence" "Runtime cache marker present:"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records runtime patch-validation marker presence" "Runtime patch validation marker present:"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records patch compatibility marker path" "Patch compatibility marker path:"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records mod readiness phase" "Mod readiness phase:"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records concrete mod readiness cache status" "(?m)^Mod readiness cache status:\s*(not-needed-vanilla|fresh|fresh-cached|memory-cache-hit)\s*$"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records concrete mod play mode" "(?m)^Mod play mode:\s*(vanilla|modded)\s*$"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records numeric installed mod count" "(?m)^Mod installed count:\s*\d+\s*$"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records numeric enabled mod count" "(?m)^Mod enabled count:\s*\d+\s*$"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records numeric unsupported mod count" "(?m)^Mod unsupported count:\s*\d+\s*$"
    Require-Pattern "diagnostics/last_launch_attempt.txt" "records modded-save Cloud Push lock state" "(?m)^Modded save cloud push locked:\s*(true|false)\s*$"
    Require-LaunchAttemptFreshness $MaxLaunchAttemptAgeMinutes
}

if ($RequirePublic) {
    Require-Pattern "summary.md" "public review uses public run label" "Run label:\s*public"
    Require-JsonPattern "run-metadata.json" "public review uses public metadata label" '(?i)"runLabel"\s*:\s*"public"'
    Require-JsonPattern "diagnostics/current_runtime_slot.json" "public slot is selected" '(?i)"[^"]*branch[^"]*"\s*:\s*"public"'
    Require-Pattern "diagnostics/current_runtime_cache.txt" "public cache path is selected" "files/game/SlayTheSpire2\.pck|files\\game\\SlayTheSpire2\.pck"
}

if ($RequirePublicBeta) {
    Require-Pattern "summary.md" "public-beta review uses public-beta run label" "Run label:\s*public-beta"
    Require-JsonPattern "run-metadata.json" "public-beta review uses public-beta metadata label" '(?i)"runLabel"\s*:\s*"public-beta"'
    Require-JsonPattern "diagnostics/current_runtime_slot.json" "public-beta slot is selected" "public-beta"
    Require-JsonPattern "diagnostics/current_runtime_slot.json" "runtime slot is playable" '(?i)"playable"\s*:\s*true'
    Require-JsonPattern "diagnostics/current_runtime_slot.json" "runtime slot files are ready" '(?i)"filesReady"\s*:\s*true'
    Require-JsonPattern "diagnostics/current_runtime_slot.json" "runtime slot is runtime-compatible" '(?i)"runtimeCompatible"\s*:\s*true'
    Require-JsonPattern "diagnostics/current_runtime_slot.json" "runtime slot is patch-compatible" '(?i)"patchCompatible"\s*:\s*true'
    Require-Pattern "diagnostics/current_runtime_cache.txt" "public-beta cache path is selected" "game_versions/public-beta-|game_versions\\public-beta-"
    Require-JsonPattern "diagnostics/selected_runtime_pack_compatibility.json" "runtime pack manifest is readable" "\{"
    Require-JsonPattern "diagnostics/selected_runtime_pack_compatibility.json" "runtime pack was generated from clean directory" '(?i)"generatedFromCleanDirectory"\s*:\s*true'
    Require-JsonPattern "diagnostics/selected_runtime_pack_compatibility.json" "runtime pack declares support assemblies" '(?i)"supportAssemblies"\s*:'
    Require-JsonPattern "diagnostics/selected_runtime_pack_compatibility.json" "runtime pack declares support hashes" '(?i)"supportAssemblySha256"\s*:'
    Require-JsonPattern "diagnostics/selected_runtime_pack_patch_validation.json" "runtime pack validation report is readable" "\{"
    Require-AnyPattern "diagnostics/selected_runtime_pack_patch_validation.json" "runtime pack validation report passed" @(
        '(?i)"patchValidationStatus"\s*:\s*"passed"',
        '(?i)"status"\s*:\s*"passed"'
    )
    Require-Pattern "validation-report.md" "runtime pack closed DLL set is classified" "runtimePackClosedDllSet|closed DLL"
    Require-Pattern "validation-report.md" "selected runtime pack matching is classified" "Selected runtime pack manifest|runtimePackReportMatches"
}

if ($RequireBranchSwitch) {
    Require-Pattern "summary.md" "branch-switch review uses branch-switch run label" "Run label:\s*branch-switch"
    Require-JsonPattern "run-metadata.json" "branch-switch review uses branch-switch metadata label" '(?i)"runLabel"\s*:\s*"branch-switch"'
    Require-Pattern "validation-report.md" "classifies in-process branch switch reuse for branch-switch evidence" "In-process branch switch reuse"
    Require-Pattern "validation-report.md" "reports prepared-cache behavior for branch-switch evidence" "Prepared cache matches runtime|Canonical slot bound to native cache identity"
    Require-Pattern "diagnostics/runtime-marker-files.txt" "has branch-switch marker file evidence" "last_game_branch_switch\.txt"
    Require-Pattern "diagnostics/runtime-marker-contents.txt" "has branch-switch marker content evidence" "last_game_branch_switch\.txt|branch switch|selected branch|previous branch|target branch"
    Require-Pattern "diagnostics/current_runtime_cache.txt" "has branch-aware runtime cache marker" "Selected branch:|Runtime ID:"
    Require-Pattern "logs/logcat-runtime-filtered.txt" "has branch-switch runtime/cache log evidence" "Assembly cache branch changed|Assembly cache runtime changed|Runtime slot evidence ready for startup|Blocking selected game startup|Loading PCK from:"
}

if ($RequireSaveSafety) {
    Require-Pattern "diagnostics/current_android_save_origin.txt" "has save-origin evidence" "runtime slot ID|Selected runtime playable|Current Android local saves verified for selected runtime|Origin action:\s*branch switch pending Pull|Current Android local saves verified for selected branch:\s*false|Required next action:\s*Pull from Cloud"
    Require-Pattern "validation-report.md" "save-origin safety is classified" "Steam Cloud Push save-origin safety"
}

if ($RequireResolvedClassification) {
    Require-NoPattern "validation-report.md" "does not carry unknown classifications into release signoff" "(?im)\|\s*(unknown|needs device-only validation)\s*\|"
}

if ($failures.Count -gt 0) {
    Write-Host ""
    Write-Host "Multi-version runtime evidence review failed:"
    foreach ($failure in $failures) {
        Write-Host "FAIL $failure"
    }
    throw "Multi-version runtime evidence review failed with $($failures.Count) failure(s)."
}

Write-Host "Multi-version runtime evidence review passed ($passes checks): $resolvedEvidenceDir"
