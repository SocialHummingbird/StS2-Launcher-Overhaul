param(
    [Parameter(Mandatory = $true)]
    [string]$EvidenceDir
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "evidence-path-utils.ps1")
. (Join-Path $PSScriptRoot "evidence-redaction-utils.ps1")

$resolvedEvidenceDir = Resolve-Path -LiteralPath $EvidenceDir
$root = $resolvedEvidenceDir.Path
$failures = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()

function Add-Failure([string]$Message) {
    $failures.Add($Message) | Out-Null
}

function Add-Warning([string]$Message) {
    $warnings.Add($Message) | Out-Null
}

function Test-CompletedRedactionReview {
    $reviewPath = Join-Path $root "PUBLIC_EVIDENCE_REDACTION_REVIEW.txt"
    if (-not (Test-Path -LiteralPath $reviewPath)) {
        return $false
    }

    $text = Get-Content -Raw -LiteralPath $reviewPath
    foreach ($field in Get-PublicEvidenceRedactionReviewFields) {
        $pattern = "$([regex]::Escape($field)):\s*true"
        if ($text -notmatch $pattern) {
            return $false
        }
    }

    return $true
}

if (-not (Test-Path -LiteralPath $root -PathType Container)) {
    throw "EvidenceDir is not a directory: $EvidenceDir"
}

$files = @(Get-ChildItem -LiteralPath $root -Recurse -File -Force)
$completedRedactionReview = Test-CompletedRedactionReview
if (-not $completedRedactionReview) {
    Add-Failure "PUBLIC_EVIDENCE_REDACTION_REVIEW.txt is missing or not fully completed with true values."
}

$textExtensions = Get-EvidenceTextFileExtensions
$imageExtensions = Get-EvidenceImageFileExtensions
$textContentChecks = Get-EvidenceSensitiveTextChecks

foreach ($file in $files) {
    $relative = Get-EvidenceRelativePath -RootPath $root -Path $file.FullName

    if (Test-EvidenceLocalOnlyPath -RelativePath $relative) {
        Add-Failure "Local-only or high-risk artifact path is present: $relative"
    }

    if ($imageExtensions -contains $file.Extension.ToLowerInvariant()) {
        continue
    }

    if ($textExtensions -notcontains $file.Extension.ToLowerInvariant()) {
        Add-Warning "Skipped non-text artifact: $relative"
        continue
    }

    $text = Get-Content -Raw -LiteralPath $file.FullName
    foreach ($check in $textContentChecks) {
        if ($text -match $check.Pattern) {
            Add-Failure "$($check.Name) detected in $relative"
        }
    }
}

$images = @($files | Where-Object { $imageExtensions -contains $_.Extension.ToLowerInvariant() })
if ($images.Count -gt 0 -and -not $completedRedactionReview) {
    foreach ($image in $images) {
        $relativeImagePath = Get-EvidenceRelativePath -RootPath $root -Path $image.FullName
        Add-Failure "Screenshot/image requires completed PUBLIC_EVIDENCE_REDACTION_REVIEW.txt before public sharing: $relativeImagePath"
    }
}

if ($warnings.Count -gt 0) {
    foreach ($warning in $warnings) {
        Write-Host "WARN $warning"
    }
}

if ($failures.Count -gt 0) {
    Write-Host ""
    Write-Host "Public evidence redaction review failed:"
    foreach ($failure in $failures) {
        Write-Host "FAIL $failure"
    }
    throw "Public evidence redaction review failed with $($failures.Count) failure(s)."
}

Write-Host "Public evidence redaction review passed: $root"
