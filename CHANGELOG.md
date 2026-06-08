# Changelog

All notable changes for the overhauled repository are recorded here.

## [Unreleased]

### Added
- Added an overhaul migration check-list + roadmap to formalize the independent rewrite scope.
- Added repository labels for severity/priority/category tracking:
  - severity: critical/high/medium/low
  - priority: p0/p1/p2/p3
  - category: reliability/overhaul
- Added project governance artifacts:
  - `OVERHAUL_STATUS.md`
  - `docs/device-log-checklist.md`

### Changed
- Updated GitHub-facing project text for the current APK state: `v0.2.175-refactor-apk`, ARM64-only release assets, signed release expectations, emulator limits, and the current working-but-hardening Android validation state.
- Updated GitHub-facing project text to reflect that the ARM64 local Android path now works through fresh install/runtime validation, Steam download, Pull from Cloud, Android local save handoff, and game launch, while Push/release hardening remains active.
- Reframed overhaul status from the old phase-closure language to the current refactor and validation stabilization work.
- Improved launcher timeout control for manual cloud sync operations to avoid UI hangs.
- Clarified launcher cloud-sync wording/status and startup recovery wording for the current working-but-hardening phase.
- Added per-path and per-operation timeouts for cloud sync coordinator reads/writes.
- Hardened lifecycle cloud flush paths to avoid unbounded waits.
- Hardened dependency reflection in `ModLoaderPatches` to avoid startup breakage when mod metadata shape changes.

### Fixed
- Fixed stale documentation that presented older universal/x86 release assets as the current phone testing path.
- Fixed `Task.WhenAny`-based dead-ends for cloud sync operations that could block launcher interaction.
- Added time-bound guardrails around cache read/write/update operations in cloud sync paths.

### Known Issues
- Push to Cloud still requires explicit end-to-end validation before it should be treated as release-ready, because confirmed Push can overwrite Steam Cloud state.
- Release-readiness validation still needs complete fresh-install, upgrade-install, Pull, Push, game-launch, locked-screen interruption, and stale assembly cache coverage.
- Android `x86_64` emulator runs are fallback/diagnostic coverage only unless the unsafe Godot path is explicitly forced.

## [Initial Overhaul Baseline]
- Forked repository and established independent project documentation and workflow for a sustained rewrite.
