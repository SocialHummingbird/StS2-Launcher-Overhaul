# Steam Version Selection Evidence Template

Use this file as the evidence capture format when validating Steam game version selection. Do not mark the version-selection work release-ready until every required row is backed by build output, device logs, diagnostics, screenshots, or explicit blocker notes.

Validation build:

Static guardrails:

| Check | Evidence | Result | Notes |
| --- | --- | --- | --- |
| `scripts/audit-steam-version-selection.ps1` passes |  |  |  |
| `scripts/audit-steam-branch-guidance-parity.ps1` passes |  |  |  |
| CI static audit workflow result captured when available |  |  |  |
| `docs/steam-version-selection-release-readiness.md` reviewed against this evidence package |  |  |  |

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
| Beta launches only when marker branch, install-slot provenance, depot manifest provenance, and branch-integrity provenance are valid |  |  |  |

Public-vs-beta branch integrity:

| Check | Evidence | Result | Notes |
| --- | --- | --- | --- |
| Beta slot was clean-redownloaded before integrity capture |  |  |  |
| `beta-integrity-summary.txt` clean-redownload fields show marker present, investigated branch match, and selected directories cleared |  |  |  |
| `beta-integrity-summary.txt` branch-availability fields show marker present, investigated branch match, selected-branch visibility, Windows depot manifest count, and visible-branch count |  |  |  |
| `beta-integrity-summary.txt` names public/default and selected branch marker paths when present |  |  |  |
| `beta-integrity-summary.txt` includes bounded public/default and selected branch depot manifest rows |  |  |  |
| Focused beta-integrity logcat captured selected branch routing, marker readiness, manifest provenance, fallback, or public-inherited lines where available |  |  |  |
| Marker records `selectedBranchManifest`, `publicManifest`, `manifestSource`, `manifestRequestBranch`, `selectedMatchesPublic`, and `effectiveMatchesPublic` for depot rows |  |  |  |
| Marker records depots matching public, differing from public, inherited from public, missing selected branch manifest, and without public comparison |  |  |  |
| Launcher diagnostics include selected branch marker depot manifest rows |  |  |  |
| Download completion log includes selected-branch integrity summary |  |  |  |
| Public/default file inventory captured with SHA-256 hashes |  |  |  |
| Public/default cache tree captured for stale-cache/fallback comparison |  |  |  |
| Selected beta file inventory captured with SHA-256 hashes |  |  |  |
| Selected beta cache tree captured for stale-cache/fallback investigation |  |  |  |
| Public-vs-beta inventory comparison captured |  |  |  |
| `public-vs-<branch>-key-assets.tsv` captures focused PCK/art/audio/data/font hash states |  |  |  |
| `beta-integrity-summary.txt` includes bounded changed key-asset rows |  |  |  |
| `SlayTheSpire2.pck` hash compared between public and beta |  |  |  |
| Affected art asset paths/hashes compared where known |  |  |  |
| `beta-integrity-summary.txt` records a `Classification:` line for Steam partial branch, stale cache, launcher fallback, runtime remote/config behavior, or inconclusive evidence |  |  |  |
| `beta-integrity-summary.txt` records `Evidence readiness:` and `Evidence missing/weak:` lines |  |  |  |
| `scripts/review-beta-integrity-summary.ps1` verdict captured for `beta-integrity-summary.txt` |  |  |  |
| `beta-integrity-summary.txt` records classification input metrics for branch availability, depot counters, inventory differences, and art/bundle hash differences |  |  |  |
| Classification is inconclusive unless clean-redownload proof belongs to the investigated branch and selected directories were cleared, except for branch-availability issues proven by app-info |  |  |  |

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
| Selected-version helper text treats password-protected, no-Windows-manifest, and not-listed branches as blocked states |  |  |  |
| `REFRESH GAME VERSIONS` fetches Steam app-info branch metadata, refreshes dropdown options, and does not download/delete game files |  |  |  |
| Diagnostics show Android credential provider model, native integrated credential panel supported, native credential fields Autofill hints configured, Steam credential web domain configured, Godot login field credential metadata configured, Android keyboard credential hints configured, Godot fields are native Android Autofill targets false, password-manager suggestions device validated false until proven, native credential handoff popup unsupported/user-facing disabled, launcher stores Steam password for credential providers false, Android credential provider capability boundary, SteamKit debug logs opt-in status, and SteamKit debug logs are disabled by default/sanitized for credentials and tokens when enabled |  |  |  |
| Normal Android login uses the integrated native Steam credential panel and does not show the native `USE ANDROID AUTOFILL` handoff popup |  |  |  |
| Manual username/password entry reaches Steam login, Steam Guard if required, failed-login recovery, and successful return to launcher; the Godot password field clears after request capture |  |  |  |
| Android/Samsung/password-manager suggestion behavior in the native credential panel is validated on device or remains an explicit blocker |  |  |  |
| Diagnostics show `Steam branch selector mode: Steam branch dropdown` |  |  |  |

