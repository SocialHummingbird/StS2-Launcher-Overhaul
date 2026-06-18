param(
    [Parameter(Mandatory = $true)]
    [string]$EvidenceDir,
    [string]$AdbPath = "adb",
    [string]$PackageName = "com.sts2launcher.overhaul.fork.dev",
    [string]$DeviceSerial = "",
    [switch]$IncludeRawLogcat
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
. (Join-Path $PSScriptRoot "android-adb-utils.ps1")
$AdbPath = Resolve-AndroidAdbPath -AdbPath $AdbPath

function Resolve-RepoPath([string]$RelativePath) {
    $normalized = $RelativePath -replace '[\\/]', [System.IO.Path]::DirectorySeparatorChar
    return Join-Path $root $normalized
}

$resolvedEvidenceDir = $EvidenceDir
if (-not [System.IO.Path]::IsPathRooted($resolvedEvidenceDir)) {
    $resolvedEvidenceDir = Resolve-RepoPath $resolvedEvidenceDir
}

New-Item -ItemType Directory -Force $resolvedEvidenceDir | Out-Null
$logsDir = Join-Path $resolvedEvidenceDir "logs"
$markersDir = Join-Path $resolvedEvidenceDir "branch-markers"
$diagnosticsDir = Join-Path $resolvedEvidenceDir "diagnostics"
$backupsDir = Join-Path $resolvedEvidenceDir "backup-evidence"
$branchSwitchMarkerFileName = "last_game_branch_switch.txt"
$manualPullMarkerFileName = "last_manual_cloud_pull.txt"
$manualPushMarkerFileName = "last_manual_cloud_push.txt"
$manualPushBlockedMarkerFileName = "last_manual_cloud_push_blocked.txt"
$cacheCleanupMarkerFileName = "last_game_version_cache_cleanup.txt"
$redownloadMarkerFileName = "last_game_version_redownload.txt"
$branchAvailabilityMarkerFileName = "last_steam_branch_availability.txt"
New-Item -ItemType Directory -Force $logsDir | Out-Null
New-Item -ItemType Directory -Force $markersDir | Out-Null
New-Item -ItemType Directory -Force $diagnosticsDir | Out-Null
New-Item -ItemType Directory -Force $backupsDir | Out-Null

$adbPrefix = @()
if ($DeviceSerial.Trim().Length -gt 0) {
    $adbPrefix = @("-s", $DeviceSerial)
}

function Invoke-AdbText([string[]]$Arguments, [switch]$AllowFailure) {
    $output = & $AdbPath @adbPrefix @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0 -and -not $AllowFailure) {
        throw "adb $($Arguments -join ' ') failed with exit code $exitCode`: $output"
    }

    return ($output -join [Environment]::NewLine)
}

function Invoke-RunAsShell([string]$Command, [switch]$AllowFailure) {
    return Invoke-AdbText -Arguments @("shell", "run-as", $PackageName, "sh", "-c", $Command) -AllowFailure:$AllowFailure
}

function Redact-LogLine([string]$Line) {
    if ($null -eq $Line) {
        return ""
    }

    $redacted = $Line
    $redacted = $redacted -replace '(?i)\b(password|passwd|refresh[_-]?token|access[_-]?token|login[_-]?key|steamLoginSecure|sessionid|shared[_-]?secret|identity[_-]?secret|guard[_-]?code|twofactorcode|authorization|account[_-]?name|username|user[_-]?name|device[_-]?serial|serial)\b\s*[:=]\s*["'']?[^"'',;&\s]+', '$1=<redacted>'
    $redacted = $redacted -replace '(?i)\bBearer\s+[A-Za-z0-9._~+/=-]+', 'Bearer <redacted>'
    $redacted = $redacted -replace '[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}', '<redacted-email>'
    $redacted = $redacted -replace '(?i)\b([A-Z]:\\Users\\)[^\\\s]+', '${1}<redacted-user>'
    $redacted = $redacted -replace '(?i)(/Users/|/home/)[^/\s]+', '${1}<redacted-user>'
    return $redacted
}

$summaryPath = Join-Path $resolvedEvidenceDir "capture-summary.txt"
$artifactHygienePath = Join-Path $resolvedEvidenceDir "ARTIFACT_HYGIENE.txt"
$publicShareManifestPath = Join-Path $resolvedEvidenceDir "PUBLIC_SHARE_MANIFEST.txt"
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss K"

