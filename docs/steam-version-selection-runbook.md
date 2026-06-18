# Steam Version Selection Validation Runbook

This runbook defines the safe order for validating Steam game version selection and side-by-side branch caches. It is intentionally evidence-first: do not treat a later step as meaningful if an earlier gate has failed.

Use [Steam version selection release readiness](steam-version-selection-release-readiness.md) as the high-level release gate and this runbook as the execution order for collecting that evidence.

## Scope

- Validate the default/public Steam branch still works through the legacy install path.
- Validate the beta branch path is branch-aware from selection through download, startup, diagnostics, and cleanup.
- Preserve save safety by proving selected-version Pull-before-Push, Android local save evidence, and branch-switch backup posture before any Push test.
- Keep private/password-protected beta behavior and save compatibility as release blockers until proven.

## Evidence targets

Capture evidence in `docs/steam-version-selection-evidence-template.md`.

Required artifacts:

- Build output for managed C# and Android Java.
- Device model, CPU ABI, APK build identifier, and install source.
- Launcher diagnostics bundle after public/default validation.
- Launcher diagnostics bundle after beta validation.
- Native startup or logcat evidence for selected branch startup routing.
- Branch marker files for each validated installed branch.
- Public-vs-beta depot manifest comparison, including selected/public manifest IDs, manifest source, manifest request branch, inherited-public counts, and missing-selected-manifest counts.
- Public/default and beta file inventories with SHA-256 hashes when beta content appears mixed or art assets look wrong.
- Cloud Pull evidence for the selected version before any Push validation.
- Android local save evidence before any Push validation.
- Local pre-Push and cloud pre-Push backup evidence before any post-switch Push mutation.

## Optional auth diagnostics

SteamKit debug logging is disabled by default to keep normal validation logs quiet. If login/authentication debugging needs SteamKit messages, enable sanitized logs only for that focused run:

```powershell
adb shell settings put global sts2_steamkit_debug_logs 1
```

Disable the setting again before routine evidence capture:

```powershell
adb shell settings put global sts2_steamkit_debug_logs 0
```

When enabled, diagnostics must still show `SteamKit debug logs sanitized for credentials/tokens: true`. Do not attach unsanitized logs publicly.

## Safe validation order

1. Build gate

   Compile the managed C# launcher code and Android Java entry points before any runtime validation. If this fails, stop and fix build errors before continuing.

2. Fresh install or controlled upgrade

   Install the APK on ARM64 hardware. Record whether this is a fresh install or an upgrade over an earlier public-only APK.

3. Public/default startup baseline

   Start the launcher with the selected version set to default/public. Confirm the launcher uses the legacy `files/game` and `files/download_state` paths.

4. Public/default download and launch

   Download or update the public/default game files. Confirm `files/game/SlayTheSpire2.pck` is ready and launches. Public/default installs upgraded from older APKs may remain ready without `steam_branch.txt`.

5. Public/default diagnostics capture

   Capture diagnostics. Confirm selected branch, selected version name, selected game directory, selected PCK path, selected readiness result, selected download state, and cached version inventory are present.

6. Switch warning gate

   Refresh Game Versions, then select a non-public Steam branch exposed in the dropdown, such as `beta` if Steam app-info exposes it for the account. Confirm the branch-switch confirmation warns about download requirements, save compatibility risk, local backup enablement, manual Push backup-storage requirement, private/password-protected branches, and lack of beta password entry. If no non-public branch appears, record that as missing/private/inaccessible behavior instead of manually entering a branch.

7. Beta download path

   Confirm the selected non-public `game_branch` persists and the download uses `files/game_versions/<selected-branch>/game` plus `files/game_versions/<selected-branch>/download_state`. For `beta`, this remains `files/game_versions/beta/game` plus `files/game_versions/beta/download_state`.

8. Beta branch marker provenance

   After beta download, inspect the selected beta `steam_branch.txt`. It must include the selected branch, display/state directory name, depot manifest count, one or more depot manifest entries, selected branch manifest IDs, public manifest IDs, manifest source, manifest request branch, selected/effective public-match fields, public-identical depot counts, branch-specific depot counts, inherited-public depot counts, and missing-selected-manifest counts.

9. Beta branch integrity and art asset comparison

   If the beta branch appears mixed or art assets look wrong, clean-redownload the selected beta slot and run `scripts/capture-steam-beta-integrity-evidence.ps1`. For local device evidence, install with `scripts/build-android-local.ps1 -EvidenceDebuggable` first so `adb run-as` can read app-private branch markers and inventories. Compare `files/game` against the selected beta cache. Capture `SlayTheSpire2.pck` hashes and affected art asset hashes where possible. Treat `manifestSource=public-inherited` as evidence that Steam appears to serve public content for that depot inside the selected beta branch, not as a silent full-branch launcher fallback. Read `beta-integrity-summary.txt` first: `Evidence readiness: not ready for final classification` means the evidence package is not strong enough for release signoff, even if a possible cause is suggested.

10. Beta startup routing

   Launch the game with beta selected. Confirm native pre-routing logs selected branch, selected branch note, and resolved game directory. Confirm native startup routes to the selected side-by-side cache, for example `files/game_versions/public-beta-8128824d/game/SlayTheSpire2.pck`, and logs selected branch, selected branch note, resolved startup game directory, branch marker readiness, depot-manifest provenance, and depot manifest entry count. Confirm game startup reaches the main menu and preserve the bootstrap trace when investigating art/runtime differences.

