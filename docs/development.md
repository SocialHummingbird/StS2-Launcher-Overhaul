# Building and testing

These instructions are for working on the launcher source. For installation, see [Getting started](getting-started.md). Run commands from the repository root.

## Prerequisites

The Windows build uses PowerShell 7, Git for Windows (including `sh.exe`), the .NET 9 SDK, and Python 3.12.10. The Harmony helper obtains its separate .NET 10.0.100 SDK when needed.

For Android checks and APK builds, install JDK 17, Gradle 8.14.3, and the SDK components in [android/config.gradle](../android/config.gradle): Android SDK 35, build-tools 35.0.0, and NDK 28.1.13356709. Set `JAVA_HOME` and `ANDROID_HOME`, and put Gradle on `PATH`. The commands below use installed Gradle because `android/gradlew.bat` is not tracked in this repository.

Several required inputs are excluded from Git and must be supplied separately:

- Original game publish files under `upstream/godot-export/.godot/mono/publish/arm64/`, including `sts2.dll` and the matching `GodotSharp.dll`.
- The matching Android .NET 9.0.7 BCL already staged under `android/assets/dotnet_bcl/`. In particular, `System.Private.CoreLib.dll` must have the exact version and SHA-256 pinned in [patches/mono/runtime.json](../patches/mono/runtime.json). The Mono helper checks that file before building; it does not populate the BCL directory.
- For APKs, the matching custom Godot 4.5.1 Mono Android AAR/native libraries and licensed FMOD inputs. [setup-godot-source.ps1](../scripts/setup-godot-source.ps1) and [build-godot.ps1](../scripts/build-godot.ps1) prepare and build the patched engine; they do not provision the whole game/BCL/third-party bundle. Godot setup installs hash-pinned SCons from [requirements-godot-build.txt](../scripts/requirements-godot-build.txt).
- For APKs, an existing signing keystore, alias, and password. The wrapper defaults to `tmp/localtest.keystore`; it does not create that file. An in-place update requires the installed app's package and signing identity.

The build helpers need network access for uncached source, SDK/tool archives, NuGet packages, and native runtime packages. A fresh clone is not a self-contained offline build.

`build-android-mono.ps1` accepts `-AndroidHome` and `-PythonPath`. `build-android-harmony.ps1` accepts `-DotNetPath` for an already installed pinned SDK. The APK wrapper accepts `-AndroidHome`, `-JavaHome`, `-GradlePath`, and the keystore parameters shown below; its default tool locations are specific to the original local build environment.

## Tests

Stage the paired native and managed runtimes before the first managed build:

```powershell
.\scripts\build-android-mono.ps1 -AndroidHome $env:ANDROID_HOME -Stage
.\scripts\build-android-harmony.ps1 -Stage
```

The managed assembly embeds both build manifests, including the native library
hash, so changing the launcher or shared runtime invalidates historical mod
results automatically. The APK wrapper builds and stages the native runtime
before Harmony and publishes only after their pairing has been verified.

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

The same probe can reproduce the Android Mono native-host GC wait and verify
the corrected transition using the staged runtime on a connected ARM64 phone:

```powershell
.\scripts\test-asset-preload.ps1 -AndroidMonoTransition `
  -AdbPath "$env:ANDROID_HOME\platform-tools\adb.exe" `
  -ClangPath "$env:ANDROID_NDK_ROOT\toolchains\llvm\prebuilt\windows-x86_64\bin\clang++.exe" `
  -DeviceSerial "<adb-serial>"
```

This writes a temporary fixture under `/data/local/tmp`, without changing app
data. It requires the original case to time out and the repaired case to finish
64 collections with callbacks preserved. APK integration still requires the
physical launch procedure in [Android status](current-android-status.md).

[The runtime recipe](../patches/harmony/runtime.json) pins SDK 10.0.100,
Harmony's source commit and patched source tree. The helper downloads and checks
the SDK archive when needed, restores locked packages from NuGet, rejects
edited cached source, and rebuilds outputs whose recorded hashes change.
Harmony targets .NET 9; its vendored MonoMod projects target .NET 8. The Android build wrapper runs this
step before publishing the launcher and packages the build provenance alongside
the assemblies. It preserves the source game DLL's bytes and member visibility.

[The native recipe](../patches/mono/runtime.json) pins .NET 9.0.7 source, the matching CoreLib bytes, NDK 28.1.13356709, Python 3.12.10, and checked CMake/Ninja archives. The helper builds native Mono against the existing BCL for ARM64 and/or x86_64. It rejects edited cached source and records compiler flags and library hashes. The patches address method preparation, required initialization, assembly access/context, and instruction-cache maintenance; the isolated contract below checks these behaviors on the staged ARM64 runtime.

Run the shared patching contract on the staged Android Mono runtime:

```powershell
.\scripts\test-asset-preload.ps1 -AndroidModRuntime `
  -AdbPath "$env:ANDROID_HOME\platform-tools\adb.exe" `
  -ClangPath "$env:ANDROID_NDK_ROOT\toolchains\llvm\prebuilt\windows-x86_64\bin\clang++.exe" `
  -DeviceSerial "<adb-serial>"
```

