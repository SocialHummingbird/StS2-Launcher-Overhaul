# Android release validation checklist

Current posture: historical ARM64 Android evidence covers Steam download, the superseded manual Pull/Push path, Android local save handoff, and game launch. The current local-only saving, launcher-owned automatic sync, and recovery implementation has no exact-candidate device pass. Its Stage 5 result is 0/10. See [current Android status](current-android-status.md).

Use this checklist to build and validate one exact candidate before any public fix or release announcement. The current workflow does not publish a release.

Current GitHub APK release reference:

```text
release=v0.2.416-startup-recovery-ime
asset=StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk
sha256=fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b
package=com.sts2launcher.overhaul.fork.local
versionName=0.2.416-startup-recovery-ime-local
versionCode=416001
validation=The exact non-debuggable APK was installed on Samsung SM-F966B / Android 16, matched the installed base.apk hash, rendered the full cold transition and launcher with IME hidden, promoted the manifest-matched public runtime pack, reached real NMainMenu, and logged the 60-second heartbeat. Automated checks and update compatibility from v0.2.412 passed. Xiaomi/Odin reporter devices, exact-build mods and every branch/GPU, broad lifecycle/display coverage, and real Steam Cloud Push remain unvalidated.
```

Current-source API 36 x86_64 emulator evidence is post-release. It validates newer native routing and recovery changes only and must not be added to `v0.2.416` ARM64 claims.

## Stage 5 save-safety release gate

Current exact-source Android result: **0/10 device rows passed**. The save-safety, automatic-sync, and recovery changes are implemented and desktop-tested only. They are not a public fix, are not release-validated, and must not be described as fixed until every row below and the Review 5 inspection pass on one exact candidate APK.

Historical manual Pull/Push, branch, mod, startup-recovery, and x86_64 emulator artifacts remain regression evidence. They predate the current implementation and count as zero rows in this matrix.

Until physical-device testing starts, keep validation deterministic and local: use filesystem fixtures for vanilla/modded and public/beta contexts, the existing fake Steam store for success and injected failures, and test-process termination between persisted transitions to prove restart recovery. These checks are required regression coverage, but they do not satisfy a device row. Existing Android logs stay in place as historical regression evidence.

Current desktop evidence passes 70/70 integrated transfer/automatic-sync/recovery scenarios, including 9/9 filesystem scenarios, plus 118/118 hard child-process termination edges (Begin 12, upload 32, Pull 30, Restore 28, Undo 16). Local gameplay safety passes 4/4, legacy recovery passes 6/6, and local backup-only recovery passes 5/5. These results use no device, credentials, network, or real Steam Cloud operation.

| Required exact-candidate device row | Current evidence |
| --- | --- |
| Vanilla public play, normal Quit, automatic upload, and remote read-back verification | Missing |
| Independent remote PC change safely downloaded before Play, after local backup | Missing |
| Different local and remote changes leave both sides untouched and report conflict | Missing |
| Exact mod set changes only its matching modded namespace | Missing |
| Changed mod set blocks before any Steam mutation | Missing |
| Public/beta switching never silently activates the other branch's saves | Missing |
| Offline gameplay preserves local bytes and retries the pending upload later | Missing |
| Crash or force-stop resumes persisted reconciliation on the next launcher start | Missing |
| Commit failure and remote read-back mismatch never report success | Missing |
| Restore followed by Undo reproduces the original local bytes exactly | Missing |

Review 5 must inspect captured Android-local byte hashes and independently read Steam Cloud byte hashes, not UI text. The focused and full logs must show no dropped write or swallowed failure, and every displayed `Synced` result must be tied to the matching verified remote manifest. A persisted baseline is useful evidence, but is not an independent live Steam query by itself.

The Android workflow now builds a candidate artifact only. It does not publish on tags and has no release-promotion job. Its build-info sidecar binds `source_commit`, `candidate_run_id`, `candidate_run_attempt`, `abi`, the exact `apk_sha256`, and the APK's verified actual `signer_sha256`; device evidence must additionally bind the installed base-APK SHA-256, package/version, and device identity. The tested APK must be promoted byte-for-byte after signoff rather than rebuilt. No promotion path should be added until it rejects an incomplete matrix or mismatched evidence binding.