Set-Content -LiteralPath $summaryPath -Value @(
    "Steam version-selection evidence capture",
    "Timestamp: $timestamp",
    "Package: $PackageName",
    "Device serial: $DeviceSerial",
    "Evidence dir: $resolvedEvidenceDir",
    "Raw full logcat included: $($IncludeRawLogcat.IsPresent)",
    "",
    "This helper intentionally avoids shared preferences and credential-bearing files."
) -Encoding UTF8

Set-Content -LiteralPath $artifactHygienePath -Value @(
    "Steam version-selection evidence artifact hygiene",
    "",
    "Do not publish Steam credentials, refresh tokens, shared preferences, private save contents, or unsanitized logs.",
    "Raw full logcat is omitted by default. If logs/logcat-full.txt was captured with -IncludeRawLogcat, treat it as local-only raw diagnostics unless manually reviewed and redacted.",
    "Treat logs/logcat-steam-version-focused.txt as local-only raw diagnostics unless manually reviewed and redacted.",
    "Use logs/logcat-steam-version-focused-redacted.txt as the safer default public log excerpt, but review it manually before posting.",
    "The redacted focused log is best-effort pattern-based redaction and cannot guarantee every identifier was removed.",
    "If diagnostics show SteamKit debug logs opt-in enabled, confirm SteamKit debug logs sanitized for credentials/tokens before sharing any log excerpt.",
    "The diagnostics/steamkit-debug-log-setting.txt file records whether sts2_steamkit_debug_logs was enabled during capture."
) -Encoding UTF8

Set-Content -LiteralPath $publicShareManifestPath -Value @(
    "Steam version-selection public sharing manifest",
    "",
    "Prefer these artifacts for public GitHub issue reports after manual review:",
    "- ARTIFACT_HYGIENE.txt",
    "- diagnostics/steamkit-debug-log-setting.txt",
    "- diagnostics/logcat-redaction-summary.txt",
    "- logs/logcat-steam-version-focused-redacted.txt",
    "- branch-markers/steam-branch-marker-list.txt",
    "- branch-markers/last_steam_branch_availability.txt",
    "- branch-markers/last_game_branch_switch.txt",
    "- branch-markers/last_manual_cloud_pull.txt",
    "- branch-markers/last_manual_cloud_push.txt",
    "- branch-markers/last_manual_cloud_push_blocked.txt",
    "- diagnostics/game-version-cache-tree.txt",
    "- diagnostics/game-version-cache-sizes.txt",
    "- backup-evidence/pre-push-backup-counts.txt",
    "",
    "Local-only or manual-review artifacts:",
    "- logs/logcat-full.txt",
    "- logs/logcat-steam-version-focused.txt",
    "- full launcher diagnostics reports listed in diagnostics/launcher-diagnostics-index.txt",
    "- any private save files, shared preferences, credentials, refresh tokens, or unsanitized logs",
    "",
    "Generated redaction is best-effort. Review every artifact before public posting."
) -Encoding UTF8

$devicesPath = Join-Path $logsDir "adb-devices.txt"
Invoke-AdbText -Arguments @("devices", "-l") -AllowFailure | Set-Content -LiteralPath $devicesPath -Encoding UTF8

$steamKitDebugLogSettingPath = Join-Path $diagnosticsDir "steamkit-debug-log-setting.txt"
$steamKitDebugLogSetting = Invoke-AdbText -Arguments @("shell", "settings", "get", "global", "sts2_steamkit_debug_logs") -AllowFailure
Set-Content -LiteralPath $steamKitDebugLogSettingPath -Value @(
    "Android global setting: sts2_steamkit_debug_logs",
    "Value: $steamKitDebugLogSetting",
    "Expected routine evidence value: 0 or null",
    "If value is 1, SteamKit debug logging was explicitly enabled and public log excerpts must confirm sanitizer diagnostics before sharing."
) -Encoding UTF8

$logcatFullPath = Join-Path $logsDir "logcat-full.txt"
$logcatRawText = Invoke-AdbText -Arguments @("logcat", "-d") -AllowFailure
$logcatLines = $logcatRawText -split '\r?\n'
if ($IncludeRawLogcat) {
    Set-Content -LiteralPath $logcatFullPath -Value $logcatLines -Encoding UTF8
} else {
    Set-Content -LiteralPath $logcatFullPath -Value @(
        "Raw full logcat omitted by default for artifact hygiene.",
        "Re-run with -IncludeRawLogcat only for local diagnostics, then manually review and redact before public sharing.",
        "Use logs/logcat-steam-version-focused-redacted.txt as the safer default public log excerpt."
    ) -Encoding UTF8
}

