# OVERHAUL_ROADMAP.md

This roadmap tracks the overhaul phases and the current Android release-hardening path.

## Current release posture

The app has a working ARM64 Android baseline and a published five-destination launcher redesign. The unreleased source now includes the targeted Pixel/PowerVR engine and renderer-policy fix, but it is not yet release-candidate complete because reporter-class compatibility confirmation, exact-build physical UI/game-handoff validation, confirmed Push-to-Cloud upload, production-signing upgrade behavior, Steam version-selection hardening, and repeated release-readiness evidence remain open.

Canonical status: [docs/current-android-status.md](docs/current-android-status.md)

## Phase 1 - Reliability hardening

- [x] Stabilize background/cloud async flow with timeouts and structured cancellation.
- [x] Harden locale parsing and startup crash paths.
- [x] Improve reflection patch hardening for version drift.
- [x] Keep downloader resume/retry behavior from racing duplicate writes.

## Phase 2 - Android build and release hygiene

- [x] Keep GitHub release APKs structurally verifiable.
- [x] Require stable signing inputs before publishing update-compatible releases.
- [x] Verify package name, signer, versionCode, native libraries, and release checksums.
- [ ] Re-run release-readiness validation after the current Android hardening changes land in a published APK.

## Phase 3 - Launcher and runtime UX

- [x] Present the launcher reliably on fresh ARM64 installs.
- [x] Replace the all-in-one scroll surface with Home, Saves, Versions, Mods, and Help destinations plus phone/wide navigation.
- [x] Add deterministic phone, landscape, foldable, and desktop preview/accessibility/event-contract validation.
- [ ] Complete exact `v0.2.398` unlocked physical destination, rotation, cover/inner display, and real game-handoff validation.
- [x] Preserve Android local save behavior even when cloud sync is disabled.
- [x] Improve cloud sync wording from ambiguous auto-sync language to explicit Game Cloud Sync behavior.
- [ ] Continue polishing recovery/status text so successful startup is not presented as a failure.
- [ ] Reduce noisy diagnostics while preserving actionable startup/cache/cloud evidence.

## Phase 4 - Steam and cloud-save validation

- [x] Validate Steam login and ownership-gated depot download on ARM64 hardware.
- [x] Validate Pull from Cloud through Steam enumeration, download, Android local save write, and in-game profile load.
- [x] Validate Push confirmation and cancel/no-confirm no-upload behavior.
- [ ] Validate confirmed Push upload with controlled Steam Cloud overwrite evidence.
- [ ] Validate Push/Pull round-trip after a controlled local save mutation.

## Phase 5 - Steam version selection and branch cache hardening

- [x] Persist selected Steam branch in launcher preferences.
- [x] Add default/public versus `beta` selector for validation.
- [x] Make manifest resolution, update checks, download state, and game directories branch-aware.
- [x] Keep non-public branch installs in side-by-side `game_versions/<branch>/` caches.
- [x] Require branch marker/provenance metadata before treating non-public caches as ready.
- [x] Add selected-version diagnostics, cached-version inventory, and native startup/fallback marker reporting.
- [x] Add wrapped selector guidance, selected-version notes in managed/native diagnostics, branch-switch marker evidence, and managed/native guidance parity guardrails.
- [x] Replace normal-user branch text entry with a dropdown-first Steam game version selector.
- [x] Add `Refresh Game Versions` metadata refresh from Steam app-info without downloading or deleting game files.
- [x] Surface selected-branch availability/password/build metadata in helper text and diagnostics.
- [x] Harden Login credential providers through the integrated native Steam credential panel, real Android username/password fields, and password-manager hints without app-owned password storage.
- [x] Add Android one-shot native Autofill login dialog using password-manager hints without app-owned Autofill password storage.
- [x] Keep SteamKit debug logs disabled by default with opt-in sanitized auth diagnostics via `sts2_steamkit_debug_logs=1`.
- [x] Add safe branch-switch warnings and local-backup posture before switching versions.
- [x] Gate manual Push after branch switches when backup storage permission is unavailable.
- [x] Document validation checklist, runbook, user/tester guide, issue template, evidence template, and static audit helper.
- [ ] Validate public/default regression on ARM64 hardware after version-selection changes.
- [ ] Validate `beta` download, marker provenance, and selected-PCK startup routing on ARM64 hardware.
- [ ] Validate missing/private/password-protected beta branch behavior or explicitly keep unsupported UI/docs wording.
- [ ] Validate `Refresh Game Versions`, dropdown metadata labels, and Android/Samsung/password-manager suggestion behavior in the native credential panel on ARM64 hardware.
- [ ] Validate save compatibility across public/non-public branch switches, including Pull-after-switch, local-save evidence, and backup safety, or explicitly document incompatibility risk.
- [ ] Validate selected-version redownload and inactive-cache cleanup on device.
- [ ] Validate Pull-after-switch, local-save evidence, pre-Push backup evidence, `last_manual_cloud_push.txt`, and aggregate successful post-switch Push evidence after a branch switch before accepting any manual Push mutation.

## Phase 6 - Device lifecycle and install-path validation

- [x] Add startup freshness and assembly cache diagnostics for installed runtime/schema/cache evidence.
- [x] Install exact `v0.2.398` over existing local-package app data and verify version code `398031`.
- [ ] Validate upgrade install behavior from the current public release baseline.
- [ ] Validate locked-screen interruption and return-to-app after manual unlock.
- [ ] Repeat stale assembly cache/freshness checks across reinstall and upgrade scenarios.

## Phase 7 - Public release readiness

- [x] Publish `v0.2.398` release notes that clearly say the app works on the validated ARM64 path but remains tester software.
- [x] Backport the Godot 4.5.2 all-PowerVR transform-feedback shader-cache workaround and repair Auto/Vulkan/OpenGL plus Safe Start behavior in source.
- [x] Capture renderer-attempt and Android historical process-exit evidence for post-restart diagnostics.
- [x] Build and inspect an ARM64 local APK containing the patched native marker and renderer/process-exit DEX evidence.
- [ ] Retest issue #34 on reporter-class Pixel/PowerVR hardware before publishing or closing it.
- [ ] Keep confirmed Push overwrite risk explicit until validated.
- [ ] Keep Steam beta/version selection release blockers explicit until ARM64 evidence exists.
- [ ] Keep x86_64 emulator limitations explicit.
- [x] Keep the current APK, checksum, metadata, release body, and generated GitHub release inventory mutually consistent.
