# Building and testing

These instructions are for working on the launcher source. For installation, see [Getting started](getting-started.md). Run commands from the repository root.

## Tests

Build the managed project:

```powershell
dotnet build src\STS2Mobile\STS2Mobile.csproj -c Release
```

Run the focused save and launch probe:

```powershell
.\scripts\test-local-gameplay-save-safety.ps1
```

The twelve scenarios cover saves, sync ordering, mod and version selection, and startup. They use a simulated remote service; test real Steam Cloud transfers separately.

Run the managed regression suite:

```powershell
dotnet run --project tests\STS2Mobile.GameIdentityTests\STS2Mobile.GameIdentityTests.csproj -c Release
```

Check the Android asset-preload patch against the actual game loading class and
synthetic threaded resources in desktop Godot:

```powershell
.\scripts\test-asset-preload.ps1 -VerifyBaseline
```

For emulator startup/input checks, see
[the responsiveness investigation](startup-responsiveness-2026-09-07.md) and
[the isolated Android harness](../tools/AndroidStartupHarness/README.md).

Run the focused launcher/mod test against the already-downloaded representative
fixture (adjust the three local paths if Steam is installed elsewhere):

```powershell
.\scripts\test-launcher-ui-preview.ps1 `
  -ImportVanillaSavesRoot "C:\Program Files (x86)\Steam\steamapps\workshop\content\2868840\3747503308" `
  -BaseGamePckPath "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck" `
  -SteamworksNetPath "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\Steamworks.NET.dll"
```

These checks use local game and mod files in a fresh desktop Godot process. They test whether the importer stays off in Vanilla/disabled mode and activates when enabled. Android loading and the in-game effect need separate device checks.

## Preview the launcher

Render one fixture and destination when a UI change needs visual inspection:

```powershell
.\scripts\run-launcher-ui-preview.ps1 -Fixture ready -Destination home -Width 1080 -Height 2400 -TouchOptimized $true
```

The image is written under `artifacts/ui-preview/`. The preview requires Godot 4.5.1 Mono; the script uses the local runtime under `tmp/godot-4.5.1-mono/` by default or accepts `-GodotPath`.

## Build an APK

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

## Source layout

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

