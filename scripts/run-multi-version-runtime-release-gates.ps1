param(
    [string[]]$EvidenceDirs = @(),
    [string[]]$PublicEvidenceDirs = @(),
    [string[]]$PublicBetaEvidenceDirs = @(),
    [string[]]$BranchSwitchEvidenceDirs = @(),
    [switch]$RequirePublic,
    [switch]$RequirePublicBeta,
    [switch]$RequireBranchSwitch,
    [switch]$RequireSaveSafety,
    [switch]$RequireResolvedClassification,
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

    & $path @Arguments
}

Invoke-RepoScript -RelativePath "scripts\audit-multi-version-runtime.ps1" -Arguments @("-Quiet")
Invoke-RepoScript -RelativePath "scripts\audit-steam-version-selection.ps1"
Invoke-RepoScript -RelativePath "scripts\audit-steam-branch-guidance-parity.ps1"

function Invoke-EvidenceReview {
    param(
        [Parameter(Mandatory = $true)][string]$EvidenceDir,
        [switch]$Public,
        [switch]$PublicBeta,
        [switch]$BranchSwitch
    )

    $reviewArgs = @("-EvidenceDir", $evidenceDir)
    if ($Public -or $RequirePublic) {
        $reviewArgs += "-RequirePublic"
    }
    if ($PublicBeta -or $RequirePublicBeta) {
        $reviewArgs += "-RequirePublicBeta"
    }
    if ($BranchSwitch -or $RequireBranchSwitch) {
        $reviewArgs += "-RequireBranchSwitch"
    }
    if ($RequireSaveSafety) {
        $reviewArgs += "-RequireSaveSafety"
    }
    if ($RequireResolvedClassification) {
        $reviewArgs += "-RequireResolvedClassification"
    }
    if ($Quiet) {
        $reviewArgs += "-Quiet"
    }

    Invoke-RepoScript -RelativePath "scripts\review-multi-version-runtime-evidence.ps1" -Arguments $reviewArgs
}

foreach ($evidenceDir in $EvidenceDirs) {
    Invoke-EvidenceReview -EvidenceDir $evidenceDir
}

foreach ($evidenceDir in $PublicEvidenceDirs) {
    Invoke-EvidenceReview -EvidenceDir $evidenceDir -Public
}

foreach ($evidenceDir in $PublicBetaEvidenceDirs) {
    Invoke-EvidenceReview -EvidenceDir $evidenceDir -PublicBeta
}

foreach ($evidenceDir in $BranchSwitchEvidenceDirs) {
    Invoke-EvidenceReview -EvidenceDir $evidenceDir -BranchSwitch
}

if (-not $Quiet) {
    Write-Host "Multi-version runtime release gates passed."
}
