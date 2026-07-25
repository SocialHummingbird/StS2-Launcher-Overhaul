# OVERHAUL_STATUS.md

This file tracks the active overhaul status for the GitHub branch.

## Current Focus

The ARM64 Android launcher path now works. Current work is polish and hardening around that working baseline, not proving the launcher from zero.

Validated baseline:

- Fresh APK/runtime install reaches the launcher on ARM64 hardware.
- Steam login and depot download complete.
- Pull from Cloud downloads Steam Cloud files into Android local app storage.
- `v0.2.398-launcher-ui-redesign` publishes five stable Home/Saves/Versions/Mods/Help destinations with phone bottom navigation and wide/foldable top navigation. Its 20-viewport deterministic preview, accessibility/bounds/target checks, event contract, 819-check static audit, exact ARM64 update install, and release hygiene passed; final unlocked physical portrait/landscape capture remains pending after the device disconnected.
- `v0.2.399-powervr-renderer-mod-runtime` publishes the all-PowerVR engine workaround, Auto/Vulkan/OpenGL/Safe Start policy, lifecycle/process-exit diagnostics, Android-safe mod identity hashing, public runtime-pack activation, and per-mod evidence. Connected Adreno validation reached real `NMainMenu` under Auto/Vulkan and explicit OpenGL, produced post-startup heartbeats through 120 seconds, and exercised Quick Restart in combat with zero selected-mod runtime failures.
- The issue #34 reporter subsequently confirmed that the PowerVR Auto route loads the game with active touch controls. The renderer/startup investigation is resolved; the retained policy still routes PowerVR to OpenGL.
- The game launches and reads the pulled profile in-game.
- Native modded-save Pull support prefers cloud modded namespaces, seeds missing namespaces from the fresh vanilla download, backs up replaced files, and records provenance. Steam Cloud Push remains separate and explicit.
- `v0.2.412-bootstrap-cloud-hardening` moved assembly preparation before Godot startup, corrected the `GodotActivity` superclass lifecycle, made cache replacement transactional, and added observable/cancellable Pull plus structured Upload eligibility without weakening overwrite protections.
- `v0.2.416-startup-recovery-ime` corrects runtime-pack assembly identity validation, clears pending launch state during native recovery, and suppresses unintended launcher keyboard requests.
- Startup freshness and assembly cache diagnostics prove the current installed runtime is being used.
- Current GitHub release: `v0.2.416-startup-recovery-ime`, APK `StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk`, version code `416001`, SHA-256 `fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b`, local test signing channel.

Active blockers:

- Xiaomi issue #36 requires reporter confirmation that `v0.2.416` accepts the authentic patched runtime pack, reaches the game, and recovers to the launcher without `SuperNotCalledException` if preparation still fails.
- Odin issue #35 requires reporter confirmation that Pull progress and completion are clear and that **Review Upload** explains every blocking safeguard. A real Steam Cloud Push remains explicitly deferred.
- Exact-build unlocked physical validation of all five launcher destinations, portrait/landscape rotation, cover/inner displays, and real game handoff remains incomplete.
- Steam beta/version selection is implemented for validation but not release-signed. The launcher now exposes discovery-led public/non-public selector guidance, labels refreshed branch options with concise metadata badges, blocks known unavailable selected branches before game-version download/update attempts, records selected-version notes in diagnostics/logs/branch-switch/Pull/Push evidence, mirrors guidance in native routing/fallback diagnostics, blocks native selected-version launch when branch provenance is missing or mismatched, and guards the static contract through CI. ARM64 evidence still needs to prove public/default regression safety, account-visible non-public branch download/startup routing, branch marker provenance, inaccessible/private/password branch handling, cache cleanup, save compatibility, Pull-before-Push/current-backup safety, pre-Push backup evidence, and successful selected-version Push marker evidence. The current signoff contract is tracked in `docs/steam-version-selection-release-readiness.md`.
- Confirmed Push to Cloud on the newest public APK still needs explicit overwrite-risk smoke because it can overwrite real Steam Cloud state.
- Exact-build native modded-save Pull/profile visibility still needs broader device validation before it can be called broadly proven.
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
| P9 | Launcher UX | Five-destination responsive shell and deterministic viewport/event validation | UX / Runtime validation | Published; physical matrix pending |

## Open Follow-up Tasks

- Complete confirmed Push to Cloud validation on the newest public APK with controlled overwrite-risk evidence, or keep it explicitly deferred.
- Complete the Steam version selection validation runbook: static version-selection/parity audits, build gate, public/default baseline, account-visible non-public branch path, selector guidance visual check, native selected-branch diagnostics, branch marker/provenance checks, inactive cache cleanup, missing/private/password branch behavior, save compatibility review, Pull-before-Push gate, local-save evidence gate, backup permission gate, and manual Push smoke only after backup evidence exists and `last_manual_cloud_push.txt` records the selected version.
- Run release-readiness validation across fresh install, upgrade install, locked-screen interruption, stale cache, Pull, confirmed Push, game launch, and release artifact paths.
- Keep launcher recovery and sync status UX clear enough that successful startup and local-save runtime behavior are not presented as failures.
- Reduce low-value diagnostics while preserving startup freshness, assembly cache, cloud-save, and release-evidence logs.
- Maintain artifact hygiene for APKs, checksums, logs, summaries, and validation manifests.
- Complete the remaining exact-build five-destination portrait/landscape and foldable display checks on `v0.2.416` without Steam Cloud Push.
- Collect reporter confirmation for Xiaomi issue #36 and Odin cloud issue #35 before closing either issue.

## Notes

- ARM64 hardware is the proof target for Steam login, download, cloud sync, and game launch.
- Steam version selection evidence is tracked through [docs/steam-version-selection-validation.md](docs/steam-version-selection-validation.md), [docs/steam-version-selection-runbook.md](docs/steam-version-selection-runbook.md), and [docs/steam-version-selection-evidence-template.md](docs/steam-version-selection-evidence-template.md).
- Android `x86_64` emulator coverage remains install/routing/native-fallback diagnostics unless explicitly forcing the crash-prone Godot path for investigation.
- No single issue is authoritative. Use this file, [docs/current-android-status.md](docs/current-android-status.md), release notes, and validation logs as the current source of truth.