Cache switching and cleanup:

| Check | Evidence | Result | Notes |
| --- | --- | --- | --- |
| Switching branches writes `last_game_branch_switch.txt` |  |  |  |
| Branch switch marker records parseable UTC, Branch switch previous branch, Branch switch selected branch, selected branch selection kind, selector mode, selected version, selected version slot kind, selected version slot directory, selected branch match against the active selected branch, selected branch note, local backup, Push backup-storage requirement, warning acknowledgement, and required-evidence status for the selected branch |  |  |  |
| Branch switch confirmation shows refreshed selected-branch availability/status and any selected-branch blocked reason |  |  |  |
| Pull-after-switch writes `last_manual_cloud_pull.txt` with parseable UTC, selected branch, selected branch selection kind, selector mode, selected version, selected version slot kind, selected version slot directory, selected branch note, and completion flag |  |  |  |
| Selected non-public cache is preserved by `CLEAR CACHED VERSIONS` |  |  |  |
| Inactive non-public caches are removed by `CLEAR CACHED VERSIONS` |  |  |  |
| Stale runtime packs are removed by `CLEAR CACHED VERSIONS` |  |  |  |
| Selected runtime pack is preserved by `CLEAR CACHED VERSIONS` when present |  |  |  |
| Cleanup writes `last_game_version_cache_cleanup.txt` with parseable UTC and diagnostics expose current selected branch context, UTC parseability, selected branch, selected-branch match, selected version, selected version slot kind, selected version slot directory, game_versions presence, runtime_packs presence, selected runtime pack directory, selected runtime pack pre-cleanup presence, removed count, removed runtime pack count, selected-cache-preserved aggregate, selected-runtime-pack-preserved aggregate, removed caches/runtime packs, and preserved selected cache/runtime pack where applicable |  |  |  |
| Diagnostics show cleanup marker filename, path, presence, UTC, UTC parseability, selected branch, selected version, selected version slot kind, selected version slot directory, game_versions presence, runtime_packs presence, selected runtime pack state, removed count, and removed runtime pack count |  |  |  |
| Launcher status/log names the selected version preserved by cleanup and reports both game-version and runtime-pack removal counts |  |  |  |
| Logcat records removed inactive cache/runtime-pack paths and preserved selected cache/runtime-pack paths |  |  |  |
| Evidence bundle includes bounded `game-version-cache-tree.txt`, `game-version-cache-sizes.txt`, and `last_game_version_cache_cleanup.txt` before/after cleanup where possible |  |  |  |
| Diagnostics show cached cache `selected`, `inactive`, `branchMarkerPresent`, `branchMarkerBranch`, `branchMarkerExpectedInstallSlotKind`, `branchMarkerExpectedInstallSlotDirectory`, `branchMarkerMatchingInstallSlotProvenance`, `branchMarkerDepotManifests`, `branchMarkerIntegrityProvenance`, public/differing/inherited/missing selected manifest counts, and `branchMarkerReady` flags |  |  |  |
| Cached non-public `branchMarkerReady` requires matching cache directory, install-slot provenance, depot manifest provenance, and branch-integrity provenance |  |  |  |

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
| Missing non-public depot provenance or branch-integrity provenance blocks readiness |  |  |  |
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

Steam Workshop mod safety:

Recommended capture commands for each phase:

```powershell
.\scripts\capture-workshop-mod-evidence.ps1 -Phase no-mods -Launch -ClearLogcat -Screenshot
.\scripts\review-workshop-mod-evidence.ps1 -EvidenceDir <no-mods-dir> -RequirePhase no-mods -RequireScreenshot

.\scripts\capture-workshop-mod-evidence.ps1 -Phase simple -Launch -ClearLogcat -Screenshot
.\scripts\review-workshop-mod-evidence.ps1 -EvidenceDir <simple-dir> -RequirePhase simple -RequireScreenshot

.\scripts\capture-workshop-mod-evidence.ps1 -Phase dependency -Launch -ClearLogcat -Screenshot
.\scripts\review-workshop-mod-evidence.ps1 -EvidenceDir <dependency-dir> -RequirePhase dependency -RequireScreenshot

.\scripts\capture-workshop-mod-evidence.ps1 -Phase broken -Launch -ClearLogcat -Screenshot
.\scripts\review-workshop-mod-evidence.ps1 -EvidenceDir <broken-dir> -RequirePhase broken -RequireScreenshot

.\scripts\capture-workshop-mod-evidence.ps1 -Phase public -Launch -StartGame -ClearLogcat -Screenshot
.\scripts\review-workshop-mod-evidence.ps1 -EvidenceDir <public-dir> -RequirePhase public -RequireScreenshot

.\scripts\capture-workshop-mod-evidence.ps1 -Phase public-beta -Launch -StartGame -ClearLogcat -Screenshot
.\scripts\review-workshop-mod-evidence.ps1 -EvidenceDir <public-beta-dir> -RequirePhase public-beta -RequireScreenshot

.\scripts\capture-workshop-mod-evidence.ps1 -Phase core-release -Launch -StartGame -ClearLogcat -Screenshot
.\scripts\review-workshop-mod-evidence.ps1 -EvidenceDir <core-release-dir> -RequirePhase core-release -RequireScreenshot
```

