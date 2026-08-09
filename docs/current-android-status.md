# Current Android status

Updated: 2026-08-09

StS2 Launcher is an unofficial Android launcher for Steam owners of Slay the Spire 2. The current source retains code for Steam authentication, owned-game download, and ARM64 launch, but this reduced candidate has not been exercised on Android hardware.

## Current source

The current reduction keeps five product responsibilities:

- Steam authentication and ownership checking.
- Game and Workshop download.
- Game launch, including existing renderer recovery choices.
- Atomic application-local gameplay saving.
- One Steam save-synchronization path shared by automatic and manual actions.

## Save data

Android gameplay writes land first in Godot's `user://` directory inside the private application data for package `com.sts2launcher.overhaul.fork.local`. Current source includes automatic pre-load reconciliation, automatic verified Push during gameplay, and the three manual Saves actions, all through one synchronization service. No real Android Steam transfer has validated this implementation yet. Clearing the app's data or uninstalling the app removes the local copy; do not assume Steam contains a verified copy.

Startup-crash recovery and Help diagnostics remain. They recover launcher startup or collect troubleshooting information; they do not manage gameplay saves.

## Published artifact

The latest published artifact remains `v0.2.416-startup-recovery-ime`. It predates the current reduction and synchronization implementation and must not be treated as proof of current source behavior.

- Package: `com.sts2launcher.overhaul.fork.local`
- Version code: `416001`
- APK SHA-256: `fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b`

Preserving that package identity and signing continuity is required for an in-place update to retain existing app-private data. Update compatibility and signer continuity were not tested for the current candidate.

## Evidence boundary

No Android device is available for this reduction. Current checks cover managed compilation, focused local-save and synchronization behavior, and basic desktop launcher interaction. The fake remote used by the synchronization tests proves neither Steam transport nor Android transport. Desktop interaction cannot prove Android rendering, Steam service behavior, game startup, or gameplay.

Historical device evidence applies only to the exact historical artifact and path recorded with it.

## Known limitations

- ARM64 Android remains the intended game target; x86_64 emulator results are diagnostic only.
- Device, Android-version, and graphics-driver compatibility varies.
- Steam branches and Workshop mods remain experimental.
