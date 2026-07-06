# Multi-version runtime architecture

## Objective

Support public and beta Steam branches on one Android device without pairing one branch's PCK/assets with another branch's game-code assembly.

The launcher must treat a playable install as a matched runtime unit:

```text
Steam branch + depot manifest + release_info.json + PCK + Android-compatible sts2.dll + patch compatibility + save policy
```

## Current first implementation slice

The launcher now has an explicit `GameRuntimeSlot` identity for the selected branch. It records:

- branch and display name
- install slot kind and directory
- game directory
- selected PCK path and SHA-256
- `release_info.json` path
- release version, release commit, and release build ID when present
- depot manifest counts and a depot-manifest fingerprint from `steam_branch.txt`
- canonical runtime slot ID
- canonical runtime slot identity string
- selected branch source `sts2.dll` path and SHA-256
- active Android `sts2.dll` path and SHA-256
- future runtime-pack manifest path
- runtime pairing status

Readiness now requires runtime compatibility in addition to a valid PCK and valid branch marker. A non-public branch is considered runtime-compatible only when a usable runtime pack is installed. A non-public branch source `sts2.dll` by itself is not enough to mark the selected version ready, and a stale publish-cache `sts2.dll` is not accepted as readiness evidence without a selected-runtime-matched runtime pack. The public branch keeps the legacy packaged-public-runtime path so existing public installs can still prepare the Android cache at launch.

The native Android startup cache now records a runtime identity and refreshes when the selected branch/PCK/source `sts2.dll` identity changes. Branch game-code assemblies such as `sts2.dll` are copied into the active Android publish cache instead of being treated as permanently APK-owned files.

Native startup now prefers a runtime-pack Android game-code assembly when present:

```text
files/runtime_packs/<branch>/sts2.dll
```

If no runtime-pack assembly is present, startup falls back to the selected Steam branch source assembly under the selected game directory. Runtime-pack `sts2.dll` participates in the runtime cache ID, so switching between public, beta, and future compatibility packs refreshes the active Android assembly cache instead of reusing stale game code.

The native runtime cache key also includes the runtime-pack directory name, `compatibility.json`, `patch_validation.json`, and every runtime-pack `.dll` file identity. This means changing pack metadata, patch-validation evidence, support assemblies, or the game-code assembly invalidates the active publish cache.

Native startup also writes a prepared-runtime cache marker:

```text
current_runtime_cache.txt
```

This marker records the selected branch, selected PCK hash, selected source `sts2.dll` hash, runtime source, runtime pack path, runtime ID, fixed Godot publish-cache path, and active publish-cache `sts2.dll` hash. It gives diagnostics and future device evidence collection a file-level proof of which runtime slot prepared the currently active Godot Mono cache.

Runtime-pack manifests are now parsed as evidence, not just detected as files. Diagnostics report whether the pack matches the selected branch, whether its Android `sts2.dll` exists, whether the declared hash matches the file, whether declared patch validation passed, and whether the pack is usable for the selected runtime slot.

The canonical runtime slot ID is derived from the normalized branch, branch storage directory, release metadata, depot-manifest fingerprint, PCK hash, source `sts2.dll` hash, runtime source, runtime-pack ID, Android/runtime `sts2.dll` hash, patch-set version, and patch-validation status. This gives logs, save-origin markers, runtime packs, and diagnostics one shared identifier for the same matched runtime unit.

Download completion now writes persistent installed runtime-slot evidence:

```text
current_runtime_slot.json
```

This marker records the selected runtime slot ID, runtime slot identity, readiness decision, readiness problem, PCK/source/active assembly hashes, runtime-pack usability status, and patch-compatibility status. It provides a stable installed-slot baseline before the selected branch is launched. Diagnostics must treat the marker as current evidence only when its branch, runtime slot ID, PCK hash, and source `sts2.dll` hash still match the currently selected runtime.

The marker is written by the explicit launch/download readiness path after selected `GameRuntimeSlot` inspection. Startup display, offline fallback, and branch-switch status use a lightweight downloaded-state check so they do not spend time in duplicate hash/inspection passes while `current_runtime_slot.json` may still describe the previous branch. Native launch still treats a stale marker as a blocker: a Start Game request with public-beta selected and a public marker, or public selected and a public-beta marker, returns to launcher/bootstrap mode until managed readiness inspection writes fresh selected-branch evidence.

