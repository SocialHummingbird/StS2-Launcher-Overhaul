# Steam Version Selection Architecture

This note documents the intended architecture for Steam game version selection in StS2 Launcher. It is the implementation contract used by validation, diagnostics, and release readiness.

## Design goals

- Keep the default/public Steam branch compatible with the historical install path.
- Allow non-public branches to coexist without overwriting the public install cache.
- Make startup use the selected branch, not whichever PCK happens to exist.
- Require provenance before launching non-public cached files.
- Make destructive save/cloud actions explicit after branch switches.
- Keep unsupported Steam branch features visible until they are implemented or intentionally rejected.

## Branch model

The launcher currently supports this selector mode:

```text
Steam branch dropdown
```

Supported branch IDs:

- `public`
- `beta`

Supported selector capabilities:

- Dropdown-first game version selection.
- Dropdown options refresh from captured Steam app-info visible branch evidence after download/update app-info has been captured.
- Dropdown labels stay concise so the selector remains usable on phone screens.
- Selected-version helper text surfaces availability metadata from Steam app-info when available, including downloadability, password flag, build ID, and description.
- The launcher exposes a non-mutating `REFRESH GAME VERSIONS` action that fetches Steam app-info branch metadata and updates the dropdown without downloading or deleting game files.
- `public` remains available even when Steam app-info branch evidence is absent.
- Previously saved custom branch values remain selectable so older validation installs do not lose their selected branch.

Unsupported capabilities:

- Steam beta password entry.

Diagnostics and user-facing docs must continue to expose these limitations while they remain true.

Managed launcher guidance comes from `SteamGameBranch.SelectorHelpText`, with launcher UI using `SteamGameBranch.SelectorInstallSlotHelpText` so the active install slot is visible beside the branch limitation wording. Native Android routing/fallback guidance comes from `SteamBranchInfo.selectorHelpText`; keep the limitation strings semantically aligned until the native layer can consume the managed source directly. Managed and native branch cache paths must also stay aligned: `SteamGameBranch.StateDirectoryName`, managed startup routing, and `SteamBranchInfo.stateDirectoryName` must preserve `public`/`beta` paths and use the same collision-resistant hash rule for future arbitrary branch IDs.

Launcher selector guidance should be shown as wrapped, non-interactive helper text under the game-version dropdown so it remains readable on short/wide Android screens without intercepting taps. The normal user path must stay dropdown-first; free-form branch entry is not acceptable as the release UX because Steam exposes branch selection as a list.

## Storage model

The public branch keeps the legacy paths for upgrade compatibility:

```text
files/game/
files/download_state/
```

Non-public branches use side-by-side storage:

```text
files/game_versions/<safe-branch>/game/
files/game_versions/<safe-branch>/download_state/
```

For current compatibility, `public` keeps the legacy `files/game` and `beta` keeps `files/game_versions/beta`. Future arbitrary non-public branch IDs must use a sanitized name plus a stable branch hash so distinct Steam branches cannot collide in the same cache directory.

This separation is required so beta downloads do not overwrite the public install and public rollbacks do not destroy beta caches.

Diagnostics must name the active install slot kind and slot directory. Public/default should report a `public legacy` slot rooted at `files/`; non-public branches should report a `side-by-side branch cache` slot rooted at `files/game_versions/<safe-branch>/`.

## Branch marker provenance

Completed downloads should write this marker into the selected game directory:

```text
steam_branch.txt
```

The marker is expected to include:

- Branch ID.
- Display name.
- Storage directory name.
- Update timestamp.
- Depot manifest count.
- Depot manifest entries.

For non-public branches, this marker is part of readiness. A non-public cache is not trustworthy if the marker is missing, names a different branch, lacks install-slot provenance, or lacks depot manifest provenance.

Public/default installs may remain ready without a marker to preserve compatibility with older public-only APKs and existing public caches.

## Readiness rules

Public/default selected:

- `files/game/SlayTheSpire2.pck` must exist and be valid.
- `steam_branch.txt` is preferred but not required for upgraded public installs.

Non-public selected:

- `files/game_versions/<safe-branch>/game/SlayTheSpire2.pck` must exist and be valid.
- `steam_branch.txt` must exist in that selected game directory.
- Marker branch must match the selected branch.
- Marker must include install-slot provenance and depot manifest provenance.

If a selected non-public cache fails marker readiness, the launcher should show selected-version redownload/rebuild-cache wording instead of launching it.

## Download and update rules

Download, update check, progress, completion, failure, and redownload status must name or imply the selected game version. Ready and download-required launcher status must also show the active install slot kind so public legacy installs and side-by-side branch caches are distinguishable without exporting diagnostics.

The downloader must resolve depot manifests for the selected branch and write the selected branch marker after a successful completed download.

