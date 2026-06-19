# Current Android Status

_Last updated: 2026-06-19_

Current device evidence ledgers:

- [android-device-validation-20260608.md](android-device-validation-20260608.md)
- [android-cloud-save-validation-20260609.md](android-cloud-save-validation-20260609.md)
- [launcher-loading-screen-staging.md](launcher-loading-screen-staging.md)
- [steam-version-selection-validation.md](steam-version-selection-validation.md)
- [steam-version-selection-runbook.md](steam-version-selection-runbook.md)
- [steam-version-selection-release-readiness.md](steam-version-selection-release-readiness.md)

## Headline

The app now works on the validated ARM64 Android path, but it is still in polish and hardening rather than release-candidate signoff.

Validated locally on ARM64 hardware:

- Fresh APK/runtime install reaches the launcher.
- The latest public release APK verifies structurally, installs, launches, and has captured ARM64 launcher visual checks for login, active download progress, diagnostics drawer, ready-state layout, and Push confirmation/cancel.
- Public release upgrade compatibility has advanced through `v0.2.187-beta-art-fallback` with `versionCode=218700`; the latest signed dev APK installed over the previous dev package while preserving app data. Earlier proof showed `v0.2.175-refactor-apk` to `v0.2.177-login-a8729d6` preserving install state.
- Locked-screen interruption returns to the app after manual unlock without app-specific crash markers.
- Steam login and game depot download complete.
- Pull from Cloud downloads real Steam Cloud files.
- Push to Cloud completes on the local hardening build without post-Push process death.
- Pull after Push downloads and writes the pushed cloud state back to Android local storage.
- Android local save handoff works.
- The downloaded game launches and shows the pulled `Profile 1` in-game.
- The selected `public-beta` branch launches from its side-by-side cache on the local ARM64 version-selection hardening build.
- The latest local runtime-pack prerelease proves public-after-beta, public/default, and public-beta launch with matched PCK/runtime evidence on ARM64 hardware; fix27 also proves the public-after-beta startup crash is fixed after branch switching from public-beta.
- Force-stop/relaunch returns to the launcher with saved Steam credentials available.

## Latest hardening evidence

Latest GitHub APK prerelease evidence:

```text
release=v0.2.188-local-runtime-beta-fix27
asset=StS2Launcher-v0.2.188-local-runtime-beta-fix27-arm64-v8a.apk
sha256=a4067fc02b2823061cf4af0872a1bc42eece1044c552c396ac086d0f641df3ca
package=com.sts2launcher.overhaul.fork.local
versionName=0.2.188-local-runtime-beta-fix27
versionCode=218853
validation=dotnet build passed; ARM64 public/default, public-after-beta, public-beta, and branch-switch runtime evidence passed release gates
upgradeBaseline=local runtime-pack validation line
```

Latest verified public release evidence remains:

```text
release=v0.2.187-beta-art-fallback
asset=StS2Launcher-v0.2.187-beta-art-fallback-arm64-v8a.apk
sha256=f2ef1c3ef2149d4901fc1051058d44cfdb2e45afb1c7a9ef5693d4714d31dffe
package=com.sts2launcher.overhaul.fork.dev
versionName=0.2.187-beta-art-fallback
versionCode=218700
upgradeBaseline=v0.2.186-sts2-mobile-version-selection / versionCode=218600
```

Latest device evidence folders:

