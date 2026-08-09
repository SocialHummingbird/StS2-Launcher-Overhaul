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

This runs eight focused desktop behaviors: local path containment; atomic local writes; the four deterministic synchronization decisions; interrupted Pull preserving local saves; failed Push staying dirty and retryable; Pull-before-save-load ordering; a gameplay save queuing Push without returning to the launcher; and manual Push/Pull through the same synchronization service.

The policy tests use one in-memory `FakeSaveRemote`. It does not prove Steam Cloud or Android transport.

## Launcher navigation and actions

```powershell
.\scripts\test-launcher-ui-preview.ps1
```

This runs one screenshot-free interaction smoke for Home, Saves, Versions, Mods, and Help plus representative action callbacks. It does not prove Android rendering, touch behavior, Android transport, or real Steam transport.

When one desktop screenshot is useful for UI work, render only that state and destination:

```powershell
.\scripts\run-launcher-ui-preview.ps1 -Fixture ready -Destination home -Width 1080 -Height 2400 -TouchOptimized $true
```

The preview is optional and is not a publication gate.
