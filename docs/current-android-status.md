# Current Android Status

_Last updated: 2026-08-06_

See [Unofficial project notice](unofficial-project-notice.md). StS2 Launcher is an unofficial community launcher, is not affiliated with or endorsed by Mega Crit Games, and bundles no Slay the Spire 2 game files or assets. Steam ownership is required.

Current device evidence ledgers:

- [android-release-validation.md](android-release-validation.md) — current Stage 5 exact-candidate gate (0/10)
- [android-device-validation-20260608.md](android-device-validation-20260608.md)
- [android-cloud-save-validation-20260609.md](android-cloud-save-validation-20260609.md)
- [launcher-loading-screen-staging.md](launcher-loading-screen-staging.md)
- [steam-version-selection-validation.md](steam-version-selection-validation.md)
- [steam-version-selection-runbook.md](steam-version-selection-runbook.md)
- [steam-version-selection-release-readiness.md](steam-version-selection-release-readiness.md)
- [android-workshop-mods.md](android-workshop-mods.md)
- [android-powervr-input-compatibility-20260716.md](android-powervr-input-compatibility-20260716.md)

## Headline

The app works on the validated ARM64 Android path, but it remains unofficial prerelease tester software rather than broad device signoff. `v0.2.416-startup-recovery-ime` is the current GitHub release. It corrects authentic patched runtime-pack validation, prevents native fallback restart loops, suppresses unintended launcher keyboard requests, and retains the observable/cancellable cloud workflow from `v0.2.412`. The exact final APK reaches real public-branch `NMainMenu` and remains there through the 60-second heartbeat on the connected Samsung device. Xiaomi issue #36 and Odin cloud issue #35 still require reporter confirmation; broader Workshop/mod, branch, controller, graphics-driver, and confirmed Steam Cloud Push coverage also remain open.

That published-build evidence does not validate the current save-safety changes. Local-only game saving, launcher-owned automatic synchronization, and Restore/Undo remain unreleased and at 0/10 in the Stage 5 exact-candidate device matrix.

## Evidence Boundary

The current evidence must not be combined into a broader claim than each test target supports:

| Evidence source | Validated | Not validated |
| --- | --- | --- |
| Automated/local checks | Managed Release compilation, Java regressions, Gradle assembly, APK structure/ABI/crypto, branch/mod fixtures, fake-loader startup failure paths, temporary save-tree recovery, and fake-store cloud safety | Android compositor output, OEM lifecycle behavior, real Steam auth/network/cloud, or gameplay |
| API 36 x86_64 emulator, current unreleased source | Cold and cached native routing, designed splash/fallback frames, fallback controls, exact-once recovery, forced bootstrap failure and retry, active-cache preservation, rotation, Home/Recents resume, native-path IME suppression, and absence of scoped fatal/ANR/lifecycle errors | Managed Godot/.NET launcher rendering, the full managed readiness bridge, Steam workflows, ARM64 runtime, `NMainMenu`, or gameplay |
| Exact published `v0.2.416`, Samsung ARM64 | Installed-artifact hash equality, four-second cold transition, launcher rendering with IME hidden, patched public runtime-pack promotion, real `NMainMenu`, and 1s/3s/10s/30s/60s heartbeats | Xiaomi/Odin reporter devices, broad manufacturer/GPU support, every branch/mod/display/lifecycle path, or a real Steam Cloud Push |
| Outstanding ARM64 work | Exact current-source candidate, full managed transition and launcher controls, current native routing corrections, public/public-beta and vanilla/modded launch, lock/rotation/Home/resume, reporter-device retests, and cloud Pull/upload-eligibility UX | These paths remain unvalidated until an exact ARM64 APK and evidence bundle are recorded |

The Stage 5 emulator artifact was local evidence APK `0.2.417-stage5-final5-evidence-local` (`241714`, x86_64, SHA-256 `ce7f37ae898665ebba3798b923c9f98741c8b6092b14ae6b41851832dd7bc891`). It is not a GitHub release and does not supersede `v0.2.416`.

July 25 Android startup/recovery/IME status:

- Exact non-debuggable local-test APK `0.2.416-startup-recovery-ime-local` / version code `416001` / SHA-256 `fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b` was installed as an in-place update on Samsung `SM-F966B` / Android 16 / ARM64. The pulled installed `base.apk` hash matches the release artifact.
- Public Start Game selected the usable public runtime pack, validated patched `sts2.dll` size `9305600` and SHA-256 `5c3c2bead75b05883073e7ed99420c1241ecd0f99c0407bda8677a9ec7caca27` against runtime-pack evidence, continued validating ordinary dependencies against the selected game install, promoted the staged cache, reached real `NMainMenu`, and logged 1s, 3s, 10s, 30s, and 60s heartbeats.
- A controlled missing-runtime-validation-report probe on cumulative debuggable build `0.2.415-launcher-ime-evidence` retried once and routed to `NativeFallbackActivity` without starting `GodotApp`, `SuperNotCalledException`, cache replacement, fatal exception, or ANR. The report was restored with its original hash.
- The actual native **Restart launcher** button cleared the carried game-launch payload, blocked duplicate routing, started `LauncherActivity` exactly once with transition skip, and reached the ready launcher. The launcher remained at `mInputShown=false` after cold start and boot cleanup.
- Pure Android bootstrapper/lifecycle/routing/recovery/IME/boot tests, managed Release compilation, non-mutating cloud production-path tests, Gradle release assembly, ARM64 structure/crypto checks, and update compatibility from `v0.2.412` pass. No automated lock/sleep/power-key test and no Steam Cloud Push was run.

July 16 PowerVR input compatibility status:

- The issue #34 reporter tested exact `v0.2.399` on Pixel 10 Pro / Android 17 / PowerVR D-Series DXT-48-1536. Auto, Vulkan, OpenGL, and Safe Start all reach the game, so the earlier startup/process-teardown failure is no longer reproduced.
- Auto, Vulkan, and Safe Start show the game but do not accept touch. OpenGL accepts touch. Its menus are initially slow, while gameplay and the menu after returning from gameplay are normal. This is renderer/driver behavior, not evidence of an old or underpowered device and not a launcher handoff, shader-warmup, or memory-pressure crash.
- `v0.2.400` records the live Godot adapter name, vendor, driver, and method in `graphics_device.txt`. If the adapter or vendor is PowerVR/ImgTec/Imagination, the managed launcher selects OpenGL and the native Android restart boundary forces `opengl3` plus `gl_compatibility` for every requested mode, including Vulkan and Safe Start.
- Exact release APK `0.2.400-powervr-touch-compat-local` / version code `400001` / SHA-256 `623830caad7a684e3358fbb22564210a1236588e03e7161dfcf30cc5aa76cdc3` passed APK structure, ABI, crypto-patch, Java policy, static audit, and Release C# build checks. Its pulled installed `base.apk` hash matches the release artifact. On Samsung `SM-F966B` / Android 16 / Adreno 830, Auto remained Vulkan and accepted touch, Safe Start remained unforced Vulkan, explicit Vulkan remained Vulkan, and explicit OpenGL reached `NMainMenu` and accepted touch.
- A synthetic reporter-class PowerVR marker on the same installed APK changed a saved Vulkan request to effective OpenGL, reached `NMainMenu`, wrote the three-second post-startup probe, and accepted the touch that opened character selection. The issue #34 reporter later confirmed the real PowerVR Auto route loads the game with active touch; broader PowerVR devices remain unproven. Steam Cloud Push was not run.

July 15 Pixel / PowerVR cause analysis:

- Reporter `log7.txt` is from exact release `v0.2.397-atlas-memory-compat`, version code `397001`, on Pixel 10 Pro / Android 17 / PowerVR D-Series DXT-48-1536. It uses the public branch with no mods, reaches real `NMainMenu`, passes the main-menu guard, and then disappears before the first one-second post-startup trace probe or heartbeat.
- The attachment starts again in a new launcher process, so it does not contain the dying game process's terminal signal or Android process-exit reason. Its lack of `FATAL EXCEPTION`, `SIGSEGV`, ANR, LMKD, or `NativeFallback` evidence cannot prove that none occurred in the previous process.
- The exact device/GPU/renderer combination matches [Godot issue #113911](https://github.com/godotengine/godot/issues/113911): Godot 4.5, Pixel 10, PowerVR D-Series DXT-48-1536, OpenGL Compatibility, and a GLES3 particle/transform-feedback crash path. [Godot PR #111329](https://github.com/godotengine/godot/pull/111329) disables transform-feedback shader caching on every PowerVR GPU and was included in Godot 4.5.2.
- The then-published `v0.2.398` APK used custom Godot `4.5.1.stable.mono.custom_build.f62fdbde1`. Its engine only disabled that cache for `PowerVR Rogue GE8320`, so the Pixel 10 GPU was not covered.
- `v0.2.398` also forced OpenGL Compatibility unless a previous startup marker equalled `game startup completed`, while managed recovery wrote `post-startup observation`. Its Safe Start advertised the default renderer while the Android boundary still forced OpenGL. The alternative renderer path therefore was not actually tested in `v0.2.398` or the reporter's `v0.2.397`.
- `v0.2.399` backports the Godot 4.5.2 all-PowerVR workaround onto the custom 4.5.1 engine. Release builds compile that patched engine instead of extracting the old native library from `v0.2.88`, and the engine emits `PowerVR renderer detected; transform feedback shader cache disabled` when the workaround activates.
- `v0.2.399` also provides explicit Auto, Vulkan, and OpenGL modes. Auto passes no renderer override, Vulkan selects the mobile renderer, OpenGL selects Compatibility, and Safe Start always uses unforced Auto while keeping shader warmup and Steam Cloud disabled.
- The Android boundary records each effective renderer plan in `last_renderer_attempt.txt`. On Android 11 and newer, the next launcher start records historical `ApplicationExitInfo` plus bounded trace data in `last_process_exit_info.txt` when Android supplies it. These files are included in launcher diagnostics.
- Exact release APK `0.2.399-powervr-mod-runtime-hashfix-activation-evidence-local` / version code `399004` / SHA-256 `d39825fe2f79ca86af6ff4c83bbcd1eaeeb0fd3eeeedde509cdb3aa6a17420fe` passes structural, crypto, package, ABI, signing, update-compatibility, and GitHub release-hygiene checks. On Samsung `SM-F966B` / Android 16 / Adreno 830, Auto/Vulkan and explicit OpenGL both reached real `NMainMenu`, produced post-startup probes and heartbeats through 120 seconds, and avoided focused fatal/native/ANR/target-process-kill signatures. This is an engine/launcher integration change, not a core game redesign and not evidence that the Pixel 10 is old or underpowered. Reporter testing later confirmed the crash correction and exposed the separate PowerVR touch defect documented in the July 16 section.

July 11 atlas compatibility status:

- Reporter `log6.txt` showed that Android deferred startup eagerly called `AtlasManager.LoadAllAtlases()` immediately before the PowerVR process teardown. Unsupported desktop BPTC/DXT atlas pages were converted to RGBA8; the three card pages alone expand to about 166 MiB, with relic, power, and other sheets adding more startup pressure.
- `v0.2.397-atlas-memory-compat` skips eager `LoadAllAtlases()` on Android. Atlas-backed card, relic, power, and potion resources use individual texture files while their source atlas remains unloaded; entries without an individual fallback can still load the smaller source atlas lazily. Non-Android behavior is unchanged.
- Exact reporter-runtime validation passed against public `sts2.dll` SHA-256 `a1f9e653f1e28e4076558fee1e60d218619cb7e057b887c6417f62c62c6d7a52` and PCK size `1901378340`. All 875 card sprites have non-BPTC/S3TC individual imports, and all three Harmony prefixes attach to the reporter runtime.
- Connected ARM64 validation used local evidence APK `0.2.397-atlas-memory-compat-local` on Samsung `SM-F966B`. Public Start Game reached real `NMainMenu`, passed 1s/3s/10s/30s probes and 60s/300s heartbeats, rendered Card Library and Relic Collection, logged individual relic/relic-outline/power fallbacks, and kept one process alive without focused fatal, native signal, ANR, LMKD, lifecycle teardown, or Godot static-string cleanup markers. Steam Cloud Push was not run.
- The reporter subsequently tested `v0.2.397`; `log7.txt` still reaches `NMainMenu` and exits before the first post-startup probe. The atlas change remains useful for Android resource pressure, but it did not resolve the Pixel 10 PowerVR failure.

July 8 Pixel / PowerVR compatibility status:

- GitHub issue #34 now has reporter evidence from `v0.2.396-post-startup-anchor` on Pixel 10 Pro / Android 17 / PowerVR D-Series DXT-48-1536 / OpenGL ES 3.2 Compatibility. The log proves the game reaches the real `NMainMenu`, records `NGame.GameStartup completed`, passes the main-menu guard, hides launcher recovery UI, and logs the Android post-startup task anchor as active. About 200ms later Godot emits static-string cleanup errors and the app returns to launcher/bootstrap with a new process. The report still shows no `NativeFallback`, Java `FATAL EXCEPTION`, AndroidRuntime fatal, SIGSEGV, SIGABRT, signal 11/6, ANR, LMKD kill, or obvious Java exception.
- This shifts the remaining issue #34 failure away from launcher handoff, shader warmup, recovery UI, and dev-console handling. The likely problem area is broader Android graphics/resource compatibility on the PowerVR/OpenGL ES fallback path, where Godot repeatedly decompresses or converts unsupported PC-oriented texture formats such as `RGBFloat`, `BPTC_RGBA`, and `DXT1`/`DXT5` during main-menu/deferred asset loading.
- At that stage, the next direction was reducing startup asset pressure and avoiding eager atlas loading. `v0.2.397` implemented that work but the reporter still failed, so the July 15 engine/renderer analysis above supersedes this as the primary cause. The existing `SM-F966B` ARM64 validation remains useful, but it does not prove compatibility on Pixel / PowerVR / Android 17.

July 5 runtime/update status:

- July 5 known-issues validation installed local evidence build `0.2.358-known-issues-fmod-marker-fix` (`versionCode=358001`) on ARM64 device `SM-F966B`. Public-beta launches to the main menu with the selected side-by-side public-beta PCK/runtime pack; this is a playable branch-routing pass, not a `NativeFallbackActivity` fallback. Patch marker `.android_pck_patch_v35` records matched source/current PCK hash `109f61f7e13a7f329c9fe10aab81dec55e4c415e9863ef5468b76dadac7c7aee`, all five desktop FMOD bank entries in the selected public-beta PCK (`Master.strings.bank`, `Master.bank`, `sfx.bank`, `temp_sfx.bank`, `ambience.bank`), and diagnostic extraction copies under `/sdcard/sts2b`. Runtime-pack evidence records public-beta `v0.108.0`, PCK hash `109f61f7e13a7f329c9fe10aab81dec55e4c415e9863ef5468b76dadac7c7aee`, runtime pack `public-beta-109f61f7e13a-51a671bfeb93-startup-orchestrator-v1`, source/runtime/active `sts2.dll` hash `51a671bfeb937271af3e643d017396b13432098ed2b9debceb110c74939bbba1`, and patch validation `passed`.
- BGM/ambience native initialization is now fixed in local evidence build `0.2.364-fmod-audio-devices-test`. The Android FMOD Java/JNI bridge initializes `libfmod.so`, reports successful DSP buffer setup and global 3D settings, and loads the desktop FMOD banks from `res://banks/desktop/...` without the previous invalid-handle/JNI/bank-load failures. Human audible-output confirmation is still useful, but the previous log-level native FMOD blocker is closed.
- Silent Steam sign-in failure reporting is fixed in local evidence build `0.2.366-auth-failure-reporting-test`. An invalid one-shot local credential handoff on ARM64 was consumed, connected to Steam, failed with `InvalidPassword`, wrote `last_steam_auth_failure.txt` with category `credentials`, selected branch `public-beta`, user-facing recovery text, and technical SteamKit detail, and left `last_manual_cloud_push.txt` missing.
- Shader compile crash/stall triage now has v6 device proof in local evidence build `0.2.376-shader-warmup-budget-evidence-local`: normal public-beta launch automation reached the main menu on `SM-F966B`, `last_shader_warmup_status.txt` recorded `Status: completed`, `Warmup version: 6`, `Warmup time budget seconds: 90`, and `Rendered 1713 shader warmup materials` in 40149ms. No `NativeFallbackActivity`, fatal exception, ANR, or manual Steam Cloud Push signature was present in the focused evidence.
- Current local shader hardening keeps the 90-second precompile budget and classifies `completed-partial` when startup continues after rendering only part of the shader set. Source builds after `v0.2.377` also roll shader scanner failures into status-marker counters and rate-limit repetitive scanner failure logs. This avoids repeatedly forcing weak devices through an unbounded first-run precompile, but partial warmup is degraded compatibility evidence, not a full device pass. We still need weaker-device reports before publishing a precise RAM/GPU minimum.
- July 7 shader compatibility validation installed local evidence build `0.2.385-shader-warmup-compat-local` (`versionCode=385001`) on `SM-F966B` over existing app data. Normal public Start Game automation reached `Phase: in-process signalled`, used public PCK hash `1ed2b5d8878c22f35770628e171101b95fdd879c2f54747acaca9206e90b127f`, matched source/active `sts2.dll` hash `a1f9e653f1e28e4076558fee1e60d218619cb7e057b887c6417f62c62c6d7a52`, wrote shader warmup marker `Warmup version: 7`, `Status: completed-partial`, `Render plan: android-bounded-large-shader-set`, `Render target materials: 128/1592`, `Render batch size: 1`, and `Classification: android-bounded-large-shader-set; partial compatibility warmup completed; startup continued`. Focused logs still show Godot texture conversion warnings for unsupported compressed formats, but no `NativeFallbackActivity`, `FATAL EXCEPTION`, `AndroidRuntime`, app-PID signal 6/11, or app-PID exit signature was present. Evidence is under `artifacts/android/shader-warmup-compat-0.2.385-20260707-rerun`.
- Current source builds after `v0.2.377` also route Start Game through a prepared launch-readiness result. The launcher writes `last_launch_attempt.txt` with a per-press attempt ID, selected branch, ready/blocked state, runtime slot ID, selected PCK path/hash, source and active `sts2.dll` paths/hashes, runtime pack path/status, runtime cache marker path/presence, runtime patch-validation marker path/presence, patch compatibility marker path/status, whether prepared readiness was used, whether the readiness result came from a fresh check or a safe in-memory cache hit, and elapsed timing for total launch attempt, selected-version readiness, and mod readiness. Modded starts also capture launcher-side mod readiness before handoff, including play mode, enabled count, selected mods, and selector cache status; vanilla starts skip mod-source scans on the primary launch path. The saved-session, offline, and branch-switch ready-status paths now use a lightweight downloaded-state check and defer full runtime pairing/patch validation to Start Game, where it can still block launch and emit evidence. Post-download runtime validation still runs after files change so runtime-pack evidence is created promptly, but validation exceptions now become a visible retryable not-ready state instead of escaping the main-thread download-complete event. Ready and download-required copy uses the branch carried by the readiness snapshot, and branch-switch checks pass the just-selected branch directly instead of re-reading preferences during the UI refresh. Startup, branch-switch, and refresh-complete UI setup apply saved action preferences, selected branch, and branch dropdown options together where possible, avoiding duplicate dropdown rebuilds and duplicate marker reads on those paths. Branch-switch UI refresh now reuses the just-saved branch when reading aggregate action preferences instead of reading the branch preference back immediately. Branch-switch confirmation and refresh-complete status reuse the same visible branch-catalog snapshot for dropdown options and selected-branch blocker/status text. Duplicate Start Game taps while launch is already in progress now log the selected branch from the active launch-attempt context instead of rereading selected-branch preferences. Previous-startup warnings now read that marker once per launcher session to show targeted recovery hints for blocked files, runtime-pack/pairing problems, restart handoff stalls, selected launch paths, runtime/patch marker evidence, modded/vanilla selection context, launch timing, and generic startup failures without repeating marker reads or log lines on later ready-status refreshes.
- The July 5 launcher reachability pass on the connected foldable layout shows Start Game, version selection, vanilla/mod mode controls, staged Workshop controls, Fixes & Help, and Help & Reports reachable in the main scroll area. The launcher is usable on that display but remains visually dense and still needs UX polish.
- The latest tested Steam public-beta payload is `v0.108.0` / build `24032229`. Public-beta fresh download, Android PCK patching, runtime-pack creation, runtime-pack validation, and launch now reach the main menu with matched beta PCK plus matched beta managed runtime. This is no longer a `NativeFallbackActivity` fallback/gating issue.
- Public-after-beta switching was retested after the beta launch and returned to the public PCK/runtime pairing correctly.
- Core-release can launch through its side-by-side slot, but the current Steam metadata still exposes an inherited public manifest, so it is effectively public content until Steam publishes a distinct core-release payload.
- Workshop/mod support is functional but still beta-quality. Historical public-beta and v0.2.399 runs selected a launcher SavesMerger substitute, but current unreleased source removes that save-path override and marks SavesMerger/UnifiedSavePath deprecated. Current Pull and automatic sync keep vanilla and native modded namespaces separate and transfer only the selected exact-context allowlist; they never seed modded saves from vanilla data.
- No Steam Cloud Push was performed during the July 3 validation. Safe-launch testing kept cloud upload disabled/locked.

Validated locally on ARM64 hardware:

- Fresh APK/runtime install reaches the launcher.
- `v0.2.398` passed the 20-viewport deterministic Home/Saves/Versions/Mods/Help preview, accessibility/bounds/target checks, and event contract. Complete unlocked physical destination/rotation coverage remains pending.
- Exact `v0.2.400-powervr-touch-compat` tester APK installed over the existing `com.sts2launcher.overhaul.fork.local` package while preserving app data and reported version code `400001`. Its package and signing certificate match `v0.2.399`, proving update continuity on the current local test channel, not production-signer compatibility.
- Locked-screen interruption returns to the app after manual unlock without app-specific crash markers.
- Steam login and game depot download complete.
- Pull from Cloud downloads real Steam Cloud files.
- Push to Cloud completes on the local hardening build without post-Push process death.
- Pull after Push downloads and writes the pushed cloud state back to Android local storage.
- Android local save handoff works.
- The downloaded game launches and shows the pulled `Profile 1` in-game.
- The selected `public-beta` branch launches from its side-by-side cache on the local ARM64 version-selection hardening build.
- The latest local runtime-pack prerelease proves public-after-beta, public/default, and public-beta launch with matched PCK/runtime evidence on ARM64 hardware; fix30 also proves public can launch immediately after a `public-beta` runtime-cache switch without routing to `NativeFallbackActivity`.
- The latest local UI/public-startup prerelease proves fresh public redownload of `v0.107.1` reaches the game main menu with branch-matched managed runtime evidence and removes the launcher startup status overlay after startup observation.
- Latest connected `v0.2.400` Workshop/mod evidence reconfirms BaseLib and Quick Restart payload loading, Quick Restart's three Harmony targets, and real `NMainMenu` with no focused fatal or native signal. Same-device v0.2.399 evidence remains the behavioral Quick Restart proof. Current `v0.2.416` hardware validation used the vanilla public path, so it does not supersede that mod evidence. Steam Cloud Push was not run.
- Force-stop/relaunch returns to the launcher with saved Steam credentials available.

## Latest hardening evidence

Latest local known-issues/audio evidence:

```text
build=0.2.364-fmod-audio-devices-test
asset=artifacts/android/StS2Launcher-v0.2.364-fmod-audio-devices-test-arm64-v8a.apk
package=com.sts2launcher.overhaul.fork.local
versionName=0.2.364-fmod-audio-devices-test
versionCode=364001
device=RFCY70XQE7F / SM-F966B
validation=Public-beta direct launch reached the Slay the Spire 2 main menu with no NativeFallbackActivity, matched public-beta PCK hash 109f61f7e13a7f329c9fe10aab81dec55e4c415e9863ef5468b76dadac7c7aee, matched source/runtime/active sts2.dll hash 51a671bfeb937271af3e643d017396b13432098ed2b9debceb110c74939bbba1, usable runtime pack public-beta-109f61f7e13a-51a671bfeb93-startup-orchestrator-v1, and patch validation passed.
audioStatus=Fixed at log/runtime level. The Android FMOD bridge loads libfmod.so plus libsts2fmodbridge.so, records native fmod android init complete, reports FMOD Sound System successfully initialized, successfully sets global 3D settings, and loads Master.strings.bank, Master.bank, sfx.bank, temp_sfx.bank, and ambience.bank from res://banks/desktop without the previous invalid object handles, DSP buffer setup failure, JNI NoSuchMethodError aborts, or Cannot load bank errors.
cloudSafety=No Steam Cloud Push was run during this validation; startup logs show upstream Android startup sync was skipped and the cloud write thread stopped.
evidence=artifacts/android/known-issues-fmod-audio-devices-v364-20260705-213145; intermediate bridge crash repros: artifacts/android/known-issues-fmod-jni-bridge-v361-20260705-212010, artifacts/android/known-issues-fmod-asset-manager-v362-20260705-212403, artifacts/android/known-issues-fmod-platform-capabilities-v363-20260705-212804; earlier failed bank-path experiments: artifacts/android/known-issues-fmod-user-banks-v32-20260705-202900 and artifacts/android/known-issues-fmod-sdcard-banks-v33-20260705-203422
```

Latest local Steam auth failure reporting evidence:

```text
build=0.2.366-auth-failure-reporting-test
asset=artifacts/android/StS2Launcher-v0.2.366-auth-failure-reporting-test-arm64-v8a.apk
package=com.sts2launcher.overhaul.fork.local
versionName=0.2.366-auth-failure-reporting-test
versionCode=366001
device=RFCY70XQE7F / SM-F966B
validation=Invalid local credential handoff was consumed, SteamKit connected to Steam, authentication failed with InvalidPassword, and the launcher wrote last_steam_auth_failure.txt with category credentials, selected branch public-beta, user-facing retry guidance, and technical AuthenticationException detail.
shaderTriage=Superseded by v368 normal-launch evidence below; the marker now has ARM64 proof for public-beta warmup completion, while weaker-device shader stall reports remain pending.
cloudSafety=No Steam Cloud Push was run. last_manual_cloud_push.txt remained missing; the existing blocked-push marker was unchanged. The temporary ownership marker move used only to force auth flow was restored after capture.
evidence=artifacts/android/auth-failure-v366-marker-20260705-215808
```

Latest local bounded shader warmup evidence:

```text
build=0.2.376-shader-warmup-budget-evidence-local
asset=artifacts/android/StS2Launcher-v0.2.376-shader-warmup-budget-evidence-local-arm64-v8a.apk
package=com.sts2launcher.overhaul.fork.local
versionName=0.2.376-shader-warmup-budget-evidence-local
versionCode=376000
device=SM-F966B / RFCY70XQE7F
automation=branch=public-beta + action=launch consumed from launcher_automation_action.txt after forcing game cloud sync off for the validation launch; original persisted cloud-sync preference was restored after capture
launch=public-beta reached the Slay the Spire 2 main menu, not NativeFallbackActivity
runtime=selected PCK SHA256 109f61f7e13a7f329c9fe10aab81dec55e4c415e9863ef5468b76dadac7c7aee; source sts2.dll SHA256 51a671bfeb937271af3e643d017396b13432098ed2b9debceb110c74939bbba1; active Android sts2.dll SHA256 0fa05b11c176bfb04271e30ae4e6cc7272f1cc01548a738e4c4d9726e40d1b82; runtime pack /files/runtime_packs/public-beta-8128824d usable; patch validation passed
shaderWarmup=last_shader_warmup_status.txt completed; version=6; budget=90s; detail=Rendered 1713 shader warmup materials; elapsed=40149ms; device diagnostics recorded model=SM-F966B, Android 16/API 36, 8 processors, lowRamDevice=false, totalMemBytes=11653025792, Vulkan hardware feature flags present
cloudSafety=No Steam Cloud Push; validation launch forced CloudSync=false, logs show Android local-only SaveManager and disabled cloud writes ignored, manual Push markers/log signatures absent; persisted cloud_sync_enabled restored to original true after capture
evidence=artifacts/android/shader-warmup-v6-normal-launch-evidence-device-20260706-100947
remaining=Shader scanner still emits noisy Godot error stack traces for some material/scene extraction attempts even though warmup completes and the game reaches the menu; weaker-device `completed-partial`/crash reports are still needed to define a precise minimum spec.
```

Previous local normal-launch/shader warmup evidence:

```text
build=0.2.368-normal-launch-automation-test
asset=artifacts/android/StS2Launcher-v0.2.368-normal-launch-automation-test-arm64-v8a.apk
package=com.sts2launcher.overhaul.fork.local
versionName=0.2.368-normal-launch-automation-test
versionCode=368001
device=SM-F966B / RFCY70XQE7F
automation=branch=public-beta + action=launch consumed from launcher_automation_action.txt; marker completed with selected/requested branch public-beta
launch=public-beta reached the Slay the Spire 2 main menu, not NativeFallbackActivity
runtime=selected PCK SHA256 109f61f7e13a7f329c9fe10aab81dec55e4c415e9863ef5468b76dadac7c7aee; source/runtime/active sts2.dll SHA256 51a671bfeb937271af3e643d017396b13432098ed2b9debceb110c74939bbba1; runtime pack /files/runtime_packs/public-beta-8128824d
shaderWarmup=last_shader_warmup_status.txt completed; detail=Rendered 1713 shader warmup materials; watchdog also logged at 45s during the same run. Superseded by v6 bounded warmup evidence above.
audio=FMOD initialized and desktop banks loaded during launch
cloudSafety=No Steam Cloud Push; last_manual_cloud_push.txt missing and no BeginFileUpload/CommitFileUpload/upload markers in focused logs
evidence=artifacts/android/normal-launch-automation-v368-public-beta-valid-20260705-221923
remaining=Shader scanner emits noisy Godot error stack traces for some material/scene extraction attempts even though warmup completes and the game reaches the menu; controller action handling and weaker-device shader crash/partial-warmup reports remain open.
```

Latest GitHub APK release evidence:

```text
release=v0.2.416-startup-recovery-ime
asset=StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk
sha256=fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b
package=com.sts2launcher.overhaul.fork.local
versionName=0.2.416-startup-recovery-ime-local
versionCode=416001
validation=Exact non-debuggable APK installed on Samsung SM-F966B / Android 16, rendered the launcher with IME hidden, promoted the manifest-matched patched public runtime pack, reached real NMainMenu, and remained stable through the 60-second heartbeat. Managed/Java/cloud tests, Gradle release assembly, ARM64 structure/content/crypto checks, installed-artifact hash equality, and package/signer/version update compatibility against v0.2.412 passed.
cloudSafety=No Push to Cloud was run.
knownIssue=Xiaomi issue #36 and Odin cloud issue #35 require reporter confirmation. Exact-build modded startup, confirmed real Steam Cloud Push, broad manufacturer/GPU coverage, and full cover/inner five-destination visual coverage are not claimed.
evidence=GitHub release v0.2.416-startup-recovery-ime assets and metadata; artifacts/android/v0.2.416-release-hardware-20260725; cumulative forced-failure/recovery evidence under artifacts/android/v0.2.415-cumulative-hardware-20260725.
```

Previous GitHub atlas compatibility release evidence:

```text
release=v0.2.397-atlas-memory-compat
asset=StS2Launcher-v0.2.397-atlas-memory-compat-arm64-v8a.apk
sha256=2ab7d1264ff0c67f8f86a14ed233e9276a4467a63626e09b06510e92145cbeaa
package=com.sts2launcher.overhaul.fork.local
versionName=0.2.397-atlas-memory-compat
versionCode=397001
validation=Release APK build, ARM64 content/ABI checks, Android crypto verification, checksum/metadata generation, and direct-update compatibility against v0.2.396 passed. Same-source local evidence on Samsung SM-F966B reached NMainMenu, passed 1s/3s/10s/30s probes and 60s/300s heartbeats, rendered Card Library and Relic Collection through the individual fallback path, and showed no focused fatal, native signal, ANR, app kill, lifecycle teardown, or Godot static-string cleanup marker.
cloudSafety=No Push to Cloud was run during the matching atlas compatibility validation.
knownIssue=The Pixel 10 Pro / Android 17 / PowerVR D-Series DXT-48-1536 reporter path did not retest v0.2.397. Treat this as a targeted Android atlas/resource-pressure fix, not broad PowerVR/OpenGL ES compatibility signoff.
evidence=GitHub release v0.2.397-atlas-memory-compat assets and metadata; local ARM64 evidence under artifacts/android/issue34-atlas-compat-20260711-055456Z; reporter issue #34 log6.txt identified eager desktop-compressed atlas loading immediately before teardown.
```

Previous GitHub shader-warmup APK evidence:

```text
release=v0.2.377-shader-warmup-budget
asset=StS2Launcher-v0.2.377-shader-warmup-budget-arm64-v8a.apk
sha256=66ed9e5712235eeeb5376d57dd6413dd577c834af6563d8c3d055d886341cbac
package=com.sts2launcher.overhaul.fork.local
versionName=0.2.377-shader-warmup-budget
versionCode=377001
validation=Android build/APK verification passed; APK crypto patch verification passed; GitHub release hygiene check passed with matching APK/checksum/metadata/release-body SHA-256; downloaded release APK verification passed. This release publishes the public-beta stability, FMOD/audio bridge, auth-failure reporting, and bounded shader warmup work, and remains a local-package hardening APK rather than broad release-candidate public-package signoff.
cloudSafety=No Push to Cloud was run during the matching public-beta launch validation.
evidence=GitHub release v0.2.377-shader-warmup-budget assets and metadata; matching bounded shader evidence remains artifacts/android/shader-warmup-v6-normal-launch-evidence-device-20260706-100947, and latest strict Workshop/mod evidence remains the July 3 artifacts listed below.
```

Latest full public/public-beta runtime gate evidence:

```text
release=v0.2.188-local-runtime-beta-fix30-public-after-beta
asset=StS2Launcher-v0.2.188-local-runtime-beta-fix30-public-after-beta-arm64-v8a.apk
sha256=b3f0b645356dfd72e6bcddc735a07352a4b911720f84532b499e543358ce4515
package=com.sts2launcher.overhaul.fork.local
versionName=0.2.188-local-runtime-beta-fix30-public-after-beta
versionCode=218857
validation=Android build/APK verification passed; ARM64 public/default, public-after-beta, public-beta, and branch-switch runtime evidence passed release gates
upgradeBaseline=local runtime-pack validation line
```

Matching launcher UI redesign evidence:

```text
build=0.2.398-launcher-ui-redesign-local
asset=StS2Launcher-v0.2.398-launcher-ui-redesign-local-arm64-v8a.apk
versionCode=398031
sha256=52d05adf3a26ee8c2135edae6ceb986a2d021b99c4b92a4c00eebc7e4fa66d97
scope=five stable Home/Saves/Versions/Mods/Help destinations; phone bottom navigation; wide foldable/tablet top navigation; destination-owned controls; deterministic destination page origin; Android safe-area handling; responsive confirmation, Steam Guard, and diagnostics layouts; retained Pull-before-Push and explicit Push confirmation gates
validation=ARM64 APK build and ABI/content/crypto verification passed; installed over existing app data on SM-F966B; deterministic desktop matrix passed 20 screenshots across phone portrait, phone landscape, foldable, and desktop viewports; accessibility, bounds, target-size, event-routing, and cloud-Push non-invocation contract passed; static audit passed 819 checks
evidence=artifacts/ui-preview; artifacts/android/StS2Launcher-v0.2.398-launcher-ui-redesign-local-arm64-v8a.apk and sidecars
not_yet_proven=exact-build unlocked cover/inner five-destination visual pass and real game handoff; Steam Cloud Push was deliberately not run
```

Previous local launcher UI hardening evidence:

```text
build=0.2.273-local-startup-overlay-fix
asset=StS2Launcher-v0.2.273-local-startup-overlay-fix-arm64-v8a.apk
sha256=46dbb6b05a6f477a105d29356ab25c228b55c80bd094b63589bfe70e0e0ebe12
scope=compact Android launcher polish, including title-cased compact mobile action labels, a subdued current-section top navigator with short app-like Sign in/Verify/Files/Play task labels, structured compact install CTA labels, stable two-line download progress status, stable compact armed Push warning label, direct-parent-safe compact Cloud Safety cue reordering before Pull/Push controls, compact ready-state priority that keeps the ready summary, Save Check shortcut, and Get-saves-first cloud controls before Start Game while moving version management below the primary launch path, compact save-settings drawer demoted below Start Game with Save Backup / Local safety and Cloud Sync / Steam saves labels, user-facing compact support labels such as Safe Start / Cloud off, Check Files / Updates, Game Versions / Refresh list, Repair Files / Rebuild game, Free Space / Old versions, Help Report / Share details, Last Problem / Open details, and Copy Log / Review first, collapsed compact Save Check / Get saves first cloud-safety drawer labels, bounded compact Save Check detail copy, bounded two-line Files for / Play version helper labels, bounded compact Steam Guard helper copy, row-based compact quick-start guidance with visible Sign in/Get files/Get saves/Play/Upload locked steps, readable shared compact detail-label font, readable compact workflow step number badges, shared touch-safe compact height for the inline current-task bar, padded compact scroll anchors so workflow/current-task jumps land active sections below the sticky header instead of flush against the viewport edge, viewport-aware compact status headline reflow after rotation or keyboard viewport changes, short compact status detail copy with tap-to-expand full status and a visible Details/Hide cue, viewport-aware sticky task header reflow after rotation or keyboard viewport changes, viewport-aware compact task re-anchoring after rotation or keyboard viewport changes, viewport-aware compact Steam Guard code/action row reflow after rotation or keyboard viewport changes, rounded shared metric scaling so compact fonts, touch targets, separators, and margins keep fractional Android scale instead of being floored, current-viewport confirmation dialog sizing so branch/cache/cloud warnings stay sized correctly after rotation or keyboard-driven viewport changes, keyboard-focused managed input scrolling so Steam Guard/fallback fields stay reachable above the Android soft keyboard, a readable bounded compact support log viewport with review-before-sharing log copy, viewport-aware diagnostics log resizing after Android rotation or keyboard viewport changes, Android-readable post-launch startup status card, and successful cleanup of that card after main-menu startup observation
validation=static audit, managed Release build, ARM64 APK build, Android crypto verification, APK SHA-256 metadata, APK signature verification, ARM64 install over existing app data on SM-F966B, launcher screenshot capture from pass178, public redownload/startup screenshot capture on v0.2.273, resumed `GodotApp` window-state capture, and focused/PID log scan with no `NativeFallbackActivity`, fatal exception, crash marker, or previous `Child is not a child` Godot UI error
evidence=artifacts/evidence/ui-pass178-device-view; artifacts/android/public-redownload-evidence-20260620
not_yet_proven=touch validation, Help & Reports/launcher-log copy pass visual proof, short compact status detail copy visual proof, public/public-beta branch-switch runtime evidence on this UI build, rotated/keyboard viewport confirmation-dialog capture, focused input soft-keyboard capture, compact status headline rotation/keyboard reflow capture, compact Steam Guard code/action row rotation/keyboard reflow capture, sticky task header rotation/keyboard reflow capture, compact task re-anchor rotation/keyboard capture, and diagnostics log rotation/keyboard resize capture
```

Latest Workshop/mod evidence:

```text
latestStrictModdedRuntimeBuild=0.2.352-savemerger-compat-local
package=com.sts2launcher.overhaul.fork.local
device=RFCY70XQE7F
validation=Public-beta v0.108.0 launches with matched beta PCK/runtime. Strict public-beta modded validation with BaseLib, Quick Restart 2, and manual SavesMerger completes Android selected-root scanning, records `last_mod_launch.json` with `playMode=modded`, `scannedRoots=3`, `enabledMods=3`, and preserves matched public-beta runtime-pack evidence. The previous native upstream scan/load termination is no longer the current blocker.
unsupported=3747532120 / Vanilla and Modded Saves Merger remains unsupported through direct Workshop sync because Steam exposed a legacy Workshop UGC handle but no direct URL or depot manifest; manual import exists at /sdcard/StS2Launcher/Mods/3747532120
cloudSafety=No Steam Cloud Push was run during July 3 runtime/mod validation
evidence=artifacts/android/public-beta-modded-validation-connected-public-beta-modded-savemerger-20260703-10; artifacts/android/workshop-mods-public-beta-connected-public-beta-modded-savemerger-20260703-10-20260703-103201; artifacts/android/multi-version-runtime-public-beta-playable-modeldb-adaptive-20260703-20260703-084139; artifacts/android/workshop-mods-public-beta-public-beta-playable-modeldb-adaptive-20260703-20260703-084139; artifacts/android/multi-version-runtime-public-after-beta-modeldb-adaptive-20260703-20260703-084514; artifacts/android/workshop-mods-public-public-after-beta-modeldb-adaptive-20260703-20260703-084514; artifacts/android/multi-version-runtime-core-release-modeldb-adaptive-20260703-20260703-085147; artifacts/android/workshop-mods-core-release-core-release-modeldb-adaptive-20260703-20260703-085147
docs=docs/android-workshop-mods.md
not_yet_proven=Exact-APK/device validation of native modded-save visibility and exact-context transfer without cross-namespace copying, Android support for BaseLib patch classes currently skipped by the compatibility filter, proof of a distinct core-release Steam payload if Steam exposes one later, controller behavior with mods, shader compile stability on weaker devices, and broader release UX signoff
```

Latest public feedback themes from Reddit/GitHub:

- `v0.2.416` retains the five-destination phone/wide launcher. New reports should focus on regressions such as clipping, unreachable controls, keyboard overlap, wrong destination layout, or broken rotation on unusual displays and foldables.
- Controller behavior needs focused Odin/Thor/Android-handheld testing.
- Some devices may crash or stall during first-run shader compilation.
- Users need exact install/update/uninstall guidance and should report exact release tags rather than "latest".
- Users are interested in Discord/community support, but GitHub issues remain the only tracked support channel for now.

Previous production-package update-compatibility evidence:

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

- `artifacts/android/public-redownload-evidence-20260620`
- `artifacts/evidence/ui-pass178-device-view`
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
- `artifacts/android/fix29-readonly-current-marker-status-20260619-115426`
- `artifacts/android/fix29-readonly-current-marker-status-20260619-115426-public-redacted`
- `artifacts/android/fix30-beta-before-public-20260619-120921`
- `artifacts/android/fix30-beta-active-runtime-20260619-121052`
- `artifacts/android/fix30-public-after-beta-20260619-121225`
- `artifacts/android/multi-version-runtime-public-20260619-121256`
- `artifacts/android/fix30-public-beta-game-launch-20260619-121514`
- `artifacts/android/multi-version-runtime-public-beta-20260619-121531`
- `artifacts/android/fix30-public-beta-pull-cloud-complete-20260619-122423`
- `artifacts/android/fix30-public-beta-synced-compendium-route-20260619-122605`
- `artifacts/android/fix30-public-beta-post-pull-runtime-refresh-20260619-123027`
- `artifacts/android/multi-version-runtime-public-beta-20260619-123054`
- `artifacts/android/StS2Launcher-v0.2.188-local-runtime-beta-fix31-save-origin-pck-arm64-v8a.apk`
- `artifacts/android/fix31-public-beta-post-pull-runtime-refresh-20260619-124648`
- `artifacts/android/multi-version-runtime-public-beta-20260619-124816`
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
fix30PublicAfterBeta=artifacts/android/fix30-public-after-beta-20260619-121225 proves direct public startup after a beta runtime-cache launch no longer falls through to `NativeFallbackActivity`: native startup switched assembly cache identity from public-beta/runtime-pack to public/selected-game, allowed the public legacy runtime path only because active `sts2.dll` matched public source hash 81c8f3443c4504e38a17570df688489414fceb6ea7fcf5b044d8117318ea8e49, mounted files/game/SlayTheSpire2.pck, applied 19/19 runtime patches, and reached the main menu. Structured public evidence artifacts/android/multi-version-runtime-public-20260619-121256 passed 30 public/save-safety checks with PCK hash 8f0dbfef10a31994eb0f58e8d811db08712153c5c0d4491bc5fc4732be530f68. Current post-Pull fix30 public-beta evidence artifacts/android/multi-version-runtime-public-beta-20260619-123054 passed 42 public-beta/save-safety checks with runtime slot public-beta-8128824d-c114cccf86a73ccd, runtime-pack ID public-beta-a263c68cfdeb-4ad31f07b718-startup-orchestrator-v1, mounted PCK hash 957bd95f2bbe97fad18ea467e67b8525861a49aec08a0f31448e276925cb684a, source/runtime-pack/active `sts2.dll` hash 4ad31f07b71820060b178ce3961f8589dbc94b3f8109428eaec8e7037ae2fdb3, and save-origin action `manual cloud pull` for public-beta. The synced-save route artifacts/android/fix30-public-beta-synced-compendium-route-20260619-122605 proves that after Pull from Cloud the game opens on `Profile 1` rather than the tutorial opener, enters Compendium, enters Bestiary, and renders Assassin Raider with focused resource loads for the Bestiary layout and assassin_ruby_raider assets. The old doormaker/no-loader hard-lock route did not reproduce; remaining route warnings are non-blocking `CARD.FOLLOW_THROUGH` save/progress parse warnings, FMOD startup warnings, DXT hardware conversion warnings, and Steam Input initialization warnings. The combined gate passed with current public/current post-Pull public-beta and branch-switch evidence: scripts/run-multi-version-runtime-release-gates.ps1 -PublicEvidenceDirs artifacts/android/multi-version-runtime-public-20260619-121256 -PublicBetaEvidenceDirs artifacts/android/multi-version-runtime-public-beta-20260619-123054 -BranchSwitchEvidenceDirs artifacts/android/multi-version-runtime-branch-switch-20260618-211533 -RequireSaveSafety -Quiet. Pull from Cloud was performed for public-beta route validation; no Steam Cloud Push was performed.
fix31SaveOriginPck=local APK artifacts/android/StS2Launcher-v0.2.188-local-runtime-beta-fix31-save-origin-pck-arm64-v8a.apk (sha256 33fa866b5d8b9462f2aa83cd34606b84b3f7f8b8a5a8da159d08cecc2ed04ae6, versionCode 218858) is installed on the ARM64 device and fixes the selected-runtime save-origin comparison for Android-patched non-public PCKs. Save-origin markers record the source PCK hash; runtime patch validation and native cache markers record the mounted Android-patched PCK hash. The launcher/runtime-cache helpers and read-only collector now accept that pairing only when the selected runtime pack maps the source PCK to the mounted cache PCK and the runtime slot/source assembly still match. Evidence artifacts/android/multi-version-runtime-public-beta-20260619-124816 passed 42 public-beta/save-safety checks and reports Steam Cloud Push save-origin safety `matched` with `pckDirect=False` and `pckRuntimePackSource=True`; the save/config hypothesis is now `unknown` rather than falsely confirmed. The combined gate passed with public evidence, fix31 public-beta evidence, and branch-switch evidence: scripts/run-multi-version-runtime-release-gates.ps1 -PublicEvidenceDirs artifacts/android/multi-version-runtime-public-20260619-121256 -PublicBetaEvidenceDirs artifacts/android/multi-version-runtime-public-beta-20260619-124816 -BranchSwitchEvidenceDirs artifacts/android/multi-version-runtime-branch-switch-20260618-211533 -RequireSaveSafety -Quiet. No Steam Cloud Push was performed.
```

## Historical manual cloud-save evidence (superseded)

The statements and artifacts in this section describe the old manual Pull/Push implementation tested in June 2026. They do not validate the current local-only game saving, launcher-owned automatic synchronization, or Restore/Undo implementation. The exact current-source Stage 5 matrix is 0/10; see [Android release validation](android-release-validation.md). Nothing in this historical section may be used to describe the current save-loss issue as fixed.

The superseded manual Pull and Push path was validated end to end on its local ARM64 hardening build. Steam Cloud files were enumerated/downloaded, Android local save files were written, Push uploaded/flushed the local save batch without process death, and Pull after Push wrote the cloud state back into Android app storage.

- Upload evaluates only transferable local-save presence and interrupted-Pull state before the destructive confirmation can be armed; it still requires a separate arming tap before the overwrite confirmation appears.
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
- Upload confirmation names the overwrite risk and selected local-to-Steam direction. Pull is independent and is not required before Upload.
- Current Upload eligibility uses only transferable files in the selected vanilla/modded local allowlist plus the absence of an interrupted Pull marker. Steam installation, preceding Pull, branch-switch/save-origin history, modded mode, and shared-storage permission do not control eligibility. The transfer then enforces exact SteamID64/runtime/namespace/mod-set context, destination backup, failure propagation, and read-back verification.
- Branch-switch safety evidence now covers the pending-Pull posture after switching public -> public-beta: `current_android_save_origin.txt` records `branch switch pending Pull`, the review report classifies Steam Cloud Push as `do-not-push`, and no Push-to-Cloud control was exercised during the branch-runtime investigation.
- Save discovery now skips app runtime/cache trees such as `.godot`, `cache`, `game`, and `tmp` during fallback enumeration so cloud-save diagnostics stay focused on save candidates.

## Remaining release-readiness blockers

- Steam game version selection is in hardening: selected branch is persisted and used for manifest resolution/update checks, non-public branches download into side-by-side `game_versions/<branch>/game` caches, completed branch downloads write `steam_branch.txt` marker/provenance metadata, inactive cached versions can be cleared from support options, and local backup is enabled before branch switches. The launcher now uses a discovery-led dropdown Steam branch selector instead of normal-user text entry: default/public remains always available, account-visible options refresh from Steam app-info branch availability evidence, and locally installed non-public branch slots remain selectable as recovery/downloaded-cache options. It includes a non-mutating `Refresh Game Versions` action, concise ready/build/password/unavailable/installed dropdown badges, selected-branch helper text, pre-download/update gates for known unavailable branches, wrapped selector guidance for private/password branch hardening and unproven save compatibility, and branch availability diagnostics after failed downloads. Native Android startup now blocks selected-version launch when branch marker provenance is missing or mismatched instead of falling through toward stale branch startup, and the local ARM64 build has validated selected `public-beta` launch from its side-by-side slot. Fix23 also fixes the Android JNI crash seen after confirming a switch to locally installed `public-beta` by using branch-local patch-validation/runtime-pack hashes before any direct non-public PCK hash fallback, and the focused fix23 route retry validates public-beta Compendium/Bestiary rendering without the earlier hard-lock or doormaker loader failure chain. Beta-integrity hardening records per-depot selected/public manifest comparison, explicit public-inherited depot provenance, manifest request branch evidence, selected-branch integrity log summaries, clean-redownload-gated `Classification:` and `Evidence readiness:` summaries, focused logcat, runtime selected-PCK path/hash evidence, and public-vs-beta file inventory/hash capture guidance so mixed beta/public behavior and art asset issues can be classified from evidence. The same selected-version guidance is captured in launcher diagnostics, branch-switch marker evidence, manual Pull evidence, native pre-routing/startup logs, and native fallback diagnostics. Static CI guardrails cover version-selection docs, release blockers, discovery-led dropdown behavior, unavailable-branch gates, native launch gating, Autofill cleanup, beta-integrity evidence fields, and managed/native selector-guidance parity. Refresh-game-versions negative cases, Steam beta/password behavior, inaccessible/private branch handling, cache cleanup, exact save-transfer context/backup/read-back safety, save compatibility across branches, and release-candidate public/default retest still need ARM64 device validation before release-candidate signoff; see [Steam version selection validation](steam-version-selection-validation.md) and [Steam version selection runbook](steam-version-selection-runbook.md).
- The current Steam version-selection release gate is tracked in [Steam version selection release readiness](steam-version-selection-release-readiness.md). Treat that tracker as the source of truth for what is implemented, what is unvalidated, and what evidence is required before release-candidate signoff. The remaining public-beta integrity device pass is summarized in [Steam beta integrity runtime checklist](steam-beta-integrity-runtime-checklist.md).
- Current public-beta runtime evidence rules out silent public fallback for the tested local ARM64 path: native routing selected `public-beta`, Godot mounted the selected beta PCK, startup patches completed, and the game reached the main menu. A public/default auto-launch baseline on the same build reached the same visible main menu but successfully loaded the run-history `doormaker_boss` imported textures where `public-beta` logged loader failures after mounting the selected beta PCK. PCK directory inspection shows `doormaker_boss` run-history imports exist in public but not in `public-beta`; `public-beta` contains `aeonglass_boss` instead and still contains shared `unknown_monster` fallback art. Runtime patching now falls back to branch-local `unknown_monster` art when the game returns a missing run-history icon path. This is a beta content/import compatibility fix, not public-content fallback.
- Current evidence tooling resolves `adb` from explicit `-AdbPath`, PATH, common Android SDK roots, and the repo-local `.w40k-android-toolchain` SDK path. A locked-device read-only capture on `com.sts2launcher.overhaul.fork.local` (`artifacts/android/multi-version-runtime-locked-readonly-cache-runtime-20260618-215544`) still shows matched `public-beta` runtime slot, runtime pack, active `sts2.dll`, and branch-switch pending-Pull save-origin posture, but it is not release signoff: the device was locked, no UI cleanup action was run, focused startup logcat was stale/missing, and no `last_game_version_cache_cleanup.txt` existed yet to prove runtime-pack cleanup on device.
- ARM64 branch-cache cleanup evidence now exists for the local fix23 package: after injecting a controlled dummy orphan runtime-pack directory, `artifacts/android/multi-version-runtime-after-stale-runtime-pack-cleanup-20260618-221746` shows `last_game_version_cache_cleanup.txt` preserving selected `public-beta` cache `public-beta-8128824d`, preserving selected runtime pack `public-beta-8128824d`, removing orphan runtime pack `stale-proof-runtime-pack` with `existsAfterDelete=false`, and recording `Removed runtime pack count: 1`. This proves runtime-pack cleanup/preservation on the local debug build only; release-candidate public/default cleanup, fresh startup logcat, and full public evidence review remain open.
- Fresh public and public-beta runtime evidence on the local ARM64 fix27/fix23 package line now passes the branch-specific review wrapper. Public evidence `artifacts/android/multi-version-runtime-public-20260618-231242` passed `review-multi-version-runtime-evidence.ps1 -RequirePublic -RequireSaveSafety` with 30 checks after a public game launch, and sanitized public-share export `artifacts/android/multi-version-runtime-public-20260618-231242-public-redacted` passed `review-public-evidence-redaction.ps1`. Public-beta evidence `artifacts/android/multi-version-runtime-public-beta-20260618-224239` passed `review-multi-version-runtime-evidence.ps1 -RequirePublicBeta -RequireSaveSafety` with 42 checks after a clean package restart. Together with `artifacts/android/multi-version-runtime-branch-switch-20260618-211533`, the combined gate now passes with public, public-beta, branch-switch, and save-safety evidence. This is not release-candidate signoff because these are local debug package artifacts; private/password/no-manifest negative cases and release-candidate APK evidence remain open.
- Read-only fix27 version-selection marker capture `artifacts/android/steam-version-selection-fix27-readonly-marker-status-20260618-2342` records the then-current ARM64 device state without mutating saves or Steam Cloud. It remains historical branch/runtime evidence only; its Pull and pre-Push marker posture does not validate the current Stage 2 transfer, which still needs direct eligibility, context, backup/tombstone, failure, and read-back evidence.
- Fix27 refresh/dropdown ARM64 evidence now exists at `artifacts/android/steam-version-selection-fix27-refresh-dropdown-20260618-2348`, with sanitized export `artifacts/android/steam-version-selection-fix27-refresh-dropdown-20260618-2348-public-redacted` passing `review-public-evidence-redaction.ps1`. The UI screenshot set shows launcher-only startup, `REFRESH GAME VERSIONS` under support options, refreshed status `Steam game version list refreshed. Selected version: Default.`, dropdown label `Default / public (ready)`, and dropdown option `public-beta (build 23575630)`. The captured `last_steam_branch_availability.txt` records selected branch `public`, selected branch visible in Steam metadata, one Windows depot manifest for selected branch, visible branch count `2`, public build `23478716`, and `public-beta` build `23575630`. This closes the local fix27 refresh/dropdown proof gap only; release-candidate repeat evidence and private/password/no-manifest negative cases remain open.
- Fix27 synthetic unavailable saved-branch evidence now exists at `artifacts/android/steam-version-selection-fix27-unavailable-saved-branch-20260619-0000`, with read-only public-share export `artifacts/android/steam-version-selection-fix27-unavailable-saved-branch-20260619-0000-public-redacted` passing `review-public-evidence-redaction.ps1`. The device was seeded with saved branch `stale-private-proof` while the latest Steam branch availability marker only listed `public` and `public-beta`; the launcher surfaced the selected stale branch, warned that it was not listed in the Steam app-info catalog, and blocked `DOWNLOAD SELECTED VERSION` before any selected-version download/update. The device branch was restored to `public`, a follow-up device search recorded no residual `stale-private-proof` paths under app files, and no Steam Cloud Push was performed. This proves the local stale/absent saved-branch block path only; real account-side password-protected, private/inaccessible, and no-Windows-manifest branch cases still need release-candidate evidence or explicit release-note limitation.
- Fix27 immediate-launch smoke evidence now exists at `artifacts/android/fix27-immediate-launch-smoke-20260619-0015`, with public-redacted text export `artifacts/android/fix27-immediate-launch-smoke-20260619-0015-public-redacted` passing `review-public-evidence-redaction.ps1` after exporter/reviewer hardening for ad hoc raw logcat captures. It proves the installed local package `0.2.188-local-runtime-beta-fix27` / `versionCode=218853` starts from `LauncherActivity`, keeps a live package PID, focuses `GodotApp`, shows the public/default ready launcher UI, and has no strict package crash markers or `NativeFallbackActivity` after launch. The read-only marker capture still shows no `last_manual_cloud_push.txt` or `last_manual_cloud_push_blocked.txt`; no Steam Cloud Push was performed. This is local debug package immediate-crash evidence only, not release-candidate signoff.
- Fix27 public/default game-launch evidence now exists at `artifacts/android/fix27-public-game-launch-smoke-20260619-0027` and structured runtime evidence `artifacts/android/multi-version-runtime-public-20260619-002349`, with sanitized exports `artifacts/android/fix27-public-game-launch-smoke-20260619-0027-public-redacted` and `artifacts/android/multi-version-runtime-public-20260619-002349-public-redacted` both passing `review-public-evidence-redaction.ps1`. The screenshot shows the Slay the Spire 2 main menu with `Profile 1`; focused logs show selected branch `public`, PCK `files/game/SlayTheSpire2.pck` hash `8f0dbfef10a31994eb0f58e8d811db08712153c5c0d4491bc5fc4732be530f68`, source and active Android `sts2.dll` hash `81c8f3443c4504e38a17570df688489414fceb6ea7fcf5b044d8117318ea8e49`, runtime slot `public-d8a7082fc63977cc`, runtime patch compatibility `passed`, and no strict crash markers or `NativeFallbackActivity`. `review-multi-version-runtime-evidence.ps1 -RequirePublic -RequireSaveSafety` passed 30 checks. The report still classifies Steam Cloud Push as `do-not-push` because save-origin evidence is stale/mismatched after the earlier branch switch; no Push marker was created. This proves local fix27 public/default game launch on ARM64, not release-candidate signoff.
- Fix27 selected-version redownload marker evidence now exists at `artifacts/android/fix27-redownload-synthetic-cache-20260619-0035`, with sanitized export `artifacts/android/fix27-redownload-synthetic-cache-20260619-0035-public-redacted` passing `review-public-evidence-redaction.ps1`. The test seeded a synthetic side-by-side branch cache `redownload-proof-2851fdaf` with a valid PCK header but no `steam_branch.txt`, selected that branch, and verified the launcher surfaced the missing/mismatched branch metadata warning plus blocked-cache-delete confirmation because the branch was absent from Steam app-info. Confirming it wrote `last_game_version_redownload.txt` with selected branch `redownload-proof`, side-by-side slot directory, game directory existed before delete `true`, game directory exists after delete `false`, download state after delete `false`, and runtime-pack after delete `false`. The selected branch was restored to `public`, the synthetic slot was removed, the real `public-beta` cache marker remained present, and no Steam Cloud Push marker was created. This proves local selected-cache deletion/redownload marker behavior without deleting real beta content; full release-candidate evidence for real selected-version redownload and replacement download remains open.
- Fix27 exposed a branch-switch Push safety bug: with selected branch restored to `public` while `current_android_save_origin.txt` still recorded `public-beta` pending Pull, tapping `Push Saves to Steam Cloud` armed the destructive confirmation UI instead of writing `last_manual_cloud_push_blocked.txt`. The confirmation was not accepted and no Steam Cloud Push was performed. Evidence is preserved at `artifacts/android/fix27-blocked-push-save-origin-20260619-111324`.
- Fix28 historically moved the then-current selected-version Push gate ahead of the arming UI. Evidence APK `0.2.188-local-runtime-beta-fix28-evidence` / `versionCode=218855` proves selected branch `public` with stale `public-beta` save-origin evidence blocked on the first Push tap and wrote the blocked marker without uploading. That evidence is retained for the older build but does not validate current Stage 2 eligibility or transfer safety, which requires new allowlist, interrupted-Pull, context, backup/tombstone, failure, and read-back evidence.
- Fix28 read-only current marker evidence now exists at `artifacts/android/fix28-readonly-current-marker-status-20260619-113732`, with curated text-only public export `artifacts/android/fix28-readonly-current-marker-status-20260619-113732-public-redacted` passing `review-public-evidence-redaction.ps1`. It proves the connected device was still on local package `0.2.188-local-runtime-beta-fix28-evidence` / `versionCode=218855`, selected branch `public`, public and public-beta branch markers both present, public-beta depot manifest differing from public, SteamKit debug logs disabled (`null`), `last_manual_cloud_push.txt` missing, `last_manual_cloud_push_blocked.txt` present with before-upload block reason, and pre-Push local/cloud backup counts both `0`. The saved encrypted Steam session files were still present after the native-login-panel test with hashes matching the earlier local raw evidence. This was read-only; no Pull from Cloud or Push to Cloud was performed.
- Fix28 capture-script hygiene evidence now exists at `artifacts/android/fix28-readonly-capture-script-diagnostics-index-20260619-1200`, with curated text-only public export `artifacts/android/fix28-readonly-capture-script-diagnostics-index-20260619-1200-public-redacted` passing `review-public-evidence-redaction.ps1`. It verifies `capture-steam-version-selection-evidence.ps1` now writes a bounded `diagnostics/launcher-diagnostics-index.txt` for external diagnostics discovery instead of accidentally collecting a device-root listing; the fixed read-only capture produced a 276-byte index. No Pull from Cloud or Push to Cloud was performed.
- Fix29 local package `0.2.188-local-runtime-beta-fix29-diagnostics` / `versionCode=218856` built and installed on the connected ARM64 device after clearing stale generated Android build intermediates. Read-only evidence at `artifacts/android/fix29-readonly-current-marker-status-20260619-115426`, with curated text-only public export `artifacts/android/fix29-readonly-current-marker-status-20260619-115426-public-redacted` passing `review-public-evidence-redaction.ps1`, proves selected branch `public`, public and public-beta branch markers present, public-beta depot manifest `4153965503881405416` differing from public depot manifest `6171184689563260868`, SteamKit debug logs disabled (`null`), `last_manual_cloud_push.txt` missing, blocked-Push marker present, and pre-Push local/cloud backup counts both `0`. A launcher-open sanity check on the same installed package kept the app alive, focused `GodotApp`, rendered the public/default launcher UI, validated public PCK hash `8f0dbfef10a31994eb0f58e8d811db08712153c5c0d4491bc5fc4732be530f68`, wrote current runtime-slot evidence, and reported `playable=True` through the legacy packaged public runtime path. It also reported the public runtime-pack compatibility manifest as not installed, so this proves public launcher/runtime fallback status only; it does not prove public-beta gameplay or release-candidate readiness. No Pull from Cloud or Push to Cloud was performed.
- Re-run full login/Pull/confirmed-Push/game-launch smoke on an exact current ARM64 candidate, and keep its local test signing boundary explicit. A real Push requires separate explicit authorisation and controlled backups.
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
- Upload warning text names Steam Cloud explicitly, calls out overwrite risk, and directs testers to verify the selected Android local-save context without presenting Pull as a prerequisite.
- Manual cloud-sync start/complete/failure status updates now keep the launcher header aligned with the operation result instead of leaving stale generic status text behind.
- Recovery cleanup logging now describes normal post-startup cleanup as success-path UI cleanup.
- Diagnostics filters retain startup freshness, assembly cache, expectedSource/expectedBytes, cloud sync, and crash evidence while reducing broad log noise.
- Native splash now uses the scalable launcher vector icon, shader-warmup/loading uses an Android-readable mobile-width compact panel, post-launch startup status uses a framed mobile-width card, native fallback keeps verbose diagnostics collapsed until requested and splits recovery actions into responsive rows on narrow landscape screens, the native Steam login panel uses portrait full-width controls plus responsive wide credential/action rows in landscape, short-height copy on cramped landscape screens, sentence-case action labels with Android all-caps transformation disabled, short-height copy reflow when the landscape height class or IME-visible usable height changes, and reflows when Android orientation/screen size changes, and the launcher itself now uses a short-edge-aware responsive shell with collapsible diagnostics, readable bounded compact diagnostics log output with viewport-aware resizing, viewport-aware compact status headline reflow, viewport-aware sticky task header reflow, viewport-aware compact task re-anchoring, viewport-aware compact Steam Guard code/action row reflow, structured compact Sign in with Steam / Android login labeling, compact Steam Guard bounded two-line helper labels, structured compact Verify Code / Submit once labeling, compact Start Game / Ready version launch CTA labeling, structured compact Get Steam Saves / Download to Android labeling, primary structured compact retry recovery, compact launcher-log review labeling, structured compact startup recovery actions, structured compact Upload Locked / Review first title/detail labels, and structured compact Upload to Steam / Overwrite cloud and Confirm Upload / Overwrite cloud title/detail labels after explicit unlock.
- The `0.2.398-launcher-ui-redesign-local` work supersedes the launcher's sticky task header, workflow strip, task re-anchoring, and all-in-one scroll surface. Home, Saves, Versions, Mods, and Help now own stable page bodies; phone layouts use bottom navigation, wide foldable/tablet layouts use top navigation, and Android safe-area plus composition refresh handling covers rotation and cover-display changes. The native login, warmup, startup-status, and fallback behavior described above remains in place.

## Static upgrade/cache freshness review

The Android activity cache path has a usable static evidence chain for upgrade/freshness diagnosis:

- Startup freshness logs package, versionName/versionCode, schema, stored schema, stored versionCode, stored package, runtime arch, cache existence, and `STS2Mobile.dll` bytes.
- Assembly setup logs `cache-hit` versus full recopy and emits `expectedSource`/`expectedBytes` for required managed assemblies.
- Public package runtime proof exists for `v0.2.175 -> v0.2.177`; the remaining gap is local-package in-place upgrade proof after `.local` signing continuity is restored.

## Emulator limitation

Stage 5 used the visible API 36 `sts2_test_api36` x86_64 emulator and current unreleased source. It validated native cold/cached routing, presentation continuity into fallback, fallback input and recovery, forced assembly-bootstrap failure, transactional cache preservation, rotation, Home/Recents resume, exact-once activity state, and native-path keyboard suppression. Decoded cold, cached, forced-failure, and recovery recordings contained no accidental pure-black compositor frame; the cold and forced-failure recordings each contained one designed near-black separation frame within the identity sequence.

Production x86_64 intentionally routes to `NativeFallbackActivity`. A separate forced-Godot diagnostic reproduced the known GodotSharp/Mono native runtime failure with destroyed-mutex/static-string evidence and signal 6. Therefore the emulator does not validate the managed launcher, Steam login/download/cloud, ARM64 runtime behavior, `NMainMenu`, or gameplay.

ARM64 hardware remains the proof target. The current unreleased Stage 5 routing changes must be installed and exercised as an exact ARM64 candidate before they can be included in release claims.

## 2026-06-14 public-beta run-history asset fallback validation

- Evidence APK: `0.2.617-local-beta-art-fallback` (`versionCode=2261217`) installed on the connected Android device.
- Evidence folder: `artifacts/android/runtime-public-beta-art-fallback-20260614`.
- Runtime selected Steam branch: `public-beta`.
- Runtime selected PCK: `/data/user/0/com.sts2launcher.overhaul.fork.local/files/game_versions/public-beta-8128824d/game/SlayTheSpire2.pck`, `sha256=957bd95f2bbe97fad18ea467e67b8525861a49aec08a0f31448e276925cb684a`.
- Startup patch orchestration applied `gameplay/Run history asset fallback`.
- Missing beta run-history paths `res://images/ui/run_history/doormaker_boss.png` and `res://images/ui/run_history/doormaker_boss_outline.png` now resolve to branch-local `unknown_monster` fallback art.
- Focused runtime check found `old_doormaker_loader_error_count=0`.
- Main menu reached: `Main menu present after startup: MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NMainMenu name=MainMenu`.
