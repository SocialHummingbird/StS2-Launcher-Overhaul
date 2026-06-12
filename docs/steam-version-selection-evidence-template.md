# Steam Version Selection Evidence Template

Use this file as the evidence capture format when validating Steam game version selection. Do not mark the version-selection work release-ready until every required row is backed by build output, device logs, diagnostics, screenshots, or explicit blocker notes.

Validation build:

Static guardrails:

| Check | Evidence | Result | Notes |
| --- | --- | --- | --- |
| `scripts/audit-steam-version-selection.ps1` passes |  |  |  |
| `scripts/audit-steam-branch-guidance-parity.ps1` passes |  |  |  |
| CI static audit workflow result captured when available |  |  |  |

| Check | Evidence | Result | Notes |
| --- | --- | --- | --- |
| Managed C# launcher compiles |  |  |  |
| Android Java entry points compile |  |  |  |
| APK installs on ARM64 device |  |  |  |
| Launcher starts after install/update |  |  |  |

Public/default branch:

| Check | Evidence | Result | Notes |
| --- | --- | --- | --- |
| Default branch uses legacy `files/game` path |  |  |  |
| Default branch downloads successfully |  |  |  |
| Default branch writes `steam_branch.txt` after fresh download |  |  |  |
| Upgraded public install remains ready without marker if no marker exists |  |  |  |
| Default branch launches from `files/game/SlayTheSpire2.pck` |  |  |  |

Beta/non-public branch:

| Check | Evidence | Result | Notes |
| --- | --- | --- | --- |
| Selecting beta persists `game_branch=beta` |  |  |  |
| Selector helper text shows the active install slot |  |  |  |
| Beta uses `files/game_versions/beta/game` |  |  |  |
| Beta download writes selected branch marker |  |  |  |
| Marker includes install slot kind, install slot directory, depot manifest count, and IDs |  |  |  |
| Native startup logs selected branch, selected branch note, startup directory, marker readiness, and depot manifest count |  |  |  |
| Beta launches only when marker branch, install-slot provenance, and depot manifest provenance are valid |  |  |  |

Missing/private/password branch behavior:

| Check | Evidence | Result | Notes |
| --- | --- | --- | --- |
| Missing/private beta branch fails with branch-specific message |  |  |  |
| Failure logs include `Visible Steam branches:` from Steam app info |  |  |  |
| Launcher failure status shows a compact branch availability diagnosis without adb/logcat only when the marker matches the current selected branch |  |  |  |
| `last_steam_branch_availability.txt` exists after app-info resolution |  |  |  |
| Diagnostics show Steam branch availability marker path, UTC, selected branch, current-selected-branch match, selected-branch visibility, selected-branch Windows depot manifest count, selected-branch downloadable status, selected-branch problem, visible branch count, and visible branch list |  |  |  |
| Branch availability marker values are single-line/sanitized; if Steam returns more branches than the marker captures, overflow count is recorded |  |  |  |
| Failure message includes selected-branch visibility and Windows depot manifest count |  |  |  |
| Failure evidence distinguishes visible-but-not-downloadable from not-visible-to-account where Steam exposes enough app-info data |  |  |  |
| Password-protected branch behavior is observed or explicitly blocked |  |  |  |
| Diagnostics show selected game branch preference key, source, and selection kind |  |  |  |
| Diagnostics show selected game version note |  |  |  |
| Diagnostics show selected game version slot kind and slot directory |  |  |  |
| Diagnostics show `Steam beta password entry supported: false` |  |  |  |
| Diagnostics show `Steam branch discovery supported: true` |  |  |  |
| Diagnostics show Steam branch catalog source, Steam branch dropdown options, and Steam branch dropdown option metadata |  |  |  |
| Dropdown labels remain concise, while selected-version helper text shows availability, password status, build ID, and description where Steam exposes them |  |  |  |
| `REFRESH GAME VERSIONS` fetches Steam app-info branch metadata, refreshes dropdown options, and does not download/delete game files |  |  |  |
| Diagnostics show Android credential Autofill provider model, Godot login field Autofill hints configured, native Android Autofill overlay supported, and launcher stores Steam password for Autofill |  |  |  |
| `USE ANDROID AUTOFILL` opens a native Android username/password dialog with password-manager Autofill hints |  |  |  |
| Native Autofill-filled credentials are consumed by the existing login flow, cancelled, or expired after 60 seconds without being logged or written to artifact files |  |  |  |
| Android/Samsung/password-manager Autofill behavior is validated on device or remains an explicit blocker |  |  |  |
| Diagnostics show `Steam branch selector mode: Steam branch dropdown` |  |  |  |

