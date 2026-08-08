# Changelog

## 2026-08-08 - Stage 5 evidence and candidate custody hardening (unreleased; device matrix 0/10)

- Bound every non-debuggable in-app save export to an exact-byte read-back hash, current Android tree identity, selected SaveContext identity, and a structured completion event in the inventoried raw device log. The physical-matrix reviewer rejects missing, reused, tampered, or wrong-session bindings.
- Made inventoried raw logcat and captured state files authoritative for Review 5. Collector summary booleans and counts are independently recomputed, and fatal/ANR, Steam-backed gameplay saving, dropped or swallowed save failures, false `Synced`, and mismatched recovery/failure evidence fail the review.
- Pinned the candidate workflow's GitHub Actions, Godot commit, SCons version, Gradle distribution, and checked-in wrapper bytes. Gradle now builds and verifies unsigned bytes without secrets; a fresh runner signs them with a scoped temporary keystore, removes credentials immediately, and only then verifies and retains the candidate.
- Hash-sealed the 14,896-file historical Android evidence tree without moving or rewriting it. The sole v0.2.416 update keystore still has no verified independent offline backup, so signing credentials remain unconfigured and no candidate build, device test, Steam operation, or release has started.

## 2026-08-06 - Stage 5 desktop validation (unreleased; device matrix 0/10)

- Added filesystem-backed automatic-sync fixtures for vanilla/modded saves on public/public-beta, exact namespace isolation, branch and mod-set mismatch blocking, safe pre-Play download, divergence, offline retry, commit/read-back failures, and byte-exact Restore/Undo. The integrated production-path probe passes 70/70 scenarios, including 9/9 filesystem scenarios.
- Replaced same-process restart simulation as Stage 5 evidence with real child-process termination and fresh-process reopening of the Android local store plus persisted deterministic fake Steam state. The hard-restart matrix passes 118/118 before/after persistence edges: Begin 12, upload 32, Pull 30, Restore 28, and Undo 16.
- Made save transfer, destination backup, and remote verification byte-exact. BOM, CRLF, NUL, and raw-byte-only changes now survive Push/Pull or fail read-back verification instead of being normalized through text hashes; launcher context/control documents remain textual.
- Preserved and indexed historical Android logs as regression evidence, expanded the read-only Stage 5 collector, and changed Android automation to retain a commit-bound candidate APK without publishing it. The exact-candidate physical matrix and Review 5 remain 0/10, so none of this is a public fix or release claim.
- Bound the sole candidate path to the affected v0.2.416 `.local` package, exact published APK baseline, and FD0E…E57A update signer. Package/baseline selection and signing-reset bypasses were removed; dedicated signing credentials remain unconfigured, and the candidate workflow still cannot publish.
- Extended the always-available local support export with bounded, full, hash-verified retained and recovery-journal rollback snapshots without writing local data or contacting Steam. Ambiguous legacy transfer-backup trees are left for read-only recovery scanning instead of being guessed into full snapshots.

## 2026-08-06 - Desktop-tested local save recovery implementation (unreleased; device validation pending)

- Reused automatic-sync snapshots for bounded, content-addressed per-context recovery history plus byte-for-byte Restore and Undo. Recovery writes are Android-local, atomic, serialized against transfer operations, and held away from Steam until explicit post-validation approval.
- Added a read-only legacy scan across vanilla, modded, temporary, launcher-backup, and retained-snapshot sources. Originals are never moved or deleted; imported candidates are read twice, unknown provenance stays unknown, and foreign-account candidates are quarantined.
- Added support export bundles whose local bytes are read back and hashed, plus a Saves-page recovery flow that requires Export before Restore. Unknown-account recovery can be tested locally but cannot be approved or synchronized.
- Removed automatic vanilla-to-modded seeding. Run-history progression reconstruction remains a manual support-only last resort rather than an automatic recovery path.
- Hardened crash retry around Restore/Undo journals, partial overlays, account/context and snapshot-role binding, case-fold history collisions, and unrelated concurrent local edits. Desktop fake-store recovery/transfer coverage passes 59/59, the legacy scanner passes 6/6, namespace isolation passes 4/4, local gameplay safety passes 4/4, cancellation passes 11/11, and the 34-view UI matrix passes. Android-device recovery and real Steam Cloud validation remain pending; no real Steam Cloud operation was performed. This implementation is not a public fix or a release claim until the complete Stage 5 physical matrix passes.

