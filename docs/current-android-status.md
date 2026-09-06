# Current Android status

Updated: 2026-09-06

StS2 Launcher is an unofficial Android launcher for Steam owners of Slay the Spire 2. It retains Steam authentication, owned-game and Workshop download, ARM64 launch, application-local saves, Steam save synchronization, version selection, renderer recovery, and the validated mod-loading path described in [Android Workshop mods](android-workshop-mods.md).

## Current release: v0.2.431

[Release and APK](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/tag/v0.2.431). Package `com.sts2launcher.overhaul.fork.local`, version code `431000`, ARM64, matching local/tester signing identity. Install over the existing matching app.

133 managed tests, 45 Android tests, 12 save-safety scenarios, both UI profiles, and APK validation passed. The exact APK installed on Samsung SM-F971B / Android 17 and launcher initialization completed. Full game launch is not verified: the selected public-beta branch is currently in an incomplete downloading state. Earlier test results below apply to their named APK, not this release.

See [release notes](release-notes/v0.2.431.md) and [launch refactor](reliable-launch-implementation-2026-09-06.md).

## Previous release validation (v0.2.429)

The previously validated ARM64 tester release was **[v0.2.429 — Issue #38 ARM64 RC4](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/tag/v0.2.429-issue38-arm64-rc4)**.

```text
Asset: StS2Launcher-v0.2.429-issue38-arm64-rc4-arm64-v8a.apk
Package: com.sts2launcher.overhaul.fork.local
VersionName: 0.2.429-issue38-arm64-rc4
VersionCode: 429041
ABI: arm64-v8a
SHA-256: b2d0e8154afc69e11eac4159483e837559e398844444a5d015d18f3fda458181
```

[Download the ARM64-v8a APK](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/download/v0.2.429-issue38-arm64-rc4/StS2Launcher-v0.2.429-issue38-arm64-rc4-arm64-v8a.apk) and install it over the existing application. Android should present an **Update** or **Install** action; ADB users can run `adb install -r <apk-path>`. Do not uninstall or clear application data first.

## Issue #38 runtime and update behavior

Issue #38 was caused by an updated Steam branch being paired with identity data from its previous files. The native bootstrap correctly rejected that mismatched runtime pack.

Current behavior has one authority and one transaction:

1. The final installed, Android-ready `SlayTheSpire2.pck` and source `data_sts2_windows_x86_64/sts2.dll` are hashed from the actual files. Together with the normalized branch and completed install generation, they form `GameIdentity`.
2. A Steam update writes the selected branch's `installation_state.json` as `updating` before replacing installed files. Missing, malformed, mismatched, or `updating` state cannot authorize launch.
3. After Steam files and PCK preparation complete, the launcher calculates a fresh identity. Old runtime-pack manifests, validation reports, branch markers, and active-cache metadata cannot override those bytes.
4. The runtime pack is generated in a unique staging directory, validated against the exact identity and every declared assembly hash, promoted atomically, and validated again at its final path.
5. Android independently hashes the installed PCK and source DLL, validates the final runtime pack, then stages and promotes the active assembly cache.

If recovery is required, **Versions → Redownload Selected Version** removes only the selected branch's downloaded game/runtime state. It preserves saves, Steam credentials, Workshop content, and other downloaded branches. The former bulk **Remove old versions** action has been removed. Uninstalling or clearing all application data is not a routine recovery path.

See [Steam version selection architecture](steam-version-selection-architecture.md) for the concise model and [Issue #38 runtime identity design](issue-38-runtime-identity-design.md) for the full contract.

## Launcher handoff

The launcher overlay now has one owner and each Start Game operation has a unique attempt ID. Main-menu readiness is accepted only for the active attempt, and the overlay is dismissed only after that same attempt is ready while the game activity is foregrounded and focused. Duplicate or late readiness events cannot complete a newer launch.

## RC4 device validation

The exact release APK was installed in place on Samsung `SM-F971B`, Android 17 / API 37, `arm64-v8a`. It passed 10/10 counted launches:

- Four cold launches.
- Four warm launches.
- One background/resume during handoff.
- One lock/unlock during handoff.

Every launch recorded one readiness event, one overlay-hidden event, and one handoff-completed event for its own attempt ID. All ten validated the selected identity, runtime pack, and patch compatibility; displayed the game; left no launcher overlay visible; and recorded no stale completion. The before/after preservation audit also found the local save inventories, Steam Cloud inventory, credentials, selected `public-beta` branch, and unrelated public runtime data unchanged.

This result validates the exact RC4 APK on that device. It does not establish every device, Android version, GPU/driver, Steam branch, Workshop mod, or live Steam save-transfer path. The original Pixel 9 and Xiaomi 17 Ultra reporters have not yet confirmed the fix on their devices.

## Save data

Gameplay saves live in Godot's application-private `user://` storage for package `com.sts2launcher.overhaul.fork.local`. Vanilla and modded namespaces remain separate. Normal in-place updates preserve that data; uninstalling or clearing application data removes the local copy. Do not assume Steam contains a current verified copy unless synchronization has been confirmed.

Selected-branch download, update, validation, promotion, and recovery code does not enumerate or mutate the save root. Startup recovery and support-report creation diagnose launcher state; they are not save-repair tools.

## Known limitations

- ARM64 Android is the intended game target; x86_64 emulator results are native-fallback diagnostics only.
- Device, Android-version, graphics-driver, branch, and mod compatibility varies.
- Private/password Steam branch entry is not implemented.
- RC4's 10/10 result is a focused launcher/runtime validation, not broad gameplay or live-service certification.
- Current-source Android mod activation and the importer effect retain the evidence limits in [Android Workshop mods](android-workshop-mods.md).
