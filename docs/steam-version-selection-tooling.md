# Steam Version Selection Tooling

This page lists the local helper scripts used by the Steam version-selection validation path.

These tools do not replace ARM64 runtime validation. They only make static guardrails and evidence capture repeatable.

The PowerShell helpers normalize local path separators so they can be run from Windows PowerShell or PowerShell Core on CI/Linux. Android `adb shell` paths intentionally remain Android-style paths.

## Static guardrail audit

Purpose:

- Checks that branch/version implementation guardrails, docs, release blockers, tester guidance, and CI coverage are still present.
- Does not prove Steam login, depot download, beta branch behavior, game launch, save compatibility, or Push safety.

Command:

```powershell
.\scripts\audit-steam-version-selection.ps1
```

Quiet mode:

```powershell
.\scripts\audit-steam-version-selection.ps1 -Quiet
```

## Managed/native selector guidance parity audit

Purpose:

- Checks that the managed launcher selector note and native Android selector note still share the same important safety phrases.
- Catches drift between `SteamGameBranch.SelectorHelpText` and `SteamBranchInfo.selectorHelpText`.
- Does not prove UI rendering, startup routing, or device behavior.

Command:

```powershell
.\scripts\audit-steam-branch-guidance-parity.ps1
```

Quiet mode:

```powershell
.\scripts\audit-steam-branch-guidance-parity.ps1 -Quiet
```

## Create an evidence folder

Purpose:

- Creates a timestamped validation folder under `artifacts/android/`.
- Copies the evidence template into `evidence.md`.
- Points the generated README at `docs/steam-version-selection-release-readiness.md` so each evidence run starts from the current release gate.
- Creates `ARTIFACT_HYGIENE.txt` with local-only/raw-log and public-sharing guidance.
- Creates `PUBLIC_SHARE_MANIFEST.txt` with preferred public artifacts and local-only/manual-review artifacts.
- Creates subfolders for logs, diagnostics, branch markers, screenshots, and backup evidence.

Command:

```powershell
.\scripts\new-steam-version-selection-evidence.ps1
```

With a label:

```powershell
.\scripts\new-steam-version-selection-evidence.ps1 -Label "public-baseline"
```

Output shape:

```text
artifacts/android/steam-version-selection-<timestamp>/
  evidence.md
  ARTIFACT_HYGIENE.txt
  PUBLIC_SHARE_MANIFEST.txt
  README.md
  logs/
  diagnostics/
  branch-markers/
  screenshots/
  backup-evidence/
```

## Capture non-secret device evidence

Purpose:

- Captures `adb devices -l`.
- Writes `ARTIFACT_HYGIENE.txt` so the bundle labels raw logs as local-only and points public reports to the redacted focused log.
- Writes `PUBLIC_SHARE_MANIFEST.txt` so testers can see which generated artifacts are the safer public-sharing defaults.
- Captures `sts2_steamkit_debug_logs` global setting state so evidence records whether SteamKit debug logging was disabled, or explicitly enabled for sanitized auth diagnostics.
- Captures focused logcat snapshots and a redacted focused logcat. Raw full logcat is omitted by default and only captured when `-IncludeRawLogcat` is passed for local diagnostics.
- Creates `logs/logcat-steam-version-focused-redacted.txt` as the safer default log excerpt for public GitHub issue sharing. The generated file includes a warning header because this is best-effort pattern-based redaction for common credentials, tokens, account/username fields, serial-like fields, email addresses, and local user paths, not a guarantee that every identifier is removed.
- Writes `diagnostics/logcat-redaction-summary.txt` with focused-line and changed-line counts so reviewers can see whether best-effort redaction changed the generated public-log artifact.
- Writes `diagnostics/launcher-diagnostics-index.txt` to list available launcher diagnostics reports without automatically copying full report contents.
- Captures coarse app game/cache directory listings through `run-as`.
- Captures app-private Steam branch markers, including the latest branch availability result from app info.
- Captures bounded non-public branch cache tree and cache-size snapshots.
- Captures non-secret local/cloud pre-Push backup filename listings and counts from external backup storage.
- Copies `steam_branch.txt` marker files from app storage when present.
- Copies `last_game_branch_switch.txt` branch-switch safety evidence when present.
- Copies `last_manual_cloud_pull.txt` Pull-after-switch safety evidence when present.
- Copies `last_manual_cloud_push.txt` successful Push evidence when present.
- Copies `last_manual_cloud_push_blocked.txt` blocked Push evidence when present.
- Copies `last_game_version_cache_cleanup.txt` cleanup evidence when present.
- Copies `last_game_version_redownload.txt` selected-version redownload evidence when present.
- Avoids shared preferences and credential-bearing files.
- Normalizes local evidence-folder paths across Windows and PowerShell Core.