## 2026-07-25 - Android evidence consolidation (unreleased)

- Consolidated Android evidence across the README, troubleshooting guidance, runtime/status ledgers, release notes, and validation checklists. The documentation now labels automated, API 36 x86_64 emulator, exact published ARM64 hardware, and outstanding ARM64 paths separately.
- Recorded the visible Stage 5 emulator pass against current unreleased source: local x86_64 evidence APK `0.2.417-stage5-final5-evidence-local` (`241714`, SHA-256 `ce7f37ae898665ebba3798b923c9f98741c8b6092b14ae6b41851832dd7bc891`) validated native cold/cached routing, fallback/recovery controls, forced bootstrap failure and retry, active-cache preservation, rotation, Home/Recents resume, native IME state, and scoped fatal/ANR checks.
- Preserved the architecture boundary: production x86_64 routes to native fallback, while a forced-Godot diagnostic reproduced the known GodotSharp/Mono signal-6 failure. The emulator did not validate the managed launcher, Steam services, ARM64 behavior, `NMainMenu`, or gameplay.
- Corrected stale `v0.2.400`/`v0.2.401` current-release references and verification/install commands to the published `v0.2.416-startup-recovery-ime` ARM64 artifact.
- Added the remaining exact-candidate ARM64 gate. No APK was built or released, no device or Steam Cloud Push was used, and no core-game file was modified for this documentation stage.

## 2026-07-25 - Android runtime-pack, native recovery, and IME hardening

- Corrected Android assembly validation so the patched `sts2.dll` supplied by the active runtime pack is checked against `compatibility.json` / `patch_validation.json`, while ordinary game assemblies continue to be checked against the selected Steam installation. Patched and source DLLs may now differ in both size and hash without rejecting an authentic runtime pack.
- Preserved transactional assembly-cache staging, full required-file validation, one retry, and the last valid active cache when runtime-pack evidence is stale, incomplete, corrupt, or interrupted.
- Changed native fallback recovery to clear pending normal/Safe Start preferences and carried intent payloads before starting `LauncherActivity` exactly once. Restarting the launcher can no longer immediately retrigger the failed game route.
- Added an Android launcher IME policy around Godot's hidden `GodotEditText`. Launcher startup, boot-transition cleanup, and non-editor resume/focus paths suppress Samsung's unintended keyboard request, while deliberate managed text-field focus remains allowed.
- Built exact non-debuggable local-test APK `0.2.416-startup-recovery-ime-local` (`416001`, SHA-256 `fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b`) and installed it as an in-place update on ARM64 Samsung `SM-F966B` / Android 16. Public Start Game promoted the manifest-matched runtime-pack assembly, reached real `NMainMenu`, and logged stable 1s, 3s, 10s, 30s, and 60s heartbeats.
- A cumulative debuggable build exercised a controlled missing-validation-report failure, routed to `NativeFallbackActivity` without `SuperNotCalledException`, preserved the patched active cache, and returned to the launcher through the actual **Restart launcher** button after clearing the pending payload.
- Android bootstrapper/lifecycle/routing/recovery/IME/boot tests, managed Release compilation, non-mutating cloud production-path tests, Gradle release assembly, ARM64 APK structure and crypto checks, installed-artifact hash comparison, and update compatibility from `v0.2.412` pass. No device lock/power automation, Steam Cloud Push, or core-game modification was performed.

## 2026-07-17 - Automatic local save backup and recovery

