# Android validation runbook

Updated: 2026-08-22

The exact `v0.2.429-issue38-arm64-rc4` device result is recorded below. Older deferred-preload and mod journeys remain specialized historical/incomplete procedures; they do not replace the current runtime/handoff validation or inherit RC4's result.

## Current procedure

1. Record the source commit and working-tree state.
2. Build `src/STS2Mobile/STS2Mobile.csproj` in Release configuration.
3. Run `scripts/test-local-gameplay-save-safety.ps1`.
4. Run `tests/STS2Mobile.GameIdentityTests` in Release configuration.
5. Run `android/gradlew.bat testReleaseUnitTest`.
6. Run `scripts/test-launcher-ui-preview.ps1` with the explicit local fixture
   paths shown in
   [Focused development commands](steam-version-selection-tooling.md#launcher-navigation-and-mod-activation).
7. If an APK is required, build it with `scripts/build-android-local.ps1` and inspect it with `scripts/verify-android-apk.ps1`.
8. Record exact commands, exit codes, and failures.

The save suite covers local path containment, atomic writes, the four synchronization decisions, transactional Pull failure, retryable Push failure, pre-load ordering, gameplay Push queuing, and shared manual synchronization. The Issue #38 suite covers authoritative identity, transactional N → N+1 updates, interruption recovery, atomic runtime-pack promotion, selected-branch isolation, save/credential preservation, and attempt-bound handoff ordering. Fake remotes and desktop fixtures prove neither live Steam transport nor physical Android behavior.

## v0.2.429 RC4 runtime and handoff result

Exact artifact:

```text
Release: v0.2.429-issue38-arm64-rc4
Asset: StS2Launcher-v0.2.429-issue38-arm64-rc4-arm64-v8a.apk
Commit: 559251309f68659102bcabbf12e669f9512e7776
Package: com.sts2launcher.overhaul.fork.local
VersionCode: 429041
SHA-256: b2d0e8154afc69e11eac4159483e837559e398844444a5d015d18f3fda458181
Device: Samsung SM-F971B
Android: 17 / API 37
ABI: arm64-v8a
```

The APK installed over the existing application with `adb install -r`. Ten counted attempts passed: cold ×4, warm ×4, background/resume ×1, and lock/unlock ×1. Every attempt had a unique ID; exactly one `main_menu_ready`, `overlay_hidden`, and `handoff_completed`; current identity/runtime-pack/patch validation; a visible foreground game; no remaining launcher overlay; and no stale completion.

The before/after preservation audit found local save inventories, Steam Cloud inventory, credentials, selected `public-beta` branch identity, and unrelated public branch/runtime data unchanged. This is the authoritative RC4 result. It does not prove the original Pixel 9/Xiaomi 17 Ultra flows, every branch/GPU/mod, live Steam transfer, or every update-interruption checkpoint.

## Historical Stage 3: deferred-preload A/B (not run by RC4)

Use only
`artifacts/android/StS2Launcher-v0.2.427-deferred-preload-experiment-arm64-v8a.apk`
(version code `427000`, SHA-256
`78889980818C16E311B42BCAA87E28EDF00377C2C7C61CD9328F209E04DABAFF`).
The APK is normal by default. It samples the Android global setting
`sts2_deferred_preload_experiment` once in `GodotApp.onCreate`; changing the
setting cannot alter a running process.

Before the comparison, select **Modded** and enable only **Import Vanilla
Saves**. Do not enable BaseLib: the importer declares no BaseLib dependency.
Keep the same device, branch, renderer, selection fingerprint, APK, and startup
path for both arms. Start one continuous filtered log before arm A and keep it
running through arm B.

```powershell
adb -s <serial> logcat -c
adb -s <serial> logcat -b main -b system -b crash -v threadtime STS2Mobile:I AndroidRuntime:E libc:F DEBUG:F ActivityManager:I '*:S' > artifacts/android/stage3-deferred-preload-ab.log
```

Arm A uses normal loading:

```powershell
adb -s <serial> shell am force-stop com.sts2launcher.overhaul.fork.local
adb -s <serial> shell settings delete global sts2_deferred_preload_experiment
adb -s <serial> shell log -p i -t STS2Mobile "STAGE3 ARM A BEGIN"
```

Arm B suppresses only the deferred startup invocation immediately following
`OneTimeInitialization.ExecuteDeferred()`:

```powershell
adb -s <serial> shell am force-stop com.sts2launcher.overhaul.fork.local
adb -s <serial> shell settings put global sts2_deferred_preload_experiment 1
adb -s <serial> shell log -p i -t STS2Mobile "STAGE3 ARM B BEGIN"
```

Launch the launcher and press the same contextual Play action after each
force-stop. In both fresh game processes require the same branch/runtime hashes,
selection-file SHA, importer-only activation, stable menu handoff, and heartbeat
sequence through 60 seconds. The log must show `ExecuteDeferred completed` in
both arms. It must show `call=1 action=normal` in A and
`call=1 action=suppressed` in B. Delete the global setting after arm B.

For each gameplay PID, require the native `Deferred preload experiment arm:`
and managed `[DeferredPreloadExperiment] installed arm=` lines to agree, plus
successful patch lines for both exact target methods. Importer-only proof is
`enabled=1`, selection of Import Vanilla Saves, its validated manifest/planned
load, and an activation aggregate of
`selected=1 discovered=1 payloadLoaded=1 initialized=1 activated=1 partial=0 failed=0`.
Any BaseLib planned-load line invalidates that arm.

After observing the first missing milestone or the 60-second heartbeat, write an
`ARM A END` or `ARM B END` operator boundary to the same log before the next
deliberate force-stop. This keeps an intentional stop distinct from a genuine
process exit.

```powershell
adb -s <serial> shell settings delete global sts2_deferred_preload_experiment
```

- A freezes at its first missing required milestone and B reaches 60 seconds: deferred
  common/menu loading is implicated, not proved as the ultimate cause.
- B freezes at the same boundary despite the suppression marker: that call is
  ruled out as a necessary cause.
- Both runs are healthy, or only B freezes: inconclusive. Do not manufacture a
  stability or causality claim from an intermittent non-reproduction.

Record a genuine process exit separately from a live-process freeze. Do not add
another collector, mod, device, branch, or renderer to this comparison.

## Historical Stage 9: one-device mod acceptance (incomplete)

This is the only authorized mod-loading device journey. Do not begin a renderer,
branch, mod, or device matrix until it passes.

### Pin the inputs

- APK: `artifacts/android/StS2Launcher-v0.2.425-mod-chain-fix2-unverified-arm64-v8a.apk`
- Package: `com.sts2launcher.overhaul.fork.local`
- Version: `0.2.425-mod-chain-fix2-unverified` (`425000`)
- APK SHA-256: `2A80E58A6301EFD0C6A0251FF9BC0887434071661DD8E002EF9CA89E25BEDA0B`
- Signer SHA-256: `FD0E3D5ACF435C1D23BFC5C426E99AA9EB5808619FF1FC214FFCA99CFAC7E57A`
- ABI: `arm64-v8a` only
- BaseLib: Workshop `3737335127`, manifest ID `BaseLib`
- Importer: Workshop `3747503308`, manifest ID `ImportVanillaSaves`

The importer does not depend on BaseLib. Both must be enabled explicitly.

### Safety preflight

1. Connect exactly one authorized device. Record its serial, model, Android
   version, and ABI; stop for zero, multiple, unauthorized, or offline targets.
2. Compare the installed package and signer with the candidate before an
   in-place update. Never uninstall, clear data, downgrade, or use `-d`.
3. Install only with `adb -s <serial> install -r <candidate>` after update
   compatibility is proven. Confirm the installed version is `425000`.
4. Record the one selected game branch and do not change it or the renderer
   during this journey. If no branch is selected yet, use `public`.
5. Proceed only if this device's existing local saves may safely be changed by
   normal gameplay and synchronization.

### Select the exact chain

Use the launcher, not desktop Steam. If either item is absent, use **Update
Workshop mods** once. Open **Mods**, choose **Modded**, disable every other mod,
and enable only BaseLib and Import Vanilla Saves. Before Play, require:

- `Play Modded · 2 mods`
- `Uses Modded saves · 2 mods enabled`
- both rows' `Enabled` toggles are on

### Retain one filtered log

In one dedicated terminal, start one continuous capture immediately before
pressing Play. Do not PID-filter it because Play and relaunch cross processes:

```powershell
adb -s <serial> logcat -c
adb -s <serial> logcat -v threadtime STS2Mobile:I AndroidRuntime:E libc:F DEBUG:F '*:S' > artifacts/android/stage9-one-device-filtered.log
```

Press Play once. The log must show both validated manifest IDs, both planned
payloads loading, BaseLib's Android-safe initializer completing, positive exact
Harmony targets for both owners, and this aggregate:

```text
selected=2 discovered=2 payloadLoaded=2 initialized=2 activated=2 partial=1 failed=0
```

### Direct observations

1. Reach profile selection and observe all three `Import Profile` controls.
   Do not press them or import/overwrite a profile.
2. Require at least one real `[Save] Android local save ...` line whose logical
   path begins `modded/`. The launcher label alone is not runtime proof.
3. Exit normally and let the launcher relaunch without changing the selection.
4. Reopen Mods. Require the same two enabled items, BaseLib `Partly loaded`,
   importer `Active last launch`, and no `Not run with this setup` result.
5. Stop the capture and retain only that one filtered log.

Stop and report failure on a crash, missing payload, missing Harmony target,
wrong aggregate, absent importer controls, non-`modded/` save access,
`Failed last launch` or `Not run with this setup` result, or changed selection.
A pass proves only this exact
device, branch, APK, and mod chain; it is not broad compatibility evidence.

### Latest attempt

The retained `0.2.425` filtered log shows both selected mods discovered,
payload-loaded, initialized, and activated, the expected aggregate above, a real
`modded/` save path, and main-menu startup. The user observed marked improvement
and mods appearing to work. The importer control and matching relaunch result
were not captured. The retained event is an intermittent live-process freeze
after the main menu appeared, not a proven process crash; the precise timeline
and first missing milestone are recorded in `current-android-status.md`. The
device has since been disconnected. This attempt is useful partial
evidence, not a Stage 9 pass; repeat the same single journey before making a
working or release-ready claim.
