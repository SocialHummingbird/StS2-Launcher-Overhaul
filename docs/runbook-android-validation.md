# Android Validation Runbook

Current posture: automated and API 36 x86_64 emulator evidence are pre-hardware gates, not substitutes for ARM64 validation. The published ARM64 baseline is exact `v0.2.416` on Samsung `SM-F966B`; current unreleased native-routing changes require a new exact-candidate ARM64 pass. See [current Android status](current-android-status.md).

This runbook is used for manual verification of startup and reliability changes where full automation is not available.

## Scope

- New startup patches, platform behavior, locale behavior, and optional patch groups
- Steam login/authentication and ownership checks
- Cloud sync lifecycle behavior changes
- UI/layout and launcher interaction updates
- Multiplayer/cloud-dependent changes

## Evidence Labels

Label every result as one of:

- `automated`: compilation, Java tests, Gradle, APK inspection, fixtures, fake loaders/stores, or temporary files only;
- `emulator-x86_64`: visible API 36 emulator evidence limited to native routing, fallback, lifecycle, input, and diagnostics;
- `hardware-arm64`: exact APK installed on a named ARM64 device with device metadata, recording/screenshots, logs, and installed-artifact hash; or
- `unvalidated`: required path not exercised on the exact candidate.

Never use `emulator-x86_64` as evidence for the managed launcher, Steam services, ARM64 runtime, `NMainMenu`, or gameplay.

## Prerequisites

- Device with fresh log access: `adb` installed and authorized
- Installed APK/package you are validating
- Expected branch/commit and any feature flags noted
- `docs/device-log-checklist.md` completed

## Representative Device Matrix

Run at least one device from each row for patch-level changes:

- Pixel / Android 16 + default locale
- Pixel / Android 17 + PowerVR/OpenGL ES compatibility path, if available
- Samsung Galaxy S25/S26 class + non-US locale, such as Korean or locale-extension-heavy settings
- Samsung Fold / One UI with locale extensions enabled
- Mid-tier Android 12-13 fallback device, for smoke coverage on older API levels

## Standard Procedure

1. Verify the current published ARM64 APK:

```powershell
.\scripts\verify-android-release-apk.ps1 `
  -ReleaseTag "v0.2.416-startup-recovery-ime" `
  -AssetName "StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk" `
  -Abi arm64-v8a
```

2. Install/update the verified APK on the target device only after its save bytes have been exported and verified, or on a disposable device that has no preservation data. Never clear app data on a tester's `.local` install. For the current published ARM64 release:

```powershell
.\scripts\install-android-release.ps1 `
  -ReleaseTag "v0.2.416-startup-recovery-ime" `
  -AssetName "StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk"
```

Launch and diagnostics capture are separate, deliberate steps after preservation. For the Stage 5 save matrix, follow [Android release validation](android-release-validation.md); do not use this general runtime procedure as a substitute.

3. If the APK is already installed and only runtime evidence is needed:

```powershell
.\scripts\capture-android-diagnostics.ps1 -Launch -ClearLogcat -WaitSeconds 15
```

4. Capture baseline metadata.

The scripts above write metadata to `artifacts/android/phone-diagnostics-*`. If collecting manually, include:

- Device model
- Android version / security patch
- ABI
- GPU model and active renderer/backend from Godot logs
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
- [ ] Native splash starts with the Godot mark, transitions to the StS2 Launcher identity, and does not stretch or reveal an adaptive-icon square
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
- [ ] If the target is Pixel / PowerVR / OpenGL ES, main-menu startup survives at least the 1s, 3s, 10s, 30s, and 60s post-startup markers
- [ ] On PowerVR/OpenGL Compatibility, the engine includes the all-PowerVR transform-feedback shader-cache workaround or the release notes explicitly state that issue #34 remains open
- [ ] Focused logs do not show Godot static-string cleanup immediately after unsupported texture-format conversion warnings
- [ ] Locked-screen or Android focus interruption is not misreported as a game crash
- [ ] Normal diagnostics avoid missing-path log floods; verbose save diagnostics remain opt-in

## Current-Source ARM64 Gate

The API 36 x86_64 Stage 5 pass does not complete these items:

- [ ] Build one exact ARM64 candidate from current source and record commit, version, versionCode, package, ABI, signer, and SHA-256
- [ ] Pull installed `base.apk` and prove its hash matches the candidate
- [ ] Cold and cached starts show no blank/white/accidental-black frame
- [ ] Full managed Godot-to-StS2 Launcher transition completes and launcher controls accept touch after cleanup
- [ ] Normal and Safe Start skip routing remains exact-once
- [ ] Forced assembly-bootstrap failure reaches one native diagnostics activity and preserves the active cache
- [ ] **Restart launcher** clears pending launch state and returns once without fallback loops
- [ ] Rotation, Home/resume, and lock-screen return show no duplicate activity, keyboard intrusion, blocked control, fatal exception, ANR, or timeout
- [ ] Public Start Game reaches real `NMainMenu` and records post-startup markers
- [ ] Public-beta and vanilla/modded paths are either validated or explicitly marked unvalidated
- [ ] Cloud Pull progress and blocked/unblocked Upload eligibility are clear; no real Push is run without separate explicit authorisation and controlled backups

## Evidence format for PR comments/issues

Include:

- Evidence label: `automated`, `emulator-x86_64`, `hardware-arm64`, or `unvalidated`
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
  - `PostStartupTrace`
  - `PostStartupHeartbeat`
  - `Native lifecycle event`
- Timestamped failure window and symptom duration

## Escalation

If the same fatal signal repeats on the same device class twice in the same runbook session:

- Open a new issue using the bug template
- Link this runbook and attach logs
- Note the last successful checklist entry before regression