- `artifacts/android/public-after-beta-fix20-20260618`
- `artifacts/android/public-beta-fix20-retry-20260618`
- `artifacts/android/public-beta-compendium-fix20-20260618`
- `artifacts/android/fix23-public-beta-startup-crash-retest-20260618`
- `artifacts/android/fix23-public-beta-game-launch-20260618`
- `artifacts/android/fix23-public-beta-compendium-route-20260618`
- `artifacts/android/fix23-public-beta-compendium-route-retry-20260618`
- `artifacts/android/multi-version-runtime-branch-switch-20260618-211533`
- `artifacts/android/multi-version-runtime-public-20260618-231242`
- `artifacts/android/multi-version-runtime-public-20260618-231242-public-redacted`
- `artifacts/android/multi-version-runtime-public-beta-20260618-224239`
- `artifacts/android/steam-version-selection-fix27-readonly-marker-status-20260618-2342`
- `artifacts/android/steam-version-selection-fix27-readonly-marker-status-20260618-2342-public-redacted`
- `artifacts/android/steam-version-selection-fix27-refresh-dropdown-20260618-2348`
- `artifacts/android/steam-version-selection-fix27-refresh-dropdown-20260618-2348-public-redacted`
- `artifacts/android/steam-version-selection-fix27-unavailable-saved-branch-20260619-0000`
- `artifacts/android/steam-version-selection-fix27-unavailable-saved-branch-20260619-0000-public-redacted`
- `artifacts/android/fix27-immediate-launch-smoke-20260619-0015`
- `artifacts/android/fix27-immediate-launch-smoke-20260619-0015-public-redacted`
- `artifacts/android/fix27-public-game-launch-smoke-20260619-0027`
- `artifacts/android/fix27-public-game-launch-smoke-20260619-0027-public-redacted`
- `artifacts/android/multi-version-runtime-public-20260619-002349`
- `artifacts/android/multi-version-runtime-public-20260619-002349-public-redacted`
- `artifacts/android/fix27-redownload-synthetic-cache-20260619-0035`
- `artifacts/android/fix27-redownload-synthetic-cache-20260619-0035-public-redacted`
- `artifacts/android/fix27-blocked-push-save-origin-20260619-111324`
- `artifacts/android/fix28-blocked-push-save-origin-20260619-111828`
- `artifacts/android/fix28-evidence-blocked-push-save-origin-20260619-112107`
- `artifacts/android/fix28-evidence-blocked-push-save-origin-20260619-112107-public-redacted`
- `artifacts/android/fix28-native-login-panel-cancel-20260619-112725`
- `artifacts/android/fix28-native-login-panel-cancel-20260619-112725-public-redacted`
- `artifacts/android/fix28-readonly-current-marker-status-20260619-113732`
- `artifacts/android/fix28-readonly-current-marker-status-20260619-113732-public-redacted`
- `artifacts/android/fix28-readonly-capture-script-diagnostics-index-20260619-1200`
- `artifacts/android/fix28-readonly-capture-script-diagnostics-index-20260619-1200-public-redacted`
- `artifacts/android/public-after-beta-game-launch-20260618-230719`
- `artifacts/android/startup-crash-20260612-233812`
- `artifacts/android/github-release-v0.2.187-beta-art-fallback`
- `artifacts/android/responsive-ui-check-20260609`
- `artifacts/android/phone-diagnostics-20260609-204439`
- `artifacts/android/loading-scale-release-visual-20260609`
- `artifacts/android/physical-login-RFCY70XQE7F-logcat.txt`
- `artifacts/android/phone-diagnostics-20260608-220359`
- `artifacts/android/lock-unlock-validation-20260608-215548`
- `artifacts/android/local-pull-smoke-20260608-221143`
- `artifacts/android/local-start-game-dpad-20260608`
- `artifacts/android/local-game-profile-center-20260608`
- `artifacts/android/local-restart-diagnostics-20260608`

The latest local hardening build proved that the phone was running the freshly installed runtime and managed assemblies:

```text
versionName=0.2.615-local-beta-evidence
versionCode=2261215
schema=22
arch=arm64
```

The startup freshness probe and assembly cache diagnostics report the installed package/version/schema, cache presence, `STS2Mobile.dll` size, and expected source/byte counts for required assemblies. The latest selected-version startup evidence also proves that Android native routing treats `/data/data/<package>` and `/data/user/0/<package>` as equivalent app-private paths before comparing branch marker provenance. Native routing/startup diagnostics now log the selected PCK path, byte count, and SHA-256 before Godot startup, so branch-integrity evidence can be tied to the exact runtime content bundle. The validated `public-beta` run loaded `game_versions/public-beta-8128824d/game/SlayTheSpire2.pck`, passed that file as Godot `--main-pack`, completed startup patch orchestration with `17/17` patches applied, and reached the game main menu. The runtime PCK hash is expected to differ from the raw Steam inventory hash because Android download completion patches the PCK in place to remove Android-incompatible plugin startup references.

Latest public-beta integrity/runtime evidence:

