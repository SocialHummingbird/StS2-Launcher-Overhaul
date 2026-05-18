# Android Validation Runbook

This runbook is used for manual verification of startup and reliability changes where full automation is not available.

## Scope

- New startup patches (platform, locale, optional patch groups)
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
- Samsung Galaxy S25/S26 class + non-US locale (Korean or locale extension heavy)
- Samsung Fold / One UI with locale extensions enabled
- Mid-tier Android 12–13 fallback device (smoke coverage for older API)

## Standard Procedure

1. Install/update test build on target device.
2. Capture baseline metadata:
   - Device model
   - Android version / security patch
   - Locale + region settings
   - App branch + commit hash
3. Start log capture:

```bash
adb logcat > sts2launcher-<device>-<date>.log
```

4. Repro the target flow once only (avoid duplicate noise):
   - Launch app from home screen
   - Open launcher
   - Trigger patch area (where applicable): cloud sync, locale switch, multiplayer join, etc.
5. Stop log capture and collect:
   - First 300 lines around first failure
   - Final 120 lines of session
   - Any black-screen/retry overlay screenshots

## Pass/Fail Checklist

### Startup path

- [ ] Launcher opens without immediate dev/command overlay
- [ ] Game logo/menu appears within expected window for device class
- [ ] No repeated `NullReferenceException` or locale parsing loop
- [ ] No crash stack repeatedly referencing startup patch entry points

### Cloud path

- [ ] Initial sync does not stall indefinitely
- [ ] Large cloud backlog remains bounded and progresses/logs progress
- [ ] No repeated write-queue timeouts on background/resume

### UI/layout path

- [ ] Main layout loads without `NullReferenceException` spikes in same frame
- [ ] Menu/buttons remain interactive in initial scene

## Evidence format for PR comments/issues

- Include:
  - Device matrix row used
  - Repro steps and whether a clean install was used
  - Log excerpt block names:
    - `PatchHelper`
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
