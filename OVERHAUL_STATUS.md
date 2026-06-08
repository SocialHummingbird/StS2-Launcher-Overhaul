# OVERHAUL_STATUS.md

This file tracks the active overhaul status for the GitHub branch.

## Current Focus

The ARM64 Android launcher path now works locally. Current work is polish and hardening around that working baseline, not proving the launcher from zero.

Validated baseline:

- Fresh APK/runtime install reaches the launcher on ARM64 hardware.
- Steam login and depot download complete.
- Pull from Cloud downloads Steam Cloud files into Android local app storage.
- The game launches and reads the pulled profile in-game.
- Startup freshness and assembly cache diagnostics prove the current installed runtime is being used.

Active blockers:

- Confirmed Push to Cloud upload is deliberately unproven because it can overwrite real Steam Cloud state.
- Upgrade install behavior still needs release-readiness evidence.
- Locked-screen interruption needs full manual unlock-return evidence.
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

## Open Follow-up Tasks

- Complete confirmed Push to Cloud validation with controlled overwrite-risk evidence, or keep it explicitly deferred.
- Run release-readiness validation across fresh install, upgrade install, locked-screen interruption, stale cache, and release artifact paths.
- Keep launcher recovery and sync status UX clear enough that successful startup and local-save runtime behavior are not presented as failures.
- Reduce low-value diagnostics while preserving startup freshness, assembly cache, cloud-save, and release-evidence logs.
- Maintain artifact hygiene for APKs, checksums, logs, summaries, and validation manifests.

## Notes

- ARM64 hardware is the proof target for Steam login, download, cloud sync, and game launch.
- Android `x86_64` emulator coverage remains install/routing/native-fallback diagnostics unless explicitly forcing the crash-prone Godot path for investigation.
- No single issue is authoritative. Use this file, [docs/current-android-status.md](docs/current-android-status.md), release notes, and validation logs as the current source of truth.