```text
integrityEvidence=artifacts/android/steam-beta-integrity-20260614-170912
runtimeEvidence=artifacts/android/runtime-public-beta-20260614
runtimePckEvidence=artifacts/android/runtime-public-beta-pck-20260614
publicRuntimeEvidence=artifacts/android/runtime-public-game-auto-pck-20260614
classification=likely branch-specific installed content
evidenceReadiness=ready for manifest/cache/art classification
selectedBranch=public-beta
selectedSlot=files/game_versions/public-beta-8128824d/game
publicPckSha256=8f0dbfef10a31994eb0f58e8d811db08712153c5c0d4491bc5fc4732be530f68
selectedRuntimePckSha256=957bd95f2bbe97fad18ea467e67b8525861a49aec08a0f31448e276925cb684a
startupResult=main menu reached
runtimeArtFindings=public and public-beta main-menu screenshots are visually equivalent at this level; public loads run-history doormaker_boss imported textures successfully, while public-beta reports loader failures for the same resource names after selected beta PCK mount. Treat as beta game-content/import-runtime behavior, not launcher public fallback, unless future selected-PCK evidence contradicts this run.
runtimeArtFix=run-history room icon helpers now fall back to branch-local unknown_monster art when a selected branch returns a missing run-history icon path; this avoids loading public assets while keeping older saves usable on beta branches.
fix23CompendiumBestiary=artifacts/android/fix23-public-beta-compendium-route-retry-20260618 proves the synced-save public-beta main menu can enter Compendium and Bestiary on ARM64. Bestiary rendered Assassin Raider with enemy list/model, active/source/runtime-pack sts2.dll all matched beta hash 4ad31f07b71820060b178ce3961f8589dbc94b3f8109428eaec8e7037ae2fdb3, focused logs showed no NativeFallback/SIGSEGV/JNI/fatal package failure, old doormaker/no-loader failure count was 0, and unknown_monster fallback resources loaded instead.
fix23BranchSwitchSafety=artifacts/android/multi-version-runtime-branch-switch-20260618-211533 passed 34 branch-switch/save-safety review checks. It proves public-beta source PCK a263c68cfdeb6e94af9029088e1bab0c4c72a1641bc1c1ff72c180396a7b134c maps through runtime-pack evidence to Android-patched mounted PCK 957bd95f2bbe97fad18ea467e67b8525861a49aec08a0f31448e276925cb684a, runtime-pack/source/active sts2.dll all match beta hash 4ad31f07b71820060b178ce3961f8589dbc94b3f8109428eaec8e7037ae2fdb3, stale downloader cache/wrong launch path/shared runtime cache are ruled out, and Steam Cloud Push safety is do-not-push until selected-runtime Pull/save evidence is current.
fix27PublicAfterBeta=artifacts/android/multi-version-runtime-public-20260618-231242 passed public/save-safety review after launching public from the same app data that previously held public-beta runtime evidence. It proves selected branch public, PCK files/game/SlayTheSpire2.pck hash 8f0dbfef10a31994eb0f58e8d811db08712153c5c0d4491bc5fc4732be530f68, source/cache sts2.dll hash 81c8f3443c4504e38a17570df688489414fceb6ea7fcf5b044d8117318ea8e49, runtime patch validation passed, and canonical runtime slot public-d8a7082fc63977cc bound to the native runtime cache identity. The wrapper passed with public, public-beta, and branch-switch evidence: scripts/run-multi-version-runtime-release-gates.ps1 -PublicEvidenceDirs artifacts/android/multi-version-runtime-public-20260618-231242 -PublicBetaEvidenceDirs artifacts/android/multi-version-runtime-public-beta-20260618-224239 -BranchSwitchEvidenceDirs artifacts/android/multi-version-runtime-branch-switch-20260618-211533 -RequireSaveSafety -Quiet.
```

## Cloud-save posture

Pull from Cloud and Push to Cloud are now validated end to end on the local ARM64 hardening path. Steam Cloud files were enumerated/downloaded, Android local save files were written, Push uploaded/flushed the local save batch without process death, and Pull after Push wrote the cloud state back into Android app storage.

