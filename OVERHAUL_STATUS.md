# OVERHAUL_STATUS.md

This file tracks what we are actively working on in the overhaul branch.

## Current Focus
- Continue the polish and hardening pass after proving the ARM64 Android path works locally.
- Keep GitHub release assets verifiable, signed, and update-compatible.
- Preserve the validated Pull baseline: fresh runtime install, Steam game download, Pull from Cloud, Android local save handoff, and in-game profile load.
- Finish safe Push to Cloud validation, including confirmation/no-upload behavior and explicit handling of Steam Cloud overwrite risk.
- Use ARM64 hardware as the proof target for launcher login, game download, cloud sync, and game launch.
- Treat Android `x86_64` emulator coverage as install/routing/native-fallback diagnostics unless explicitly forcing the crash-prone Godot path.

## High-Impact Reliability Backlog

### Completed

| Priority | Area | Issue | Category | Target |
| --- | --- | --- | --- | --- |
| P0 | Startup crash paths | Locale parsing + patch compatibility | Reliability | Completed |
| P1 | Cloud sync path | Timeout handling for slow or stalled reads/writes | Reliability | Completed |
| P2 | Downloader | Duplicate download/write race conditions under resume/retry | Reliability | Completed |
| P3 | Multiplayer | LAN beacon persistence and discovery stability | Reliability | Completed |
| P7 | Closure | CI artifact handling + phase transition hygiene | Reliability / Governance | Completed |

## Open Follow-up Tasks
- Complete confirmed Push to Cloud validation or defer it with clear overwrite-risk rationale and required evidence.
- Run release-readiness validation across fresh install, upgrade install, Pull, Push, game launch, locked-screen interruption, and stale assembly cache behavior.
- Keep launcher recovery and sync status UX clear enough that successful startup and local-save runtime behavior are not presented as failures.
- Keep normal diagnostics quiet while preserving opt-in verbose capture for save/cloud investigation.
- Keep release verification aligned with the latest APK asset in [docs/android-release-validation.md](docs/android-release-validation.md) and [docs/runbook-android-validation.md](docs/runbook-android-validation.md).
- Continue targeted refactors in launcher diagnostics, startup recovery, cloud sync, Steam helpers, and downloader code after validation-sensitive risks are understood.

## Active Status Issue
- No single active status issue is authoritative. Use release issues and validation logs as the current source of truth.

## Rollback Strategy
- Keep each PR scoped to one logical change so labels and patches can be reverted independently.
- For platform/game-API fixes, preserve previous behavior behind compatibility guards where practical.
