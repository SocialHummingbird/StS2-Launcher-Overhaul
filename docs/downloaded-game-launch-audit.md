# Downloaded-game launch audit

Last updated: 2026-06-08

## Scope

This audit tracks the Android launcher path from launcher UI to downloaded game startup:

1. Launcher requests one-shot game start.
2. `GodotApp` consumes the one-shot launch request.
3. `GodotApp` sets Android environment flags and appends the downloaded `SlayTheSpire2.pck`.
4. Java prepares the app-private managed assembly cache.
5. Java applies Android PCK startup repair before Godot loads the PCK.
6. Godot/.NET loads `STS2Mobile`.
7. `STS2Mobile` applies startup/mobile Harmony patches.
8. Launcher startup gate starts the real game scene.
9. Recovery UI either reports deterministic failure or self-cleans after successful startup.

## Runtime requirements

### Managed assemblies

Required managed files observed in the app-private assembly cache:

- `STS2Mobile.dll`
- `SteamKit2.dll`
- `0Harmony.dll`
- `protobuf-net.dll`
- `protobuf-net.Core.dll`
- `System.IO.Hashing.dll`
- `System.Private.CoreLib.dll`
- `ZstdSharp.dll`
- `GodotSharp.dll`
- `sts2.dll`
- `Steamworks.NET.dll`
- `Sentry.dll`

Current evidence:

- Latest device logs show `SteamKit2.dll` and `Steamworks.NET.dll` cache hits.
- Previous missing `Steamworks.NET.dll` and `Sentry.dll` failures are patched in cache preparation.
- Latest game start reached main menu, so managed startup dependencies are sufficient for that path.
- Latest cloud-pull test logs showed `Assemblies already set up` / `cache-hit` while the launcher still displayed old confirmation-dialog behavior. This means reinstalling the APK can leave stale managed runtime code active if the app-private Mono publish cache is not invalidated.
- `ASSEMBLY_CACHE_SCHEMA` is now `22`.
- Assembly cache diagnostics now include `schema=<number>` in every phase log. If the next device logs do not show `schema=22`, the installed Java/APK path is stale before any save-system conclusions can be trusted.
- Assembly cache required-file logs now include `expectedBytes=<n>` next to cached `bytes=<n>` so stale packaged/game assemblies are visible without pulling files from the device.
- Game assembly cache validation now compares copied game `.dll` / `.json` files against the downloaded source files byte-for-byte instead of only checking existence and length.
- Packaged bootstrap assemblies are still compared against APK assets through `packagedAssetMatchesFile`.

### Native libraries

Required ARM64 native libraries included in the release merge inputs include:

- `libgodot_android.so`
- `libmonosgen-2.0.so`
- `libsteam_api.so`
- `libsentry.so`
- `libfmod.so`
- `libfmodstudio.so`
- `libGodotFmod.android.template_release.arm64.so`
- `libspine_godot.android.template_release.arm64.so`
- .NET native support libraries under `libSystem.*.so`

Current evidence:

- ARM64 package contains FMOD native libraries.
- Previous PCK patch rules stripped FMOD GDExtension registration even though native FMOD libraries exist.
- This caused runtime parse errors for `FmodEvent`, `FmodBankLoader`, and `FmodServer`.

### PCK startup repair

The PCK repair layer currently handles:

- `project.binary`
- `project.godot`
- `.godot/extension_list.cfg`
- `scenes/game.tscn`

Current behavior after this audit update:

- Sentry startup references remain stripped.
- FMOD GDExtension registration is preserved/restored so GDScript type references can resolve.
- FMOD manager autoload and desktop bank/listener scene nodes are stripped so the game does not load desktop FMOD banks on Android.
- Java patch marker was bumped from `.android_pck_patch_v26` through `.android_pck_patch_v28` so existing installs attempt repair on next launch.

Risk:

- If the existing PCK was modified in a way outside the known reversible FMOD edits, full game file repair or re-download may still be required.
- Keeping `res://addons/fmod/fmod.gdextension` registered currently avoids `music_controller_proxy.gd` parse errors, but the FMOD extension still emits Android initialization errors before managed startup.
- A gated PCK diagnostic dump is available through Android global setting `sts2_dump_pck_diagnostics=1`.
- The diagnostic dump confirmed the FMOD-dependent scripts in the downloaded PCK are compiled `.gdc` bytecode, not editable `.gd` source:
  - `addons/fmod/FmodManager.gdc`
  - `src/gdscript/audio_manager_proxy.gdc`
  - `src/gdscript/music_controller_proxy.gdc`