- Push now validates the current selected-version safety gates before the destructive confirmation can be armed; when those gates pass, it still requires a separate arming tap before the overwrite confirmation gate appears.
- Direct Cancel returns to the launcher without upload.
- Back/no-confirm dismissal returns without upload.
- The latest warning text names Steam Cloud overwrite risk in code and documentation.
- Confirmed Push can overwrite real Steam Cloud state; keep it treated as an explicit destructive action.
- The 2026-06-09 Push crash was fixed by replacing Android native `SHA1.HashData` in the cloud upload file-hash path with managed SHA-1.
- Push evidence: `artifacts/android/push-managedsha1-observation-20260609-1`.
- Current-build manual Push evidence: `artifacts/android/push-clean3-observation-20260609-1`.
- Pull-after-Push evidence: `artifacts/android/pull-after-push-observation-20260609-1`.
- Preferred local evidence summaries: `summary-scrubbed.md` inside each 2026-06-09 Push/Pull artifact folder.
- Current launcher Push/Pull UI status text now names direction explicitly: Push makes Steam Cloud reflect Android local saves, and Pull makes Android local saves reflect Steam Cloud.
- Push confirmation now tells testers to Pull first and verify Android local saves exist before pushing, because Push can overwrite Steam Cloud state. The launcher also requires selected-version Pull/save-origin safety gates before the arming/confirmation UI can appear.
- Manual Push now has a baseline evidence gate before upload: Pull from Cloud must have completed for the currently selected game version, and important Android local save evidence must be present. Diagnostics expose live current local-save evidence count/presence, baseline manual Push prerequisite status, and recorded completed/blocked Push marker values for the same evidence.
- Branch-switch safety evidence now covers the pending-Pull posture after switching public -> public-beta: `current_android_save_origin.txt` records `branch switch pending Pull`, the review report classifies Steam Cloud Push as `do-not-push`, and no Push-to-Cloud control was exercised during the branch-runtime investigation.
- Save discovery now skips app runtime/cache trees such as `.godot`, `cache`, `game`, and `tmp` during fallback enumeration so cloud-save diagnostics stay focused on save candidates.

## Remaining release-readiness blockers

