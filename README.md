# StS2 Launcher

<p align="center">
  <img src="docs/assets/sts2-mobile-icon.svg" alt="StS2 Launcher icon" width="128" height="128">
</p>

## What This App Is

StS2 Launcher is an unofficial Android launcher for people who own Slay the Spire 2 on Steam. It does not include the game, its assets, saves, or Workshop content. After Steam confirms ownership, the launcher can download the owner's game files and attempt to run them on ARM64 Android hardware.

This project is not made, approved, sponsored, or supported by Mega Crit Games, Steam, or Valve. See the [unofficial project notice](docs/unofficial-project-notice.md).

## Current Status

The current source is an **unverified save-synchronization and mod-loading candidate**. It builds and its focused desktop tests pass. A limited one-device run of `0.2.425` showed both selected mods loading and activating, the modded save namespace being used, and the game reaching the main menu. A later freeze/crash prevented the importer control and relaunch result from being verified, so the candidate is published only as an unverified prerelease and must not be called phone-ready or release-ready.

- Package name: `com.sts2launcher.overhaul.fork.local`
- Target hardware: ARM64 Android
- Android emulator and x86_64 paths are diagnostic only.
- Branch selection, Workshop mods, renderer compatibility, and device coverage remain experimental.
- The current ARM64 prerelease candidate is `0.2.425-mod-chain-fix2-unverified`; it uses the existing local-test package and signer lineage.

Android gameplay saves live in application-private storage. Clearing application data or uninstalling the app removes those local files. See [Current Android status](docs/current-android-status.md#save-data) before using a build with saves that matter.

## Current Product Boundary

The launcher retains:

- Steam authentication and encrypted credential storage.
- Steam game download and version selection.
- Unconditional game launch after a bounded synchronization attempt.
- One application-local gameplay save store with atomic local writes.
- One Steam Cloud transport and one synchronization service shared by automatic and manual operations.
- Automatic Pull before the game loads saves and queued Push after committed gameplay saves.
- Manual **Sync now**, **Pull from Steam**, and **Push to Steam** actions.
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

It runs nine focused desktop behaviors:

- Local path containment.
- Atomic local writes.
- The four deterministic synchronization decisions.
- Interrupted Pull preserving local saves.
- Failed Push staying dirty and retryable.
- Pull completing before save loading.
- A gameplay save queuing Push without returning to the launcher.
- Manual Push and Pull using the same synchronization service.
- Persisted mod selection and validated manifest discovery surviving a simulated restart.

The synchronization tests use one in-memory `FakeSaveRemote`. That fake proves deterministic policy behavior only; it does not prove Steam Cloud or Android transport.

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
.\scripts\build-android-local.ps1 -VersionName "0.2.425-mod-chain-fix2-unverified" -VersionCode 425000 -Abi arm64-v8a
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