The only Stage 5 candidate line is the published v0.2.416 `.local` lineage. The workflow hard-codes package `com.sts2launcher.overhaul.fork.local` and verifies every candidate against the exact v0.2.416 APK bytes and signing certificate before retaining the artifact. This is necessary for an in-place update to preserve and read existing private saves; a side-by-side `.dev` install cannot do that. It is not sufficient release evidence: the dedicated v0.2.416 signing credentials must first be configured, the candidate must pass the complete matrix, and affected-user exports must be verified before any recovery or Steam operation.

Preserve existing Android artifacts in place. Create a read-only hash inventory without copying, moving, or rewriting them:

```powershell
.\scripts\new-stage5-android-evidence-inventory.ps1 `
  -EvidenceRoot artifacts\android `
  -OutputPath artifacts\stage5-android-regression-evidence-inventory.json
```

The preserved historical tree was sealed on 2026-08-08 at source commit `c0ad9d78a842ba9f126d828a8c7627251f5ee371`: 14,896 files, 36,858,265,297 bytes, manifest SHA-256 `b86ac021fd4c98def319d3fd75a59f82f9d1268b13b3a0bb8762b47f34eccf73`. The ignored manifest remains at the path above so its contents can be rechecked without committing 6 MB of generated inventory. This seal is historical regression evidence only and satisfies zero current device rows.

For a new exact-candidate capture, use the read-only wrapper and pass the candidate APK plus the full source commit so the capture records both candidate and installed hashes. If ADB is unauthorized, the collector exits before creating an evidence directory. If the exact package is non-debuggable, it records private-file evidence as unavailable rather than treating filenames or UI messages as byte proof. The published v0.2.416 APK is nondebuggable, so do not claim that ADB can export its private saves before the update. The verified in-app export introduced by the update-compatible candidate is the byte authority.

```powershell
.\scripts\start-android-save-validation-capture.ps1 `
  -DeviceSerial <authorized-device> `
  -ApkPath <exact-candidate.apk> `
  -SourceCommit <40-character-source-commit> `
  -PackageName com.sts2launcher.overhaul.fork.local `
  -Stage5Row <1-10> `
  -EvidencePhase <short-phase-name> `
  -LogcatSince "MM-dd HH:mm:ss.fff"
```

The signed candidate is expected to be non-debuggable. Treat the verified in-app recovery export as the Android byte authority; `run-as` availability is optional collector cross-check evidence, not a matrix prerequisite. Record `LogcatSince` before the phase, perform the in-app export while that scenario window is active, and retain the collector's raw `logcat.txt`. A completed export emits one `STS2_SAVE_EXPORT_COMPLETE` JSON line only after byte-for-byte write/read-back verification. Convert every retained export phase into its canonical manifest:

```powershell
.\scripts\verify-stage5-android-save-bundle.ps1 `
  -BundlePath <retained-recovery-export.json> `
  -OutputPath <android-manifest.json>
```

After each independent Steam client download, retain its dedicated download root unchanged and capture all allowed remote files. The capture rejects unknown paths, case variants, and reparse points; the matrix reviewer later reads and hashes the retained bytes again.

```powershell
.\scripts\new-stage5-live-steam-manifest.ps1 `
  -SteamCloudRoot <dedicated-retained-download-root> `
  -SelectedNamespace Vanilla `
  -ExpectedSteamId64 <steam-id-64> `
  -ExpectedRuntimeIdentity public `
  -SteamSyncEvidencePath <independent-steam-client-evidence> `
  -CaptureMethod <capture-method> `
  -RetainedImmutableSource `
  -OutputPath <steam-manifest.json>
