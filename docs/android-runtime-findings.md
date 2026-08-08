# Android runtime findings

Current posture: the app works on the validated ARM64 path and is in polish/hardening. Treat this document as runtime findings supporting [current Android status](current-android-status.md), not as a claim that release-candidate validation is complete.

## Current conclusion

The Android `x86_64` emulator is useful for install, native routing, presentation continuity into fallback, recovery input, forced bootstrap failures, cache preservation, rotation, Home/resume, and native diagnostic-screen validation. It is not a reliable target for the Godot/.NET launcher or downloaded game runtime.

Use an `arm64-v8a` Android device/build as the proof target for actual game launch.

The latest published APK has passed structural asset inspection and update-compatibility verification against `v0.2.412`. ARM64 validation has proven the launcher path through runtime install, Steam game download, Pull from Cloud, Android local save handoff, and game launch with the pulled profile visible in-game. Exact `v0.2.416` hardware evidence additionally proves manifest-matched patched runtime-pack promotion, real public `NMainMenu`, 60-second post-startup stability, launcher IME suppression, and installed-artifact hash equality.

This is still a hardening state, not a finished release-candidate signoff. Newest-public-release Pull/confirmed-Push/game-launch smoke, persisted Steam-session/update UX, Samsung reporter retests if fresh reports arrive, stale assembly cache behavior, Workshop/mod compatibility polish, and repeated release-readiness coverage remain open validation gates.

Current source includes post-`v0.2.416` native first-frame, splash, task-routing, and fallback-recovery corrections validated on an API 36 x86_64 emulator. They are unreleased and have not yet been validated on ARM64 hardware.

## Evidence classification

| Target | Proven | Boundary |
| --- | --- | --- |
| Automated checks | Managed and Java behavior, Android assembly, APK content/crypto, fixture and fake-store safety | No physical rendering, OEM, Steam-service, or gameplay claim |
| API 36 x86_64 emulator | Native cold/cached routing, splash-to-fallback continuity, fallback controls/recovery, forced preparation failure and retry, cache preservation, orientation, Home/resume, native IME state | Production x86_64 cannot run the managed launcher or game |
| Exact published `v0.2.416` on Samsung ARM64 | Full cold managed transition, launcher, IME suppression, public runtime-pack promotion, real `NMainMenu`, 60-second heartbeat | Does not prove the unreleased routing changes, reporter devices, broad compatibility, mods/branches, or real Push |

## Evidence so far

- `STS2Mobile.dll` can be loaded from the custom Godot/Mono bootstrap.
- Native delegates into `STS2Mobile` can be created.
- `BootstrapProbe()` returns.
- Replacing the stock GodotSharp initializer with `STS2Mobile.InitializeGodotSharp` broke C# script discovery, so the stock initializer must remain in control.
- On Android `x86_64`, `new Harmony(HarmonyId)` crashes inside the Godot/Mono runtime path before normal launcher recovery is possible.
- Skipping Harmony on Android `x86_64` let the app reach deeper GodotSharp/game startup paths, but it still crashed in the same native runtime class of failure.
- The recurring emulator crash signature included native runtime failures such as static-string/runtime lifetime issues and destroyed mutex access.

## Current implementation

- `LauncherActivity` is the exported Android launcher activity.
- `LauncherActivity` prepares Android assemblies and starts `GodotApp` exactly once for normal ARM64 operation.
- On Android `x86_64`, `LauncherActivity` routes to `NativeFallbackActivity` instead of starting Godot.
- Current unreleased source retains the native splash until first routed content draw, keeps all three activities `singleTop`, and uses a dedicated fallback splash theme to avoid compositor gaps during native routing.
- Current unreleased fallback recovery stays in the existing task and returns once through `LauncherActivity` with the boot-transition skip extra.
- The fallback diagnostics report whether the PCK header has valid `GDPC` magic, so corrupt or partial downloads are visible without taking the unsafe Godot path.
- `NativeFallbackActivity` is plain Android UI, not Godot. It avoids the emulator crash path entirely.
- The native fallback shows and can copy diagnostics including:
  - app version and version code,
  - Android SDK,
  - device manufacturer/model,
  - supported ABIs,
  - downloaded PCK path,
  - downloaded PCK existence and byte size.
- Android Godot/.NET assemblies are copied by Java to `files/.godot/mono/publish/<arch>` before native Godot starts.
- The patched Godot engine now looks for Android .NET assemblies in that same app-private data path instead of `res://.godot/mono/publish/<arch>`.
- Local builds and release APKs verify that `libgodot_android.so` contains the app-data assembly lookup marker and not the stale PCK lookup marker.
- If Java cannot prepare the assembly cache after one recovery attempt, the app routes to `NativeFallbackActivity` with copyable diagnostics instead of continuing into the generic native `.NET assemblies not found` alert.

## Expected behavior by target

