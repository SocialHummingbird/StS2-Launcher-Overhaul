# Steam Version Selection Release Readiness

This tracker is the current release-readiness contract for Steam game version selection and login hardening.

The implementation is substantially in place, but release readiness is not proven until the evidence rows below are filled from an ARM64 device or an explicitly accepted diagnostic-only emulator path.

## Current status

Steam game version selection is published in the latest ARM64 APK, but it is not release-candidate signed off until the evidence gates below are repeated on the release candidate.

| Area | Current state | Release posture |
| --- | --- | --- |
| Public/default branch | Public/default remains available and keeps the legacy `files/game` install path. Local fix27 ARM64 evidence proves public/default launcher startup and game main-menu launch with matched PCK/runtime hashes. | Requires release-candidate repeat evidence for public/default download or update check, launch, and cloud Pull posture. |
| Branch selector | Normal users select from a discovered dropdown; manual branch text entry is no longer the normal path. Local fix27 ARM64 evidence proves `REFRESH GAME VERSIONS` updates the dropdown with public and public-beta branch metadata. | Requires release-candidate repeat evidence for refresh/dropdown behavior and selected-helper text. |
| Branch metadata | Refreshed Steam app-info can expose branch visibility, build IDs, password flags, Windows depot manifests, and availability problems. | Requires account-visible branch evidence for public, available beta, and inaccessible/private/password cases where possible. |
| Blocked branches | Known password-protected, no-manifest, or absent saved branches are blocked before download/update attempts when refreshed app-info proves they are unavailable. Local fix27 ARM64 evidence proves a synthetic stale saved branch is surfaced as unavailable and blocked before selected-version download/update. | Requires release-candidate repeat evidence plus true account-side private/password/no-manifest branch coverage where possible. |
| Side-by-side storage | Non-public branches use `files/game_versions/<branch>/game` and matching download-state storage. | Requires beta/non-public install evidence and branch marker contents. |
| Native startup routing | Native startup blocks selected-version launch when marker provenance is missing or mismatched. Local ARM64 evidence now proves selected `public-beta` launch from its side-by-side cache after Android app-private path alias normalization. | Requires release-candidate repeat evidence, public/default retest, and negative-case evidence for missing or untrusted selected slots. |
| Beta branch integrity | Selected branch markers record per-depot effective manifest, selected-branch manifest, public manifest, manifest source, and whether each depot matches or inherits public. Beta-integrity evidence capture now emits a clean-redownload-gated `Classification:`, `Evidence readiness:`, and `Evidence missing/weak:` summary. Local ARM64 evidence classifies `public-beta` as branch-specific installed content and runtime startup now logs the selected PCK path/byte count/SHA-256 mounted by Godot. Public/default and public-beta both reach the visible main menu; public loads the run-history `doormaker_boss` imported textures while public-beta lacks those entries and contains `aeonglass_boss` instead. Runtime patching falls back to branch-local `unknown_monster` art for missing run-history icon paths. | Requires release-candidate repeat evidence and continued user-facing caution that visible beta/public art differences are a game-content/runtime question, not proof of launcher fallback, unless the runtime selected-PCK evidence contradicts the selected branch. |
| Cache cleanup | Selected-version redownload and inactive cache cleanup are branch-aware by design, and inactive cleanup removes stale runtime packs while preserving the selected runtime pack when present. Local fix27 evidence proves inactive runtime-pack cleanup/preservation and a synthetic selected side-by-side cache redownload marker/delete path. | Requires release-candidate evidence that real selected-version redownload clears only the selected cache/runtime pack and, when requested, performs the replacement download. |
| Steam Cloud safety | Manual Push requires selected-version Pull evidence and Android local save evidence; branch-switch Push adds backup evidence gates. Local fix28 evidence proves missing/stale selected-version evidence blocks on the first Push tap before destructive confirmation can be armed, writes `last_manual_cloud_push_blocked.txt`, and does not create `last_manual_cloud_push.txt`. A follow-up read-only fix28 marker capture still shows selected `public`, `last_manual_cloud_push.txt` missing, blocked-Push evidence present, and pre-Push local/cloud backup counts both `0`. | Requires release-candidate Pull-before-Push, blocked-Push, and successful-Push evidence on the selected version before signoff. |
| Login credential providers | Android builds use an integrated in-app native Steam credential panel with real username/password fields, Android Autofill credential-provider hints, and Steam web-domain metadata. The old native handoff popup is not user-facing, and the launcher does not store or inject Steam passwords. Local fix28 ARM64 evidence proves the panel appears after saved-session removal, Back/Cancel dismissal works, empty submit is handled inline, and Samsung Pass/Android Autofill recognize the Steam web domain plus username/password fields. | Requires manual real-login, Steam Guard, failed-login, successful-return, Google Password Manager, and matched saved-credential suggestion evidence on device. The current Samsung Pass provider reported no matched saved Steam credential, so matched suggestion behavior remains open rather than proven. |
| Credential/log safety | SteamKit debug logging is disabled by default and sanitized when explicitly enabled; diagnostics and raw-log copy paths warn before sharing. | Requires evidence capture review before public issue/release artifacts are posted. |

