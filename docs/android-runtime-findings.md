# Android runtime findings

## Current conclusion

The Android `x86_64` emulator is useful for install, routing, release packaging, and native diagnostic-screen validation, but it is not a reliable target for the Godot/.NET launcher or downloaded game runtime.

Use an `arm64-v8a` Android device/build as the proof target for actual game launch.

The latest published refactor APK has passed GitHub release build and structural asset verification. Local ARM64 validation has now proven the working launcher path through fresh runtime install, Steam game download, Pull from Cloud, Android local save handoff, and game launch with the pulled profile visible in-game.

This is still a hardening state, not a finished release-candidate signoff. Push to Cloud, upgrade install behavior, locked-screen interruption, stale assembly cache behavior, and repeated release-readiness coverage remain open validation gates.

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
- `LauncherActivity` starts `GodotApp` for normal operation.
- On Android `x86_64`, `LauncherActivity` routes to `NativeFallbackActivity` instead of starting Godot.
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
- This validates APK installability, ABI routing, package metadata, and native diagnostic UI on a visible emulator.
- It does not validate Steam login, download, or Godot/.NET launcher runtime.
- This is expected and intentional.
- If `sts2_force_godot_x86=1` is used to bypass the fallback, crashes in the forced Godot path are expected diagnostic evidence, not a release blocker by themselves.

### Android arm64-v8a device

- Before game files are downloaded: show launcher UI.
- After game files are downloaded: start Godot/game runtime and apply mobile patches.
- This path now has local ARM64 proof through authenticated game download, Pull from Cloud, Android local save handoff, and normal game launch.
- Treat the app as working but still under polish/hardening until Push and release-readiness gates are complete.

## Local validation commands

The current published APK release is:

- Release: `v0.2.175-refactor-apk`
- Asset: `StS2Launcher-v0.2.175-refactor-apk-arm64-v8a.apk`
- Release URL: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/tag/v0.2.175-refactor-apk
- SHA-256: `78f40ad39d6cab30af4178fba6fcee713ae8df54db20d4e3c9f8a5e225b1d097`

Before installing, verify the uploaded GitHub release asset itself:

```powershell
.\scripts\verify-android-release-apk.ps1 `
  -ReleaseTag "v0.2.175-refactor-apk" `
  -AssetName "StS2Launcher-v0.2.175-refactor-apk-arm64-v8a.apk" `
  -Abi arm64-v8a
```

Expected result:

```text
Release digest OK: 78f40ad39d6cab30af4178fba6fcee713ae8df54db20d4e3c9f8a5e225b1d097
Release APK verification passed: v0.2.175-refactor-apk/StS2Launcher-v0.2.175-refactor-apk-arm64-v8a.apk
Verified ABIs: arm64-v8a
```

Install the verified release APK to a connected phone and capture diagnostics in one run:

```powershell
.\scripts\install-android-release.ps1 `
  -ReleaseTag "v0.2.175-refactor-apk" `
  -AssetName "StS2Launcher-v0.2.175-refactor-apk-arm64-v8a.apk" `
  -ClearAppData `
  -Launch `
  -CaptureDiagnostics
```

If the app is already installed and you only need fresh diagnostics:

```powershell
.\scripts\capture-android-diagnostics.ps1 -Launch -ClearLogcat -WaitSeconds 15
```

The diagnostic capture writes a timestamped `artifacts/android/phone-diagnostics-*` directory with device metadata, full logcat, filtered logcat, package state, and app-private assembly cache listings when `run-as` is available.

For phone installs, use an APK that matches the device ABI. The current public refactor APK is ARM64-only. Older `x86_64` APKs are emulator-only and will be incompatible with ARM64 phones.

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

- Confirmed Push to Cloud upload behavior, including Steam Cloud metadata/file mutation after explicit user approval.
- Cancel/no-confirm Push safety evidence showing no upload starts without confirmation.
- Upgrade install evidence showing package `lastUpdateTime` advances and stale app-private assembly cache behavior does not recur.
- Locked-screen interruption behavior showing Android focus loss does not get misclassified as a game crash.
- Repeated release-readiness pass covering fresh install, upgrade install, Pull, Push, game launch, and diagnostics.
