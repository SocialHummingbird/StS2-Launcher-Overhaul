# Troubleshooting

Start by checking that you have the [latest launcher release](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/latest). Update over the existing app; **uninstalling or clearing app data removes local saves**.

## Android won't install the APK

- **Not compatible with your phone:** the published APK requires ARM64 (`arm64-v8a`).
- **Update incompatible:** the APK and installed app have different package names or signing keys. Report the error rather than uninstalling to get around it.
- **No certificates / invalid package:** download the APK again. The release includes a SHA-256 checksum if you want to check the file.
- **Older SDK / Android version too old:** your Android version is below the APK's minimum.
- **Install permission needed:** allow installations from the browser, file manager, or launcher that opened the APK, then return to the installer.

See [Getting started](getting-started.md) for the normal update steps.

## The game download or update is stuck

Check your connection and the message on **Versions**. The game cannot launch until its selected download and Android preparation have finished.

If the launcher offers automatic preparation, let it finish. If it requires **Redownload selected version**, that replaces only the chosen branch's game files; saves, login details, mods, and other branches stay in place.

If the same error repeats, create a bug report before trying another redownload. Include the branch and whether this was a first download or an update.

## Black screen, loading hang, or launcher covering the game

Try launching Vanilla to rule out selected mods. If normal startup stalls, try **Safe Start**, which skips shader warmup. Auto, Vulkan, and OpenGL graphics options are also available, although a device-specific compatibility rule may restrict them.

If preparation says it is waiting for file operations to finish, let it settle before another attempt. It keeps launch controls blocked so two preparation jobs cannot change the same files at once.

For a bug report, describe the last screen you saw and whether the app closed, stopped responding, or kept running behind the launcher. Mention if you switched apps, rotated, or locked the screen while it loaded. A screenshot or short recording helps.

## A native diagnostics screen appears

On ARM64, this means the app could not prepare or start the game runtime. Save its support report and include it in your issue. On x86_64 emulators, this fallback is expected; they are not the supported game target.

## Music or sound effects don't work

The fix for missing music and delayed effects is included in v0.2.430 and later. The original reporter confirmed it worked in [issue #37](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/issues/37#issuecomment-5461909959).

If it still happens on your device, report your app version, device, game branch, and whether you use mods. Mention whether sounds never play or arrive late.

## Saves are missing or out of date

Check the **Saves** page and whether you are playing Vanilla or Modded. Those modes use different save sets. Check the sync result before assuming Steam has your latest Android save.

If both Android and Steam changed, choose carefully when asked which copy to keep. Don't overwrite either copy just to see whether it fixes the problem. Include the sync message in your report.

## The keyboard opens by itself

Report whether this happened after starting, resuming, rotating, or unlocking the app. Include a screenshot if possible. Selecting a login or Steam Guard field should still open the keyboard normally.

## Send a useful bug report

Open **Help → Report a bug on GitHub**. The launcher fills in redacted diagnostic information. Add:

- What you were trying to do and the steps that caused the problem.
- What happened instead, including the exact error message.
- Your device and Android version, if the report does not already include them.
- The selected game version and any enabled mods.

Review the text before submitting. Never include passwords, Steam Guard codes, or login tokens. If the prefilled report is too short, use **Create support report** in Help to save a fuller report. Review raw logs before attaching them.

The [testing status page](current-android-status.md) explains which builds and devices have been checked. Developers can find deeper diagnostic steps in the [validation runbook](runbook-android-validation.md).