Command:

```powershell
.\scripts\capture-steam-version-selection-evidence.ps1 `
  -EvidenceDir "artifacts\android\steam-version-selection-<timestamp>"
```

To include raw full logcat for local-only diagnostics:

```powershell
.\scripts\capture-steam-version-selection-evidence.ps1 `
  -EvidenceDir "artifacts\android\steam-version-selection-<timestamp>" `
  -IncludeRawLogcat
```

With explicit package and device serial:

```powershell
.\scripts\capture-steam-version-selection-evidence.ps1 `
  -EvidenceDir "artifacts\android\steam-version-selection-<timestamp>" `
  -PackageName "com.sts2launcher.overhaul.fork.dev" `
  -DeviceSerial "<adb-serial>"
```

Captured files:

```text
logs/adb-devices.txt
ARTIFACT_HYGIENE.txt
PUBLIC_SHARE_MANIFEST.txt
diagnostics/steamkit-debug-log-setting.txt
diagnostics/logcat-redaction-summary.txt
diagnostics/launcher-diagnostics-index.txt
logs/logcat-full.txt (placeholder by default; raw only with -IncludeRawLogcat)
logs/logcat-steam-version-focused.txt
logs/logcat-steam-version-focused-redacted.txt
diagnostics/app-game-cache-listing.txt
diagnostics/game-version-cache-tree.txt
diagnostics/game-version-cache-sizes.txt
backup-evidence/pre-push-backup-list.txt
backup-evidence/pre-push-backup-counts.txt
branch-markers/steam-branch-marker-list.txt
branch-markers/<copied-marker-files>
branch-markers/last_game_branch_switch.txt
branch-markers/last_manual_cloud_pull.txt
branch-markers/last_manual_cloud_push.txt
branch-markers/last_manual_cloud_push_blocked.txt
branch-markers/last_game_version_cache_cleanup.txt
branch-markers/last_game_version_redownload.txt
branch-markers/last_steam_branch_availability.txt
capture-summary.txt
```

## Capture beta branch integrity evidence

Purpose:

- Captures app-private `steam_branch.txt` markers for all installed branch caches.
- Names both public/default and selected branch marker paths in `beta-integrity-summary.txt` when present.
- Embeds bounded public/default and selected branch depot manifest rows in `beta-integrity-summary.txt`.
- Captures `last_steam_branch_availability.txt` and summarizes selected-branch visibility, Windows depot manifest count, visible-branch count, and whether the marker matches the investigated branch.
- Captures `last_game_version_redownload.txt` and summarizes whether the clean redownload marker matches the investigated branch and cleared the selected game/download-state directories.
- Captures best-effort focused logcat lines for selected branch routing, marker readiness, manifest provenance, fallback, and public-inherited evidence.
- Writes a public-sharing warning into `beta-integrity-summary.txt`; manually review the summary, focused logcat, branch markers, cache tree, and inventory paths before posting.
- Builds a public/default file inventory from `files/game`.
- Captures a bounded public/default cache tree for stale-cache and fallback comparison.
- Builds a selected beta file inventory from the selected branch cache.
- Captures a bounded selected-branch cache tree for stale-cache and fallback investigation.
- Records file size and SHA-256 for each installed file.
- Compares public/default files against the selected beta cache.
- Summarizes identical, different, public-only, selected-only, and art/bundle-like file differences.
- Writes `inventories/public-vs-<branch>-key-assets.tsv` for focused PCK, art, audio, data, font, and bundle hash comparison.
- Embeds bounded changed key-asset rows in `beta-integrity-summary.txt`.
- Writes a conservative `Classification:` line in `beta-integrity-summary.txt` using branch-availability evidence, clean-redownload proof, marker counters, and inventory differences.
- Writes `Evidence readiness:` and `Evidence missing/weak:` lines so the summary states whether the package is ready for branch-availability or manifest/cache/art classification.
- Gates manifest/cache/art classifications on `last_game_version_redownload.txt` proving the investigated branch was redownloaded and its selected game/download-state directories were cleared.
- Writes the classification input metrics into `beta-integrity-summary.txt` so the final cause label can be audited without reopening the full inventory files.
- Helps classify mixed beta/public behavior as Steam-served partial branch content, stale branch-cache files, launcher fallback, or possible runtime remote/config behavior.

Runtime checklist: `docs/steam-beta-integrity-runtime-checklist.md`

Review command:

```powershell
.\scripts\review-beta-integrity-summary.ps1 `
  -SummaryPath "artifacts\android\steam-beta-integrity-<timestamp>\beta-integrity-summary.txt"
