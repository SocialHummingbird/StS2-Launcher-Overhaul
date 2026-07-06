# Issue Reporting Guide

Good StS2 Mobile reports include enough evidence to separate launcher bugs, Steam account/branch availability, Android runtime routing, Steam Cloud safety, and mod/save compatibility. Reports that only say "latest APK" or "mods do not work" usually cannot be acted on.

Current recurring report themes are launcher UI scaling/scroll reachability, shader compile crashes or stalls, controller input on Android handhelds, public-beta/core branch freshness, and SavesMerger save usability. These are useful reports when they include exact APK, device, branch, mod, screenshot, and focused log details.

For Start Game failures, include `last_launch_attempt.txt` when available. Current source builds write a per-press attempt ID, selected branch, ready/blocked state, runtime slot ID, selected PCK path/hash, source and active `sts2.dll` paths/hashes, runtime pack path/status, runtime cache marker path/presence, runtime patch-validation marker path/presence, patch compatibility marker path/status, whether the launch used the prepared readiness result rather than repeating primary-path validation, whether that readiness came from a fresh check or a safe in-memory cache hit, and elapsed timing for total launch attempt, selected-version readiness, and mod readiness. Modded starts also include play mode, enabled mod count, selected mods, selector cache status, whether modded-save Cloud Push was locked, and cache evidence that invalidates when selector metadata, Workshop metadata, or staged/manual mod file metadata changes.

When the launcher detects that the previous game startup failed, it now reads `last_launch_attempt.txt` and adds a short targeted recovery hint to the launcher log. Include that text in issue reports when available.

## Choose The Right Template

| Problem | Template |
| --- | --- |
| Immediate crash, black screen, slow startup, NativeFallback, or Start Game hang | Crash, hang, or startup problem |
| Public/default, public-beta, core-release, private/password branch, wrong branch, missing/mixed assets, PCK/runtime mismatch | Game download or Steam branch issue |
| Steam login, Steam Guard, ownership, Pull from Cloud, Push to Cloud, Push blocked by safety gates | Steam login or Cloud save problem |
| Workshop sync, manual import, mod selection, modded launch, Vanilla and Modded Saves Merger, save compatibility | Mods, Workshop, or save-merger test |
| New phone/tablet result, Samsung/One UI behavior, display scaling, keyboard/password-manager behavior | Device compatibility report |
| Anything else reproducible | General bug report |

The older Steam version-selection report template remains available for deep branch validation runs. Use the branch issue form for normal tester reports.

## Minimum Evidence For Every Report

- Exact APK release tag and APK filename. Do not write only "latest".
- Device model, Android version, vendor skin/version, and device ABI.
- Package name and app version/versionCode when known.
- Clean install or update install.
- Selected game branch: public/default, public-beta, core-release, or other.
- Whether mods were enabled.
- Whether Pull from Cloud ran.
- Whether Push to Cloud ran. If unsure, say so.
- Focused repro steps from opening the app through the failure.
- Screenshot for UI or visual asset problems.
- Focused logcat or diagnostics for crashes, hangs, failed startup, failed downloads, Steam login failures, cloud failures, and mod load failures.

For launcher layout reports, include whether the Start Game/Play button is reachable, whether scrolling works, orientation, display size/font scale, and a screenshot.

For controller reports, include controller/device model, connection mode, whether launcher navigation works, whether in-game actions work, and whether the result differs in vanilla versus modded launch.

For shader compile reports, include how long the compile screen stayed visible, whether Android showed an app-not-responding dialog, device thermal/performance mode if known, and the contents of `last_shader_warmup_status.txt` when available. Current source builds include scanner counters in that marker, including scenes scanned, unique materials, whether the scan stopped by budget, and scanner failure counts. Current local ARM64 proof on `SM-F966B` completes v6 warmup in 40149ms; a `completed-partial` marker means the launcher hit its warmup time budget and continued with degraded shader-cache coverage. That is useful compatibility evidence, not full device signoff.

## Do Not Share Publicly

