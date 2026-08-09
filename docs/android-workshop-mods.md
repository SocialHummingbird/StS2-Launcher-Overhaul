# Android Steam Workshop mods

Workshop support downloads subscribed mod files into app-private Android storage and lets the user select them for a modded launch.

## Supported paths

- Steam Workshop subscription download.
- Manual import into the existing launcher mod directory.
- Mod selection and dependency warnings.
- Vanilla launch with no selected mods.
- Modded launch with the selected mod set.

SavesMerger and UnifiedSavePath are deprecated. Current source uses the game's native vanilla and modded application-local save namespaces.

## Current limitations

- Mod compatibility varies by mod, branch, and game build.
- BaseLib support is partial on Android.
- Native-code or desktop-only dependencies may not work.
- A successful Workshop download does not prove that a mod can initialize or run.
- No Android device is available for current-source validation.

## Useful evidence

- Exact APK/source identity and selected game branch.
- Workshop item IDs and selected mod order.
- Files present for each mod.
- Dependency or unsupported-item warnings.
- Selected PCK and runtime-pack hashes.
- Focused logs around mod discovery and initialization.
- Whether the game reached the main menu and whether the expected local profile was visible.

Do not attach Steam credentials, tokens, private account data, or private save contents.
