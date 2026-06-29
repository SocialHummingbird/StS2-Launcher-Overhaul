# Contributing

Before claiming release readiness, check [docs/current-android-status.md](docs/current-android-status.md) and keep Push-to-Cloud overwrite risk explicit until confirmed Push evidence exists.

Thank you for contributing to this overhaul project.

This repository is an independent continuation of the original launcher with a strong focus on reliability and architecture changes.

## Branching

- `main` is the stable base branch for the overhaul.
- `compat/legacy` is maintained as a rollback/compatibility branch when needed.
- Use short-lived feature branches:
  - `rewrite/<area>-<topic>`
  - `fix/<area>`
  - `chore/<topic>`
  - `refactor/<topic>`

## Workflow

1. Create or grab an issue for your work.
2. Implement one small, reviewable change per PR.
3. Add/update notes, migration rationale, and test notes in the PR body.
4. Open a PR marked as draft until you can verify expected behavior.
5. Move to Ready when validation is complete.

## What PRs should include

- Scope-limited changes (small surface area)
- Repro steps (when fixing bugs)
- Validation data (manual logs, device/test case, or build output)
- Any compatibility assumptions or known limitations
- Rollback plan for risky refactors

## Validation priorities

- Prefer local behavior checks on target devices.
- Include log snippets for startup/crash/lifecycle paths when relevant.
- If build/test setup is incomplete, explain exact blockers in PR notes.
- For public bug reports, use the focused issue forms and [issue reporting guide](docs/issue-reporting.md) so device/app version, branch/runtime evidence, cloud safety state, mod state, and redaction checks are captured consistently.

## Communication

- Keep discussion explicit: if behavior changed, state why and what risk remains.
- If a fix is a workaround, label it clearly and include a follow-up task to remove it when feasible.

## Governance and release process

- `main` is PR-only and requires review-based merging.
- The branch protection and rollback expectations are documented in [`docs/branch-protection.md`](docs/branch-protection.md).
- Release cadence, tagging, and backport decisions from upstream are documented in [`docs/release-and-backport-policy.md`](docs/release-and-backport-policy.md).
