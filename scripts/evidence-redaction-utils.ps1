function ConvertTo-RedactedEvidenceText([AllowNull()][string]$Text) {
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

function ConvertTo-RedactedLogLine([AllowNull()][string]$Line) {
    if ($null -eq $Line) {
        return ""
    }

    $redacted = $Line
    $redacted = $redacted -replace '(?i)\b(password|passwd|refresh[_-]?token|access[_-]?token|login[_-]?key|steamLoginSecure|sessionid|shared[_-]?secret|identity[_-]?secret|guard[_-]?code|twofactorcode|authorization|account[_-]?name|username|user[_-]?name|device[_-]?serial|serial)\b\s*[:=]\s*["'']?[^"'',;&\s]+', '$1=<redacted>'
    $redacted = $redacted -replace '(?i)\bBearer\s+[A-Za-z0-9._~+/=-]+', 'Bearer <redacted>'
    return ConvertTo-RedactedEvidenceText $redacted
}

function Get-EvidenceTextFileExtensions {
    return @(
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
}

function Get-EvidenceImageFileExtensions {
    return @(".png", ".jpg", ".jpeg", ".webp")
}

function Get-EvidenceLocalOnlyPathPatterns {
    return @(
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
}

function Test-EvidenceLocalOnlyPath([string]$RelativePath) {
    $normalized = $RelativePath -replace '/', '\'
    foreach ($pattern in Get-EvidenceLocalOnlyPathPatterns) {
        if ($normalized -match $pattern) {
            return $true
        }
    }

    return $false
}

function Get-EvidenceSensitiveTextChecks {
    return @(
        [pscustomobject]@{
            Name = "credential/token assignment"
            Pattern = "(?i)\b(password|passwd|guard_code|shared_secret|refresh_token|access_token|session_token)\b\s*[:=]\s*['""]?[A-Za-z0-9+/_=.\-]{4,}"
        },
        [pscustomobject]@{
            Name = "email address"
            Pattern = "(?i)\b[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}\b"
        },
        [pscustomobject]@{
            Name = "local Windows user path"
            Pattern = "(?i)\b[A-Z]:\\Users\\[^\\\s]+"
        },
        [pscustomobject]@{
            Name = "Android package-private data path"
            Pattern = "/data/(user|data)/0/[^/\s]+"
        },
        [pscustomobject]@{
            Name = "known connected device serial"
            Pattern = "\bRFCY[0-9A-Z]{7,}\b"
        },
        [pscustomobject]@{
            Name = "Steam shared secret label"
            Pattern = "(?i)\bshared_secret\b"
        },
        [pscustomobject]@{
            Name = "raw save/profile content marker"
            Pattern = "(?i)\b(profile|save)\s*(name|slot|content|data)\s*[:=]\s*[^<\r\n][^\r\n]{3,}"
        }
    )
}

function Get-PublicEvidenceRedactionReviewFields {
    return @(
        "Screenshots manually reviewed",
        "Credential suggestions absent",
        "Account identifiers redacted",
        "Device notifications absent",
        "Private save/profile contents absent",
        "Steam credentials absent",
        "Steam Guard codes absent",
        "Refresh/session tokens absent",
        "Local user paths redacted",
        "Device identifiers redacted",
        "Only sanitized diagnostics selected for public sharing"
    )
}

function Format-PublicEvidenceRedactionReviewFields([bool]$Completed) {
    $state = if ($Completed) { "true" } else { "false" }
    return @(Get-PublicEvidenceRedactionReviewFields | ForEach-Object { "${_}: $state" })
}
