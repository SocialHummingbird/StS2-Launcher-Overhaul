# StS2 Mobile

<p align="center">
  <img src="docs/assets/sts2-mobile-icon.svg" alt="StS2 Mobile icon" width="128" height="128">
</p>

## What This App Is

StS2 Mobile is an unofficial Android launcher for people who already own Slay the Spire 2 on Steam.

It is not the game itself. It does not include Slay the Spire 2 files, art, music, saves, or Workshop content. After you sign in with Steam, the launcher downloads your own copy of the game from Steam and tries to run it on an ARM64 Android phone or tablet.

You must own Slay the Spire 2 on Steam. If your Steam account does not own the game, this app cannot download or run it.

This project is not made, approved, sponsored, or supported by Mega Crit Games, Steam, or Valve. It is community tester software, not an official Android release or mobile port. See the [unofficial project notice](docs/unofficial-project-notice.md).

## What It Does

- Lets you sign in to Steam.
- Downloads Slay the Spire 2 game files from Steam after Steam confirms ownership.
- Starts the downloaded game on supported ARM64 Android devices.
- Can pull Steam Cloud saves down to Android local storage.
- Can push Android saves back to Steam Cloud, but only after extra warnings because that can overwrite cloud saves.
- Includes early support for Steam branches and some Workshop/mod testing.
- Collects diagnostics so crash reports can show what happened.

## Current Status

StS2 Mobile works on some tested ARM64 Android devices, but compatibility is not broad yet. Treat every APK as prerelease tester software.

Latest published APK: [v0.2.400-powervr-touch-compat](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/tag/v0.2.400-powervr-touch-compat)

- APK asset: `StS2Launcher-v0.2.400-powervr-touch-compat-local-arm64-v8a.apk`
- Package name: `com.sts2launcher.overhaul.fork.local`
- Version code: `400001`
- SHA-256: `623830caad7a684e3358fbb22564210a1236588e03e7161dfcf30cc5aa76cdc3`
- Signing channel: local debug/test channel

Known important limitations:

- Device compatibility varies. The issue #34 reporter tested `v0.2.399` on Pixel 10 Pro / Android 17 / PowerVR: every renderer mode reached the game, but only OpenGL accepted touch. `v0.2.400` detects PowerVR and forces that OpenGL path for Auto, Vulkan, OpenGL, and Safe Start. Reporter confirmation of this release is still required.
- The app currently targets ARM64 Android hardware. Android emulator and x86_64 builds are diagnostic only and are not supported for real game launch.
- Some graphics drivers and renderer paths remain incompatible. On the reported PowerVR device, OpenGL works but its menus can be slow while graphics are first compiled; gameplay and later menu use were reported normal.
- Steam version selection, beta branches, Workshop mods, and save-merger behavior are still experimental.
- Steam Cloud Push is intentionally cautious because it can overwrite remote saves. Pull from Steam Cloud first.
- This is not a finished consumer app. Expect bugs, incomplete device coverage, and device-specific problems.

Before installing:

- Make sure you own Slay the Spire 2 on Steam.
- Use an ARM64 Android device.
- Back up important saves before testing Steam Cloud Push.
- Download APKs only from this repository's [Releases](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases).
- When reporting a bug, include the exact APK filename, device model, Android version, selected branch, and logs. Do not just say "latest".

Useful docs:

- Current Android status: [docs/current-android-status.md](docs/current-android-status.md)
- PowerVR input compatibility evidence: [docs/android-powervr-input-compatibility-20260716.md](docs/android-powervr-input-compatibility-20260716.md)
- Testing needed: [docs/testing-needed.md](docs/testing-needed.md)
- Issue reporting guide: [docs/issue-reporting.md](docs/issue-reporting.md)
- Android Steam Workshop mods: [docs/android-workshop-mods.md](docs/android-workshop-mods.md)
- Steam version selection user guide: [docs/steam-version-selection-user-guide.md](docs/steam-version-selection-user-guide.md)

## Project Background

