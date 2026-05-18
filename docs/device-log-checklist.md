# Device Log Checklist

Use this checklist before filing overhaul issues that involve crashes, stutters, or startup failures.

## Required Capture
- Device model + OS + Android version
- App version / package version
- Exact issue context (clean install, updated install, locale, cloud-sync state)
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
- `Locale` / `CultureInfo` / `GetThreeLetterLanguageCode`
- `ApplyLocaleFontSubstitution`
- `Cloud` / `Flush`
- `NGame` lifecycle events

## Evidence Requirements for Issue
- Attach first 200-300 lines around the primary exception
- Attach at least one 30-second sample around the failure window
- Include device locale and region settings

## Optional but Useful
- Screenshot of launcher/overlay state
- Game save-state or repro save file (if reproducible)
- Whether cloud sync was enabled at startup