- Changed `Save Backup` from a cloud-operation-only guard into an automatic, path-preserving mirror under `StS2Launcher/Saves/Current`. Existing saves are captured when the setting is applied, before game launch, after Manual Pull/modded-save seeding, and immediately after each successful Android game-save write.
- Added bounded `StS2Launcher/Saves/History` generations when mirrored content changes. Vanilla and native modded namespaces remain distinct, including legacy pre-Push backup paths, and root `profile.save` is now treated as critical backup content.
- Added guarded startup/pre-launch recovery for missing or empty profile, progress, and preferences files. Existing non-empty saves are never overwritten, and current-run/run-history files are backed up but never automatically restored.
- The Save Backup control now reports the number of mirrored save files; diagnostics report the mirror directory, count, and latest write time.
- Managed Release compilation, the local-save backup policy probe, the 834-check Steam version-selection audit, and diff checks pass. No connected-device validation, APK build, release, push, Steam Cloud Push, or core-game modification was performed.

## 2026-07-17 - Larger StS2 Launcher boot identity

- Extended the readiness-driven sequence from 2,480 ms to 4,000 ms, with a longer separation, 800 ms identity reveal, weighted 350 ms compression-and-settle, 1,200 ms confirmation hold, 1.4% tactile pulse, and 700 ms launcher handoff. Reduced motion remains a direct 200 ms fade.
- Increased the boot mark from 42% to 52% of the viewport short edge, raised its responsive clamp from 112-184 dp to 144-232 dp, and increased the live wordmark from 18 sp to 20 sp while retaining viewport-fit clamping.
- Renamed current user-facing branding from `StS2 Mobile` to `StS2 Launcher`, including the boot wordmark, Android app label, launcher header, diagnostics, authentication copy, and current project documentation. The launcher identity does not claim ownership of Slay the Spire 2 or represent an official mobile port.
- Added `Made by SocialHummingbird` and a direct link to the project repository in the Help destination.
- Updated Java sequence/layout tests for the new timing, scale, compression, and pulse contracts. The existing Samsung hardware evidence predates this choreography, so the revised presentation still requires connected-device visual validation.

## 2026-07-17 - Readiness-driven Android boot transition

- Replaced the app-icon-only Android splash with an official Godot Engine mark that holds while the runtime and managed launcher genuinely initialize, then runs an original seven-phase, 2,480 ms Godot-to-StS2 Mobile reveal before handing off to the ready launcher.
- Added transparent cyan, orange, and resolved StS2 Mobile vectors without the adaptive-icon square, responsive short-edge sizing, a live `StS2 MOBILE` wordmark, one authoritative animation clock, and independently testable opacity, scale, registration-offset, clipping, and pulse state.
- Added cold-start-only playback policy, explicit restart handoff suppression, pending normal/Safe Start suppression, a direct 200 ms reduced-motion path, early-readiness caching, duplicate-signal protection, activity-destruction cleanup, touch interception only while visible, and a 30-second fail-open watchdog.
- Added `AndroidBootTransitionSound`, seven phase-aligned cue definitions, lifecycle-safe cue/session handling, and the active no-op `SilentBootTransitionSound`. The extension point adds no audio asset, playback API, audio-focus request, permission, service, dependency, or preference, and future sound cannot retime or block the visual sequence.
- Added a managed-to-Java first-render bridge after two process frames and one `FramePostDraw`, and disabled the bootstrap project's separate Godot boot image without changing downloaded game content or shader warmup.
- Added Godot Engine logo attribution and explicit restrictions against Sony/PlayStation branding, assets, startup audio, sound-alike cues, or reproduced console timing, easing, geometry, and composition.
- Initial Samsung hardware testing found that a repeated splash-exit callback could reattach a terminal overlay and expose the keyboard after Home/resume. The corrected controller rejects terminal attachment and posts a second IME suppression pass; policy/source tests and dense resume captures cover the fix.
- Installed exact local ARM64 build `0.2.407-boot-hardware-resume-local` (`407001`, SHA-256 `f4ed4266a2019669383d16b5a4108601f4fe41a9a5107969f85532f74387434a`) on Samsung `SM-F966B` / Android 16. Portrait, landscape, reduced motion, Home/resume, rotation, secure lock/unlock, touch release, and restart/normal/Safe Start skip routes passed with no focused fallback, fatal, signal, ANR, unexpected process-death, IME, or transition-timeout evidence.
- Managed Release compilation, all Java policy/identity/sequence/sound tests, Android Mono Java/resource compilation, ARM64 APK structure/ABI/crypto verification, exact installed-hash comparison, screenshot audit, and diff checks pass. The game and Safe Start probes stopped after their native skip markers, so this is not new downstream game-startup evidence. No commit, push, release, Steam Cloud Push, or downloaded/core-game modification was performed.

