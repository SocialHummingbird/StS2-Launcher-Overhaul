# Current Android status

Updated: 2026-08-13

StS2 Launcher is an unofficial Android launcher for Steam owners of Slay the Spire 2. The current source retains Steam authentication, owned-game and Workshop download, ARM64 launch, save synchronization, and one validated mod-loading path. The current candidate has not completed its Android acceptance journey.

## Current source

The current reduction keeps six product responsibilities:

- Steam authentication and ownership checking.
- Game and Workshop download.
- Game launch, including existing renderer recovery choices.
- Atomic application-local gameplay saving.
- One Steam save-synchronization path shared by automatic and manual actions.
- One validated Workshop/manual mod launch plan and loader with truthful last-launch results.

## Save data

Android gameplay writes land first in Godot's `user://` directory inside the private application data for package `com.sts2launcher.overhaul.fork.local`. Current source includes automatic pre-load reconciliation, automatic verified Push during gameplay, and the three manual Saves actions, all through one synchronization service. No real Android Steam transfer has validated this implementation yet. Clearing the app's data or uninstalling the app removes the local copy; do not assume Steam contains a verified copy.

Startup-crash recovery and Help diagnostics remain. They recover launcher startup or collect troubleshooting information; they do not manage gameplay saves.

## Current prerelease artifact

The current public test candidate is the prerelease `v0.2.428-launcher-simplification-unverified`. It is not a stable or phone-ready release, and no Android device was available to validate this build.

- Package: `com.sts2launcher.overhaul.fork.local`
- Version code: `428000`
- APK SHA-256: `CC79353BE2B22641BC76424BBAB9AB36F7AB4A57F8C900379D20E91862005C4C`

The asset is `StS2Launcher-v0.2.428-launcher-simplification-unverified-arm64-v8a.apk`. Its package and signer match the prior local-test lineage and its archive contains only `arm64-v8a`.

Preserving that package identity and signing continuity is required for an in-place update to retain existing app-private data.

## Offline diagnostic artifact

**DIAGNOSTIC MAIN-MENU HANDOFF APK — ANDROID STABILITY UNVERIFIED**

`StS2Launcher-v0.2.426-main-menu-handoff-diagnostic-arm64-v8a.apk` was built and inspected offline with version code `426000`, package `com.sts2launcher.overhaul.fork.local`, ARM64-only contents, and APK SHA-256 `5FFE86369B05C78CCF528C2186A46CD2E901A186CFB91F490F5C63B44490F294`.

This diagnostic APK restores the existing menu-preparation/startup-task lifetime guard, but it has not been run on Android and does not establish that the intermittent main-menu freeze—or any crash, mod, save, or Steam behaviour—is fixed. It is not the current public prerelease.

## Deferred-preload experiment artifact

`StS2Launcher-v0.2.427-deferred-preload-experiment-arm64-v8a.apk` is a second
offline diagnostic artifact. It has version code `427000`, package
`com.sts2launcher.overhaul.fork.local`, ARM64-only contents, signer continuity
with the local-test lineage, and APK SHA-256
`78889980818C16E311B42BCAA87E28EDF00377C2C7C61CD9328F209E04DABAFF`.

The same APK supports both experiment arms. Normal loading is the default; an
ADB-controlled setting sampled at process creation arms a one-shot suppression
of only the first deferred `LoadCommonAndMainMenuAssets()` call after
`ExecuteDeferred()` completes. Later calls remain normal. No device is attached,
so neither arm has run and no stability or causality conclusion exists yet.

## Evidence boundary

No Android device is currently connected. In the retained one-device run, both exact selected mods were discovered, payload-loaded, initialized, and activated; BaseLib remained honestly `Partial`, a real `modded/` save path was used, and the game reached the main menu. The user reported marked improvement and that mods appeared to work.

The single authoritative failure timeline in `artifacts/android/stage9-one-device-filtered.log` is: **mod activation completed → `NMainMenu` appeared → the 1-second heartbeat ran → the 3-second heartbeat ran → the expected 10-second heartbeat was the first missing milestone**. The same process emitted focus lifecycle events minutes later, and the retained log contains no fatal exception, native signal, ANR, low-memory kill, or process-death evidence. Classify this event as an **intermittent live-process main-loop freeze after the main menu appeared**, not a proven process crash. Any future event with genuine process-exit evidence must be recorded as a separate incident rather than used to reclassify this one.

The run did not directly capture the importer control or a matching relaunch result. It therefore does not complete Stage 9 or prove broad Android mod compatibility. Real Android Steam transfer also remains unproven.

Historical device evidence applies only to the exact historical artifact and path recorded with it.

## Known limitations

- ARM64 Android remains the intended game target; x86_64 emulator results are diagnostic only.
- Device, Android-version, and graphics-driver compatibility varies.
- Steam branches and Workshop mods remain experimental.
- The exact one-device BaseLib plus ImportVanillaSaves journey remains mandatory before any Android mod-loading success claim.
