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

Current source is being published as an explicitly unverified prerelease. The limited `0.2.425` device run is useful evidence for this exact candidate, but it does not establish broad Android compatibility, a completed importer journey, working Android Steam transfer, phone readiness, or release readiness.

## Reduction verification

- The old coordinator/cache/recovery/evidence save-transfer stack remains removed; the replacement is one transport, one service, and one atomic state document.
- The earlier physical run stopped at the startup-cover failure. The redundant post-`NMainMenu` gate has been removed in source; the managed build passes and the retained save/synchronization/mod probe passes 9/9.
- Source inspection confirms the package identity, Android data paths, Steam authentication, depot download, unconditional launch, and local save-store patch remain.
- `StS2Launcher-v0.2.425-mod-chain-fix2-unverified-arm64-v8a.apk` was built and structurally inspected with package `com.sts2launcher.overhaul.fork.local`, version code `425000`, ARM64-only payload, signer SHA-256 `FD0E3D5ACF435C1D23BFC5C426E99AA9EB5808619FF1FC214FFCA99CFAC7E57A`, and APK SHA-256 `2A80E58A6301EFD0C6A0251FF9BC0887434071661DD8E002EF9CA89E25BEDA0B`.
- A limited run on one Samsung ARM64 device showed both selected mods discovered, payload-loaded, initialized, and activated; the modded save namespace was read and the game reached the main menu. The user observed marked improvement and mods appearing to work. A later freeze/crash prevented direct confirmation of the importer control and the launcher relaunch result; the retained filtered log contains no fatal signal for that later report. The device is no longer connected, so Stage 9 remains incomplete.
