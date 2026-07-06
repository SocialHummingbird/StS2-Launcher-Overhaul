param(
    [string[]]$EvidenceDirs = @(),
    [string[]]$PublicEvidenceDirs = @(),
    [string[]]$PublicBetaEvidenceDirs = @(),
    [string[]]$BranchSwitchEvidenceDirs = @(),
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

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path (Join-Path $scriptDir "..")).ProviderPath

function Invoke-RepoScript {
    param(
        [Parameter(Mandatory = $true)][string]$RelativePath,
        [string[]]$Arguments = @()
    )

    $path = Join-Path $root $RelativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing required gate script: $RelativePath"
    }

    if (-not $Quiet) {
        Write-Host "Running $RelativePath"
    }

    if ($Quiet) {
        & $path @Arguments *> $null
        return
    }

    & $path @Arguments
}

Invoke-RepoScript -RelativePath "scripts\audit-multi-version-runtime.ps1" -Arguments @("-Quiet")
$quietArguments = @()
if ($Quiet) {
    $quietArguments += "-Quiet"
}
Invoke-RepoScript -RelativePath "scripts\audit-steam-version-selection.ps1" -Arguments $quietArguments
Invoke-RepoScript -RelativePath "scripts\audit-steam-branch-guidance-parity.ps1" -Arguments $quietArguments
$reviewerTestArguments = @()
if ($Quiet) {
    $reviewerTestArguments += "-Quiet"
}
Invoke-RepoScript -RelativePath "scripts\test-multi-version-runtime-evidence-reviewer.ps1" -Arguments $reviewerTestArguments

function Invoke-EvidenceReview {
    param(
        [Parameter(Mandatory = $true)][string]$EvidenceDir,
        [bool]$PublicRequired,
        [bool]$PublicBetaRequired,
        [bool]$BranchSwitchRequired
    )

    $reviewScriptRelativePath = "scripts\review-multi-version-runtime-evidence.ps1"
    $reviewScriptPath = Join-Path $root $reviewScriptRelativePath
    if (-not (Test-Path -LiteralPath $reviewScriptPath)) {
        throw "Missing required gate script: $reviewScriptRelativePath"
    }
    if (-not $Quiet) {
        Write-Host "Running $reviewScriptRelativePath"
    }

    & $reviewScriptPath `
        -EvidenceDir "$EvidenceDir" `
        -RequirePublic:$PublicRequired `
        -RequirePublicBeta:$PublicBetaRequired `
        -RequireBranchSwitch:$BranchSwitchRequired `
        -RequireSaveSafety:$RequireSaveSafety `
        -RequireLaunchAttempt:$RequireLaunchAttempt `
        -RequireResolvedClassification:$RequireResolvedClassification `
        -MaxLaunchAttemptAgeMinutes $MaxLaunchAttemptAgeMinutes `
        -Quiet:$Quiet
}

foreach ($evidenceDir in $EvidenceDirs) {
    Invoke-EvidenceReview "$evidenceDir" $RequirePublic.IsPresent $RequirePublicBeta.IsPresent $RequireBranchSwitch.IsPresent
}

foreach ($evidenceDir in $PublicEvidenceDirs) {
    Invoke-EvidenceReview "$evidenceDir" $true $RequirePublicBeta.IsPresent $RequireBranchSwitch.IsPresent
}

foreach ($evidenceDir in $PublicBetaEvidenceDirs) {
    Invoke-EvidenceReview "$evidenceDir" $RequirePublic.IsPresent $true $RequireBranchSwitch.IsPresent
}

foreach ($evidenceDir in $BranchSwitchEvidenceDirs) {
    Invoke-EvidenceReview "$evidenceDir" $RequirePublic.IsPresent $RequirePublicBeta.IsPresent $true
}

if (-not $Quiet) {
    Write-Host "Multi-version runtime release gates passed."
}