Branch-specific download state must stay with the branch-specific game directory. Resetting selected download state must not reset inactive branch caches.

## Startup routing rules

Managed launcher startup, Android native startup, and native fallback diagnostics must all agree on the selected branch and resolved game directory.

Startup must not load a non-public PCK only because a file exists. Non-public startup requires marker readiness, install-slot provenance, and depot manifest provenance.

Fallback diagnostics should show enough information to explain why a selected branch did or did not launch:

- Selected branch.
- Resolved game directory.
- Marker path.
- Marker presence.
- Marker branch.
- Depot manifest provenance.
- Marker readiness.

## Cache cleanup rules

`REDOWNLOAD SELECTED VERSION`:

- Deletes only the selected version game cache.
- Deletes only the selected version download state.
- Preserves inactive branch caches.
- Starts replacement download for the selected version.

`CLEAR CACHED VERSIONS`:

- Deletes inactive non-public caches.
- Preserves the selected non-public cache.
- When public/default is selected, inactive non-public caches may be removed.
- Never deletes public/default legacy files as part of inactive non-public cleanup.

## Branch switch safety

Switching branches is a save-compatibility risk. The confirmation must make that risk explicit and should record branch-switch posture for diagnostics.

Expected branch-switch behavior:

- Warn that a download may be required.
- Warn that saves may be incompatible.
- Warn that local backup will be enabled.
- Warn that Steam Cloud Push requires backup storage permission after switching.
- Warn that non-public branches may be private or password-protected.
- Warn that beta password entry is not implemented.
- Enable local backup before applying the branch switch.
- Write branch-switch marker evidence.
- Store the selected branch note in branch-switch marker evidence.

## Steam Cloud Push safety

Manual Push after branch switching is destructive until proven otherwise. It can overwrite Steam Cloud state with local Android state from a different game version.

Push should remain blocked after a branch switch when branch-switch marker safety evidence is incomplete, belongs to a different selected branch, current Pull evidence is unavailable, Android local save evidence is unavailable, or backup storage permission is unavailable.

Early branch-switch Push gate blocks write `last_manual_cloud_push_blocked.txt` before any upload request starts, so missing marker/Pull/local-save/permission evidence can be audited without relying only on logcat.

Before allowing a Push validation after branch switching, evidence must show:

- Pull from Cloud completed after the branch switch for the selected version.
- Pull evidence must have a parseable UTC after the branch-switch marker UTC, an explicit completion flag, and a selected branch matching the active game version.
- Branch-switch marker evidence is complete, readable, and its selected branch matches the active selected branch.
- Branch-switch marker safety booleans show local backup forced, manual Push backup-storage requirement, branch-switch warning acknowledgement, and non-public/password warning acknowledgement.
- Android local saves exist.
- Local backup is enabled.
- Backup storage permission is available.
- Local pre-Push backup evidence covers every important Android local save selected for Push.
- Cloud pre-Push backup evidence covers every existing important Steam Cloud save selected for Push.
- Diagnostics expose pre-Push local/cloud backup counts, latest local/cloud backup UTC, whether each side is newer than the branch-switch marker, and the aggregate branch-switch pre-Push backup evidence result.
- When Local Backup is enabled, manual Push fails before upload if backup storage permission is unavailable, any important Android local save selected for Push cannot be backed up, or any existing important Steam Cloud save selected for Push cannot be backed up.
- Blocked manual Push writes `last_manual_cloud_push_blocked.txt` with selected branch/version, prerequisite status, backup evidence status, and reason.
- User intentionally confirmed the overwrite-risk action.

## Diagnostics contract

Diagnostics should expose:

- Selected game branch.
- Selected game version display name.
- Selected game version note.
- Selector mode.
- Branch discovery support.
- Beta password entry support.
- Selected branch storage directory.
- Selected game version slot kind.
- Selected game version slot directory.
- Selected game directory.
- Selected PCK path.
- Selected readiness result.
- Selected readiness problem.
- Selected download state.
- Selected branch marker path.
- Selected branch marker presence.
- Selected branch marker branch.
- Selected branch marker depot manifest presence/count.
- Selected branch marker readiness.
- Cached non-public version inventory.
- Branch switch marker filename.
- Branch switch marker presence.
- Push backup-storage requirement after branch switch.
- Backup storage permission and directory state.

This is required so tester reports can distinguish a real Steam branch/download problem from stale cache, marker, startup-routing, or save-safety issues.

## Release blockers

The feature is not release-signed until current ARM64 evidence proves:

- Public/default branch regression safety.
- Beta branch download and startup routing.
- Branch marker/provenance readiness.
- Selected-version redownload behavior.
- Inactive cache cleanup behavior.
- Missing/private/password-protected beta behavior.
- Save compatibility across branch switches, or explicit unsupported-risk wording.
- Pull-after-switch and pre-Push backup safety after branch switching.
