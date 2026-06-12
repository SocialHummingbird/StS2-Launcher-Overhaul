# Steam Version Selection Completion Audit

This tracker maps the active goal to evidence. It is intentionally conservative: implementation and documentation are not treated as completion until runtime evidence proves the behavior on the intended target.

## Completion rule

The Steam version-selection goal is complete only when every requirement below has current evidence. Static source or documentation evidence can prove design intent and implementation coverage, but ARM64 device evidence is required for Steam login, depot download, game launch, cloud saves, beta branch behavior, and save compatibility.

## Requirement audit

| Requirement | Static status | Runtime evidence needed | Current completion state |
| --- | --- | --- | --- |
| Branch-aware downloader metadata and manifest resolution | Implemented for selected branch with branch marker/provenance contract documented | Build passes; public/default update check; beta update/download check; inaccessible/private/password branch behavior | Not complete |
| Persist selected branch | Implemented through launcher preferences and documented diagnostics | Device session proves selected branch persists across app restart and update/download flows | Not complete |
| Expose selected branch in launcher UI | Implemented as manual Steam branch entry with wrapped/non-interactive selector guidance shown in managed UI, branch-switch confirmation, logs, diagnostics, native routing logs, and native fallback diagnostics | Visual/device evidence for selector, switch warning, wrapped helper text, persisted selected-version display, native pre-routing logs, and fallback diagnostics | Not complete |
| Branch-aware update/download/redownload flows | Implemented and documented through selected-version wording and cache rules | Public/default and beta download/update/redownload logs prove selected branch paths and messages | Not complete |
| Safe branch-switch warnings | Implemented and documented with non-public/password/save/backup warnings | Device evidence captures confirmation text and local-backup enablement before applying switch | Not complete |
| Save-backup protections | Implemented and documented with complete branch-switch marker evidence gate, successful manual Pull evidence marker, local-save evidence gate, Push backup-storage gate, full local/cloud pre-Push backup coverage enforcement when Local Backup is enabled, blocked Push marker evidence, pre-Push backup evidence diagnostics, successful manual Push marker with backup counts/timestamps, Manual Push evidence marker filename diagnostics, and aggregate post-switch Push evidence diagnostic | Complete branch-switch marker evidence, Pull-after-switch, local-save evidence, backup permission, fail-before-upload evidence for incomplete backups, `last_manual_cloud_push_blocked.txt`, full local pre-Push coverage, full cloud pre-Push coverage, blocked-Push evidence, `last_manual_cloud_push.txt`, marker backup counts/timestamps, and `Manual Push completed after branch switch for selected version with backup evidence` after branch switch | Not complete |
| Side-by-side per-branch install storage | Implemented and documented for `files/game_versions/<branch>/` | Device filesystem/diagnostics prove beta cache coexists with public cache and survives branch switches | Not complete |
| Active-version startup selection | Implemented and documented for managed/native/fallback startup routing | Native startup/logcat proves selected public and selected beta PCK routing; non-public marker failures do not launch | Not complete |
| Diagnostics | Implemented and documented for selected branch, selected-version note, native selected-branch note, marker, cache, and backup state | Captured diagnostics bundle and logcat prove fields are present and accurate on public/default and beta paths | Not complete |
| Cleanup controls | Implemented and documented for selected-version redownload and inactive cache cleanup | Device evidence proves selected cache deletion is scoped and inactive cleanup preserves selected cache | Not complete |
| Explicit release-readiness blockers | Documented in validation, runbook, roadmap, release policy, README, user guide, release-note snippet, and issue template | Release notes/status remain accurate after validation; blockers are cleared only by evidence | Partially complete |
| Beta/password behavior | Unsupported capabilities documented: no arbitrary discovery, no beta password entry | Steam behavior observed for accessible beta, missing/private beta, and password-protected beta or explicit unsupported UI decision | Not complete |
| Save compatibility across branches | Risk documented and Push guarded | Public/beta save load/switch behavior observed, or incompatibility explicitly documented as unsupported | Not complete |

## Evidence inventory

Static artifacts now in place:

- `docs/steam-version-selection-architecture.md`
- `docs/steam-version-selection-validation.md`
- `docs/steam-version-selection-runbook.md`
- `docs/steam-version-selection-evidence-template.md`
- `docs/steam-version-selection-user-guide.md`
- `docs/steam-version-selection-release-note-snippet.md`
- `.github/ISSUE_TEMPLATE/steam_version_selection_report.md`
- `scripts/audit-steam-version-selection.ps1`
- `scripts/audit-steam-branch-guidance-parity.ps1`
- `.github/workflows/steam-version-selection-static-audit.yml`
- `.github/workflows/overhaul-governance-ci.yml` required-scaffold checks for version-selection guardrails.
- README, roadmap, status, changelog, release-validation, and release-policy references.

Runtime evidence still required:

- Managed C# build result.
- Android Java/Gradle build result.
- ARM64 install/startup result.
- Public/default branch download/update/launch.
- Beta branch download/update/launch.
- Branch marker/provenance contents for completed downloads.
- Native selected-PCK startup routing.
- Marker failure recovery/redownload behavior.
- Selected-version redownload behavior.
- Inactive cache cleanup behavior.
- Missing/private/password-protected beta behavior.
- Pull from Cloud after the branch switch for the selected version before Push.
- Backup storage permission evidence.
- Full local pre-Push and cloud pre-Push backup coverage evidence.
- Fail-before-upload evidence and `last_manual_cloud_push_blocked.txt` when required backup coverage is incomplete.
- Manual Push smoke only after safety gates, with `last_manual_cloud_push.txt` and `Manual Push completed after branch switch for selected version with backup evidence`.
- Save compatibility outcome across branch switches.

## Current release decision

Do not mark Steam beta/version selection release-ready yet.

Safe public wording:

- Implemented for validation.
- Available as manual Steam branch entry with `public` as the default branch.
- Still in hardening.
- Not release-signed until ARM64 evidence proves beta/password/private branch behavior, save compatibility, startup routing, cache cleanup, Pull-after-switch/current-backup safety, pre-Push backup evidence, and successful post-switch Push marker evidence.
