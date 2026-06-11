# Changelog

## 2026-06-11 - Steam version selection hardening docs

- Documented the Steam game version selection hardening path: default/public versus `beta` selection, branch-aware manifest resolution, side-by-side non-public caches, selected-version diagnostics, branch marker provenance, and safe branch-switch/Push guardrails.
- Added `docs/steam-version-selection-runbook.md` as the ordered validation path for build gate, public/default baseline, beta cache/startup routing, marker failure recovery, cleanup, Pull-before-Push safety, pre-Push backup evidence, and release-readiness signoff.
- Updated GitHub-facing status text to clarify that Steam beta/version selection is implemented for validation but is not release-signed until ARM64 evidence proves beta/password behavior, inaccessible/private branch handling, save compatibility, and Push safety.
- Added selector-level helper text in the launcher UI explaining the fixed public/beta toggle, unsupported beta password/private branch behavior, and unproven save compatibility.
- Added selected-version guidance to branch-switch confirmation, launcher logs, launcher diagnostics, branch-switch marker evidence, native Android pre-routing logs, native startup logs, and native fallback diagnostics.
- Added managed/native selector-guidance parity checks plus a Steam version-selection static audit workflow so public/beta safety wording, marker/provenance requirements, and release blockers are guarded in CI.

## 2026-06-09 - Responsive launcher UI release

- Published and structurally verified `v0.2.185-responsive-ui`, an ARM64 public APK with the redesigned short-edge-aware launcher shell.
- Replaced the fixed two-column launcher screen with a responsive arcane-terminal layout that keeps `START GAME` and `SAFE LAUNCH` reachable on short/wide Samsung-style landscape screens.
- Moved the verbose console into a collapsible diagnostics drawer so login, download progress, ready actions, cloud controls, support actions, and diagnostics no longer compete for horizontal space.
- Validated the latest public APK on ARM64 hardware across fresh login, active download progress, ready-state actions, diagnostics drawer, and Push-to-Cloud confirmation/cancel.
- Updated GitHub-facing release/status/Reddit prep docs and GitHub issue responses for the latest responsive UI and remaining hardening boundaries.

## 2026-06-09 - Public release and Reddit-readiness polish

- Published and structurally verified `v0.2.185-responsive-ui`, an ARM64 public APK that includes managed SHA-1 Push hardening, the new launcher icon, and the responsive launcher shell.
- Updated GitHub-facing release instructions, safe public trial guidance, ARM64 caveats, Push overwrite warnings, and support boundaries for public testers.
- Added a Reddit announcement prep note with posting constraints, known-risk wording, and a draft announcement.
## 2026-06-09 - Android cloud-save Push/Pull hardening

- Fixed the post-Push Android process death by replacing the cloud upload SHA-1 file-hash path with managed SHA-1 instead of Android native crypto.
- Changed manual Push to upload the collected Steam Cloud batch directly before reporting completion, instead of queuing work to the background writer and waiting on a flush.
- Validated local ARM64 Push completion with `105` files uploaded/flushed and no crash markers.
- Revalidated manual Push on the current `0.2.0-codexcloudfix-clean3` ARM64 local build through the launcher UI; Push completed, Steam later idled out normally, and the app process stayed alive.
- Validated Pull after Push with `105` cloud files downloaded/written and `57` absent-in-cloud paths reported without app crash.
- Reduced fallback save discovery noise by skipping app runtime/cache directories during manual cloud sync enumeration.
- Tightened manual Push/Pull launcher status text so successful operations state which side now reflects the other, and Push warns testers to Pull first and verify Android local saves exist before overwriting Steam Cloud.
- Added `docs/android-cloud-save-validation-20260609.md` as the current cloud-save evidence ledger.

## 2026-06-08 - Device validation evidence refresh

- Updated release-facing docs and helper defaults from `v0.2.175-refactor-apk` to `v0.2.177-login-a8729d6`.
- Recorded ARM64 phone evidence for public release verification, `v0.2.175 -> v0.2.177` upgrade install, locked-screen return, Pull from Cloud, game launch/profile visibility, and force-stop/relaunch recovery.
- Narrowed remaining release-readiness blockers to confirmed Push-to-Cloud overwrite/round-trip evidence, safe controlled local save mutation, `.local` signing continuity, repeated local cache/freshness upgrade coverage, and diagnostics polish.

## 2026-06-08 - Documentation status refresh

- Documentation now has a canonical current Android status page advertising the working ARM64 baseline while keeping polish/hardening blockers explicit.
- README, overhaul status, roadmap, Android validation docs, runtime findings, login testing notes, and validation runbooks now share the same release posture: working locally, not release-candidate complete.
- GitHub repository description already matched this posture: working Android launcher, currently in polish and cloud-save hardening.

## 2026-06-08 - Device-independent polish pass

- Centralized Android APK metadata selection so smoke/login/verification scripts prefer compatible APKs by versionCode instead of only by file write time.
- Aligned release install/verify helper defaults with the current ARM64-only public release asset.
- Tightened Push-to-Cloud warning text and recovery cleanup logging so normal success is less likely to read like a failure.
- Focused Android diagnostics filters and save-validation summaries on startup freshness, assembly cache evidence, cloud Pull/Push markers, and crash indicators.

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
- Updated GitHub-facing project text for the current APK state: `v0.2.185-responsive-ui`, ARM64-only release assets, signed release expectations, emulator limits, and the current working-but-hardening Android validation state.
- Updated Steam version selection docs for the current hardening state, including the validation runbook, branch marker provenance requirements, side-by-side cache behavior, and explicit release blockers for beta/password handling and save compatibility.
- Updated GitHub-facing project text to reflect that the ARM64 local Android path now works through fresh install/runtime validation, Steam download, Pull from Cloud, Push to Cloud, Pull-after-Push round trip, Android local save handoff, and game launch, while release hardening remains active.
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
- Steam beta/version selection is implemented for validation, but release signoff still requires ARM64 evidence for public/default regression, beta branch download/startup routing, inaccessible/private/password branch behavior, save compatibility across branches, and Pull-before-Push backup safety.
- Push to Cloud is locally validated and the latest public APK has release-facing confirmation/cancel safety evidence, but confirmed newest-public Push mutation still needs an explicit smoke before release-candidate signoff.
- Release-readiness validation still needs repeated local stale assembly cache coverage after `.local` signing continuity is restored.
- Android `x86_64` emulator runs are fallback/diagnostic coverage only unless the unsafe Godot path is explicitly forced.

## [Initial Overhaul Baseline]
- Forked repository and established independent project documentation and workflow for a sustained rewrite.
