# Testing Needed

See [Unofficial project notice](unofficial-project-notice.md). StS2 Mobile / StS2 Launcher Overhaul is an unofficial community launcher, is not affiliated with or endorsed by Mega Crit Games, and bundles no Slay the Spire 2 game files or assets. Steam ownership is required.

This project needs focused Android tester reports more than broad "works for me" comments. Good reports help confirm device compatibility, Steam Cloud safety, public/beta branch behavior, and mod loading without exposing Steam account data.

Current APK for tester reports: `v0.2.385-shader-warmup-compat` / `StS2Launcher-v0.2.385-shader-warmup-compat-local-arm64-v8a.apk`.

## Current Priority

1. **Startup and loading speed**
   - Time from tapping the app icon to launcher visible.
   - Time from pressing Start Game to game main menu.
   - Any black screen, native fallback screen, shader compile stall, or app crash.
   - For Start Game failures, attach `last_launch_attempt.txt` when available. Current source builds record a per-press attempt ID, selected branch, ready/blocked state, runtime slot ID, selected PCK path/hash, source and active `sts2.dll` paths/hashes, runtime pack path/status, runtime cache marker path/presence, runtime patch-validation marker path/presence, patch compatibility marker path/status, whether the prepared readiness result was used, whether readiness came from a fresh check or an in-memory cache hit, and elapsed timing for total launch attempt, selected-version readiness, and mod readiness. Modded starts also record play mode, enabled mod count, selected mods, selector cache status, and whether modded-save Cloud Push was locked. Cache hits are only valid while the selected PCK, branch marker, release info, source assembly, runtime pack manifest, runtime cache marker, patch-validation marker, mod selection, Workshop manifest, and cheap metadata digests for all staged/manual `.json`, `.pck`, and `.dll` mod files are unchanged.
   - For shader reports, attach `last_shader_warmup_status.txt` when available. `completed` means the full precompile pass finished; `completed-partial` means the launcher intentionally continued startup with degraded shader-cache coverage after a budget or compatibility cap. Current source builds also include render-plan evidence, batch size, target material count, scanner counters such as scenes scanned, unique materials, budget-stop state, and scanner failure counts. Current ARM64 evidence on `SM-F966B` completes v7 bounded public warmup with `Render plan: android-bounded-large-shader-set`, `Render target materials: 128/1592`, and no app crash signatures; weaker-device reports are still needed.

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
   - Do not treat "mod selected" or "main menu reached" as full save-merger success unless the save/profile is visible and loadable.

6. **Samsung/One UI and unusual display sizes**
   - Launcher layout, keyboard, password manager suggestions, and button reachability.
   - Include display size/font size settings when reporting UI problems.

7. **Controller and Android handheld input**
   - Device, controller, and connection mode.
   - Whether launcher navigation works.
   - Whether in-game card/map/menu actions work.
   - Whether behavior changes between vanilla and modded launch.

## Known Working Evidence So Far

| Device class | Android ABI | Evidence | Status |
| --- | --- | --- | --- |
| Physical ARM64 Samsung test device | `arm64-v8a` | Public/default, public-beta runtime matching, Steam Cloud Pull, guarded Push behavior, mod selector, manually imported SavesMerger launch | Working in local validation |
| Android x86_64 emulator | `x86_64` | Install/routing/native fallback diagnostics only | Not a game-launch proof target |

## Current Practical Device Floor

- ARM64 Android hardware is the proof target.
- A working Vulkan path is required for the bundled Godot runtime.
- Android x86_64 emulator results do not prove game support.
- There is not enough cross-device evidence yet to publish a precise RAM/GPU minimum. Current high-end ARM64 proof is `SM-F966B`, Android 16/API 36, 8 processors, about 11.6GB total memory, full v6 warmup in 40149ms.
- If a device cannot reach the game after the bounded shader warmup path records `completed-partial`, treat it as a device-floor candidate unless focused logs show a specific launcher/runtime defect. If it fails before recording `completed-partial`, attach focused logcat because that is still actionable.

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
- Whether controller input was used.
- Whether shader compilation happened before the failure.
- Shader warmup marker status, especially `watchdog-warning`, `completed`, `completed-partial`, elapsed milliseconds, rendered material count, scanner failure counts, and whether scanning stopped by budget if present.
- Screenshot if the problem is visual.
- Focused logcat if the app crashes or hangs.
- For branch reports: selected branch, PCK path/hash, runtime pack path/hash, active `sts2.dll` hash, runtime cache marker, and patch validation marker when available.
- For launch/startup reports: `last_launch_attempt.txt`, `last_startup_context.txt`, `last_startup_timeline.txt`, and `last_shader_warmup_status.txt` when present.
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
8. If using a controller, test one in-game combat/menu action rather than only launcher navigation.
9. File a focused issue using the matching template.
