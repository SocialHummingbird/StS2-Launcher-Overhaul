# Focused development commands

Run these commands from the repository root after completing the [development setup](development.md). The examples assume the default Steam installation path; adjust it for your machine and use game files that match the prepared runtime.

## Build

```powershell
dotnet build src/STS2Mobile/STS2Mobile.csproj -c Release
```

## Save, synchronization, and launch behavior

```powershell
.\scripts\test-local-gameplay-save-safety.ps1
```

The probe runs twelve desktop scenarios covering:

- Local path containment and atomic save writes.
- Synchronization decisions and status, interrupted save downloads, failed uploads, and manual transfers.
- Save checks before loading and uploads queued by gameplay saves.
- Persisted mod and game-version selection, launch readiness, and the startup guard staying alive through main-menu preparation.

The policy tests use one in-memory `FakeSaveRemote`. It does not prove Steam Cloud or Android transport.

## Runtime identity, branch update, and launcher handoff

```powershell
dotnet run --project tests\STS2Mobile.GameIdentityTests\STS2Mobile.GameIdentityTests.csproj -c Release
gradle -p android testMonoReleaseUnitTest --rerun-tasks
```

The managed suite checks game-file identity, PCK cache reuse, replacing a branch with a newer build, interruption recovery, stale results, runtime-pack validation and rollback, selected-branch cleanup, and isolation of saves, credentials, and other branches. It also checks that handoff events belong to the current launch attempt. Android unit tests cover the native runtime-pack and handoff contracts. These checks do not replace physical-device validation.

## Launcher navigation and mod loading

```powershell
.\scripts\test-launcher-ui-preview.ps1 `
  -ImportVanillaSavesRoot "C:\Program Files (x86)\Steam\steamapps\workshop\content\2868840\3747503308" `
  -BaseGamePckPath "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck" `
  -SteamworksNetPath "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\Steamworks.NET.dll"
```

This uses an already-downloaded `ImportVanillaSaves` fixture without opening Steam. It tests navigation in phone and desktop layouts, then starts separate Godot processes for Vanilla, disabled, and enabled selection. The enabled scenario checks original discovery and loading, mounted resources, the importer's C# scene, its save-path patch, and saved results for the selected game and mod files. These desktop checks do not prove Android compatibility or an in-game effect on a phone.

## BaseLib and ImportVanillaSaves desktop check

Check the two-mod setup in a fresh desktop Godot process:

```powershell
.\scripts\run-launcher-ui-preview.ps1 `
  -ModRuntimeTest `
  -ModRuntimeScenario stage9-chain `
  -BaseLibRoot "C:\Program Files (x86)\Steam\steamapps\workshop\content\2868840\3737335127" `
  -ImportVanillaSavesRoot "C:\Program Files (x86)\Steam\steamapps\workshop\content\2868840\3747503308" `
  -BaseGamePckPath "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck" `
  -SteamworksNetPath "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\Steamworks.NET.dll"
```

This check requires original game discovery of both mods, Loaded results with no errors, mounted resources, a working importer C# scene and patch, and a saved result tied to the selected game assembly. It does not establish Android loading, the import button's effect, or save behavior on a phone. See the [validation runbook](runbook-android-validation.md) and [current testing status](current-android-status.md) for device requirements and results.

When one desktop screenshot is useful for UI work, render only that state and destination:

```powershell
.\scripts\run-launcher-ui-preview.ps1 -Fixture ready -Destination home -Width 1080 -Height 2400 -TouchOptimized $true
```

The preview is optional and is not a publication gate.