- Because the dependent scripts are compiled, removing the FMOD GDExtension entirely is high-risk unless replacement bytecode or source stubs are supplied.

### Environment flags

Known launch flags:

- `STS2_AUTO_LAUNCH_GAME=1` for one-shot game startup.
- `STS2_AUTO_SAFE_LAUNCH=1` for safe launch.

Current evidence:

- Latest logs show `Auto-launch game mode: true`.
- Latest normal launch logs show `Auto-safe-launch mode: false`.
- Safe launch marker lifecycle was fixed so normal launch clears stale Java and managed safe-launch state.
- Latest normal launch reached main menu in about 2.9 seconds without consuming manual safe launch.

## Patch orchestration

Latest observed startup patch result:

- `17/17 applied`
- `0 failed`
- `criticalFailed=False`

Important behavior:

- Critical patch failure must block game startup and keep the launcher/recovery path visible.
- If the `GameStartupWrapper` patch succeeds, `LauncherStartupFlow` blocks downloaded-game startup while `ModEntry.HasStartupFallbackReason` is active.
- If the startup-gate patch itself fails, `ModEntry` now schedules standalone launcher fallback and adds an opaque fallback shield behind the launcher, so the broken downloaded-game scene is visually suppressed.
- Successful startup now logs scene snapshots and keeps recovery controls only briefly.

Current evidence:

- Latest logs show `NGame.GameStartup completed`.
- Latest logs show `Main menu present after startup`.
- Latest logs show `Post-startup recovery cleanup finished; controlsCleared=True, statusCleared=True, scene snapshot retained`.
- Fresh screenshot after cleanup shows no launcher recovery overlay.
- Forced critical patch failure proof on 2026-06-08:
  - Android setting `sts2_force_critical_patch_failure=1` injected a core critical failure.
  - Logs showed `criticalFailed=True`.
  - Logs showed `Startup fallback shield displayed`.
  - Logs showed `Startup fallback launcher raised above shield`.
  - Logs showed `Startup fallback banner displayed`.
  - Screenshot `artifacts/android/sts2-forced-critical-fallback.png` showed launcher over a dark fallback surface, not the old broken game screen.

## Cloud save path

Current manual pull behavior:

- The launcher `Pull from Cloud` confirmation dialog is currently unreliable on device: tapping `OK` can focus the button but not activate it.
- The controller now bypasses the confirmation dialog only for manual pull, so `Pull from Cloud` should execute immediately after the next install if the fresh managed assembly is actually loaded.
- Manual push still requires confirmation because it can overwrite Steam cloud saves.
- Manual pull/push now reload encrypted Steam credentials from disk before starting, so button actions do not depend on stale static cloud state.
- Credential reload is authoritative: if persisted credentials are missing or fail to decrypt, static cloud credentials are cleared before manual pull/push.
- If manual pull/push cannot find usable credentials after reload, the UI failure message now explicitly tells the user to log in again before pulling cloud saves.
- The SteamKit2 cloud store singleton now recreates itself when account name or refresh token changes, so credential reload cannot keep using an old Steam cloud connection.
- The dialog implementation also has fallback pointer hit testing for OK/Cancel, but this should not be the main dependency for pull.
- Manual pull writes local files through `AndroidLocalSaveStore`, whose base path is `OS.GetUserDataDir()`.
- Android local save store creation now logs `[Cloud] Android local save base: ...`.
- Every cloud-to-local write now logs the logical save path and the concrete app-private filesystem path:
  - `[Cloud] Android local save write: <logical> -> <full path> (<bytes> bytes)`
  - `[Cloud] Local write path: <logical> -> <full path>`
- Android local save reads and file probes now log concrete app-private paths for save-like files:
  - `[Cloud] Android local save exists: <logical> -> <full path> = <true|false>`
  - `[Cloud] Android local save read: <logical> -> <full path>`
  - `[Cloud] Android local save files: <logical dir> -> <full path> count=<n>`
