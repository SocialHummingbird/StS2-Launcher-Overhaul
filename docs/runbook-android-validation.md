# Android validation runbook

Updated: 2026-08-09

No device is available for the current reduction. Do not invent device results or treat emulator/desktop results as physical-device proof.

## Current procedure

1. Record the source commit and working-tree state.
2. Build `src/STS2Mobile/STS2Mobile.csproj` in Release configuration.
3. Run `scripts/test-local-gameplay-save-safety.ps1`.
4. Run `scripts/test-launcher-ui-preview.ps1`.
5. If an APK is required, build it with `scripts/build-android-local.ps1` and inspect it with `scripts/verify-android-apk.ps1`.
6. Record exact commands, exit codes, and failures.

The save suite covers local path containment, atomic writes, the four synchronization decisions, transactional Pull failure, retryable Push failure, pre-load ordering, gameplay Push queuing, and shared manual synchronization. Its fake remote is deterministic test infrastructure; it proves neither Steam nor Android transport. The launcher test covers basic navigation and action wiring on desktop only.

## If a device becomes available later

Use the exact candidate APK and record its package, version, signer, SHA-256, device, Android version, ABI, GPU, install type, and selected branch. Exercise only the paths explicitly authorized for that candidate.

Do not clear or uninstall an affected app before preserving required data. Keep any device journey narrow, identify the exact APK, and do not claim broad device support from one device.
