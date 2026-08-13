# Focused development commands

Use the smallest command that answers the current question.

## Build

```powershell
dotnet build src/STS2Mobile/STS2Mobile.csproj -c Release
```

## Save, synchronization, and launch behavior

```powershell
.\scripts\test-local-gameplay-save-safety.ps1
```

This runs twelve focused desktop behaviors: local path containment; atomic local writes; the four deterministic synchronization decisions; truthful save-status presentation; interrupted Pull preserving local saves; failed Push staying dirty and retryable; Pull-before-save-load ordering; a gameplay save queuing Push without returning to the launcher; manual Push/Pull through the same synchronization service; persisted mod selection with validated manifest discovery; persisted game-version selection reaching the matching launch-readiness path; and the restored Android startup handoff staying alive through main-menu preparation.

The policy tests use one in-memory `FakeSaveRemote`. It does not prove Steam Cloud or Android transport.

## Launcher navigation and mod activation

```powershell
.\scripts\test-launcher-ui-preview.ps1 `
  -ImportVanillaSavesRoot "C:\Program Files (x86)\Steam\steamapps\workshop\content\2868840\3747503308" `
  -BaseGamePckPath "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck" `
  -SteamworksNetPath "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\Steamworks.NET.dll"
```

This reads one already-downloaded `ImportVanillaSaves` fixture without opening
Steam. It runs one screenshot-free interaction test plus fresh desktop processes
for Vanilla, disabled, and enabled selection. Desktop fixtures do not prove
Android mod activation or an in-game effect.

## Deferred two-mod device-acceptance preflight

Before the one-device journey, the exact BaseLib plus importer chain can be
checked once in a fresh desktop Godot process:

```powershell
.\scripts\run-launcher-ui-preview.ps1 `
  -ModRuntimeTest `
  -ModRuntimeScenario stage9-chain `
  -BaseLibRoot "C:\Program Files (x86)\Steam\steamapps\workshop\content\2868840\3737335127" `
  -ImportVanillaSavesRoot "C:\Program Files (x86)\Steam\steamapps\workshop\content\2868840\3747503308" `
  -BaseGamePckPath "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck" `
  -SteamworksNetPath "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\Steamworks.NET.dll"
```

This opt-in check requires exactly those two selected mods, concrete Harmony
activation for both, and a durable result of BaseLib `Partial` plus
ImportVanillaSaves `Active`. It does not prove Android activation, the importer
control, the modded save namespace, or an in-game effect; those remain the
mandatory one-device acceptance.

When one desktop screenshot is useful for UI work, render only that state and destination:

```powershell
.\scripts\run-launcher-ui-preview.ps1 -Fixture ready -Destination home -Width 1080 -Height 2400 -TouchOptimized $true
```

The preview is optional and is not a publication gate.