- Normal Android launch no longer disables `SaveManager` cloud/save-store injection. This is critical: the game must construct a patched save manager backed by the same local store that manual pull writes to.
- Android `SaveManager` construction now falls back to a local-only `CloudSaveStore` using `AndroidLocalSaveStore` plus a disabled cloud store even when cloud sync is disabled or saved Steam credentials are not available in memory. Cloud credentials should decide cloud access only; they must not decide whether the game reads the Android save directory. The disabled cloud store reports reads as missing and ignores writes/deletes with logs so upstream save code can still complete local writes.
- Safe launch and previous-stall recovery can still force cloud access off. Android local save-store injection remains available so local pulled saves can still be read.

Open cloud issue:

- Cloud enumeration still reports task-cancelled failures.
- Manual known-path probing can recover common save files, but enumeration/path discovery is not reliable enough for release readiness until the next device run proves it finds the user's actual cloud files.
- CCloud RPC waits now have a deterministic 45-second timeout wrapper and named cancellation/timeout errors, so future logs should say which Steam cloud method failed instead of only `A task was canceled`.
- Android manual pull fallback probes `.backup` variants of key profile save files:
  - `progress.save.backup`
  - `current_run.save.backup`
  - `current_run_mp.save.backup`
  - `prefs.backup`
  - `prefs.save.backup`
- Manual pull path discovery now also recursively enumerates store directories and adds discovered save-like files:
  - `.save`
  - `.save.backup`
  - `.run`
  - `.bak`
  - `prefs`
  - `prefs.save`
- Manual pull logs the first 25 candidate sync paths and logs a distinct no-download completion message when zero files are downloaded.
- Manual pull/push summaries now return to the launcher UI after completion, so `0 downloaded` or real downloaded counts are visible without opening logcat.
- Android manual pull now attempts the game's managed save path APIs first, then adds Android fallback/backup/enumerated paths. Pull should no longer rely only on guessed `profile*/saves/*` paths when the game expects a different managed layout.
- Manual pull path candidates are canonicalized and deduplicated before probing cloud, so managed/fallback overlap does not trigger repeated downloads or misleading candidate counts.
- Steam cloud enumeration now logs the first 25 canonical cloud file names. Comparing this sample to `[Cloud] Candidate sync paths` should expose path naming mismatches immediately.

Current save-system failure model:

1. If stale `STS2Mobile.dll` remains in `.godot/mono/publish/<arch>`, new C# fixes do not run. This explains why confirmation bypass/fallback fixes appeared ineffective on device.
2. If normal Android game launch disables save-store injection, pulled files can be written into the launcher's Android local store while the game reads a different/default store. This explains a launch reaching the main menu but not surfacing pulled saves.
3. If `SaveManager` replacement requires in-memory cloud credentials, a fresh game-start process can still miss the Android local store even after manual pull succeeds. Android now creates a local-only patched save manager when cloud credentials are unavailable.
4. If the SteamKit2 cloud store singleton keeps an old connection after credential reload/account switch, manual pull can query the wrong session. The singleton is now credential-sensitive.
5. If cloud enumeration/path discovery misses backup or variant filenames, manual pull can complete with zero useful downloads despite Steam containing save files. Managed path probing, recursive path enumeration, and explicit zero-download logs are now in place to expose this.

## Current verified state

Verified on connected ARM64 phone:

- App alive after launch.
- Downloaded PCK loads.
- Assembly cache hits required managed files.
- Startup/mobile patches apply.
- Game reaches main menu.
- Normal game launch reaches the Early Access modal and main menu.
- Recovery controls self-clean after successful startup.
- Normal `START GAME` launch no longer consumes stale safe-launch state.

## Current local implementation state

Implemented locally but not yet verified on device:

- Assembly cache schema is bumped to `22`.
- Assembly diagnostics include `schema=<n>`, cached `bytes=<n>`, and `expectedBytes=<n>`.
- Cached downloaded game assemblies/config files are compared byte-for-byte against source files.
- `Pull from Cloud` bypasses the broken confirmation dialog; `Push to Cloud` still requires confirmation.
- Manual cloud actions reload encrypted Steam credentials before running.
- Failed credential reload clears static cloud credentials instead of reusing stale tokens.
- The SteamKit2 cloud store singleton is recreated when account name or refresh token changes.
- Android normal launch no longer globally disables save-store injection.
- Android game startup can create a local-only `CloudSaveStore` backed by `AndroidLocalSaveStore` when cloud credentials are unavailable.
- Manual pull probes managed game save paths, fallback Android paths, backup variants, and recursively enumerated save-like paths.
- Manual pull returns its real summary to the launcher UI.
- Steam cloud enumeration logs a sample of canonical cloud filenames.
- Android local save writes, reads, existence checks, and directory enumeration log concrete filesystem paths.
- `scripts/collect-android-save-validation.ps1` captures focused evidence and writes `summary.txt` gate output.

Do not treat the items above as proven until a rebuilt APK is installed and the next phone run shows matching runtime logs. The first gate is `schema=22`; if it is missing, the device is still running stale Java/APK code.

## Next-session decision boundary

The next connected-device session must follow this order:

1. Prove the installed runtime is fresh.
   - Required evidence: `Assembly cache diagnostics ... schema=22`.
   - If missing: stop save/cloud investigation and fix build/install/APK packaging first.
2. Prove managed assemblies were refreshed.
   - Required evidence: either `New version detected, re-copying all assemblies` or `cache-hit` with `STS2Mobile.dll bytes=<fresh build size> expectedBytes=<same fresh build size>`.
   - If stale: fix Mono publish cache invalidation before testing pull.
3. Prove pull actually executes.
   - Required evidence: `Pulling cloud saves to local...`, no old pull confirmation dialog, and a returned pull summary in the launcher UI.
   - If not: fix launcher cloud button/controller path before testing Steam cloud.
4. Prove cloud path discovery.
   - Required evidence: `[Cloud] Candidate sync paths: ...`.
   - If enumeration succeeds, also compare `[Cloud] Enumerated cloud file sample: ...`.
   - If zero downloads and cloud sample names do not overlap candidates: fix path mapping.
5. Prove local write/read alignment.
   - Required evidence: `[Cloud] Android local save write: ... -> <full path>` followed by game startup probing/reading the same logical/full paths.
   - If writes happen but reads do not: fix `SaveManager`/local-store injection.
6. Prove game surfacing.
   - Required evidence: pulled save/profile is visible/usable in normal launch.
   - If paths align but UI does not surface saves: investigate game save migration/profile selection, not Steam pull.

Not currently verified:

- Fresh install after schema `22` forces `New version detected, re-copying all assemblies`.
- Manual `Pull from Cloud` runs without showing or depending on the inert confirmation dialog.
- Steam cloud pull downloads the user's real save files on the current device/account.
- Pulled files land under the logged Android local save base.
- The game constructs the patched save manager during normal launch and surfaces the pulled save/profile.

## Remaining launch-readiness blockers

1. FMOD extension initialization is still noisy on Android.
   - Fixed: `music_controller_proxy.gd` no longer reports missing `FmodEvent`, `FmodBankLoader`, or `FmodServer` types.
   - Fixed: `FmodManager` autoload and desktop bank loading are stripped again.
   - Remaining: registering `res://addons/fmod/fmod.gdextension` still triggers FMOD system initialization errors before managed startup, including invalid FMOD object handles and DSP buffer setup failure.
   - Dumped PCK evidence shows remaining FMOD-dependent scripts are compiled `.gdc`, so patching them safely is not currently practical with byte-preserving PCK edits.
   - Next realistic options are either make the FMOD extension initialize cleanly on Android, rebuild/stub the dependent GDScript bytecode, or accept the nonfatal FMOD init noise while keeping launch stable.

2. Save pull and save surfacing are not yet proven end to end.
- Current local fixes address stale managed assemblies, the inert confirmation dialog, normal-launch save-store injection, and recursive backup/variant path discovery.
- Android local-only `SaveManager` fallback now ensures the game can use pulled local saves even if cloud credentials are not loaded during game startup.
- The next device run must prove that the installed APK actually refreshes `.godot/mono/publish/<arch>/STS2Mobile.dll`.
   - The next device run must prove that manual pull writes files under the same Android local save base used by the game.
   - The next device run must prove that the game shows/uses the pulled save after normal launch.

