# Steam Version Selection User Guide

This guide explains the current Steam game version selector in StS2 Mobile.

The feature is implemented for validation and hardening. The local ARM64 hardening path now has evidence that selected `public-beta` launches from its side-by-side cache. It is not release-signed yet. ARM64 device evidence is still required for beta/password behavior, inaccessible/private branches, save compatibility, Steam Cloud Push safety across branch switches, and release-candidate retest.

For the current release gate and evidence matrix, see [Steam version selection release readiness](steam-version-selection-release-readiness.md).

## What is supported now

- Select the default/public Steam branch.
- Select Steam game versions from a dropdown instead of typing branch names.
- Refresh account-visible Steam branch/version options with `REFRESH GAME VERSIONS` before downloading.
- See selected branch availability metadata in helper text, including downloadability, password status, build ID, and description where Steam exposes them.
- Keep default/public files in the legacy app storage path.
- Keep non-public branch files in side-by-side caches under `game_versions/<branch>/`.
- Redownload only the selected version.
- Clear inactive non-public cached versions.
- Show selected-version diagnostics.
- Warn before switching branches.
- Show wrapped helper text under the game-version selector explaining current public/non-public branch limitations and the active install slot.
- Enable local backup before branch switches.
- Block manual Push after a branch switch when backup storage permission is unavailable.
- Launch selected `public-beta` from `game_versions/public-beta-8128824d/game` on the local ARM64 hardening build.

## What is not supported yet

- Steam beta password entry.
- Release-signed device proof for refreshed dropdown metadata and private/password/unavailable branch behavior.
- Release-signed behavior for private, inaccessible, or password-protected branches.
- Release-candidate proof for selected-version launch/failure routing beyond the local `public-beta` hardening run.
- Proven save compatibility between public and beta game versions.
- Treating Steam Cloud Push as safe after a branch switch without Pull and backup evidence.

## Version selector

Use `REFRESH GAME VERSIONS` after logging in to fetch Steam app-info branch metadata for the current account. This action updates the dropdown and `last_steam_branch_availability.txt`; it does not download, delete, or modify game files.

The dropdown intentionally keeps labels short for phone screens. Detailed selected-version status appears below the selector and should explain:

- Whether the selected branch appears downloadable for this account.
- Whether Steam exposed a Windows depot manifest.
- Whether Steam marked the branch as password-protected.
- Build ID and description when Steam exposes them.
- Whether metadata has not been refreshed yet.
- Whether a previously saved branch was not listed in the latest Steam app-info catalog and may be stale, private, inaccessible, password-protected, or unavailable.

When Steam app-info metadata is available, dropdown labels may include short badges such as `(ready)`, `(build <id>)`, `(password)`, or `(unavailable)`. These badges are only summaries; use the helper text and diagnostics for the full selected-branch evidence.

The selected-version helper text should treat password-protected, no-Windows-manifest, and not-listed branches as blocked states. A branch must not be described as safe to download when Steam marks it password-protected and the launcher has no beta password entry support.

`public` remains available even when Steam branch metadata has not been captured. A previously saved custom branch remains selectable for compatibility with older validation builds.

The launcher blocks selected-version download/update attempts before contacting Steam when refreshed app-info evidence already proves the selected non-public branch is password-protected, exposes no Windows depot manifest to the account, or is a saved branch absent from the latest account-visible catalog. APK/app update checks can still run when the selected game-version update check is blocked. If no refreshed catalog exists yet, the launcher still allows a game-version attempt so it can gather live branch availability evidence.

If the selected local cache has missing or mismatched branch metadata while the branch is also blocked by app-info evidence, the launcher can still delete the selected cache. It does not start a replacement download until the selected branch becomes account-visible/downloadable or a different version is selected.

## Steam login Autofill

Android builds include `USE ANDROID AUTOFILL` on the login screen. This opens a native Android username/password dialog with Autofill hints so Samsung Pass, Google Password Manager, Bitwarden, 1Password, and similar providers can offer credentials.

