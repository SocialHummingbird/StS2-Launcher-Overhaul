# Branch Protection and Release Readiness

This document defines the repository workflow controls for `main` and release readiness.

## Required state for PRs into `main`

Before a PR is merged, require:

- At least one approving review, or an explicit maintainer bypass policy
- Branch is up to date with target
- PR body includes:
  - Validation steps run, or an explicit blocker explanation
  - Manual verification note when automation is not available
- Conflicts resolved and no unresolved review-blocking comments

## Suggested GitHub branch protection rules

Use one of:

1. GitHub UI:
   - Settings -> Branches -> Add rule for `main`
   - Enable:
     - `Require a pull request before merging`
     - `Require approvals`
     - `Require status checks to pass` when CI exists
     - `Require linear history` optional
     - `Restrict who can push` owner/admin only
2. GitHub CLI, if your token has admin permission:

```bash
gh api repos/SocialHummingbird/StS2-Launcher-Overhaul/branches/main/protection \
  --method PUT \
  --field required_pull_request_reviews='{"required_approving_review_count":1}'
```

When CI is available, prefer required status checks. Release claims should also stay aligned with [current Android status](current-android-status.md):

- `Governance Smoke Check` job: `governance-smoke` in `.github/workflows/overhaul-governance-ci.yml`
- `Build Smoke Check` job: `build-smoke` in `.github/workflows/overhaul-governance-ci.yml`

Build smoke is intentionally safe by default:

- it runs in advisory mode when publish artifacts are missing
- it does not fail merge safety if the repo checkout does not include required assemblies yet

If CI is not yet available, keep status-check enforcement off and switch it on when workflows are added.

## Rollback Branch Policy

Recommended long-term branch model:

- `main`: stable integration branch
- `compat/legacy`: optional fallback branch for emergency maintenance if a major rewrite regression appears
- `rewrite/*`, `fix/*`, `chore/*`: short-lived feature/maintenance branches

### How to create rollback branch

```bash
git fetch origin
git switch -c compat/legacy origin/main
git push origin compat/legacy
```

Keep `compat/legacy` aligned only via explicit, intentionally chosen rollback PRs.
