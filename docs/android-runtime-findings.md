# Android runtime findings

Updated: 2026-08-09

The current source is an unverified save-synchronization candidate. Historical emulator and device results apply only to the exact artifacts that produced them.

## Runtime boundary

- ARM64 Android is the intended game target.
- Android x86_64 intentionally uses the native fallback path and cannot validate the Godot/.NET launcher or game.
- The launcher still owns Steam authentication, game download, runtime preparation, and unconditional Play.
- Gameplay writes use the application-private local save store before any network work.
- Automatic and manual save synchronization share one service.

No current-source Android or live-Steam validation is available.

## Local checks

```powershell
dotnet build src\STS2Mobile\STS2Mobile.csproj -c Release
.\scripts\test-local-gameplay-save-safety.ps1
.\scripts\test-launcher-ui-preview.ps1
```

For an offline APK candidate:

```powershell
.\scripts\build-android-local.ps1 `
  -VersionName "<candidate-version>" `
  -VersionCode <candidate-code> `
  -PackageName "com.sts2launcher.overhaul.fork.local" `
  -Abi arm64-v8a

.\scripts\verify-android-apk.ps1 `
  -ApkPath "<candidate.apk>" `
  -Abi arm64-v8a
```

The save suite uses a small deterministic fake remote. It proves local policy and failure behavior only; it does not prove Steam transport or Android transport. The desktop launcher test does not prove Android rendering, touch, lifecycle, or gameplay.

## Remaining proof

A later device check must identify the exact APK, package, version, signer, hash, device, Android version, and ABI. Real Steam authentication, transfer, game download, launch, save write, and resume remain unverified until exercised on that exact build. One device result does not establish broad compatibility or publication readiness.