## 2026-07-16 - Native modded-save Pull release

- Replaced the launcher SavesMerger save-path substitute with native modded save directories. Installed SavesMerger/UnifiedSavePath entries are now shown as deprecated and excluded from runtime activation.
- Added Manual Pull modded-save seeding based on the game's upstream v0.108 first-launch copy set: `profile.save`, profile 1-3 progress/current-run/multiplayer-run/preferences files, and run history.
- Made cloud-provided modded namespaces authoritative. Vanilla files are seeded only when Steam Cloud has no modded data for that account/profile, and only from content downloaded successfully during the current Pull.
- Added mandatory app-private backups before any affected local modded file is replaced, write verification, a local provenance marker, diagnostics, and plain-language Pull status. Private backup/provenance paths are excluded from cloud discovery.
- Added a no-device policy regression probe covering vanilla-only, mixed cloud namespaces, account metadata, path normalization, history, excluded backup/legacy files, and full SavesMerger Workshop-title detection.
- Built `v0.2.401-native-modded-save-pull` as ARM64 APK `StS2Launcher-v0.2.401-native-modded-save-pull-local-arm64-v8a.apk`, version code `401001`, and SHA-256 `1295cdb113010063e2c3a44123cff379f110480bded5c80bd24e4896e8dbcea3`. Managed compilation, the policy probe, APK structure/ABI/crypto checks, and update compatibility against `v0.2.400` pass.
- No connected-device Manual Pull/save-visibility test was run on this exact APK, no device save state was changed, and Steam Cloud Push was not run.

## 2026-07-16 - PowerVR touch compatibility release

- Published `v0.2.400-powervr-touch-compat` as GitHub's Latest release with exact connected-device-tested ARM64 APK `StS2Launcher-v0.2.400-powervr-touch-compat-local-arm64-v8a.apk`, version code `400001`, and SHA-256 `623830caad7a684e3358fbb22564210a1236588e03e7161dfcf30cc5aa76cdc3`.
- Added live graphics-device evidence and automatic PowerVR/ImgTec/Imagination routing to OpenGL Compatibility at both the managed launcher and native Android restart boundaries.
- Preserved Auto/Vulkan/Safe Start behavior on the connected Adreno device and validated the PowerVR Vulkan-to-OpenGL handoff with a synthetic reporter-class graphics marker.
- Confirmed exact package/signing continuity and a higher version code against `v0.2.399`; APK structure, ABI, Android crypto patches, renderer policy, PowerVR audit, mod evidence, remote release digest, and GitHub release-hygiene checks passed.
- Reconfirmed BaseLib plus Quick Restart activation on public/default startup, including Quick Restart's three installed Harmony targets and real `NMainMenu`.
- No Steam Cloud Push was run. Actual Pixel 10 Pro / PowerVR reporter confirmation remains required before closing issue #34.

## 2026-07-16 - PowerVR renderer and public mod runtime release

- Published `v0.2.399-powervr-renderer-mod-runtime` as GitHub's Latest release with exact connected-device-tested ARM64 APK `StS2Launcher-v0.2.399-powervr-mod-runtime-hashfix-activation-evidence-local-arm64-v8a.apk`, version code `399004`, and SHA-256 `d39825fe2f79ca86af6ff4c83bbcd1eaeeb0fd3eeeedde509cdb3aa6a17420fe`.
- Verified package/signing continuity and a higher version code against `v0.2.398`; GitHub release hygiene passed for the APK, checksum sidecar, JSON metadata, release body, and Latest target.
- Connected Samsung `SM-F966B` / Android 16 / Adreno 830 validation reached real `NMainMenu` under Auto/Vulkan and explicit OpenGL, wrote post-startup probes and heartbeats through 120 seconds, and contained no focused target-process fallback/fatal/native/ANR/kill signature.
- Fixed the Android native signal 11 found during mod identity hashing by routing launch-readiness SHA-256 through the Java bridge, then fixed nested selected-root/runtime-manifest activation evidence matching.
- Validated public/default BaseLib, Quick Restart 2, and SavesMerger with three enabled mods and zero failed runtime loads. Quick Restart installed three Harmony targets and its in-game `Restart Room` action successfully restored an active combat room; BaseLib remains partial Android compatibility and SavesMerger uses the launcher substitute.
- Kept issue #34 open because the connected device is Adreno rather than PowerVR. The release contains the leading PowerVR fix, but reporter-class hardware confirmation is still required. Steam Cloud Push was not run.

