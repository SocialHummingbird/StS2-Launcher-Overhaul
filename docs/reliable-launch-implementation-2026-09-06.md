# Reliable launch implementation

Implements the approved [launch cycle review](launch-cycle-review-2026-09-06.md). Existing game identity, install-generation, runtime-pack authorization, save ordering, and foreground/focus checks remain authoritative.

## Behavior

- Each handoff attempt owns one game startup task. A timeout fails the attempt and observes late completion/failure; it cannot start a competing main-menu load. Retrying after game initialization has started uses the existing one-shot signal to require a fresh process.
- Main-menu readiness is captured by a dedicated critical Harmony patch at scene construction. Readiness must belong to the originating operation and game, match the current live scene, and survive a rendered frame before the visibility owner dismisses the launcher.
- File and mod preparation run asynchronously after the loading status paints. A modal interaction lock prevents another launch, version change, repair, or mod change during preparation. If a deadline expires, the attempt stays blocked until its uncancellable file work settles; a late result never automatically launches the game.
- Startup/frame waits use attempt and node lifetime, with foreground time budgets paused while the application is suspended. Save-sync cleanup has a separate bounded wait; stalled cleanup stops launch rather than bypassing a save writer.
- Android restarts use one versioned durable request carrying attempt, branch, safe mode, game identity, runtime pack, and install generation. Native code persists the request and starts the target before acknowledging acceptance. Managed code records that acceptance, then explicitly finishes the old process. The old process cannot claim its own request; the next process claims/consumes it once and validates its identity again.
- Removed the force-load watchdog paths, unused workshop safety helper and previous-stall inference, forwarding-only startup layer, and warmup finish delay. The intentional Android post-startup lifetime anchor remains on the sole successful path.

## Regression coverage

New cases cover duplicate startup, timeout followed by late success/fault, immutable scene provenance, same-ID operation replacement, restart rejection/persistence/intent failures and consumption, stalled save cleanup, and preparation timeout draining. Existing identity, interrupted-install, lifecycle, visibility, and gameplay-save probes remain part of validation.

Actual Harmony hooks, Android activity/process transitions, and gameplay require device validation in addition to the managed and Java contracts. Device results are recorded after installing the local build; no file authorization is bypassed for testing.

## Validation results

- Managed suite: **132 passed, 0 failed**.
- Android unit suite: **34 passed, 0 failed**, including seven restart-store cases.
- Gameplay/save safety probe: **12/12 passed**. Its fake remote covers ordering, not real Steam Cloud transport.
- Godot launcher interaction/layout checks: passed at **412 × 915** and **1280 × 800**.
- Release arm64 APK build, content/ABI verification, and Android crypto validation: passed.
- Independent source reviews found and resolved restart acknowledgement and preparation-drain races; final restart/scene review reported no additional P1 finding.
- Installed `0.2.430-reliable-launch-local` (**430042**) in place on Samsung `R3GL70DKNKB`; launcher opened and package version was verified. Existing application data was retained.

**Device limitation:** the existing public-beta PCK is rejected before Play because its managed FMOD entries do not match supported Android preparation. The new diagnostic log identifies this separately from successful game-identity checks. Therefore normal/safe game loading, actual Harmony scene callbacks, and restart-to-game lifecycle have **not** been validated on this device with this build. Resolving that existing installation prerequisite is required before those checks can complete. No game files were deleted or authorization checks bypassed.

Evidence is under `artifacts/android/device-test-20260906/`: `reliable-managed-tests.log`, `reliable-java-tests.log`, `reliable-save-safety.log`, `reliable-ui-phone.log`, `reliable-ui-desktop.log`, `reliable-apk-build.log`, `reliable-device-app-logcat.txt`, and `reliable-installed.png`.
