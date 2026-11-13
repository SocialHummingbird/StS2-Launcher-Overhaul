# Android testing status

Updated November 13, 2026. This page separates local validation from physical-device and live-service results.

## Current release: v0.2.438

[Release notes](release-notes/v0.2.438.md), [ARM64 APK](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/download/v0.2.438/StS2Launcher-v0.2.438-arm64-v8a.apk), [checksum](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/download/v0.2.438/StS2Launcher-v0.2.438-arm64-v8a.apk.sha256).

This release replaces whole-file game transfers with verified chunk reuse, bounded
parallel fetching and persistent staging, and adds Pause/Resume routing. It also fixes
NUL-terminated Steam manifest filenames before staging suffixes are appended.

| Check | Observed result |
| --- | --- |
| Managed checks | 266 regression tests passed, including reuse, shared worker limits, interruption recovery, prepared-PCK authority, filename/path validation and obsolete-file cleanup before promotion. |
| Android/JVM | 56 tests passed with no failures or skips. Android lint still reports 15 errors and 39 warnings; every error location's source is unchanged from v0.2.437. Lint is not a clean pass. |
| Live Fold transfer | Candidate `438013`, SM-F971B, public-beta manifest `5756684179086186957`: 3,160,619,181 logical bytes transferred and verified. Raw Steam PCK verification passed before Android preparation reached v2. |
| Pause and process death | Actual UI Pause after 36,436,880 compressed bytes, then Resume and force-stop after another 34,646,944. Relaunch offered Resume; none of the 212 pause-verified or 387 restart-verified distinct staging identities was fetched again. |
| Concurrency | Three logical workers, including one large PCK; Java workers also peaked at three. |
| CDN reuse fixture | Synthetic moved-range layout over real Steam CDN: 14,215,464 logical bytes reused, exactly one missing 281,184-byte compressed chunk requested, expected final SHA-1. This is not a real same-branch update or a performance baseline. |
| Runtime limitation | Full sandbox runtime readiness rejected an identity mismatch because Java reads the normal app directory while C# uses the sandbox. The native guard was not bypassed. This does not establish a production runtime defect; full fresh-install readiness remains incomplete. |
| Preservation | Normal `438014` passed offline startup and remained installed/stopped. All 1,178 protected entries compared with zero semantic user-data differences; only documented runtime metadata changed. Saves, credentials, installed versions and four typed user preferences matched. |
| Release packaging | Final release APK verification is recorded in the release notes before publication; candidate results above must not be attributed to an untested release install. |

