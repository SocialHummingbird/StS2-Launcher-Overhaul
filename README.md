# StS2 Launcher Overhaul

## Project note (independent copy)

This repository is a full copy of [Ekyso/StS2-Launcher](https://github.com/Ekyso/StS2-Launcher), created to continue and broaden development as a focused rewrite project.
The goal is a **drastic architecture and reliability overhaul** that is harder to do in the upstream repo while maintaining compatibility and delivering incremental improvements back when possible.

## Migration and governance

- Migration checklist: [MIGRATION_CHECKLIST.md](MIGRATION_CHECKLIST.md)
- Overhaul plan: [OVERHAUL_ROADMAP.md](OVERHAUL_ROADMAP.md)
- Contribution process: [CONTRIBUTING.md](CONTRIBUTING.md)
- Overhaul status: [OVERHAUL_STATUS.md](OVERHAUL_STATUS.md)
- Changelog: [CHANGELOG.md](CHANGELOG.md)
- Device log checklist: [docs/device-log-checklist.md](docs/device-log-checklist.md)
- Android runtime findings: [docs/android-runtime-findings.md](docs/android-runtime-findings.md)

### Suggested working remotes

```bash
git remote rename origin upstream
git remote add origin https://github.com/SocialHummingbird/StS2-Launcher-Overhaul.git
git remote set-url origin https://github.com/SocialHummingbird/StS2-Launcher-Overhaul.git
git fetch origin
git fetch upstream
```

An Android launcher for Slay the Spire 2, built on a custom Godot 4.5.1 engine with .NET/Mono and Harmony runtime patching.

> **Disclaimer**: This is an unofficial community project. Slay the Spire 2 is developed and published by Mega Crit Games. A valid Steam account that owns Slay the Spire 2 is required. Game files are downloaded directly from Steam after authentication. No game assets are included in this repository.

## Features

- **Steam authentication**  
  Login via SteamKit2 with Steam Guard 2FA support.
- **Game file download**  
  Depot download directly from Steam, with update checking.
- **Cloud saves**  
  Full Steam cloud sync via SteamKit2's CCloud API, with timestamp-aware conflict resolution and non-blocking background uploads.
- **Mobile adaptation**  
  Touch input, UI scaling, layout adjustments, and app lifecycle handling via Harmony runtime patches.
- **LAN multiplayer**  
  UDP broadcast discovery and manual IP join.
- **Shader warmup**  
  Vulkan pipeline cache persistence and canvas ubershader support to eliminate first-encounter stutters.
- **Credential security**  
  Steam refresh tokens encrypted at rest via Android Keystore (AES-256-GCM, hardware-backed TEE).

## How It Works

At startup, `STS2Mobile.dll` is loaded via `coreclr_create_delegate` and applies [Harmony](https://github.com/pardeike/Harmony) patches to adapt the desktop game for mobile. The launcher intercepts `GameStartupWrapper()` to present a Steam login screen before the game starts.

- **Launcher-only mode**  
If no game files are present, the app loads a minimal `bootstrap.pck` and shows the launcher UI for Steam login and game download.  
- **Normal mode**  
With game files downloaded, all patches apply against `sts2.dll` and the game runs natively after authentication.

## Engine Patches

Custom patches to the Godot 4.5.1 engine source for Android-specific issues:

- **Vulkan pipeline cache persistence**  
Saves compiled pipelines when the app loses focus, preventing recompilation after Android kills the process.
- **Canvas ubershaders**  
Enable ubershader fallback for 2D rendering, eliminating first-encounter VFX stutters from blocking pipeline compilation.

## Project Structure

```
src/STS2Mobile/
  ModEntry.cs              # Entry point ([UnmanagedCallersOnly] Apply())
  PatchHelper.cs           # Shared patch utility + logging
  Patches/                 # Harmony patches (one file per concern)
  Launcher/                # Programmatic Godot UI (MVC)
  Steam/                   # SteamKit2 login, depot download, cloud saves
android/                   # Godot Android gradle project
  src/.../GodotApp.java    # Activity, assembly setup, Keystore encryption
  assets/bootstrap.pck     # Minimal PCK for launcher-only mode
src/stubs/                 # Native library stubs (Steam API, Sentry)
scripts/                   # Build and tooling scripts
```

## Prerequisites

- .NET 9 SDK
- Android SDK + NDK (see `android/config.gradle` for versions)
- Python 3 (for `make-bootstrap-pck.py` and SCons)
- Original game files in `upstream/godot-export/`
- Custom Godot engine build (see `scripts/build-godot.sh`)
- FMOD SDK in `vendor/fmod-sdk/`

## Building

**Note: This is a WIP. There are other binaries that are required and will fail if you just run the `./build.sh` script. Godot Engine can be found on their repo https://github.com/godotengine/godot. Harmony can be found here https://github.com/Ekyso/Harmony but the version used in StS2 Launcher is compiled using dotnet 9.0. FMOD can be found here https://www.fmod.com/. Spine can be found here https://esotericsoftware.com/. I plan to upload the custom fork of Godot Engine used and the dotnet 9.0 Harmony soon. However, Spine and FMOD will not be uploaded due to licensing restrictions. Information on licensing can be found in the [THIRD-PARTY-NOTICES.txt](https://github.com/Ekyso/StS2-Launcher/blob/main/THIRD_PARTY_LICENSES.md) of the root folder.** 

```bash
bash scripts/build.sh
```

This runs the full pipeline:
1. `dotnet publish` the patcher (outputs `STS2Mobile.dll` + SteamKit2 dependencies)
2. Copies published DLLs to `android/assets/dotnet_bcl/`
3. Copies `libSystem.Security.Cryptography.Native.Android.so` to JNI libs (for TLS)
4. Bumps the version in `gradle.properties`
5. Builds the APK via `./gradlew assembleMonoRelease`

Output: `android/build/outputs/apk/mono/release/StS2Launcher-v<version>.apk`

### Local Android ABI builds

For local Android testing, use the PowerShell build wrapper so the managed assemblies, patched SteamKit dependency, Mono Android runtime libraries, and native ABI selection stay in sync:

```powershell
.\scripts\build-android-local.ps1 -VersionName "0.2.0-local-x86" -VersionCode 200 -Abi x86_64
.\scripts\build-android-local.ps1 -VersionName "0.2.0-local-arm64" -VersionCode 201 -Abi arm64-v8a
.\scripts\build-android-local.ps1 -VersionName "0.2.0-local-universal" -VersionCode 202 -Abi universal
```

The Gradle output directory only keeps the most recent mono release APK. The wrapper also archives every local build to `artifacts/android/` with the ABI in the filename, for example:

```text
artifacts/android/StS2Launcher-v0.2.0-local-x86-x86_64.apk
artifacts/android/StS2Launcher-v0.2.0-local-x86-x86_64.apk.sha256
artifacts/android/StS2Launcher-v0.2.0-local-arm64-arm64-v8a.apk
artifacts/android/StS2Launcher-v0.2.0-local-arm64-arm64-v8a.apk.sha256
artifacts/android/StS2Launcher-v0.2.0-local-universal-universal.apk
artifacts/android/StS2Launcher-v0.2.0-local-universal-universal.apk.sha256
```

Verify a local archived APK from `artifacts/android/`:

```bash
sha256sum -c StS2Launcher-v0.2.0-local-arm64-arm64-v8a.apk.sha256
```

The Android `x86_64` emulator is useful for Steam authentication and download testing, but it is not a valid proof target for launching the downloaded Godot/.NET game. Once a non-empty game PCK is present, `x86_64` routes to a native fallback screen instead of starting Godot, because the emulator path crashes inside the Mono/GodotSharp native runtime. The fallback diagnostics report whether the PCK header looks valid. Use an `arm64-v8a` Android device/build to test actual game launch.

### Local Android smoke test

Once `adb devices` shows exactly one attached device or emulator, run:

```powershell
.\scripts\test-android-local.ps1
```

The smoke-test script selects the newest archived APK matching the attached device ABI, installs it, launches `LauncherActivity`, captures logcat to `artifacts/android/logcat-smoke-*-full.txt`, writes a focused subset to `artifacts/android/logcat-smoke-*-filtered.txt`, writes a handoff summary to `artifacts/android/logcat-smoke-*-summary.txt`, and reports whether it saw the native x86 fallback route or crash markers.
If the selected APK has a `.sha256` sidecar, the script verifies it before install and stops on mismatch.
By default, local builds and the smoke-test script use package `com.sts2launcher.overhaul.fork.dev`.

For a clean app-data run:

```powershell
.\scripts\test-android-local.ps1 -ClearAppData
```

If the emulator is still booting or ADB is slow to attach, wait before failing:

```powershell
.\scripts\test-android-local.ps1 -WaitForDeviceSeconds 60
```

If more than one device/emulator is attached, pass the target serial:

```powershell
.\scripts\test-android-local.ps1 -DeviceSerial emulator-5554
```

### Installing

```bash
adb install -r android/build/outputs/apk/mono/release/StS2Launcher-v*.apk

# Fresh install for local build wrapper default package
adb shell pm clear com.sts2launcher.overhaul.fork.dev

# Fresh install for production/release package
adb shell pm clear com.sts2launcher.overhaul.fork
```

### Downloadable Android release

GitHub Actions now builds Android APKs and publishes them to Releases.

1. Open the repository **Releases** page: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases
2. Download `StS2Launcher-vX.Y.Z.apk` for the latest release.
    - Direct latest URL: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/latest
    - Downloaded artifact names include:
      - `StS2Launcher-v<version>.apk` (installable package)
      - `StS2Launcher-v<version>.apk.sha256` (optional checksum)
3. (Optional) Verify checksum:

```bash
sha256sum -c StS2Launcher-vX.Y.Z.apk.sha256
```

4. Install on your phone:

```bash
adb install -r StS2Launcher-vX.Y.Z.apk
```

Signed vs unsigned behavior:

- Signed release (production): generated when repository signing secrets are configured.
- Unsigned release (testing): generated when signing secrets are missing; install works for test devices only and may trigger OS security warnings on fresh devices.

### Release install troubleshooting

If installation fails:

- `INSTALL_PARSE_FAILED_NO_CERTIFICATES` or signature errors:
  - likely a partially downloaded APK or signing mismatch.
  - re-download and re-run `sha256sum -c`.
- `INSTALL_FAILED_UPDATE_INCOMPATIBLE`:
  - remove previous app install first, then reinstall (this fork now uses package `com.sts2launcher.overhaul.fork`):
  
  ```bash
  adb uninstall com.sts2launcher.overhaul.fork
  adb install -r StS2Launcher-vX.Y.Z.apk
  ```
- `INSTALL_FAILED_OLDER_SDK`:
  - your device is running an unsupported Android API level.
- `INSTALL_FAILED_DEXOPT` or immediate crash:
  - capture logs with `adb logcat` and open a release issue with stack trace.

### Release workflow (for contributors)

Maintainers can trigger the release workflow manually from the Actions tab or let it run automatically when pushing tags like `v1.2.3`.

- Tag-based publish:
  - Push `vX.Y.Z` to `main`.
- Manual publish:
  - `workflow_dispatch` input fields support overriding `release_tag`, `package_name`, version name/code, and whether to create the GitHub release.
- Optional signing:
  - Configure repository secrets:
    - `ANDROID_RELEASE_KEYSTORE_BASE64`
    - `ANDROID_RELEASE_KEYSTORE_PASSWORD`
    - `ANDROID_RELEASE_KEY_ALIAS`

If signing secrets are missing, the workflow still creates an unsigned release APK so download/testing can continue.

Release validation checklist for every release is tracked in [docs/android-release-validation.md](docs/android-release-validation.md).

### Other build tasks

```bash
# Regenerate bootstrap PCK (only if project.godot changes)
python3 scripts/make-bootstrap-pck.py

# Rebuild Godot engine (only if engine source changes)
bash scripts/setup-godot-source.sh
bash scripts/build-godot.sh

# Windows PowerShell equivalent
.\scripts\setup-godot-source.ps1
.\scripts\build-godot.ps1

# Rebuild native stubs (requires Android NDK)
bash src/stubs/build_stubs.sh
```

`scripts/setup-godot-source.sh` or `scripts/setup-godot-source.ps1` restores `vendor/godot` and the Python/SCons virtualenv needed by the matching build script. By default it checks out upstream Godot `4.5.1-stable`; set `GODOT_REPO` and `GODOT_REF` if you have the original patched engine fork. Emulator `x86_64` testing requires `arm64-v8a` and `x86_64` `libgodot_android.so` to be built from the same engine checkout.

## LAN Multiplayer

Both devices must be on the same local network. The mobile app discovers nearby games via UDP broadcast, or you can enter the PC's IP address manually.

On the PC, add `--fastmp` to the Steam launch options:
**Steam > Slay the Spire 2 > Properties > Launch Options** and enter `--fastmp`

This enables the fast multiplayer mode that the mobile client expects.

## Technical Notes

- Native library stubs (`src/stubs/`) provide no-op `.so` files for desktop-only libraries (Steamworks SDK, Sentry) so the linker is satisfied at runtime.
- The bootstrap PCK is a minimal `project.godot` wrapper that enables .NET module initialization without game files.
- The game's Sentry plugin has no `android.arm64` build, so it's disabled via PCK patching and Harmony patches.
- GodotSharp interop is manually bootstrapped in `ModEntry.cs` since the Godot SDK source generators aren't available.

## License

This project is licensed under the [MIT License](LICENSE). See [THIRD_PARTY_LICENSES.md](THIRD_PARTY_LICENSES.md) for third-party dependency licenses.

FMOD requires a commercial license if your project generates revenue. Spine Runtimes require a valid Spine Editor license. See the third-party licenses file for details.