After an initial simple/dependency Workshop sync succeeds, repeat the same sync and capture the second launch to prove unchanged items reuse the cached download instead of downloading again:

```powershell
.\scripts\capture-workshop-mod-evidence.ps1 -Phase simple -Launch -ClearLogcat -Screenshot -RunLabel cached-reuse
.\scripts\review-workshop-mod-evidence.ps1 -EvidenceDir <simple-cached-reuse-dir> -RequirePhase simple -RequireCachedDownloadReuse -RequireScreenshot
```

For downloaded Workshop content that contains no usable `.pck`, collect it as a broken phase and require explicit `staged-no-pck` evidence. Repeating the sync should reuse the cached no-PCK directory instead of downloading it again:

```powershell
.\scripts\capture-workshop-mod-evidence.ps1 -Phase broken -Launch -ClearLogcat -Screenshot -RunLabel no-pck
.\scripts\review-workshop-mod-evidence.ps1 -EvidenceDir <broken-no-pck-dir> -RequirePhase broken -RequireScreenshot

.\scripts\capture-workshop-mod-evidence.ps1 -Phase broken -Launch -ClearLogcat -Screenshot -RunLabel no-pck-cached-reuse
.\scripts\review-workshop-mod-evidence.ps1 -EvidenceDir <broken-no-pck-cached-reuse-dir> -RequirePhase broken -RequireCachedDownloadReuse -RequireScreenshot
```

Before collecting device evidence, run the local reviewer regression so false positives are caught without touching the device or Steam Cloud:

```powershell
.\scripts\test-workshop-mod-evidence-reviewer.ps1
```

| Check | Evidence | Result | Notes |
| --- | --- | --- | --- |
| No-mods baseline launches with no active Workshop staged PCK mods |  |  |  |
| Clear Workshop Mods writes `last_workshop_mod_clear.txt`, empties active staged Workshop directories and loose staged root files, preserves downloads, and records `steamCloudPushPerformed=false` |  |  |  |
| Workshop sync records subscription query type and query attempts |  |  |  |
| Workshop sync records subscribed, resolved dependency, missing dependency, total discovered, and manifest item counts without counting missing dependencies as resolved |  |  |  |
| Repeated sync for unchanged Workshop items, including `staged-no-pck` no-PCK content, records `ReusedCachedDownload=true` and logs `Using cached Workshop download` |  |  |  |
| Interrupted-download leftovers are cleaned before sync, with no `.download`, `.tmp-*`, or `.old-*` artifacts under `files/workshop_mods/downloads` in `workshop-tree.txt` |  |  |  |
| Simple subscribed Workshop mod downloads, stages into app-private storage, records staged path/hash, and loads through the Android mod loader path |  |  |  |
| Dependency Workshop mod records required-by parent IDs, stages dependencies once, and does not duplicate shared dependency downloads |  |  |  |
| Simple/dependency launch logs show the Android Workshop staged mod root was scanned by the mod loader (`Scanning Workshop staged mods`, `ModLoader`, or `Loaded mod`) |  |  |  |
| Workshop sync removes stale loose root-level staged files so only current item directories can be loaded by the recursive mod scan |  |  |  |
| Broken/missing dependency/no-PCK Workshop mod records missing dependency IDs, failed/unsupported status, or `staged-no-pck` status and visible launcher/diagnostic status beginning with `Workshop mods need attention` instead of silently treating sync as clean |  |  |  |
| Workshop manifest lists each item ID, title, status, PCK presence, file count, content hash, download source kind (`direct-url`, `ugc-hcontent`, or `depot-manifest`), manifest ID when depot-backed, expected download bytes, UGC content handle, fresh-vs-cached update state, sanitized download URL presence/host, dependency flag, required-by IDs, staged directory, source directory, and error without raw signed download URLs |  |  |  |
| Derived Workshop state records manifest active PCK count, raw staged PCK count, `workshopCloudPushLocked`, and `steamCloudPushPerformed=false` |  |  |  |
| Required phase reviews prove `launchRequested=true` and reject focused launch logs containing `NativeFallback`, `FATAL EXCEPTION`, `SIGSEGV`, or equivalent crash/fallback signatures |  |  |  |
| Public/public-beta/core-release game-start captures use `-StartGame` so logs prove the launcher tapped Start Game and loaded the selected PCK path |  |  |  |
| Public/public-beta/core-release Workshop phase reviews prove staged Workshop PCK mods are present, Cloud Push is locked/off, and the Android Workshop mod-loader scan loaded staged mods |  |  |  |
| Public/public-beta/core-release Workshop phase evidence includes `current_runtime_slot.json`, `current_runtime_cache.txt`, `last_runtime_patch_validation.json`, runtime PCK/`sts2.dll` hashes, and selected runtime-pack manifest/validation files when a runtime pack is active |  |  |  |
| Public-beta/core-release Workshop phase review rejects evidence unless the selected runtime cache PCK path is under `files/game_versions/<branch>-*`, runtime patch validation passed, and the selected runtime pack is clean/generated/validated |  |  |  |
| Manual Steam Cloud Push is blocked when active Workshop PCK mods are staged, and the blocked marker records the reason before upload starts |  |  |  |
| Manual Steam Cloud Push remains blocked when raw staged Workshop `.pck` files exist even if the Workshop manifest is missing or stale |  |  |  |
| Public branch launches after Workshop sync or clear no-mods state with expected mod loader state |  |  |  |
| Public-beta branch launches after Workshop sync or clear no-mods state with expected mod loader state |  |  |  |
| Core-release branch launches after Workshop sync or clear no-mods state with expected mod loader state |  |  |  |

