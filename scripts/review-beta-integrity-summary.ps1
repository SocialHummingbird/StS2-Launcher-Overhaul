param(
    [Parameter(Mandatory = $true)]
    [string]$SummaryPath,
    [switch]$FailOnNotReady
)

$ErrorActionPreference = "Stop"

function Read-SummaryValue([string[]]$Lines, [string]$Prefix) {
    foreach ($line in $Lines) {
        if ($line.StartsWith($Prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $line.Substring($Prefix.Length).Trim()
        }
    }

    return "<missing>"
}

if (-not (Test-Path -LiteralPath $SummaryPath)) {
    throw "Beta integrity summary not found: $SummaryPath"
}

$lines = Get-Content -LiteralPath $SummaryPath
$classification = Read-SummaryValue $lines "Classification:"
$readiness = Read-SummaryValue $lines "Evidence readiness:"
$missing = Read-SummaryValue $lines "Evidence missing/weak:"
$publicSharingWarning = Read-SummaryValue $lines "Public sharing warning:"
$publicMarker = Read-SummaryValue $lines "Public branch marker:"
$selectedMarker = Read-SummaryValue $lines "Selected branch marker:"
$redownloadMatches = Read-SummaryValue $lines "Clean redownload matches investigated branch:"
$redownloadCleared = Read-SummaryValue $lines "Clean redownload selected directories cleared:"
$branchAvailabilityMatches = Read-SummaryValue $lines "Branch availability matches investigated branch:"

$isReady = $readiness -match "^ready for "
$hasPublicSharingWarning = $publicSharingWarning -ne "<missing>"

Write-Host "Beta integrity summary review"
Write-Host "Summary: $SummaryPath"
Write-Host "Classification: $classification"
Write-Host "Evidence readiness: $readiness"
Write-Host "Evidence missing/weak: $missing"
Write-Host "Public sharing warning present: $($hasPublicSharingWarning.ToString().ToLowerInvariant())"
Write-Host "Public branch marker: $publicMarker"
Write-Host "Selected branch marker: $selectedMarker"
Write-Host "Clean redownload matches investigated branch: $redownloadMatches"
Write-Host "Clean redownload selected directories cleared: $redownloadCleared"
Write-Host "Branch availability matches investigated branch: $branchAvailabilityMatches"

if ($isReady) {
    if (-not $hasPublicSharingWarning -and $FailOnNotReady) {
        Write-Host "Review verdict: evidence package is ready, but public-sharing warning is missing."
        Write-Host "Exit code: 3 (public-sharing warning missing)"
        exit 3
    }

    Write-Host "Review verdict: evidence package is ready for the classification type named above."
    exit 0
}

Write-Host "Review verdict: evidence package is NOT ready for final classification."
if ($FailOnNotReady) {
    if (-not $hasPublicSharingWarning) {
        Write-Host "Public-sharing warning is also missing."
    }

    Write-Host "Exit code: 2 (evidence not ready for final classification)"
    exit 2
}

exit 0
