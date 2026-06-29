# Device Log Checklist

Current posture: collect logs for a working ARM64 baseline plus remaining hardening gates. Prioritize startup freshness, assembly cache expectedSource/expectedBytes, cloud Pull/Push evidence, save handoff, crash markers, and release package/version identity. See [current Android status](current-android-status.md).

Use this checklist before filing overhaul issues that involve crashes, stutters, startup failures, branch/runtime mismatches, cloud-save problems, or mod-loading failures. See [Issue reporting](issue-reporting.md) for template selection and redaction rules.

## Required Capture
- Device model + OS + Android version
- Device ABI
- App version / package version
- Release tag / APK asset when testing a GitHub release
- Package name and versionCode when available
- Exact issue context (clean install, updated install, locale, cloud-sync state)
- Selected Steam branch and whether mods were enabled
- Whether Pull from Cloud ran and whether Push to Cloud ran
- Timestamped sequence:
  - app launch -> launcher screen -> login/download/launch -> failure point

## Log Capture (Android)
1. Start a fresh `adb logcat` stream:

```bash
adb logcat > sts2launcher-logcat.txt
```

2. Reproduce once to reproduce the issue.
3. Stop stream and search for:
- `Mono` / `Unhandled` / `Exception`
- `PatchHelper` lines
- `Steam` / `SteamKit` authentication lines
- `Locale` / `CultureInfo` / `GetThreeLetterLanguageCode`
- `ApplyLocaleFontSubstitution`
- `Cloud` / `Flush`
- `Pull` / `Push`
- `NativeFallback`
- `Workshop` / `Mods`
- `Runtime pack` / `PCK`
- `NGame` lifecycle events

## Evidence Requirements for Issue
- Attach first 200-300 lines around the primary exception
- Attach at least one 30-second sample around the failure window
- Include device locale and region settings
- Include whether Steam login reached Steam Guard, authentication success, ownership verification, or failed earlier
- Include selected branch, PCK path/hash, runtime pack path/hash, active `sts2.dll` hash, runtime cache marker, and patch validation marker for branch/startup reports when available
- Include selected mods, mod sources, and save visibility/load result for Workshop or SavesMerger reports

## Optional but Useful
- Screenshot of launcher/overlay state
- Game save-state or repro save file (if reproducible)
- Whether cloud sync was enabled at startup