3. Steam cloud enumeration is unreliable.
   - Known-path fallback exists, and recursive store enumeration has been added for backup/variant filenames.
   - Enumeration timeout/cancellation needs repeated device evidence with the new candidate-path logs.
   - The low-level CCloud wait path now reports method-specific timeout/cancellation errors.
   - Manual pull needs another user-triggered run to determine whether the failing method is still `Cloud.EnumerateUserFiles#1`, whether backup paths exist, or whether a later cloud operation fails.

4. Manual cloud pull needs repeated crash regression.
   - One post-fix pull survived.
   - Release readiness needs repeated pull attempts, including after app restart.

5. Texture compression warnings remain.
   - Game reaches menu, but Android is falling back from desktop-compressed textures.
   - Treat as performance/memory risk until play-session testing proves it is acceptable.

6. Some mobile patches remain intentionally disabled.
   - UI scale dropdown replacement is disabled.
   - Event layout patch is disabled.
   - These avoid known Android wrapper instability but leave feature gaps.

7. Direct ADB game launch remains unavailable.
   - Direct ADB start of `GodotApp` with launch extras is blocked because `GodotApp` is not exported.
   - Device validation currently uses launcher UI taps; the confirmed `START GAME` coordinate on the current landscape device is around `555,1535`.

8. Fault-injection setting must stay disabled outside diagnostics.
   - `sts2_force_critical_patch_failure` was used to prove critical fallback behavior.
   - The connected device was reset to `sts2_force_critical_patch_failure=0` after proof.
   - Release validation should confirm this setting is off before any normal gameplay run.

## Next validation pass

After rebuilding and installing:

Use `scripts/run-next-android-save-validation.ps1` when the phone is connected and ready. It builds/installs, starts timed capture, and prints the phone actions to perform. Use `scripts/start-android-save-validation-capture.ps1` when the APK is already installed. Use `scripts/collect-android-save-validation.ps1 -DeviceSerial <serial> -ClearLogcat` for lower-level manual capture. For one-command capture, use `-ClearLogcat -WaitSeconds <n>`, perform the pull/launch while it waits, then read the generated `summary.txt`. Add `-DumpSaveFiles` after a pull attempt when `run-as` works for the installed package. The collector writes `summary.txt` and `manifest.json` gate booleans so schema/cache/cloud/save evidence can be read without manually scanning full logcat.

1. Launch app.
2. Confirm Java logs `New version detected, re-copying all assemblies`.
3. Confirm `Assembly cache diagnostics [before-copy]` or `[after-copy]` includes `schema=22`.
4. Confirm `Assembly cache diagnostics [after-copy]` shows `STS2Mobile.dll bytes=<n> expectedBytes=<same n>` with the fresh build size.
5. Tap `Pull from Cloud`.
6. Confirm no confirmation dialog blocks execution.
7. Confirm logs include `[Cloud] Android local save base: ...`.
8. Confirm logs include `[Cloud] Candidate sync paths: ...`.
9. Confirm logs include `[Cloud] Enumerated cloud file sample: ...` when Steam enumeration succeeds.
10. Compare the cloud file sample to candidate sync paths if pull downloads zero files.
11. Confirm pull logs either `wrote <path>` for real save files or the explicit `complete with no downloads` message.
12. Confirm the launcher UI also shows the manual pull summary, not only generic `Pull complete.`
13. If files were written, compare `[Cloud] Android local save write` full paths to the logged Android local save base.
14. Start game from launcher in normal mode.
15. Confirm game startup logs `[Cloud] Created Android local-only SaveManager` or `[Cloud] Created SaveManager with SteamKit2 cloud store`.
16. Compare `[Cloud] Android local save exists/read/files` paths during game startup to the pull write paths.
17. Confirm main menu appears.
18. Confirm no FMOD parse errors.
19. Press `Proceed`.
20. Exercise menu navigation and save/profile visibility.
21. Run `Pull from Cloud` twice, once before and once after app restart.
22. Capture only filtered logs for:
   - `STS2Mobile`
   - `Patch orchestration`
   - `PCK`
   - `FMOD` / `Fmod`
   - `Cloud`
   - `AndroidRuntime`