The managed launch-readiness cache includes `current_runtime_slot.json` in its file identity key. If that marker is deleted, rewritten, or stale while the selected branch files are otherwise unchanged, Start Game misses the memory cache and reruns full selected-runtime inspection so the native launch gate receives fresh marker evidence instead of falling through to launcher/bootstrap mode. Empty-cache and wrong-branch/data-dir misses are rejected before any file-identity comparison, so first launch and branch-changed launch attempts do not spend time hashing marker files or probing source assemblies just to discover that no cache entry can match. Candidate cache entries compare current identities incrementally and stop at the first mismatch, which keeps stale branch-marker/runtime-slot evidence from forcing every later marker and source-assembly identity read. When a same-target cache entry fails identity validation or throws during validation, that exact snapshot is cleared so later Start Game attempts do not keep revalidating a stale entry that is already known unusable. Only freshly evaluated readiness with an inspected runtime slot is stored; pending launch markers, lightweight downloaded-state checks, downloaded-state failures, missing downloaded-file blockers, and cache-hit snapshots are rejected by `Store` so startup/status paths and cheap file-presence blockers cannot seed the Start Game cache. When that rejected result targets the same branch/data directory as the cached entry, the stale matching entry is cleared immediately so repeated Start Game presses do not keep revalidating a cache entry that can no longer be used. Cache validation and cache-store exceptions are recorded as cache miss/skipped phases, then Start Game continues into fresh selected-runtime readiness instead of treating the cache itself as a launch blocker. Large files such as the PCK, selected source assembly, active Android publish-cache assembly, and runtime-pack DLL payloads use cheap path/size/mtime identity; small launch-gating metadata and marker files, including runtime-pack JSON reports, use SHA-256 content identity so marker rewrites cannot be missed by coarse timestamp resolution. Runtime-pack payload identity is also bounded: payload entries are streamed into a stable digest with only a small sample retained in the cache key, so cache validation still notices DLL/JSON changes without building an unbounded string for every payload file. The runtime-pack manifest `compatibility.json` has its own cache identity and is excluded from the generic runtime-pack payload identity, so cache validation still tracks manifest changes without hashing that manifest twice. Marker content hashing is capped at 1 MiB so abnormal marker files cannot turn a cache check into an unbounded startup read. `LauncherLaunchReadinessCache` now owns only cache storage, locking, hit/miss reporting, and store/clear flow; `LauncherLaunchReadinessCacheKey` owns target matching and mismatch reporting; `LauncherLaunchReadinessCacheIdentities` owns launch-relevant file and marker identity capture for both cache creation and current-state validation. That split keeps synchronization changes away from the expensive file identity path and keeps the invalidation list from drifting between store and hit checks.

Patch compatibility is now a separate pre-launch evidence gate. Public keeps the legacy APK patch baseline. Non-public branches require either a runtime pack declaring passed patch validation or a branch-local validation marker:

```text
files/game_versions/<branch>/game/.android_patch_validation.json
files/runtime_packs/<branch>/patch_validation.json
```

Validation evidence must declare and match the selected branch, PCK hash, source `sts2.dll` hash, and patch-set version. A downloaded beta PCK plus branch source assembly is not sufficient by itself to mark the slot playable, and old validation markers that omit PCK/source assembly hashes are not accepted for non-public readiness.

Download completion now runs a first-pass static validator for non-public branches. It writes `.android_patch_validation.json` only for the selected game directory and records:

- selected branch
- selected PCK SHA-256
- selected source `sts2.dll` SHA-256
- patch-set version
- validation mode
- validation surface version
- checked/present/missing critical symbol counts
- category summaries for startup, cloud-save, model-db, and platform compatibility
- missing critical symbols, if any

This validator checks the critical startup/platform/save/model symbols required by the Android launcher patch baseline. It is not a full replacement for runtime Harmony validation, but it prevents a branch from being treated as playable when the selected game-code assembly is obviously incompatible with the Android patch set.

When static validation passes for a non-public branch, the launcher now also writes a branch-local runtime pack. Runtime-pack generation first deletes the previous selected-branch runtime-pack directory and then writes a fresh pack, so stale support assemblies or old sidecar files cannot survive a branch update. When validation fails, the selected-branch runtime-pack directory is deleted so an older passed pack cannot remain as hidden playable evidence.

The current Android runtime-pack format is a closed single-game-assembly pack:

```text
files/runtime_packs/<branch>/sts2.dll
files/runtime_packs/<branch>/compatibility.json
files/runtime_packs/<branch>/patch_validation.json
```