The launcher does not create a separate Autofill password store and does not log the filled values. The native dialog keeps filled username/password values in memory only until the existing Steam login flow consumes them.

If the native Autofill dialog is cancelled, dismissed, consumed by login, the Android activity stops/destroys, or the result is left pending for 60 seconds, the pending username/password values are cleared from the Java bridge buffer. The Godot password field is also cleared immediately after the login request captures the value.

The launcher still stores Steam session credentials/tokens for normal SteamKit login and Steam Cloud use using the existing encrypted Android Keystore path. That is separate from password-manager Autofill and must not be described as Autofill password storage.

SteamKit debug logging is disabled by default to keep normal diagnostics quieter. If a developer enables it with the Android global setting `sts2_steamkit_debug_logs=1`, SteamKit messages are sanitized before they enter launcher diagnostics. Diagnostics should show `SteamKit debug logs opt-in enabled` and `SteamKit debug logs sanitized for credentials/tokens: true` alongside the Autofill fields.

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

On Android, native startup normalizes `/data/data/<package>` and `/data/user/0/<package>` as equivalent app-private path aliases before comparing install-slot marker provenance.

For beta-integrity reports, the marker should also record public comparison evidence for each depot:

- `publicManifest=<id>` when Steam exposes a public manifest for the same depot.
- `selectedMatchesPublic=true` when Steam serves the same manifest for the selected branch and public.
- `selectedMatchesPublic=false` when the selected branch has a different manifest for that depot.
- `selectedMatchesPublic=unknown` when public comparison data was unavailable.
- `selectedBranchManifest`, `publicManifest`, `manifestRequestBranch`, and `manifestSource` identify branch-integrity provenance for which Steam depot manifest was selected and why.
- `manifestSource=public-inherited` when Steam exposes no explicit selected-branch manifest for a depot and the launcher intentionally downloads the public manifest as inherited branch content.
- `Classification:` in beta-integrity evidence summarizes whether the selected branch looks branch-specific, public-inherited, partial, unavailable, or not ready for final classification.
- `Public-vs-beta key asset comparison captured` should be true before treating art differences as classified evidence.
- `manifestRequestBranch=public` for inherited public depots, so Steam manifest authorization is requested against the branch that actually owns the effective manifest.

If a beta branch has both public-identical and branch-specific depot manifests, the launcher should describe that as partial Steam branch evidence rather than silently implying every depot differs from public.

Inherited public depots are not treated as a full-branch public fallback. They are recorded per depot so users can tell when Steam appears to serve a partial beta branch: some depots are branch-specific, while others are intentionally inherited from public.

After a non-public download completes, the launcher should log a selected-branch integrity summary. If the branch is partial, inherited from public, public-identical, or missing explicit selected-branch manifests, the log should say that plainly and direct testers to diagnostics/file-hash evidence instead of implying the install is fully branch-specific.

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

Manual Push also requires the baseline evidence even without a branch switch: Pull from Cloud must have completed for the currently selected version, and Android local save evidence must exist before upload. If either prerequisite is missing, the launcher blocks Push with status/log text naming the selected game version. The Pull evidence marker records `Manual Pull completed before Push: true`; branch-switch validation also keeps the stricter branch-switch Pull flag.

The Push confirmation should name the selected game version, the selected version slot, and the required selected-version safety evidence: Pull-after-switch, Android local save evidence, backup storage permission, local pre-Push backup evidence, and cloud pre-Push backup evidence.

## Diagnostics to capture

When reporting version-selection behavior, capture launcher diagnostics and include:

- Selected branch.
- Branch marker depot manifest count.
- Branch marker depots matching public.
- Branch marker depots differing from public.
- Branch marker depots without public comparison.
- Branch marker depots inherited from public.
- Branch marker depots missing selected branch manifest.
- Branch marker depot manifest rows.
- Any art/assets paths that look public while code/UI looks beta.

