# System cleanup and local validation — 2026-10-02

Implemented the requested removal/consolidation across launcher-owned managed code, native startup, tooling callers, tests and active guidance. The current [disposition inventory](../REDUCTION_INVENTORY.md) identifies authority, callers, state boundaries and retained candidates. The affected phone remains unavailable; this work does not establish resolution of the title-screen freeze.

## Changes and retained boundaries

- **Consolidated implementations:** handoff owner now holds attempt-bound readiness under its existing lock; controller initializes preferences directly; recovery owns private cleanup; diagnostic reports capture each marker once; runtime/mod file identity uses the existing SHA-256 owner; authentication retries and completion use their existing owners; mod-result parsing/documents live in the result store; discovery uses the existing known-mod snapshot; durable restart queries/cancellation live in the native request store. Bootstrap branch storage naming delegates to the existing branch owner.
- **Deleted implementations:** nested readiness owner/snapshot, startup preference coordinator, unused timeout, cleanup target, constant Play adapter, retry scaffolding, connection-result wrapper, presentation-owned mod parsing, known-mod cache wrapper and four SHA-256 bodies. Native sound interface/session/cue/silent implementation and their calls are removed because production only instantiated silence. No audible feature, save feature or supported launch route was removed.
- **File-only consolidation:** 301 fragmented source files become 63 responsibility files across view/sections, downloader, warmup, diagnostics, runtime models/writer/evidence, authentication/model and LAN. All 1,468 member token sequences were verified unchanged. Managed source files decreased from 730 to 490; lines from 63,567 to 55,479, including formatting changes. These are measurements, not behavior or deletion quotas.
- **Retained compatibility:** factories outside managed state locks, observed late tasks, scene/render provenance, Android startup-task lifetime anchor, preload accounting/failure/VFX ordering, metadata-only Harmony inspection, process/mutation-boundary authorization, legacy restart migration, atomic request commits, current credential/Keystore/storage formats, dependency/script registration, account isolation and separate Vanilla/Modded saves. Workshop boundary-local move/path/event helpers remain with recorded reasons.
- **Guidance:** contribution rules remain in CONTRIBUTING, commands in development, validation in Android status. Root status now points to those sources; its dated body is archived. Dated investigations and upstream/generated/downloaded content remain retained. No source-shape gate or governance framework was added.

## Validation

Evidence is retained in `tmp/system-cleanup-20261002/`. All commands ran against this workspace; phone/live-service results are excluded.

| Check | Reconfirmed baseline | Final result / evidence |
| --- | --- | --- |
| Managed regressions | 145 passed | **154 passed, 0 failed**; `managed-final.log`. Includes original readiness assertions, restoration/failed-operation preservation, stale auth completion, retry limits and coherent/missing/malformed diagnostic snapshots. |
| Android/JVM | 53 passed | **56 passed, 0 failures/errors**; `native-cleanup.log` and JUnit XML. Adds request-state/query/cancellation characterization. |
| Save-safety probe | 12 scenarios passed | **12/12**; `save-final.log`. Atomic saves, interrupted/account-scoped transfers, synchronization-before-loading, selection persistence and lifetime anchor retained. Simulated remote only. |
| Actual-resource preload probe | Existing regression | **161 resources / 44 frames**, at most four finalizations/eight outstanding; cached repeat three frames. Fallback completes eight resources with one failure/nested scenes; Harmony factory is not re-executed. `asset-final.log`. |
| UI/mod fixtures | Existing profiles/fixtures | Both 412×915 touch and 1280×800 desktop interaction profiles pass. Importer Vanilla/disabled/active and BaseLib/dependent/importer chain pass; `ui-mod-final.log`, `baselib-final.log`. Desktop results only. |
| Surface and source audit | Source/APK preserved | 25 public managed declarations unchanged; no live resource/build bindings to removed partial paths; all baseline files have dispositions; `public-surface.log`, `file-dispositions.csv`, `consolidation-provenance.json`, `source.diff`, `semantic.diff`. |
| APK build and inspection | v0.2.433 retained | Existing build wrapper passes ARM64 contents and crypto checks; package/certificate match and version increases. Packaged managed DLL matches current publish; removed native classes absent from fresh DEX. `apk-build.log`, `apk-update-compatibility.log`, `final-audit.log`. |

