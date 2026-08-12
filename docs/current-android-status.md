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

The current public test candidate is the prerelease `v0.2.425-mod-chain-fix2-unverified`. It is not a stable or phone-ready release.

- Package: `com.sts2launcher.overhaul.fork.local`
- Version code: `425000`
- APK SHA-256: `2A80E58A6301EFD0C6A0251FF9BC0887434071661DD8E002EF9CA89E25BEDA0B`

The asset is `StS2Launcher-v0.2.425-mod-chain-fix2-unverified-arm64-v8a.apk`. Its package and signer match the prior local-test lineage and its archive contains only `arm64-v8a`.

Preserving that package identity and signing continuity is required for an in-place update to retain existing app-private data.

## Evidence boundary

No Android device is currently connected. In the retained one-device run, both exact selected mods were discovered, payload-loaded, initialized, and activated; BaseLib remained honestly `Partial`, a real `modded/` save path was used, and the game reached the main menu. The user reported marked improvement and that mods appeared to work. The run did not directly capture the importer control or a matching relaunch result, and a later freeze/crash was reported after the retained filtered log ended. It therefore does not complete Stage 9 or prove broad Android mod compatibility. Real Android Steam transfer also remains unproven.

Historical device evidence applies only to the exact historical artifact and path recorded with it.

## Known limitations

- ARM64 Android remains the intended game target; x86_64 emulator results are diagnostic only.
- Device, Android-version, and graphics-driver compatibility varies.
- Steam branches and Workshop mods remain experimental.
- The exact one-device BaseLib plus ImportVanillaSaves journey remains mandatory before any Android mod-loading success claim.
