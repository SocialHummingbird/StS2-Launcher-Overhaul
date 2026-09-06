# Getting started

You need an ARM64 Android device, internet access for setup, and a Steam account that owns Slay the Spire 2. The game is downloaded from Steam after you sign in; it is not included in the APK.

## Install and download the game

1. Open the [latest release](https://github.com/SocialHummingbird/StS2-Launcher-Overhaul/releases/latest) and download the APK ending in `-arm64-v8a.apk`.
2. Open the APK. If Android asks, allow the browser or file manager to install apps from this source, then return to the installer.
3. Launch the app and sign in to Steam. Complete Steam Guard if asked.
4. Select the regular public game branch and download it. Other available branches are listed under **Versions**.
5. Start in **Vanilla** mode and press **Play**. Add mods after the base game works.

The first download needs space for the game and its Android preparation files. Each additional game branch uses more storage.

## Find your way around

| Page | Use it for |
| --- | --- |
| Home | Start the game and see what needs attention. |
| Saves | Check Steam sync or choose a manual transfer. |
| Versions | Download, switch, update, or repair a game branch. |
| Mods | Choose Vanilla or Modded play and enable mods. |
| Help | Report a bug, create a support report, or check for launcher updates. |

## Update the launcher

The launcher checks for new releases after opening. Choose **Update** when prompted, or use **Help → Check for app updates**. It downloads and checks the APK before opening Android's installer. Android may ask you to allow installations from the launcher.

You can also download the APK from GitHub and install it over the current app. Normal updates keep saves, login details, mods, and downloaded games. **Do not uninstall or clear app data first.**

The current APK uses package `com.sts2launcher.overhaul.fork.local`. Android will reject an update signed differently from your installed copy. If that happens, use the [troubleshooting guide](android-troubleshooting.md); uninstalling to get past the error would remove local saves.

Launcher updates and game updates are separate. Use **Versions** to update the game itself.

## Use Steam saves

The launcher checks Steam saves before the game loads and queues uploads after gameplay saves. Check the Saves page before switching devices, especially after playing offline.

- **Sync now** checks both copies.
- **Get saves from Steam** downloads the Steam copy.
- **Send saves to Steam** uploads the Android copy.

If both copies changed, the app asks which one to keep. Read that choice carefully. A network failure does not mean your local save was lost, and it does not mean Steam has received it.

Vanilla and modded saves are separate. Changing modes or game versions does not merge them.

## If something goes wrong

Use **Help → Report a bug on GitHub**. The app fills in redacted diagnostics; add the steps that caused the problem and review the text before posting. For common fixes, see [Troubleshooting](android-troubleshooting.md).
