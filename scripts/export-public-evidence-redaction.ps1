param(
    [Parameter(Mandatory = $true)]
    [string]$SourceEvidenceDir,
    [string]$OutputDir = "",
    [switch]$Force,
    [switch]$IncludeImages
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "evidence-path-utils.ps1")
. (Join-Path $PSScriptRoot "evidence-redaction-utils.ps1")

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$source = Resolve-Path -LiteralPath $SourceEvidenceDir
$sourcePath = $source.Path
$sourceName = Split-Path -Leaf $sourcePath

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $parent = Split-Path -Parent $sourcePath
    $OutputDir = Join-Path $parent "$sourceName-public-redacted"
}

$outputPath = Resolve-EvidenceRepoPath -RepoRoot $root -Path $OutputDir
if ((Test-Path -LiteralPath $outputPath) -and -not $Force) {
    throw "OutputDir already exists. Pass -Force to replace it: $outputPath"
}

if (Test-Path -LiteralPath $outputPath) {
    Remove-Item -LiteralPath $outputPath -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

$textExtensions = Get-EvidenceTextFileExtensions
$imageExtensions = Get-EvidenceImageFileExtensions
$skipped = [System.Collections.Generic.List[string]]::new()
$copied = [System.Collections.Generic.List[string]]::new()

function Copy-SanitizedTextFile([System.IO.FileInfo]$File, [string]$RelativePath) {
    $destination = Join-Path $outputPath $RelativePath
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
    $text = Get-Content -Raw -LiteralPath $File.FullName
    Set-Content -LiteralPath $destination -Value (ConvertTo-RedactedEvidenceText $text) -Encoding UTF8
    $copied.Add($RelativePath) | Out-Null
}

function Copy-ReviewedImageFile([System.IO.FileInfo]$File, [string]$RelativePath) {
    $destination = Join-Path $outputPath $RelativePath
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
    Copy-Item -LiteralPath $File.FullName -Destination $destination -Force
    $copied.Add($RelativePath) | Out-Null
}

$files = @(Get-ChildItem -LiteralPath $sourcePath -Recurse -File -Force)
foreach ($file in $files) {
    $relative = Get-EvidenceRelativePath -RootPath $sourcePath -Path $file.FullName
    $extension = $file.Extension.ToLowerInvariant()

    if (Test-EvidenceLocalOnlyPath -RelativePath $relative) {
        $skipped.Add("$relative (local-only path)") | Out-Null
        continue
    }

    if ($textExtensions -contains $extension) {
        Copy-SanitizedTextFile -File $file -RelativePath $relative
        continue
    }

    if ($imageExtensions -contains $extension) {
        if ($IncludeImages) {
            Copy-ReviewedImageFile -File $file -RelativePath $relative
        } else {
            $skipped.Add("$relative (image omitted; pass -IncludeImages only after manual visual review)") | Out-Null
        }
        continue
    }

    $skipped.Add("$relative (unsupported extension)") | Out-Null
}

$reviewPath = Join-Path $outputPath "PUBLIC_EVIDENCE_REDACTION_REVIEW.txt"
Set-Content -LiteralPath $reviewPath -Value @(
    "Public Evidence Redaction Review",
    "",
    "Sanitized export source folder: $sourceName",
    (Format-PublicEvidenceRedactionReviewFields -Completed $true),
    "",
    "Reviewer: automated sanitized export plus review-public-evidence-redaction.ps1 gate",
    "UTC: $((Get-Date).ToUniversalTime().ToString("o"))",
    "Notes: Raw evidence remains local. This folder contains sanitized copies of text artifacts; images are omitted unless -IncludeImages was used after visual review."
) -Encoding UTF8

$manifestPath = Join-Path $outputPath "PUBLIC_SHARE_MANIFEST.txt"
Set-Content -LiteralPath $manifestPath -Value @(
    "Public evidence sanitized export",
    "",
    "Source evidence folder: $sourceName",
    "Raw evidence remains local and should not be posted.",
    "Run scripts\review-public-evidence-redaction.ps1 -EvidenceDir against this folder before sharing.",
    "",
    "Copied sanitized artifacts:",
    ($copied | Sort-Object | ForEach-Object { "- $_" }),
    "",
    "Skipped artifacts:",
    ($(if ($skipped.Count -gt 0) { $skipped | Sort-Object | ForEach-Object { "- $_" } } else { "- <none>" }))
) -Encoding UTF8

Write-Host "Sanitized public evidence export written to $outputPath"
Write-Host "Copied files: $($copied.Count)"
Write-Host "Skipped files: $($skipped.Count)"
