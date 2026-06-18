param(
    [Parameter(Mandatory = $true)]
    [string]$EvidenceDir
)

$ErrorActionPreference = "Stop"

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

function Get-RelativePath([string]$Path) {
    $rootWithSeparator = $root
    if (-not $rootWithSeparator.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
        $rootWithSeparator += [System.IO.Path]::DirectorySeparatorChar
    }

    $rootUri = [System.Uri]::new($rootWithSeparator)
    $pathUri = [System.Uri]::new($Path)
    return [System.Uri]::UnescapeDataString(
        $rootUri.MakeRelativeUri($pathUri).ToString()
    ) -replace '/', [System.IO.Path]::DirectorySeparatorChar
}

function Test-CompletedRedactionReview {
    $reviewPath = Join-Path $root "PUBLIC_EVIDENCE_REDACTION_REVIEW.txt"
    if (-not (Test-Path -LiteralPath $reviewPath)) {
        return $false
    }

    $text = Get-Content -Raw -LiteralPath $reviewPath
    $required = @(
        "Screenshots manually reviewed:\s*true",
        "Credential suggestions absent:\s*true",
        "Account identifiers redacted:\s*true",
        "Device notifications absent:\s*true",
        "Private save/profile contents absent:\s*true",
        "Steam credentials absent:\s*true",
        "Steam Guard codes absent:\s*true",
        "Refresh/session tokens absent:\s*true",
        "Local user paths redacted:\s*true",
        "Device identifiers redacted:\s*true",
        "Only sanitized diagnostics selected for public sharing:\s*true"
    )

    foreach ($pattern in $required) {
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

$localOnlyPathPatterns = @(
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

$textContentPatterns = @(
    @{
        Name = "credential/token assignment"
        Pattern = "(?i)\b(password|passwd|guard_code|shared_secret|refresh_token|access_token|session_token)\b\s*[:=]\s*['""]?[A-Za-z0-9+/_=.\-]{4,}"
    },
    @{
        Name = "email address"
        Pattern = "(?i)\b[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}\b"
    },
    @{
        Name = "local Windows user path"
        Pattern = "(?i)\b[A-Z]:\\Users\\[^\\\s]+"
    },
    @{
        Name = "Android package-private data path"
        Pattern = "/data/(user|data)/0/[^/\s]+"
    },
    @{
        Name = "known connected device serial"
        Pattern = "\bRFCY[0-9A-Z]{7,}\b"
    },
    @{
        Name = "Steam shared secret label"
        Pattern = "(?i)\bshared_secret\b"
    },
    @{
        Name = "raw save/profile content marker"
        Pattern = "(?i)\b(profile|save)\s*(name|slot|content|data)\s*[:=]\s*[^<\r\n][^\r\n]{3,}"
    }
)

foreach ($file in $files) {
    $relative = Get-RelativePath $file.FullName
    $normalized = $relative -replace '/', '\'

    foreach ($pattern in $localOnlyPathPatterns) {
        if ($normalized -match $pattern) {
            Add-Failure "Local-only or high-risk artifact path is present: $relative"
            break
        }
    }

    if ($imageExtensions -contains $file.Extension.ToLowerInvariant()) {
        continue
    }

    if ($textExtensions -notcontains $file.Extension.ToLowerInvariant()) {
        Add-Warning "Skipped non-text artifact: $relative"
        continue
    }

    $text = Get-Content -Raw -LiteralPath $file.FullName
    foreach ($check in $textContentPatterns) {
        if ($text -match $check.Pattern) {
            Add-Failure "$($check.Name) detected in $relative"
        }
    }
}

$images = @($files | Where-Object { $imageExtensions -contains $_.Extension.ToLowerInvariant() })
if ($images.Count -gt 0 -and -not $completedRedactionReview) {
    foreach ($image in $images) {
        Add-Failure "Screenshot/image requires completed PUBLIC_EVIDENCE_REDACTION_REVIEW.txt before public sharing: $(Get-RelativePath $image.FullName)"
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