The compatibility manifest records the runtime pack ID, source runtime slot ID, selected branch, release/version metadata, depot-manifest fingerprint, PCK hash, source `sts2.dll` hash, Android/runtime `sts2.dll` hash, support assemblies copied into the pack, per-support-assembly SHA-256 values, patch-set version, validation mode, validation surface version, critical-symbol counts, a patch-validation report path, `generatedFromCleanDirectory`, and passed patch-validation status. The current writer emits an empty support-assembly list and copies only `sts2.dll`; future support assemblies must be explicitly declared and hashed before they are accepted. A runtime pack is not usable unless it declares a runtime pack ID, source runtime slot ID, source branch, source PCK hash, source assembly hash, Android assembly hash, explicit support assembly list, matching support assembly hashes, `generatedFromCleanDirectory=true`, passed patch-validation status, and an existing patch-validation report file whose own status is also `passed`. Runtime packs are closed DLL sets: every `.dll` in the pack directory must be either `sts2.dll` or a declared support assembly, every declared support assembly must exist, and every support assembly hash must match. The report must bind back to the same runtime pack ID, source runtime slot ID, branch, PCK hash, source assembly hash, Android assembly hash, support assembly list, support assembly hashes, patch-set version, validation surface version, and clean-directory marker declared by the manifest. These fields must all match the selected runtime and runtime-pack file. Patch compatibility is not accepted from an unusable runtime pack. Diagnostics report distinct runtime-pack usability statuses so missing runtime pack IDs, missing slot IDs, missing Android hashes, missing support declarations, undeclared DLLs, support hash mismatches, missing patch-validation reports, failed report statuses, report/manifest mismatches, slot-ID mismatches, and manifest status failures are not hidden behind generic errors. Native startup also refuses incomplete runtime packs: `compatibility.json`, `patch_validation.json`, runtime-pack `sts2.dll`, runtime pack ID, source runtime slot ID, source branch, source PCK hash, source assembly hash, Android assembly hash, explicit support declarations/hashes, `generatedFromCleanDirectory=true`, and passed patch-validation status must all be present. Native startup cross-checks the declared branch/PCK/source assembly against the selected on-device game files, requires `patch_validation.json` to report `passed` and match the compatibility manifest, and requires the declared Android assembly hash to match the runtime-pack `sts2.dll` file. Runtime packs missing the clean-directory marker or support assembly hash contract are treated as legacy/stale evidence and rejected. Native startup then prefers that runtime-pack `sts2.dll` for the active Android assembly cache and copies only the manifest-declared runtime-pack DLL set into the active publish cache.

Managed selected-version validation uses the same runtime-pack contract before doing expensive regeneration work. If a non-public Start Game readiness check inspects a slot that is already playable through a usable runtime pack with passed patch validation, it returns that inspected slot immediately instead of rescanning critical symbols and rewriting the runtime pack. Runtime-pack inspection has already verified the patch set version, selected branch, PCK/source hashes, Android assembly hash, support assembly declarations, and validation report, so this shortcut preserves the same launch gate while avoiding repeat runtime-pack generation on normal cache misses.

Android PCK patching can change the selected PCK file hash after the runtime pack was generated from the original Steam PCK. The native patcher records `.android_pck_patch_v35` next to the patched PCK. Managed and native runtime-pack validation accept the runtime-pack source PCK hash when that marker proves the currently selected Android-patched PCK was produced from the declared source PCK. Empty legacy markers are accepted only for existing patched installs; newly patched PCKs write a JSON marker containing source/current PCK hashes and patch version. Patch marker `v35` repairs the ARM64 FMOD scene wiring that previous Android PCK patches had commented out for launch stability, records lightweight FMOD bank presence metadata, deterministically resolves prior marker chains without inheriting stale older source hashes, and migrates failed experimental `user://fmod_banks` or `/sdcard/sts2b` bank path patches back to the canonical `res://banks/desktop` scene paths. Diagnostic bank extraction remains evidence-only; the actual Android audio fix is the native FMOD Java/JNI bridge that initializes `libfmod.so`, provides the expected `org.fmod.FMOD` shim methods, and allows FMOD to initialize, set global 3D settings, and load the canonical desktop bank files.

The native startup path also treats `current_runtime_slot.json` as a launch gate for downloaded game PCKs. A one-shot Start Game request may only mount the selected downloaded PCK when the marker branch matches the selected branch, `filesReady`, `playable`, `runtimeCompatible`, and `patchCompatible` are all true, and the marker PCK/source `sts2.dll` hashes match the currently selected files on disk. Missing, stale, unreadable, hash-mismatched, or non-playable runtime-slot evidence returns the app to launcher/bootstrap mode instead of mounting a potentially mismatched public/beta PCK.

