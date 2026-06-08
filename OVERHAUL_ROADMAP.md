# OVERHAUL_ROADMAP.md

This roadmap tracks the overhaul phases and the current Android release-hardening path.

## Current release posture

The app has a working ARM64 Android baseline. It is not yet release-candidate complete because confirmed Push-to-Cloud upload, upgrade behavior, locked-screen interruption, and repeated release artifact validation still need evidence.

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

## Phase 5 - Device lifecycle and install-path validation

- [x] Add startup freshness and assembly cache diagnostics for installed runtime/schema/cache evidence.
- [ ] Validate upgrade install behavior from the current public release baseline.
- [ ] Validate locked-screen interruption and return-to-app after manual unlock.
- [ ] Repeat stale assembly cache/freshness checks across reinstall and upgrade scenarios.

## Phase 6 - Public release readiness

- [ ] Publish release notes that clearly say the app works on the validated ARM64 path but is still being polished/hardened.
- [ ] Keep confirmed Push overwrite risk explicit until validated.
- [ ] Keep x86_64 emulator limitations explicit.
- [ ] Keep APK artifacts, checksums, validation logs, and summaries clean enough for external testers.