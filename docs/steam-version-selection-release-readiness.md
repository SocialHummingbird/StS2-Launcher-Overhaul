# Steam Version Selection Release Readiness

This tracker is the current release-readiness contract for Steam game version selection and login hardening.

The implementation is substantially in place, but release readiness is not proven until the evidence rows below are filled from an ARM64 device or an explicitly accepted diagnostic-only emulator path.

## Current status

| Area | Current state | Release posture |
| --- | --- | --- |
| Public/default branch | Public/default remains available and keeps the legacy `files/game` install path. | Requires fresh public/default download, update, and launch evidence on the current APK. |
| Branch selector | Normal users select from a discovered dropdown; manual branch text entry is no longer the normal path. | Requires device evidence for refresh/dropdown behavior and selected-helper text. |
| Branch metadata | Refreshed Steam app-info can expose branch visibility, build IDs, password flags, Windows depot manifests, and availability problems. | Requires account-visible branch evidence for public, available beta, and inaccessible/private/password cases where possible. |
| Blocked branches | Known password-protected, no-manifest, or absent saved branches are blocked before download/update attempts when refreshed app-info proves they are unavailable. | Requires runtime evidence that no silent public fallback occurs. |
| Side-by-side storage | Non-public branches use `files/game_versions/<branch>/game` and matching download-state storage. | Requires beta/non-public install evidence and branch marker contents. |
| Native startup routing | Native startup is expected to block selected-version launch when marker provenance is missing or mismatched. | Requires logcat/native routing evidence for selected public and non-public branches. |
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

   If the account can see a non-public branch, evidence must show branch-aware download into the side-by-side cache, a matching `steam_branch.txt` marker with depot manifest provenance, and launch routing to that selected branch.

5. Unavailable/private/password branch handling.

   Evidence must show unavailable branches are surfaced clearly and blocked before download/update attempts when app-info proves they are not downloadable. If no inaccessible branch can be tested, the release note must keep this as an explicit limitation.

6. No silent public fallback.

   Evidence must show selected non-public branch failures do not silently start, update, or redownload the public/default branch.

7. Cache mutation safety.

   Evidence must show redownload clears only the selected version, inactive cleanup preserves the selected cache, and switch-back-to-public leaves non-public caches alone unless cleanup is explicitly invoked.

8. Save and Steam Cloud safety.

   Evidence must show selected-version Pull-before-Push, Android local save evidence, branch-switch backup requirements, blocked-Push markers when prerequisites are missing, and successful Push only after the required selected-version evidence exists.

9. Autofill behavior.

   Evidence must show the native Autofill dialog can receive credentials from Android/Samsung/password-manager providers, feed the existing Steam login flow, and clear pending values after consume/cancel/timeout/activity stop.

10. Artifact hygiene.

    Public evidence must use the redacted/focused artifacts where possible. Raw logs, full diagnostics, private saves, credentials, tokens, account names, local paths, and device-identifying details must remain local or be manually reviewed and redacted before sharing.

## Known release blockers

- ARM64 validation has not yet proven refreshed dropdown behavior on the current implementation.
- Public/default branch must still be revalidated on the current APK after the latest branch-selector and login-hardening changes.
- Non-public branch download, startup routing, marker provenance, and cleanup still need current-device evidence.
- Private, inaccessible, password-protected, or no-manifest branch behavior is not release-proven.
- Save compatibility across Steam branches is unknown and must remain user-facing until proven.
- Branch-switch Steam Cloud Push remains blocked from release signoff until Pull-before-Push, local-save, and backup evidence is captured on the current implementation.
- Android Autofill provider behavior is implemented but not provider-validated.
- SteamKit debug log sanitizer and public-sharing warnings are implemented, but public evidence packages still require manual review before posting.

## Release rule

Do not describe Steam game version selection as release-ready until every required evidence item above is either proven on the current release candidate or explicitly documented as an unsupported limitation in the user-facing release notes.
