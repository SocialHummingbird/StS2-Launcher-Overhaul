# Android troubleshooting

StS2 Launcher is unofficial ARM64 Android tester software. It downloads a Steam owner's copy of Slay the Spire 2; it does not include the game. Check [current Android status](current-android-status.md) before assuming a symptom is supported or resolved.

## Identify the APK first

The latest ARM64 release is [v0.2.429 — Issue #38 ARM64 RC4](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/tag/v0.2.429-issue38-arm64-rc4).

```text
Asset: StS2Launcher-v0.2.429-issue38-arm64-rc4-arm64-v8a.apk
Package: com.sts2launcher.overhaul.fork.local
VersionName: 0.2.429-issue38-arm64-rc4
VersionCode: 429041
SHA-256: b2d0e8154afc69e11eac4159483e837559e398844444a5d015d18f3fda458181
ABI: arm64-v8a
```

Always report the exact release tag, APK filename, version code, device model, Android version, and selected Steam branch.

## Installation problems

- Install RC4 over the existing application. Open the APK and choose **Update**, or use `adb install -r <apk-path>`.
- `INSTALL_PARSE_FAILED_NO_CERTIFICATES`: re-download the APK and verify its SHA-256.
- `INSTALL_FAILED_UPDATE_INCOMPATIBLE`: stop. The APK package or signer differs from the installed application. Record both identities and open an issue; do not uninstall or clear data to bypass the check.
- `INSTALL_FAILED_OLDER_SDK`: the Android version is below the APK minimum.
- `App isn't compatible with your phone`: confirm the device supports `arm64-v8a`. The release APK is ARM64-only.

An in-place update is the normal path. Reinstalling from scratch or clearing application data is not routine recovery and removes application-private local saves.

## Native diagnostics screen

On x86_64, native fallback is intentional because the emulator cannot run the production GodotSharp/Mono game path. On ARM64, the screen means startup could not validate or prepare the selected runtime. Create and share the diagnostics report before restarting or repairing the selected version. Do not delete the game, saves, credentials, or assembly cache before collecting evidence.

## Branch update or runtime-pack failure

The final installed `SlayTheSpire2.pck` and source `sts2.dll` are authoritative. The launcher should reject stale manifests or runtime packs instead of launching mixed files.

If a selected branch is stuck in `updating`, reports an identity/runtime-pack mismatch, or repeatedly routes to native fallback:

1. Open **Help** and create a support report before deleting anything.
2. Record the selected branch, installation-state status/transaction, `GameIdentity` ID, PCK hash, source DLL hash, runtime-pack ID, patched DLL hash, and first readiness failure shown in the report.
3. Open **Versions** and use **Redownload Selected Version** once.
4. If the repair fails again, attach the new support report and the exact error.

Selected-version recovery targets only that branch's downloaded game, download state, runtime pack, and matching derived cache. It preserves local saves, Steam credentials, Workshop content, and other installed branches. There is no current bulk **Remove old versions** action. Do not clear every branch or all application data.

## Keyboard appears without text entry

The launcher suppresses unintended keyboard requests from Godot's hidden editor during startup and non-editor resume. If the keyboard appears without selecting a field, record the exact APK/device, whether the app had just resumed, rotated, or unlocked, and a screenshot. Username, password, Steam Guard, and other deliberately selected text fields should still open the keyboard.

## Launcher remains visible over the game

RC4 binds launcher handoff to one launch-attempt ID. The active attempt must report main-menu readiness while the game activity is foregrounded and focused before its overlay can be dismissed; duplicate and late events are ignored.

RC4 passed 10/10 counted device launches, including background/resume and lock/unlock, with one overlay dismissal and one completion per attempt. If the launcher still covers a visible game, include:

- The launch-attempt ID and ordered handoff events.
- Whether `main_menu_ready`, `overlay_hidden`, and `handoff_completed` occurred for that same attempt.
- Whether Android backgrounded, locked, rotated, or changed focus during handoff.
- A screenshot or short recording showing the foreground surface.
- Focused logcat around `LauncherHandoff`, `NMainMenu`, `AndroidRuntime`, and `Native lifecycle event`.

## Black screen, hang, or process exit

Record the last visible surface and whether the launcher overlay was present. For shader stalls include `last_shader_warmup_status.txt`; for startup/process failures include the support report plus these files when available:

- `last_launch_attempt.txt`
- `last_startup_timeline.txt`
- `last_post_startup_trace.txt`
- `last_post_startup_heartbeat.txt`
- `last_app_lifecycle_event.txt`
- `last_renderer_attempt.txt`
- `last_process_exit_info.txt`

Focused logcat terms include `FATAL EXCEPTION`, `AndroidRuntime`, `Fatal signal`, `SIGSEGV`, `SIGABRT`, `ANR`, `lmkd`, `has died`, `PowerVR`, `OpenGL`, and `Vulkan`. Do not label a hang as a crash without process-exit evidence.

## Collect diagnostics safely

In the launcher, open **Help → Diagnostics → Create support report**. Android saves the report and opens the share sheet. If startup is already in progress, use **Create Startup Help Report** on the recovery controls. **Copy Launcher Log** is a rawer fallback and must be reviewed before public posting.

Before sharing any report or log:

- Remove Steam usernames/account identifiers, email addresses, filesystem details you consider private, and private save contents.
- Never include a Steam password, Steam Guard code, refresh token, session token, QR/login payload, or full unsanitized log.
- Prefer the generated support report and a focused logcat window over an entire device log.
- Do not clear credentials, uninstall, or erase game data merely to produce diagnostics.

## What to include in a bug report

- Exact release tag, APK filename, package, version name/code, and whether installation was an in-place update.
- Device model, Android/API version, ABI, GPU/renderer, and relevant lifecycle event such as background, rotation, or lock.
- Selected Steam branch and whether this was a download, update, redownload, or launch.
- Reproduction steps, expected result, actual result, and the last visible screen.
- `GameIdentity`/runtime-pack evidence for branch or startup failures.
- Launch-attempt/handoff evidence for overlay or main-menu readiness failures.
- Generated support report, focused logs, and screenshots after credential/privacy review.
- Whether local saves and other downloaded branches remained present.

Use the focused GitHub issue template that matches the failure.

## Local saves

If a profile is missing, stop destructive recovery. Record whether it is vanilla or modded, the selected branch, sync status, package/signing identity, and whether Android restored or cleared the application. Do not uninstall or clear the affected app while investigating.

## Evidence boundary

RC4's 10/10 Samsung ARM64 result validates the exact artifact and tested launch matrix. It does not prove every device, branch, GPU, mod, or live Steam transfer. Desktop and fake-backed tests remain local-policy evidence only. See the [Android validation runbook](runbook-android-validation.md).