```

Use `-FailOnNotReady` when CI or a release-gate checklist should fail if `Evidence readiness:` is not ready. Exit code `2` means the evidence package is not ready for final classification, not that the parser crashed. Exit code `3` means the evidence is otherwise ready but the public-sharing warning is missing. The review output also reports whether the beta-integrity public-sharing warning is present. The capture helper accepts `-ReviewSummary` and `-FailOnNotReady` to run the same review immediately after writing `beta-integrity-summary.txt`.

Command:

```powershell
.\scripts\capture-steam-beta-integrity-evidence.ps1 `
  -EvidenceDir "artifacts\android\steam-beta-integrity-<timestamp>" `
  -Branch "public-beta" `
  -ReviewSummary
```

With explicit package and device serial:

```powershell
.\scripts\capture-steam-beta-integrity-evidence.ps1 `
  -EvidenceDir "artifacts\android\steam-beta-integrity-<timestamp>" `
  -PackageName "com.sts2launcher.overhaul.fork.dev" `
  -Branch "public-beta" `
  -DeviceSerial "<adb-serial>"
```

Captured files:

```text
branch-markers/steam-branch-marker-list.txt
branch-markers/last_steam_branch_availability.txt
branch-markers/<copied-marker-files>
logs/beta-integrity-logcat-focused.txt
inventories/public-files.tsv
inventories/public-cache-tree.txt
inventories/<branch>-files.tsv
inventories/<branch>-cache-tree.txt
inventories/public-vs-<branch>-comparison.txt
inventories/public-vs-<branch>-key-assets.tsv
beta-integrity-summary.txt
```

Interpretation:

- `manifestSource=selected` means the selected branch supplied that depot manifest.
- `manifestSource=public-inherited` means Steam exposed no explicit selected-branch manifest for that depot and the launcher intentionally used the public manifest as inherited branch content.
- Public-identical and branch-specific depots in the same marker are evidence of a partial Steam branch.
- File inventory differences show whether the installed beta cache actually differs from public for PCK, art, audio, JSON, font, or other bundle-like files.
- Treat an `inconclusive` summary classification as a requirement to clean-redownload, capture stronger marker evidence, or inspect runtime logs before claiming a cause. Strong manifest/cache/art classifications require clean-redownload proof for the investigated branch.
- Treat `Evidence readiness: not ready for final classification` as a release blocker until the missing/weak evidence line is resolved or explicitly accepted as a blocker note.
- Treat the public-sharing warning as part of the evidence contract: filtered logs and marker paths are still not automatically safe to publish.
- If a clean-redownloaded beta slot still has public-inherited depot markers and public-identical art hashes, the likely cause is Steam-served partial branch content.
- If marker evidence says selected manifests differ but files remain stale or public-only, investigate cache cleanup and download replacement.

## Safe validation sequence

Use the tools in this order:

1. Run the static audit only when you want a static guardrail check.
2. Create an evidence folder before touching device state.
3. Review `docs/steam-version-selection-release-readiness.md` and decide which gates this run can prove.
4. Follow `docs/steam-version-selection-runbook.md`.
5. Capture device evidence after each meaningful phase.
6. Run beta-integrity capture after a clean selected-branch redownload if public-beta appears mixed or art assets look wrong.
7. Fill `evidence.md` as results are observed.
8. Do not perform manual Push after a branch switch until Pull, local-save existence, backup permission, local pre-Push backup, and cloud pre-Push backup evidence are captured.

## Artifact hygiene

- Do not store Steam credentials.
- Do not store refresh tokens.
- Do not copy shared preferences.
- Keep SteamKit debug logging disabled by default; use `adb shell settings put global sts2_steamkit_debug_logs 1` only for focused sanitized auth diagnostics and reset it to `0` before routine evidence capture.
- Prefer scrubbed summaries when sharing evidence publicly.
- Prefer `logs/logcat-steam-version-focused-redacted.txt` over raw logcat when attaching evidence publicly, but review it manually before posting.
- Treat full launcher diagnostics and startup-recovery diagnostics reports as manual attachments; review/redact them before sharing because they can contain account names, local paths, device details, and log excerpts. These reports include a public-sharing warning, but that warning is not a substitute for manual review.
- Treat copied raw error logs the same way: they include a warning, but must be reviewed/redacted before public posting.
- The launcher support UI labels raw-log copy as review-before-sharing.
- The startup recovery UI labels raw-log copy as review-before-sharing because raw logs can contain identifying data.
- Keep raw logs local if they contain account-identifying paths, usernames, or device identifiers.

## Autofill versus local credential handoff

Android/Samsung/password-manager Autofill is the user-facing login convenience path. It uses the native `USE ANDROID AUTOFILL` dialog and must not create a launcher-owned Autofill password store.

Local credential handoff files are developer-only automation aids for repeatable test runs. Do not describe them as Autofill, do not include them in evidence bundles, and do not copy them into public artifacts.
