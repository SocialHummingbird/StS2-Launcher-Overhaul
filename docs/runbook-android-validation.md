# Android validation runbook

Updated: 2026-08-13

No device is currently connected. A limited `0.2.425` one-device run is recorded below; do not extend its result beyond the exact observed evidence or treat desktop results as physical-device proof.

## Current procedure

1. Record the source commit and working-tree state.
2. Build `src/STS2Mobile/STS2Mobile.csproj` in Release configuration.
3. Run `scripts/test-local-gameplay-save-safety.ps1`.
4. Run `scripts/test-launcher-ui-preview.ps1` with the explicit local fixture
   paths shown in
   [Focused development commands](steam-version-selection-tooling.md#launcher-navigation-and-mod-activation).
5. If an APK is required, build it with `scripts/build-android-local.ps1` and inspect it with `scripts/verify-android-apk.ps1`.
6. Record exact commands, exit codes, and failures.

The save suite covers local path containment, atomic writes, the four synchronization decisions, transactional Pull failure, retryable Push failure, pre-load ordering, gameplay Push queuing, and shared manual synchronization. Its fake remote is deterministic test infrastructure; it proves neither Steam nor Android transport. The launcher/mod test uses one desktop fixture and one interaction test; it does not prove Android mod activation or an in-game effect.

## Stage 9: one-device mod acceptance (incomplete)

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

Use the launcher, not desktop Steam. If either item is absent, use **Sync
Workshop Mods** once. Open **Mods**, choose **Modded**, disable every other mod,
and enable only BaseLib and Import Vanilla Saves. Before Play, require:

- `Play Modded · 2 enabled`
- `Next save set: Modded saves`
- both rows show installed and enabled for Modded

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
4. Reopen Mods. Require the same two enabled items, BaseLib `Partial`, importer
   `Loaded last launch`, and no stale-selection warning.
5. Stop the capture and retain only that one filtered log.

Stop and report failure on a crash, missing payload, missing Harmony target,
wrong aggregate, absent importer controls, non-`modded/` save access, `Failed` or
`Not tested yet` result, or changed selection. A pass proves only this exact
device, branch, APK, and mod chain; it is not broad compatibility evidence.

### Latest attempt

The retained `0.2.425` filtered log shows both selected mods discovered,
payload-loaded, initialized, and activated, the expected aggregate above, a real
`modded/` save path, and main-menu startup. The user observed marked improvement
and mods appearing to work. The importer control and matching relaunch result
were not captured, and a later freeze/crash was reported after the retained log
ended. The device has since been disconnected. This attempt is useful partial
evidence, not a Stage 9 pass; repeat the same single journey before making a
working or release-ready claim.
