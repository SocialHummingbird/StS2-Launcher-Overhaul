# Android device validation evidence - 2026-06-08

This record summarizes the current ARM64 phone validation pass for StS2 Launcher release-readiness hardening.

## Device and packages

- Device serial: `RFCY70XQE7F`
- Device model: Samsung `SM_F966B`
- ABI: `arm64-v8a`
- Public release package: `com.sts2launcher.overhaul.fork.dev`
- Local validation package: `com.sts2launcher.overhaul.fork.local`
- Latest public release validated: `v0.2.177-login-a8729d6`
- Public release APK: `StS2Launcher-v0.2.177-login-a8729d6-arm64-v8a.apk`
- Public release SHA-256: `bde43591aeb6904488560bb1e27421276cc3248bbc7d2eb9151e29b8b9fef199`

## Validated passes

### Published APK verification

Status: pass.

Evidence:

- `artifacts/android/github-release-v0.2.177-login-a8729d6`
- `artifacts/android/phone-diagnostics-20260608-220359`

Observed result:

- Release digest matched the GitHub asset digest.
- APK structural verifier passed for Android crypto patches and `arm64-v8a`.
- Installed package reported `versionName=0.2.177-login-a8729d6`.
- Installed package reported `versionCode=217700`.

### Public release upgrade install

Status: pass.

Evidence:

- `artifacts/android/github-release-v0.2.175-refactor-apk`
- `artifacts/android/github-release-v0.2.177-login-a8729d6`
- `artifacts/android/phone-diagnostics-20260608-220012`
- `artifacts/android/phone-diagnostics-20260608-220359`

Observed result:

- `v0.2.175-refactor-apk` installed first with `versionCode=217500`.
- `v0.2.177-login-a8729d6` installed over it with `versionCode=217700`.
- `firstInstallTime` was preserved across the upgrade.
- `lastUpdateTime` changed on the `v0.2.177` install.
- No uninstall or data clear was used for the public upgrade step.

### Locked-screen interruption

Status: pass.

Evidence:

- `artifacts/android/lock-unlock-validation-20260608-215548`

Observed result:

- Manual unlock returned focus to `com.sts2launcher.overhaul.fork.local/com.game.sts2launcher.GodotApp`.
- The app process stayed alive.
- No app-specific crash marker was found.
- Broad logcat still contained unrelated Samsung/system crash noise, so app-specific filtering is required.

### Pull from Cloud

Status: pass.

Evidence:

- `artifacts/android/local-pull-smoke-20260608-221143`

Observed result:

- Steam Cloud files were enumerated.
- Steam Cloud files were downloaded.
- Android local save writes occurred under the local app package.
- Pull completed with `103 downloaded, 58 not in cloud`.
- The launcher console showed `Pull complete. (22:13:24)`.

### Game launch and profile visibility

Status: pass.

Evidence:

- `artifacts/android/local-start-game-dpad-20260608`
- `artifacts/android/local-game-profile-center-20260608`

Observed result:

- DPAD navigation from the launcher activated Start Game.
- Slay the Spire 2 launched.
- The in-game screen showed `Profile 1`.
- The early-access dialog appeared normally.
- The app stayed foreground.
- No app-specific AndroidRuntime fatal crash was observed after launch.

### Restart diagnostics

Status: pass with noisy-log caveat.

Evidence:

- `artifacts/android/local-restart-diagnostics-20260608`

Observed result:

- Force-stop followed by package launch returned to the launcher.
- The launcher showed saved Steam credentials with `Welcome back, purcho`.
- `Game Cloud Sync: OFF` was visible.
- The app stayed foreground and alive.
- The initial broad fatal detector matched ADB monkey runtime startup and the Godot plugin name `AndroidRuntime`, not an app crash.

### Cloud-sync/status UX

Status: partially pass.

Evidence:

- `artifacts/android/local-pull-smoke-20260608-221011`
- `artifacts/android/local-pull-smoke-20260608-221143`
- `artifacts/android/local-restart-diagnostics-20260608`

Observed result:

- `Game Cloud Sync: OFF` state is visible on launcher.
- OFF wording explains that the game uses Android local saves while manual Push/Pull remains available.
- Pull completion appears in the console after a successful Pull.
- Restart screen shows saved credentials and cloud-sync state.

Remaining UX gap:

- Latest Push warning wording was not validated on the installed `.local` app because `.local` cannot be updated in-place with the recreated test keystore.

## Blockers and unproven requirements

### Confirmed Push-to-Cloud overwrite and round-trip Pull

Status: blocked by safety and controllability.

Reason:

- Confirmed Push can overwrite real Steam Cloud state.
- `run-as` is unavailable for `com.sts2launcher.overhaul.fork.local`, so private Android save files cannot be safely inspected or mutated directly.
- The installed `.local` app is signed with the original local test key.
- The original local test key is unavailable.
- The recreated `tmp/localtest.keystore` has a different certificate.
- Installing the latest `.local` APK over the existing `.local` app fails with `INSTALL_FAILED_UPDATE_INCOMPATIBLE`.
- Uninstalling `.local` would discard useful saved login/app state unless explicitly accepted.

Required evidence before this can pass:

- A controlled local save mutation that is known before upload.
- A confirmed Push that uploads only the controlled mutation.
- Steam Cloud evidence that the expected file/metadata changed.
- A follow-up Pull proving the pushed state round-trips back to Android local storage.
- Cancel/no-confirm evidence on the latest warning build proving no upload markers occur.

Safe ways to unblock:

- Restore the original local signing key and update `.local` in place.
- Use a disposable Steam/test account and disposable cloud state.
- Intentionally uninstall/reset `.local` after accepting loss of current local app data.
- Add a debuggable/safe test package path that can inspect private storage without touching the current `.local` state.

### Local in-place stale-cache/freshness upgrade coverage

Status: incomplete.

Reason:

- Public package upgrade from `v0.2.175` to `v0.2.177` passed.
- Local package in-place upgrade coverage is blocked by the `.local` signing mismatch.
- Static freshness/cache logs exist, but local in-place runtime proof still needs a same-signature update.

## Evidence hygiene

Keep these current evidence folders:

- `artifacts/android/device-goal-baseline-20260608-215110`
- `artifacts/android/github-release-v0.2.177-login-a8729d6`
- `artifacts/android/phone-diagnostics-20260608-220359`
- `artifacts/android/lock-unlock-validation-20260608-215548`
- `artifacts/android/local-pull-smoke-20260608-221143`
- `artifacts/android/local-start-game-dpad-20260608`
- `artifacts/android/local-game-profile-center-20260608`
- `artifacts/android/local-restart-diagnostics-20260608`

Historical release caches and failed tap-coordinate attempts can be archived or deleted after the current validation record is committed and no longer needed for audit context.

## Release-readiness conclusion

The ARM64 Android path works through published APK verification, public upgrade install, locked-screen return, Pull from Cloud, local save handoff, game launch/profile visibility, and restart diagnostics.

The release is still not fully signed off because confirmed Push-to-Cloud overwrite behavior and round-trip Pull remain unvalidated by controlled evidence.