Cache switching and cleanup:

| Check | Evidence | Result | Notes |
| --- | --- | --- | --- |
| Switching branches writes `last_game_branch_switch.txt` |  |  |  |
| Branch switch marker records parseable UTC, Branch switch previous branch, Branch switch selected branch, selected branch selection kind, selector mode, selected version, selected version slot kind, selected version slot directory, selected branch match against the active selected branch, selected branch note, local backup, Push backup-storage requirement, warning acknowledgement, and required-evidence status for the selected branch |  |  |  |
| Pull-after-switch writes `last_manual_cloud_pull.txt` with parseable UTC, selected branch, selected branch selection kind, selector mode, selected version, selected version slot kind, selected version slot directory, selected branch note, and completion flag |  |  |  |
| Selected non-public cache is preserved by `CLEAR CACHED VERSIONS` |  |  |  |
| Inactive non-public caches are removed by `CLEAR CACHED VERSIONS` |  |  |  |
| Cleanup writes `last_game_version_cache_cleanup.txt` with parseable UTC and diagnostics expose current selected branch context, UTC parseability, selected branch, selected-branch match, selected version, selected version slot kind, selected version slot directory, game_versions presence, removed count, selected-cache-preserved aggregate, removed caches, and preserved selected cache where applicable |  |  |  |
| Diagnostics show cleanup marker filename, path, presence, UTC, UTC parseability, selected branch, selected version, selected version slot kind, selected version slot directory, game_versions presence, and removed count |  |  |  |
| Launcher status/log names the selected version preserved by cleanup |  |  |  |
| Logcat records removed inactive cache paths and preserved selected cache path |  |  |  |
| Evidence bundle includes bounded `game-version-cache-tree.txt`, `game-version-cache-sizes.txt`, and `last_game_version_cache_cleanup.txt` before/after cleanup where possible |  |  |  |
| Diagnostics show cached cache `selected`, `inactive`, `branchMarkerPresent`, `branchMarkerBranch`, `branchMarkerExpectedInstallSlotKind`, `branchMarkerExpectedInstallSlotDirectory`, `branchMarkerMatchingInstallSlotProvenance`, `branchMarkerDepotManifests`, and `branchMarkerReady` flags |  |  |  |
| Cached `branchMarkerReady` requires matching cache directory, install-slot provenance, and depot manifest provenance |  |  |  |

Readiness and recovery:

| Check | Evidence | Result | Notes |
| --- | --- | --- | --- |
| Diagnostics show selected PCK path and final selected game files readiness |  |  |  |
| Diagnostics show selected readiness problem when not ready |  |  |  |
| Diagnostics show selected branch marker branch, depot manifest presence, and depot manifest entry count |  |  |  |
| Diagnostics show branch switch marker filename, path, presence, UTC, UTC parseability, previous branch, selected branch, selected branch note, per-field safety booleans, and required safety evidence status |  |  |  |
| Native pre-routing logs selected branch, selected branch note, and resolved game directory |  |  |  |
| Native startup logs selected branch note, marker readiness, depot manifest presence, and depot manifest entry count |  |  |  |
| Native fallback diagnostics show selected branch note, marker readiness, depot manifest presence, and depot manifest entry count |  |  |  |
| Missing/mismatched non-public marker blocks readiness |  |  |  |
| Missing non-public depot provenance blocks readiness |  |  |  |
| Metadata mismatch shows `REDOWNLOAD SELECTED VERSION` |  |  |  |
| Confirming metadata rebuild clears selected cache before replacement download |  |  |  |
| Redownload writes `last_game_version_redownload.txt` with parseable UTC and diagnostics expose current selected branch context, UTC parseability, selected branch, selected-branch match, selected version, selected version slot kind, selected version slot directory, deleted game directory, pre/post-delete game directory existence, deleted download state directory, pre/post-delete download state existence, and selected directories cleared status |  |  |  |

