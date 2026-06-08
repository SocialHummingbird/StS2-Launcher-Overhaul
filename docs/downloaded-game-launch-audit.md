# Downloaded-game launch audit

Last updated: 2026-06-07

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

- Steam cloud pull previously crashed while fetching CDN bytes.
- Root cause was raw Android .NET HTTP use in the cloud CDN file download path.
- `SteamKit2CloudSaveStore.Transfer` now uses `AndroidJavaHttpMessageHandler.CreateCdnClient()` on Android.
- Verified once after patch: pull completed without crash, downloading 2 known save files and reporting 28 known fallback paths not present.

Open cloud issue:

- Cloud enumeration still reports task-cancelled failures.
- Manual known-path probing can recover common save files, but enumeration is not reliable enough for release readiness.
- CCloud RPC waits now have a deterministic 45-second timeout wrapper and named cancellation/timeout errors, so future logs should say which Steam cloud method failed instead of only `A task was canceled`.
- Android manual pull fallback now probes `.backup` variants of key profile save files:
  - `progress.save.backup`
  - `current_run.save.backup`
  - `current_run_mp.save.backup`
  - `prefs.backup`
  - `prefs.save.backup`

## Current verified state

Verified on connected ARM64 phone:

- App alive after launch.
- Downloaded PCK loads.
- Assembly cache hits required managed files.
- Startup/mobile patches apply.
- Game reaches main menu.
- Pulled-save launch reaches Early Access modal.
- Recovery controls self-clean after successful startup.
- Normal `START GAME` launch no longer consumes stale safe-launch state.

## Remaining launch-readiness blockers

1. FMOD extension initialization is still noisy on Android.
   - Fixed: `music_controller_proxy.gd` no longer reports missing `FmodEvent`, `FmodBankLoader`, or `FmodServer` types.
   - Fixed: `FmodManager` autoload and desktop bank loading are stripped again.
   - Remaining: registering `res://addons/fmod/fmod.gdextension` still triggers FMOD system initialization errors before managed startup, including invalid FMOD object handles and DSP buffer setup failure.
   - Dumped PCK evidence shows remaining FMOD-dependent scripts are compiled `.gdc`, so patching them safely is not currently practical with byte-preserving PCK edits.
   - Next realistic options are either make the FMOD extension initialize cleanly on Android, rebuild/stub the dependent GDScript bytecode, or accept the nonfatal FMOD init noise while keeping launch stable.

2. Steam cloud enumeration is unreliable.
   - Known-path fallback works, but backup save discovery is still incomplete.
   - Enumeration timeout/cancellation needs a bounded retry/timeout strategy or a stronger fallback list.
   - The low-level CCloud wait path now reports method-specific timeout/cancellation errors.
   - Manual pull now probes known backup save names directly.
   - Manual pull needs another user-triggered run to determine whether the failing method is still `Cloud.EnumerateUserFiles#1`, whether backup paths exist, or whether a later cloud operation fails.

3. Manual cloud pull needs repeated crash regression.
   - One post-fix pull survived.
   - Release readiness needs repeated pull attempts, including after app restart.

4. Texture compression warnings remain.
   - Game reaches menu, but Android is falling back from desktop-compressed textures.
   - Treat as performance/memory risk until play-session testing proves it is acceptable.

5. Some mobile patches remain intentionally disabled.
   - UI scale dropdown replacement is disabled.
   - Event layout patch is disabled.
   - These avoid known Android wrapper instability but leave feature gaps.

6. Direct ADB game launch remains unavailable.
   - Direct ADB start of `GodotApp` with launch extras is blocked because `GodotApp` is not exported.
   - Device validation currently uses launcher UI taps; the confirmed `START GAME` coordinate on the current landscape device is around `555,1535`.

7. Fault-injection setting must stay disabled outside diagnostics.
   - `sts2_force_critical_patch_failure` was used to prove critical fallback behavior.
   - The connected device was reset to `sts2_force_critical_patch_failure=0` after proof.
   - Release validation should confirm this setting is off before any normal gameplay run.

## Next validation pass

After rebuilding and installing:

1. Launch app.
2. Start game from launcher.
3. Confirm PCK patch marker v27 runs or FMOD repair log appears.
4. Confirm main menu appears.
5. Confirm no FMOD parse errors.
6. Press `Proceed`.
7. Exercise menu navigation and save/profile visibility.
8. Run `Pull from Cloud` twice, once before and once after app restart.
9. Capture only filtered logs for:
   - `STS2Mobile`
   - `Patch orchestration`
   - `PCK`
   - `FMOD` / `Fmod`
   - `Cloud`
   - `AndroidRuntime`
