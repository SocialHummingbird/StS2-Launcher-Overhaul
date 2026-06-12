param(
    [string]$Label = "",
    [string]$OutputRoot = "artifacts\android"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")

function Resolve-RepoPath([string]$RelativePath) {
    $normalized = $RelativePath -replace '[\\/]', [System.IO.Path]::DirectorySeparatorChar
    return Join-Path $root $normalized
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$safeLabel = $Label.Trim()

if ($safeLabel.Length -gt 0) {
    $safeLabel = $safeLabel -replace "[^A-Za-z0-9._-]", "-"
    $folderName = "steam-version-selection-$timestamp-$safeLabel"
} else {
    $folderName = "steam-version-selection-$timestamp"
}

$outputRootPath = Resolve-RepoPath $OutputRoot
$evidenceDir = Join-Path $outputRootPath $folderName
$logsDir = Join-Path $evidenceDir "logs"
$diagnosticsDir = Join-Path $evidenceDir "diagnostics"
$markersDir = Join-Path $evidenceDir "branch-markers"
$screenshotsDir = Join-Path $evidenceDir "screenshots"
$backupsDir = Join-Path $evidenceDir "backup-evidence"

New-Item -ItemType Directory -Force $logsDir | Out-Null
New-Item -ItemType Directory -Force $diagnosticsDir | Out-Null
New-Item -ItemType Directory -Force $markersDir | Out-Null
New-Item -ItemType Directory -Force $screenshotsDir | Out-Null
New-Item -ItemType Directory -Force $backupsDir | Out-Null

$templatePath = Join-Path $root "docs\steam-version-selection-evidence-template.md"
$evidencePath = Join-Path $evidenceDir "evidence.md"

if (Test-Path -LiteralPath $templatePath) {
    Copy-Item -LiteralPath $templatePath -Destination $evidencePath -Force
} else {
    Set-Content -LiteralPath $evidencePath -Value "# Steam Version Selection Evidence`n" -Encoding UTF8
}

$readmePath = Join-Path $evidenceDir "README.md"
$readme = @"
# Steam Version Selection Evidence Folder

Created: $timestamp
Label: $Label

Use this folder for one validation run. Do not place Steam credentials or personally identifying account details in artifacts.

## Fill first

- ``evidence.md`` copied from ``docs/steam-version-selection-evidence-template.md``.

## Put artifacts here

- ``logs/``: focused and full logcat captures.
- ``diagnostics/``: launcher diagnostics bundles, copied diagnostics text, bounded branch-cache tree snapshots, and cache-size snapshots.
- ``branch-markers/``: copied ``steam_branch.txt`` files for public/default and beta caches.
- ``branch-markers/``: copied ``last_game_branch_switch.txt`` evidence when branch-switch validation runs.
- ``branch-markers/``: copied ``last_manual_cloud_pull.txt`` evidence when Pull-after-switch validation runs.
- ``branch-markers/``: copied ``last_manual_cloud_push.txt`` evidence when Push-after-switch validation runs.
- Manual Push evidence marker filename: ``last_manual_cloud_push.txt``.
- ``branch-markers/``: copied ``last_manual_cloud_push_blocked.txt`` evidence when Push is blocked before upload.
- ``branch-markers/``: copied ``last_game_version_cache_cleanup.txt`` evidence when cache cleanup validation runs.
- ``branch-markers/``: copied ``last_game_version_redownload.txt`` evidence when selected-version redownload validation runs.
- ``screenshots/``: launcher, fallback, selector, warning, and game-launch screenshots.
- ``backup-evidence/``: local pre-Push and cloud pre-Push backup summaries only. Do not include secrets.

## Reference docs

- ``docs/steam-version-selection-architecture.md``
- ``docs/steam-version-selection-validation.md``
- ``docs/steam-version-selection-runbook.md``
- ``docs/steam-version-selection-user-guide.md``
- ``docs/steam-version-selection-completion-audit.md``

## Safety gates

Do not run manual Push after a branch switch unless:

1. Pull from Cloud was run after the branch switch for the selected version.
2. Android local saves exist.
3. Local backup is enabled.
4. Backup storage permission is available.
5. Local pre-Push backup evidence exists.
6. Cloud pre-Push backup evidence exists.
7. The overwrite-risk confirmation is intentional.
"@

Set-Content -LiteralPath $readmePath -Value $readme -Encoding UTF8

Write-Host "Created Steam version-selection evidence folder:"
Write-Host $evidenceDir
