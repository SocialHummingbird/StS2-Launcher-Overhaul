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
$inventoriesDir = Join-Path $evidenceDir "inventories"

New-Item -ItemType Directory -Force $logsDir | Out-Null
New-Item -ItemType Directory -Force $diagnosticsDir | Out-Null
New-Item -ItemType Directory -Force $markersDir | Out-Null
New-Item -ItemType Directory -Force $screenshotsDir | Out-Null
New-Item -ItemType Directory -Force $backupsDir | Out-Null
New-Item -ItemType Directory -Force $inventoriesDir | Out-Null

$templatePath = Join-Path $root "docs\steam-version-selection-evidence-template.md"
$evidencePath = Join-Path $evidenceDir "evidence.md"

if (Test-Path -LiteralPath $templatePath) {
    Copy-Item -LiteralPath $templatePath -Destination $evidencePath -Force
} else {
    Set-Content -LiteralPath $evidencePath -Value "# Steam Version Selection Evidence`n" -Encoding UTF8
}

$artifactHygienePath = Join-Path $evidenceDir "ARTIFACT_HYGIENE.txt"
$artifactHygiene = @"
Steam Version Selection Artifact Hygiene

Do not store or publish Steam credentials, guard codes, refresh tokens, shared preferences, private save contents, or unsanitized full logs in this evidence folder.

Raw logs and full launcher diagnostics are local-only unless they have been manually reviewed and redacted before sharing.

Before posting any artifact publicly, complete PUBLIC_EVIDENCE_REDACTION_REVIEW.txt and run:

scripts\review-public-evidence-redaction.ps1 -EvidenceDir "$evidenceDir"

Prefer public attachments from this folder in this order:

1. evidence.md after completing non-secret result fields.
2. PUBLIC_SHARE_MANIFEST.txt after reviewing it.
3. logs/logcat-steam-version-focused-redacted.txt when generated and manually reviewed.
4. diagnostics/logcat-redaction-summary.txt when generated.
5. screenshots that do not expose account names, credentials, guard codes, save contents, or device-identifying details.
6. branch marker files only after checking they do not expose identifying data.

The release-readiness contract is docs/steam-version-selection-release-readiness.md.
"@

Set-Content -LiteralPath $artifactHygienePath -Value $artifactHygiene -Encoding UTF8

$redactionReviewPath = Join-Path $evidenceDir "PUBLIC_EVIDENCE_REDACTION_REVIEW.txt"
$redactionReview = @"
Public Evidence Redaction Review

Set every field to true only after direct manual review of the public share candidate. Keep raw files local when any field is false.

Screenshots manually reviewed: false
Credential suggestions absent: false
Account identifiers redacted: false
Device notifications absent: false
Private save/profile contents absent: false
Steam credentials absent: false
Steam Guard codes absent: false
Refresh/session tokens absent: false
Local user paths redacted: false
Device identifiers redacted: false
Only sanitized diagnostics selected for public sharing: false

Reviewer:
UTC:
Notes:
"@

Set-Content -LiteralPath $redactionReviewPath -Value $redactionReview -Encoding UTF8

$publicShareManifestPath = Join-Path $evidenceDir "PUBLIC_SHARE_MANIFEST.txt"
$publicShareManifest = @"
Steam Version Selection Public Share Manifest

Preferred public artifacts after manual review:

- evidence.md
- ARTIFACT_HYGIENE.txt
- PUBLIC_EVIDENCE_REDACTION_REVIEW.txt
- logs/logcat-steam-version-focused-redacted.txt
- diagnostics/logcat-redaction-summary.txt
- diagnostics/steamkit-debug-log-setting.txt
- diagnostics/launcher-diagnostics-index.txt
- screenshots that do not expose account or device-identifying details
- branch-markers/steam-branch-marker-list.txt
- branch-markers/last_steam_branch_availability.txt

Local-only or manual-review artifacts:

- logs/logcat-full.txt
- logs/logcat-steam-version-focused.txt
- full launcher diagnostics reports
- startup-recovery diagnostics reports
- raw copied error logs
- private save files or save-content dumps
- any artifact containing credentials, guard codes, refresh tokens, shared preferences, account names, local user paths, or device identifiers

Before public posting, compare the evidence against docs/steam-version-selection-release-readiness.md and only mark a release-readiness gate as covered when the artifact directly proves that gate.

Run scripts\review-public-evidence-redaction.ps1 against this folder before posting. The script is a guardrail, not a substitute for manual review.
"@

Set-Content -LiteralPath $publicShareManifestPath -Value $publicShareManifest -Encoding UTF8

$readmePath = Join-Path $evidenceDir "README.md"
$readme = @"
# Steam Version Selection Evidence Folder

Created: $timestamp
Label: $Label

Use this folder for one validation run. Do not place Steam credentials or personally identifying account details in artifacts.

## Fill first

- ``evidence.md`` copied from ``docs/steam-version-selection-evidence-template.md``.
- Review ``docs/steam-version-selection-release-readiness.md`` and mark only gates this folder directly proves.
- Review ``ARTIFACT_HYGIENE.txt`` and ``PUBLIC_SHARE_MANIFEST.txt`` before sharing anything publicly.
- Complete ``PUBLIC_EVIDENCE_REDACTION_REVIEW.txt`` and run ``scripts\review-public-evidence-redaction.ps1 -EvidenceDir "<this folder>"`` before posting public artifacts.

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
- ``inventories/``: public/default and selected beta file inventories plus SHA-256 comparison summaries from ``scripts/capture-steam-beta-integrity-evidence.ps1``.
- Review beta integrity evidence with ``scripts/review-beta-integrity-summary.ps1`` after capture. Treat ``Evidence readiness: not ready for final classification`` as unresolved runtime evidence unless it is explicitly carried as a release blocker.

## Reference docs

- ``docs/steam-version-selection-architecture.md``
- ``docs/steam-version-selection-validation.md``
- ``docs/steam-version-selection-runbook.md``
- ``docs/steam-version-selection-release-readiness.md``
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
