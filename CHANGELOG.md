# Changelog

## 2026-06-21 - Cache cleanup marker refactor prerelease

- Published the local-package ARM64 prerelease `v0.2.283-local-cache-cleanup-marker-refactor` with APK `StS2Launcher-v0.2.283-local-cache-cleanup-marker-refactor-arm64-v8a.apk`.
- Centralized selected-version cache cleanup marker prefixes behind a shared launcher partial so cleanup readers, writers, runtime-pack preservation evidence, and static audits use one marker contract.
- Updated branch-switch safety marker writing to use the existing branch-switch marker prefix constants instead of duplicated literal labels.
- Preserved the build/static validation posture: Steam version-selection static audit passed 461 checks, multi-version runtime audit passed 156 checks, branch-guidance parity passed, managed Release build passed, and the ARM64 APK build/verification plus crypto patch verification passed. This APK is build/static-audit evidence only and does not replace ARM64 public/public-beta runtime evidence. No Steam Cloud Push was run during this validation.

## 2026-06-21 - Evidence marker prefix refactor prerelease

- Published the local-package ARM64 prerelease `v0.2.282-local-evidence-marker-refactor` with APK `StS2Launcher-v0.2.282-local-evidence-marker-refactor-arm64-v8a.apk`.
- Centralized runtime-cache, save-origin, and manual cloud-sync marker prefixes behind shared launcher partials instead of duplicating marker labels across readers, writers, and validation evidence.
- Updated the Steam version-selection and multi-version runtime static audits to guard the new evidence-marker helper boundaries while preserving non-public runtime-pack, selected-runtime save-origin, and Steam Cloud Push safety checks.
- Preserved the build/static validation posture: this APK passed static audits, managed Release build, ARM64 APK verification, and crypto patch verification, but public/public-beta runtime behavior still relies on the existing ARM64 runtime evidence until this exact APK is device-tested. No Steam Cloud Push was run during this validation.

## 2026-06-21 - Branch marker/runtime cache helper refactor prerelease

- Published the local-package ARM64 prerelease `v0.2.281-local-branch-marker-refactor` with APK `StS2Launcher-v0.2.281-local-branch-marker-refactor-arm64-v8a.apk`.
- Centralized Steam branch-marker field names and integrity-provenance parsing behind shared launcher helpers used by readiness, diagnostics, installed-branch catalog parsing, runtime metadata inspection, and static audits.
- Centralized Android app-private path normalization and `/data/user/0/<package>` versus `/data/data/<package>` alias handling so branch marker provenance and runtime-cache identity checks use one guarded comparison path.
- Updated the Steam version-selection and multi-version runtime static audits to guard the new helper boundaries without weakening non-public runtime-pack, branch-selection, or Steam Cloud safety checks.
- Preserved the build/static validation posture: this APK passed static audits, managed Release build, ARM64 APK verification, and crypto patch verification, but public/public-beta runtime behavior still relies on the existing ARM64 runtime evidence until this exact APK is device-tested.

## 2026-06-21 - Evidence redaction helper refactor prerelease

- Published the local-package ARM64 prerelease `v0.2.280-local-evidence-redaction-refactor` with APK `StS2Launcher-v0.2.280-local-evidence-redaction-refactor-arm64-v8a.apk`.
- Split evidence path resolution, safe evidence filenames, public-evidence redaction, focused-log redaction, local-only artifact policy, sensitive-content checks, and redaction-review field formatting into shared PowerShell helpers.
- Updated the version-selection evidence scaffold, exporter, reviewer, branch capture, and beta-integrity capture scripts to use the shared helpers instead of duplicating policy strings and path logic.
- Preserved the build/static validation posture: this APK is build and audit evidence only, and public/public-beta runtime behavior still relies on the existing ARM64 runtime evidence until this exact APK is device-tested.

## 2026-06-20 - Audit evidence helper refactor prerelease

- Published the local-package ARM64 prerelease `v0.2.279-local-audit-helper-refactor` with APK `StS2Launcher-v0.2.279-local-audit-helper-refactor-arm64-v8a.apk`.
- Split common PowerShell static-audit plumbing, Android `run-as` shell quoting, evidence marker parsing, and Markdown evidence table formatting into shared helpers used by the Steam version-selection and multi-version runtime evidence scripts.
- Extended both static audits to guard the new helper boundaries and the evidence collector imports, preserving the non-public runtime-pack, branch-selection, and Steam Cloud safety checks.
- Preserved the build/static validation posture: this APK is build and audit evidence only, and public/public-beta runtime behavior still relies on the existing ARM64 runtime evidence until this exact APK is device-tested.

