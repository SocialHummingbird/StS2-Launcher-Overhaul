# StS2 Launcher

<p align="center">
  <img src="docs/assets/sts2-mobile-icon.svg" alt="StS2 Launcher icon" width="128" height="128">
</p>

## What This App Is

StS2 Launcher is an unofficial Android launcher for people who already own Slay the Spire 2 on Steam.

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

StS2 Launcher works on tested ARM64 Android devices, but compatibility is not broad yet. Treat every APK as prerelease tester software.

Latest published APK: [v0.2.416-startup-recovery-ime](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/tag/v0.2.416-startup-recovery-ime)

- APK asset: `StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk`
- Package name: `com.sts2launcher.overhaul.fork.local`
- Version code: `416001`
- SHA-256: `fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b`
- Signing channel: local test channel

The working tree contains post-release Android routing changes validated on an API 36 x86_64 emulator. Those changes are not in `v0.2.416`, have not been released, and still require ARM64 hardware validation.

Known important limitations:

- Device compatibility varies. The exact `v0.2.416` APK reaches the real public-branch game main menu and remains stable through the 60-second heartbeat on the connected Samsung ARM64 test device. This does not prove every manufacturer, Android version, or graphics driver.
- `v0.2.416` fixes a runtime-pack validation failure that could reject an authentic patched game assembly, a native fallback loop caused by retained pending-launch state, and Samsung's unexpected launcher keyboard. Issue #36 still needs confirmation on the reporter's Xiaomi tablet.
- The app currently targets ARM64 Android hardware. Android emulator and x86_64 builds are diagnostic only and are not supported for real game launch.
- Some graphics drivers and renderer paths remain incompatible. PowerVR devices are routed to OpenGL because reporter testing found that Vulkan reached the game without usable touch.
- Steam version selection, beta branches, Workshop mods, and native modded-save compatibility remain experimental.
- Steam Cloud Pull has visible phase/progress reporting and Upload explains its blocking safeguards. Steam Cloud Push is intentionally cautious because it can overwrite remote saves; issue #35 still needs confirmation on the reporter's Odin device.
- No real Steam Cloud Push was run while validating `v0.2.416`. Pull from Steam Cloud first and keep independent backups.
- This is not a finished consumer app. Expect bugs, incomplete device coverage, and device-specific problems.

Validation claims are deliberately separated:

| Evidence source | What it proves | What it does not prove |
| --- | --- | --- |
| Automated checks | Managed Release compilation, Java regressions, Gradle assembly, APK structure/ABI/crypto checks, fixture-based branch/mod checks, and fake-store cloud safety | Android rendering, OEM lifecycle behavior, real Steam services, or gameplay |
| API 36 x86_64 emulator | Current-source cold/cached native routing, fallback/recovery controls, forced bootstrap failure, transactional cache preservation, rotation, Home/resume, and native-path IME state | The Godot/.NET launcher, Steam login/download/cloud, ARM64 runtime behavior, `NMainMenu`, or gameplay |
| Samsung ARM64 hardware | Exact published `v0.2.416` artifact install/hash, full cold transition, launcher rendering with IME hidden, public runtime-pack promotion, real `NMainMenu`, and heartbeats through 60 seconds | Other manufacturers, every branch/mod/GPU, reporter devices, or a real Steam Cloud Push |
| Still requires ARM64 hardware | Current unreleased routing changes, reporter-device retests, broad renderer/display/lifecycle coverage, modded and branch launch paths, and cloud Pull/upload-eligibility UX | Nothing is considered validated until the exact candidate and test path are recorded |

Before installing:

- Make sure you own Slay the Spire 2 on Steam.
- Use an ARM64 Android device.
- Back up important saves before testing Steam Cloud Push.
- Download APKs only from this repository's [Releases](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases).
- When reporting a bug, include the exact APK filename, device model, Android version, selected branch, and logs. Do not just say "latest".

Useful docs:

- Current Android status: [docs/current-android-status.md](docs/current-android-status.md)
- Android troubleshooting: [docs/android-troubleshooting.md](docs/android-troubleshooting.md)
- Android validation runbook: [docs/runbook-android-validation.md](docs/runbook-android-validation.md)
- PowerVR input compatibility evidence: [docs/android-powervr-input-compatibility-20260716.md](docs/android-powervr-input-compatibility-20260716.md)
- Testing needed: [docs/testing-needed.md](docs/testing-needed.md)
- Issue reporting guide: [docs/issue-reporting.md](docs/issue-reporting.md)
- Android Steam Workshop mods: [docs/android-workshop-mods.md](docs/android-workshop-mods.md)
- Steam version selection user guide: [docs/steam-version-selection-user-guide.md](docs/steam-version-selection-user-guide.md)