Managed launcher UI follows the same readiness rule at launch time. START GAME, safe launch, and restart auto-launch run through `LauncherLaunchReadiness.Evaluate`, which includes selected `GameRuntimeSlot.Playable`; blocked launches surface the prepared readiness problem instead of a generic download-required message. The saved-session, login, offline, and branch-switch ready-status paths only check that selected downloaded files and branch metadata are present, then defer full runtime pairing and patch validation to Start Game. Ready and download-required status copy uses the branch carried by the prepared readiness object when it is available, so the visible selected-version and install-slot text stays tied to the same readiness snapshot instead of rereading branch preferences while the UI is being refreshed. Branch-switch ready checks pass the newly selected branch directly into lightweight downloaded-state readiness after saving the preference, which avoids a second preference read on that refresh and keeps the displayed state aligned with the branch that was just confirmed. Startup, branch-switch, and refresh-complete UI setup apply saved action preferences, selected branch, and branch dropdown options together where possible, so the launcher does not populate the branch controls once with a fallback branch and immediately rebuild them for the saved branch. Branch-switch UI refresh uses the just-saved branch when rebuilding aggregate action preferences, so it does not read the branch preference back from storage in the same operation. Branch-switch confirmation and refresh-complete status build selectable dropdown options from the same visible branch-catalog snapshot already used for selected-branch blocker/status text, so those UI paths do not reread the Steam branch availability marker just to derive adjacent selector copy. If that lightweight downloaded-state inspection itself fails, it records `downloaded-state-check-failed` readiness evidence and shows the failure as not-ready status instead of crashing the launcher or treating fallback as success. `LauncherLaunchReadiness` is split so the core file is the evidence model, `LauncherLaunchReadiness.FullRuntime` owns full selected-runtime validation and cache storage, `LauncherLaunchReadiness.DownloadedState` owns the lightweight startup/status path, and `LauncherLaunchReadiness.Pending` owns accepted-launch pending marker construction. Launch-readiness cache statuses are centralized in `LauncherLaunchReadinessCacheStatus`, so marker values such as `fresh`, `fresh-cached`, `memory-cache-hit`, and `downloaded-state-only` stay aligned with evidence tooling while the startup path is refactored. Mod-readiness cache statuses are similarly centralized in `LauncherModLaunchReadinessCacheStatus`, including the vanilla fast path value `not-needed-vanilla`, so `last_launch_attempt.txt` can distinguish skipped vanilla mod scans from fresh or cached modded selector checks. Modded Start Game now builds the mod-source identity first and checks `LauncherModLaunchReadinessCache` before building the full known-mod snapshot, so a valid cache hit does not still rescan Workshop/manual mod lists. That source identity walks staged/manual mod directories once, filters `.json`, `.pck`, and `.dll` files by extension, and streams each file identity into the digest instead of materializing every identity record before hashing. The model-level launch path also refuses in-process or restart launches when prepared readiness is missing or the selected runtime is not ready, so alternate launch entry points cannot bypass the runtime-slot gate.

Launcher support actions follow the same split. Help Report exports run full selected-runtime readiness so the attachment has runtime-slot, runtime-pack, patch-compatibility, and cache evidence. The on-screen Last Problem and Copy Log actions use downloaded-state readiness only, so opening recovery text or copying the visible log does not rerun full runtime inspection after a failure. Previous-startup warnings read the startup and launch-attempt markers once per launcher session, then leave later ready-status refreshes to the lightweight downloaded-state path without repeating recovery log lines. This keeps the support UI responsive while preserving full evidence in explicit reports.

The shared Start Game coordinator suppresses duplicate launch requests once a launch attempt is in progress and disables the Start Game/Safe Start controls while readiness and handoff run. Its managed path is split into explicit request-acceptance, selected-runtime readiness, mod readiness, and launch-handoff phases. `LauncherLaunchCoordinator.StartGame` is now only the phase list; `LauncherLaunchCoordinator.Attempt` owns accepted-request and terminal failure marker plumbing, `LauncherLaunchCoordinator.Readiness` owns selected-runtime readiness, `LauncherLaunchCoordinator.ModReadiness` owns launcher-side mod readiness, and `LauncherLaunchCoordinator.Handoff` owns the final bridge/no-op handoff result. `LaunchAttemptContext` owns the live attempt, per-press attempt ID, selected-runtime readiness, and mod-readiness timers, so phase methods ask the context for blocked, failed, or ready timing evidence instead of passing raw stopwatch objects between phases. The coordinator writes a pre-handoff `ready` marker, then passes the same attempt ID and a live timing snapshot callback into the model so terminal `restart requested`, `safe android restart requested`, and `in-process signalled` markers include elapsed time up to the actual handoff request instead of reusing stale pre-handoff timing. Selected-version display/status helpers live in a separate status partial. Model-level handoff is split the same way: `LauncherModel.Launch` keeps the normal/safe handoff entry flow, `LauncherModel.Launch.Readiness` detects defensive prepared-readiness blockers, `LauncherModel.Launch.InProcess` owns current-process signalling and signal-failure evidence, and `LauncherModel.Launch.Restart` owns Android restart bridge markers. `LauncherLaunchHandoffResult` carries a concrete requested/failure result back to the coordinator, so a model-level blocker such as `restart requested without ready files` is not overwritten by a generic no-op handoff marker. Successful handoff results must also name an accepted evidence phase (`restart requested`, `safe android restart requested`, or `in-process signalled`), and the coordinator validates it with `LauncherLaunchAttemptPhases.IsSuccessfulHandoffPhase`; any other requested phase is treated as a failed no-op handoff instead of success. Terminal blocked/failed paths share `FinishFailedLaunchAttempt`, so marker writes, visible status/log updates, optional launcher-log entries, and launch-control re-enabling stay consistent across setup, readiness, mod-readiness, and handoff failures. If accepted-launch setup fails before selected-runtime readiness can start, it writes `last_launch_attempt.txt` with phase `setup failed`, records the exception type/message, and re-enables launch controls. Otherwise it writes `last_launch_attempt.txt` with phase `checking` as soon as an accepted request starts, before selected-runtime readiness begins, so a crash during readiness inspection cannot leave stale evidence from an older launch. If selected-runtime readiness throws, the coordinator overwrites the marker with `readiness failed`, records the exception type/message, and re-enables launch controls. If readiness blocks or a pre-handoff exception occurs, the guard resets and launch controls are re-enabled so the user can retry. After an explicit successful restart or in-process handoff request, the guard stays set because the app is leaving the launcher path. If the model returns without requesting either handoff and has no more specific failure, the coordinator overwrites the unified launch-attempt marker with `launch handoff not requested`, restores launch controls, and leaves that terminal blocker available for diagnostics. Duplicate taps record `launch request ignored` instead of rerunning readiness work or overwriting the launch-attempt marker, and that ignored-request evidence uses the active attempt context's selected branch rather than rereading selected-branch preferences while a launch is already in progress. Startup, launch-attempt, manual safe-launch, and auth-failure marker paths are resolved through a shared best-effort helper, so diagnostics can fail quietly and log their own path-resolution problem instead of becoming the cause of a startup failure.

