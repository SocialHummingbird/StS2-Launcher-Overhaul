# Android runtime findings

## Current conclusion

The Android `x86_64` emulator is a useful target for Steam authentication and game-file download testing, but it is not a reliable target for launching the downloaded Godot/.NET game runtime.

Use an `arm64-v8a` Android device/build as the proof target for actual game launch.

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
- On Android `x86_64`, if any non-empty downloaded `SlayTheSpire2.pck` exists, `LauncherActivity` routes to `NativeFallbackActivity` instead of starting Godot.
- The fallback diagnostics report whether the PCK header has valid `GDPC` magic, so corrupt or partial downloads are visible without taking the unsafe Godot path.
- `NativeFallbackActivity` is plain Android UI, not Godot. It avoids the emulator crash path entirely.
- The native fallback shows and can copy diagnostics including:
  - app version and version code,
  - Android SDK,
  - device manufacturer/model,
  - supported ABIs,
  - downloaded PCK path,
  - downloaded PCK existence and byte size.

## Expected behavior by target

### Android x86_64 emulator

- Before game files are downloaded: use the launcher path for Steam login and download testing.
- After any non-empty game PCK is present: show the native x86 fallback screen instead of starting Godot.
- This is expected and intentional.

### Android arm64-v8a device

- Before game files are downloaded: show launcher UI.
- After game files are downloaded: start Godot/game runtime and apply mobile patches.
- This path still needs runtime proof on real ARM64 hardware.

## Local validation commands

Build ABI-specific local artifacts:

```powershell
.\scripts\build-android-local.ps1 -VersionName "0.2.0-local-x86" -VersionCode 200 -Abi x86_64
.\scripts\build-android-local.ps1 -VersionName "0.2.0-local-arm64" -VersionCode 201 -Abi arm64-v8a
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

## Missing proof

- ARM64 device run from clean app data through Steam auth.
- ARM64 download of game files.
- ARM64 launch of downloaded game PCK.
- Confirmation that Harmony/mobile patches apply successfully on ARM64 without the Android `x86_64` runtime crash signature.