- Steam game version selection is in hardening: selected branch is persisted and used for manifest resolution/update checks, non-public branches download into side-by-side `game_versions/<branch>/game` caches, completed branch downloads write `steam_branch.txt` marker/provenance metadata, inactive cached versions can be cleared from support options, and local backup is enabled before branch switches. The launcher now uses a discovery-led dropdown Steam branch selector instead of normal-user text entry: default/public remains always available, account-visible options refresh from Steam app-info branch availability evidence, and locally installed non-public branch slots remain selectable as recovery/downloaded-cache options. It includes a non-mutating `REFRESH GAME VERSIONS` action, concise ready/build/password/unavailable/installed dropdown badges, selected-branch helper text, pre-download/update gates for known unavailable branches, wrapped selector guidance for private/password branch hardening and unproven save compatibility, and branch availability diagnostics after failed downloads. Native Android startup now blocks selected-version launch when branch marker provenance is missing or mismatched instead of falling through toward stale branch startup, and the local ARM64 build has validated selected `public-beta` launch from its side-by-side slot. Fix23 also fixes the Android JNI crash seen after confirming a switch to locally installed `public-beta` by using branch-local patch-validation/runtime-pack hashes before any direct non-public PCK hash fallback, and the focused fix23 route retry validates public-beta Compendium/Bestiary rendering without the earlier hard-lock or doormaker loader failure chain. Beta-integrity hardening records per-depot selected/public manifest comparison, explicit public-inherited depot provenance, manifest request branch evidence, selected-branch integrity log summaries, clean-redownload-gated `Classification:` and `Evidence readiness:` summaries, focused logcat, runtime selected-PCK path/hash evidence, and public-vs-beta file inventory/hash capture guidance so mixed beta/public behavior and art asset issues can be classified from evidence. The same selected-version guidance is captured in launcher diagnostics, branch-switch marker evidence, manual Pull evidence, native pre-routing/startup logs, and native fallback diagnostics. Static CI guardrails cover version-selection docs, release blockers, discovery-led dropdown behavior, unavailable-branch gates, native launch gating, Autofill cleanup, beta-integrity evidence fields, and managed/native selector-guidance parity. Refresh-game-versions negative cases, Steam beta/password behavior, inaccessible/private branch handling, cache cleanup, Pull-before-Push/current-backup safety, save compatibility across branches, and release-candidate public/default retest still need ARM64 device validation before release-candidate signoff; see [Steam version selection validation](steam-version-selection-validation.md) and [Steam version selection runbook](steam-version-selection-runbook.md).
- The current Steam version-selection release gate is tracked in [Steam version selection release readiness](steam-version-selection-release-readiness.md). Treat that tracker as the source of truth for what is implemented, what is unvalidated, and what evidence is required before release-candidate signoff. The remaining public-beta integrity device pass is summarized in [Steam beta integrity runtime checklist](steam-beta-integrity-runtime-checklist.md).
- Current public-beta runtime evidence rules out silent public fallback for the tested local ARM64 path: native routing selected `public-beta`, Godot mounted the selected beta PCK, startup patches completed, and the game reached the main menu. A public/default auto-launch baseline on the same build reached the same visible main menu but successfully loaded the run-history `doormaker_boss` imported textures where `public-beta` logged loader failures after mounting the selected beta PCK. PCK directory inspection shows `doormaker_boss` run-history imports exist in public but not in `public-beta`; `public-beta` contains `aeonglass_boss` instead and still contains shared `unknown_monster` fallback art. Runtime patching now falls back to branch-local `unknown_monster` art when the game returns a missing run-history icon path. This is a beta content/import compatibility fix, not public-content fallback.
- Current evidence tooling resolves `adb` from explicit `-AdbPath`, PATH, common Android SDK roots, and the repo-local `.w40k-android-toolchain` SDK path. A locked-device read-only capture on `com.sts2launcher.overhaul.fork.local` (`artifacts/android/multi-version-runtime-locked-readonly-cache-runtime-20260618-215544`) still shows matched `public-beta` runtime slot, runtime pack, active `sts2.dll`, and branch-switch pending-Pull save-origin posture, but it is not release signoff: the device was locked, no UI cleanup action was run, focused startup logcat was stale/missing, and no `last_game_version_cache_cleanup.txt` existed yet to prove runtime-pack cleanup on device.
- ARM64 branch-cache cleanup evidence now exists for the local fix23 package: after injecting a controlled dummy orphan runtime-pack directory, `artifacts/android/multi-version-runtime-after-stale-runtime-pack-cleanup-20260618-221746` shows `last_game_version_cache_cleanup.txt` preserving selected `public-beta` cache `public-beta-8128824d`, preserving selected runtime pack `public-beta-8128824d`, removing orphan runtime pack `stale-proof-runtime-pack` with `existsAfterDelete=false`, and recording `Removed runtime pack count: 1`. This proves runtime-pack cleanup/preservation on the local debug build only; release-candidate public/default cleanup, fresh startup logcat, and full public evidence review remain open.
- Fresh public and public-beta runtime evidence on the local ARM64 fix27/fix23 package line now passes the branch-specific review wrapper. Public evidence `artifacts/android/multi-version-runtime-public-20260618-231242` passed `review-multi-version-runtime-evidence.ps1 -RequirePublic -RequireSaveSafety` with 30 checks after a public game launch, and sanitized public-share export `artifacts/android/multi-version-runtime-public-20260618-231242-public-redacted` passed `review-public-evidence-redaction.ps1`. Public-beta evidence `artifacts/android/multi-version-runtime-public-beta-20260618-224239` passed `review-multi-version-runtime-evidence.ps1 -RequirePublicBeta -RequireSaveSafety` with 42 checks after a clean package restart. Together with `artifacts/android/multi-version-runtime-branch-switch-20260618-211533`, the combined gate now passes with public, public-beta, branch-switch, and save-safety evidence. This is not release-candidate signoff because these are local debug package artifacts; private/password/no-manifest negative cases and release-candidate APK evidence remain open.
- Read-only fix27 version-selection marker capture `artifacts/android/steam-version-selection-fix27-readonly-marker-status-20260618-2342` records the current ARM64 device state without mutating saves or Steam Cloud, and sanitized export `artifacts/android/steam-version-selection-fix27-readonly-marker-status-20260618-2342-public-redacted` passes `review-public-evidence-redaction.ps1`. `branch-markers/marker-evidence-status.txt` proves public and `public-beta` `steam_branch.txt` markers are present, branch-switch warning evidence is present, selected-version Pull evidence is present, and cleanup evidence is present. It also explicitly shows `last_steam_branch_availability.txt`, `last_game_version_redownload.txt`, `last_manual_cloud_push.txt`, and `last_manual_cloud_push_blocked.txt` are missing, with `pre-push-backup-counts.txt` showing `0` local and `0` cloud pre-Push backups. Treat refresh/dropdown evidence, redownload evidence, blocked-Push evidence, successful Push evidence, and branch-switch backup coverage as still missing for release-candidate signoff.
- Fix27 refresh/dropdown ARM64 evidence now exists at `artifacts/android/steam-version-selection-fix27-refresh-dropdown-20260618-2348`, with sanitized export `artifacts/android/steam-version-selection-fix27-refresh-dropdown-20260618-2348-public-redacted` passing `review-public-evidence-redaction.ps1`. The UI screenshot set shows launcher-only startup, `REFRESH GAME VERSIONS` under support options, refreshed status `Steam game version list refreshed. Selected version: Default.`, dropdown label `Default / public (ready)`, and dropdown option `public-beta (build 23575630)`. The captured `last_steam_branch_availability.txt` records selected branch `public`, selected branch visible in Steam metadata, one Windows depot manifest for selected branch, visible branch count `2`, public build `23478716`, and `public-beta` build `23575630`. This closes the local fix27 refresh/dropdown proof gap only; release-candidate repeat evidence and private/password/no-manifest negative cases remain open.
- Fix27 synthetic unavailable saved-branch evidence now exists at `artifacts/android/steam-version-selection-fix27-unavailable-saved-branch-20260619-0000`, with read-only public-share export `artifacts/android/steam-version-selection-fix27-unavailable-saved-branch-20260619-0000-public-redacted` passing `review-public-evidence-redaction.ps1`. The device was seeded with saved branch `stale-private-proof` while the latest Steam branch availability marker only listed `public` and `public-beta`; the launcher surfaced the selected stale branch, warned that it was not listed in the Steam app-info catalog, and blocked `DOWNLOAD SELECTED VERSION` before any selected-version download/update. The device branch was restored to `public`, a follow-up device search recorded no residual `stale-private-proof` paths under app files, and no Steam Cloud Push was performed. This proves the local stale/absent saved-branch block path only; real account-side password-protected, private/inaccessible, and no-Windows-manifest branch cases still need release-candidate evidence or explicit release-note limitation.
- Fix27 immediate-launch smoke evidence now exists at `artifacts/android/fix27-immediate-launch-smoke-20260619-0015`, with public-redacted text export `artifacts/android/fix27-immediate-launch-smoke-20260619-0015-public-redacted` passing `review-public-evidence-redaction.ps1` after exporter/reviewer hardening for ad hoc raw logcat captures. It proves the installed local package `0.2.188-local-runtime-beta-fix27` / `versionCode=218853` starts from `LauncherActivity`, keeps a live package PID, focuses `GodotApp`, shows the public/default ready launcher UI, and has no strict package crash markers or `NativeFallbackActivity` after launch. The read-only marker capture still shows no `last_manual_cloud_push.txt` or `last_manual_cloud_push_blocked.txt`; no Steam Cloud Push was performed. This is local debug package immediate-crash evidence only, not release-candidate signoff.
- Fix27 public/default game-launch evidence now exists at `artifacts/android/fix27-public-game-launch-smoke-20260619-0027` and structured runtime evidence `artifacts/android/multi-version-runtime-public-20260619-002349`, with sanitized exports `artifacts/android/fix27-public-game-launch-smoke-20260619-0027-public-redacted` and `artifacts/android/multi-version-runtime-public-20260619-002349-public-redacted` both passing `review-public-evidence-redaction.ps1`. The screenshot shows the Slay the Spire 2 main menu with `Profile 1`; focused logs show selected branch `public`, PCK `files/game/SlayTheSpire2.pck` hash `8f0dbfef10a31994eb0f58e8d811db08712153c5c0d4491bc5fc4732be530f68`, source and active Android `sts2.dll` hash `81c8f3443c4504e38a17570df688489414fceb6ea7fcf5b044d8117318ea8e49`, runtime slot `public-d8a7082fc63977cc`, runtime patch compatibility `passed`, and no strict crash markers or `NativeFallbackActivity`. `review-multi-version-runtime-evidence.ps1 -RequirePublic -RequireSaveSafety` passed 30 checks. The report still classifies Steam Cloud Push as `do-not-push` because save-origin evidence is stale/mismatched after the earlier branch switch; no Push marker was created. This proves local fix27 public/default game launch on ARM64, not release-candidate signoff.
- Fix27 selected-version redownload marker evidence now exists at `artifacts/android/fix27-redownload-synthetic-cache-20260619-0035`, with sanitized export `artifacts/android/fix27-redownload-synthetic-cache-20260619-0035-public-redacted` passing `review-public-evidence-redaction.ps1`. The test seeded a synthetic side-by-side branch cache `redownload-proof-2851fdaf` with a valid PCK header but no `steam_branch.txt`, selected that branch, and verified the launcher surfaced the missing/mismatched branch metadata warning plus blocked-cache-delete confirmation because the branch was absent from Steam app-info. Confirming it wrote `last_game_version_redownload.txt` with selected branch `redownload-proof`, side-by-side slot directory, game directory existed before delete `true`, game directory exists after delete `false`, download state after delete `false`, and runtime-pack after delete `false`. The selected branch was restored to `public`, the synthetic slot was removed, the real `public-beta` cache marker remained present, and no Steam Cloud Push marker was created. This proves local selected-cache deletion/redownload marker behavior without deleting real beta content; full release-candidate evidence for real selected-version redownload and replacement download remains open.
- Fix27 exposed a branch-switch Push safety bug: with selected branch restored to `public` while `current_android_save_origin.txt` still recorded `public-beta` pending Pull, tapping `Push Saves to Steam Cloud` armed the destructive confirmation UI instead of writing `last_manual_cloud_push_blocked.txt`. The confirmation was not accepted and no Steam Cloud Push was performed. Evidence is preserved at `artifacts/android/fix27-blocked-push-save-origin-20260619-111324`.
- Fix28 moves the selected-version Push safety gate ahead of the arming UI. Evidence APK `0.2.188-local-runtime-beta-fix28-evidence` / `versionCode=218855` proves selected branch `public` with stale `public-beta` save-origin evidence blocks on the first Push tap, shows `Push blocked: Pull from Cloud must complete for selected game version Default before Push.`, does not show `CONFIRM: OVERWRITE STEAM CLOUD`, advances `last_manual_cloud_push_blocked.txt` to `2026-06-19T10:21:36.3012281Z`, and leaves `last_manual_cloud_push.txt` absent. Standard capture is `artifacts/android/fix28-evidence-blocked-push-save-origin-20260619-112107`; sanitized export `artifacts/android/fix28-evidence-blocked-push-save-origin-20260619-112107-public-redacted` passed `review-public-evidence-redaction.ps1`. No Steam Cloud Push was performed. This closes the local blocked-Push-before-confirmation proof gap only; successful Push remains destructive and release-candidate signoff still requires Pull/local-save/pre-Push-backup evidence on the selected version.
- Fix28 read-only current marker evidence now exists at `artifacts/android/fix28-readonly-current-marker-status-20260619-113732`, with curated text-only public export `artifacts/android/fix28-readonly-current-marker-status-20260619-113732-public-redacted` passing `review-public-evidence-redaction.ps1`. It proves the connected device was still on local package `0.2.188-local-runtime-beta-fix28-evidence` / `versionCode=218855`, selected branch `public`, public and public-beta branch markers both present, public-beta depot manifest differing from public, SteamKit debug logs disabled (`null`), `last_manual_cloud_push.txt` missing, `last_manual_cloud_push_blocked.txt` present with before-upload block reason, and pre-Push local/cloud backup counts both `0`. The saved encrypted Steam session files were still present after the native-login-panel test with hashes matching the earlier local raw evidence. This was read-only; no Pull from Cloud or Push to Cloud was performed.
- Fix28 capture-script hygiene evidence now exists at `artifacts/android/fix28-readonly-capture-script-diagnostics-index-20260619-1200`, with curated text-only public export `artifacts/android/fix28-readonly-capture-script-diagnostics-index-20260619-1200-public-redacted` passing `review-public-evidence-redaction.ps1`. It verifies `capture-steam-version-selection-evidence.ps1` now writes a bounded `diagnostics/launcher-diagnostics-index.txt` for external diagnostics discovery instead of accidentally collecting a device-root listing; the fixed read-only capture produced a 276-byte index. No Pull from Cloud or Push to Cloud was performed.
- Re-run full login/Pull/confirmed-Push/game-launch smoke on the clean public `v0.2.187-beta-art-fallback` release-facing build.
- Keep Push treated as destructive. The newest public APK has confirmation/cancel safety evidence, but confirmed Push mutation still needs an explicit newest-public smoke before release-candidate signoff.
- Repeated local stale assembly cache/freshness checks across in-place local upgrade once signing continuity is restored.
- Repeat release asset hygiene on every new release: signer, package name, versionCode monotonicity, checksums, structural verifier, and GitHub release notes.
- Public evidence hygiene now has an executable guardrail: new version-selection evidence folders include `PUBLIC_EVIDENCE_REDACTION_REVIEW.txt`, and `scripts\review-public-evidence-redaction.ps1 -EvidenceDir <folder>` must pass before public artifacts count toward release signoff. Raw runtime evidence can be exported into a separate sanitized public-share candidate with `scripts\export-public-evidence-redaction.ps1`, which leaves the raw folder local, redacts text artifacts, omits images by default, skips raw/ad hoc logcat and raw focused startup extracts, and still requires the redaction reviewer to pass. This is a local review gate for screenshots/logs that may expose credentials, account identifiers, device notifications, private save/profile data, local paths, or device IDs.
- Steam branch availability summaries now classify password-protected branches as `password-protected` instead of `downloadable` even if Steam exposes Windows depot manifests; download/update gates already block those branches until beta password entry is implemented.
- Compact launcher branch-availability failure text and diagnostics now also treat `passwordRequired=true` marker metadata as `password-protected` for the selected branch and visible branch list, so a password-protected branch with exposed Windows manifests is not described as downloadable in failure/status guidance or evidence reports.
- Further diagnostics polish so normal successful startup/cloud-save behavior is not hidden by remaining low-value platform logs.
- Improve persisted Steam session/update UX so game update checks do not appear to require unnecessary re-login when a saved session is still valid.
- Validate Android/Samsung/password-manager login behavior on ARM64; see [Android Steam login validation](android-steam-login-validation.md). Android now uses an integrated native credential panel with real username/password fields, credential-provider hints, and Steam web-domain metadata; the old native one-shot handoff popup is no longer user-facing. Diagnostics prove the app does not store or inject Steam passwords, and SteamKit debug logs are disabled by default with opt-in sanitized diagnostics via `sts2_steamkit_debug_logs=1`. Local fix28 evidence at `artifacts/android/fix28-native-login-panel-cancel-20260619-112725` proves the native panel appears after saved-session removal, Back/Cancel dismiss it without exiting the app, it can reopen immediately, empty submit is handled inline without Steam authentication, and Samsung Pass/Android Autofill recognize the Steam web domain plus username/password fields. The provider reported no matched saved Steam credential, so matched credential suggestion behavior remains unproven. Manual real credential entry, Steam Guard, failed login, successful return to launcher, Google Password Manager behavior, and release-candidate repeat evidence remain open.

