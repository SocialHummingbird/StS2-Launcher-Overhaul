# Android runtime findings

Updated: 2026-10-26

## Runtime boundary

- ARM64 Android is the intended game target.
- Android x86_64 intentionally uses the native fallback path and cannot validate the Godot/.NET launcher or game.
- The launcher owns Steam authentication, game/Workshop download, transactional branch updates, runtime-pack preparation, and game launch.
- Gameplay writes use application-private local storage before network synchronization; automatic and manual save synchronization share one service.

## Authoritative runtime identity

The current selected-game identity is derived from the normalized Steam branch, completed install generation, final installed `SlayTheSpire2.pck` hash, and final installed source `sts2.dll` hash. Both content hashes come from the actual files. A runtime pack, compatibility manifest, validation report, branch marker, or active-cache marker cannot substitute for them.

The selected branch becomes non-launchable before an update mutates installed files. Completed installed files are published as `ready` before runtime-pack preparation begins. Launch still requires a matching validated pack and cache, followed by exact-identity authorization in `current_runtime_slot.json`.

Runtime-pack generation uses unique staging, manifest/report and assembly-hash validation, promotion with backup recovery, and final-path validation. Android independently validates the actual files and prepares the active assembly cache. The source `sts2.dll` is copied unchanged; Harmony applies runtime patches when the game loads.

See [Steam version selection architecture](steam-version-selection-architecture.md).

## Local checks

Supply the [development prerequisites and staged runtimes](development.md#prerequisites) first. Then run:

```powershell
dotnet build src\STS2Mobile\STS2Mobile.csproj -c Release
.\scripts\test-local-gameplay-save-safety.ps1
dotnet run --project tests\STS2Mobile.GameIdentityTests\STS2Mobile.GameIdentityTests.csproj -c Release
gradle -p android testMonoReleaseUnitTest --rerun-tasks
```

Run the launcher/mod test with the explicit fixture paths in [Focused development commands](steam-version-selection-tooling.md#launcher-navigation-and-mod-loading).

For a local APK candidate, follow [Build an APK](development.md#build-an-apk) with your tool paths and existing signing key. Uncached builds require network access. Inspect the result with:

```powershell
.\scripts\verify-android-apk.ps1 `
  -ApkPath "<candidate.apk>" `
  -Abi arm64-v8a
```

Fake Steam remotes and desktop fixtures prove local policy and failure behavior only; they do not prove live Steam transport, Android input/rendering, or an in-game mod effect.

## RC4 device result

[v0.2.429 — Issue #38 ARM64 RC4](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/tag/v0.2.429-issue38-arm64-rc4) passed 10/10 counted launches on Samsung `SM-F971B`, Android 17 / API 37, `arm64-v8a`. The matrix covered four cold, four warm, one background/resume, and one lock/unlock launch. Each attempt validated installed identity, runtime pack, patch compatibility, foreground/focus handoff, and launcher-overlay removal. Saves and unrelated branch/runtime data were unchanged before and after.

This result applies only to that exact APK and device. The original Pixel 9 and Xiaomi 17 Ultra reporters have not yet confirmed RC4, and broad device/GPU/branch/mod/live-Steam coverage is not claimed.
