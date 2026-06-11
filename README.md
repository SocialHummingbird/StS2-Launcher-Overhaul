# StS2 Launcher Overhaul

<p align="center">
  <img src="docs/assets/sts2-launcher-icon.svg" alt="StS2 Launcher icon" width="128" height="128">
</p>

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
- Current Android status: [docs/current-android-status.md](docs/current-android-status.md)

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

## Current Status

**Working ARM64 Android baseline:** the launcher now installs, starts, authenticates with Steam, downloads game files, pulls Steam Cloud saves into Android local app storage, pushes Android saves back to Steam Cloud on the local hardening build, and launches the game with the pulled profile visible in-game.

This is still a polish and hardening phase, not release-candidate signoff. The active work is focused on broader Samsung/One UI retesting, persisted Steam-session/update UX, Steam beta/version selection validation, quieter diagnostics, repeatable release asset hygiene, and full newest-public Pull/confirmed-Push/game-launch smoke.

- Latest published APK release: `v0.2.185-responsive-ui`
- Current APK asset: `StS2Launcher-v0.2.185-responsive-ui-arm64-v8a.apk`
- Package name: `com.sts2launcher.overhaul.fork.dev`
- Release asset SHA-256: `7f15f28c5f7307a798d29c4ce00930cc45322c68867e0c1171435bbadf62afb0`
- Latest verified public release: `0.2.185-responsive-ui` / `versionCode=218500`
- Validated locally/publicly: fresh APK/runtime install, public `v0.2.184 -> v0.2.185` update-compatible release build, responsive launcher login/download-progress/diagnostics/ready-state visual checks on ARM64 hardware, Push-to-Cloud confirmation/cancel safety on the public `v0.2.185` APK, Steam login to Steam Guard on public `v0.2.183`, Steam game download, Pull from Cloud, Push to Cloud, Pull-after-Push round trip, Android local save handoff, game launch/profile visibility, and restart-to-launcher behavior on ARM64 hardware.
- Still hardening: Samsung A53/S25+/S24 Ultra reporter retests, repeated public-release Pull/confirmed-Push/game-launch smoke on the newest APK, persisted Steam session/update UX, Steam beta/version selection validation, richer launch progress UI, diagnostics polish, and release-candidate signoff.
- Steam beta/version selection is implemented for validation, with a default/public versus `beta` selector, branch-aware downloads, side-by-side non-public caches, selected-version diagnostics, wrapped selector guidance, native routing/fallback diagnostics, branch-switch marker evidence, and safe branch-switch warnings. Static CI guardrails cover version-selection docs, release blockers, and managed/native selector-guidance parity. It is not release-signed yet: beta password behavior, inaccessible/private branch handling, startup routing, cache cleanup, Push backup evidence, and save compatibility across branches still require ARM64 device validation.
- Steam version selection user guide: [docs/steam-version-selection-user-guide.md](docs/steam-version-selection-user-guide.md).
- Branch validation checklist: [docs/steam-version-selection-validation.md](docs/steam-version-selection-validation.md).
- Branch validation runbook: [docs/steam-version-selection-runbook.md](docs/steam-version-selection-runbook.md).
- Emulator limitation: Android `x86_64` is fallback/diagnostic coverage only. ARM64 hardware remains the proof target.

See [docs/current-android-status.md](docs/current-android-status.md) for the current evidence and remaining blockers.

## Features

- **Steam authentication**  
  Login via SteamKit2 with Steam Guard 2FA support. ARM64 device validation has progressed through authenticated download, cloud pull, local hardening Push, and game launch; release hardening is still ongoing.
- **Game file download**  
  Depot download directly from Steam, with update checking, an ARM64-validated responsive progress screen, Steam branch selection for the default/public branch versus a named beta branch, and side-by-side cached installs for non-public branches. Beta branch support is currently a hardening feature: arbitrary branch discovery and Steam beta password entry are not implemented.