For mixed public-beta behavior or art asset reports, run `scripts/capture-steam-beta-integrity-evidence.ps1` after a clean selected-version redownload and read `beta-integrity-summary.txt` first. Treat `Evidence readiness: not ready for final classification` as unresolved evidence, not as a final cause. Strong manifest/cache/art conclusions require `Clean redownload matches investigated branch: true` and `Clean redownload selected directories cleared: true`; branch-availability issues can be classified from app-info evidence when Steam reports the selected branch is absent or exposes no Windows depot manifests.

```text
Selected game branch:
Selected game branch preference key:
Selected game branch source:
Selected game branch selection kind:
Selected game version:
Selected game version note:
Steam branch selector mode:
Steam branch discovery supported:
Steam branch catalog source:
Steam branch dropdown options:
Steam branch dropdown option metadata:
Steam beta password entry supported:
Android credential Autofill provider model:
Godot login field Autofill hints configured:
Native Android Autofill overlay supported:
Launcher stores Steam password for Autofill:
Native Android Autofill result TTL seconds:
Android credential Autofill implementation note:
SteamKit debug logs opt-in enabled:
SteamKit debug logs sanitized for credentials/tokens:
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
Steam branch availability marker filename:
Steam branch availability marker path:
Steam branch availability marker present:
Steam branch availability UTC:
Steam branch availability selected branch:
Steam branch availability matches current selected branch:
Steam branch availability selected branch visibility:
Steam branch availability selected branch Windows depot manifests:
Steam branch availability selected branch downloadable:
Steam branch availability selected branch problem:
Steam branch availability visible branch count:
Steam branch availability visible branches:
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
Branch switch selected branch selection kind:
Branch switch selector mode:
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
Manual Pull evidence selected branch selection kind:
Manual Pull evidence selector mode:
Manual Pull evidence selected version:
Manual Pull evidence selected version slot kind:
Manual Pull evidence selected version slot directory:
Manual Pull completion flag recorded:
Manual Pull completed before Push:
Manual Pull evidence is after branch switch:
Manual Pull evidence matches selected branch:
Manual Pull completed after branch switch for selected version:
Current important Android local save evidence count:
Current important Android local save evidence present:
Baseline manual Push prerequisites satisfied:
Manual Push evidence marker filename:

`last_manual_cloud_push.txt`

Blocked Manual Push evidence marker filename:

`last_manual_cloud_push_blocked.txt`
Manual Push evidence marker path:
Manual Push evidence marker present:
Latest manual Push evidence outcome:
Latest manual Push evidence UTC:
Latest manual Push evidence selected branch:
Latest manual Push evidence selected branch selection kind:
Latest manual Push evidence selector mode:
Latest manual Push evidence selected version:
Latest manual Push evidence selected version slot kind:
Latest manual Push evidence selected version slot directory:
Latest manual Push evidence reason:
Manual Push evidence UTC:
Manual Push evidence UTC parseable:
Manual Push evidence selected branch:
Manual Push evidence selected branch selection kind:
Manual Push evidence selector mode:
Manual Push evidence selected version:
Manual Push evidence selected version slot kind:
Manual Push evidence selected version slot directory:
Manual Push evidence recorded local backup count:
Manual Push evidence recorded cloud backup count:
Manual Push evidence recorded latest local backup UTC:
Manual Push evidence recorded latest cloud backup UTC:
Manual Push evidence recorded important local save evidence count:
Manual Push evidence recorded baseline prerequisites satisfied:
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
Manual Push blocked evidence selected branch selection kind:
Manual Push blocked evidence selector mode:
Manual Push blocked evidence selected version:
Manual Push blocked evidence selected version slot kind:
Manual Push blocked evidence selected version slot directory:
Manual Push blocked evidence matches selected branch:
Manual Push blocked evidence recorded prerequisites satisfied:
Manual Push blocked evidence recorded local backup count:
Manual Push blocked evidence recorded cloud backup count:
Manual Push blocked evidence recorded latest local backup UTC:
Manual Push blocked evidence recorded latest cloud backup UTC:
Manual Push blocked evidence recorded important local save evidence count:
Manual Push blocked evidence recorded baseline prerequisites satisfied:
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
Track implementation-versus-evidence status with `docs/steam-version-selection-release-readiness.md`.
