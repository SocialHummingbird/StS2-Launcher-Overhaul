# Android release validation checklist

Current posture: the ARM64 Android path works locally, including Steam download, Pull from Cloud, Push-to-Cloud hardening, Android local save handoff, and game launch. Release validation is now about hardening that baseline on the newest public APK, especially Samsung retests, persisted Steam-session/update UX, Steam version-selection validation, confirmed Push overwrite evidence, upgrade install behavior, freshness/cache checks, visual loading-screen regressions, and release artifact hygiene. See [current Android status](current-android-status.md).

Use this checklist after every release run (manual or tag-triggered) to confirm artifact publication before announcing a release.

Current build-only prerelease reference:

```text
release=v0.2.278-local-compact-label-refactor
asset=StS2Launcher-v0.2.278-local-compact-label-refactor-arm64-v8a.apk
sha256=9e85ce823c01fc0857ba6215dff9540362a4e38d82bf0297adb2aac1dedd4d76
package=com.sts2launcher.overhaul.fork.local
versionName=0.2.278-local-compact-label-refactor
versionCode=278000
validation=build/static-gate only; device runtime signoff still requires the public/public-beta evidence workflow below
```

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

4. If the release includes Steam game version selection, run the static version-selection guardrails before device validation:

```powershell
.\scripts\audit-steam-version-selection.ps1
.\scripts\audit-steam-branch-guidance-parity.ps1
```

5. If the release includes public/beta coexistence or multi-version runtime changes, run the multi-version runtime release gate wrapper before device validation and use the release gate checklist:

```powershell
.\scripts\run-multi-version-runtime-release-gates.ps1
```

Checklist: [multi-version runtime release gates](multi-version-runtime-release-gates.md).

6. If these are missing or stale, configure them from the stable release keystore:

```powershell
.\scripts\configure-android-release-signing.ps1 `
  -KeystorePath C:\path\to\release.keystore `
  -KeystorePassword "<password>" `
  -KeyAlias "<alias>"
```

7. Confirm the compatibility step reports the same package name as the installed release package.
8. Confirm the compatibility step reports the same signer SHA-256 as `ANDROID_RELEASE_SIGNER_SHA256`.
9. Confirm the compatibility step reports a higher `versionCode` than the baseline APK.
10. Do not announce the APK as update-compatible if the run used `allow_update_baseline_reset=true`; that mode creates a new stable baseline and cannot update APKs signed by a different previous key.

## 3) Verify published release assets

1. Open the release page for the tag (for example `v0.2.0`).
2. Confirm at least one APK asset exists with name pattern:
   - Current ARM64 releases: `StS2Launcher-v<version>-arm64-v8a.apk`
   - Older universal releases: `StS2Launcher-v<version>.apk` or `StS2Launcher-v<version>-universal*.apk`
3. Confirm checksum file exists:
   - `StS2Launcher-v<version>-arm64-v8a.apk.sha256`
4. Confirm the release body includes generated release notes.
5. Confirm the release notes distinguish the current working ARM64 path from release-candidate status. Do not claim release-ready cloud sync until Pull and Push have both been validated, including no-accidental-upload behavior for Push.
6. Confirm the release notes do not claim Steam beta/version selection is release-signed unless the Steam version-selection runbook has current ARM64 evidence for public/default, beta, marker provenance, cache cleanup, missing/private/password branch behavior, save compatibility, and Pull-after-switch/current-backup safety.

## 4) Structural asset verification

Run the release verifier against the exact release tag and asset:

```powershell
.\scripts\verify-android-release-apk.ps1 `
  -ReleaseTag "v0.2.187-beta-art-fallback" `
  -AssetName "StS2Launcher-v0.2.187-beta-art-fallback-arm64-v8a.apk" `
  -Abi arm64-v8a
```

For manual checksum verification after downloading the `.sha256` sidecar:

```bash
sha256sum -c StS2Launcher-v<version>-arm64-v8a.apk.sha256
```

Expected output:

```text
StS2Launcher-v<version>-arm64-v8a.apk: OK
```

## 5) Install validation on device

```powershell
.\scripts\install-android-release.ps1 `
  -ReleaseTag "v0.2.187-beta-art-fallback" `
  -AssetName "StS2Launcher-v0.2.187-beta-art-fallback-arm64-v8a.apk" `
  -ClearAppData `
  -Launch `
  -CaptureDiagnostics
```

1. Start app and confirm launcher UI appears.
2. Confirm no immediate crash on cold start.
3. Confirm native splash, launcher loading/warmup, and settled launcher surfaces do not clip or distort on the target screen.
4. Confirm Steam login reaches authentication success or ownership verification.
5. Confirm game download works when game files are absent.
6. Confirm Steam version selection does not regress the default/public path: selected branch is default/public, download/update uses the legacy `files/game` path, and the game launches.
7. If beta/version selection is included in the release claim, follow [Steam version selection runbook](steam-version-selection-runbook.md) and capture evidence for `beta` selection, side-by-side cache path, `steam_branch.txt` marker provenance, selected-PCK startup routing, selected-version redownload, inactive-cache cleanup, and missing/private/password branch behavior.
8. If public/beta coexistence is included in the release claim, capture multi-version runtime evidence and run the release gate wrapper against that artifact:

```powershell
.\scripts\capture-multi-version-runtime-evidence.ps1 `
  -PackageName <installed.package.name> `
  -RunLabel public

.\scripts\capture-multi-version-runtime-evidence.ps1 `
  -PackageName <installed.package.name> `
  -RunLabel public-beta

.\scripts\capture-multi-version-runtime-evidence.ps1 `
  -PackageName <installed.package.name> `
  -RunLabel branch-switch

.\scripts\run-multi-version-runtime-release-gates.ps1 `
  -PublicEvidenceDirs artifacts\android\multi-version-runtime-public-<timestamp> `
  -PublicBetaEvidenceDirs artifacts\android\multi-version-runtime-public-beta-<timestamp> `
  -BranchSwitchEvidenceDirs artifacts\android\multi-version-runtime-branch-switch-<timestamp> `
  -RequireSaveSafety `
  -RequireResolvedClassification
```

For one-off runtime evidence artifact review, run the same wrapper with `-EvidenceDirs` plus `-RequirePublic`, `-RequirePublicBeta`, or `-RequireBranchSwitch`. Prefer the branch-specific evidence directory parameters for release signoff.

9. Confirm save compatibility across public/non-public branch switches is validated or explicitly documented as an unsupported risk.
10. Confirm Pull from Cloud downloads real Steam Cloud files, writes Android local saves, and the game reads/surfaces the pulled profile.
11. Confirm Push to Cloud requires confirmation, cancel/no-confirm does not upload, and confirmed Push behavior is explicitly validated or deferred with overwrite-risk rationale. Manual Push must stay blocked until Pull from Cloud evidence exists for the currently selected game version and important Android local save evidence is present. After any Steam branch switch, manual Push must also stay blocked until backup storage permission and local/cloud pre-Push backup evidence exist. Diagnostics must expose `Manual Pull completed before Push`, `Current important Android local save evidence count`, `Current important Android local save evidence present`, `Baseline manual Push prerequisites satisfied`, and the recorded completed/blocked Push marker baseline/local-save evidence fields.
12. Confirm locked-screen/focus interruption and upgrade install behavior for every new release candidate before calling it ready.

## 6) Archive and follow-up

- Record any failures in the release PR or issue tracker.
- Fix root cause before creating the next tag.
- Add a short note to `OVERHAUL_STATUS.md` if a process step changes.