- **Cloud saves**  
  Steam cloud sync via SteamKit2's CCloud API, with timestamp-aware conflict resolution and non-blocking background uploads. Pull from Cloud, Push to Cloud, and Pull-after-Push round trip are validated on ARM64 local hardening builds. Push remains an explicit overwrite-risk action because it can replace Steam Cloud state, and now requires an `ARE YOU SURE?` arming tap before the final confirmation.
- **Mobile adaptation**  
  Touch input, short-edge-aware launcher scaling, responsive ready/download/login layouts, and app lifecycle handling via Harmony runtime patches.
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

The Android `x86_64` emulator is useful for install, routing, release packaging, and native diagnostic-screen testing, but it is not a valid proof target for the Godot/.NET launcher or downloaded game. `x86_64` routes to a native fallback screen instead of starting Godot, because the emulator path crashes inside the Mono/GodotSharp native runtime. A forced-Godot emulator run can still crash with native runtime failures such as destroyed mutex access. Use an `arm64-v8a` Android device/build to test Steam login, download, and actual game launch.

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

# Fresh install for future production package, if used
adb shell pm clear com.sts2launcher.overhaul.fork
```

### Downloadable Android release

GitHub Actions now builds Android APKs and publishes them to Releases.

1. Open the repository **Releases** page: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases
2. Download the APK for the latest release.
    - Direct latest URL: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/latest
    - Current release assets are ARM64-only test packages, named like:
      - `StS2Launcher-v<version>-arm64-v8a.apk`
    - Older releases may include universal or x86_64 assets. Prefer ARM64 for phones.

Current published APK release:

```powershell
.\scripts\verify-android-release-apk.ps1 `
  -ReleaseTag "v0.2.185-responsive-ui" `
  -AssetName "StS2Launcher-v0.2.185-responsive-ui-arm64-v8a.apk" `
  -Abi arm64-v8a

.\scripts\install-android-release.ps1 `
  -ReleaseTag "v0.2.185-responsive-ui" `
  -AssetName "StS2Launcher-v0.2.185-responsive-ui-arm64-v8a.apk" `
  -ClearAppData `
  -Launch `
  -CaptureDiagnostics
