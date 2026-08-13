# Android Steam Workshop mods

Workshop support downloads subscribed mod files into app-private Android storage and lets the user select them for a modded launch. Selection and staging do not establish that a mod loaded or activated.

Missing, unreadable, or unrecognized selection state defaults to Vanilla. Modded mode is used only after the player explicitly selects it and enables at least one mod. Workshop and manual roots are validated into the same immutable launch plan and enter the same runtime loader.

## Supported paths

- Steam Workshop subscription download.
- Manual import into the existing launcher mod directory.
- Mod selection and dependency warnings.
- Vanilla launch with no selected mods.
- A modded launch attempt with the selected, validated mod set.

SavesMerger and UnifiedSavePath are deprecated. Current source uses the game's native vanilla and modded application-local save namespaces.

Each launch atomically replaces `last_mod_launch.json`. It records only the launch mode, exact selection fingerprint, timestamp, discovered/loaded/active/partial/failed counts, and one short result per selected mod. During a live load, a missing runtime state is Failed, never Active. If the marker itself is missing, the later launcher UI says `Not run with this setup`. The launcher does not merge, copy, or reinterpret vanilla and modded saves.

The Mods page reads the persisted selection and discovered files as soon as it opens, even when sign-in, download, or mod loading is unavailable. `Vanilla` and `Modded` are selectors. One summary identifies the next save namespace and enabled-mod count. Each mod row contains its name, source, `Enabled` toggle, and one runtime result: `Active last launch`, `Partly loaded`, `Failed last launch`, or `Not run with this setup`. A missing, corrupt, contradictory, or different-fingerprint result is never presented as active. Home shows `Play Vanilla` or `Play Modded · N mods`. The Saves page states that vanilla and modded save sets sync separately and are not merged. Removing downloaded Workshop mods requires confirmation.

## Current limitations

- The offline-confirmed journey is the exact BaseLib plus `ImportVanillaSaves` chain: both validated payloads load and install exact-owner Harmony targets in one fresh desktop Godot process; BaseLib remains `Partial` and the importer is `Active`.
- That desktop result does not prove Android loading or the visible in-game effect on a device.
- The exact offline BaseLib path installs concrete exact-owner Harmony targets but remains `Partial`, because full PatchAll and custom-save extensions are deliberately unavailable. It remains unverified on Android and is not a dependency of `ImportVanillaSaves`.
- Mod compatibility varies by mod, branch, and game build.
- Native-code or desktop-only dependencies may not work.
- A successful Workshop download does not prove that a mod can initialize or run.
- Current-source Android mod activation and the visible importer effect remain unverified on a device.

## Useful evidence

- Exact APK/source identity and selected game branch.
- Workshop item IDs and selected mod order.
- Files present for each mod.
- Dependency or unsupported-item warnings.
- Selected PCK and runtime-pack hashes.
- Focused logs around mod discovery and initialization.
- Whether the game reached the main menu and whether the expected local profile was visible.

Do not attach Steam credentials, tokens, private account data, or private save contents.
