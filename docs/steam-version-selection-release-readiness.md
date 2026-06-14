# Steam Version Selection Release Readiness

This tracker is the current release-readiness contract for Steam game version selection and login hardening.

The implementation is substantially in place, but release readiness is not proven until the evidence rows below are filled from an ARM64 device or an explicitly accepted diagnostic-only emulator path.

## Current status

Steam game version selection is published in the latest ARM64 APK, but it is not release-candidate signed off until the evidence gates below are repeated on the release candidate.

| Area | Current state | Release posture |
| --- | --- | --- |
| Public/default branch | Public/default remains available and keeps the legacy `files/game` install path. | Requires fresh public/default download, update, and launch evidence on the current APK. |
| Branch selector | Normal users select from a discovered dropdown; manual branch text entry is no longer the normal path. | Requires device evidence for refresh/dropdown behavior and selected-helper text. |
| Branch metadata | Refreshed Steam app-info can expose branch visibility, build IDs, password flags, Windows depot manifests, and availability problems. | Requires account-visible branch evidence for public, available beta, and inaccessible/private/password cases where possible. |
| Blocked branches | Known password-protected, no-manifest, or absent saved branches are blocked before download/update attempts when refreshed app-info proves they are unavailable. | Requires runtime evidence that no silent public fallback occurs. |
| Side-by-side storage | Non-public branches use `files/game_versions/<branch>/game` and matching download-state storage. | Requires beta/non-public install evidence and branch marker contents. |
| Native startup routing | Native startup blocks selected-version launch when marker provenance is missing or mismatched. Local ARM64 evidence now proves selected `public-beta` launch from its side-by-side cache after Android app-private path alias normalization. | Requires release-candidate repeat evidence, public/default retest, and negative-case evidence for missing or untrusted selected slots. |
| Beta branch integrity | Selected branch markers record per-depot effective manifest, selected-branch manifest, public manifest, manifest source, and whether each depot matches or inherits public. Beta-integrity evidence capture now emits a clean-redownload-gated `Classification:`, `Evidence readiness:`, and `Evidence missing/weak:` summary. Local ARM64 evidence classifies `public-beta` as branch-specific installed content and runtime startup now logs the selected PCK path/byte count/SHA-256 mounted by Godot. Public/default and public-beta both reach the visible main menu; public loads the run-history `doormaker_boss` imported textures while public-beta lacks those entries and contains `aeonglass_boss` instead. Runtime patching falls back to branch-local `unknown_monster` art for missing run-history icon paths. | Requires release-candidate repeat evidence and continued user-facing caution that visible beta/public art differences are a game-content/runtime question, not proof of launcher fallback, unless the runtime selected-PCK evidence contradicts the selected branch. |
| Cache cleanup | Selected-version redownload and inactive cache cleanup are branch-aware by design. | Requires evidence that selected cache is preserved or cleared only when intended. |
| Steam Cloud safety | Manual Push requires selected-version Pull evidence and Android local save evidence; branch-switch Push adds backup evidence gates. | Requires Pull-before-Push, blocked-Push, and successful-Push evidence on the selected version before signoff. |
| Autofill | Android builds expose one-shot native Autofill handoff and do not store Steam passwords for Autofill. | Requires Samsung/Android/password-manager provider validation on device. |
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

   Evidence must show unavailable branches are surfaced clearly and blocked before download/update attempts when app-info proves they are not downloadable. If no inaccessible branch can be tested, the release note must keep this as an explicit limitation.

6. No silent public fallback.

   Evidence must show selected non-public branch failures do not silently start, update, or redownload the public/default branch.

7. Beta branch integrity.

   Evidence must show whether selected branch depots are public-identical, branch-specific, or explicitly inherited from public by comparing selected and public manifest IDs per depot. If art assets look public while other behavior looks beta, evidence must include the branch marker public-comparison counts, selected branch file inventory, public branch file inventory, focused logcat, key asset or PCK hashes, runtime selected-PCK path/byte count/SHA-256, and `Evidence readiness:` not blocked in `beta-integrity-summary.txt`. Runtime PCK hashes can differ from raw Steam inventory hashes because Android download completion patches the PCK in place before launch.

8. Cache mutation safety.

   Evidence must show redownload clears only the selected version, inactive cleanup preserves the selected cache, and switch-back-to-public leaves non-public caches alone unless cleanup is explicitly invoked.

9. Save and Steam Cloud safety.

   Evidence must show selected-version Pull-before-Push, Android local save evidence, branch-switch backup requirements, blocked-Push markers when prerequisites are missing, and successful Push only after the required selected-version evidence exists.

10. Autofill behavior.

   Evidence must show the native Autofill dialog can receive credentials from Android/Samsung/password-manager providers, feed the existing Steam login flow, and clear pending values after consume/cancel/timeout/activity stop.

11. Artifact hygiene.

    Public evidence must use the redacted/focused artifacts where possible. Raw logs, full diagnostics, private saves, credentials, tokens, account names, local paths, and device-identifying details must remain local or be manually reviewed and redacted before sharing.

## Known release blockers

- ARM64 validation has not yet proven refreshed dropdown behavior on the current implementation.
- Public/default branch must still be revalidated on the current APK after the latest branch-selector and login-hardening changes.
- Non-public branch cleanup, private/password failure handling, and release-candidate startup routing still need current-device evidence.
- Public versus public-beta branch integrity is classified locally as branch-specific installed content, and runtime evidence proves Godot mounted the selected beta PCK. Release-candidate repeat evidence is still required. Current resource-chain evidence shows `doormaker_boss` run-history image entries exist in public but not in `public-beta`; runtime patching now falls back to branch-local `unknown_monster` art for missing run-history icon paths. Mixed-looking beta/public behavior or art asset differences should now be treated as beta game-side content/import-runtime behavior unless selected-PCK evidence contradicts the selected branch.
- Private, inaccessible, password-protected, or no-manifest branch behavior is not release-proven.
- Save compatibility across Steam branches is unknown and must remain user-facing until proven.
- Branch-switch Steam Cloud Push remains blocked from release signoff until Pull-before-Push, local-save, and backup evidence is captured on the current implementation.
- Android Autofill provider behavior is implemented but not provider-validated.
- SteamKit debug log sanitizer and public-sharing warnings are implemented, but public evidence packages still require manual review before posting.

## Release rule

Do not describe Steam game version selection as release-ready until every required evidence item above is either proven on the current release candidate or explicitly documented as an unsupported limitation in the user-facing release notes.
