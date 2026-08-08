# Android Troubleshooting

StS2 Launcher is unofficial ARM64 Android tester software. It downloads a Steam owner's copy of Slay the Spire 2; it does not include the game. See [current Android status](current-android-status.md) before assuming a symptom is supported or resolved.

## Identify the APK First

The latest published APK is:

```text
Release: v0.2.416-startup-recovery-ime
Asset: StS2Launcher-v0.2.416-startup-recovery-ime-local-arm64-v8a.apk
Package: com.sts2launcher.overhaul.fork.local
VersionName: 0.2.416-startup-recovery-ime-local
VersionCode: 416001
SHA-256: fdf2dcfcf2352d0e1a370da76922fb5b70cee3654d98c5fe9afbbd39554fc17b
ABI: arm64-v8a
```

Always report the exact tag and filename. Current-source emulator evidence may be newer than this release and must not be treated as published ARM64 evidence.

## Installation Problems

- `INSTALL_PARSE_FAILED_NO_CERTIFICATES`: re-download the APK and verify its SHA-256.
- `INSTALL_FAILED_UPDATE_INCOMPATIBLE`: stop. The candidate package or signer does not match the installed `.local` lineage. Verify both APK identities; never uninstall or clear the published install unless its private save bytes have already been exported and independently read-back verified.
- `INSTALL_FAILED_OLDER_SDK`: the Android version is below the APK's minimum.
- `App isn't compatible with your phone`: confirm the device supports `arm64-v8a`. Public test APKs are ARM64-only.
- Immediate crash or `INSTALL_FAILED_DEXOPT`: capture focused logcat and open an issue.

## Native Diagnostics Screen

On x86_64 this screen is intentional: the emulator cannot safely run the production GodotSharp/Mono path.

On ARM64 it means startup could not prepare or validate the runtime. Select **Show diagnostics**, copy the full report, and attach it before selecting **Restart launcher**. `v0.2.416` clears pending normal and Safe Start state during recovery so Restart should return to the launcher rather than repeat the failed launch.

Do not delete the game, saves, credentials, or assembly cache before collecting the report. The diagnostics should identify the original file operation, storage state, branch, runtime-pack evidence, cache state, and retry result.

## Keyboard Appears Without Text Entry

`v0.2.416` suppresses the hidden Godot editor's unintended IME request during launcher startup and non-editor resume. If the keyboard still appears while no text field was selected, record the device model, OEM skin, exact APK, and whether the app had just resumed, rotated, unlocked, or completed Cloud Pull. Deliberate username, password, Steam Guard, and other editable-field focus must still open the keyboard.

## Black Screen or Long Loading

Cold launch first shows the Godot-to-StS2 Launcher transition. Start Game can later show real shader-warmup progress for 45-90 seconds on a first run; the boot identity must not hide that progress.

Record which surface was last visible. For shader stalls include `last_shader_warmup_status.txt`. For a blank frame include a screenshot or recording, display orientation, reduced-motion state, and whether it happened on cold start, cached start, Home/resume, rotation, or lock-screen return.

## Game Freezes or Exits at the Main Menu

Reaching `NMainMenu` proves startup completed, not that the process remained healthy. Include:

- `last_launch_attempt.txt`
- `last_startup_timeline.txt`
- `last_post_startup_trace.txt`
- `last_post_startup_heartbeat.txt`
- `last_app_lifecycle_event.txt`
- `last_renderer_attempt.txt`
- `last_process_exit_info.txt`

Search focused logcat for `FATAL EXCEPTION`, `AndroidRuntime`, `Fatal signal`, `SIGSEGV`, `SIGABRT`, `ANR`, `lmkd`, `has died`, `PowerVR`, `OpenGL`, and `Vulkan`.

## Cloud Saves

Pull should show its current phase and transfer counts. **Review Upload** should list every reason Upload remains blocked and what action is required. Pull does not upload.

Do not run a real Steam Cloud Push merely to diagnose the interface. Push can overwrite remote saves and requires separate explicit authorisation plus controlled backups. Report whether Pull or Push was selected, whether saves appeared in-game, and the exact blocked or completion summary.

## Evidence Boundaries

| Evidence source | Valid conclusions | Invalid conclusions |
| --- | --- | --- |
| Automated tests | Build, policy, fixtures, deadlines, cache and fake-store safety | Physical rendering, OEM behavior, real Steam services, gameplay |
| API 36 x86_64 emulator | Native routing, fallback/recovery, forced bootstrap failure, rotation, Home/resume, native IME state | Godot/.NET launcher, Steam workflows, ARM64, `NMainMenu`, gameplay |
| Exact `v0.2.416` on Samsung ARM64 | Published artifact, cold transition, launcher, public runtime pack, `NMainMenu`, 60-second heartbeat | Reporter devices, broad compatibility, every branch/mod/GPU, real Push |

Use the [Android validation runbook](runbook-android-validation.md) and [device log checklist](device-log-checklist.md) for a complete report.