The source snapshot contains 916 files and the original v0.2.433 APK. Pre-consolidation source and compiled assembly are retained separately so semantic edits can be reviewed independently from file movement. Existing nullable-context and Gradle deprecation warnings remain; the build has no errors.

## Published artifact

- [ARM64 APK](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/download/v0.2.434-cleanup-local/StS2Launcher-v0.2.434-cleanup-local-arm64-v8a.apk), [checksum](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/download/v0.2.434-cleanup-local/StS2Launcher-v0.2.434-cleanup-local-arm64-v8a.apk.sha256), [metadata](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/download/v0.2.434-cleanup-local/StS2Launcher-v0.2.434-cleanup-local-arm64-v8a.apk.json).
- The owned-source snapshot and audit/review/verification evidence remain locally retained under `artifacts/android/`. The original source/APK baseline remains separately preserved.
- Version `0.2.434-cleanup-local`, code `434001`; package `com.sts2launcher.overhaul.fork.local`.
- APK SHA-256: `0d54b858cd7afd8c6242fa5cc862008b407d4294c601a7e96afbd25edd922e55`.
- Signer SHA-256: `FD0E3D5ACF435C1D23BFC5C426E99AA9EB5808619FF1FC214FFCA99CFAC7E57A`.

The matching identity supports an in-place update over the retained v0.2.433 candidate. Publication verification also compared the APK against the downloaded GitHub v0.2.432 artifact: package and signing certificate match, and the version code increases from 432002 to 434001. The user requested publication after the original unpublished delivery. No installation, uninstall or data reset was performed. Build outputs use a task-local Gradle cache to avoid existing OneDrive placeholders; production build dependencies are unchanged.

## Line-count audit

Against the preserved v0.2.433 source baseline, owned code has 17,469 physical lines removed and 9,363 added: a net reduction of 8,106 lines. This includes comments, blank lines, moved code and formatting; generated outputs, third-party sources, downloaded content and documentation are excluded. Production changes before mechanical consolidation removed 1,142 lines and added 506, a net reduction of 636. The full per-file audit is retained in `tmp/system-cleanup-20261002/line-count-by-file.csv`.

## Review and pending acceptance

One fresh-context final review found no Critical, Important or actionable Minor issues. It inspected semantic changes, original/current implementations, consolidation/type/initializer preservation, characterizations and evidence. It independently verified APK SHA-256, managed payload equality and deleted native-class absence. The verdict permits delivery as an unpublished locally validated candidate; it does not establish device stability or release readiness. The full review is retained in the evidence package.

Execution decisions: retain source/APK snapshots because the workspace had no Git metadata at the start of the audit (local storage cost); use pre-change characterization for behavior-preserving refactors and RED/GREEN for confirmed defects as requested (behavior proof rather than artificial failure); retain small Workshop helpers to avoid widening internal APIs (some helper repetition remains). The review left physical freeze/stability, Android rendering/lifecycle/mod effects, live Android Steam, multi-device LAN, and actual installation/data preservation unproven because the required devices/live sessions were unavailable. Their cost is pending acceptance, not a claim of success from local checks.

Physical launch stability, the original freeze, actual affected renderer/game/mod configuration, Android mod gameplay and live Android Steam transfers remain unverified. Preserve the existing [device investigation and acceptance procedure](launch-flow-audit-2026-10-02.md#physical-device-acceptance--pending-at-the-users-direction): controlled Modded/Vanilla reproduction with bugreport/logcat/Perfetto, then 20 successful Modded launches (ten cold, six warm, two background/resume, two lock/unlock), title interaction, gameplay/return, Vanilla startup and preservation of saves, credentials and selection. Local passes do not replace that procedure.
