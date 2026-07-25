# Testing Needed

See [Unofficial project notice](unofficial-project-notice.md). StS2 Launcher is an unofficial community launcher, is not affiliated with or endorsed by Mega Crit Games, and bundles no Slay the Spire 2 game files or assets. Steam ownership is required.

This project needs focused Android tester reports more than broad "works for me" comments. Good reports help confirm device compatibility, Steam Cloud safety, public/beta branch behavior, and mod loading without exposing Steam account data.

Current APK for tester reports: `v0.2.416-startup-recovery-ime` / `StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk`.

## Current Priority

1. **Startup and loading speed**
   - Time from tapping the app icon to launcher visible.
   - Time from pressing Start Game to game main menu.
   - Any black screen, native fallback screen, shader compile stall, or app crash.
   - Issue #36 reporter-class Xiaomi validation is specifically needed. `v0.2.416` validates patched runtime-pack assemblies against runtime-pack evidence, keeps `GodotApp.onCreate()` lifecycle-correct, and clears pending launch state before native recovery. Report whether Start Game reaches `NMainMenu`; if fallback appears, copy its full diagnostics before selecting **Restart launcher**.
   - For Start Game failures, attach `last_launch_attempt.txt` when available. Current source builds record a per-press attempt ID, selected branch, ready/blocked state, runtime slot ID, selected PCK path/hash, source and active `sts2.dll` paths/hashes, runtime pack path/status, runtime cache marker path/presence, runtime patch-validation marker path/presence, patch compatibility marker path/status, whether the prepared readiness result was used, whether readiness came from a fresh check or an in-memory cache hit, and elapsed timing for total launch attempt, selected-version readiness, and mod readiness. Modded starts also record play mode, enabled mod count, selected mods, selector cache status, and whether modded-save Cloud Push was locked. Cache hits are only valid while the selected PCK, branch marker, release info, source assembly, runtime pack manifest, runtime cache marker, patch-validation marker, mod selection, Workshop manifest, and cheap metadata digests for all staged/manual `.json`, `.pck`, and `.dll` mod files are unchanged.
   - For shader reports, attach `last_shader_warmup_status.txt` when available. `completed` means the full precompile pass finished; `completed-partial` means the launcher intentionally continued startup with degraded shader-cache coverage after a budget or compatibility cap. Current source builds also include render-plan evidence, batch size, target material count, scanner counters such as scenes scanned, unique materials, budget-stop state, and scanner failure counts. Current ARM64 evidence on `SM-F966B` completes v7 bounded public warmup with `Render plan: android-bounded-large-shader-set`, `Render target materials: 128/1592`, and no app crash signatures; weaker-device reports are still needed.
   - Pixel 10 / PowerVR reports are a specific renderer/engine investigation, not a generic low-performance category. The reporter's `v0.2.399` test reaches the game in every mode, but touch works only in OpenGL. `v0.2.400` forces OpenGL on detected PowerVR devices. Record requested/effective mode, `graphics_device.txt`, whether touch works, time until the first menu becomes responsive, and whether later menus remain slow. Attach `last_renderer_attempt.txt` and `last_process_exit_info.txt` when present.

2. **Steam Cloud workflow clarity**
   - Issue #35 reporter-class Odin validation is specifically needed.
   - Confirm that Pull always shows its current phase and transfer counts, can be cancelled, and ends with a precise success/partial/failure summary.
   - Confirm that **Review Upload** lists every blocking reason and required action, and that Upload becomes available immediately after all safeguards are satisfied.
   - Do not run a real Steam Cloud Push merely to test the interface. Push requires separate explicit authorisation and controlled backups.

3. **Public/default game launch**
   - Fresh install or update install.
   - Steam login, game download/update, Pull from Cloud, launch.
   - Whether the expected save/profile appears in-game.

4. **Public-beta or core-release branch switching**
   - Selected branch shown in the launcher.
   - Whether the branch downloads or is blocked clearly.
   - Whether the game launches without silently falling back to public/default.
   - Any mixed public/beta assets, missing UI art, or hard-lock route.

5. **Workshop/mod support**
   - Whether subscribed mods are discovered.
   - Whether the launcher stages or marks the mod as needing manual import.
   - Which mods are selected.
   - Whether vanilla launch disables mods correctly.
   - Whether modded launch reaches the game main menu.

6. **Native vanilla/modded save handling**
   - The game now owns separate vanilla and modded save namespaces; SavesMerger/UnifiedSavePath entries are deprecated by the launcher.
   - After Manual Pull, report whether the expected vanilla and modded profiles are visible and loadable in-game.
   - Do not treat "mod selected" or "main menu reached" as save compatibility proof unless the expected profile and run data are actually usable.

7. **Redesigned launcher on Samsung/One UI and unusual display sizes**
   - Validate all Home/Saves/Versions/Mods/Help destinations, portrait/landscape rotation, keyboard, password-manager suggestions, clipping, and button reachability.
   - Include display size/font size settings when reporting UI problems.

8. **Controller and Android handheld input**
   - Device, controller, and connection mode.
   - Whether launcher navigation works.
   - Whether in-game card/map/menu actions work.
   - Whether behavior changes between vanilla and modded launch.

## Known Working Evidence So Far

| Device class | Android ABI | Evidence | Status |
| --- | --- | --- | --- |
| Physical ARM64 Samsung test device | `arm64-v8a` | Exact non-debuggable `v0.2.416` APK; four-second cold boot; launcher and IME state; public runtime-pack promotion; real `NMainMenu`; 1s/3s/10s/30s/60s heartbeats; installed-artifact hash match; earlier renderer/mod/cloud evidence | Working in connected validation; Xiaomi/Odin reporter confirmation, broader PowerVR devices, modded `v0.2.416`, and broad viewport coverage pending |
| Android x86_64 emulator | `x86_64` | Install/routing/native fallback diagnostics only | Not a game-launch proof target |

## Current Practical Device Floor

- ARM64 Android hardware is the proof target.
- Renderer/driver compatibility must be validated per device. `v0.2.399` reaches the game on the reporter's PowerVR device, but Vulkan touch fails there. Current `v0.2.416` retains the PowerVR-to-OpenGL policy.
- Android x86_64 emulator results do not prove game support.
- There is not enough cross-device evidence yet to publish a precise RAM/GPU minimum. Current high-end ARM64 proof is `SM-F966B`, Android 16/API 36, 8 processors, about 11.6GB total memory, full v6 warmup in 40149ms.
- Do not classify a device as below the support floor from `completed-partial` alone. Attach focused logcat and post-startup markers so renderer bugs, lifecycle exits, and genuine memory pressure can be separated.

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
NMainMenu
PostStartupTrace
PostStartupHeartbeat
Native lifecycle event
Fatal signal
SIGSEGV
SIGABRT
has died
lmkd
PowerVR
OpenGL
Vulkan
```

Prefer a small focused excerpt around the failure over a full raw log.

## Best Current Tester Flow

1. Install the latest APK from GitHub Releases.
2. Open the launcher and record whether it reaches the main launcher screen.
3. Log in to Steam and download/update the game.
4. Pull from Steam Cloud before launching with existing saves.
5. Launch vanilla first.
6. If vanilla works, test selected mods.
7. For modded saves, verify the expected native modded profile rather than enabling deprecated SavesMerger/UnifiedSavePath entries.
8. If using a controller, test one in-game combat/menu action rather than only launcher navigation.
9. File a focused issue using the matching template.