## Evidence required before release-candidate signoff

1. Build/static gate.

   Current managed C#, Android Java, and static version-selection guardrails must pass for the release candidate being tested.

2. Public/default baseline.

   Evidence must show default/public selected, legacy public install paths, successful download or update check, successful launch, and no regression in Steam login/cloud Pull.

3. Account-visible branch refresh.

   Evidence must show `REFRESH GAME VERSIONS` updating the dropdown and branch availability marker without downloading, deleting, or mutating game files.

4. Available non-public branch.

   If the account can see a non-public branch, evidence must show branch-aware download into the side-by-side cache, a matching `steam_branch.txt` marker with depot manifest provenance, and launch routing to that selected branch. Local `public-beta` evidence is captured; release-candidate repeat evidence is still required.

5. Unavailable/private/password branch handling.

   Evidence must show unavailable branches are surfaced clearly and blocked before download/update attempts when app-info proves they are not downloadable. Local fix27 evidence at `artifacts/android/steam-version-selection-fix27-unavailable-saved-branch-20260619-0000` covers a synthetic stale saved branch that is absent from the latest Steam app-info catalog; the sanitized export at `artifacts/android/steam-version-selection-fix27-unavailable-saved-branch-20260619-0000-public-redacted` passed redaction review. If no real inaccessible/password/no-manifest branch can be tested, the release note must keep this as an explicit limitation.

6. No silent public fallback.

   Evidence must show selected non-public branch failures do not silently start, update, or redownload the public/default branch.

7. Beta branch integrity.

   Evidence must show whether selected branch depots are public-identical, branch-specific, or explicitly inherited from public by comparing selected and public manifest IDs per depot. If art assets look public while other behavior looks beta, evidence must include the branch marker public-comparison counts, selected branch file inventory, public branch file inventory, focused logcat, key asset or PCK hashes, runtime selected-PCK path/byte count/SHA-256, and `Evidence readiness:` not blocked in `beta-integrity-summary.txt`. Runtime PCK hashes can differ from raw Steam inventory hashes because Android download completion patches the PCK in place before launch.

8. Cache mutation safety.

   Evidence must show redownload clears only the selected version, inactive cleanup preserves the selected cache, and switch-back-to-public leaves non-public caches alone unless cleanup is explicitly invoked.

9. Save and Steam Cloud safety.

   Evidence must show selected-version Pull-before-Push, Android local save evidence, branch-switch backup requirements, blocked-Push markers when prerequisites are missing, and successful Push only after the required selected-version evidence exists.

