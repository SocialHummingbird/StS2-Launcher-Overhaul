# OVERHAUL_STATUS.md

This file tracks the active overhaul status for the GitHub branch.

## Current Focus

The ARM64 Android launcher path now works. Current work is polish and hardening around that working baseline, not proving the launcher from zero.

Validated baseline:

- Fresh APK/runtime install reaches the launcher on ARM64 hardware.
- Steam login and depot download complete.
- Pull from Cloud downloads Steam Cloud files into Android local app storage.
- The latest public APK has visual validation for the responsive launcher login, active download-progress, ready, diagnostics-drawer, and Push confirmation/cancel states.
- The game launches and reads the pulled profile in-game.
- Startup freshness and assembly cache diagnostics prove the current installed runtime is being used.
- Latest build-only prerelease: `v0.2.282-local-evidence-marker-refactor` packages runtime-cache, save-origin, and manual cloud-sync evidence marker prefix extraction and passed static/build/APK verification. It is not new device runtime signoff.

Active blockers:

- Steam beta/version selection is implemented for validation but not release-signed. The launcher now exposes discovery-led public/non-public selector guidance, labels refreshed branch options with concise metadata badges, blocks known unavailable selected branches before game-version download/update attempts, records selected-version notes in diagnostics/logs/branch-switch/Pull/Push evidence, mirrors guidance in native routing/fallback diagnostics, blocks native selected-version launch when branch provenance is missing or mismatched, and guards the static contract through CI. ARM64 evidence still needs to prove public/default regression safety, account-visible non-public branch download/startup routing, branch marker provenance, inaccessible/private/password branch handling, cache cleanup, save compatibility, Pull-before-Push/current-backup safety, pre-Push backup evidence, and successful selected-version Push marker evidence. The current signoff contract is tracked in `docs/steam-version-selection-release-readiness.md`.
- Confirmed Push to Cloud on the newest public APK still needs explicit overwrite-risk smoke because it can overwrite real Steam Cloud state.
- Upgrade install behavior needs repeated release-readiness evidence on the current signed line.
- Locked-screen interruption has manual unlock-return evidence, but should remain part of recurring release smoke.
- Diagnostics should be quieter and focused on actionable freshness/cache/cloud-save facts.
- Release assets need continued signer/package/version/checksum hygiene before public release-candidate claims.

Canonical status: [docs/current-android-status.md](docs/current-android-status.md)

## High-Impact Reliability Backlog

### Completed

| Priority | Area | Issue | Category | Target |
| --- | --- | --- | --- | --- |
| P0 | Startup crash paths | Locale parsing and patch compatibility | Reliability | Completed |
| P1 | Cloud sync path | Timeout handling for slow or stalled reads/writes | Reliability | Completed |
| P2 | Downloader | Duplicate download/write race conditions under resume/retry | Reliability | Completed |
| P3 | Multiplayer | LAN beacon persistence and discovery stability | Reliability | Completed |
| P7 | Closure | CI artifact handling and phase transition hygiene | Reliability / Governance | Completed |
| P8 | Android working path | ARM64 fresh install, Steam download, Pull from Cloud, local save handoff, and game launch | Runtime validation | Completed baseline |
| P9 | Launcher UX | Responsive shell, collapsed diagnostics, reachable launch buttons, active download-progress validation | UX / Runtime validation | Completed baseline |

## Open Follow-up Tasks

- Complete confirmed Push to Cloud validation on the newest public APK with controlled overwrite-risk evidence, or keep it explicitly deferred.
- Complete the Steam version selection validation runbook: static version-selection/parity audits, build gate, public/default baseline, account-visible non-public branch path, selector guidance visual check, native selected-branch diagnostics, branch marker/provenance checks, inactive cache cleanup, missing/private/password branch behavior, save compatibility review, Pull-before-Push gate, local-save evidence gate, backup permission gate, and manual Push smoke only after backup evidence exists and `last_manual_cloud_push.txt` records the selected version.
- Run release-readiness validation across fresh install, upgrade install, locked-screen interruption, stale cache, Pull, confirmed Push, game launch, and release artifact paths.
- Keep launcher recovery and sync status UX clear enough that successful startup and local-save runtime behavior are not presented as failures.
- Reduce low-value diagnostics while preserving startup freshness, assembly cache, cloud-save, and release-evidence logs.
- Maintain artifact hygiene for APKs, checksums, logs, summaries, and validation manifests.
- Device-test the latest compact-label refactor APK before treating it as runtime or UX evidence beyond build/static gates.

## Notes

- ARM64 hardware is the proof target for Steam login, download, cloud sync, and game launch.
- Steam version selection evidence is tracked through [docs/steam-version-selection-validation.md](docs/steam-version-selection-validation.md), [docs/steam-version-selection-runbook.md](docs/steam-version-selection-runbook.md), and [docs/steam-version-selection-evidence-template.md](docs/steam-version-selection-evidence-template.md).
- Android `x86_64` emulator coverage remains install/routing/native-fallback diagnostics unless explicitly forcing the crash-prone Godot path for investigation.
- No single issue is authoritative. Use this file, [docs/current-android-status.md](docs/current-android-status.md), release notes, and validation logs as the current source of truth.
