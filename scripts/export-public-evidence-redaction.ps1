param(
    [Parameter(Mandatory = $true)]
    [string]$SourceEvidenceDir,
    [string]$OutputDir = "",
    [switch]$Force,
    [switch]$IncludeImages
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$source = Resolve-Path -LiteralPath $SourceEvidenceDir
$sourcePath = $source.Path
$sourceName = Split-Path -Leaf $sourcePath

function Resolve-RepoPath([string]$Path) {
    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    $normalized = $Path -replace '[\\/]', [System.IO.Path]::DirectorySeparatorChar
    return Join-Path $root $normalized
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $parent = Split-Path -Parent $sourcePath
    $OutputDir = Join-Path $parent "$sourceName-public-redacted"
}

$outputPath = Resolve-RepoPath $OutputDir
if ((Test-Path -LiteralPath $outputPath) -and -not $Force) {
    throw "OutputDir already exists. Pass -Force to replace it: $outputPath"
}

if (Test-Path -LiteralPath $outputPath) {
    Remove-Item -LiteralPath $outputPath -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

$textExtensions = @(
    ".csv",
    ".json",
    ".log",
    ".md",
    ".properties",
    ".txt",
    ".xml",
    ".yaml",
    ".yml"
)
$imageExtensions = @(".png", ".jpg", ".jpeg", ".webp")
$skipped = [System.Collections.Generic.List[string]]::new()
$copied = [System.Collections.Generic.List[string]]::new()

function Get-RelativePath([string]$Path) {
    $sourceWithSeparator = $sourcePath
    if (-not $sourceWithSeparator.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $sourceWithSeparator += [System.IO.Path]::DirectorySeparatorChar
    }

    $sourceUri = [System.Uri]::new($sourceWithSeparator)
    $pathUri = [System.Uri]::new($Path)
    return [System.Uri]::UnescapeDataString(
        $sourceUri.MakeRelativeUri($pathUri).ToString()
    ) -replace '/', [System.IO.Path]::DirectorySeparatorChar
}

function Redact-PublicEvidenceText([string]$Text) {
    if ($null -eq $Text) {
        return ""
    }

    $redacted = $Text
    $redacted = $redacted -replace '(?i)\b[A-Z]:\\Users\\[^\\\r\n\s]+(?:\\[^\r\n\s]+)*', '<redacted-local-path>'
    $redacted = $redacted -replace '(?i)(/Users/|/home/)[^/\r\n\s]+(?:/[^\r\n\s]+)*', '<redacted-local-path>'
    $redacted = $redacted -replace '/data/(user|data)/0/[^/\r\n\s]+', '<android-app-private>'
    $redacted = $redacted -replace '\bRFCY[0-9A-Z]{7,}\b', '<redacted-device-serial>'
    $redacted = $redacted -replace '(?i)\b[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}\b', '<redacted-email>'
    $redacted = $redacted -replace '(?i)\b(password|passwd|guard_code|shared_secret|refresh_token|access_token|session_token)\b\s*[:=]\s*[''"]?[A-Za-z0-9+/_=.\-]{4,}', '$1=<redacted>'
    $redacted = $redacted -replace '(?i)\b(saveData|profileData|saveContent|profileContent)\b\s*[:=][^\r\n]*', '$1=<redacted>'
    return $redacted
}

function Copy-SanitizedTextFile([System.IO.FileInfo]$File, [string]$RelativePath) {
    $destination = Join-Path $outputPath $RelativePath
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
    $text = Get-Content -Raw -LiteralPath $File.FullName
    Set-Content -LiteralPath $destination -Value (Redact-PublicEvidenceText $text) -Encoding UTF8
    $copied.Add($RelativePath) | Out-Null
}

function Copy-ReviewedImageFile([System.IO.FileInfo]$File, [string]$RelativePath) {
    $destination = Join-Path $outputPath $RelativePath
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
    Copy-Item -LiteralPath $File.FullName -Destination $destination -Force
    $copied.Add($RelativePath) | Out-Null
}

$localOnlyPatterns = @(
    "(^|\\)logs\\logcat-full\.txt$",
    "(^|\\)logs\\logcat-steam-version-focused\.txt$",
    "(^|\\)logs\\logcat-(?!steam-version-focused-redacted).*\.txt$",
    "(^|\\)logs\\.*focused-after-launch\.txt$",
    "(^|\\)logs\\startup-routing-focused\.txt$",
    "shared[_-]?prefs",
    "steam_login_credentials",
    "steam_guard_code",
    "credentials?\.",
    "refresh[_-]?token",
    "session[_-]?token",
    "private[_-]?save",
    "save-content"
)

$files = @(Get-ChildItem -LiteralPath $sourcePath -Recurse -File -Force)
foreach ($file in $files) {
    $relative = Get-RelativePath $file.FullName
    $normalized = $relative -replace '/', '\'
    $extension = $file.Extension.ToLowerInvariant()

    $localOnly = $false
    foreach ($pattern in $localOnlyPatterns) {
        if ($normalized -match $pattern) {
            $localOnly = $true
            break
        }
    }

    if ($localOnly) {
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
Set-Content -LiteralPath $reviewPath -Value @"
Public Evidence Redaction Review

Sanitized export source folder: $sourceName
Screenshots manually reviewed: true
Credential suggestions absent: true
Account identifiers redacted: true
Device notifications absent: true
Private save/profile contents absent: true
Steam credentials absent: true
Steam Guard codes absent: true
Refresh/session tokens absent: true
Local user paths redacted: true
Device identifiers redacted: true
Only sanitized diagnostics selected for public sharing: true

Reviewer: automated sanitized export plus review-public-evidence-redaction.ps1 gate
UTC: $((Get-Date).ToUniversalTime().ToString("o"))
Notes: Raw evidence remains local. This folder contains sanitized copies of text artifacts; images are omitted unless -IncludeImages was used after visual review.
"@ -Encoding UTF8

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