Artifact hygiene:

| Check | Evidence | Result | Notes |
| --- | --- | --- | --- |
| Evidence bundle excludes Steam credentials, refresh tokens, and shared preferences |  |  |  |
| Publicly shared evidence is scrubbed of account names, device identifiers, and local user paths |  |  |  |
| Raw full logcat was omitted by default, or captured only with `-IncludeRawLogcat` for local-only diagnostics and manually reviewed/redacted before sharing |  |  |  |
| Evidence bundle records `sts2_steamkit_debug_logs` setting state; public log excerpts confirm SteamKit logs were disabled or sanitized |  |  |  |
| Public issue evidence uses `logcat-steam-version-focused-redacted.txt` or equivalent manual redaction instead of raw full logcat, and the best-effort redacted file was manually reviewed before posting |  |  |  |
| Redacted focused logcat includes its best-effort/manual-review warning header |  |  |  |
| Evidence bundle includes `ARTIFACT_HYGIENE.txt` and raw logs are treated as local-only unless manually reviewed and redacted |  |  |  |
| Evidence bundle includes `PUBLIC_SHARE_MANIFEST.txt` listing preferred public artifacts and local-only/manual-review artifacts |  |  |  |
| Beta-integrity summary public-sharing warning reviewed; focused logcat, marker paths, cache tree, and inventory paths manually checked before public posting |  |  |  |
| Evidence bundle includes `logcat-redaction-summary.txt` with focused-line and changed-line counts |  |  |  |
| Evidence bundle includes `launcher-diagnostics-index.txt`; any full launcher diagnostics report attached publicly was manually reviewed/redacted |  |  |  |
| Full launcher diagnostics and startup-recovery diagnostics reports include a public-sharing warning before detailed state/evidence/log sections |  |  |  |
| Copied raw error logs include review/redaction warning text before public posting |  |  |  |
| Launcher support UI labels raw-log copy as review-before-sharing |  |  |  |
| Startup recovery UI labels raw-log copy as review-before-sharing and warns raw logs can contain identifying data |  |  |  |

Release decision:

| Item | Evidence | Decision |
| --- | --- | --- |
| Release-readiness tracker reviewed |  |  |
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
- Manual Pull completed before Push
- Manual Pull evidence is after branch switch
- Manual Pull evidence matches selected branch
- Manual Pull completed after branch switch
- Current important Android local save evidence count
- Current important Android local save evidence present
- Baseline manual Push prerequisites satisfied
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
- Manual Push evidence recorded important local save evidence count
- Manual Push evidence recorded baseline prerequisites satisfied
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
- Manual Push blocked evidence recorded important local save evidence count
- Manual Push blocked evidence recorded baseline prerequisites satisfied
- Manual Push blocked evidence recorded pre-Push backup evidence satisfied
- Manual Push blocked evidence reason
- Manual Push blocked before upload evidence recorded
- Selected game branch marker depots matching public
- Selected game branch marker depots differing from public
- Selected game branch marker depots without public comparison
- Selected game branch marker depots inherited from public
- Selected game branch marker depots missing selected branch manifest
- Selected game branch marker partial Steam branch evidence
- Selected game branch marker depot manifest rows