```

Release details:

```text
Release: v0.2.185-responsive-ui
Asset: StS2Launcher-v0.2.185-responsive-ui-arm64-v8a.apk
Package: com.sts2launcher.overhaul.fork.dev
SHA-256: 7f15f28c5f7307a798d29c4ce00930cc45322c68867e0c1171435bbadf62afb0
```

The verifier downloads the GitHub release asset, checks its release SHA-256 digest, confirms the expected native libraries are present, and checks that `libgodot_android.so` contains the Android app-data .NET assembly lookup marker rather than the stale PCK lookup marker.

Safe public trial checklist:

1. Use an ARM64 Android phone. Current public APKs are not x86_64 emulator proof.
2. Install the latest GitHub release APK from the Releases page.
3. Log in only with a Steam account that owns Slay the Spire 2.
4. Download the game through the launcher.
5. Use Pull from Cloud before Push to Cloud.
6. Confirm Android local saves/profiles exist before using Push to Cloud.
7. Treat Push to Cloud as destructive: it makes Steam Cloud reflect Android local saves, can overwrite remote save state, now requires an `ARE YOU SURE?` arming tap, and still requires the final confirmation dialog.

Support boundaries for public testers:

- This is an unofficial community launcher and does not include game assets.
- Do not post Steam credentials, guard codes, refresh tokens, private save data, or full unsanitized logs in public issues or Reddit threads.
- Current support target is ARM64 Android hardware. x86_64 emulator behavior is diagnostic-only.
- If reporting a cloud-save issue, say whether you used Pull or Push, but scrub usernames, account IDs, and save contents first.

3. Optional manual checksum verification:

```bash
sha256sum -c StS2Launcher-vX.Y.Z-arm64-v8a.apk.sha256
```

4. Optional manual install:

```bash
adb install -r StS2Launcher-vX.Y.Z-arm64-v8a.apk
```

Signing behavior:

- Published APK releases require repository signing secrets and a pinned release signer fingerprint.
- The release workflow refuses to publish a temporary-key APK because it would not update existing installs safely.

Known current runtime limitations:

- The app now has a validated working ARM64 path through download, cloud pull, cloud push hardening, and game launch, but this is not yet a finished release-candidate pass.
- Push to Cloud is locally validated after the managed SHA-1 hardening fix, and that fix is included in the verified public APK line. Repeat Push confirmation/cancel smoke on the newest public APK is still required before release-candidate signoff.
- The public release package has passed update-compatible release builds through `v0.2.185-responsive-ui`; the newest APK also has ARM64 visual validation for the responsive launcher download-progress and ready states plus Push confirmation/cancel path. Repeated local `.local` in-place upgrade coverage remains secondary because local builds use a separate package identity.
- Stale assembly cache behavior still needs repeated local upgrade coverage after signing continuity is fixed.
- `x86_64` emulator validation is fallback/diagnostic coverage only unless explicitly forcing Godot for crash investigation.

### Release install troubleshooting

If installation fails:

- `INSTALL_PARSE_FAILED_NO_CERTIFICATES` or signature errors:
  - likely a partially downloaded APK or signing mismatch.
  - re-download and re-run `sha256sum -c`.
- `INSTALL_FAILED_UPDATE_INCOMPATIBLE`:
  - remove the previous install first, then reinstall. Current test releases use package `com.sts2launcher.overhaul.fork.dev`:
  
  ```bash
  adb uninstall com.sts2launcher.overhaul.fork.dev
  adb install -r StS2Launcher-vX.Y.Z-arm64-v8a.apk
  ```
- `INSTALL_FAILED_OLDER_SDK`:
  - your device is running an unsupported Android API level.
- `App isn't compatible with your phone`:
  - make sure you downloaded an APK matching your device ABI. Current public test releases are ARM64-only.
- `INSTALL_FAILED_DEXOPT` or immediate crash:
  - capture logs with `adb logcat` and open a release issue with stack trace.

### Release workflow (for contributors)

Maintainers can trigger the release workflow manually from the Actions tab or let it run automatically when pushing tags like `v1.2.3`.

- Tag-based publish:
  - Push `vX.Y.Z` to `main`.
- Manual publish:
  - `workflow_dispatch` input fields support overriding `release_tag`, `package_name`, version name/code, and whether to create the GitHub release.
- Required release signing:
  - Configure repository secrets:
    - `ANDROID_RELEASE_KEYSTORE_BASE64`
    - `ANDROID_RELEASE_KEYSTORE_PASSWORD`
    - `ANDROID_RELEASE_KEY_ALIAS`
  - Configure repository variable:
    - `ANDROID_RELEASE_SIGNER_SHA256`

If signing secrets or `ANDROID_RELEASE_SIGNER_SHA256` are missing, the workflow refuses to publish. This prevents GitHub from creating APKs that cannot update the installed app.

Use the helper script to configure GitHub from a stable release keystore:

```powershell
.\scripts\configure-android-release-signing.ps1 `
  -KeystorePath C:\path\to\release.keystore `
  -KeystorePassword "<password>" `
  -KeyAlias "<alias>"
```

Check whether GitHub is ready to publish update-compatible APKs:

```powershell
.\scripts\check-android-release-readiness.ps1
```

The release workflow also verifies the built APK against a previous GitHub release APK before upload. It fails if the package name changes, the signing certificate changes, or `versionCode` does not increase. If the current public APK was signed with a temporary key, create one explicit stable-signing baseline release with `allow_update_baseline_reset=true`; direct update from the temporary-key APK is impossible, but later GitHub releases will be pinned to the stable signer.

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