10. Login/password-manager behavior.

   Evidence must show the integrated native Steam credential panel supports manual entry, keyboard/password-manager suggestion behavior where available, Steam Guard prompts, failed-login recovery, successful return to the launcher, and immediate password-field clearing after request capture. The separate native handoff popup must remain absent from the normal user-facing login flow.

11. Artifact hygiene.

    Public evidence must use the redacted/focused artifacts where possible. Raw logs, full diagnostics, private saves, credentials, tokens, account names, local paths, and device-identifying details must remain local or be manually reviewed and redacted before sharing. The evidence folder must include a completed `PUBLIC_EVIDENCE_REDACTION_REVIEW.txt`, and `scripts\review-public-evidence-redaction.ps1 -EvidenceDir <evidence-folder>` must pass before any artifact is posted publicly or counted for release signoff.

## Known release blockers

- Local fix27 ARM64 validation now proves refreshed dropdown behavior with public and public-beta branch metadata; release-candidate repeat evidence is still required.
- Public/default branch has local fix27 ARM64 launch evidence with matched runtime/PCK hashes; release-candidate repeat evidence is still required after the latest branch-selector and login-hardening changes.
- Non-public branch cleanup, true private/password/no-manifest failure handling, and release-candidate startup routing still need current-device evidence. Local fix27 evidence covers synthetic stale saved-branch blocking and synthetic selected-cache redownload marker deletion only.
- Real selected-version redownload and replacement download evidence remains release-open; current fix27 evidence deletes only a synthetic side-by-side cache and proves marker semantics/cache boundaries without deleting real beta content.
- Public versus public-beta branch integrity is classified locally as branch-specific installed content, and runtime evidence proves Godot mounted the selected beta PCK. Release-candidate repeat evidence is still required. Current resource-chain evidence shows `doormaker_boss` run-history image entries exist in public but not in `public-beta`; runtime patching now falls back to branch-local `unknown_monster` art for missing run-history icon paths. Mixed-looking beta/public behavior or art asset differences should now be treated as beta game-side content/import-runtime behavior unless selected-PCK evidence contradicts the selected branch.
- Private, inaccessible, password-protected, or no-manifest branch behavior is not release-proven. The synthetic stale saved-branch path is locally proven to block before selected-version download/update, restore to `public`, and avoid Steam Cloud Push.
- Static hardening now covers compact branch-availability status text and diagnostics for password-protected branches: if Steam metadata reports `passwordRequired=true`, launcher failure/status guidance and selected-branch downloadable diagnostics say password-protected/not downloadable instead of downloadable even when Windows depot manifests are visible. True account-side password/private/no-manifest ARM64 evidence is still required or must remain an explicit release-note limitation.
- Save compatibility across Steam branches is unknown and must remain user-facing until proven.
- Branch-switch Steam Cloud Push remains blocked from release signoff until Pull-before-Push, local-save, and backup evidence is captured on the current implementation. Local fix28 evidence at `artifacts/android/fix28-evidence-blocked-push-save-origin-20260619-112107` proves stale selected-version Pull/save-origin evidence blocks before confirmation and writes `last_manual_cloud_push_blocked.txt`; it does not prove successful Push readiness.
- Read-only local fix28 marker evidence at `artifacts/android/fix28-readonly-current-marker-status-20260619-113732` confirms the device still has no successful Push marker and no pre-Push backup evidence after the blocked-Push and login-panel tests; no Steam Cloud action was performed during that capture.
- Android/Samsung/password-manager behavior in the native credential panel is partially provider-validated on local fix28 evidence: Samsung Pass/Android Autofill recognized the Steam web domain and username/password fields, but no matched saved Steam credential was available, and manual real-login/Steam Guard/failed-login/successful-return/Google Password Manager coverage is still missing.
- SteamKit debug log sanitizer and public-sharing warnings are implemented, but public evidence packages still require manual review before posting.

## Release rule

Do not describe Steam game version selection as release-ready until every required evidence item above is either proven on the current release candidate or explicitly documented as an unsupported limitation in the user-facing release notes.