The isolated ARM64 fixture checks private access and visibility, local-variable
IL, branches, exceptions and fault/finally handlers, Harmony patch forms,
original assembly context across ordinary and reverse patches, deferred
constructor and method preparation, required field/type/module initialization,
constructor arguments/failures, cached failures, shared generic code, and
reverse calls before and after GC from cached/fresh callers, reflection,
delegates and managed function pointers. It also checks that detours preserve
initialization before prefixes and unpatching restores the original body.
Ordinary cross-assembly fixtures compare matching/mixed-case attributes,
unrelated targets, absent attributes, inherited access and `InternalsVisibleTo`
while checking that reflection visibility remains unchanged.
Its result is written to `artifacts/android-mod-runtime-contract.log`. These
runtime checks do not establish game startup or any mod's in-game effect.

Run the Android/JVM suite with the configured JDK 17 and Android SDK:

```powershell
gradle -p android testMonoReleaseUnitTest --rerun-tasks
```

If generated outputs contain unavailable OneDrive placeholders, a task-local Gradle init script can direct outputs to a local cache. Keep it outside production build dependencies; source, inputs and signing stay unchanged.

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

Run the existing two-mod fixture separately:

```powershell
.\scripts\run-launcher-ui-preview.ps1 -ModRuntimeTest -ModRuntimeScenario stage9-chain `
  -BaseLibRoot "C:\Program Files (x86)\Steam\steamapps\workshop\content\2868840\3737335127" `
  -ImportVanillaSavesRoot "C:\Program Files (x86)\Steam\steamapps\workshop\content\2868840\3747503308" `
  -BaseGamePckPath "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck" `
  -SteamworksNetPath "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\Steamworks.NET.dll"
```

## Preview the launcher

Render one fixture and destination when a UI change needs visual inspection:

```powershell
.\scripts\run-launcher-ui-preview.ps1 -Fixture ready -Destination home -Width 1080 -Height 2400 -TouchOptimized $true
```

The image is written under `artifacts/ui-preview/`. The preview requires Godot 4.5.1 Mono; the script uses the local runtime under `tmp/godot-4.5.1-mono/` by default or accepts `-GodotPath`.

## Build an APK

After supplying the [prerequisites](#prerequisites), use the build wrapper so the managed assemblies, Android runtime libraries, and ABI selection stay aligned. Pass your local tool and signing inputs explicitly:

```powershell
.\scripts\build-android-local.ps1 `
  -VersionName "<unique-version>" -VersionCode <higher-version-code> -Abi arm64-v8a `
  -AndroidHome $env:ANDROID_HOME -JavaHome $env:JAVA_HOME `
  -GradlePath (Get-Command gradle.bat).Source `
  -KeystorePath "<existing-keystore>" -KeystoreAlias "<alias>" -KeystorePassword "<password>"
```

The wrapper archives the APK and checksum under `artifacts/android/`. For a later build, choose a unique version name and a version code higher than the installed APK. A successful build or structural APK inspection does not establish phone compatibility, gameplay success, or working Steam save transfer.

Never uninstall, downgrade, clear application data, or change package/signing identity when existing application-private saves must be preserved.

Check in-place update compatibility against the retained previous APK:

```powershell
.\scripts\verify-android-update-compat.ps1 -ApkPath "<candidate.apk>" -PreviousApkPath "<retained.apk>"
```

Current ownership is in [the reduction inventory](../REDUCTION_INVENTORY.md). Build-specific results belong in [Android testing status](current-android-status.md).

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