$logcatFocusedPath = Join-Path $logsDir "logcat-steam-version-focused.txt"
$logcatFocusedRedactedPath = Join-Path $logsDir "logcat-steam-version-focused-redacted.txt"
$logcatRedactionSummaryPath = Join-Path $diagnosticsDir "logcat-redaction-summary.txt"
$focusedPatterns = "Steam|SteamKit|Depot|Branch|branch|version|PCK|pck|Cloud|backup|Push|Pull|Launcher|GodotApp|NativeFallback|PatchHelper|Exception|FATAL|AndroidRuntime"
$focusedLogLines = $logcatLines |
    Select-String -Pattern $focusedPatterns |
    ForEach-Object { $_.Line }
Set-Content -LiteralPath $logcatFocusedPath -Value $focusedLogLines -Encoding UTF8
$redactedLogHeader = @(
    "Best-effort redacted Steam version-selection focused logcat.",
    "Review manually before public posting; pattern-based redaction cannot guarantee every identifier was removed.",
    ""
)
$redactedLogBody = @()
$redactedChangedLineCount = 0
foreach ($line in $focusedLogLines) {
    $redactedLine = Redact-LogLine $line
    $redactedLogBody += $redactedLine
    if ($redactedLine -ne $line) {
        $redactedChangedLineCount += 1
    }
}
Set-Content -LiteralPath $logcatFocusedRedactedPath -Value ($redactedLogHeader + $redactedLogBody) -Encoding UTF8
Set-Content -LiteralPath $logcatRedactionSummaryPath -Value @(
    "Steam version-selection focused logcat redaction summary",
    "Focused log lines processed: $($focusedLogLines.Count)",
    "Focused log lines changed by best-effort redaction: $redactedChangedLineCount",
    "Redacted artifact: $logcatFocusedRedactedPath",
    "Manual review still required before public posting."
) -Encoding UTF8

$listingPath = Join-Path $diagnosticsDir "app-game-cache-listing.txt"
Invoke-RunAsShell -Command "ls -la files files/game files/game_versions 2>/dev/null || true" -AllowFailure |
    Set-Content -LiteralPath $listingPath -Encoding UTF8

$launcherDiagnosticsIndexPath = Join-Path $diagnosticsDir "launcher-diagnostics-index.txt"
Set-Content -LiteralPath $launcherDiagnosticsIndexPath -Value @(
    "Launcher diagnostics artifact index",
    "",
    "Internal diagnostics files:",
    (Invoke-RunAsShell -Command "find files/diagnostics -maxdepth 1 -type f -name '*diagnostics*.txt' -print 2>/dev/null | sort || true" -AllowFailure),
    "",
    "External diagnostics files:",
    (Invoke-AdbText -Arguments @("shell", "sh", "-c", "find /storage/emulated/0/Android/data/$PackageName/files/diagnostics -maxdepth 1 -type f -name '*diagnostics*.txt' -print 2>/dev/null | sort || true") -AllowFailure),
    "",
    "Full launcher diagnostics reports can contain account names, local paths, device details, and log excerpts.",
    "Attach full reports manually only after review/redaction."
) -Encoding UTF8

$cacheTreePath = Join-Path $diagnosticsDir "game-version-cache-tree.txt"
Invoke-RunAsShell -Command "find files/game_versions -maxdepth 4 -print 2>/dev/null | sort | head -300 || true" -AllowFailure |
    Set-Content -LiteralPath $cacheTreePath -Encoding UTF8

$cacheSizePath = Join-Path $diagnosticsDir "game-version-cache-sizes.txt"
Invoke-RunAsShell -Command "du -sk files/game_versions/* 2>/dev/null || true" -AllowFailure |
    Set-Content -LiteralPath $cacheSizePath -Encoding UTF8

$backupListPath = Join-Path $backupsDir "pre-push-backup-list.txt"
Invoke-AdbText -Arguments @("shell", "sh", "-c", "find /storage/emulated/0/StS2Launcher/Saves -type f \( -name '*.local-pre-push.bak' -o -name '*.cloud-pre-push.bak' \) 2>/dev/null | sort | head -300 || true") -AllowFailure |
    Set-Content -LiteralPath $backupListPath -Encoding UTF8

$backupCountsPath = Join-Path $backupsDir "pre-push-backup-counts.txt"
Invoke-AdbText -Arguments @("shell", "sh", "-c", "printf 'local-pre-push: '; find /storage/emulated/0/StS2Launcher/Saves -type f -name '*.local-pre-push.bak' 2>/dev/null | wc -l; printf 'cloud-pre-push: '; find /storage/emulated/0/StS2Launcher/Saves -type f -name '*.cloud-pre-push.bak' 2>/dev/null | wc -l") -AllowFailure |
    Set-Content -LiteralPath $backupCountsPath -Encoding UTF8