## 2026-07-15 - PowerVR renderer compatibility implementation

- Backported Godot's 4.5.2 all-PowerVR transform-feedback shader-cache workaround onto the custom 4.5.1 Android engine. The release workflow now builds that patched engine from source instead of reusing the historical `v0.2.88` native library.
- Replaced the Android launcher's unconditional OpenGL Compatibility override with explicit Auto, Vulkan, and OpenGL renderer modes. Auto leaves renderer choice to the project, while Safe Start now truthfully uses Auto with shader warmup and Steam Cloud disabled.
- Added persistent `last_renderer_attempt.txt` evidence and Android 11+ `ApplicationExitInfo` capture in `last_process_exit_info.txt`, including bounded trace data when Android provides it, so a restarted launcher can report why the previous process ended.
- Added focused Java policy tests, a PowerVR compatibility source audit, managed and Android Java builds, two-ABI native engine builds, deterministic launcher UI validation, and an inspected ARM64 local APK (`0.2.399-powervr-renderer-local`, version code `399001`, SHA-256 `d0c98e73a79226d503030c80cfd32b1872df0db0401bc301740671be557b344b`). The packaged stripped native library contains the all-PowerVR marker and no legacy single-model marker. Reporter-class Pixel 10 / PowerVR hardware confirmation remains pending, so issue #34 is not yet considered fixed. No Steam Cloud Push was run.

## 2026-07-15 - Five-destination launcher UI and PowerVR cause analysis

- Published `v0.2.398-launcher-ui-redesign` with Home, Saves, Versions, Mods, and Help destinations; phone bottom navigation; wide/foldable top navigation; Android safe-area/orientation handling; and retained launch, cloud, mod, repair, and diagnostic event routing.
- Added a deterministic 20-viewport preview matrix with accessibility, bounds, minimum-target, pixel-determinism, event-contract, and no-Steam-Cloud-Push checks. The static repository audit passed 819 checks.
- Installed the exact ARM64 APK over existing app data on Samsung `SM-F966B`; final unlocked physical portrait/landscape capture did not run after the device disconnected, and Steam Cloud Push was not run.
- Analysed issue #34 reporter `log7.txt`: `v0.2.397-atlas-memory-compat` reaches real `NMainMenu` on Pixel 10 Pro / Android 17 / PowerVR D-Series DXT-48-1536, then exits before the first one-second post-startup probe.
- Matched the failure to the known Godot 4.5.1 OpenGL Compatibility PowerVR transform-feedback shader-cache bug. Godot 4.5.2 disables that cache on all PowerVR devices; the current custom 4.5.1 engine only carries the older single-model `PowerVR Rogue GE8320` exception.
- Confirmed the published launcher forces OpenGL Compatibility through an unreachable success-marker condition and that its Safe Start does not actually select the advertised default renderer. That analysis identified an engine backport plus renderer-policy repair, not a core game redesign.

## 2026-07-11 - Android atlas memory compatibility

- Added an Android-only atlas compatibility path for issue #34 that skips eager `AtlasManager.LoadAllAtlases()` during deferred startup.
- Atlas-backed card, relic, power, and potion resources now prefer individual texture files while their source atlas is unloaded; missing individual entries can still use lazy source-atlas loading.
- Added exact reporter-runtime/PCK validation and a Harmony attachment probe. All 875 card sprites in the reporter PCK have individual imports that avoid BPTC/S3TC.
- Connected ARM64 validation on Samsung `SM-F966B` reached real `NMainMenu`, passed post-startup probes and the 300-second heartbeat, and rendered Card Library and Relic Collection without focused fatal, native signal, ANR, LMKD, lifecycle teardown, or static-string cleanup evidence.
- Published tester APK `v0.2.397-atlas-memory-compat`; PowerVR confirmation remains pending on the Pixel 10 Pro / Android 17 reporter device. Steam Cloud Push was not run.

