# Android testing status

Updated September 6, 2026. This page separates available features from what has actually been tested.

## Current release: v0.2.431

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

- Full game loading and repeated launches on v0.2.431, including returning from the background and the lock screen.
- Audio and graphics on more devices and drivers.
- Real Steam Cloud transfers and switching play between PC and Android.
- Android mod loading and each mod's actual in-game behavior.
- LAN multiplayer across devices.

Password-protected Steam branches are not supported. x86_64 emulator builds are for diagnostics, not the supported game target.

For help with a problem, see [Troubleshooting](android-troubleshooting.md). Implementation details are in the [launch refactor notes](reliable-launch-implementation-2026-09-06.md) and [runtime identity design](issue-38-runtime-identity-design.md).
