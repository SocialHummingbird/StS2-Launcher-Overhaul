# Steam Version Selection Release Note Snippet

Use this wording when a build includes Steam game version selection but has not completed the full ARM64 validation runbook.

## Short release note

Steam game version selection is now available for validation. The launcher uses a discovery-led, dropdown-first selector, can refresh account-visible Steam branch metadata with `REFRESH GAME VERSIONS`, keeps non-public branch downloads in side-by-side caches, reports selected-version diagnostics, shows selected-branch availability/password/build metadata in labels/helper text, blocks known unavailable selected branches before download/update attempts, records selected-version notes in branch-switch and Push/Pull evidence, warns before branch switches, and now presents the login/install/play flow through a cleaner status-led portal with a cleaner phase-labeled status-led portal, structured phase chip, error-first guided next-action label, compact vertical next-step hero, larger touch-first action targets, task-led primary action wording, consistent `START GAME` primary CTA, high-contrast rounded actions, a branded atmospheric backdrop, safer Pull-before-Push cloud action ordering, armed Push overwrite warning, collapsible safe first-run guidance, version details, cloud-safety guidance, and cloud options on compact screens, mobile-first compact panel sizing, dynamic compact content width, reduced compact header chrome, compact section headers, compact button labels, titled action sections, and a hidden diagnostics drawer. The latest local ARM64 hardening build validates selected `public-beta` startup from its side-by-side cache after fixing Android app-private path alias checks.

This is still a hardening feature, not release-candidate signoff. Beta password entry, inaccessible/private branch behavior, release-candidate startup/failure routing, cache cleanup, Android/Samsung/password-manager suggestion behavior in the native credential panel, save compatibility across branches, and Steam Cloud Push safety after branch switching still require ARM64 validation evidence.

## Detailed release note

This build adds validation-stage Steam game version selection:

- Default/public branch selection remains always available.
- Discovery-led dropdown branch selection from account-visible Steam app-info, with saved custom branches retained only for compatibility/retry diagnostics.
- Non-mutating `REFRESH GAME VERSIONS` Steam app-info branch metadata refresh.
- Concise dropdown labels with ready/build/password/unavailable badges plus selected-version availability/password/build metadata in helper text.
- Branch-aware manifest resolution and update checks, with known unavailable selected branches blocked before game-version download/update attempts.
- Side-by-side non-public caches under `game_versions/<branch>/`.
- `steam_branch.txt` marker/provenance metadata for completed branch downloads.
- Selected-version diagnostics and native fallback reporting.
- Native startup blocks selected-version launch when branch marker provenance is missing or mismatched.
- Native startup normalizes Android app-private path aliases before marker provenance comparison, and local ARM64 evidence now covers selected `public-beta` launch from `game_versions/public-beta-8128824d/game`.
- Wrapped selector guidance for selected-version status and current limitations.
- Android now uses an integrated native Steam credential panel with real username/password fields, credential-provider hints, Steam web-domain metadata, accessible field labels, inline status/error guidance, keyboard-safe scrollable layout, and stacked full-width touch controls. The unreliable native `USE ANDROID AUTOFILL` handoff popup is no longer user-facing. The launcher still does not store or inject Steam passwords, and password fields clear after request capture.
- Launcher portal polish adds a stronger branded header and atmospheric backdrop with reduced compact chrome, readable phase-labeled status capsule with a structured phase chip and error-first guided next-action label, compact vertical next-step hero, larger touch-first action targets, task-led primary action wording, consistent `START GAME` primary CTA, high-contrast rounded actions, safer Pull-before-Push cloud action ordering, armed Push overwrite warning, collapsible safe first-run guidance, version details, cloud-safety guidance, and cloud options on compact screens, mobile-first compact panel sizing, dynamic compact content width, compact section headers, compact button labels, titled Steam sign-in/Steam Guard/install/play-sync sections, and a diagnostics console that stays hidden until requested.
- SteamKit debug logs are disabled by default; focused auth diagnostics can opt in with `sts2_steamkit_debug_logs=1`, and enabled SteamKit messages are sanitized for credentials/tokens before entering launcher diagnostics.
- Selected-version notes in launcher diagnostics, branch-switch marker evidence, native pre-routing logs, native startup logs, and native fallback diagnostics.
- Selected-version redownload behavior.
- Inactive non-public cache cleanup.
- Branch-switch warnings.
- Local-backup posture before switching branches.
- Manual Push guardrails requiring current-version Pull evidence and Android local save evidence before upload, with stricter selected-version Pull/local-save/backup gates after branch switching.
- Static CI guardrails for version-selection docs, release blockers, unavailable-branch gates, native launch gating, Autofill cleanup, and managed/native selector-guidance parity.

Known limitations:

- Refresh/dropdown behavior still needs ARM64 device evidence.
- Steam beta password entry is not implemented.
- Android/Samsung/password-manager suggestion behavior in the native credential panel still needs ARM64 device evidence.
- Missing/private/password-protected branch behavior still needs ARM64 evidence.
- Release-candidate startup and failure routing still need broader ARM64 evidence beyond the local `public-beta` proof.
- Public-beta branch integrity is still under investigation: Steam may serve some depots with public-identical or public-inherited manifests, and art asset issues require per-depot manifest/file evidence before claiming the beta branch is complete.
- Save compatibility between public and beta branches is not proven.
- Manual Push must remain treated as destructive until current-version Pull evidence, local-save existence, storage permission, local/cloud pre-Push backup evidence, `last_manual_cloud_push.txt`, and aggregate successful selected-version Push evidence are captured. Branch switches additionally require Pull-after-switch evidence for the selected version and aggregate successful post-switch Push evidence.

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
