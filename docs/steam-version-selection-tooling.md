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
- Captures full and focused logcat snapshots.
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
logs/logcat-full.txt
logs/logcat-steam-version-focused.txt
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

## Safe validation sequence

Use the tools in this order:

1. Run the static audit only when you want a static guardrail check.
2. Create an evidence folder before touching device state.
3. Follow `docs/steam-version-selection-runbook.md`.
4. Capture device evidence after each meaningful phase.
5. Fill `evidence.md` as results are observed.
6. Do not perform manual Push after a branch switch until Pull, local-save existence, backup permission, local pre-Push backup, and cloud pre-Push backup evidence are captured.

## Artifact hygiene

- Do not store Steam credentials.
- Do not store refresh tokens.
- Do not copy shared preferences.
- Prefer scrubbed summaries when sharing evidence publicly.
- Keep raw logs local if they contain account-identifying paths, usernames, or device identifiers.

## Autofill versus local credential handoff

Android/Samsung/password-manager Autofill is the user-facing login convenience path. It uses the native `USE ANDROID AUTOFILL` dialog and must not create a launcher-owned Autofill password store.

Local credential handoff files are developer-only automation aids for repeatable test runs. Do not describe them as Autofill, do not include them in evidence bundles, and do not copy them into public artifacts.
