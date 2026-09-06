# StS2 Launcher

<p align="center">
  <img src="docs/assets/sts2-mobile-icon.svg" alt="StS2 Launcher icon" width="128" height="128">
</p>

## What This App Is

StS2 Launcher is an unofficial Android launcher for people who own Slay the Spire 2 on Steam. It does not include the game, its assets, saves, or Workshop content. After Steam confirms ownership, the launcher can download the owner's game files and attempt to run them on ARM64 Android hardware.

This project is not made, approved, sponsored, or supported by Mega Crit Games, Steam, or Valve. See the [unofficial project notice](docs/unofficial-project-notice.md).

## Current Status

The latest ARM64 tester release is **[v0.2.429 — Issue #38 ARM64 RC4](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/tag/v0.2.429-issue38-arm64-rc4)**.

- Package name: `com.sts2launcher.overhaul.fork.local`
- Version name: `0.2.429-issue38-arm64-rc4`
- Version code: `429041`
- ABI: `arm64-v8a` only
- APK SHA-256: `b2d0e8154afc69e11eac4159483e837559e398844444a5d015d18f3fda458181`
- Android emulator and x86_64 paths are diagnostic only.
- Branch selection, Workshop mods, renderer compatibility, and device coverage remain experimental.

RC4 passed **10/10 physical-device launches** on Samsung `SM-F971B`, Android 17 / API 37: four cold, four warm, one background/resume, and one lock/unlock launch. Every attempt reached the game with the launcher overlay removed exactly once; no stale completion was accepted. The validation also confirmed that local saves, Steam Cloud inventory, and unrelated branch/runtime data stayed unchanged. This is strong evidence for that exact device and APK, not a broad compatibility claim.

See [Current Android status](docs/current-android-status.md) for the validation boundary and [v0.2.429 release notes](docs/release-notes/v0.2.429-issue38-arm64-rc4.md) for the change summary.

## Install or Update

1. Confirm the device supports `arm64-v8a`.
2. Download [`StS2Launcher-v0.2.429-issue38-arm64-rc4-arm64-v8a.apk`](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/download/v0.2.429-issue38-arm64-rc4/StS2Launcher-v0.2.429-issue38-arm64-rc4-arm64-v8a.apk).
3. Open the APK and choose **Update** or **Install** over the existing `com.sts2launcher.overhaul.fork.local` application. With ADB, use `adb install -r <apk-path>`.
4. Confirm the installed version is `0.2.429-issue38-arm64-rc4` (`429041`).

Do **not** uninstall the existing application or clear its data as a routine update or recovery step. Either action removes application-private local saves. If Android reports `INSTALL_FAILED_UPDATE_INCOMPATIBLE`, stop and report the installed and downloaded package/signing identities; do not work around it by uninstalling.

## Issue #38 Reliability Fix

- The final installed `SlayTheSpire2.pck` and source `data_sts2_windows_x86_64/sts2.dll` bytes are authoritative. Runtime-pack manifests, cache markers, and old validation reports cannot define the current game identity.
- Steam branch updates are transactional. The selected branch becomes `updating` before installed files change and becomes `ready` only after the completed files have a fresh identity.
- Runtime packs are generated in a unique staging directory, validated against that identity, promoted atomically, and independently revalidated by Android before launch.
- **Redownload Selected Version** repairs only the selected branch. It preserves gameplay saves, Steam credentials, Workshop content, and every other downloaded branch. The former bulk **Remove old versions** action is not part of the current launcher.
- Launcher-to-game handoff readiness is bound to one launch-attempt ID. Main-menu readiness, foreground/focus state, and overlay dismissal must all belong to the active attempt, so late callbacks cannot complete a later launch.

The detailed contracts are in [Steam version selection architecture](docs/steam-version-selection-architecture.md) and the [Issue #38 runtime identity design](docs/issue-38-runtime-identity-design.md).

## Current Product Boundary

The launcher retains:

- Steam authentication and encrypted credential storage.
- Steam game download and version selection.
- Game launch is not blocked by save synchronization after its bounded attempt; selected-runtime identity and readiness are still required.
- One application-local gameplay save store with atomic local writes.
- One Steam Cloud transport and one synchronization service shared by automatic and manual operations.
- Automatic Pull before the game loads saves and queued Push after committed gameplay saves.
- Manual **Sync now**, **Get saves from Steam**, and **Send saves to Steam** actions.
- Home, Saves, Versions, Mods, and Help destinations, with Play remaining available when synchronization is temporarily unavailable.

When both Android and Steam changed differently, synchronization stops and asks the player to choose which copy to keep. Failed or interrupted transfers must not report success or discard the durable dirty/retry state.

## Focused Verification

Build the managed project:

```powershell
dotnet build src\STS2Mobile\STS2Mobile.csproj -c Release
```

Run the focused save and launch probe:

```powershell
.\scripts\test-local-gameplay-save-safety.ps1
```

It covers twelve focused save, synchronization, mod-selection, version-selection, and startup behaviors. The synchronization tests use one in-memory `FakeSaveRemote`; that proves deterministic policy only, not Steam Cloud or Android transport.

Run the Issue #38 identity, update, recovery, runtime-pack, and handoff suite:

```powershell
dotnet run --project tests\STS2Mobile.GameIdentityTests\STS2Mobile.GameIdentityTests.csproj -c Release
```

Run the focused launcher/mod test against the already-downloaded representative
fixture (adjust the three local paths if Steam is installed elsewhere):

```powershell
.\scripts\test-launcher-ui-preview.ps1 `
  -ImportVanillaSavesRoot "C:\Program Files (x86)\Steam\steamapps\workshop\content\2868840\3747503308" `
  -BaseGamePckPath "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck" `
  -SteamworksNetPath "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\Steamworks.NET.dll"
```

This runs one screenshot-free interaction test and fresh desktop-host checks that
the same `ImportVanillaSaves` fixture stays unloaded for Vanilla/disabled selection
and reaches Active when enabled. It reads only the explicit local files and does
not open Steam. Desktop fixtures do not prove Android mod activation or an in-game
effect.

## Optional Desktop UI Preview

Render one fixture and destination when a UI change needs visual inspection:

```powershell
.\scripts\run-launcher-ui-preview.ps1 -Fixture ready -Destination home -Width 1080 -Height 2400 -TouchOptimized $true
```

The image is written under `artifacts/ui-preview/`. This is a manual preview, not a screenshot suite or device-validation substitute. The preview requires Godot 4.5.1 Mono; the script uses the local runtime under `tmp/godot-4.5.1-mono/` by default or accepts `-GodotPath`.

## Building an Android APK

Required local inputs include:

- .NET 9 SDK.
- Android SDK and NDK versions expected by `android/config.gradle`.
- Original game publish files under `upstream/godot-export/`.
- The matching custom Godot engine and licensed third-party runtime inputs.

Use the existing build wrapper so the managed assemblies, Android runtime libraries, and ABI selection stay aligned:

```powershell
.\scripts\build-android-local.ps1 -VersionName "<unique-version>" -VersionCode <higher-version-code> -Abi arm64-v8a
```

The wrapper archives the APK and checksum under `artifacts/android/`. For a later build, choose a unique version name and a version code higher than the installed APK. A successful build or structural APK inspection does not establish phone compatibility, gameplay success, or working Steam save transfer.

Never uninstall, downgrade, clear application data, or change package/signing identity when existing application-private saves must be preserved.

## Project Structure

```text
src/STS2Mobile/
  ModEntry.cs              # Managed entry point
  PatchHelper.cs           # Shared patch utility and logging
  Patches/                 # Android and gameplay Harmony patches
  Launcher/                # Programmatic Godot launcher UI
  Steam/                   # Authentication, download, save transport and sync
android/                   # Android Gradle project and bootstrap assets
scripts/                   # Focused build, test and inspection commands
tools/                     # Small desktop test and preview projects
```

## Other Features

- Steam branch and version selection.
- Early Steam Workshop and mod-launch support.
- Auto, Vulkan, OpenGL, and Safe Start renderer choices.
- LAN multiplayer discovery and manual address entry.
- Android-specific Godot and Harmony compatibility patches.

For LAN multiplayer, both devices must be on the same local network. Add `--fastmp` to the PC game's Steam launch options before connecting from Android.

## Documentation

- [Current Android status](docs/current-android-status.md)
- [v0.2.429 release notes](docs/release-notes/v0.2.429-issue38-arm64-rc4.md)
- [Android troubleshooting](docs/android-troubleshooting.md)
- [Android Workshop mods](docs/android-workshop-mods.md)
- [Steam version selection user guide](docs/steam-version-selection-user-guide.md)
- [Contributing](CONTRIBUTING.md)
- [Changelog](CHANGELOG.md)

## Technical Notes

- Native stubs under `src/stubs/` satisfy desktop-only native dependencies on Android.
- `bootstrap.pck` starts the .NET launcher before downloaded game files exist.
- GodotSharp interop is bootstrapped from `ModEntry.cs`.
- Steam passwords are not stored by the launcher; stored Steam session credentials must never be copied into saves or logs.

## License

This project is licensed under the [MIT License](LICENSE). See [THIRD_PARTY_LICENSES.md](THIRD_PARTY_LICENSES.md) for third-party dependency terms. FMOD and Spine have their own licensing requirements.
