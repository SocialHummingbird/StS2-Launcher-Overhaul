# Android Validation Runbook

Current posture: validate regressions against the working ARM64 baseline, then collect evidence for remaining hardening gates. The baseline is fresh install, Steam download, Pull from Cloud, Push hardening, Android local save handoff, game launch, and adaptive launcher/loading screen behavior. See [current Android status](current-android-status.md).

This runbook is used for manual verification of startup and reliability changes where full automation is not available.

## Scope

- New startup patches, platform behavior, locale behavior, and optional patch groups
- Steam login/authentication and ownership checks
- Cloud sync lifecycle behavior changes
- UI/layout and launcher interaction updates
- Multiplayer/cloud-dependent changes

## Prerequisites

- Device with fresh log access: `adb` installed and authorized
- Installed APK/package you are validating
- Expected branch/commit and any feature flags noted
- `docs/device-log-checklist.md` completed

## Representative Device Matrix

Run at least one device from each row for patch-level changes:

- Pixel / Android 16 + default locale
- Samsung Galaxy S25/S26 class + non-US locale, such as Korean or locale-extension-heavy settings
- Samsung Fold / One UI with locale extensions enabled
- Mid-tier Android 12-13 fallback device, for smoke coverage on older API levels

## Standard Procedure

1. Verify the current published ARM64 APK:

```powershell
.\scripts\verify-android-release-apk.ps1 `
  -ReleaseTag "v0.2.184-loading-scale" `
  -AssetName "StS2Launcher-v0.2.184-loading-scale-arm64-v8a.apk" `
  -Abi arm64-v8a
```

2. Install/update the verified APK on target device. For the current published ARM64 release:

```powershell
.\scripts\install-android-release.ps1 `
  -ReleaseTag "v0.2.184-loading-scale" `
  -AssetName "StS2Launcher-v0.2.184-loading-scale-arm64-v8a.apk" `
  -ClearAppData `
  -Launch `
  -CaptureDiagnostics
```

3. If the APK is already installed and only runtime evidence is needed:

```powershell
.\scripts\capture-android-diagnostics.ps1 -Launch -ClearLogcat -WaitSeconds 15
```

4. Capture baseline metadata.

The scripts above write metadata to `artifacts/android/phone-diagnostics-*`. If collecting manually, include:

- Device model
- Android version / security patch
- ABI
- Locale + region settings
- App version, release tag, APK asset, branch, and commit hash
- Clean install or update install

5. Start log capture if not using the scripts:

```bash
adb logcat > sts2launcher-<device>-<date>.log
```

6. Repro the target flow once only to avoid duplicate noise:

- Launch app from home screen
- Open launcher
- Trigger patch area where applicable: Steam login, cloud sync, locale switch, multiplayer join, download, or game launch

7. Stop log capture and collect:

- First 300 lines around first failure
- Final 120 lines of session
- Any black-screen, retry overlay, or login failure screenshots

## Pass/Fail Checklist

### Startup path

- [ ] Launcher opens without immediate dev/command overlay
- [ ] Native splash uses the launcher icon and does not stretch/distort on the target display
- [ ] Loading/warmup/startup status surfaces fit inside safe screen margins
- [ ] No `.NET assemblies not found` alert
- [ ] `Assembly cache diagnostics` shows `arm64` and required DLLs present on ARM64 phone
- [ ] Steam login reaches authentication success or ownership verification
- [ ] Game logo/menu appears within expected window for device class
- [ ] No repeated `NullReferenceException` or locale parsing loop
- [ ] No crash stack repeatedly referencing startup patch entry points

### Cloud path

- [ ] Initial sync does not stall indefinitely
- [ ] Large cloud backlog remains bounded and progresses/logs progress
- [ ] No repeated write-queue timeouts on background/resume
- [ ] Pull from Cloud downloads real Steam Cloud files and writes Android local saves
- [ ] Game startup reads the same Android local save paths populated by Pull
- [ ] Push to Cloud shows confirmation before upload
- [ ] Cancel/no-confirm Push path does not start upload
- [ ] Confirmed Push is validated only when it is safe to mutate Steam Cloud state, or explicitly deferred with risk noted

### UI/layout path

- [ ] Main layout loads without `NullReferenceException` spikes in same frame
- [ ] Menu/buttons remain interactive in initial scene

### Release hardening path

- [ ] Fresh install proves runtime freshness with current assembly schema logs
- [ ] Upgrade install advances package update time and does not reuse stale managed assemblies
- [ ] Successful game startup hides launcher recovery controls quickly
- [ ] Locked-screen or Android focus interruption is not misreported as a game crash
- [ ] Normal diagnostics avoid missing-path log floods; verbose save diagnostics remain opt-in

## Evidence format for PR comments/issues

Include:

- Device matrix row used
- Repro steps and whether a clean install was used
- Release tag, APK asset, package name, and ABI
- Log excerpt block names:
  - `PatchHelper`
  - `Steam` / `SteamKit`
  - `Loc` / `CultureInfo`
  - `FontSubstitution`
  - `Cloud`
  - `Lifecycle`
- Timestamped failure window and symptom duration

## Escalation

If the same fatal signal repeats on the same device class twice in the same runbook session:

- Open a new issue using the bug template
- Link this runbook and attach logs
- Note the last successful checklist entry before regression
