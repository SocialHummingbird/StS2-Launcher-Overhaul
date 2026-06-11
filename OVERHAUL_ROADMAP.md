# OVERHAUL_ROADMAP.md

This roadmap tracks the overhaul phases and the current Android release-hardening path.

## Current release posture

The app has a working ARM64 Android baseline. It is not yet release-candidate complete because confirmed Push-to-Cloud upload, upgrade behavior, locked-screen interruption, Steam version-selection hardening, and repeated release artifact validation still need evidence.

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
- [x] Add safe branch-switch warnings and local-backup posture before switching versions.
- [x] Gate manual Push after branch switches when backup storage permission is unavailable.
- [x] Document validation checklist, runbook, user/tester guide, issue template, evidence template, and static audit helper.
- [ ] Validate public/default regression on ARM64 hardware after version-selection changes.
- [ ] Validate `beta` download, marker provenance, and selected-PCK startup routing on ARM64 hardware.
- [ ] Validate missing/private/password-protected beta branch behavior or explicitly keep unsupported UI/docs wording.
- [ ] Validate save compatibility across public/beta branch switches, including Pull-after-switch, local-save evidence, and backup safety, or explicitly document incompatibility risk.
- [ ] Validate selected-version redownload and inactive-cache cleanup on device.
- [ ] Validate Pull-after-switch, local-save evidence, pre-Push backup evidence, `last_manual_cloud_push.txt`, and aggregate successful post-switch Push evidence after a branch switch before accepting any manual Push mutation.

## Phase 6 - Device lifecycle and install-path validation

- [x] Add startup freshness and assembly cache diagnostics for installed runtime/schema/cache evidence.
- [ ] Validate upgrade install behavior from the current public release baseline.
- [ ] Validate locked-screen interruption and return-to-app after manual unlock.
- [ ] Repeat stale assembly cache/freshness checks across reinstall and upgrade scenarios.

## Phase 7 - Public release readiness

- [ ] Publish release notes that clearly say the app works on the validated ARM64 path but is still being polished/hardened.
- [ ] Keep confirmed Push overwrite risk explicit until validated.
- [ ] Keep Steam beta/version selection release blockers explicit until ARM64 evidence exists.
- [ ] Keep x86_64 emulator limitations explicit.
- [ ] Keep APK artifacts, checksums, validation logs, and summaries clean enough for external testers.
