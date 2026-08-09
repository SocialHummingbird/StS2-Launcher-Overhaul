# Overhaul status

Updated: 2026-08-09

## Current focus

Keep the launcher on the smallest useful Android path:

1. Keep the existing package identity and Android data paths.
2. Keep Steam authentication and owned-game download.
3. Keep unconditional game launch.
4. Keep one application-local gameplay save store.
5. Keep one verified-result Steam transport and one synchronization service.
6. Keep only automatic synchronization plus three manual Saves actions; omit recovery, backup, export, and safety bureaucracy.

## Current state

- The reduction starts from public `main`, not the abandoned idle-suspension branch.
- Gameplay writes commit atomically to the application files directory before synchronization is notified.
- Automatic pre-load reconciliation, gameplay Push, and the three manual Saves actions share one service and transport.
- Steam authentication and depot download remain product features.
- Cloud failure cannot create a launch loop or block the unconditional local launch fallback.
- Startup-crash recovery and Help diagnostics remain because they are unrelated to gameplay-save management.
- An earlier `0.2.420` local-only candidate was installed in place on one Samsung SM-F971B. That historical run does not validate the restored synchronization implementation.

## Release boundary

Current source is not a new public release. Historical device results and release notes do not validate the restored synchronization implementation. Do not claim working Android Steam transfer, phone readiness, or release readiness until the deferred one-device round trip passes.

## Reduction verification

- The old coordinator/cache/recovery/evidence save-transfer stack remains removed; the replacement is one transport, one service, and one four-field atomic state document.
- The earlier physical run stopped at the startup-cover failure. The redundant post-`NMainMenu` gate has been removed in source; the managed build passes and the retained save/synchronization probe passes 8/8.
- Source inspection confirms the package identity, Android data paths, Steam authentication, depot download, unconditional launch, and local save-store patch remain.
- `StS2Launcher-v0.2.421-auto-sync-unverified-arm64-v8a.apk` was built and structurally inspected offline with package `com.sts2launcher.overhaul.fork.local`, version code `421000`, ARM64-only payload, and the expected local signer.
- No device is currently available. Automatic PC-to-Android Pull, in-game Android Push with verified read-back, the Android-to-PC return trip, and live manual Pull/Push remain mandatory and unproven. The candidate must not be published or called phone-ready.