```

Create one matrix, replace its placeholders with the exact candidate, device, context, evidence paths, and SHA-256 values, then run the reviewer. Do not edit evidence after it is referenced.

```powershell
.\scripts\new-stage5-physical-matrix-template.ps1 `
  -OutputPath artifacts\stage5-physical-matrix.json

.\scripts\review-stage5-physical-matrix.ps1 `
  -MatrixPath artifacts\stage5-physical-matrix.json `
  -OutputPath artifacts\stage5-physical-matrix-review.json
```

The verifier independently recomputes the exported save tree and SaveContext identities and records them with the ExportId and exact bundle SHA-256. The reviewer requires that complete identity to match an inventoried raw completion line captured from the bound candidate and device, including when `run-as` is unavailable. A bundle or canonical manifest copied from another capture therefore cannot be substituted silently.

The reviewer also requires all 10 rows, both row 9 failure subcases, exact candidate/source/device binding, row-specific machine checks, and a later independent live-Steam equality check for every `Synced` log. Its deterministic fixture test is regression coverage only and cannot satisfy a physical-device row.

Use the existing `cloud_sync_enabled` setting throughout this matrix. Do not add a production feature flag or alternate sync mode to make a row easier to test.

## 1) Verify candidate workflow outcome

1. In GitHub Actions, open the latest manually dispatched **Android Release Candidate** run.
2. Dispatch with an explicit numeric `version_code` greater than the pinned baseline's `416001` (the default is `417003`), then confirm `Build Android APK` completed and that there is no publish job.
3. Confirm the `Verify release update compatibility` step completed.
4. Confirm the output APK path is present in logs:
   - `android/build/outputs/apk/mono/release/StS2Launcher-v<version>.apk`
5. Download the candidate artifact and keep it unchanged through the complete Stage 5 matrix.
6. Confirm its build-info sidecar records the expected `source_commit`, `candidate_run_id`, `candidate_run_attempt`, `abi=arm64-v8a`, actual verified `signer_sha256`, and `apk_sha256`.
7. Confirm `apk_sha256` matches both the checksum sidecar and a fresh hash of the downloaded APK. Every device capture must bind that hash and the installed base-APK hash.

## 2) Verify update guardrails

1. Confirm repository secrets are configured:
   - `ANDROID_LOCAL_UPDATE_KEYSTORE_BASE64`
   - `ANDROID_LOCAL_UPDATE_KEYSTORE_PASSWORD`
   - `ANDROID_LOCAL_UPDATE_KEY_ALIAS`
2. Confirm repository variable `ANDROID_LOCAL_UPDATE_SIGNER_SHA256` is configured to the published v0.2.416 certificate SHA-256 fingerprint `FD0E3D5ACF435C1D23BFC5C426E99AA9EB5808619FF1FC214FFCA99CFAC7E57A`.
3. Audit the current repository setup. This requires authenticated GitHub CLI access, a JDK, and Android SDK build-tools. The script uses explicit `-AndroidHome` / `-JavaHome` paths when provided, otherwise checks the current environment and the repo-standard `.w40k-android-toolchain` SDK/JDK roots, restoring the caller's process environment afterward. It downloads the fixed baseline into a temporary directory, verifies its SHA-256, and reads the APK itself to prove package `com.sts2launcher.overhaul.fork.local` and signer match `ANDROID_LOCAL_UPDATE_SIGNER_SHA256`:

```powershell
.\scripts\check-android-release-readiness.ps1
```

The candidate workflow has no package selector, baseline override, or baseline-reset mode. Its fixed update baseline is `v0.2.416-startup-recovery-ime` / `StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk` / SHA-256 `fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b`, package `com.sts2launcher.overhaul.fork.local`, versionCode `416001`, signer `FD0E3D5ACF435C1D23BFC5C426E99AA9EB5808619FF1FC214FFCA99CFAC7E57A`. A candidate is retained only when its package and signer match and its versionCode is higher.

The default downloaded build inputs are also byte-pinned: upstream `0.2.0` APK SHA-256 `6dddbd6716c3830ec802e2672be2402bd06d4270dcf1bcd72c27e46ca05acf41`, and the explicit `v0.2.416` sts2 source APK SHA-256 `fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b`. If either source is overridden, its matching expected digest must be overridden in the same manual dispatch; a missing or mismatched digest fails before extraction.

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