## Device-independent polish completed after baseline proof

- Local smoke/login/verification scripts now select APKs by parsed package metadata and versionCode, with ABI/package compatibility checks where applicable, instead of relying only on file write time.
- Push-to-Cloud warning text now names Steam Cloud explicitly, calls out overwrite risk before upload, and directs testers to Pull first and verify Android local saves exist before pushing.
- Manual cloud-sync start/complete/failure status updates now keep the launcher header aligned with the operation result instead of leaving stale generic status text behind.
- Recovery cleanup logging now describes normal post-startup cleanup as success-path UI cleanup.
- Diagnostics filters retain startup freshness, assembly cache, expectedSource/expectedBytes, cloud sync, and crash evidence while reducing broad log noise.
- Native splash now uses the scalable launcher vector icon, shader-warmup/loading panel sizing is short-edge aware, startup status text is safe-margin anchored for short/wide Android screens, and the launcher itself now uses a short-edge-aware responsive shell with collapsible diagnostics.

## Static upgrade/cache freshness review

The Android activity cache path has a usable static evidence chain for upgrade/freshness diagnosis:

- Startup freshness logs package, versionName/versionCode, schema, stored schema, stored versionCode, stored package, runtime arch, cache existence, and `STS2Mobile.dll` bytes.
- Assembly setup logs `cache-hit` versus full recopy and emits `expectedSource`/`expectedBytes` for required managed assemblies.
- Public package runtime proof exists for `v0.2.175 -> v0.2.177`; the remaining gap is local-package in-place upgrade proof after `.local` signing continuity is restored.

