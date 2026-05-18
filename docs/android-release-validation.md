# Android release validation checklist

Use this checklist after every release run (manual or tag-triggered) to confirm artifact publication before announcing a release.

## 1) Verify workflow outcome

1. In GitHub Actions, open the latest **Android Release** run.
2. Confirm both jobs completed:
   - `Build Android APK`
   - `Publish GitHub release`
3. Confirm the output APK path is present in logs:
   - `android/build/outputs/apk/mono/release/StS2Launcher-v<version>.apk`

## 2) Verify published release assets

1. Open the release page for the tag (for example `v0.2.0`).
2. Confirm at least one APK asset exists with name pattern:
   - `StS2Launcher-v<version>.apk`
3. Confirm checksum file exists:
   - `StS2Launcher-v<version>.apk.sha256`
4. Confirm the release body includes generated release notes.

## 3) Optional checksum verification (local)

```bash
sha256sum -c StS2Launcher-v<version>.apk.sha256
```

Expected output:

```text
StS2Launcher-v<version>.apk: OK
```

## 4) Install validation on device

```bash
adb install -r StS2Launcher-v<version>.apk
```

1. Start app and confirm launcher UI appears.
2. Confirm no immediate crash on cold start.

## 5) Archive and follow-up

- Record any failures in the release PR or issue tracker.
- Fix root cause before creating the next tag.
- Add a short note to `OVERHAUL_STATUS.md` if a process step changes.