11. Marker failure recovery

   Validate that a selected non-public cache with missing, mismatched, or provenance-free `steam_branch.txt` is not treated as ready. The launcher should show the selected-version redownload/rebuild-cache action instead of launching stale or ambiguous files.

12. Redownload selected version

   Confirm `REDOWNLOAD SELECTED VERSION` deletes only the selected branch cache and selected branch download state, writes `last_game_version_redownload.txt`, then starts a replacement download. Inactive branch caches must remain intact.

13. Inactive cache cleanup

   Confirm `CLEAR CACHED VERSIONS` removes inactive non-public caches and preserves the currently selected branch cache. Capture the launcher status/log line naming the selected version preserved, logcat lines for removed inactive cache paths and the preserved selected cache path, the diagnostics line `Game version cache cleanup marker selected cache preserved where applicable`, and `last_game_version_cache_cleanup.txt`.

14. Switch back to public/default

   Select default/public again. Confirm startup uses `files/game/SlayTheSpire2.pck` and does not delete the beta cache unless cleanup is explicitly invoked.

15. Missing/private/password beta behavior

   Validate the behavior when a selected dropdown branch is stale, missing, private, inaccessible to the Steam account, or password-protected. If password-protected behavior cannot be tested, keep it as a release blocker and ensure diagnostics show `Steam beta password entry supported: false`.

16. Save compatibility review

   Before any Push test, confirm local save files and Steam Cloud save files are understood for the selected branch. Treat compatibility between branches as unproven unless game behavior confirms otherwise.

17. Cloud Pull gate

   Perform Pull from Cloud first for the selected game version. Confirm important save files exist locally after Pull and diagnostics show `Manual Pull completed before Push`, current important Android local save evidence count/presence, and `Baseline manual Push prerequisites satisfied` before considering any Push test.

18. Backup permission gate

   For branch-switch Push testing, confirm local backup is enabled, branch-switch marker safety evidence is complete, Pull from Cloud completed after the branch switch for the selected version, Android local save evidence is present, backup storage permission is available, and diagnostics show structured branch-switch marker details, manual Pull evidence, local save evidence count/presence, backup directory path/existence, plus `Branch-switch manual Push prerequisites satisfied`. If a branch switch marker exists and branch-switch marker safety evidence, current Pull evidence, Android local save evidence, or backup storage permission is missing, Push must remain blocked and write `last_manual_cloud_push_blocked.txt`.

19. Pre-Push backup evidence

   Before mutating Steam Cloud after a branch switch, confirm local-pre-push backups cover every important Android local save and cloud-pre-push backups cover every existing important Steam Cloud save. Capture `Pre-Push local backup evidence count`, `Pre-Push cloud backup evidence count`, `Latest pre-Push local backup UTC`, `Latest pre-Push cloud backup UTC`, `Pre-Push local backup evidence after branch switch`, `Pre-Push cloud backup evidence after branch switch`, and `Branch-switch pre-Push backup evidence satisfied`. If Local Backup is enabled and storage permission, full local pre-Push coverage, or full cloud pre-Push coverage for existing important Steam Cloud saves is missing, manual Push must fail before upload and write `last_manual_cloud_push_blocked.txt`.

20. Manual Push smoke test

   Only after selected-version Pull, local save existence, storage permission where required, and branch-switch backup evidence where required are proven, perform a manual Push. Record selected game version, confirmation wording, baseline prerequisite evidence, backup evidence, upload result, and any crash/log output.

21. Final evidence package

   Fill the evidence template with concrete pass/fail results, linked diagnostics, branch marker contents, logs, and unresolved blockers. Complete `PUBLIC_EVIDENCE_REDACTION_REVIEW.txt`, then run `scripts\review-public-evidence-redaction.ps1 -EvidenceDir <evidence-folder>` before posting or attaching any public artifact. Do not mark release-ready while Steam beta password behavior, save compatibility, Push safety evidence, or artifact hygiene review is missing.

## Stop conditions

- Build gate fails.
- Public/default no longer downloads, updates, or launches.
- Selected beta cache can launch without matching branch marker provenance.
- Beta marker lacks selected/public manifest comparison, manifest source, or manifest request branch evidence after a clean beta redownload.
- Branch switch does not force local backup posture.
- Manual Push can proceed after branch switch without backup storage permission.
- Pull evidence or local save existence is missing before Push.
- Pre-Push backup evidence is missing before Push.
- Diagnostics do not expose enough branch, marker, cache, and backup state to debug failures.
- Public evidence redaction review fails, or screenshots/logs have not been manually reviewed for credentials, account identifiers, device notifications, private save/profile data, local paths, and device IDs.

## Release-readiness rule

Release readiness requires evidence, not implementation intent. The version-selection feature remains blocked until public/default, beta, startup routing, cache cleanup, diagnostics, branch marker provenance, Pull-before-Push/local-save safety, Pull-after-switch/backup safety, backup evidence, missing/private/password beta behavior, and save compatibility are all proven or explicitly documented as unsupported user-facing limitations.