## Emulator limitation

ARM64 hardware is the proof target for Steam login, download, cloud sync, and game launch. Android `x86_64` emulator coverage is useful for install/routing/native-fallback diagnostics only; forcing Godot on `x86_64` remains a crash-prone diagnostic path, not release proof.
## 2026-06-14 public-beta run-history asset fallback validation

- Evidence APK: `0.2.617-local-beta-art-fallback` (`versionCode=2261217`) installed on the connected Android device.
- Evidence folder: `artifacts/android/runtime-public-beta-art-fallback-20260614`.
- Runtime selected Steam branch: `public-beta`.
- Runtime selected PCK: `/data/user/0/com.sts2launcher.overhaul.fork.local/files/game_versions/public-beta-8128824d/game/SlayTheSpire2.pck`, `sha256=957bd95f2bbe97fad18ea467e67b8525861a49aec08a0f31448e276925cb684a`.
- Startup patch orchestration applied `gameplay/Run history asset fallback`.
- Missing beta run-history paths `res://images/ui/run_history/doormaker_boss.png` and `res://images/ui/run_history/doormaker_boss_outline.png` now resolve to branch-local `unknown_monster` fallback art.
- Focused runtime check found `old_doormaker_loader_error_count=0`.
- Main menu reached: `Main menu present after startup: MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NMainMenu name=MainMenu`.