- Steam password.
- Steam Guard codes.
- Steam refresh tokens.
- Full unsanitized launcher diagnostics.
- Full unsanitized logcat.
- Private account names, Steam IDs, local Windows/Android user paths, or device identifiers unless intentionally disclosed.
- Save files or save contents unless you intentionally want them public.

Before attaching logs, search for account names, tokens, `refresh`, `guard`, `password`, email addresses, local user folders, and save contents.

## Focused Logcat Capture

Use this for crashes, hangs, NativeFallback, failed startup, failed downloads, login failures, cloud failures, or mod-loading failures:

```powershell
adb logcat -c
adb logcat -v time > sts2-mobile-logcat.txt
```

Reproduce the issue once, stop logcat, then attach a focused excerpt around the failure. Prefer 200-300 lines around the primary exception plus a short lead-in showing what action triggered it.

Useful search terms:

```text
AndroidRuntime
FATAL EXCEPTION
Godot
STS2Mobile
PatchHelper
NativeFallback
Steam
SteamKit
Cloud
Pull
Push
Workshop
Mods
SavesMerger
Runtime pack
PCK
```

## Branch And Runtime Evidence

For public-beta, core-release, other non-public branches, NativeFallback, missing/mixed assets, or wrong-branch behavior, include these when available:

```text
Selected game branch:
Selected game version display name:
Selected game slot kind:
Selected game slot directory:
Selected game PCK path:
Selected game PCK SHA-256:
Runtime pack path:
Runtime pack SHA-256:
Active sts2.dll path:
Active sts2.dll SHA-256:
Runtime cache marker:
Patch validation marker:
Branch marker path/value:
Visible Steam branches:
Windows depot manifests for selected branch:
Public-vs-selected depot integrity summary:
```

Fallback or gating is evidence, not a successful launch. A non-public branch report is only launch-positive if the selected branch, selected PCK, runtime pack, active managed runtime, runtime cache marker, and patch validation marker all match the selected branch.

## Steam Cloud Safety Evidence

Steam Cloud Push can overwrite remote save state. Any cloud issue must say whether Pull or Push ran.

For Push reports, include:

```text
Manual Pull completed before Push:
Current important Android local save evidence count:
Current important Android local save evidence present:
Baseline manual Push prerequisites satisfied:
Branch-switch manual Push prerequisites satisfied:
Latest manual Push evidence outcome:
Latest manual Push blocked reason:
Backup storage permission available:
```

If you were testing branch switching or mods, do not treat a blocked Push as a bug unless the report explains why the launcher had enough safe evidence to upload.

## Workshop And Save-Merger Evidence

For mod reports, list each selected mod:

```text
Mod name:
Workshop item ID:
Source, Workshop sync or manual import:
Files present, for example .dll/.json/.pck:
Enabled or disabled:
Launcher showed unsupported/attention warning:
```

For Vanilla and Modded Saves Merger, include:

```text
Existing vanilla save present before test:
Existing modded save present before test:
Pull from Steam Cloud run before test:
Push to Steam Cloud run during test:
Save/profile became visible in-game:
Save/profile loaded successfully:
Disabling the mod returned expected vanilla/modded save behavior:
```

The current useful result is not just "the game reached main menu." The important evidence is whether existing saves became visible and loadable with the merger enabled, and whether disabling mods returns to expected behavior.

## Current Support Boundaries

- ARM64 Android hardware is the real proof target for Steam login, download, game launch, branch switching, cloud saves, and mods.
- A working Vulkan path is required for the bundled Godot runtime. The project does not yet have enough evidence to publish a precise RAM/GPU minimum.
- If shader warmup records `completed-partial` and the device still cannot reach the game, treat that as below the current practical support floor unless focused logs show a specific launcher/runtime defect.
- Android x86_64 emulator results are diagnostic only unless a maintainer asks for a forced-Godot investigation.
- Steam beta password entry is not currently a release-ready path.
- Workshop/mod support is functional but still hardening. Some Workshop items exposed only as legacy UGC handles may still need manual import.
- SavesMerger loading/scanning is not the same as full save-merge signoff. The useful report is whether existing saves become visible and loadable.
- Push to Cloud is intentionally guarded and may stay blocked when branch-switch, modded-save, local-save, Pull, or backup evidence is incomplete.