## 2026-06-20 - Compact label helper refactor prerelease

- Published the local-package ARM64 prerelease `v0.2.278-local-compact-label-refactor` with APK `StS2Launcher-v0.2.278-local-compact-label-refactor-arm64-v8a.apk`.
- Replaced the remaining bespoke compact two-line button label implementations with the shared `CompactButtonDetailLabels.Apply` path and `CompactButtonDetailLabelSpec.Default(...)` helper where the standard compact sizing applies.
- Removed obsolete compact-label partials for the quick-start toggle and support/action buttons while keeping custom sizing explicit for the controls that intentionally differ.
- Updated the Steam version-selection static audit to guard the consolidated compact-label helper boundary without weakening runtime-pack, branch-selection, or Steam Cloud safety checks.
- Preserved the build/static validation posture: this APK is build and audit evidence only, and public/public-beta runtime behavior still relies on the existing ARM64 runtime evidence until this exact APK is device-tested.

## 2026-06-20 - Compact helper refactor prerelease

- Published the local-package ARM64 prerelease `v0.2.277-local-helper-refactor` with APK `StS2Launcher-v0.2.277-local-helper-refactor-arm64-v8a.apk`.
- Split compact launcher helper code into smaller partials: selected-version summary card skinning, cloud Push confirmation warning construction, compact two-line button text parsing, and compact two-line Godot control construction now have focused files.
- Extended the Steam version-selection static audit so those refactor boundaries remain guarded without weakening branch/runtime-pack or Steam Cloud safety checks.
- Preserved the build/static validation posture: this APK is build and audit evidence only, and public/public-beta runtime behavior still relies on the existing ARM64 runtime evidence until this exact APK is device-tested.

## 2026-06-20 - Launcher/runtime refactor audit prerelease

- Published the local-package ARM64 prerelease `v0.2.276-local-refactor-audits` with APK `StS2Launcher-v0.2.276-local-refactor-audits-arm64-v8a.apk`.
- Continued the launcher/runtime/UI refactor by splitting branch-availability diagnostics into report output and marker parsing helpers, with static audit coverage for the new boundary.
- Preserved multi-version runtime and Steam branch selection guardrails: Steam version-selection static audit passed 454 checks, multi-version runtime audit passed 148 checks, managed Release build reran cleanly with zero warnings/errors, and the local ARM64 APK build/crypto verification passed.
- Documented that this prerelease is build/static-gate evidence only. Public/public-beta runtime behavior still relies on the existing ARM64 evidence until this APK is device-tested.

## 2026-06-14 - Public-beta Android art fallback release

- Published `v0.2.187-beta-art-fallback`, an ARM64 public APK with Steam beta branch hardening and the validated Android run-history art fallback.
- Added a branch-local fallback for missing selected-branch run-history room icons so `public-beta` no longer errors on older/current save references to missing `doormaker_boss` art.
- Validated on ARM64 hardware that `public-beta` still mounts its own side-by-side PCK, applies the `gameplay/Run history asset fallback` patch, reaches the main menu, and no longer emits the old `doormaker_boss` loader errors.
- Updated current release docs and verification commands to point at `v0.2.187-beta-art-fallback`.

## 2026-06-14 - Steam beta branch integrity diagnostics

- Added per-depot public-manifest comparison evidence to selected Steam branch markers so `public-beta` can show whether each downloaded depot is branch-specific, public-identical, or missing public comparison data.
- Changed non-public depot resolution so a depot with no explicit selected-branch manifest but a public manifest is downloaded as an explicit `public-inherited` depot instead of being skipped.
- Added launcher diagnostics for partial Steam branch evidence, including counts of selected depots matching public, differing from public, and lacking public comparison.
- Clarified the beta integrity investigation path so mixed beta/public behavior and art asset issues can be distinguished between Steam-served partial branches, launcher fallback, stale cache files, or runtime remote/config behavior.
- Tightened non-public launch readiness so old branch markers without public-vs-selected integrity counters no longer satisfy managed or native startup checks.
- Expanded beta integrity evidence capture with an availability-aware, clean-redownload-gated, auditable `Classification:` summary, evidence-readiness verdict, public-sharing warning, classifier input metrics, bounded public/default and selected depot manifest rows, branch-availability marker proof, clean-redownload marker proof, public/default marker status, selected cache tree capture, focused beta-integrity logcat capture, and focused PCK/art/audio/data/font key-asset hash comparison.
- Added a beta-integrity summary review helper so release-gate checks can read `Evidence readiness:` and fail when the captured evidence is not ready for final classification.
- Added optional beta-integrity capture switches to run the summary review immediately after capture and fail the step when `Evidence readiness:` is not ready.

