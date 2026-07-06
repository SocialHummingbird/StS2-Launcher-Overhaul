param(
    [string]$TempRoot = "",
    [switch]$KeepArtifacts,
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$reviewScript = Join-Path $PSScriptRoot "review-multi-version-runtime-evidence.ps1"
$phaseScript = Join-Path $PSScriptRoot "evidence-launch-attempt-phases.ps1"
if (-not (Test-Path -LiteralPath $phaseScript)) {
    throw "Missing launch-attempt phase contract helper: $phaseScript"
}
. $phaseScript
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

function New-MultiVersionRuntimeEvidenceBundle(
    [string]$BaseDir,
    [switch]$LaunchAttempt,
    [switch]$LaunchAttemptPreparedFalse,
    [switch]$LaunchAttemptReportMismatch,
    [switch]$LaunchAttemptMissingMarker,
    [switch]$LaunchAttemptUnmeasuredTiming,
    [switch]$LaunchAttemptStale,
    [switch]$LaunchAttemptFallbackLog,
    [string]$GeneratedUtc = "2026-07-06T00:10:00.0000000Z",
    [string]$LaunchAttemptPhase = "restart requested"
) {
    New-Item -ItemType Directory -Force -Path (Join-Path $BaseDir "diagnostics"), (Join-Path $BaseDir "logs") | Out-Null
    $launchAttemptUtc = if ($LaunchAttemptStale) { "2026-07-06T00:00:00.0000000Z" } else { "2026-07-06T00:09:00.0000000Z" }
    $launchAttemptId = "0123456789abcdef0123456789abcdef"

    $summary = @(
        "# Multi-version runtime evidence summary",
        "",
        "Run label: synthetic",
        "Collector boundary: This collector is read-only and does not mutate Steam Cloud or app data."
    )
    if ($LaunchAttempt) {
        $summary += @(
            "",
            "Launch attempt UTC: $launchAttemptUtc",
            "Launch attempt ID: $launchAttemptId",
            "Launch attempt phase: $LaunchAttemptPhase",
            "Launch attempt action: normal",
            "Launch attempt source: automation",
            "Launch attempt successful handoff phase: true",
            "Launch attempt ready: true",
            "Launch attempt prepared readiness used: true",
            "Launch attempt readiness cache status: fresh-cached",
            "Launch attempt selected branch: public",
            "Launch attempt PCK SHA256: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "Launch attempt source sts2.dll SHA256: cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
            "Launch attempt active Android sts2.dll SHA256: dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd",
            "Launch attempt timings ms: attempt=12 readiness=8 mod=1"
        )
    }
    Save-TestText (Join-Path $BaseDir "summary.md") ($summary -join [Environment]::NewLine)

    Save-TestText (Join-Path $BaseDir "run-metadata.json") (@{
        generatedUtc = $GeneratedUtc
        packageName = "com.sts2launcher.overhaul.fork.test"
        runLabel = "synthetic"
        collector = "capture-multi-version-runtime-evidence.ps1"
        readOnly = $true
    } | ConvertTo-Json -Depth 5)

    $launchAttemptMarkerStatus = if ($LaunchAttempt -and -not $LaunchAttemptMissingMarker) { "captured" } else { "missing" }
    $launchAttemptRuntimeStatus = if ($LaunchAttempt -and -not $LaunchAttemptReportMismatch -and -not $LaunchAttemptMissingMarker) { "matched" } else { "mismatch" }
    $validationReport = @(
        "# Multi-version runtime validation report",
        "",
        "Status values: confirmed, ruled out, likely, unknown, needs device-only validation.",
        "",
        "| Area | Status | Evidence | Required next action |",
        "| --- | --- | --- | --- |",
        "| Steam branch partial/shared content | likely | synthetic | compare exact assets |",
        "| Stale/incomplete downloader cache | ruled out | synthetic | none |",
        "| Wrong launch path | ruled out | synthetic | none |",
        "| Shared assembly/runtime cache | ruled out | synthetic | none |",
        "| In-process branch switch reuse | confirmed | synthetic | none |",
        "| Android PCK patch side effect | ruled out | synthetic | none |",
        "| Godot import/resource mismatch | ruled out | synthetic | none |",
        "| Save/config asset reference mismatch | ruled out | synthetic | none |",
        "| Runtime slot marker matches selected files | fresh | synthetic | none |",
        "| Prepared cache matches runtime | matched | synthetic | none |",
        "| Canonical slot bound to native cache identity | bound | synthetic | none |",
        "| Start Game launch-attempt marker | $launchAttemptMarkerStatus | synthetic | none |",
        "| Launch attempt matches runtime validation | $launchAttemptRuntimeStatus | synthetic | none |",
        "",
        "## Mixed/split asset hypothesis matrix",
        "",
        "Steam branch partial/shared content",
        "Stale/incomplete downloader cache",
        "Wrong launch path",
        "Shared assembly/runtime cache",
        "In-process branch switch reuse",
        "Android PCK patch side effect",
        "Godot import/resource mismatch",
        "Save/config asset reference mismatch"
    )
    Save-TestText (Join-Path $BaseDir "validation-report.md") ($validationReport -join [Environment]::NewLine)

    Save-TestText (Join-Path $BaseDir "diagnostics\current_runtime_slot.json") (@{
        runtimeSlotId = "public-synthetic-slot"
        branch = "public"
        filesReady = $true
        playable = $true
        runtimeCompatible = $true
        patchCompatible = $true
    } | ConvertTo-Json -Depth 5)

    Save-TestText (Join-Path $BaseDir "diagnostics\current_runtime_cache.txt") @"
Selected branch: public
Runtime ID: public-synthetic-slot
Selected PCK SHA256: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
Selected source sts2.dll SHA256: cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc
Publish cache active sts2.dll SHA256: dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd
"@
    $logcatText = if ($LaunchAttemptFallbackLog) {
        "Loading PCK from: files/game/SlayTheSpire2.pck`nNativeFallbackActivity displayed after Start Game handoff"
    } else {
        "Loading PCK from: files/game/SlayTheSpire2.pck"
    }
    Save-TestText (Join-Path $BaseDir "logs\logcat-runtime-filtered.txt") $logcatText

    if ($LaunchAttempt -and -not $LaunchAttemptMissingMarker) {
        Save-TestText (Join-Path $BaseDir "diagnostics\runtime-marker-files.txt") "files/last_launch_attempt.txt"
        Save-TestText (Join-Path $BaseDir "diagnostics\runtime-marker-contents.txt") "===== files/last_launch_attempt.txt`nStS2 Mobile launch attempt"
        $prepared = if ($LaunchAttemptPreparedFalse) { "false" } else { "true" }
        $attemptElapsed = if ($LaunchAttemptUnmeasuredTiming) { "<not measured>" } else { "12" }
        $readinessElapsed = if ($LaunchAttemptUnmeasuredTiming) { "<not measured>" } else { "8" }
        $modElapsed = if ($LaunchAttemptUnmeasuredTiming) { "<not measured>" } else { "1" }
        Save-TestText (Join-Path $BaseDir "diagnostics\last_launch_attempt.txt") @"
StS2 Mobile launch attempt
UTC: $launchAttemptUtc
Attempt ID: $launchAttemptId
Phase: $LaunchAttemptPhase
Action: normal
Source: automation
Files ready: true
Prepared readiness used: $prepared
Readiness cache status: fresh-cached
Launch attempt elapsed ms: $attemptElapsed
Launch readiness elapsed ms: $readinessElapsed
Mod readiness elapsed ms: $modElapsed
Selected branch: public
Game directory: files/game
PCK path: files/game/SlayTheSpire2.pck
PCK SHA256: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
Source sts2.dll path: files/game/data_sts2_windows_x86_64/sts2.dll
Source sts2.dll SHA256: cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc
Active Android sts2.dll path: files/.godot/mono/publish/arm64/sts2.dll
Active Android sts2.dll SHA256: dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd
Runtime pack directory: files/runtime_packs/public
Runtime pack manifest path: files/runtime_packs/public/compatibility.json
Runtime pack usable: true
Runtime cache marker present: true
Runtime patch validation marker present: true
Patch compatibility marker path: files/game/.android_patch_validation.json
Mod readiness phase: launch mod readiness
Mod readiness cache status: not-needed-vanilla
Mod play mode: vanilla
Mod installed count: 0
Mod enabled count: 0
Mod unsupported count: 0
Modded save cloud push locked: false
"@
    } else {
        Save-TestText (Join-Path $BaseDir "diagnostics\runtime-marker-files.txt") ""
        Save-TestText (Join-Path $BaseDir "diagnostics\runtime-marker-contents.txt") ""
    }
}

function Invoke-Reviewer([string]$EvidenceDir, [switch]$RequireLaunchAttempt, [int]$MaxLaunchAttemptAgeMinutes = 0) {
    if ($Quiet) {
        & $reviewScript `
            -EvidenceDir $EvidenceDir `
            -RequireLaunchAttempt:$RequireLaunchAttempt `
            -MaxLaunchAttemptAgeMinutes $MaxLaunchAttemptAgeMinutes `
            -Quiet *> $null
        return
    }

    & $reviewScript `
        -EvidenceDir $EvidenceDir `
        -RequireLaunchAttempt:$RequireLaunchAttempt `
        -MaxLaunchAttemptAgeMinutes $MaxLaunchAttemptAgeMinutes `
        -Quiet | Out-Null
}

function Invoke-ReviewShouldPass([string]$EvidenceDir, [switch]$RequireLaunchAttempt, [int]$MaxLaunchAttemptAgeMinutes = 0) {
    Invoke-Reviewer -EvidenceDir $EvidenceDir -RequireLaunchAttempt:$RequireLaunchAttempt -MaxLaunchAttemptAgeMinutes $MaxLaunchAttemptAgeMinutes
}

function Invoke-ReviewShouldFail([string]$EvidenceDir, [switch]$RequireLaunchAttempt, [int]$MaxLaunchAttemptAgeMinutes = 0, [string]$Description) {
    try {
        Invoke-Reviewer -EvidenceDir $EvidenceDir -RequireLaunchAttempt:$RequireLaunchAttempt -MaxLaunchAttemptAgeMinutes $MaxLaunchAttemptAgeMinutes
    } catch {
        Write-TestPass "PASS negative case rejected: $Description"
        return
    }

    throw "Expected multi-version runtime evidence review to fail: $Description"
}

function Write-TestPass([string]$Message) {
    if (-not $Quiet) {
        Write-Host $Message
    }
}

function Assert-LaunchAttemptPhaseContractMatchesCSharp() {
    $phaseSourcePath = Join-Path $root "src\STS2Mobile\Launcher\LauncherLaunchAttemptPhases.cs"
    if (-not (Test-Path -LiteralPath $phaseSourcePath)) {
        throw "Missing C# launch-attempt phase contract: $phaseSourcePath"
    }

    $phaseSource = Get-Content -LiteralPath $phaseSourcePath -Raw
    $csharpPhases = @(
        [regex]::Matches($phaseSource, 'internal\s+const\s+string\s+\w+\s*=\s*"([^"]+)";') |
            ForEach-Object { Normalize-LaunchAttemptPhase -Phase $_.Groups[1].Value } |
            Sort-Object -Unique
    )
    $successPhases = @(
        $script:LaunchAttemptSuccessfulHandoffPhases |
            ForEach-Object { Normalize-LaunchAttemptPhase -Phase $_ } |
            Sort-Object -Unique
    )
    $rejectedPhases = @(
        $script:LaunchAttemptRejectedProofPhases |
            ForEach-Object { Normalize-LaunchAttemptPhase -Phase $_ } |
            Sort-Object -Unique
    )
    $reviewerPhases = @(@($successPhases + $rejectedPhases) | Sort-Object -Unique)
    $overlap = @($successPhases | Where-Object { $rejectedPhases -contains $_ })
    if ($overlap.Count -gt 0) {
        throw "Launch-attempt phase contract has phases marked as both success and rejected proof: $($overlap -join ', ')"
    }

    $unclassifiedCSharp = @($csharpPhases | Where-Object { $reviewerPhases -notcontains $_ })
    if ($unclassifiedCSharp.Count -gt 0) {
        throw "C# launch-attempt phases missing reviewer classification: $($unclassifiedCSharp -join ', ')"
    }

    $unknownReviewer = @($reviewerPhases | Where-Object { $csharpPhases -notcontains $_ })
    if ($unknownReviewer.Count -gt 0) {
        throw "Reviewer launch-attempt phases missing C# constants: $($unknownReviewer -join ', ')"
    }

    $constantsByName = @{}
    foreach ($match in [regex]::Matches($phaseSource, 'internal\s+const\s+string\s+(\w+)\s*=\s*"([^"]+)";')) {
        $constantsByName[$match.Groups[1].Value] = Normalize-LaunchAttemptPhase -Phase $match.Groups[2].Value
    }
    $helperMatch = [regex]::Match(
        $phaseSource,
        'IsSuccessfulHandoffPhase\s*\([^)]*\)\s*=>\s*(?<body>.*?);',
        [System.Text.RegularExpressions.RegexOptions]::Singleline
    )
    if (-not $helperMatch.Success) {
        throw "C# launch-attempt phase contract is missing IsSuccessfulHandoffPhase helper"
    }

    $helperConstants = @(
        [regex]::Matches($helperMatch.Groups["body"].Value, 'phase,\s*(\w+),') |
            ForEach-Object {
                $name = $_.Groups[1].Value
                if (-not $constantsByName.ContainsKey($name)) {
                    throw "IsSuccessfulHandoffPhase references unknown phase constant: $name"
                }
                $constantsByName[$name]
            } |
            Sort-Object -Unique
    )
    Assert-SameStringSet `
        -Description "C# successful handoff phase helper" `
        -Expected $successPhases `
        -Actual $helperConstants

    Write-TestPass "PASS launch-attempt phase contract matches C# constants"
}

function Get-CSharpStringConstants([string]$RelativePath) {
    $sourcePath = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $sourcePath)) {
        throw "Missing C# string constant contract: $sourcePath"
    }

    $source = Get-Content -LiteralPath $sourcePath -Raw
    $constants = @{}
    foreach ($match in [regex]::Matches($source, 'internal\s+const\s+string\s+(\w+)\s*=\s*"([^"]+)";')) {
        $constants[$match.Groups[1].Value] = $match.Groups[2].Value
    }

    if ($constants.Count -eq 0) {
        throw "No C# string constants found in $sourcePath"
    }

    return $constants
}

function Get-ReviewerStatusValues([string]$Prefix) {
    $reviewerSource = Get-Content -LiteralPath $reviewScript -Raw
    $pattern = [regex]::Escape($Prefix) + '\\s\*\(([^)]+)\)'
    $matches = @([regex]::Matches($reviewerSource, $pattern))
    if ($matches.Count -ne 1) {
        throw "Expected one reviewer cache-status regex for '$Prefix', found $($matches.Count)"
    }

    return @(
        $matches[0].Groups[1].Value.Split('|') |
            ForEach-Object { $_.Trim() } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            Sort-Object -Unique
    )
}

function Assert-SameStringSet(
    [string]$Description,
    [string[]]$Expected,
    [string[]]$Actual
) {
    $expectedSet = @($Expected | Sort-Object -Unique)
    $actualSet = @($Actual | Sort-Object -Unique)
    $missing = @($expectedSet | Where-Object { $actualSet -notcontains $_ })
    $extra = @($actualSet | Where-Object { $expectedSet -notcontains $_ })
    if ($missing.Count -gt 0 -or $extra.Count -gt 0) {
        throw "$Description mismatch. Missing: $($missing -join ', '); extra: $($extra -join ', ')"
    }
}

function Assert-LaunchAttemptCacheStatusContractMatchesCSharp() {
    $launchStatuses = Get-CSharpStringConstants "src\STS2Mobile\Launcher\LauncherLaunchReadinessCacheStatus.cs"
    $modStatuses = Get-CSharpStringConstants "src\STS2Mobile\Launcher\LauncherModLaunchReadinessCacheStatus.cs"

    $launchExpected = @(
        $launchStatuses["Fresh"],
        $launchStatuses["FreshCached"],
        $launchStatuses["MemoryCacheHit"]
    )
    if ($launchExpected -contains $null) {
        throw "C# launch-readiness cache status constants are missing one of Fresh, FreshCached, or MemoryCacheHit"
    }

    Assert-SameStringSet `
        -Description "Launch-attempt readiness cache status reviewer contract" `
        -Expected $launchExpected `
        -Actual (Get-ReviewerStatusValues "Readiness cache status:")

    Assert-SameStringSet `
        -Description "Launch-attempt mod readiness cache status reviewer contract" `
        -Expected @($modStatuses.Values) `
        -Actual (Get-ReviewerStatusValues "Mod readiness cache status:")

    Write-TestPass "PASS launch-attempt cache status contracts match C# constants"
}

$resolvedTempRoot = (Resolve-Path -LiteralPath (New-Item -ItemType Directory -Force -Path $TempRoot)).ProviderPath
$runRoot = Join-Path $resolvedTempRoot ("multi-version-runtime-reviewer-tests-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

try {
    Assert-LaunchAttemptPhaseContractMatchesCSharp
    Assert-LaunchAttemptCacheStatusContractMatchesCSharp

    $oldArtifactDir = Join-Path $runRoot "old-artifact-compatible"
    New-MultiVersionRuntimeEvidenceBundle -BaseDir $oldArtifactDir
    Invoke-ReviewShouldPass -EvidenceDir $oldArtifactDir
    Write-TestPass "PASS old artifact without launch-attempt accepted when not required"

    $positiveDir = Join-Path $runRoot "launch-attempt-positive"
    New-MultiVersionRuntimeEvidenceBundle -BaseDir $positiveDir -LaunchAttempt
    Invoke-ReviewShouldPass -EvidenceDir $positiveDir -RequireLaunchAttempt -MaxLaunchAttemptAgeMinutes 30
    Write-TestPass "PASS launch-attempt evidence accepted"

    $staleDir = Join-Path $runRoot "negative-stale-launch-attempt"
    New-MultiVersionRuntimeEvidenceBundle -BaseDir $staleDir -LaunchAttempt -LaunchAttemptStale
    Invoke-ReviewShouldFail -EvidenceDir $staleDir -RequireLaunchAttempt -MaxLaunchAttemptAgeMinutes 5 -Description "launch-attempt marker older than freshness window"

    $missingMarkerDir = Join-Path $runRoot "negative-missing-launch-attempt"
    New-MultiVersionRuntimeEvidenceBundle -BaseDir $missingMarkerDir -LaunchAttempt -LaunchAttemptMissingMarker
    Invoke-ReviewShouldFail -EvidenceDir $missingMarkerDir -RequireLaunchAttempt -Description "RequireLaunchAttempt without last_launch_attempt.txt"

    $preparedFalseDir = Join-Path $runRoot "negative-prepared-false"
    New-MultiVersionRuntimeEvidenceBundle -BaseDir $preparedFalseDir -LaunchAttempt -LaunchAttemptPreparedFalse
    Invoke-ReviewShouldFail -EvidenceDir $preparedFalseDir -RequireLaunchAttempt -Description "launch-attempt marker without prepared readiness"

    $unmeasuredTimingDir = Join-Path $runRoot "negative-unmeasured-launch-attempt-timing"
    New-MultiVersionRuntimeEvidenceBundle -BaseDir $unmeasuredTimingDir -LaunchAttempt -LaunchAttemptUnmeasuredTiming
    Invoke-ReviewShouldFail -EvidenceDir $unmeasuredTimingDir -RequireLaunchAttempt -Description "launch-attempt marker without measured readiness timings"

    $readyOnlyDir = Join-Path $runRoot "negative-ready-only-phase"
    New-MultiVersionRuntimeEvidenceBundle -BaseDir $readyOnlyDir -LaunchAttempt -LaunchAttemptPhase "ready"
    Invoke-ReviewShouldFail -EvidenceDir $readyOnlyDir -RequireLaunchAttempt -Description "launch-attempt marker that only reached pre-handoff ready phase"

    $checkingOnlyDir = Join-Path $runRoot "negative-checking-only-phase"
    New-MultiVersionRuntimeEvidenceBundle -BaseDir $checkingOnlyDir -LaunchAttempt -LaunchAttemptPhase "checking"
    Invoke-ReviewShouldFail -EvidenceDir $checkingOnlyDir -RequireLaunchAttempt -Description "launch-attempt marker that only reached pre-readiness checking phase"

    $setupFailedDir = Join-Path $runRoot "negative-setup-failed-phase"
    New-MultiVersionRuntimeEvidenceBundle -BaseDir $setupFailedDir -LaunchAttempt -LaunchAttemptPhase "setup failed"
    Invoke-ReviewShouldFail -EvidenceDir $setupFailedDir -RequireLaunchAttempt -Description "launch-attempt marker that failed before selected-version readiness"

    $readinessFailedDir = Join-Path $runRoot "negative-readiness-failed-phase"
    New-MultiVersionRuntimeEvidenceBundle -BaseDir $readinessFailedDir -LaunchAttempt -LaunchAttemptPhase "readiness failed"
    Invoke-ReviewShouldFail -EvidenceDir $readinessFailedDir -RequireLaunchAttempt -Description "launch-attempt marker that failed selected-version readiness"

    $blockedInModelDir = Join-Path $runRoot "negative-blocked-in-model-phase"
    New-MultiVersionRuntimeEvidenceBundle -BaseDir $blockedInModelDir -LaunchAttempt -LaunchAttemptPhase "blocked in model"
    Invoke-ReviewShouldFail -EvidenceDir $blockedInModelDir -RequireLaunchAttempt -Description "launch-attempt marker blocked by model-level prepared-readiness defense"

    $inProcessSignalFailedDir = Join-Path $runRoot "negative-in-process-signal-failed-phase"
    New-MultiVersionRuntimeEvidenceBundle -BaseDir $inProcessSignalFailedDir -LaunchAttempt -LaunchAttemptPhase "in-process signal failed"
    Invoke-ReviewShouldFail -EvidenceDir $inProcessSignalFailedDir -RequireLaunchAttempt -Description "launch-attempt marker with failed in-process signal"

    $handoffNotRequestedDir = Join-Path $runRoot "negative-handoff-not-requested-phase"
    New-MultiVersionRuntimeEvidenceBundle -BaseDir $handoffNotRequestedDir -LaunchAttempt -LaunchAttemptPhase "launch handoff not requested"
    Invoke-ReviewShouldFail -EvidenceDir $handoffNotRequestedDir -RequireLaunchAttempt -Description "launch-attempt marker where model returned without requesting handoff"

    $restartWithoutReadyDir = Join-Path $runRoot "negative-restart-without-ready-files-phase"
    New-MultiVersionRuntimeEvidenceBundle -BaseDir $restartWithoutReadyDir -LaunchAttempt -LaunchAttemptPhase "restart requested without ready files"
    Invoke-ReviewShouldFail -EvidenceDir $restartWithoutReadyDir -RequireLaunchAttempt -Description "launch-attempt marker with final bridge readiness blocker"

    $fallbackLogDir = Join-Path $runRoot "negative-native-fallback-log"
    New-MultiVersionRuntimeEvidenceBundle -BaseDir $fallbackLogDir -LaunchAttempt -LaunchAttemptFallbackLog
    Invoke-ReviewShouldFail -EvidenceDir $fallbackLogDir -RequireLaunchAttempt -Description "launch-attempt marker with NativeFallbackActivity log evidence"

    $reportMismatchDir = Join-Path $runRoot "negative-report-mismatch"
    New-MultiVersionRuntimeEvidenceBundle -BaseDir $reportMismatchDir -LaunchAttempt -LaunchAttemptReportMismatch
    Invoke-ReviewShouldFail -EvidenceDir $reportMismatchDir -RequireLaunchAttempt -Description "launch-attempt report not matched to runtime validation"

    Write-TestPass "Multi-version runtime evidence reviewer regression tests passed: $runRoot"
} finally {
    if (-not $KeepArtifacts) {
        $resolvedRunRoot = (Resolve-Path -LiteralPath $runRoot).ProviderPath
        if ($resolvedRunRoot.StartsWith($resolvedTempRoot, [StringComparison]::OrdinalIgnoreCase)) {
            Remove-Item -LiteralPath $resolvedRunRoot -Recurse -Force
        }
    }
}
