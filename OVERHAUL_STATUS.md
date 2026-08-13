# Overhaul status

Updated: 2026-08-13

## Current focus

Keep the launcher on the smallest useful Android path:

1. Keep the existing package identity and Android data paths.
2. Keep Steam authentication and owned-game download.
3. Keep unconditional game launch.
4. Keep one application-local gameplay save store.
5. Keep one verified-result Steam transport and one synchronization service.
6. Keep only automatic synchronization plus three manual Saves actions; omit recovery, backup, export, and safety bureaucracy.
7. Keep one validated mod launch plan and one loader path, with truthful persisted runtime results.

## Current state

- The reduction starts from public `main`, not the abandoned idle-suspension branch.
- Gameplay writes commit atomically to the application files directory before synchronization is notified.
- Automatic pre-load reconciliation, gameplay Push, and the three manual Saves actions share one service and transport.
- Steam authentication and depot download remain product features.
- Workshop and manual mods converge through one validated launch plan. Missing or corrupt selection defaults to Vanilla.
- The exact offline BaseLib plus ImportVanillaSaves chain loads both payloads and installs exact-owner Harmony targets; BaseLib remains honestly `Partial`.
- Cloud failure cannot create a launch loop or block the unconditional local launch fallback.
- Startup-crash recovery and Help diagnostics remain because they are unrelated to gameplay-save management.
- An earlier `0.2.420` local-only candidate was installed in place on one Samsung SM-F971B. That historical run does not validate the restored synchronization implementation.

## Release boundary

Current source is being published as the explicitly unverified `0.2.428` prerelease. No Android device was available for this build. The limited `0.2.425` device run is historical evidence only; it does not validate this candidate or establish broad Android compatibility, a completed importer journey, working Android Steam transfer, phone readiness, or release readiness.

## Reduction verification

- The old coordinator/cache/recovery/evidence save-transfer stack remains removed; the replacement is one transport, one service, and one atomic state document.
- The existing post-`NMainMenu` rendered-frame preparation and Android startup-task lifetime guard are reconnected. The managed Release build passes and the retained save/synchronization/mod/version/startup probe passes 12/12.
- Source inspection confirms the package identity, Android data paths, Steam authentication, depot download, unconditional launch, and local save-store patch remain.
- `StS2Launcher-v0.2.428-launcher-simplification-unverified-arm64-v8a.apk` was built and structurally inspected with package `com.sts2launcher.overhaul.fork.local`, version code `428000`, ARM64-only payload, signer SHA-256 `FD0E3D5ACF435C1D23BFC5C426E99AA9EB5808619FF1FC214FFCA99CFAC7E57A`, and APK SHA-256 `CC79353BE2B22641BC76424BBAB9AB36F7AB4A57F8C900379D20E91862005C4C`.
- A limited run on one Samsung ARM64 device showed both selected mods discovered, payload-loaded, initialized, and activated; the modded save namespace was read and the game reached the main menu. The user observed marked improvement and mods appearing to work. The retained event is classified as an intermittent live-process freeze after the main menu appeared, not a proven process crash. It prevented direct confirmation of the importer control and launcher relaunch result. The device is no longer connected, so Stage 9 remains incomplete.
- `StS2Launcher-v0.2.426-main-menu-handoff-diagnostic-arm64-v8a.apk` was built and inspected offline. It is a diagnostic artifact only; no Android stability, freeze, crash, mod, save, or Steam fix is claimed.
- `StS2Launcher-v0.2.427-deferred-preload-experiment-arm64-v8a.apk` was built and inspected offline. Neither experiment arm has run because no device is available, so it establishes no stability or causality result.
