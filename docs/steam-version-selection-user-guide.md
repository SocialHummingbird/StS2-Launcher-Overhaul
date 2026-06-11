# Steam Version Selection User Guide

This guide explains the current Steam game version selector in StS2 Launcher.

The feature is implemented for validation and hardening. It is not release-signed yet. ARM64 device evidence is still required for beta/password behavior, inaccessible/private branches, save compatibility, and Steam Cloud Push safety across branch switches.

## What is supported now

- Select the default/public Steam branch.
- Select a named `beta` branch.
- Keep default/public files in the legacy app storage path.
- Keep non-public branch files in side-by-side caches under `game_versions/<branch>/`.
- Redownload only the selected version.
- Clear inactive non-public cached versions.
- Show selected-version diagnostics.
- Warn before switching branches.
- Show wrapped helper text under the game-version selector explaining current public/beta limitations and the active install slot.
- Enable local backup before branch switches.
- Block manual Push after a branch switch when backup storage permission is unavailable.

## What is not supported yet

- Arbitrary Steam branch discovery.
- Steam beta password entry.
- Release-signed behavior for private, inaccessible, or password-protected branches.
- Proven save compatibility between public and beta game versions.
- Treating Steam Cloud Push as safe after a branch switch without Pull and backup evidence.

## Storage model

Default/public branch:

```text
files/game/
files/download_state/
```

Non-public branches:

```text
files/game_versions/<branch>/game/
files/game_versions/<branch>/download_state/
```

Completed branch downloads should write a marker file:

```text
steam_branch.txt
```

For non-public branches, the launcher should not treat a cache as ready unless the marker matches the selected branch and includes install-slot and depot manifest provenance.

## Expected branch marker evidence

The marker should identify:

- Selected branch.
- Display name.
- State directory name.
- Install slot kind.
- Install slot directory.
- Update timestamp.
- Depot manifest count.
- Depot manifest entries.

If a non-public cache is missing this evidence, the launcher should ask to rebuild or redownload the selected version instead of launching ambiguous files.

## Safe branch switching

Before switching versions, the launcher should warn that:

- A download may be required.
- Saves may not be compatible between branches.
- Local backup will be enabled.
- Manual Steam Cloud Push requires backup storage permission after switching.
- Non-public branches may be private or password-protected.
- Beta password entry is not implemented.

After switching, treat Steam Cloud Push as destructive until evidence proves otherwise.

## Steam Cloud safety rules

Before using Push to Cloud after branch switching:

1. Pull from Cloud first.
2. Confirm Android local save files exist.
3. Confirm local backup is enabled.
4. Confirm backup storage permission is available.
5. Confirm local pre-Push backup evidence exists.
6. Confirm cloud pre-Push backup evidence exists.
7. Only then perform a manual Push intentionally.

If any step is missing, do not Push.

## Diagnostics to capture

When reporting version-selection behavior, capture launcher diagnostics and include:

