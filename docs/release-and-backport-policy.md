# Release, Changelog, and Backport Policy

Release and backport decisions should use [current Android status](current-android-status.md) as the baseline: the app works locally on ARM64, but release-candidate hardening remains open.

## Release Cadence

Releases are prepared after a PR batch that:

1. Is fully reviewed
2. Has validation evidence in the PR body or linked issue
3. Closes any open blockers from the current phase

Recommended cadence:

- **Minor release**: milestone-based package updates and major reliability feature merges
- **Hotfix release**: only high-severity regressions or crash fixes, minimal scope

## Changelog and Tag Rules

- Keep `CHANGELOG.md` entries current with each release-related PR.
- Always target the fork explicitly in GitHub CLI release commands. Local `gh` may resolve the upstream `Ekyso/StS2-Launcher` repo from checkout metadata, so scripts and manual commands must pass `--repo SocialHummingbird/StS2-Launcher-Overhaul` unless intentionally inspecting upstream.
- Add a short "release note" section under a dated release heading:
  - Why this release exists
  - User-facing changes
  - Reliability/fix highlights
  - Exact APK asset name, ABI, package name, and SHA-256
  - Exact sidecar assets: `<apk>.sha256` and either `<apk>.json` or `<apk>.build-info.txt`
  - Known limitations, especially confirmed Push-to-Cloud overwrite risk, Steam beta/version selection proof, beta password/private branch behavior, save compatibility across branches, upgrade install behavior, locked-screen interruption, stale-cache freshness, and whether ARM64 release-readiness proof exists

### Tag workflow

```bash
git tag -a v0.x.y -m "Release notes: ..."
git push --tags
```

On the same day, publish a GitHub release summary:

- Highlights
- Validation references, including issues and log artifacts
- Exact APK asset name, ABI, package name, and SHA-256
- Exact package name, versionName, versionCode, signing channel, and whether this is a local/test prerelease or production update baseline
- Known limitations
- Full changelog link

Before announcing or linking a release publicly, run:

```powershell
.\scripts\check-github-release-hygiene.ps1 `
  -Repo "SocialHummingbird/StS2-Launcher-Overhaul" `
  -ReleaseTag "vX.Y.Z"

.\scripts\audit-github-release-inventory.ps1 `
  -Repo "SocialHummingbird/StS2-Launcher-Overhaul" `
  -Limit 40 `
  -OutputPath docs\github-release-inventory.md
```

The hygiene check verifies the selected fork release, APK naming, GitHub asset digest, `.sha256` sidecar, metadata sidecar, and release body agree. The inventory audit shows whether GitHub's non-prerelease Latest target differs from the recommended tester APK and flags historical assets with missing sidecars or incomplete release bodies. A release that fails the targeted check is not ready to advertise even if the APK itself built successfully.

For builds that include Steam game version selection before full ARM64 signoff, use `docs/steam-version-selection-release-note-snippet.md` so release notes describe the feature as validation-stage and keep beta/password/save-safety blockers explicit.
If selector warning text, branch diagnostics, startup routing, cache cleanup, or cloud-save safety changed, include results from `scripts/audit-steam-version-selection.ps1` and `scripts/audit-steam-branch-guidance-parity.ps1` in the release evidence before publishing.

## Backport Policy from Upstream

Scope: fixes from `Ekyso/StS2-Launcher` that should be integrated into this fork.

- **Cherry-pick when**:
  - Patch matches current architecture
  - No additional migration logic required
  - Diff is small and conflict-free
- **Rewrite/replace when**:
  - Patch hits architecture paths already refactored in this fork
  - Runtime assumptions differ, especially mobile/startup orchestrator changes
  - API contracts are not compatible
- **Delay**:
  - When upstream change is uncertain and requires broad rework, track as a follow-up issue and implement in the rewrite path instead of blind cherry-picking.

## Compatibility Tracking

- If a compatibility risk exists, land a guarded version first:
  - add reflection/version fallback
  - retain old path under compatibility checks
  - remove workaround once the new path stabilizes

- For high-risk changes, keep a scoped PR with explicit rollback instructions.