Native fallback diagnostics also report `current_runtime_slot.json` state, including files-ready, playable, runtime-compatible, patch-compatible, marker/current PCK hash matching, marker/current source `sts2.dll` hash matching, readiness problem, runtime-pack usability, and patch-compatibility status. This keeps startup blockers visible even when Godot cannot safely start.

For non-public branches, native startup must not fall back to copying the selected-game `sts2.dll` directly into the Android publish cache when no usable runtime pack exists. That failure should stay visible as missing runtime-pack evidence instead of silently booting a potentially mismatched runtime.

When a non-public branch has no usable runtime pack, native runtime identity and `current_runtime_cache.txt` report `runtimeSource=no-usable-runtime` rather than `selected-game`. The same marker records `Selected branch requires runtime pack: true/false`, and launcher diagnostics plus the evidence collector surface that field. Cache-hit validation also rejects existing publish-cache game assemblies in that state, so stale selected-game `sts2.dll` files cannot be reused as if they were a matched Android runtime.

Branch marker field names, integrity provenance counters, Android app-private path alias comparisons, and runtime evidence marker prefixes are shared helpers rather than duplicated readiness/diagnostic strings. `LauncherBranchMarkerFields` owns marker labels such as branch, depot manifest, install slot kind, and selected/public depot comparison counters. `LauncherBranchMarkerIntegrityProvenance` parses the selected/public comparison counters and reports whether integrity provenance is complete. `LauncherAndroidAppPrivatePath` owns normalization and `/data/user/0/<package>` versus `/data/data/<package>` alias matching used by branch marker provenance and runtime cache identity checks. Runtime-cache, selected-version cache cleanup, save-origin, branch-switch safety, and manual cloud-sync evidence prefixes are centralized in their respective launcher partials so marker readers and writers stay aligned. Static audits cover these helper boundaries so selected-version launch gating cannot drift from diagnostics and evidence collection.

Runtime packs and runtime evidence are lifecycle-bound to their selected branch cache. Redownloading the selected version deletes the selected game directory, selected download state, and selected runtime-pack directory. It also clears `current_runtime_slot.json`, `current_runtime_cache.txt`, and `last_runtime_patch_validation.json` before fresh download/launch evidence is written. Clearing inactive cached versions also removes the matching inactive runtime-pack directories so old compatibility packs do not remain as hidden state after their source game cache is removed.

Actual startup also records runtime Harmony patch validation evidence:

```text
last_runtime_patch_validation.json
```

This marker captures the selected branch, selected PCK hash, selected source `sts2.dll` hash, active Android `sts2.dll` hash, runtime-pack ID/status, patch counts, failure messages, and whether startup passed, passed with non-critical failures, or hit a critical patch failure. It is post-launch evidence and does not replace the pre-launch readiness gate.

## Required architecture end state

### Runtime slots

Every installed branch should resolve to a `GameRuntimeSlot` with a stable identity:

```text
files/game                                  # public game files
files/game_versions/<branch>/game           # non-public game files
files/runtime_packs/<branch>/...            # Android-compatible game-code packs
files/runtime_cache/<slot-id>/publish/arm64 # prepared Mono/Godot runtime cache
```

The slot ID should include at least:

- normalized branch
- release version
- release commit
- Steam manifest ID
- PCK hash
- Android runtime `sts2.dll` hash
- patch-set version

### Compatibility packs

Steam provides branch-matched desktop files. Android needs a branch-matched Android-compatible game-code assembly.

A compatibility pack should contain:

