# Testing Needed

This project needs focused Android tester reports more than broad "works for me" comments. Good reports help confirm device compatibility, Steam Cloud safety, public/beta branch behavior, and mod loading without exposing Steam account data.

## Current Priority

1. **Startup and loading speed**
   - Time from tapping the app icon to launcher visible.
   - Time from pressing Start Game to game main menu.
   - Any black screen, native fallback screen, or app crash.

2. **Public/default game launch**
   - Fresh install or update install.
   - Steam login, game download/update, Pull from Cloud, launch.
   - Whether the expected save/profile appears in-game.

3. **Public-beta or core-release branch switching**
   - Selected branch shown in the launcher.
   - Whether the branch downloads or is blocked clearly.
   - Whether the game launches without silently falling back to public/default.
   - Any mixed public/beta assets, missing UI art, or hard-lock route.

4. **Workshop/mod support**
   - Whether subscribed mods are discovered.
   - Whether the launcher stages or marks the mod as needing manual import.
   - Which mods are selected.
   - Whether vanilla launch disables mods correctly.
   - Whether modded launch reaches the game main menu.

5. **Vanilla and Modded Saves Merger**
   - This is the highest-priority mod compatibility test.
   - Report whether existing vanilla saves become usable when the mod is enabled.
   - Report whether disabling the mod returns to the expected vanilla/modded save behavior.

6. **Samsung/One UI and unusual display sizes**
   - Launcher layout, keyboard, password manager suggestions, and button reachability.
   - Include display size/font size settings when reporting UI problems.

## Known Working Evidence So Far

| Device class | Android ABI | Evidence | Status |
| --- | --- | --- | --- |
| Physical ARM64 Samsung test device | `arm64-v8a` | Public/default, public-beta runtime matching, Steam Cloud Pull, guarded Push behavior, mod selector, manually imported SavesMerger launch | Working in local validation |
| Android x86_64 emulator | `x86_64` | Install/routing/native fallback diagnostics only | Not a game-launch proof target |

Add new device results through the device compatibility issue template. Use [Issue reporting](issue-reporting.md) before attaching logs, branch/runtime evidence, save details, or screenshots.

## What To Include In Reports

- APK release tag and APK filename.
- Device model.
- Android version and vendor skin version, for example One UI.
- Clean install or update install.
- Selected game branch: public/default, public-beta, core-release, or another branch.
- Whether Steam Cloud Pull was run.
- Whether Steam Cloud Push was run. If you are not sure, say so.
- Selected mods and whether launch was vanilla or modded.
- Screenshot if the problem is visual.
- Focused logcat if the app crashes or hangs.
- For branch reports: selected branch, PCK path/hash, runtime pack path/hash, active `sts2.dll` hash, runtime cache marker, and patch validation marker when available.
- For mod/save-merger reports: selected mods, source of each mod, enabled/disabled state, whether existing saves became visible and loadable, and whether Push to Cloud stayed blocked or was intentionally run.

## Do Not Share

- Steam password.
- Steam Guard codes.
- Steam refresh tokens.
- Full unsanitized logs containing account names, tokens, or local private paths.
- Save files publicly unless you intentionally want them visible.

## Quick Logcat Capture

Run this while reproducing a crash or hang:

```powershell
adb logcat -c
adb logcat -v time > sts2-mobile-logcat.txt
```

Stop the command after the issue occurs, then search the file for these terms before attaching the focused section:

```text
AndroidRuntime
FATAL EXCEPTION
Godot
STS2Mobile
PatchHelper
Steam
SteamKit
Cloud
Workshop
Mods
NativeFallback
```

Prefer a small focused excerpt around the failure over a full raw log.

## Best Current Tester Flow

1. Install the latest APK from GitHub Releases.
2. Open the launcher and record whether it reaches the main launcher screen.
3. Log in to Steam and download/update the game.
4. Pull from Steam Cloud before launching with existing saves.
5. Launch vanilla first.
6. If vanilla works, test selected mods.
7. If using SavesMerger, test both enabled and disabled behavior.
8. File a focused issue using the matching template.
