# Workshop mods on Android

The launcher can download your subscribed Steam Workshop mods and let you choose which ones to use. Downloading a mod does not guarantee it will work on Android: some depend on desktop code or a different game version.

## Use mods

1. Subscribe to the mods you want on Steam.
2. Open **Mods** in the launcher and sync your Workshop content.
3. Choose **Modded**, enable the mods you want, and check any dependency warnings.
4. Return to Home and start the game.

Choose **Vanilla** to play without mods. If the launcher cannot read your saved selection, it defaults to Vanilla. Manual mod files placed in the launcher's mod directory use the same checks as Workshop files.

## Read the mod status

| Status | What it means |
| --- | --- |
| Active last launch | The mod reported activation on the last launch with this setup. |
| Partly loaded | Some loading steps worked, but the mod was not fully active. |
| Failed last launch | The mod failed to load or activate. |
| Not run with this setup | There is no matching result for your current selection. |

An Enabled switch selects a mod for the next launch. It does not mean the mod is already active or that every feature works.

## Saves

Vanilla and modded games use separate save sets. Switching modes does not copy or merge them. The old SavesMerger and UnifiedSavePath approach is deprecated.

If a profile seems to disappear after changing modes, check whether you are looking at the other save set before changing anything else.

## If a mod causes trouble

Try Vanilla first. If that works, re-enable mods one at a time and check their game-version requirements. For a report, include mod names or Workshop links, the selected game branch, and what failed. Use **Help → Report a bug on GitHub** to include the launcher diagnostics.

## What has been tested

Desktop tests cover mod selection and a BaseLib/ImportVanillaSaves setup. In that setup, BaseLib reports partial loading and the importer reports active. These results do not establish Android compatibility or prove the importer's in-game effect on a phone.

See [current Android testing status](current-android-status.md) for device results.