- Android-compatible `sts2.dll`
- compatibility manifest
- patch validation report
- source Steam branch and manifest IDs
- source PCK hash and source desktop `sts2.dll` hash
- produced Android `sts2.dll` hash
- supported launcher/patch-set version

Example:

```text
runtime_packs/public/v0.103.3/compatibility.json
runtime_packs/public/v0.103.3/sts2.dll
runtime_packs/public-beta/v0.107.0/compatibility.json
runtime_packs/public-beta/v0.107.0/sts2.dll
```

### Assembly cache

The Android native startup cache should stop treating game-code assembly identity as only APK-version and branch-name state.

The cache should be keyed by a runtime slot:

```text
selected branch
release version
release commit
PCK hash
runtime pack ID
Android sts2.dll hash
patch-set version
```

Switching public to beta must switch both:

- mounted PCK
- active Android game-code assembly

### Patch validation

Before a branch is marked playable, validation evidence should confirm:

- required classes exist
- required methods exist
- method signatures match
- Harmony patches apply
- Android platform patches apply
- known startup and localization patches are compatible

If validation fails, the branch should remain installed but not playable.

### Save policy

Branch switching should preserve save safety:

- record save origin branch and version
- warn when using saves across branches
- keep Steam Cloud Push guarded by backup evidence
- avoid silently treating beta and public saves as equivalent

The launcher now records current Android local save origin in:

```text
current_android_save_origin.txt
```

Manual Pull writes the selected branch/version/runtime-slot ID/PCK/source-assembly hashes after cloud saves are copied to Android local storage. Branch switching writes a pending marker that explicitly marks local saves as not verified for the newly selected version until Pull runs again. Manual Push is blocked unless the save-origin marker is current for the selected branch, the currently selected runtime is playable, the recorded runtime slot ID matches the current selected runtime, the recorded PCK identity matches the current selected runtime, the recorded source `sts2.dll` hash matches the current selected runtime, and important Android local save evidence exists. For non-public Android-patched PCKs, PCK identity can match through the selected runtime-pack source-PCK mapping: the save-origin marker may record the source PCK hash while runtime/cache validation records the mounted patched PCK hash, but only when the runtime pack is usable and ties both hashes to the same runtime slot/source assembly. Matching a non-playable runtime identity is not enough to allow Push.

## Future validation workflow

When Steam publishes a new public or beta manifest:

1. Refresh Steam branch metadata.
2. Download the selected branch.
3. Build or fetch the matching Android compatibility pack.
4. Validate patch compatibility.
5. Build the runtime slot cache.
6. Smoke test launch, main menu, Neow reward, death screen, card/relic/bestiary routes.
7. Mark the slot playable only after PCK/code/patch validation passes.

Use the read-only collector after each meaningful branch update or runtime change:

```powershell
.\scripts\capture-multi-version-runtime-evidence.ps1 `
  -PackageName <installed.package.name> `
  -RunLabel public

.\scripts\capture-multi-version-runtime-evidence.ps1 `
  -PackageName <installed.package.name> `
  -RunLabel public-beta

.\scripts\capture-multi-version-runtime-evidence.ps1 `
  -PackageName <installed.package.name> `
  -RunLabel branch-switch
```

The collector creates `artifacts/android/multi-version-runtime-<label>-<timestamp>/` when `-RunLabel` is supplied, or `artifacts/android/multi-version-runtime-<timestamp>/` without a label. Use labels such as `public`, `public-beta`, and `branch-switch` so release-gate review can map artifacts to the correct branch expectation. Each artifact also includes `run-metadata.json`, so the label is machine-readable and not only encoded in the folder name. It captures:

- `run-metadata.json` with generated UTC time, package name, sanitized run label, artifact folder name, collector name, and read-only marker
- selected branch/runtime marker files
- installed runtime-slot evidence
- installed runtime-slot marker freshness against the selected PCK and source `sts2.dll` files on disk
- installed-slot/runtime-validation matching evidence for runtime slot ID, branch, PCK hash, and source `sts2.dll` hash
- `last_launch_attempt.txt` evidence for the Start Game orchestration path, including UTC timestamp, per-press attempt ID, prepared-readiness use, concrete readiness/mod cache status, measured launch/readiness/mod timings, selected branch, selected PCK hash, source and active Android `sts2.dll` hashes, runtime-pack usability, runtime-cache marker presence, runtime patch-validation marker presence, mod readiness phase, play mode, enabled mod count, selected mods, and modded-save Cloud Push lock state
- selected PCK and `sts2.dll` hashes
- runtime-pack manifests and patch-validation reports
- selected runtime-pack manifest/report copied to standalone diagnostics files
- prepared runtime-cache marker
- active Android publish-cache `sts2.dll` hash
- current Android save-origin evidence
- focused runtime/logcat lines
- a `summary.md` classification aid
- a `validation-report.md` readiness table that classifies runtime-slot marker freshness, runtime patch validation, prepared-cache matching, canonical runtime-slot binding to native cache identity, selected runtime-pack manifest/report matching including clean-directory provenance and closed runtime-pack DLL-set evidence, Steam Cloud Push save-origin safety including runtime playability and selected-runtime save verification, runtime source, and artifact hygiene
- a mixed/split asset hypothesis matrix covering Steam branch partial/shared content, stale/incomplete downloader cache, wrong launch path, shared assembly/runtime cache, in-process branch switch reuse, Android PCK patch side effects, Godot import/resource mismatch, and save/config asset reference mismatch. Matrix statuses use the fixed vocabulary: `confirmed`, `ruled out`, `likely`, `unknown`, and `needs device-only validation`. Direct contradictions between selected runtime evidence and cache/save markers are reported as `confirmed`; weaker branch-content inheritance signals remain `likely` until exact asset hashes are compared

