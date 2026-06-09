# Current Android Status

_Last updated: 2026-06-09_

Current device evidence ledgers:

- [android-device-validation-20260608.md](android-device-validation-20260608.md)
- [android-cloud-save-validation-20260609.md](android-cloud-save-validation-20260609.md)

## Headline

The app now works on the validated ARM64 Android path, but it is still in polish and hardening rather than release-candidate signoff.

Validated locally on ARM64 hardware:

- Fresh APK/runtime install reaches the launcher.
- The latest public release APK verifies structurally; latest launch proof remains the previous public APK plus local ARM64 hardening builds until clean public-release smoke is rerun.
- Public release upgrade from `v0.2.175-refactor-apk` to `v0.2.177-login-a8729d6` preserves install state and advances `versionCode` from `217500` to `217700`; the newer `v0.2.178-cloudpush-icon` release is published and structurally verified at `versionCode=217800`.
- Locked-screen interruption returns to the app after manual unlock without app-specific crash markers.
- Steam login and game depot download complete.
- Pull from Cloud downloads real Steam Cloud files.
- Push to Cloud completes on the local hardening build without post-Push process death.
- Pull after Push downloads and writes the pushed cloud state back to Android local storage.
- Android local save handoff works.
- The downloaded game launches and shows the pulled `Profile 1` in-game.
- Force-stop/relaunch returns to the launcher with saved Steam credentials available.

## Latest hardening evidence

Latest public release evidence:

```text
release=v0.2.178-cloudpush-icon
asset=StS2Launcher-v0.2.178-cloudpush-icon-arm64-v8a.apk
sha256=5f8c04ad6602494f84ade6165180e18177c54c3908fe2de1cbc5ddf8cb4fd076
package=com.sts2launcher.overhaul.fork.dev
versionName=0.2.178-cloudpush-icon
versionCode=217800
upgradeBaseline=v0.2.175-refactor-apk / versionCode=217500
```

Latest device evidence folders:

- `artifacts/android/github-release-v0.2.178-cloudpush-icon`
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

Pull from Cloud and Push to Cloud are now validated end to end on the local ARM64 hardening path. Steam Cloud files were enumerated/downloaded, Android local save files were written, Push uploaded/flushed the local save batch without process death, and Pull after Push wrote the cloud state back into Android app storage.

- The Push confirmation gate appears before upload.
- Direct Cancel returns to the launcher without upload.
- Back/no-confirm dismissal returns without upload.
- The latest warning text names Steam Cloud overwrite risk in code and documentation.
- Confirmed Push can overwrite real Steam Cloud state; keep it treated as an explicit destructive action.
- The 2026-06-09 Push crash was fixed by replacing Android native `SHA1.HashData` in the cloud upload file-hash path with managed SHA-1.
- Push evidence: `artifacts/android/push-managedsha1-observation-20260609-1`.
- Current-build manual Push evidence: `artifacts/android/push-clean3-observation-20260609-1`.
- Pull-after-Push evidence: `artifacts/android/pull-after-push-observation-20260609-1`.
- Preferred local evidence summaries: `summary-scrubbed.md` inside each 2026-06-09 Push/Pull artifact folder.
- Current launcher Push/Pull UI status text now names direction explicitly: Push makes Steam Cloud reflect Android local saves, and Pull makes Android local saves reflect Steam Cloud.
- Push confirmation now tells testers to Pull first and verify Android local saves exist before pushing, because Push can overwrite Steam Cloud state.
- Save discovery now skips app runtime/cache trees such as `.godot`, `cache`, `game`, and `tmp` during fallback enumeration so cloud-save diagnostics stay focused on save candidates.

## Remaining release-readiness blockers

- Re-run launch/login/Pull/Push confirmation/cancel smoke on the clean public `v0.2.178-cloudpush-icon` release-facing build.
- Keep Push treated as destructive even though local clean3 manual Push confirmation is validated; release-facing smoke still needs to confirm the same behavior on the public APK.
- Repeated local stale assembly cache/freshness checks across in-place local upgrade once signing continuity is restored.
- Repeat release asset hygiene on every new release: signer, package name, versionCode monotonicity, checksums, structural verifier, and GitHub release notes.
- Further diagnostics polish so normal successful startup/cloud-save behavior is not hidden by remaining low-value platform logs.

## Device-independent polish completed after baseline proof

- Local smoke/login/verification scripts now select APKs by parsed package metadata and versionCode, with ABI/package compatibility checks where applicable, instead of relying only on file write time.
- Push-to-Cloud warning text now names Steam Cloud explicitly, calls out overwrite risk before upload, and directs testers to Pull first and verify Android local saves exist before pushing.
- Manual cloud-sync start/complete/failure status updates now keep the launcher header aligned with the operation result instead of leaving stale generic status text behind.
- Recovery cleanup logging now describes normal post-startup cleanup as success-path UI cleanup.
- Diagnostics filters retain startup freshness, assembly cache, expectedSource/expectedBytes, cloud sync, and crash evidence while reducing broad log noise.

## Static upgrade/cache freshness review

The Android activity cache path has a usable static evidence chain for upgrade/freshness diagnosis:

- Startup freshness logs package, versionName/versionCode, schema, stored schema, stored versionCode, stored package, runtime arch, cache existence, and `STS2Mobile.dll` bytes.
- Assembly setup logs `cache-hit` versus full recopy and emits `expectedSource`/`expectedBytes` for required managed assemblies.
- Public package runtime proof exists for `v0.2.175 -> v0.2.177`; the remaining gap is local-package in-place upgrade proof after `.local` signing continuity is restored.

## Emulator limitation

ARM64 hardware is the proof target for Steam login, download, cloud sync, and game launch. Android `x86_64` emulator coverage is useful for install/routing/native-fallback diagnostics only; forcing Godot on `x86_64` remains a crash-prone diagnostic path, not release proof.
