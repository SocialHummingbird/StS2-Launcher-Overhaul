# Current Android Status

_Last updated: 2026-06-08_

Current device evidence ledger: [android-device-validation-20260608.md](android-device-validation-20260608.md).

## Headline

The app now works on the validated ARM64 Android path, but it is still in polish and hardening rather than release-candidate signoff.

Validated locally on ARM64 hardware:

- Fresh APK/runtime install reaches the launcher.
- The latest public release APK verifies structurally and launches.
- Public release upgrade from `v0.2.175-refactor-apk` to `v0.2.177-login-a8729d6` preserves install state and advances `versionCode` from `217500` to `217700`.
- Locked-screen interruption returns to the app after manual unlock without app-specific crash markers.
- Steam login and game depot download complete.
- Pull from Cloud downloads real Steam Cloud files.
- Android local save handoff works.
- The downloaded game launches and shows the pulled `Profile 1` in-game.
- Force-stop/relaunch returns to the launcher with saved Steam credentials available.

## Latest hardening evidence

Latest public release evidence:

```text
release=v0.2.177-login-a8729d6
asset=StS2Launcher-v0.2.177-login-a8729d6-arm64-v8a.apk
sha256=bde43591aeb6904488560bb1e27421276cc3248bbc7d2eb9151e29b8b9fef199
package=com.sts2launcher.overhaul.fork.dev
versionName=0.2.177-login-a8729d6
versionCode=217700
upgradeBaseline=v0.2.175-refactor-apk / versionCode=217500
```

Latest device evidence folders:

- `artifacts/android/github-release-v0.2.177-login-a8729d6`
- `artifacts/android/phone-diagnostics-20260608-220359`
- `artifacts/android/lock-unlock-validation-20260608-215548`
- `artifacts/android/local-pull-smoke-20260608-221143`
- `artifacts/android/local-start-game-dpad-20260608`
- `artifacts/android/local-game-profile-center-20260608`
- `artifacts/android/local-restart-diagnostics-20260608`

The latest local hardening build proved that the phone was running the freshly installed runtime and managed assemblies:

```text
versionName=0.2.0-local-hardening-freshness2-20260608
versionCode=2260810
schema=22
storedSchema=22
storedVersionCode=2260810
arch=arm64
sts2MobileBytes=622080
```

The startup freshness probe and assembly cache diagnostics now report the installed package/version/schema, cache presence, `STS2Mobile.dll` size, and expected source/byte counts for required assemblies. This addresses the previous ambiguity where the phone could appear to be running a newer APK while still using stale cached managed assemblies.

## Cloud-save posture

Pull from Cloud is validated end to end. Steam Cloud files were enumerated and downloaded, Android local save files were written, and the game read the pulled profile from Android app storage.

Push to Cloud is partially hardened but not release-ready:

- The Push confirmation gate appears before upload.
- Direct Cancel returns to the launcher without upload.
- Back/no-confirm dismissal returns without upload.
- The latest warning text names Steam Cloud overwrite risk in code and documentation.
- Confirmed Push upload has intentionally not been executed in the current hardening pass because it can overwrite real Steam Cloud state.
- The current `.local` app cannot be updated in-place to the newest local warning build because the original local test signing key is unavailable; the recreated test keystore has a different certificate, and `run-as` is unavailable for safe private save mutation/inspection.

Required future Push evidence:

- Use a controlled local save mutation.
- Confirm the user approval dialog appears before upload.
- Confirm upload mutates the expected Steam Cloud file/metadata.
- Pull again and prove the pushed state round-trips back to Android local storage.
- Reconfirm cancel/no-confirm paths produce no upload markers.

## Remaining release-readiness blockers

- Confirmed Push to Cloud upload and round-trip evidence.
- Safe controlled local save mutation/inspection path for Push evidence, either by restoring the original `.local` signing key, intentionally resetting `.local` app data, or using another controlled test account/state.
- Repeated local stale assembly cache/freshness checks across in-place local upgrade once signing continuity is restored.
- Release asset hygiene on every new release: signer, package name, versionCode monotonicity, checksums, structural verifier, and GitHub release notes.
- Diagnostics polish so normal successful startup/cloud-save behavior is not hidden by noisy low-value logs.

## Device-independent polish completed after baseline proof

- Local smoke/login/verification scripts now select APKs by parsed package metadata and versionCode, with ABI/package compatibility checks where applicable, instead of relying only on file write time.
- Push-to-Cloud warning text now names Steam Cloud explicitly and calls out overwrite risk before upload.
- Recovery cleanup logging now describes normal post-startup cleanup as success-path UI cleanup.
- Diagnostics filters retain startup freshness, assembly cache, expectedSource/expectedBytes, cloud sync, and crash evidence while reducing broad log noise.

## Static upgrade/cache freshness review

The Android activity cache path has a usable static evidence chain for upgrade/freshness diagnosis:

- Startup freshness logs package, versionName/versionCode, schema, stored schema, stored versionCode, stored package, runtime arch, cache existence, and `STS2Mobile.dll` bytes.
- Assembly setup logs `cache-hit` versus full recopy and emits `expectedSource`/`expectedBytes` for required managed assemblies.
- Public package runtime proof exists for `v0.2.175 -> v0.2.177`; the remaining gap is local-package in-place upgrade proof after `.local` signing continuity is restored.

## Emulator limitation

ARM64 hardware is the proof target for Steam login, download, cloud sync, and game launch. Android `x86_64` emulator coverage is useful for install/routing/native-fallback diagnostics only; forcing Godot on `x86_64` remains a crash-prone diagnostic path, not release proof.