$markerListText = Invoke-RunAsShell -Command "find files -name steam_branch.txt -type f 2>/dev/null || true" -AllowFailure
$markerListPath = Join-Path $markersDir "steam-branch-marker-list.txt"
$markerListText | Set-Content -LiteralPath $markerListPath -Encoding UTF8

$markerPaths = $markerListText -split '\r?\n' | Where-Object { $_.Trim().Length -gt 0 }
foreach ($markerPath in $markerPaths) {
    $safeName = ($markerPath.Trim() -replace "[^A-Za-z0-9._-]", "_")
    $destination = Join-Path $markersDir $safeName
    Invoke-RunAsShell -Command "cat '$($markerPath.Trim())' 2>/dev/null || true" -AllowFailure |
        Set-Content -LiteralPath $destination -Encoding UTF8
}

$branchSwitchMarkerPath = Join-Path $markersDir $branchSwitchMarkerFileName
Invoke-RunAsShell -Command "cat files/$branchSwitchMarkerFileName 2>/dev/null || true" -AllowFailure |
    Set-Content -LiteralPath $branchSwitchMarkerPath -Encoding UTF8

$manualPullMarkerPath = Join-Path $markersDir $manualPullMarkerFileName
Invoke-RunAsShell -Command "cat files/$manualPullMarkerFileName 2>/dev/null || true" -AllowFailure |
    Set-Content -LiteralPath $manualPullMarkerPath -Encoding UTF8

$manualPushMarkerPath = Join-Path $markersDir $manualPushMarkerFileName
Invoke-RunAsShell -Command "cat files/$manualPushMarkerFileName 2>/dev/null || true" -AllowFailure |
    Set-Content -LiteralPath $manualPushMarkerPath -Encoding UTF8

$manualPushBlockedMarkerPath = Join-Path $markersDir $manualPushBlockedMarkerFileName
Invoke-RunAsShell -Command "cat files/$manualPushBlockedMarkerFileName 2>/dev/null || true" -AllowFailure |
    Set-Content -LiteralPath $manualPushBlockedMarkerPath -Encoding UTF8

$cacheCleanupMarkerPath = Join-Path $markersDir $cacheCleanupMarkerFileName
Invoke-RunAsShell -Command "cat files/$cacheCleanupMarkerFileName 2>/dev/null || true" -AllowFailure |
    Set-Content -LiteralPath $cacheCleanupMarkerPath -Encoding UTF8

$redownloadMarkerPath = Join-Path $markersDir $redownloadMarkerFileName
Invoke-RunAsShell -Command "cat files/$redownloadMarkerFileName 2>/dev/null || true" -AllowFailure |
    Set-Content -LiteralPath $redownloadMarkerPath -Encoding UTF8

$branchAvailabilityMarkerPath = Join-Path $markersDir $branchAvailabilityMarkerFileName
Invoke-RunAsShell -Command "cat files/$branchAvailabilityMarkerFileName 2>/dev/null || true" -AllowFailure |
    Set-Content -LiteralPath $branchAvailabilityMarkerPath -Encoding UTF8

Add-Content -LiteralPath $summaryPath -Value @(
    "",
    "Captured:",
    "- $artifactHygienePath",
    "- $publicShareManifestPath",
    "- $devicesPath",
    "- $steamKitDebugLogSettingPath",
    "- $logcatFullPath",
    "- $logcatFocusedPath",
    "- $logcatFocusedRedactedPath",
    "- $logcatRedactionSummaryPath",
    "- $listingPath",
    "- $launcherDiagnosticsIndexPath",
    "- $cacheTreePath",
    "- $cacheSizePath",
    "- $backupListPath",
    "- $backupCountsPath",
    "- $markerListPath",
    "- $branchSwitchMarkerPath ($branchSwitchMarkerFileName)",
    "- $manualPullMarkerPath ($manualPullMarkerFileName)",
    "- $manualPushMarkerPath ($manualPushMarkerFileName)",
    "- $manualPushBlockedMarkerPath ($manualPushBlockedMarkerFileName)",
    "- $cacheCleanupMarkerPath ($cacheCleanupMarkerFileName)",
    "- $redownloadMarkerPath ($redownloadMarkerFileName)",
    "- $branchAvailabilityMarkerPath ($branchAvailabilityMarkerFileName)",
    "- $($markerPaths.Count) branch marker file(s)"
) -Encoding UTF8

Write-Host "Captured Steam version-selection evidence:"
Write-Host $resolvedEvidenceDir