```text
Selected game branch:
Selected game version:
Selected game version note:
Steam branch selector mode:
Steam branch discovery supported:
Steam beta password entry supported:
Selected game branch storage directory:
Selected game version slot kind:
Selected game version slot directory:
Selected game directory:
Selected game PCK path:
Selected game files ready:
Selected readiness problem:
Selected download state:
Selected game branch marker path:
Selected game branch marker present:
Selected game branch marker branch:
Selected game branch marker install slot kind:
Selected game branch marker install slot directory:
Selected game branch marker expected install slot kind:
Selected game branch marker expected install slot directory:
Selected game branch marker has matching install slot provenance:
Selected game branch marker has depot manifests:
Selected game branch marker depot manifest entries:
Selected game branch marker ready:
Current selected branch for version marker comparison:
Game version redownload marker filename:
Game version redownload marker path:
Game version redownload marker present:
Game version redownload marker UTC:
Game version redownload marker UTC parseable:
Game version redownload marker selected branch:
Game version redownload marker matches selected branch:
Game version redownload marker selected version:
Game version redownload marker selected version slot kind:
Game version redownload marker selected version slot directory:
Game version redownload marker game directory:
Game version redownload marker game directory existed before delete:
Game version redownload marker game directory exists after delete:
Game version redownload marker download state directory:
Game version redownload marker download state directory existed before delete:
Game version redownload marker download state directory exists after delete:
Game version redownload marker selected directories cleared:
Game version cache cleanup marker filename:
Game version cache cleanup marker path:
Game version cache cleanup marker present:
Game version cache cleanup marker UTC:
Game version cache cleanup marker UTC parseable:
Game version cache cleanup marker selected branch:
Game version cache cleanup marker matches selected branch:
Game version cache cleanup marker selected version:
Game version cache cleanup marker selected version slot kind:
Game version cache cleanup marker selected version slot directory:
Game version cache cleanup marker game_versions present:
Game version cache cleanup marker removed count:
Game version cache cleanup marker selected cache preserved where applicable:
Branch switch marker filename:
Branch switch marker path:
Branch switch marker present:
Branch switch marker UTC:
Branch switch marker UTC parseable:
Branch switch previous branch:
Branch switch selected branch:
Branch switch selected version:
Branch switch selected version slot kind:
Branch switch selected version slot directory:
Branch switch selected branch matches current selected branch:
Branch switch selected branch note:
Branch switch local backup forced:
Branch switch manual Push requires backup storage:
Branch switch warning acknowledged:
Branch switch non-public warning acknowledged:
Branch switch marker has required safety evidence:
Branch switch marker has required safety evidence for selected branch:
Push requires backup storage after branch switch:
Manual Pull evidence marker filename:
Manual Pull evidence marker path:
Manual Pull evidence marker present:
Manual Pull evidence UTC:
Manual Pull evidence UTC parseable:
Manual Pull evidence selected branch:
Manual Pull evidence selected version:
Manual Pull evidence selected version slot kind:
Manual Pull evidence selected version slot directory:
Manual Pull completion flag recorded:
Manual Pull evidence is after branch switch:
Manual Pull evidence matches selected branch:
Manual Pull completed after branch switch for selected version:
Manual Push evidence marker filename:
Manual Push evidence marker path:
Manual Push evidence marker present:
Latest manual Push evidence outcome:
Latest manual Push evidence UTC:
Latest manual Push evidence selected branch:
Latest manual Push evidence selected version:
Latest manual Push evidence selected version slot kind:
Latest manual Push evidence selected version slot directory:
Latest manual Push evidence reason:
Manual Push evidence UTC:
Manual Push evidence UTC parseable:
Manual Push evidence selected branch:
Manual Push evidence selected version:
Manual Push evidence selected version slot kind:
Manual Push evidence selected version slot directory:
Manual Push evidence recorded local backup count:
Manual Push evidence recorded cloud backup count:
Manual Push evidence recorded latest local backup UTC:
Manual Push evidence recorded latest cloud backup UTC:
Manual Push completion flag recorded:
Manual Push evidence is after branch switch:
Manual Push evidence matches selected branch:
Manual Push evidence recorded pre-Push backup evidence satisfied:
Manual Push completed after branch switch for selected version with backup evidence:
Manual Push blocked evidence marker filename:
Manual Push blocked evidence marker path:
Manual Push blocked evidence marker present:
Manual Push blocked evidence UTC:
Manual Push blocked evidence UTC parseable:
Manual Push blocked evidence selected branch:
Manual Push blocked evidence selected version:
Manual Push blocked evidence selected version slot kind:
Manual Push blocked evidence selected version slot directory:
Manual Push blocked evidence matches selected branch:
Manual Push blocked evidence recorded prerequisites satisfied:
Manual Push blocked evidence recorded local backup count:
Manual Push blocked evidence recorded cloud backup count:
Manual Push blocked evidence recorded latest local backup UTC:
Manual Push blocked evidence recorded latest cloud backup UTC:
Manual Push blocked evidence recorded pre-Push backup evidence satisfied:
Manual Push blocked evidence reason:
Manual Push blocked before upload evidence recorded:
Important Android local save evidence count in bounded scan:
Important Android local save evidence present:
Backup storage permission available:
Backup storage directory:
Backup storage directory exists:
Branch-switch manual Push prerequisites satisfied:
Pre-Push local backup evidence count:
Pre-Push cloud backup evidence count:
Latest pre-Push local backup UTC:
Latest pre-Push cloud backup UTC:
Pre-Push local backup evidence after branch switch:
Pre-Push cloud backup evidence after branch switch:
Branch-switch pre-Push backup evidence satisfied:
```

For GitHub reports, use `.github/ISSUE_TEMPLATE/steam_version_selection_report.md`.

Before sharing diagnostics publicly, remove Steam credentials, refresh tokens, shared preferences, account names, device identifiers, and local user paths. Keep raw logs local when they contain identifying data.

Native startup and native fallback diagnostics should also show:

```text
Selected Steam branch:
Selected Steam branch note before routing:
Selected Steam branch note:
Resolved startup game directory:
Steam branch marker ready:
Steam branch marker depot manifest entries:
```

## Release readiness

The version selector is not release-ready until evidence proves:

- Public/default still downloads, updates, and launches.
- Beta downloads into the side-by-side branch cache.
- Native startup routes to the selected branch PCK.
- Non-public caches without matching marker provenance are blocked.
- Selected-version redownload only clears the selected cache.
- Inactive cache cleanup preserves the selected cache.
- Missing/private/password-protected beta behavior is understood.
- Save compatibility across branches is known or explicitly unsupported.
- Pull-after-switch, local-save, and backup evidence protects Steam Cloud state.

Track save behavior with `docs/steam-version-selection-save-compatibility.md`.
