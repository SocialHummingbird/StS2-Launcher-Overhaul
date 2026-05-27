# Android release validation checklist

Use this checklist after every release run (manual or tag-triggered) to confirm artifact publication before announcing a release.

## 1) Verify workflow outcome

1. In GitHub Actions, open the latest **Android Release** run.
2. Confirm both jobs completed:
   - `Build Android APK`
   - `Publish GitHub release`
3. Confirm the `Verify release update compatibility` step completed.
4. Confirm the output APK path is present in logs:
   - `android/build/outputs/apk/mono/release/StS2Launcher-v<version>.apk`

## 2) Verify update guardrails

1. Confirm repository secrets are configured:
   - `ANDROID_RELEASE_KEYSTORE_BASE64`
   - `ANDROID_RELEASE_KEYSTORE_PASSWORD`
   - `ANDROID_RELEASE_KEY_ALIAS`
2. Confirm repository variable `ANDROID_RELEASE_SIGNER_SHA256` is configured to the stable release certificate SHA-256 fingerprint.
3. Audit the current repository setup:

```powershell
.\scripts\check-android-release-readiness.ps1
```

4. If these are missing or stale, configure them from the stable release keystore:

```powershell
.\scripts\configure-android-release-signing.ps1 `
  -KeystorePath C:\path\to\release.keystore `
  -KeystorePassword "<password>" `
  -KeyAlias "<alias>"
```

5. Confirm the compatibility step reports the same package name as the installed release package.
6. Confirm the compatibility step reports the same signer SHA-256 as `ANDROID_RELEASE_SIGNER_SHA256`.
7. Confirm the compatibility step reports a higher `versionCode` than the baseline APK.
8. Do not announce the APK as update-compatible if the run used `allow_update_baseline_reset=true`; that mode creates a new stable baseline and cannot update APKs signed by a different previous key.

## 3) Verify published release assets

1. Open the release page for the tag (for example `v0.2.0`).
2. Confirm at least one APK asset exists with name pattern:
   - `StS2Launcher-v<version>.apk`
3. Confirm checksum file exists:
   - `StS2Launcher-v<version>.apk.sha256`
4. Confirm the release body includes generated release notes.

## 4) Optional checksum verification (local)

```bash
sha256sum -c StS2Launcher-v<version>.apk.sha256
```

Expected output:

```text
StS2Launcher-v<version>.apk: OK
```

## 5) Install validation on device

```bash
adb install -r StS2Launcher-v<version>.apk
```

1. Start app and confirm launcher UI appears.
2. Confirm no immediate crash on cold start.

## 6) Archive and follow-up

- Record any failures in the release PR or issue tracker.
- Fix root cause before creating the next tag.
- Add a short note to `OVERHAUL_STATUS.md` if a process step changes.

