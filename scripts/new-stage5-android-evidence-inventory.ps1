[CmdletBinding()]
param(
    [string]$EvidenceRoot = "artifacts\android",
    [string]$OutputPath = "",
    [string]$SourceCommit = "",
    [string]$ApkSha256 = "",
    [string]$DeviceIdentity = ""
)

$ErrorActionPreference = "Stop"

function Get-Sha256Hex {
    param([Parameter(Mandatory = $true)][byte[]]$Bytes)

    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        return (($sha256.ComputeHash($Bytes) | ForEach-Object { $_.ToString("x2") }) -join "")
    } finally {
        $sha256.Dispose()
    }
}

function Get-FileSha256Hex {
    param([Parameter(Mandatory = $true)][string]$Path)

    $stream = [System.IO.File]::Open(
        $Path,
        [System.IO.FileMode]::Open,
        [System.IO.FileAccess]::Read,
        [System.IO.FileShare]::Read
    )
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        return (($sha256.ComputeHash($stream) | ForEach-Object { $_.ToString("x2") }) -join "")
    } finally {
        $sha256.Dispose()
        $stream.Dispose()
    }
}

$resolvedEvidenceRoot = (Resolve-Path -LiteralPath $EvidenceRoot).Path
if (-not (Test-Path -LiteralPath $resolvedEvidenceRoot -PathType Container)) {
    throw "Evidence root is not a directory: $EvidenceRoot"
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$evidenceRootPrefix = $resolvedEvidenceRoot.TrimEnd(
    [System.IO.Path]::DirectorySeparatorChar,
    [System.IO.Path]::AltDirectorySeparatorChar
) + [System.IO.Path]::DirectorySeparatorChar
if ([string]::IsNullOrWhiteSpace($SourceCommit)) {
    $SourceCommit = ((& git -C $repoRoot rev-parse HEAD 2>$null) | Out-String).Trim()
}
if ($ApkSha256 -and $ApkSha256 -notmatch '^[0-9a-fA-F]{64}$') {
    throw "ApkSha256 must be a 64-character SHA-256 value."
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $OutputPath = Join-Path $repoRoot "artifacts\stage5-android-evidence-inventory-$timestamp.json"
} elseif (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path (Get-Location).Path $OutputPath
}
$resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
if ($resolvedOutputPath.StartsWith(
        $evidenceRootPrefix,
        [StringComparison]::OrdinalIgnoreCase
    ) -or [string]::Equals(
        $resolvedOutputPath,
        $resolvedEvidenceRoot,
        [StringComparison]::OrdinalIgnoreCase
    )) {
    throw "The inventory output must be outside the evidence root so the evidence remains read-only."
}
if (Test-Path -LiteralPath $resolvedOutputPath) {
    throw "Refusing to overwrite an existing evidence inventory: $resolvedOutputPath"
}

$files = @(
    Get-ChildItem -LiteralPath $resolvedEvidenceRoot -Recurse -File -Force |
        Where-Object {
            -not [string]::Equals(
                [System.IO.Path]::GetFullPath($_.FullName),
                $resolvedOutputPath,
                [StringComparison]::OrdinalIgnoreCase
            )
        } |
        ForEach-Object {
            $fullName = [System.IO.Path]::GetFullPath($_.FullName)
            if (-not $fullName.StartsWith($evidenceRootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Evidence file resolved outside the requested root: $fullName"
            }
            [pscustomobject]@{
                File = $_
                RelativePath = $fullName.Substring($evidenceRootPrefix.Length).Replace('\', '/')
            }
        }
)
[Array]::Sort(
    $files,
    [System.Collections.Generic.Comparer[object]]::Create({
        param($left, $right)
        [StringComparer]::Ordinal.Compare($left.RelativePath, $right.RelativePath)
    })
)

$entries = [System.Collections.Generic.List[object]]::new()
$canonicalEntryLines = [System.Collections.Generic.List[string]]::new()
$totalBytes = [int64]0
foreach ($candidate in $files) {
    $before = Get-Item -LiteralPath $candidate.File.FullName -Force
    $sha256 = Get-FileSha256Hex -Path $before.FullName
    $after = Get-Item -LiteralPath $candidate.File.FullName -Force
    if ($before.Length -ne $after.Length -or
        $before.LastWriteTimeUtc.Ticks -ne $after.LastWriteTimeUtc.Ticks) {
        throw "Evidence changed while it was being hashed: $($candidate.RelativePath)"
    }

    $entry = [ordered]@{
        relativePath = $candidate.RelativePath
        sizeBytes = [int64]$after.Length
        sha256 = $sha256
        modifiedUtc = $after.LastWriteTimeUtc.ToString(
            "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'",
            [Globalization.CultureInfo]::InvariantCulture
        )
    }
    $entries.Add([pscustomobject]$entry)
    $canonicalEntryLines.Add(($entry | ConvertTo-Json -Compress))
    $totalBytes += $after.Length
}

$canonicalBytes = [Text.Encoding]::UTF8.GetBytes(
    [string]::Join("`n", $canonicalEntryLines)
)
$manifestSha256 = Get-Sha256Hex -Bytes $canonicalBytes
$repoPrefix = $repoRoot.TrimEnd(
    [System.IO.Path]::DirectorySeparatorChar,
    [System.IO.Path]::AltDirectorySeparatorChar
) + [System.IO.Path]::DirectorySeparatorChar
$rootForRecord = if ($resolvedEvidenceRoot.StartsWith(
        $repoPrefix,
        [StringComparison]::OrdinalIgnoreCase
    )) {
    $resolvedEvidenceRoot.Substring($repoPrefix.Length).Replace('\', '/')
} else {
    $resolvedEvidenceRoot
}

$inventory = [ordered]@{
    schemaVersion = 1
    kind = "stage5-android-regression-evidence-inventory"
    generatedUtc = [DateTime]::UtcNow.ToString(
        "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'",
        [Globalization.CultureInfo]::InvariantCulture
    )
    evidenceRoot = $rootForRecord
    sourceCommit = $SourceCommit
    apkSha256 = $ApkSha256.ToLowerInvariant()
    deviceIdentity = $DeviceIdentity
    fileCount = $entries.Count
    totalBytes = $totalBytes
    manifestSha256 = $manifestSha256
    manifestHashScope = "UTF-8, LF-joined compact JSON entry records in ordinal relative-path order"
    entries = $entries
}

$outputDirectory = Split-Path -Parent $resolvedOutputPath
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$temporaryPath = "$resolvedOutputPath.$([Guid]::NewGuid().ToString('N')).tmp"
try {
    [System.IO.File]::WriteAllText(
        $temporaryPath,
        ($inventory | ConvertTo-Json -Depth 6),
        [Text.UTF8Encoding]::new($false)
    )
    Move-Item -LiteralPath $temporaryPath -Destination $resolvedOutputPath
} finally {
    if (Test-Path -LiteralPath $temporaryPath) {
        Remove-Item -LiteralPath $temporaryPath -Force
    }
}

Write-Host "Stage 5 Android evidence inventory: $resolvedOutputPath"
Write-Host "Files: $($entries.Count); bytes: $totalBytes; manifest SHA-256: $manifestSha256"