## 2026-06-13 - Public-beta launch validation

- Fixed Android selected-version marker provenance checks so app-private path aliases under `/data/data/<package>` and `/data/user/0/<package>` compare as the same install location.
- Validated `public-beta` side-by-side launch on an ARM64 local hardening build: marker provenance passed, `SlayTheSpire2.pck` loaded from `game_versions/public-beta-8128824d/game`, startup patching completed `17/17`, and the game reached the main menu.
- Clarified that the stale `Previous game launch did not finish` recovery banner can remain after a prior failed launch and is not by itself evidence of a current startup crash.

## 2026-06-12 - Steam branch storage hardening

- Added refreshed selected-branch availability/status details to the branch-switch confirmation so password-protected, no-manifest, and not-listed branch blockers are visible before switching.
- Fixed blocked selected-version update checks so the support button no longer remains on `Checking...` and the status/log explicitly name the selected game version and blocked branch reason.
- Updated successful `REFRESH GAME VERSIONS` handling so the launcher status/log immediately names the selected version and surfaces any selected-branch blocked state from refreshed app-info metadata.
- Fixed the ready/action and download branch selectors so refreshed Steam branch metadata immediately updates the selected-version helper text instead of leaving stale availability/password/unavailable wording visible.
- Tightened selected-version helper text so password-protected, no-manifest, and not-listed Steam branches are shown as blocked states instead of implying they are generally downloadable.
- Updated the normal launcher support-menu raw-log copy button to use the same review-before-sharing wording as startup recovery.
- Aligned the README static-audit guardrail with the current `discovery-led dropdown selector` wording and pointed overhaul status at the version-selection release-readiness tracker.
- Fixed the Steam version-selection evidence-folder scaffold so new validation folders now include `ARTIFACT_HYGIENE.txt`, `PUBLIC_SHARE_MANIFEST.txt`, and release-readiness tracker guidance before any device capture runs.
- Added a release-readiness gate checklist to the Steam version-selection GitHub issue template so tester reports map directly to the remaining signoff blockers.
- Aligned the version-selection evidence template, completion audit, validation index, and static audit with the new release-readiness tracker so signoff evidence has one consistent contract.
- Added a Steam version-selection release-readiness tracker and static guardrails so implemented branch-selection work is clearly separated from ARM64 evidence still required for release signoff.
- Updated the startup recovery raw-log copy button and helper text to warn before copying that raw logs require review/redaction before sharing.
- Added review/redaction warnings to raw error log clipboard flows so copied diagnostics are not silently treated as public-safe.
- Added the diagnostics public-sharing warning to startup-recovery diagnostics exports as well as full launcher diagnostics reports.
- Added a public-sharing warning to exported launcher diagnostics reports so full reports clearly require review/redaction before public posting.
- Added `PUBLIC_SHARE_MANIFEST.txt` to generated Steam version-selection evidence bundles to separate preferred public artifacts from local-only/manual-review artifacts.
- Added static audit coverage for the evidence capture helper's CRLF/LF-safe logcat and marker path splitting.
- Fixed CRLF/LF splitting in the Steam version-selection evidence capture helper so logcat processing and branch-marker path parsing handle Android/Windows line endings correctly.
- Added a launcher diagnostics index to Steam version-selection evidence bundles so available diagnostics reports are discoverable without auto-copying potentially identifying full report contents.
- Added a focused logcat redaction summary artifact with processed-line and changed-line counts to make evidence-bundle redaction state easier to review.
- Made raw full logcat capture opt-in in the Steam version-selection evidence helper while still generating focused and redacted focused log artifacts by default.
- Added `ARTIFACT_HYGIENE.txt` to generated Steam version-selection evidence bundles so raw logs are clearly marked local-only unless manually reviewed and redacted.
- Added a self-describing warning header to generated redacted focused logcat artifacts so shared logs still disclose the best-effort/manual-review limitation.
- Expanded focused logcat redaction to cover common account/username/serial-like fields and local user paths in addition to credentials/tokens.
- Clarified that generated redacted logcat evidence is best-effort and must still be manually reviewed before public posting.
- Added a redacted focused logcat artifact to the Steam version-selection evidence capture helper and made docs/templates prefer it for public issue sharing.
- Extended the Steam version-selection evidence capture helper to record the `sts2_steamkit_debug_logs` Android global setting so log bundles show whether SteamKit debug logging was disabled or explicitly enabled for sanitized auth diagnostics.
- Tightened the Steam version-selection issue template so public `adb logcat` attachments must be redacted and must confirm SteamKit debug logs were disabled or sanitized.
- Updated release-note and roadmap wording so public release docs mention quiet-by-default SteamKit logging with opt-in sanitized auth diagnostics.
- Documented the optional `sts2_steamkit_debug_logs` auth-diagnostics workflow in the version-selection runbook/tooling docs and static audit so sanitized SteamKit logs stay opt-in.
- Made Android SteamKit debug logging opt-in via `sts2_steamkit_debug_logs=1` to reduce normal diagnostic noise while preserving credential/token sanitization when enabled.
- Exposed SteamKit credential/token log sanitization in launcher diagnostics, evidence templates, user guide, and the Steam version-selection GitHub issue template.
- Sanitized Android SteamKit debug log forwarding so common password/token/session fields are redacted before launcher diagnostics, and added static audit coverage for the sanitizer.
- Corrected stale Steam version-selection completion-audit wording that still described manual branch entry/no discovery, and added a static guardrail against reintroducing that old selector model.
- Tightened Steam version-selection completion/runbook/save-compatibility docs and static audit checks so they distinguish baseline Pull-before-Push/local-save evidence from branch-switch-only backup evidence.
- Hardened side-by-side Steam branch cache naming so branch install slots use a case-stable storage identity across managed launcher code, bootstrap routing, and Android native startup diagnostics.
- Preserved the selected Steam branch value for Steam requests and user-facing diagnostics while preventing duplicate caches from casing-only dropdown/metadata differences such as `Beta` versus `beta`.
- Removed the normal fallback `beta` dropdown injection so non-public versions are discovery-led from Steam app-info; default/public remains always available, and an already-saved branch remains visible for recovery/retry diagnostics.
- Tightened Android native startup gating so a selected game version with a valid PCK but missing/mismatched branch provenance returns to the launcher instead of consuming a launch request or falling through toward stale branch startup.
- Tightened native Autofill lifecycle cleanup so one-shot Steam login values are cleared when the Android activity stops or is destroyed, in addition to consume/cancel/TTL cleanup.
- Cleared the Godot password field immediately after capturing a login request so Autofill/manual passwords do not remain in the launcher UI while Steam authentication runs.
- Extended the Steam version-selection static audit to guard discovery-led dropdown behavior, case-stable branch storage identity, native selected-branch launch gating, and Autofill credential cleanup.
- Updated version-selection status and validation docs to describe the discovery-led dropdown, selected-branch native launch gate, and stricter Autofill cleanup lifecycle without removing the ARM64 validation blockers.
- Improved selected-version helper text for saved branches that are absent from the refreshed Steam app-info catalog, explicitly naming stale/private/inaccessible/password-protected/unavailable possibilities before download.
- Added concise Steam app-info metadata badges to refreshed game-version dropdown labels so visible options can show ready/build/password/unavailable status before selection.
- Added a pre-download selected-branch gate that blocks known password-protected, no-Windows-manifest, or absent saved non-public branches when refreshed Steam app-info evidence already proves the branch is not downloadable for the account.
- Applied the same selected-branch availability gate to game-version update checks while still allowing APK/app update checks to complete.
- Kept selected-cache cleanup available for blocked branches with bad local branch metadata, while preventing the replacement download from starting until branch availability evidence becomes valid.
- Tightened Push confirmation wording after branch switches so it names the selected version slot and the required selected-version Pull/local-save/backup evidence before any Steam Cloud overwrite.
- Recorded important Android local save evidence counts inside completed and blocked Push markers so Pull-before-Push artifacts preserve the local-save evidence behind the gate decision.
- Added live current important Android local save evidence count/presence diagnostics alongside baseline manual Push prerequisite status.
- Updated Android status/release-validation docs to call out the baseline manual Push evidence gate and required diagnostics for current Pull/local-save evidence.
- Updated baseline Push block status/log text to name the selected game version when Pull or Android local-save evidence is missing.
- Added a baseline manual Push gate requiring Pull-from-Cloud evidence for the currently selected version and Android local save evidence before any Steam Cloud upload, even when no branch switch marker exists.
- Added a generic `Manual Pull completed before Push` evidence flag while preserving the existing branch-switch Pull flag for stricter cross-version validation.
- Exposed the generic Pull-before-Push completion flag in launcher diagnostics and static audit coverage.
- Added a baseline manual Push prerequisite aggregate to diagnostics so reports show whether current-version Pull evidence and Android local save evidence are both present before upload.
- Recorded baseline manual Push prerequisite status inside completed and blocked Push evidence markers so artifacts preserve the decision state at the time of Push.
- Updated public README/status/release-note wording for discovery-led branch selection, metadata badges, unavailable-branch gates, native launch gating, stricter Autofill cleanup, and baseline Pull-before-Push safety evidence.

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