### Android x86_64 emulator

- Always show the native x86 fallback screen instead of starting Godot.
- The Stage 5 API 36 pass validates APK installability, ABI routing, package metadata, cold/cached presentation continuity, diagnostic controls, exact-once recovery, forced bootstrap failure and retry, active-cache preservation, rotation, Home/Recents resume, and native-path IME state on a visible emulator.
- It does not validate Steam login, download, or Godot/.NET launcher runtime.
- This is expected and intentional.
- If `sts2_force_godot_x86=1` is used to bypass the fallback, crashes in the forced Godot path are expected diagnostic evidence, not a release blocker by themselves.

### Android arm64-v8a device

- Before game files are downloaded: show launcher UI.
- After game files are downloaded: start Godot/game runtime and apply mobile patches.
- This path now has local ARM64 proof through authenticated game download, Pull from Cloud, Android local save handoff, and normal game launch.
- Treat the app as working but still under polish/hardening until Push and release-readiness gates are complete.

## Local validation commands

The current published tester APK is:

- Release: `v0.2.416-startup-recovery-ime`
- Asset: `StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk`
- Release URL: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/tag/v0.2.416-startup-recovery-ime
- SHA-256: `fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b`

This is the current local test-package/signing-channel release. It is not production-signer or broad device-compatibility signoff.

Before installing, verify the uploaded GitHub release asset itself:

```powershell
.\scripts\verify-android-release-apk.ps1 `
  -ReleaseTag "v0.2.416-startup-recovery-ime" `
  -AssetName "StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk" `
  -Abi arm64-v8a
```

Expected result:

```text
Release digest OK: fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b
Release APK verification passed: v0.2.416-startup-recovery-ime/StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk
Verified ABIs: arm64-v8a
```

Install the verified release APK only after verifying a byte-for-byte save export, or on a disposable device with no preservation data. Do not clear or automatically launch an existing tester `.local` install:

```powershell
.\scripts\install-android-release.ps1 `
  -ReleaseTag "v0.2.416-startup-recovery-ime" `
  -AssetName "StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk"
```

If the app is already installed and you only need fresh diagnostics:

```powershell
.\scripts\capture-android-diagnostics.ps1 -Launch -ClearLogcat -WaitSeconds 15
```

The diagnostic capture writes a timestamped `artifacts/android/phone-diagnostics-*` directory with device metadata, full logcat, filtered logcat, package state, and app-private assembly cache listings when `run-as` is available.

For phone installs, use an APK that matches the device ABI. The current public APK is ARM64-only. Older `x86_64` APKs are emulator-only and will be incompatible with ARM64 phones.

Build ABI-specific local artifacts:

```powershell
.\scripts\build-android-local.ps1 -VersionName "0.2.0-local-x86" -VersionCode 200 -Abi x86_64
.\scripts\build-android-local.ps1 -VersionName "0.2.0-local-arm64" -VersionCode 201 -Abi arm64-v8a
.\scripts\build-android-local.ps1 -VersionName "0.2.0-local-universal" -VersionCode 202 -Abi universal
```

Run the smoke test once ADB sees a device:

```powershell
.\scripts\test-android-local.ps1 -WaitForDeviceSeconds 60
```

If multiple devices are attached:

```powershell
.\scripts\test-android-local.ps1 -DeviceSerial emulator-5554
```

The smoke test writes:

- `artifacts/android/logcat-smoke-*-summary.txt`
- `artifacts/android/logcat-smoke-*-filtered.txt`
- `artifacts/android/logcat-smoke-*-full.txt`

## Remaining proof

- Build and install an exact ARM64 candidate containing the current unreleased Stage 5 native routing changes. Recheck cold/cached startup, full managed transition, launcher rendering/input, fallback/recovery, rotation, Home/resume, lock return, and real public game startup through `NMainMenu`.
- Repeat confirmed Push to Cloud behavior on exact `v0.2.416`, including Steam Cloud metadata/file mutation only after explicit user approval and controlled backups. Do not infer this from fake-store or older confirmation/cancel evidence.
- Keep cancel/no-confirm Push safety evidence in every release-candidate pass.
- PowerVR issue #34 is reporter-confirmed resolved through the Auto/OpenGL compatibility route. Continue collecting `graphics_device.txt`, requested/effective renderer, touch response, cold-menu timing, and `last_process_exit_info.txt` for new PowerVR devices rather than treating one device as broad signoff.
- Retest Xiaomi issue #36 and Odin cloud issue #35 on their reporter devices. Connected Samsung evidence does not close either report.
- Upgrade install evidence showing package `lastUpdateTime` advances and stale app-private assembly cache behavior does not recur.
- Locked-screen interruption behavior showing Android focus loss does not get misclassified as a game crash.
- Repeated release-readiness pass covering fresh install, upgrade install, Pull, Push, game launch, and diagnostics.