6. If these are missing, configure them from the exact keystore used for v0.2.416. The configurator rejects any other signer before changing a GitHub secret or variable:

```powershell
.\scripts\configure-android-release-signing.ps1 `
  -KeystorePath C:\path\to\release.keystore `
  -KeystorePassword "<password>" `
  -KeyAlias "<alias>" `
  -OfflineBackupConfirmed
```

`-OfflineBackupConfirmed` is a safety assertion. First copy this exact v0.2.416 update key to controlled offline storage and verify that the copy opens with the expected alias and FD0E…E57A certificate. A GitHub secret cannot be the only recoverable copy of the signing key.

7. Confirm the compatibility step reports the same package name as the installed release package.
8. Confirm the compatibility step reports the same signer SHA-256 as `ANDROID_LOCAL_UPDATE_SIGNER_SHA256`.
9. Confirm the compatibility step reports a higher `versionCode` than the baseline APK.
10. Confirm the workflow contains no package override, baseline override, or baseline-reset path.

## 3) Verify published release assets after future promotion

Skip this section while Stage 5 is incomplete. It is a post-promotion audit for the future byte-identical candidate; it is not permission to publish, tag, or rebuild the tested APK.

1. Open the release page for the tag (for example `v0.2.0`).
2. Confirm at least one APK asset exists with name pattern:
   - Current ARM64 releases: `StS2Launcher-v<version>-arm64-v8a.apk`
   - Older universal releases: `StS2Launcher-v<version>.apk` or `StS2Launcher-v<version>-universal*.apk`
3. Confirm checksum file exists:
   - `StS2Launcher-v<version>-arm64-v8a.apk.sha256`
4. Confirm the release body includes generated release notes.
5. Confirm release metadata exists:
   - preferred local-build sidecar: `StS2Launcher-v<version>-arm64-v8a.apk.json`
   - GitHub Actions sidecar: `StS2Launcher-v<version>-arm64-v8a.apk.build-info.txt`
6. Run the release hygiene check against the fork explicitly:

```powershell
.\scripts\check-github-release-hygiene.ps1 `
  -Repo "SocialHummingbird/StS2-Launcher-Overhaul" `
  -ReleaseTag "v0.2.416-startup-recovery-ime" `
  -AssetName "StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk"

.\scripts\audit-github-release-inventory.ps1 `
  -Repo "SocialHummingbird/StS2-Launcher-Overhaul" `
  -Limit 40 `
  -OutputPath docs\github-release-inventory.md
```

7. Confirm the release notes distinguish historical ARM64 evidence from the current exact candidate. Do not claim save safety fixed or release-ready until all 10 Stage 5 rows and Review 5 pass on that candidate.
8. Confirm the release notes do not claim Steam beta/version selection is release-signed unless the Steam version-selection runbook has current ARM64 evidence for public/default, beta, marker provenance, cache cleanup, missing/private/password branch behavior, save compatibility, and exact save-transfer context/backup/read-back safety.

## 4) Structural candidate verification

For an already published historical release, run the release verifier against the exact release tag and asset. For the current candidate, run the equivalent local structural checks against the unchanged candidate APK and record its SHA-256; do not fetch or rebuild a substitute.

```powershell
.\scripts\verify-android-release-apk.ps1 `
  -ReleaseTag "v0.2.416-startup-recovery-ime" `
  -AssetName "StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk" `
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

## 5) Prepare and validate the device

The affected-user recovery sequence is deliberately different from a clean test install. The published v0.2.416 package is nondebuggable and cannot expose its private files through ADB; claiming a verified pre-install private-save export is therefore false. Preserve the installed app and use only this sequence:

