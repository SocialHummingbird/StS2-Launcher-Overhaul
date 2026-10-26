# Workshop mods on Android

The launcher downloads complete Steam Workshop packages and lets you choose which ones to use. Packages load through the game's original mod framework without launcher-specific edits. Use mods made for the game version you have selected.

## Use mods

1. Subscribe to the mods you want on Steam.
2. Open **Mods** in the launcher and choose **Update Workshop mods**.
3. Choose **Modded**, enable the mods you want, and check any dependency warnings.
4. Return to Home and start the game.

Choose **Vanilla** to play without mods. If the launcher cannot read your saved selection, it defaults to Vanilla. Manual mod packages are discovered under `/storage/emulated/0/StS2Launcher/Mods` and are loaded by the same game mod loader as Workshop packages.

## Read the mod status

| Status | What it means |
| --- | --- |
| Loaded last launch | The game reported the mod loaded without recorded errors. |
| Loaded with errors last launch | The game reported a loaded mod, but also recorded errors. |
| Failed last launch | The game reported a load failure, or the selected files could not be prepared. |
| Skipped last launch | The game skipped the mod. Check its loading details for the reason. |
| Not run last launch | The selected package has no mod loading result from that launch. |
| Not run with this setup | There is no completed result matching the current launcher, game, mod files, and selection. |

An Enabled switch selects a mod for the next launch. A Loaded result describes what the game reported during loading; check in game that the features you need work.

## Saves

The game normally keeps vanilla and modded progress in separate folders. Game versions with first-time modded save copying can copy vanilla progress into a new modded save set without overwriting existing modded saves. After that initial copy, the two sets develop separately; the launcher does not keep them merged.

The game decides which save set to use after loading mods. Check the save label on Home or Mods after a launch. Modded mode with no enabled mods, or with all mods skipped, can still use vanilla saves.

If a profile seems to disappear after changing modes, check whether you are looking at the other save set before changing anything else.

## If a mod causes trouble

Try Vanilla first. If that works, re-enable mods one at a time and check their game-version requirements. For a report, include mod names or Workshop links, the selected game branch, and what failed. Use **Help → Report a bug on GitHub** to include the launcher diagnostics.

## What has been tested

The desktop harness checks mod selection, the game's original mod loading, resources, C# mod scripts, and saved loading results. Its BaseLib/ImportVanillaSaves scenario requires the game to report both mods as Loaded without errors. Desktop checks do not establish Android compatibility or prove the importer's in-game effect on a phone.

See [current Android testing status](current-android-status.md) for device results.
