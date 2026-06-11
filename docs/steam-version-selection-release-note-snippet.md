# Steam Version Selection Release Note Snippet

Use this wording when a build includes Steam game version selection but has not completed the full ARM64 validation runbook.

## Short release note

Steam game version selection is now available for validation. The launcher can switch between the default/public Steam branch and a named `beta` branch, keeps non-public branch downloads in side-by-side caches, reports selected-version diagnostics, shows wrapped selector guidance, records selected-version notes in branch-switch evidence, and warns before branch switches.

This is still a hardening feature, not release-candidate signoff. Beta password entry, inaccessible/private beta behavior, native startup routing, cache cleanup, save compatibility across branches, and Steam Cloud Push safety after branch switching still require ARM64 validation evidence.

## Detailed release note

This build adds validation-stage Steam game version selection:

- Default/public branch selection.
- Named `beta` branch selection.
- Branch-aware manifest resolution and update checks.
- Side-by-side non-public caches under `game_versions/<branch>/`.
- `steam_branch.txt` marker/provenance metadata for completed branch downloads.
- Selected-version diagnostics and native fallback reporting.
- Wrapped selector guidance for public/beta limitations.
- Selected-version notes in launcher diagnostics, branch-switch marker evidence, native pre-routing logs, native startup logs, and native fallback diagnostics.
- Selected-version redownload behavior.
- Inactive non-public cache cleanup.
- Branch-switch warnings.
- Local-backup posture before switching branches.
- Manual Push guardrails after branch switching when backup storage permission is unavailable.
- Static CI guardrails for version-selection docs, release blockers, and managed/native selector-guidance parity.

Known limitations:

- Arbitrary Steam branch discovery is not implemented.
- Steam beta password entry is not implemented.
- Missing/private/password-protected beta branch behavior still needs ARM64 evidence.
- Save compatibility between public and beta branches is not proven.
- Manual Push after branch switching must remain treated as destructive until Pull-after-switch evidence for the selected version, local-save existence, storage permission, local/cloud pre-Push backup evidence, `last_manual_cloud_push.txt`, and aggregate successful post-switch Push evidence are captured.

Validation references:

- `docs/steam-version-selection-user-guide.md`
- `docs/steam-version-selection-validation.md`
- `docs/steam-version-selection-runbook.md`
- `docs/steam-version-selection-evidence-template.md`
- `scripts/audit-steam-version-selection.ps1`
- `scripts/audit-steam-branch-guidance-parity.ps1`

## Do not say yet

Do not claim:

- Steam beta/version selection is release-ready.
- Password-protected beta branches are supported.
- Private/inaccessible beta branches have known behavior.
- Public and beta saves are compatible.
- Steam Cloud Push is safe after switching branches without Pull and backup evidence.
- Emulator evidence is enough for release signoff.
