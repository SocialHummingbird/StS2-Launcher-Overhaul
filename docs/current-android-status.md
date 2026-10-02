# Android testing status

Updated October 2, 2026. This page separates local validation from physical-device and live-service results.

## Current release: v0.2.435-preload-repair-local-r2

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

- Original freeze reproduction and the current candidate's 20-launch physical acceptance, including returning from background and lock screen.
- Audio and graphics on more devices and drivers.
- Real Steam Cloud transfers and switching play between PC and Android.
- Android mod loading and each mod's actual in-game behavior.
- LAN multiplayer across devices.

Password-protected Steam branches are not supported. x86_64 emulator builds are for diagnostics, not the supported game target.

For help, see [Troubleshooting](android-troubleshooting.md). Current ownership is in [the reduction inventory](../REDUCTION_INVENTORY.md); launch investigation and pending device acceptance are in [the October audit](launch-flow-audit-2026-10-02.md). Dated release/refactor notes retain their original build-specific scope.