Steam Cloud save safety:

| Check | Evidence | Result | Notes |
| --- | --- | --- | --- |
| Branch switch forces local backup ON |  |  |  |
| Push after branch switch is blocked when branch-switch marker safety evidence is incomplete or belongs to a different selected branch |  |  |  |
| Push after branch switch is blocked until Pull from Cloud completed after the switch for the selected version |  |  |  |
| Diagnostics show manual Pull evidence marker filename, path, presence, UTC, UTC parseability, selected branch, completion flag, after-switch status, selected-branch match, and selected-version freshness |  |  |  |
| Push after branch switch is blocked when Android local save evidence is missing |  |  |  |
| Push after branch switch is blocked when backup storage permission is missing |  |  |  |
| Early branch-switch Push gate blocks write `last_manual_cloud_push_blocked.txt` before any upload request starts |  |  |  |
| Diagnostics show `Branch-switch manual Push prerequisites satisfied` only after marker evidence, Pull-after-switch evidence, Android local save evidence, and backup storage permission are all present |  |  |  |
| Diagnostics show important Android local save evidence count in bounded scan and presence |  |  |  |
| Diagnostics show backup storage permission and backup directory state |  |  |  |
| Diagnostics show backup storage directory path and whether it exists |  |  |  |
| Diagnostics show `Pre-Push local backup evidence count` and `Pre-Push cloud backup evidence count` |  |  |  |
| Diagnostics show `Latest pre-Push local backup UTC` and `Latest pre-Push cloud backup UTC` |  |  |  |
| Diagnostics show `Pre-Push local backup evidence after branch switch` and `Pre-Push cloud backup evidence after branch switch` |  |  |  |
| Diagnostics show `Branch-switch pre-Push backup evidence satisfied` only after both local and cloud pre-Push backup evidence are newer than the branch-switch marker |  |  |  |
| Manual Push creates `local-pre-push` backup evidence for every important Android local save before upload |  |  |  |
| Manual Push creates `cloud-pre-push` backup evidence for every existing important Steam Cloud save before upload |  |  |  |
| Manual Push fails before upload when Local Backup is enabled but required backup storage, full local pre-Push coverage, or full cloud pre-Push coverage is missing |  |  |  |
| Blocked manual Push writes `last_manual_cloud_push_blocked.txt` with parseable UTC, selected branch, selected version, selected version slot kind, selected version slot directory, selected branch note, prerequisite status, local/cloud pre-Push backup counts, latest local/cloud backup UTC, backup evidence status, reason, and blocked-before-upload flag |  |  |  |
| Successful manual Push writes `last_manual_cloud_push.txt` with parseable UTC, selected branch, selected version, selected version slot kind, selected version slot directory, selected branch note, local/cloud pre-Push backup counts, latest local/cloud backup UTC, completion flag, and recorded pre-Push backup evidence status |  |  |  |
| Diagnostics show `Manual Push completed after branch switch for selected version with backup evidence` only after latest Push outcome is `completed`, Push marker freshness, completion, selected branch match, and pre-Push backup evidence are all satisfied |  |  |  |
| Push confirmation names selected game version and preserves destructive overwrite warning |  |  |  |
| Cross-branch save compatibility is validated or remains a release blocker |  |  |  |

Artifact hygiene:

| Check | Evidence | Result | Notes |
| --- | --- | --- | --- |
| Evidence bundle excludes Steam credentials, refresh tokens, and shared preferences |  |  |  |
| Publicly shared evidence is scrubbed of account names, device identifiers, and local user paths |  |  |  |
| Raw logs with identifying data are kept local or redacted before sharing |  |  |  |

Release decision:

| Item | Evidence | Decision |
| --- | --- | --- |
| Build gate passed |  |  |
| Public/default path validated |  |  |
| Beta/non-public path validated |  |  |
| Cloud save safety validated |  |  |
| Known blockers accepted or resolved |  |  |
| Release readiness decision |  |  |

## Audit label checklist

Use these exact labels when collecting validation screenshots or notes so the static audit and human review map to the same evidence fields:

- Branch switch selected version
- Branch switch selected version slot kind
- Branch switch selected version slot directory
- Branch switch selected branch matches current selected branch
- Branch switch selected branch note
- Branch switch local backup forced
- Branch switch manual Push requires backup storage
- Branch switch warning acknowledged
- Branch switch non-public warning acknowledged
- Branch switch marker has required safety evidence
- Branch switch marker has required safety evidence for selected branch
- Branch-switch manual Push prerequisites satisfied
- Pre-Push local backup evidence count
- Pre-Push cloud backup evidence count
- Latest pre-Push local backup UTC
- Latest pre-Push cloud backup UTC
- Pre-Push local backup evidence after branch switch
- Pre-Push cloud backup evidence after branch switch
- Branch-switch pre-Push backup evidence satisfied
- Manual Pull evidence marker filename
- Manual Pull evidence marker path
- Manual Pull evidence UTC
- Manual Pull evidence UTC parseable
- Manual Pull evidence selected branch
- Manual Pull evidence selected version
- Manual Pull evidence selected version slot kind
- Manual Pull evidence selected version slot directory
- Manual Pull completion flag recorded
- Manual Pull evidence is after branch switch
- Manual Pull evidence matches selected branch
- Manual Pull completed after branch switch
- Manual Push evidence marker filename
- Manual Push evidence marker path
- Manual Push evidence UTC
- Latest manual Push evidence outcome
- Latest manual Push evidence UTC
- Latest manual Push evidence selected branch
- Latest manual Push evidence selected version
- Latest manual Push evidence selected version slot kind
- Latest manual Push evidence selected version slot directory
- Latest manual Push evidence reason
- Manual Push evidence UTC parseable
- Manual Push evidence selected branch
- Manual Push evidence selected version
- Manual Push evidence selected version slot kind
- Manual Push evidence selected version slot directory
- Manual Push evidence recorded local backup count
- Manual Push evidence recorded cloud backup count
- Manual Push evidence recorded latest local backup UTC
- Manual Push evidence recorded latest cloud backup UTC
- Manual Push completion flag recorded
- Manual Push evidence is after branch switch
- Manual Push evidence matches selected branch
- Manual Push evidence recorded pre-Push backup evidence satisfied
- Manual Push completed after branch switch for selected version with backup evidence
- Manual Push blocked evidence marker filename
- Manual Push blocked evidence marker path
- Manual Push blocked evidence UTC
- Manual Push blocked evidence UTC parseable
- Manual Push blocked evidence selected branch
- Manual Push blocked evidence selected version
- Manual Push blocked evidence selected version slot kind
- Manual Push blocked evidence selected version slot directory
- Manual Push blocked evidence matches selected branch
- Manual Push blocked evidence recorded prerequisites satisfied
- Manual Push blocked evidence recorded local backup count
- Manual Push blocked evidence recorded cloud backup count
- Manual Push blocked evidence recorded latest local backup UTC
- Manual Push blocked evidence recorded latest cloud backup UTC
- Manual Push blocked evidence recorded pre-Push backup evidence satisfied
- Manual Push blocked evidence reason
- Manual Push blocked before upload evidence recorded