The script is read-only and does not mutate Steam Cloud or app data. Treat full diagnostics/logcat as local-only until manually reviewed and redacted.

Review the captured artifact before using it as release evidence:

```powershell
.\scripts\review-multi-version-runtime-evidence.ps1 `
  -EvidenceDir artifacts\android\multi-version-runtime-public-beta-<timestamp> `
  -RequirePublicBeta `
  -RequireSaveSafety `
  -RequireLaunchAttempt `
  -RequireResolvedClassification
```

The review script is local-only and read-only. It checks that the artifact contains the runtime-slot marker, prepared runtime-cache marker, focused logcat, selected runtime-pack manifest/report, closed-DLL/runtime-pack classifications, fixed mixed/split asset hypothesis matrix, and Steam Cloud Push save-origin safety classification required by the release gates. Use `-RequireLaunchAttempt` when validating startup/refactor work so the artifact must include the explicit Start Game marker rather than only post-launch runtime cache state. Release signoff should use `-RequireResolvedClassification` so `unknown` or `needs device-only validation` classifications cannot be carried forward as completed evidence.

For Start Game refactor proof, `scripts/capture-launch-attempt-runtime-evidence.ps1` wraps the read-only collector and immediately runs `review-multi-version-runtime-evidence.ps1 -RequireLaunchAttempt`. It is intended to be run after the app has already reached the selected game from a manual or automated Start Game press; it does not press Start Game, install APKs, clear app data, upload Steam Cloud saves, or mutate device state. Use `-WaitForDeviceSeconds` for slower ADB reconnects and `-Quiet` when calling it from other release scripts. The wrapper defaults `-MaxLaunchAttemptAgeMinutes` to 30 so fresh Start Game proof cannot be satisfied by an older marker; generic artifact review defaults that option to 0 so archived evidence remains reviewable unless freshness is explicitly required. Branch expectations are mutually exclusive: use only one of `-RequirePublic`, `-RequirePublicBeta`, or `-RequireBranchSwitch`. Required launch-attempt evidence must show a successful handoff phase, not only pre-handoff readiness: `restart requested`, `safe android restart requested`, or `in-process signalled`. The collector and reviewer share the phase allow/reject lists through `scripts/evidence-launch-attempt-phases.ps1`, so capture summaries and release-gate validation cannot drift independently.

The launch-attempt marker is also the recovery record for failed Start Game handoff. If selected-version readiness passes but launcher-side mod readiness or the final launch bridge throws, the coordinator overwrites the marker with `mod readiness failed` or `launch handoff failed`, keeps the prepared runtime evidence attached, and surfaces the exception type/message in launcher status and diagnostics. In-process launch compares the process-loaded branch to the branch from prepared readiness rather than rereading branch preferences at handoff time, so the delivered signal and marker describe the same selected runtime evidence. It only writes `in-process signalled` after the current-process launch signal is actually delivered; if that signal cannot be delivered, the marker records `in-process signal failed` and the restart fallback path is requested. If the final bridge check finds readiness has been lost, the model returns `restart requested without ready files` as a failed handoff result and the coordinator records that blocker instead of restarting the app or replacing it with a generic no-op marker. If the bridge returns without requesting an in-process or restart handoff and has no more specific failure, the marker records `launch handoff not requested` and the launcher controls are restored. C# launch-attempt writers use `LauncherLaunchAttemptPhases` for these phase values, and the evidence collector/reviewer keep the success/reject lists in `scripts/evidence-launch-attempt-phases.ps1`. The evidence collector and reviewer reject failed phases, reject unmeasured timing placeholders, and also reject a marker that only reached `setup failed`, `checking`, `readiness failed`, or `ready`, so failed-setup, pre-readiness, failed-readiness, failed-signal, failed-handoff, unmeasured, or pre-handoff evidence cannot be mistaken for a completed launch path.

Previous-startup recovery reads the same launch-attempt marker. The marker writer and summary reader share one prefix table, then the marker is parsed once into `LaunchAttemptSummary` before the ready actions are shown, so recovery guidance does not turn launcher display into repeated marker I/O or duplicated schema strings. `LauncherLaunchMarkers.Attempt` owns marker writing and scalar reads, while `LaunchAttemptSummary` owns the compact recovery display lines. The recovery summary includes the marker UTC timestamp, per-press attempt ID, and compact selected PCK/source/active runtime hash identities, making stale or mismatched launch evidence visible in the launcher log. No-op handoff reports tell users to capture a Help Report before retrying because readiness finished but Android was not asked to start the game; failed handoff and failed in-process signal reports suggest Safe Start once, then Help Report if the bridge failure repeats.

Use the static audit before release or after changing this architecture:

```powershell
.\scripts\audit-multi-version-runtime.ps1
.\scripts\test-multi-version-runtime-evidence-reviewer.ps1
```

The audit checks that runtime-slot identity, readiness gates, runtime-pack manifests, runtime-pack generation, native assembly-cache selection, static patch validation, runtime patch evidence, save-origin safety, diagnostics, and this documentation remain wired together. The reviewer regression test compares `LauncherLaunchAttemptPhases.cs` with `scripts/evidence-launch-attempt-phases.ps1` so every C# marker phase is classified as either successful handoff proof or rejected proof, and every reviewer phase has a C# constant. It also compares the reviewer cache-status regexes with `LauncherLaunchReadinessCacheStatus.cs` and `LauncherModLaunchReadinessCacheStatus.cs`, so `last_launch_attempt.txt` cache status validation cannot drift from launcher marker output. It creates synthetic artifacts to prove `review-multi-version-runtime-evidence.ps1 -RequireLaunchAttempt` accepts matched fresh launch-attempt evidence, rejects stale, missing, unprepared, or unmeasured launch-attempt markers, rejects launch/runtime mismatch reports, and still accepts older artifacts when launch-attempt evidence is not required.

The audit is split into focused modules under `scripts/audit-multi-version-runtime.*.ps1`: helper/module boundaries, runtime slot contracts, runtime-pack contracts, patch compatibility, native cache evidence, startup patch contracts, save-origin/cloud safety, diagnostics, and evidence tooling/release docs. Keep new runtime evidence checks in the matching module so the top-level audit remains an orchestrator.

The read-only collector also keeps its device/JSON/hash/runtime-pack helper functions in `scripts/capture-multi-version-runtime-evidence.helpers.ps1`; the main collector should stay focused on capture ordering, validation rows, and summary output.

For release-gate runs, use the wrapper after static changes and again after device evidence exists:

```powershell
.\scripts\run-multi-version-runtime-release-gates.ps1