## Project Background

This repository is a full copy of [Ekyso/StS2-Launcher](https://github.com/Ekyso/StS2-Launcher), created to continue and broaden development as StS2 Launcher: a focused Android launcher and compatibility project.

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
  Development status, not a public fix: current unreleased source keeps game saving local-only; gameplay never queues, flushes, or uploads Steam operations. Launcher-owned automatic sync is enabled by default and recreates Steam's before/after-game behavior: before Play it reconciles local and remote manifests, records one immutable snapshot and one atomic pending record, and starts the game only when the selected Steam account, vanilla/modded namespace, runtime branch, and mod-set fingerprint are compatible. After normal Quit the launcher retries that record and uploads or downloads through the same desktop-tested transfer used by the manual recovery controls. A first sync requires an explicit one-time Local-or-Steam source choice; later ambiguous changes stop as conflicts instead of choosing by timestamp. Device settings stay local, every overwritten destination is backed up, current-run deletions are transferred, and authentication, upload, commit, download, deletion, and read-back failures are intended to remain pending rather than reporting success. Desktop fixtures support these statements; Android behavior and real Steam Cloud remain unproven until the complete Stage 5 physical matrix passes. There is no background service, per-write queue, lifecycle sync, or semantic merge.
- **Save recovery**
  Unreleased and desktop-tested only: the Saves page can scan launcher snapshots plus legacy vanilla, modded, temporary, and backup locations without changing the source files. Candidates are copied into the same content-addressed snapshot storage used by sync, with exact account/version/mod-set provenance kept separate, foreign-account data quarantined, and uncertain provenance labeled unknown. A locally read-back-hashed support bundle must be exported before Restore is enabled. Restore and Undo are designed as Android-local byte-for-byte operations that cannot contact Steam; the physical-device matrix has not yet proved that contract. Restored data remains blocked from synchronization until it is opened and validated locally, then explicitly approved for the exact recorded context. The launcher does not automatically reconstruct progression from run history; that remains a manual support-only last resort.
- **Steam Workshop mods**  
  Subscribed Workshop mods can be synced into app-private Android storage and selected for the runtime mod-loader. `v0.2.399` historically validated BaseLib, Quick Restart 2, and a launcher SavesMerger substitute; Quick Restart's in-game `Restart Room` action was exercised successfully. Current source builds remove that substitute, classify SavesMerger/UnifiedSavePath as deprecated, and use the game's native modded save paths. BaseLib remains partial Android compatibility. Workshop sync and clear do not run Steam Cloud Upload; modded save transfers require the exact selected mod-set fingerprint.
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
The custom engine remains based on Godot 4.5.1 but backports Godot 4.5.2's all-PowerVR workaround, disabling the unsafe GLES3 transform-feedback shader cache whenever the renderer name contains `PowerVR`. `v0.2.400` additionally reads the live GPU identity and forces OpenGL Compatibility on PowerVR because reporter testing showed that Vulkan reached the game but did not accept touch. Non-PowerVR devices retain Auto, Vulkan, OpenGL, and Safe Start behavior. The issue #34 reporter subsequently confirmed that the automatic PowerVR/OpenGL route loads the game with active touch.

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

This verifies and installs `StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk` without clearing app data, launches it, captures portrait and landscape screenshots plus focused lifecycle/fatal logs under `artifacts/android/launcher-ui-device-*`, and restores the device's rotation settings. It refuses to capture a locked or system-obscured display. It does not tap launcher actions or run Steam Cloud Push.

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

These smoke-test commands install and launch the `.local` package. Use them only on a disposable emulator or development device whose app data is not being preserved. Never run them against an affected user's published `.local` install. Once `adb devices` shows exactly one disposable target, run:

```powershell
.\scripts\test-android-local.ps1
```

The smoke-test script selects the newest archived APK matching the attached device ABI, installs it, launches `LauncherActivity`, captures logcat to `artifacts/android/logcat-smoke-*-full.txt`, writes a focused subset to `artifacts/android/logcat-smoke-*-filtered.txt`, writes a handoff summary to `artifacts/android/logcat-smoke-*-summary.txt`, and reports whether it saw the native x86 fallback route or crash markers.
If the selected APK has a `.sha256` sidecar, the script verifies it before install and stops on mismatch.
By default, local builds and the smoke-test script use package `com.sts2launcher.overhaul.fork.local`.

For a clean app-data run on that disposable target only:

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

The direct command below is for disposable development installs. It is not the affected-user recovery/update procedure and must not be combined with clearing data.

```bash
adb install -r android/build/outputs/apk/mono/release/StS2Launcher-v*.apk
```

### Downloadable Android release

GitHub Actions currently builds a manually dispatched Android release-candidate artifact only. It does not publish a GitHub Release; the exact candidate must pass the complete Stage 5 device matrix before a future byte-identical promotion.

1. Open the repository **Releases** page: https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases
2. Download the APK named in the current published APK block below.
    - GitHub's `/releases/latest` currently points at `v0.2.416-startup-recovery-ime`; still include the exact tag and APK filename in reports so later releases do not make old reports ambiguous.
    - Release inventory: [docs/github-release-inventory.md](docs/github-release-inventory.md)
    - Current release assets are ARM64-only test packages, named like:
      - `StS2Launcher-v<version>-arm64-v8a.apk`
    - Older releases may include universal or x86_64 assets. Prefer ARM64 for phones.

Current published APK release:

```powershell
.\scripts\verify-android-release-apk.ps1 `
  -ReleaseTag "v0.2.416-startup-recovery-ime" `
  -AssetName "StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk" `
  -Abi arm64-v8a
```

Do not use `-ClearAppData`, uninstall, or an install-and-launch wrapper on a published `.local` install whose saves matter.

Release details:

```text
Release: v0.2.416-startup-recovery-ime
Asset: StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk
Package: com.sts2launcher.overhaul.fork.local
VersionName: 0.2.416-startup-recovery-ime-local
VersionCode: 416001
SHA-256: fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b
```

The verifier downloads the GitHub release asset, checks its release SHA-256 digest, confirms the expected native libraries are present, and checks that `libgodot_android.so` contains the Android app-data .NET assembly lookup marker rather than the stale PCK lookup marker. Use `scripts\check-github-release-hygiene.ps1` before announcing a release so the APK, checksum sidecar, metadata sidecar, release body, package name, version, and SHA-256 all agree on the fork release page.

Published `v0.2.416` safe public trial checklist:

The current-source Stage 2 transfer behavior described above is unreleased and is not present in `v0.2.416`; public testers should follow the released-build cautions below.

1. Use an ARM64 Android phone. Current public APKs are not x86_64 emulator proof.
2. Install the latest GitHub release APK from the Releases page.
3. Log in only with a Steam account that owns Slay the Spire 2.
4. Download the game through the launcher.
5. Use Pull from Cloud before Push to Cloud.
6. Confirm Android local saves/profiles exist before using Push to Cloud.
7. Treat Push to Cloud as destructive: it makes Steam Cloud reflect Android local saves, can overwrite remote save state, now requires an `ARE YOU SURE?` arming tap, and still requires the final confirmation dialog.
8. If testing mods, Pull first, launch vanilla once, then enable selected mods. Do not enable SavesMerger, and do not push modded-save state to Steam Cloud from `v0.2.416`.
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

This generic command is only for a fresh or disposable install. Existing v0.2.416 data must use the source-bound recovery candidate procedure in [Android release validation](docs/android-release-validation.md).

```bash
adb install -r StS2Launcher-vX.Y.Z-arm64-v8a.apk
```

Signing behavior:

- The published v0.2.416 APK is package `com.sts2launcher.overhaul.fork.local`, versionCode `416001`, signed by certificate SHA-256 `FD0E3D5ACF435C1D23BFC5C426E99AA9EB5808619FF1FC214FFCA99CFAC7E57A`.
- The manually dispatched GitHub Actions candidate path is hard-coded to that same package and must use that same certificate. It verifies the exact v0.2.416 tag, asset, and APK SHA-256 before retaining a candidate; there is no package selector or baseline-reset mode.
- Until the dedicated `ANDROID_LOCAL_UPDATE_*` secrets and signer variable are configured, readiness and the candidate build fail. A differently signed APK cannot update or read the existing private data.
- A verified in-place candidate is only the path to preserve data and expose the local exporter. It is not a public fix until the complete Stage 5 matrix passes.

For an affected v0.2.416 user, statically verify the candidate first, install it only with the update-preserving `adb install -r` path without launch/clear/uninstall, and make the first launch offline. Export and read-back verify **Vanilla** and **Modded** current saves separately; only then scan and export recovery copies. Keep the device offline and do not Play, Pull, Push, Restore, or Approve until those bundles have been inspected. The exact command sequence and stop conditions are in [Android release validation](docs/android-release-validation.md).

Known current runtime limitations:

- Historical ARM64 builds have a validated path through download, the superseded manual cloud flow, and game launch. The current local-save/automatic-sync/recovery source has passed 0/10 Stage 5 device rows and is not a public fix.
- No real Steam Cloud Push was run on exact `v0.2.416`. Automated cloud tests use fake stores and prove safeguards without mutating Steam Cloud.
- Exact `v0.2.416` was installed as an in-place local-channel update from `v0.2.412`; this is not production-signer compatibility.
- Pixel 10 Pro / Android 17 / PowerVR issue #34 is reporter-confirmed resolved through the Auto/OpenGL compatibility route. That result is device-specific, not broad PowerVR support.
- Stale assembly cache behavior still needs repeated local upgrade coverage after signing continuity is fixed.
- Current-source `x86_64` emulator validation covers native routing, lifecycle, fallback/recovery, input, and forced bootstrap failure. It remains incapable of proving the Godot/.NET launcher or game.

### Release install troubleshooting

See [Android troubleshooting](docs/android-troubleshooting.md) for startup, fallback, keyboard, loading, game-exit, and cloud-report guidance.

If installation fails:

- `INSTALL_PARSE_FAILED_NO_CERTIFICATES` or signature errors:
  - likely a partially downloaded APK or signing mismatch.
  - re-download and re-run `sha256sum -c`.
- `INSTALL_FAILED_UPDATE_INCOMPATIBLE`:
  - stop. The candidate package or signer does not match the installed `.local` lineage. Diagnose both APK identities; never uninstall or clear the published install unless its private save bytes have already been exported and independently read-back verified.
- `INSTALL_FAILED_OLDER_SDK`:
  - your device is running an unsupported Android API level.
- `App isn't compatible with your phone`:
  - make sure you downloaded an APK matching your device ABI. Current public test releases are ARM64-only.
- `INSTALL_FAILED_DEXOPT` or immediate crash:
  - capture logs with `adb logcat` and open a release issue with stack trace.

### Release workflow (for contributors)

Maintainers can trigger the candidate workflow manually from the Actions tab. It has no tag trigger and cannot create or update a GitHub Release.

- Manual candidate build:
  - `workflow_dispatch` accepts candidate version/build-source metadata. Package and update baseline are fixed to the published v0.2.416 `.local` lineage.
- Required v0.2.416 local-update signing:
  - Configure repository secrets:
    - `ANDROID_LOCAL_UPDATE_KEYSTORE_BASE64`
    - `ANDROID_LOCAL_UPDATE_KEYSTORE_PASSWORD`
    - `ANDROID_LOCAL_UPDATE_KEY_ALIAS`
  - Configure repository variable:
    - `ANDROID_LOCAL_UPDATE_SIGNER_SHA256`

If these dedicated credentials are missing, the candidate workflow refuses to build. `ANDROID_LOCAL_UPDATE_SIGNER_SHA256` must be `FD0E3D5ACF435C1D23BFC5C426E99AA9EB5808619FF1FC214FFCA99CFAC7E57A`.

Use the helper script to configure GitHub from a stable release keystore:

```powershell
.\scripts\configure-android-release-signing.ps1 `
  -KeystorePath C:\path\to\release.keystore `
  -KeystorePassword "<password>" `
  -KeyAlias "<alias>" `
  -OfflineBackupConfirmed
```

Before using that confirmation switch, make and verify a controlled offline backup of the exact v0.2.416 update key. The ignored `tmp/localtest.keystore` and a GitHub secret are not sufficient as the only recoverable copies.

Check whether GitHub is ready to build an update-compatible candidate:

```powershell
.\scripts\check-android-release-readiness.ps1
```

The candidate workflow verifies the built APK against the exact published v0.2.416 APK before artifact upload. It fails if the package name or signing certificate changes, if `versionCode` does not increase, or if the fixed baseline bytes differ. There is no baseline-reset bypass.

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