1. Keep the existing `.local` install closed. Record its package, versionCode, base-APK SHA-256, and signer using read-only inspection. Do not launch, force-stop, uninstall, clear data, Pull, restore, or install any substitute build.
2. Before touching the device, statically verify the unchanged candidate, checksum sidecar, and build-info sidecar. Verify the candidate against the retained v0.2.416 baseline with `verify-android-update-compat.ps1`; require package `com.sts2launcher.overhaul.fork.local`, signer `FD0E3D5ACF435C1D23BFC5C426E99AA9EB5808619FF1FC214FFCA99CFAC7E57A`, versionCode above `416001`, and the expected candidate source/run/APK hashes. If any check fails, stop.
3. Take the device offline before installation and keep it offline. Install only the verified candidate as an update, without launching it: `adb install -r <exact-candidate.apk>`. Do not add `-d`, do not use a build/install wrapper, and do not uninstall or clear app data. Require `Success`, then read back the installed package/version/base-APK hash without launching. Any install incompatibility is a stop condition, not permission to uninstall.
4. First-launch the updated `.local` app while the device is still offline. Startup performs session setup before the recovery controls are used, so offline operation is the safety boundary; do not press Play, Pull, Push, Restore, or Approve.
5. Select **Play Vanilla**, press **Export Current Android Saves**, share/copy the resulting JSON outside the app's private storage, and run `verify-stage5-android-save-bundle.ps1` against that retained file. Keep the raw bundle and generated manifest.
6. Select the exact installed mod set and **Play Modded**, then repeat **Export Current Android Saves** into a separate retained file and verify it separately. Never substitute one namespace's export for the other.
7. Only after both current-save bundles are outside the app and read-back verified, press **Scan for Recovery Copies**. For every plausible vanilla, modded, temporary, or backup candidate, use **Export Recovery Bundle**, retain the bytes outside the app, and verify each bundle before considering Restore.
8. Leave the affected-user device offline and stop. Inspect the exports first; do not run the destructive/conflict matrix on the user's only copy and do not enable Steam sync or restore anything yet.

The in-app current-save and recovery-bundle exporters are local-only, but the launcher startup path also initializes its session and may resume a pending reconciliation on installs that already contain one. A direct v0.2.416 upgrade is not expected to contain the new pending record; offline first launch is still required instead of relying on that assumption. The same exact candidate APK can proceed through the physical matrix only on a controlled device/data set after the affected-user bundles are safe.

For each later controlled-device matrix phase, bind a read-only evidence capture to the exact candidate and source commit:

```powershell
.\scripts\start-android-save-validation-capture.ps1 `
  -DeviceSerial <authorized-device> `
  -ApkPath <exact-candidate.apk> `
  -SourceCommit <40-character-source-commit> `
  -PackageName com.sts2launcher.overhaul.fork.local `
  -Stage5Row <1-10> `
  -EvidencePhase <short-phase-name> `
  -LogcatSince "MM-dd HH:mm:ss.fff"
```

Record the device-local logcat timestamp immediately before the scenario. `-LogcatSince` produces a scenario-windowed log without clearing the historical buffer. This wrapper does not build, install, launch, force-stop, clear logcat, write a diagnostics marker, or perform a Steam operation. The older `run-next-android-save-validation.ps1` build/install path is deliberately disabled.

After the affected-user exports are safe, use a controlled device/data set for the matrix:

1. Start the app intentionally and confirm the launcher UI appears.
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
10. Confirm an independent remote PC change is downloaded automatically before Play only after the Android destination is backed up; do not use the superseded manual Pull flow as Stage 5 evidence.
11. Confirm normal Quit uploads automatically and that authentication, commit, connection, and remote read-back failures never report success. Validate exact SteamID64, namespace, runtime/public-beta identity, and mod-set matching, destination backup, and byte-hash read-back before accepting `Synced`.
12. Confirm locked-screen/focus interruption and upgrade install behavior for every new release candidate before calling it ready.

## 6) Archive and follow-up

- Record any failures in the release PR or issue tracker.
- Fix root cause before creating the next tag.
- Add a short note to `OVERHAUL_STATUS.md` if a process step changes.