.\scripts\run-multi-version-runtime-release-gates.ps1 `
  -PublicEvidenceDirs artifacts\android\multi-version-runtime-public-<timestamp> `
  -PublicBetaEvidenceDirs artifacts\android\multi-version-runtime-public-beta-<timestamp> `
  -BranchSwitchEvidenceDirs artifacts\android\multi-version-runtime-branch-switch-<timestamp> `
  -RequireSaveSafety `
  -RequireLaunchAttempt `
  -MaxLaunchAttemptAgeMinutes 30 `
  -RequireResolvedClassification
```

The wrapper runs the multi-version runtime audit, Steam version-selection audits, launch-attempt evidence reviewer regression test, and optional local evidence review. Use `-PublicEvidenceDirs` for public/default evidence artifacts, `-PublicBetaEvidenceDirs` for beta evidence artifacts, and `-BranchSwitchEvidenceDirs` for artifacts captured after public -> beta -> public -> beta switching. Add `-RequireLaunchAttempt` when the evidence must prove the refactored Start Game path and readiness cache, not just final runtime/cache state. The older `-EvidenceDirs` plus `-RequirePublic`, `-RequirePublicBeta`, or `-RequireBranchSwitch` mode remains available for one-off reviews. It does not collect evidence, install APKs, mutate app data, or touch Steam Cloud.

Branch-switch evidence must include the `last_game_branch_switch.txt` marker and focused runtime/cache logs from the switching session. Do not use a normal single-branch launch artifact as branch-switch evidence.

Use [multi-version runtime release gates](multi-version-runtime-release-gates.md) before claiming release support for public/beta coexistence. That checklist defines the required static, build, device, runtime, save-safety, and hypothesis-classification evidence for the exact APK being released.

## Non-goals for this slice

This slice does not yet run full runtime Harmony validation before launch. It establishes the runtime-slot identity, evaluates runtime-pack manifests, writes static patch-validation evidence for selected non-public downloads, generates a branch-local runtime pack when static validation passes, records actual runtime Harmony patch validation after startup, requires explicit patch-validation evidence for non-public playability, and makes native startup refresh the Android game-code assembly cache when branch runtime identity changes.
