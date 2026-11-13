# Game versions

The Versions page lets you keep separate copies of the Steam branches available to your account, such as the regular public release and a public beta.

## Download or switch a version

1. Sign in to Steam.
2. Open **Versions** and refresh the list.
3. Select a branch, then download or update it.
4. Return to **Home** and press Play.

Password-protected branches are not supported. A branch listed by Steam may still be unavailable to your account.

## Update a game version

Only the selected branch is updated. Other downloaded branches stay installed.

Updates reuse verified content from installed versions and unfinished downloads, then download the missing chunks. Replacement files need temporary storage alongside the installed files.

During a transfer, **Pause download** stops new requests and waits for active requests to finish. **Resume Download** keeps the verified progress, including after restarting the app. Retry after a connection failure also keeps it. The progress display separates reused and resumed content from downloaded compressed payload.

The launcher blocks Play while files are being replaced or prepared for Android. Resume an interrupted download before trying to launch that version. This avoids starting with a mix of old and new files.

This updates the game, not the launcher APK. For app updates, use **Help → Check for app updates**.

## Repair a version

Let the launcher complete any automatic Android preparation it offers. If it asks for a redownload, **Redownload selected version** replaces that branch's downloaded game files.

It keeps your saves, Steam login, Workshop content, and other game branches. This explicit reset also removes the selected branch's unfinished download, so try Resume or Retry first. Content in another installed version can still be reused after the reset.

If the same error returns, use **Help → Report a bug on GitHub** and mention which branch you chose and whether you had just updated or repaired it.

## Saves and mods

Downloaded game branches use the same save folders. Switching versions does not create a separate copy of your progress, and a save or mod made for one game version may not work with another.

Vanilla and modded progress normally use separate folders. The game can make an initial copy of vanilla progress for a new modded save set while protecting existing modded saves; it does not keep the sets merged afterward. See [Workshop mods and saves](android-workshop-mods.md#saves).

For the file checks behind version selection, see the [technical guide](steam-version-selection-architecture.md).
