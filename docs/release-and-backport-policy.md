# Release, Changelog, and Backport Policy

## Release Cadence

Until CI is fully in place, releases are prepared from `main` after a PR batch that:

1. Is fully reviewed
2. Has validation evidence in PR body or linked issue
3. Closes any open blockers from the current phase

Recommended cadence:

- **Minor release**: milestone-based package updates and major reliability feature merges
- **Hotfix release**: only high-severity regressions or crash fixes, minimal scope

## Changelog and Tag Rules

- Keep `CHANGELOG.md` entries current with each release-related PR.
- Add a short “release note” section under a dated release heading:
  - Why this release exists
  - User-facing changes
  - Reliability/fix highlights
  - Known limitations

### Tag workflow

```bash
git tag -a v0.x.y -m "Release notes: ..."
git push --tags
```

On the same day, publish a GitHub release summary:

- Highlights
- Validation references (issue + log artifacts)
- Full changelog link

## Backport Policy from Upstream

Scope: fixes from `Ekyso/StS2-Launcher` that should be integrated into this fork.

- **Cherry-pick when**:
  - Patch matches current architecture
  - No additional migration logic required
  - Diff is small and conflict-free
- **Rewrite/replace when**:
  - Patch hits architecture paths already refactored in this fork
  - Runtime assumptions differ (mobile/startup orchestrator changes)
  - API contracts are not compatible
- **Delay**:
  - When upstream change is uncertain and requires broad rework, track as follow-up issue and implement in the rewrite path instead of blind cherry-pick.

## Compatibility Tracking

- If a compatibility risk exists, land a guarded version first:
  - add reflection/version fallback
  - retain old path under compatibility checks
  - remove workaround once the new path stabilizes

- For high-risk changes, keep a scoped PR with explicit rollback instructions.