## 2026-07-05 - Normal launch automation and shader warmup evidence

- Added file-based normal launch automation with `action=launch`, separate from `action=launchsafe`, so device evidence can trigger the real Start Game path without unreliable rotated-display coordinate taps.
- Published and installed local ARM64 evidence build `0.2.368-normal-launch-automation-test` on `SM-F966B`; `branch=public-beta` plus `action=launch` reached the public-beta main menu with matched public-beta PCK/runtime-pack/active `sts2.dll` evidence.
- Device-proofed the shader warmup status marker: clearing `shader_warmup_version` caused first-run warmup to record a 45-second watchdog warning and then complete with `Rendered 1713 shader warmup materials`, followed by settings/saves and game startup.
- Preserved Steam Cloud safety during the normal-launch automation test: `last_manual_cloud_push.txt` was missing before and after, no upload markers were logged, and no Push to Cloud was run.
- Remaining cleanup: shader material/scene scanning still emits noisy Godot error stack traces during warmup even though launch completes; lower-power shader crash reports and controller action behavior still need focused validation.

## 2026-07-05 - Steam auth failure reporting evidence

- Added classified Steam sign-in failure reporting so login exceptions and session-returned auth failures write `last_steam_auth_failure.txt` and show user-actionable status text instead of only raw SteamKit/base exception messages.
- Included the Steam auth failure marker in Help & Reports diagnostics attachments and full report file lists.
- Published and installed local ARM64 evidence build `0.2.366-auth-failure-reporting-test` on `SM-F966B`; an invalid local credential handoff was consumed, connected to Steam, failed with `InvalidPassword`, and wrote a `credentials` marker for selected branch `public-beta`.
- Preserved Steam Cloud safety during the auth test: no `last_manual_cloud_push.txt` marker was created, the existing blocked-push marker was unchanged, and no StS2 Cloud upload path was exercised.
- Added shader warmup status diagnostics: startup now records the latest warmup phase in `last_shader_warmup_status.txt`, includes it in Help & Reports, and writes a watchdog marker if warmup is still active after 45 seconds.

## 2026-07-05 - Android FMOD bridge and public-beta audio proof

- Added a local Android FMOD Java/JNI bridge so `libfmod.so` can complete its Android platform initialization instead of failing with invalid handles, missing Java SDK methods, or bank-load errors.
- Published local evidence build `0.2.364-fmod-audio-devices-test` on ARM64 device `SM-F966B`; public-beta reached the Slay the Spire 2 main menu with matched PCK/runtime-pack/source/active `sts2.dll` evidence.
- Verified FMOD logs now show successful DSP buffer setup, successful FMOD initialization, global 3D settings, and desktop bank loads for `Master.strings.bank`, `Master.bank`, `sfx.bank`, `temp_sfx.bank`, and `ambience.bank`.
- Preserved Steam Cloud safety during testing: no Push to Cloud was run, upstream Android startup sync was skipped, and the cloud write thread stopped.

## 2026-07-03 - Public-beta modded launch and release docs

- Published the local-package ARM64 prerelease `v0.2.352-savemerger-compat-local` with APK `StS2Launcher-v0.2.352-savemerger-compat-local-arm64-v8a.apk` and SHA-256 `25daa224b90311775957a5638f7173342c078b1baca4dadd5454ca3ff80af26e`.
- Updated GitHub-facing status and release notes to make the current public-beta support explicit: the latest tested public-beta payload is `v0.108.0`, selected PCK/runtime-pack evidence matches, runtime patch validation passes, and fallback/gating is not counted as success.
- Updated Workshop/mod guidance to describe the current state honestly: BaseLib, Quick Restart 2, and manual SavesMerger are selected/scanned in strict public-beta modded validation, but SavesMerger real-save behavior still needs broader tester proof.
- Added current public feedback priorities from Reddit and GitHub reports: launcher UI scaling/scroll reachability, controller input, shader compile crashes, install/update clarity, and exact-version issue reporting.
- Added `docs/reddit-post-log.md` to track public Reddit posts, visible comment-derived issue themes, and reusable reply snippets for bug-hunting follow-up.
- Preserved Steam Cloud safety guidance: no Push to Cloud was performed during validation, and active modded-save risk states remain guarded.