Detailed commands, identities, interruption observations and evidence gaps are in the
[download validation record](../tools/DownloadValidation/README.md#manifest-filename-fix-and-focused-fold-verification-13-november-2026).
Separate PCK corruption/retention acceptance, accessible same-branch update cases,
paired speed comparisons and gameplay after a fresh download remain unverified.

## Previous release: v0.2.437

[Release notes](release-notes/v0.2.437.md), [ARM64 APK](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/download/v0.2.437/StS2Launcher-v0.2.437-arm64-v8a.apk), [checksum](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/download/v0.2.437/StS2Launcher-v0.2.437-arm64-v8a.apk.sha256).

The release restores game-owned input initialization using the change tested in
candidate `437001`, recorded below. The final APK has not been installed on a
phone; its checks establish packaging and update compatibility, while the
functional observations concern the candidate.

| Check | Observed result |
| --- | --- |
| Build source | `9066e566dca7b131cfc9cddbeada4a97f935e607`; the production file has the same SHA-256 recorded for the phone-tested candidate below. |
| Managed checks | Release build with analyzers passed, zero errors and zero warnings on this incremental run. Scoped `dotnet format --verify-no-changes` passed. 220 managed regressions passed before merge and again on merged `main`. |
| Android checks | 56 JVM tests passed, none failed or skipped. Android lint reports 15 errors and 39 warnings, matching the retained pre-merge baseline with no new diagnostic identities; the existing Gradle configuration does not fail on lint errors. |
| Final APK | `0.2.437` / `437002`, `arm64-v8a`. Build, contents and Android crypto checks passed. SHA-256: `b3ca14be47d7b3e5a8e465d33b809877e9c38b1840db1e818cda715496c67169`. |
| Update compatibility | Passed against v0.2.436 (`436009`) and the installed candidate (`437001`): same package `com.sts2launcher.overhaul.fork.local`, certificate `FD0E3D5ACF435C1D23BFC5C426E99AA9EB5808619FF1FC214FFCA99CFAC7E57A`, and higher version code. |
| Remaining device checks | Install the final APK; actual confirm-shortcut activation, physical keyboard/controller input and the separate stability procedure remain unverified. |

The final APK, checksum and metadata are under `artifacts/android/` as
`StS2Launcher-v0.2.437-arm64-v8a.apk` and its `.sha256` and `.json` sidecars.
Release build, lint, regression and update-check logs are retained under
`artifacts/input-initialization/`.

## Phone-tested candidate: game-owned input initialization

`0.2.437-input-init-local` / `437001` was built locally and installed in place on
the SM-F971B test phone; this candidate artifact was not published. The existing Android input hook
now loads real settings only when needed, then allows the selected game's original `NInputManager.Init`
to initialize its keyboard and controller bindings. Settings-load exceptions
return a faulted task to the existing game error handler. The Android Steam-input
fallback remains in place.

The change removes the copied mapping initialization, its reflection helpers and
the premature mappings-initialized message: 131 production lines removed and 13
added. The publicizer/input-layout tests from PR #49 were not present on `main`
and were not imported; the existing original-visibility and preload tests remain.

| Check | Observed result |
| --- | --- |
| Source | Change from base `c64b52ae5f6d65089e8f7159fef40f33426dcd89`, now committed as `9066e566dca7b131cfc9cddbeada4a97f935e607`. `ControllerInputPatches.cs` SHA-256: `57e0dcda47e2f12f57a3270e295c97819568457766fe1cc1fed9d2483f6496ef`. |
| Managed build | Passed with 92 warnings and zero errors. |
| Managed regression suite | 220 passed, zero failed. These existing tests do not establish working Android keyboard bindings. |
| Release preparation | Fresh managed build/analyzers and scoped `dotnet format --verify-no-changes` passed; 220 managed and 56 Android/JVM tests passed. Android lint reports 15 errors and 39 warnings, matching the retained pre-merge baseline with no new diagnostic identities. Gradle's existing configuration does not fail on lint errors. |
| Packaged launcher assembly | The prefix has the planned `bool(ref Task)` signature, retains the Steam fallback, and contains none of the removed initialization/mapping methods. |
| ARM64 APK | Contents and Android crypto-patch checks passed. SHA-256: `dacf19b5a9ccc62c97f822bc05941ecbf677ea7cba9605056d9f154400982a52`. |
| Update compatibility | Passed against published v0.2.436 (`436009`): same package `com.sts2launcher.overhaul.fork.local`, same certificate `FD0E3D5ACF435C1D23BFC5C426E99AA9EB5808619FF1FC214FFCA99CFAC7E57A`, higher version code. |
| Device baseline | SM-F971B, Android 17, installed `0.2.436` / `436009`. Its pulled APK matches the retained release byte for byte (SHA-256 `485bee90205da1fa7981a244911da27732ce1515efa7e4ab8b82c2adf0e56200`). On a cold launch the keyboard columns were empty and E did not end combat turn 1, while touch worked. |
| Game identity | Selected `public-beta`, v0.111.0. Source and active `sts2.dll` SHA-256 both `0861bfa1df347538d932f22d580e75420f08082792eb914e53b4882764acdbe9`; installed game identity `9eb628025c148dd818cb80ca514892303bd2f8aeb7d2602aac2a92efac850b57`. |
| Candidate installation | `adb install -r` succeeded without uninstalling or clearing data; Android reports `0.2.437-input-init-local` / `437001`. |
| Default keyboard bindings | Reset through the game UI, then force-stopped and restarted. In Mouse + Keyboard, one E advanced turn 1 to turn 2 (80 to 68 HP). After switching with an arrow key, Keyboard-Only Mode showed its own prompts: Enter did not end turn 2, and one E advanced it to turn 3 (68 to 62 HP). |
| Saved keyboard bindings | Rebound through the game UI, then force-stopped and restarted again. Mouse + Keyboard: E left turn 1 unchanged; one Q advanced it to turn 2. Keyboard-Only: E left turn 2 unchanged; one R advanced it to turn 3, with the R prompt displayed. The UI paired mouse/keyboard confirm and end turn on Q while keyboard-only confirm stayed on Enter. |
| Startup and touch | Three candidate cold game launches reached the title screen, loaded real settings through the prefix and initialized the Godot fallback. Captured process logs contain no input-initialization exceptions or old missing-map warnings. Touch navigated settings and played Bash and Strike in the temporary run. Offline save synchronization logged TimeoutException and continued locally with automatic Push paused. |
| Restoration | Original bindings restored (mouse/keyboard E confirm/end turn; keyboard-only Enter confirm, E end turn), Keyboard-Only preference off, profile 1 selected, and Modded with the original four enabled mods selected again. Only the initially empty Vanilla profile 3 was used and then deleted through the game UI; it shows Empty again. Profile 1 retained its displayed playtime/update date and Continue option. Wi-Fi and mobile data restored to their original enabled states; app stopped. Private save files were not directly inspected or overwritten, so no byte-for-byte preservation claim is made. |
| Coverage limits | Keyboard checks used ADB application-level key events. Physical keyboard and controller were not exercised; none was attached in the input-device inventory. Actual confirm-shortcut activation in a supported confirmation context remains unverified; the character-selection button overrides its hotkey to Select, so its response to E is not confirm evidence. The settings-load failure contract was reviewed in source, without injecting a device failure. |

The observed default and saved end-turn behavior verifies the initialization
repair on this split-map beta build. It does not establish every keyboard action
or physical-device input path. Resetting or rebinding during a session alone was
not counted as initialization evidence; the functional checks above followed
process termination and a fresh game launch.

The APK, checksum and metadata are under `artifacts/android/` with the name
`StS2Launcher-v0.2.437-input-init-local-arm64-v8a.apk`. Local build, regression and
update-check logs are under `artifacts/input-initialization/`; phone screenshots
and process logs are in its `android/` directory. A device/Steam save conflict was
left unresolved without choosing either source. Acceptance ran offline to
preserve both copies; the conflict still needs the user's source choice.

## Previous release: v0.2.436

[Release notes](release-notes/v0.2.436.md), [ARM64 APK](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/download/v0.2.436/StS2Launcher-v0.2.436-arm64-v8a.apk), [checksum](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/download/v0.2.436/StS2Launcher-v0.2.436-arm64-v8a.apk.sha256).

This release replaces launcher-owned mod parsing/loading and activation
guesses with the selected game's original framework. It removes the restricted
BaseLib initializer, named patch exclusions, assembly publicizing and replacement
model construction. Android Mono and Harmony now have pinned source recipes.

| Check | Observed result |
| --- | --- |
| Managed / Android JVM | 220 / 56 passed. This includes committed-package recovery, writable package state, fresh result identity, cold reopening, read-only background migration and rejected stale UI completions. |
| Pre-merge lint | Managed analyzers and source whitespace/syntax checks pass. Android lint reports the same 15 errors and 39 warnings on this change and `main`; comparison finds no new diagnostics. The existing Gradle configuration does not fail on lint errors. |
| Original loader contracts | Both supported game initialization shapes pass in separate desktop processes, including original discovery, dependency ordering, multiple manifests, exact manual selection, early secondary scripts and original failure propagation. |
| Desktop representative packages | Five unmodified packages initialize; original model construction, serialization-cache initialization and repeated act/encounter access pass. |
| In-place phone update | Candidate `436008` installed over the existing package with the same signing certificate; no uninstall or application-data clearing. |
| Published APK | `0.2.436` / `436009`, built from `33abc177097fe55bd2c630756f55ecd5e7a60ef8`. Contents, crypto, signing, checksum and update compatibility pass. It was not phone-tested at publication; device evidence in this table concerns candidate `436008`. The November 2 keyboard baseline above subsequently used the installed final APK. |
| Steam preparation on phone | Authenticated `mysubscriptions` query returns all four subscriptions. All four committed packages prepare successfully; their 17 payload files match the desktop packages. |
| Android host | Ordinary MD5, SHA-1, SHA-256 and background-stream SHA-256 pass on the Java-hosted phone process. Importer profile controls and BaseLib configuration render and respond through the unmodified packages' Godot scripts. |
| Runtime gate | The original ARM64 method-pointer preparation and protected-member access failures are reproduced. The corrected source pair passes the integrated ARM64 contract, including 92 named-assembly access checks, cached code, exception regions, assembly context, first invocation and required initialization; 64 worker collections/callbacks also pass. Candidate `436008` has no cached `UnlockState` failure or protected-field exception in the observed combat. x86_64 is built but not executed. |
| Android functional flow | Unmodified ARAM runs through actual combat, card awards and shared BaseLib hooks. QuickRestart restores the original enemy HP, energy and hand. Save and Quit followed by process termination and cold Continue preserves the modded card, rune and custom run configuration; their combat effects still execute after reload. |
| CookieCursor settings | Unmodified CookieCursor adds working settings controls, writes its sibling configuration and reloads the saved setting after process termination. Physical-mouse cursor appearance and multiplayer behaviour were not observed. |
| Isolation and status | Candidate `436008` Vanilla completion has zero sources, zero mods and native `IsRunningModded=false`. Disabled manual sources are absent from Modded discovery. Cold reopening shows matching Workshop/manual results; changing a companion config invalidates them, and a new launch establishes a matching result. |
| State preservation | All 403 game/save/settings files match the October 26 pre-test snapshot byte for byte. Newer user progress is preserved. Temporary packages, helper application and device backup directory are removed; profile 1 and connectivity are restored. All four Workshop choices remain enabled because the user's newer active run contains ARAM data. |

These results cover the shared loader/runtime path and the observed Android
functionality.
The original Whirlwind failure was traced to the shared protected-access contract;
the subsequent run offered Battle Trance, exercising the same card-award path.
The final ARM64 APK was published as the latest GitHub release at that time. These checks
do not certify every Workshop package or replace the separate stability procedure
recorded below. Evidence is retained under `artifacts/mod-framework-device/`.

## Previous release: v0.2.435-preload-repair-local-r2

[Release notes](release-notes/v0.2.435-preload-repair-local-r2.md), [ARM64 APK](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/download/v0.2.435-preload-repair-local-r2/StS2Launcher-v0.2.435-preload-repair-local-r2-arm64-v8a.apk), [checksum](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/download/v0.2.435-preload-repair-local-r2/StS2Launcher-v0.2.435-preload-repair-local-r2-arm64-v8a.apk.sha256).

The connected phone reproduced the v434 title-screen freeze. Android preparation publicizes game members, but the existing preload patch searched only nonpublic members and failed installation. Both lookup helpers are repaired in place, with all six field and three method shape checks retained. See the [repair and validation report](title-screen-preload-repair-2026-10-02.md) and existing [PR #48](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/pull/48).

The first candidate, code `435001`, still froze on its fifth cold Modded attempt
with all three prefixes installed. The captured native stacks and an actual Android
Mono regression exposed the existing bootstrap leaving the native engine thread
GC-unsafe after first delegate creation. That bootstrap now enters GC-safe native
execution at the same boundary; later callbacks and 64 worker collections pass.

156 managed tests, 56 JVM tests, all 12 save-safety scenarios, the real-resource
probe and the Android Mono regression pass. The signed ARM64 r2 candidate
(code `435002`) passes APK/crypto/update-compatibility checks and installed in place
without resetting app data. Two full cold Modded and two Vanilla launches pass
Common completion, 60-second title interaction, gameplay entry and Save and Quit.
A third Modded launch completes Common but is excluded after the user removes
the phone during title observation. No r2 freeze was observed. The phone is now
unavailable; the remaining comparison and 18 Modded successes, including warm
and lifecycle cases, are pending. The original failure captures and first APK
are retained. PR #48 is merged and r2 is published at the user's explicit request
before completing physical acceptance; freeze resolution is not yet established.

## Previous release: v0.2.434-cleanup-local

[Release notes](release-notes/v0.2.434-cleanup-local.md), [cleanup and validation report](system-cleanup-2026-10-02.md), [ownership inventory](../REDUCTION_INVENTORY.md), [ARM64 APK](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/download/v0.2.434-cleanup-local/StS2Launcher-v0.2.434-cleanup-local-arm64-v8a.apk), [checksum](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/download/v0.2.434-cleanup-local/StS2Launcher-v0.2.434-cleanup-local-arm64-v8a.apk.sha256).

| Item | Result |
| --- | --- |
| Package / version code | `com.sts2launcher.overhaul.fork.local` / `434001` |
| Managed tests | 154 passed |
| Android/JVM tests | 56 passed |
| Save-safety scenarios | 12 passed, simulated remote |
| Real-resource preload / UI / mod fixtures | Desktop checks passed; see report for exact scope |
| APK | ARM64 build/contents/crypto passed; matches published v0.2.432 and retained v0.2.433 package and certificate with increasing code |
| Physical phone at publication | Unavailable; no install or launch validation then. A later connected-phone investigation reproduced the freeze. |
| Original title-screen freeze / Android Steam transfer | Freeze reproduced on v434 and the first v435 candidate; r2 has two full Modded and two Vanilla passes, with full acceptance pending after device removal. Real Android Steam transfer remains pending. |

Published at the user's request after local verification. No installation or data reset was performed. The existing 20-launch acceptance procedure remains required; publication does not establish device stability.

## Previous release: v0.2.432

[Release notes](release-notes/v0.2.432.md) record 140 managed tests, 53 Android tests, 12 save scenarios, desktop resource probes and emulator checks. The ARM64 artifact was not physical-device tested.

## Earlier device record: v0.2.431

[Download and release notes](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/tag/v0.2.431)

| Item | Result |
| --- | --- |
| Target | ARM64 Android |
| Package | `com.sts2launcher.overhaul.fork.local` |
| Android version code | `431000` |
| Managed tests | 133 passed |
| Android unit tests | 45 passed |
| Save-safety scenarios | 12 passed, using a simulated remote |
| Launcher UI checks | Passed at phone and desktop sizes |
| APK checks | Build, contents, architecture, and crypto checks passed |
| Samsung SM-F971B, Android 17 | Installed over the existing app; launcher initialization completed |
| Full game loading on this APK | Still unverified; the selected game branch was mid-download during the check |

The simulated save tests do not prove a real Steam Cloud transfer. Desktop mod tests do not prove that a mod works on Android. The in-app updater's full download-to-install cycle also needs a device test with a newer compatible APK.

## Earlier results

- **v0.2.429 RC4:** ten successful launches on the Samsung SM-F971B, including cold/warm starts, background/resume, and lock/unlock. See the [original report](release-notes/v0.2.429-issue38-arm64-rc4.md).
- **v0.2.430 RC3:** the reporter of missing music and delayed sound effects confirmed on August 29 that both worked after updating. See [their reply on issue #37](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/issues/37#issuecomment-5461909959). That fix is included in v0.2.431.

Those results apply to those builds and devices; they are not a completed game test of v0.2.431.

## Still needs testing

- Install and test the published v0.2.437 APK (`437002`) on an ARM64 phone. The keyboard evidence above comes from candidate `437001`; actual confirm-shortcut activation and physical keyboard/controller input remain unverified. The installed v0.2.436 APK was used as the keyboard baseline above.
- Complete the [20-launch stability procedure](launch-flow-audit-2026-10-02.md#physical-device-acceptance--pending-at-the-users-direction) on the same APK, including warm launches, background/resume and lock/unlock. The earlier freeze was captured; its resolution still needs this acceptance testing.
- Audio and graphics on more devices and drivers.
- Real Steam Cloud transfers and switching play between PC and Android.
- Additional Workshop packages, game versions and untested mod features, including physical-mouse cursor behaviour and multiplayer interactions.
- The in-app updater's full download-to-install cycle with a newer compatible APK.
- LAN multiplayer across devices.

Password-protected Steam branches are not supported. x86_64 emulator builds are for diagnostics, not the supported game target.

For help, see [Troubleshooting](android-troubleshooting.md). Current ownership is in [the reduction inventory](../REDUCTION_INVENTORY.md); launch investigation and pending device acceptance are in [the October audit](launch-flow-audit-2026-10-02.md). Dated release/refactor notes retain their original build-specific scope.
