param(
    [Parameter(Mandatory = $true)]
    [string]$EvidenceDir,
    [string]$PackageName = "com.sts2launcher.overhaul.fork.dev",
    [string]$DeviceSerial = ""
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")

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
    $output = & adb @adbPrefix @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0 -and -not $AllowFailure) {
        throw "adb $($Arguments -join ' ') failed with exit code $exitCode`: $output"
    }

    return ($output -join [Environment]::NewLine)
}

function Invoke-RunAsShell([string]$Command, [switch]$AllowFailure) {
    return Invoke-AdbText -Arguments @("shell", "run-as", $PackageName, "sh", "-c", $Command) -AllowFailure:$AllowFailure
}

$summaryPath = Join-Path $resolvedEvidenceDir "capture-summary.txt"
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss K"

Set-Content -LiteralPath $summaryPath -Value @(
    "Steam version-selection evidence capture",
    "Timestamp: $timestamp",
    "Package: $PackageName",
    "Device serial: $DeviceSerial",
    "Evidence dir: $resolvedEvidenceDir",
    "",
    "This helper intentionally avoids shared preferences and credential-bearing files."
) -Encoding UTF8

$devicesPath = Join-Path $logsDir "adb-devices.txt"
Invoke-AdbText -Arguments @("devices", "-l") -AllowFailure | Set-Content -LiteralPath $devicesPath -Encoding UTF8

$logcatFullPath = Join-Path $logsDir "logcat-full.txt"
Invoke-AdbText -Arguments @("logcat", "-d") -AllowFailure | Set-Content -LiteralPath $logcatFullPath -Encoding UTF8

$logcatFocusedPath = Join-Path $logsDir "logcat-steam-version-focused.txt"
$focusedPatterns = "Steam|SteamKit|Depot|Branch|branch|version|PCK|pck|Cloud|backup|Push|Pull|Launcher|GodotApp|NativeFallback|PatchHelper|Exception|FATAL|AndroidRuntime"
Get-Content -LiteralPath $logcatFullPath |
    Select-String -Pattern $focusedPatterns |
    ForEach-Object { $_.Line } |
    Set-Content -LiteralPath $logcatFocusedPath -Encoding UTF8

$listingPath = Join-Path $diagnosticsDir "app-game-cache-listing.txt"
Invoke-RunAsShell -Command "ls -la files files/game files/game_versions 2>/dev/null || true" -AllowFailure |
    Set-Content -LiteralPath $listingPath -Encoding UTF8

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

$markerPaths = $markerListText -split "`r?`n" | Where-Object { $_.Trim().Length -gt 0 }
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
    "- $devicesPath",
    "- $logcatFullPath",
    "- $logcatFocusedPath",
    "- $listingPath",
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