## 2026-06-29 - GitHub issue reporting forms

- Replaced the main public Markdown issue templates with GitHub issue forms for general bugs, crashes/startup, device compatibility, game download/branch issues, Steam login/cloud saves, and Workshop/mod/save-merger reports.
- Added required fields for exact APK identity, device/app/version context, selected Steam branch, PCK/runtime/sts2.dll evidence, runtime cache and patch validation markers, cloud Pull/Push safety state, selected mods, save-merger behavior, screenshots/logs, and redaction checks.
- Added `.github/ISSUE_TEMPLATE/config.yml` to disable blank public issues and point reporters at current status, testing guidance, and the issue-reporting guide.
- Added `docs/issue-reporting.md` and refreshed README/testing/log/contributor docs so users know what evidence to attach and what not to post publicly.

## 2026-06-29 - GitHub release hygiene guardrails

- Added `scripts/check-github-release-hygiene.ps1` to validate fork releases explicitly, avoiding the local `gh` default-repo ambiguity with upstream `Ekyso/StS2-Launcher`.
- The checker verifies APK asset naming, GitHub SHA-256 digest, `.sha256` sidecar contents, JSON/build-info metadata, release body APK/SHA/package references, and single-APK release expectations.
- Updated the GitHub release verification workflow to fail on missing checksum sidecars and run the new hygiene checker before structural APK verification.
- Updated the Android release workflow to generate release notes with exact APK, SHA-256, package, versionCode, signing channel, sidecar names, and release-candidate validation boundary.
- Added `scripts/audit-github-release-inventory.ps1` and `docs/github-release-inventory.md` so the recommended APK, GitHub's non-prerelease Latest target, historical test builds, missing sidecars, and release-body gaps are visible in one place.
- Refreshed README/status/release-validation docs to point at `v0.2.336-cleartext-cdn-debug`, avoid the misleading `/releases/latest` shortcut for tester builds, and document the release hygiene check as a pre-announcement gate.
- Updated the remote `v0.2.188-branch-cache-hardening` release body because GitHub still marks it as Latest; it now labels itself as a superseded baseline and points users at the current tester APK.

## 2026-06-21 - Steam version-selection UI/support audit module split prerelease

- Published the local-package ARM64 prerelease `v0.2.288-local-audit-ui-support-split` with APK `StS2Launcher-v0.2.288-local-audit-ui-support-split-arm64-v8a.apk` and SHA-256 `2905c2f00f4facca8ea495a5787e60c090307dca25102c33ec9b4c978b690a76`.
- Split additional Steam version-selection static audit guardrails out of the top-level orchestrator into focused PowerShell modules for startup recovery reports, ready-state action/cloud/support controls, and portal status/UX support.
- Extended the focused Steam Guard code-section and native credential/login-panel audit modules so one-shot code submission, uppercase code normalization, and Android native credential-panel contracts stay guarded outside the top-level orchestrator.
- Updated helper-boundary guardrails and GitHub-facing docs for the expanded audit-module boundary. This APK is build/static-audit evidence only and does not replace ARM64 public/public-beta runtime evidence. No Steam Cloud Push was run during this validation.
- Preserved the build/static validation posture: Steam version-selection static audit passed 496 checks, multi-version runtime audit passed 156 checks, branch-guidance parity passed, managed Release build passed, and the ARM64 APK build/verification plus crypto patch verification passed.

## 2026-06-21 - Steam version-selection audit orchestrator split prerelease