This repository is a full copy of [Ekyso/StS2-Launcher](https://github.com/Ekyso/StS2-Launcher), created to continue and broaden development as StS2 Mobile: a focused Android rewrite and compatibility project.

The technical goal is to improve Android startup, Steam login, Steam download, cloud saves, mobile UI, branch switching, mods, and crash diagnostics while keeping the project clearly unofficial.

## Development and Governance

- Migration checklist: [MIGRATION_CHECKLIST.md](MIGRATION_CHECKLIST.md)
- Overhaul plan: [OVERHAUL_ROADMAP.md](OVERHAUL_ROADMAP.md)
- Contribution process: [CONTRIBUTING.md](CONTRIBUTING.md)
- Overhaul status: [OVERHAUL_STATUS.md](OVERHAUL_STATUS.md)
- Changelog: [CHANGELOG.md](CHANGELOG.md)
- Device log checklist: [docs/device-log-checklist.md](docs/device-log-checklist.md)
- Android runtime findings: [docs/android-runtime-findings.md](docs/android-runtime-findings.md)
- Unofficial project notice: [docs/unofficial-project-notice.md](docs/unofficial-project-notice.md)

## Features

- **Steam authentication**  
  Login via SteamKit2 with Steam Guard 2FA support. Android now uses an integrated in-app native Steam credential panel with real username/password fields, Android credential-provider hints, Steam web-domain metadata, accessible field labels, inline status/error guidance, keyboard-safe scrollable layout, and stacked full-width touch controls for Samsung/Google/password-manager suggestions where supported. The separate native `USE ANDROID AUTOFILL` handoff popup is no longer user-facing. The launcher does not store or inject Steam passwords, and native username/password fields are cleared after submit/cancel/expiry. ARM64 device validation has progressed through authenticated download, cloud pull, local hardening Push, and game launch; password-manager suggestion behavior in the native panel is still pending and tracked in [Android Steam login validation](docs/android-steam-login-validation.md).
- **Game file download**  
  Depot download directly from Steam, with update checking, an ARM64-validated responsive progress screen, Steam branch/version dropdown selection, a non-mutating `Refresh Game Versions` action that reads account-visible Steam app-info branch metadata, and side-by-side cached installs for non-public branches. The portal explicitly separates local version download/update actions from Steam Cloud save actions and collapses verbose version details on compact screens. Beta/version support is currently a hardening feature: dropdown labels stay concise but can show ready/build/password/unavailable badges, selected-version helper text surfaces availability/password/build metadata where Steam exposes it, known unavailable branches are blocked before game-version download/update attempts, `public-beta` has local ARM64 launch proof from its side-by-side cache, and Steam beta password entry is not implemented.
- **Cloud saves**  
  Steam cloud sync via SteamKit2's CCloud API, with timestamp-aware conflict resolution and non-blocking background uploads. Pull from Cloud, Push to Cloud, and Pull-after-Push round trip are validated on ARM64 local hardening builds. The portal labels Pull as Steam Cloud to Android and Push as Android saves to Steam Cloud, places Pull before Push so the safer baseline action is visually first, keeps those primary cloud actions above lower-frequency cloud options, and collapses cloud-safety guidance/options on compact screens to reduce clutter. Push remains an explicit overwrite-risk action because it can replace Steam Cloud state, requires an overwrite confirmation arming tap before the final confirmation, shows an armed overwrite warning before the final confirmation, and now gates manual Push on current-version Pull evidence plus Android local save evidence before upload. Branch-switch Push adds stricter selected-version Pull/local-save/backup evidence gates.
- **Steam Workshop mods**  
  Subscribed Workshop mods can be synced into app-private Android storage and selected for the runtime mod-loader. `v0.2.399` applies the Android-publicized runtime-pack path to the public branch, restarts when the prepared game assembly is not the one loaded by the process, and records per-mod payload, Harmony target, partial-compatibility, substitute, failure, and in-game-verification evidence. Connected public-branch validation loaded BaseLib, Quick Restart 2, and SavesMerger with zero runtime failures; Quick Restart's in-game `Restart Room` action was exercised successfully. BaseLib remains partial Android compatibility, and SavesMerger uses launcher save-path patches instead of loading the mod payload. Workshop sync and clear do not run Steam Cloud Push; manual Push is locked while mods are selected.
- **Mobile adaptation**  
  Touch input, five stable Home/Saves/Versions/Mods/Help destinations, bottom navigation on phones, top navigation on wide/foldable layouts, safe-area-aware composition, larger touch targets, responsive login/download/confirmation/diagnostic layouts, a consistent `Start Game` primary action, Auto/Vulkan/OpenGL recovery selection, hidden technical detail outside support flows, and app lifecycle handling via Harmony runtime patches.
- **LAN multiplayer**  
  UDP broadcast discovery and manual IP join.
- **Shader warmup**  
  Vulkan pipeline cache persistence and canvas ubershader support to eliminate first-encounter stutters.
- **Credential security**  
  Steam refresh tokens encrypted at rest via Android Keystore (AES-256-GCM, hardware-backed TEE). Steam passwords are not stored or injected by the launcher, and SteamKit debug logs are disabled by default; when explicitly enabled for diagnostics, they are sanitized before entering launcher diagnostics.

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
- **PowerVR transform-feedback cache safety**
The custom engine remains based on Godot 4.5.1 but backports Godot 4.5.2's all-PowerVR workaround, disabling the unsafe GLES3 transform-feedback shader cache whenever the renderer name contains `PowerVR`. `v0.2.400` additionally reads the live GPU identity and forces OpenGL Compatibility on PowerVR because reporter testing showed that Vulkan reached the game but did not accept touch. Non-PowerVR devices retain Auto, Vulkan, OpenGL, and Safe Start behavior. Pixel 10 / PowerVR issue #34 remains open until a reporter-class device confirms this release.

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

## Launcher UI Preview

The real managed launcher can be rendered on desktop without contacting Steam, downloading files, touching saves, or launching the game. The preview uses deterministic fixture data and an offscreen viewport, so phone, landscape, and foldable screenshots do not depend on the desktop monitor size.

Render one state and destination:

```powershell
.\scripts\run-launcher-ui-preview.ps1 -Fixture ready -Destination home -Width 1080 -Height 2400 -TouchOptimized $true
```

Render the full 20-screenshot validation matrix and interaction contract:

```powershell
.\scripts\test-launcher-ui-preview.ps1
```

Screenshots are written to `artifacts/ui-preview/`. Every render checks accessible names, focusability, viewport bounds, and target heights. The matrix also verifies deterministic pixels and existing launcher view events without contacting Steam or invoking the final Steam Cloud Push event. The harness requires Godot 4.5.1 Mono; by default the scripts use the local runtime under `tmp/godot-4.5.1-mono/`, or accept `-GodotPath` explicitly.

For physical UI validation, connect one ARM64 Android device with USB debugging enabled and run:

```powershell
.\scripts\test-launcher-ui-device.ps1
```

This verifies and installs `StS2Launcher-v0.2.400-powervr-touch-compat-local-arm64-v8a.apk` without clearing app data, launches it, captures portrait and landscape screenshots plus focused lifecycle/fatal logs under `artifacts/android/launcher-ui-device-*`, and restores the device's rotation settings. It refuses to capture a locked or system-obscured display. It does not tap launcher actions or run Steam Cloud Push.

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
By default, local builds and the smoke-test script use package `com.sts2launcher.overhaul.fork.local`.

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
adb shell pm clear com.sts2launcher.overhaul.fork.local

# Fresh install for future production package, if used
adb shell pm clear com.sts2launcher.overhaul.fork
```

### Downloadable Android release

GitHub Actions now builds Android APKs and publishes them to Releases.

1. Open the repository **Releases** page: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases
2. Download the APK named in the current published APK block below.
    - GitHub's `/releases/latest` currently points at `v0.2.400-powervr-touch-compat`; still include the exact tag and APK filename in reports so later releases do not make old reports ambiguous.
    - Release inventory: [docs/github-release-inventory.md](docs/github-release-inventory.md)
    - Current release assets are ARM64-only test packages, named like:
      - `StS2Launcher-v<version>-arm64-v8a.apk`
    - Older releases may include universal or x86_64 assets. Prefer ARM64 for phones.

Current published APK release:

```powershell
.\scripts\verify-android-release-apk.ps1 `
  -ReleaseTag "v0.2.400-powervr-touch-compat" `
  -AssetName "StS2Launcher-v0.2.400-powervr-touch-compat-local-arm64-v8a.apk" `
  -Abi arm64-v8a

.\scripts\install-android-release.ps1 `
  -ReleaseTag "v0.2.400-powervr-touch-compat" `
  -AssetName "StS2Launcher-v0.2.400-powervr-touch-compat-local-arm64-v8a.apk" `
  -ClearAppData `
  -Launch `
  -CaptureDiagnostics
```

Release details:

```text
Release: v0.2.400-powervr-touch-compat
Asset: StS2Launcher-v0.2.400-powervr-touch-compat-local-arm64-v8a.apk
Package: com.sts2launcher.overhaul.fork.local
VersionName: 0.2.400-powervr-touch-compat-local
VersionCode: 400001
SHA-256: d39825fe2f79ca86af6ff4c83bbcd1eaeeb0fd3eeeedde509cdb3aa6a17420fe
```

The verifier downloads the GitHub release asset, checks its release SHA-256 digest, confirms the expected native libraries are present, and checks that `libgodot_android.so` contains the Android app-data .NET assembly lookup marker rather than the stale PCK lookup marker. Use `scripts\check-github-release-hygiene.ps1` before announcing a release so the APK, checksum sidecar, metadata sidecar, release body, package name, version, and SHA-256 all agree on the fork release page.

Safe public trial checklist:

1. Use an ARM64 Android phone. Current public APKs are not x86_64 emulator proof.
2. Install the latest GitHub release APK from the Releases page.
3. Log in only with a Steam account that owns Slay the Spire 2.
4. Download the game through the launcher.
5. Use Pull from Cloud before Push to Cloud.
6. Confirm Android local saves/profiles exist before using Push to Cloud.
7. Treat Push to Cloud as destructive: it makes Steam Cloud reflect Android local saves, can overwrite remote save state, now requires an `ARE YOU SURE?` arming tap, and still requires the final confirmation dialog.
8. If testing mods, launch vanilla first, then enable selected mods, and do not push modded-save state to Steam Cloud.
9. If reporting a problem, include the exact release tag, APK filename, device model, Android version, selected branch, and whether mods/controller/shader compilation were involved.

Support boundaries for public testers:

- This is an unofficial community launcher, is not endorsed by Mega Crit Games, and does not include game files or assets.
- Do not post Steam credentials, guard codes, refresh tokens, private save data, or full unsanitized logs in public issues or Reddit threads.
- Current support target is ARM64 Android hardware. x86_64 emulator behavior is diagnostic-only.
- Current known user-facing pain points are controller action input, shader compile crashes or stalls, PowerVR touch compatibility and OpenGL cold-menu delay, incomplete exact-build physical viewport coverage for the redesigned UI, and broader mod/save compatibility.
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

- The current public tester APK uses package `com.sts2launcher.overhaul.fork.local` and the repository's local test signing channel. It is not a production-signed release line.
- The GitHub Actions release path requires repository signing secrets and a pinned signer fingerprint before it can claim stable update compatibility.
- APKs signed by a different key cannot update an existing install without uninstalling it first.

Known current runtime limitations:

- The app now has a validated working ARM64 path through download, cloud pull, cloud push hardening, and game launch, but this is not yet a finished release-candidate pass.
- Push to Cloud is locally validated after the managed SHA-1 hardening fix, and that fix is included in the verified public APK line. Repeat Push confirmation/cancel smoke on the newest public APK is still required before release-candidate signoff.
- The exact `v0.2.400` tester APK installed over the existing `com.sts2launcher.overhaul.fork.local` app data and reports version code `400001`. Its package and signer match `v0.2.399`, proving update continuity on the current local test channel, not production-signer compatibility.
- Pixel 10 Pro / Android 17 / PowerVR issue #34 no longer reproduces the original startup crash on `v0.2.399`: all four modes reach the game. Its remaining defect is missing touch under Auto/Vulkan/Safe, while OpenGL accepts touch with temporary cold-menu slowdown. `v0.2.400` routes every PowerVR launch mode to OpenGL; actual PowerVR confirmation is still pending.
- Stale assembly cache behavior still needs repeated local upgrade coverage after signing continuity is fixed.
- `x86_64` emulator validation is fallback/diagnostic coverage only unless explicitly forcing Godot for crash investigation.

### Release install troubleshooting

If installation fails:

- `INSTALL_PARSE_FAILED_NO_CERTIFICATES` or signature errors:
  - likely a partially downloaded APK or signing mismatch.
  - re-download and re-run `sha256sum -c`.
- `INSTALL_FAILED_UPDATE_INCOMPATIBLE`:
  - remove the previous install first, then reinstall. Current test releases use package `com.sts2launcher.overhaul.fork.local`:
  
  ```bash
  adb uninstall com.sts2launcher.overhaul.fork.local
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