- Published the local-package ARM64 prerelease `v0.2.287-local-audit-orchestrator-split` with APK `StS2Launcher-v0.2.287-local-audit-orchestrator-split-arm64-v8a.apk` and SHA-256 `a8cbcb3f8072d3639b548f07c85f9fa1da5aabd450ce49ac96e613d37e178c43`.
- Split additional Steam version-selection static audit guardrails out of the top-level orchestrator into focused PowerShell modules for status capsules, compact workflow/current-task UI, Steam Guard code-section controls, compact section/scroll flow, compact install/version/download behavior, and startup/warmup/status contracts.
- Updated helper-boundary guardrails and GitHub-facing docs so the expanded audit-module boundary is explicit and future audit work can stay local to focused modules.
- Preserved the build/static validation posture: Steam version-selection static audit passed 493 checks, multi-version runtime audit passed 156 checks, branch-guidance parity passed, managed Release build passed, and the ARM64 APK build/verification plus crypto patch verification passed. This APK is build/static-audit evidence only and does not replace ARM64 public/public-beta runtime evidence. No Steam Cloud Push was run during this validation.

## 2026-06-21 - Steam version-selection UI audit module split prerelease

- Published the local-package ARM64 prerelease `v0.2.286-local-audit-ui-module-split` with APK `StS2Launcher-v0.2.286-local-audit-ui-module-split-arm64-v8a.apk` and SHA-256 `640d6d06131143d098ebb631d6a170f4910be9f85248b576a4b39a2bde48f769`.
- Split more Steam version-selection static audit guardrails into focused PowerShell modules for launcher automation, local Steam credential handoff, contextual confirmations, branch-switch/manual cloud Push safety, native credential/login-panel contracts, compact detail labels, compact section setup, quick-start safe-flow guidance, Help & Reports diagnostics drawer behavior, and launcher portal chrome.
- Moved selected-version update-check guardrails into the download/update workflow module so update-check run/block/result coverage stays with the download flow instead of the top-level orchestrator.
- Updated the tooling docs and GitHub-facing status/checklist text to point at the expanded audit-module boundary. This APK is build/static-audit evidence only and does not replace ARM64 public/public-beta runtime evidence. No Steam Cloud Push was run during this validation.
- Preserved the build/static validation posture: Steam version-selection static audit passed 487 checks, multi-version runtime audit passed 156 checks, branch-guidance parity passed, managed Release build passed, and the ARM64 APK build/verification plus crypto patch verification passed.

## 2026-06-21 - Steam version-selection audit module split prerelease

- Published the local-package ARM64 prerelease `v0.2.285-local-audit-module-split` with APK `StS2Launcher-v0.2.285-local-audit-module-split-arm64-v8a.apk`.
- Split large Steam version-selection static audit guardrails into focused PowerShell modules for helper boundaries, launcher shell, branch selector, branch runtime/cache, branch availability, download/update workflows, and Steam session authentication.
- Updated the tooling docs and GitHub-facing status text so the module boundaries are explicit and future guardrail additions do not keep expanding the top-level audit orchestrator.
- Preserved the build/static validation posture: Steam version-selection static audit passed 477 checks, multi-version runtime audit passed 156 checks, branch-guidance parity passed, managed Release build passed, and the ARM64 APK build/verification plus crypto patch verification passed. This APK is build/static-audit evidence only and does not replace ARM64 public/public-beta runtime evidence. No Steam Cloud Push was run during this validation.

## 2026-06-21 - Branch availability marker helper prerelease

- Published the local-package ARM64 prerelease `v0.2.284-local-branch-availability-marker-helpers` with APK `StS2Launcher-v0.2.284-local-branch-availability-marker-helpers-arm64-v8a.apk`.
- Centralized Steam app-info branch availability marker labels, metadata keys, visible-row parsing, marker path/value/row reads, and branch availability report formatting behind shared helpers.
- Routed branch dropdown options, compact failure status, diagnostics, and downloader marker/report formatting through the shared marker contract.
- Preserved the build/static validation posture: Steam version-selection static audit passed 470 checks, multi-version runtime audit passed 156 checks, branch-guidance parity passed, managed Release build passed, and the ARM64 APK build/verification plus crypto patch verification passed. This APK is build/static-audit evidence only and does not replace ARM64 public/public-beta runtime evidence. No Steam Cloud Push was run during this validation.

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
